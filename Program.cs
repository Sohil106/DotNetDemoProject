using System.Text;
using System.Threading.RateLimiting;
using InterviewLab.Data;
using InterviewLab.Middleware;
using InterviewLab.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
// .NET Core & Web API Q2, Q4, Q21; DI Q312, Q325: minimal hosting, logging, and service registration.
// Keep this portable in restricted Windows environments where Event Log writes are unavailable.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "obj", "data-protection-keys")))
    .SetApplicationName("InterviewLab");
var signingKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey is required.");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Interview Lab API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        In = ParameterLocation.Header, Description = "Paste a token returned by POST /api/auth/token."
    });
});
builder.Services.AddDbContext<LabDbContext>(options =>
{
    // EF Core Q205: select a provider. InMemory is quick for local demos; SQL Server needs a reachable server.
    if (builder.Configuration["Database:Provider"]?.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
        options.UseSqlServer(builder.Configuration.GetConnectionString("InterviewLab"));
    else
        options.UseInMemoryDatabase("InterviewLab");
});
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddSingleton<DemoCounters>();
builder.Services.AddHostedService<HeartbeatService>();
// DI Q312: scoped service, singleton service, and hosted service registrations.
builder.Services.AddCors(options => options.AddPolicy("DemoClient", policy =>
    policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("lab-database");
builder.Services.AddRateLimiter(options =>
{
    // .NET Core & Web API Q18: fixed-window limiter used by /api/demo/rate-limited.
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("demo", _ => RateLimitPartition.GetFixedWindowLimiter("demo", _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = 5, Window = TimeSpan.FromSeconds(10), QueueLimit = 0, AutoReplenishment = true
    }));
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    // JWT Q333, Q335, Q343: validate signing key, issuer, audience, and token lifetime.
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(5)
    };
});
builder.Services.AddAuthorization(options => options.AddPolicy("Readers", policy => policy.RequireRole("Reader")));

var app = builder.Build();
await DemoSeed.SeedAsync(app.Services);
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<DemoExceptionMiddleware>();
// Middleware Q351, Q355, Q366: order matters; correlation IDs wrap the exception handler and downstream pipeline.
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("DemoClient");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
// Middleware Q365: rate limiting runs before auth here; authentication must precede authorization.
app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/api/demo/rate-limited", (HttpContext context, DemoCounters counters) =>
{
    var count = counters.Increment();
    return Results.Ok(new { message = "Request passed the limiter", requestNumber = count, utc = DateTimeOffset.UtcNow });
}).RequireRateLimiting("demo").WithTags("Demo");

app.Run();

public partial class Program;

public sealed class DatabaseHealthCheck(LabDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try { return await db.Database.CanConnectAsync(cancellationToken) ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Database is unavailable."); }
        catch (Exception exception) { return HealthCheckResult.Unhealthy("Database check failed.", exception); }
    }
}
