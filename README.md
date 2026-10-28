# .NET Interview Lab

A runnable ASP.NET Core Web API for practicing the .NET, C#, EF Core, LINQ, collections, async, dependency injection, JWT, middleware, and SQL questions in `All_Questions_Extracted_From_PDF.md`. Angular is intentionally excluded.

## Run it

Requires the .NET 10 SDK. The dependencies are in the project file; restore with your normal NuGet sources, then:

```powershell
dotnet restore
dotnet run
```

Open Swagger UI at <http://localhost:5257/swagger>. OpenAPI JSON is at `/swagger/v1/swagger.json`; health is at `/health`. Use the `.http` file for a few API requests.

The default provider is EF Core InMemory so the project starts without a database server. It is reset when the process stops. To try SQL Server, set `Database:Provider` to `SqlServer` and configure the `ConnectionStrings:InterviewLab` value using user secrets or an environment variable such as `ConnectionStrings__InterviewLab`. `Database:Provider` can be set with `Database__Provider`. SQL Server mode requires a reachable SQL Server and schema creation/migrations.

## Try the demos

- `GET /api/products?q=core&page=1&pageSize=10`: EF Core relationship, `AsNoTracking`, projection, filtering, paging and validation.
- `POST /api/auth/token` with `{"username":"reader","password":"demo-password"}`: returns a short lived JWT. Authorize in Swagger using the bearer token. `POST /api/products` requires the `Reader` role.
- `GET /api/learning/csharp`, `/linq`, `/collections`, `/async-stream`, `/cache`: language, LINQ, collection, async iterator and memory-cache examples.
- `GET /api/learning/throw`: exercises global Problem Details error handling and correlation IDs.
- `POST /api/files` as `multipart/form-data` with a `file` field: demonstrates bounded asynchronous file upload with generated storage names and an extension allowlist. Files go under ignored `uploads/`.
- `GET /api/demo/rate-limited`: fixed-window policy allows five requests per ten seconds per running app instance. See the load test below.
- `GET /health`: health check through DI.
- Background heartbeat logs once per minute. DI lifetimes, options/configuration, logging and middleware are in `Program.cs` and `Services/`.

## Fire concurrent requests

Start the app in one terminal, then in another run:

```powershell
.\scripts\Invoke-RateLimitDemo.ps1 -Requests 20
```

Or use the .NET load runner (works without PowerShell script execution):

```powershell
dotnet run --project tools/RateLimitLoadTest -- 20 http://localhost:5257
```

It sends the requests concurrently and reports HTTP 200 and 429 counts. Expect at most five 200 responses in a fresh ten second window from that process. Run it again after the window resets. The default limiter partitions by a constant key to make the demo easy to reproduce; a real service should partition by a trusted client identity or IP and use a distributed limiter for multiple instances.

## What is in the sample

[`docs/FEATURE_GUIDE.md`](docs/FEATURE_GUIDE.md) is the detailed feature manual with file/method locations, manual test recipes, dummy data, and cleanup steps. `docs/CONCEPTS.md` maps the source to the extracted interview topics and marks topics that are better discussed than simulated in a local API. `docs/sql-examples.sql` has SQL Server examples for joins, grouping, window functions, CTEs, transactions, pagination and safe parameters. The lab focuses on .NET. Cloud hosting, production identity, real load infrastructure, Angular, and database administration require external systems and are not pretended to be production-ready here.

> The JWT credential and signing key in appsettings are local demo values only. Configure secrets and use an established identity provider before deploying anything.
