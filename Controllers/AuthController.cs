using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace InterviewLab.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(IConfiguration configuration) : ControllerBase
{
    [HttpPost("token")]
    public IActionResult CreateToken([FromBody] TokenRequest request)
    {
        // JWT Q331, Q333, Q334, Q342: create a signed token carrying name/role claims; demo credentials only.
        // Deliberately simple demo credentials. Replace with ASP.NET Core Identity or an OIDC provider.
        if (request.Username != "reader" || request.Password != "demo-password") return Unauthorized();
        var issuer = configuration["Jwt:Issuer"]!;
        var audience = configuration["Jwt:Audience"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience,
            [new Claim(ClaimTypes.Name, request.Username), new Claim(ClaimTypes.Role, "Reader")],
            expires: DateTime.UtcNow.AddMinutes(15), signingCredentials: credentials);
        return Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token), tokenType = "Bearer", expiresInSeconds = 900 });
    }
}

public sealed record TokenRequest(string Username, string Password);
