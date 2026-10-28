# Interview Lab feature and testing guide

This guide describes the current sample project, where each feature lives, how to exercise it, and what data it expects. The main app is an ASP.NET Core Web API. Angular is not included. The guide uses PowerShell examples, but the same requests can be sent from Swagger UI, `InterviewLab.http`, curl, or another HTTP client.

## 1. Start the project

Prerequisites: .NET 10 SDK. SQL Server is optional; the default EF Core InMemory provider needs no separate service.

From the project root:

```powershell
dotnet restore
dotnet run
```

The launch profile in `Properties/launchSettings.json` serves HTTP at `http://localhost:5257` in the Development environment. Open `http://localhost:5257/swagger` for Swagger UI. The OpenAPI document is at `/swagger/v1/swagger.json`. Use `InterviewLab.http` for starter requests. Its `@baseUrl` is set to port 5257.

To stop the API, press Ctrl+C in its terminal. A `dotnet build` builds the API. The separate rate-load utility has its own project file and is excluded from the API compile items in `InterviewLab.csproj`.

## 2. Seed data and persistence

`Data/LabDbContext.cs`, `LabDbContext.OnModelCreating`, and `DemoSeed.SeedAsync` define and create the demo database. Every app start calls `EnsureCreatedAsync()` and seeds these rows:

| Entity | ID | Values |
|---|---:|---|
| Category | 1 | Books |
| Category | 2 | Hardware |
| Product | 1 | C# in Depth, 42.50, stock 12, category 1 |
| Product | 2 | Mechanical Keyboard, 89.99, stock 8, category 2 |
| Product | 3 | ASP.NET Core Guide, 35.00, stock 20, category 1 |

The normal setup uses `Database:Provider = InMemory` in `appsettings.json`. InMemory data lives for the lifetime of that app process: POST-created products remain while the process runs, then disappear when it stops. Restart the app to restore the three seed products and clear any products created during a demo. This provider is not a relational database emulator; it does not provide realistic SQL translation, constraints, transactions, or persistence checks.

### Optional SQL Server setup

`Program.cs` selects `UseSqlServer` when `Database:Provider` is `SqlServer`; otherwise it selects InMemory. `ConnectionStrings:InterviewLab` in `appsettings.json` is a LocalDB example. Override it for your server and do not put real credentials in source control:

```powershell
$env:Database__Provider = 'SqlServer'
$env:ConnectionStrings__InterviewLab = 'Server=(localdb)\MSSQLLocalDB;Database=InterviewLab;Trusted_Connection=True;TrustServerCertificate=True'
dotnet run
```

The app runs `EnsureCreatedAsync` against the configured provider on startup, so SQL Server mode needs a reachable server and database permissions that can create the schema. This sample has no EF migrations. Stop the app and drop the disposable `InterviewLab` database in SQL Server/LocalDB to clean it up. Do not point the lab at a database with valuable data. SQL snippets in `docs/sql-examples.sql` are not run automatically.

## 3. Feature map

| Feature | Implementation location | How to exercise it |
|---|---|---|
| Hosting, DI, middleware, Swagger, CORS, health, rate limit, auth | [`Program.cs`](../Program.cs#L14) (`builder` registration), [`Program.cs`](../Program.cs#L74) (pipeline and routes) | Start app; open `/swagger`; request `/health`; test the routes below. |
| Product REST API / validation | [`ProductsController.cs`](../Controllers/ProductsController.cs#L9), `List` at L13, `Get` at L22, `Create` at L26; [`Product.cs`](../Models/Product.cs#L26), request DTO validation | GET/list, GET by ID, and authenticated POST. |
| EF Core model, seed, provider selection | [`LabDbContext.cs`](../Data/LabDbContext.cs#L6), `OnModelCreating` at L11, `DemoSeed.SeedAsync` at L30; `Program.cs` provider selection at L36 | Query seeded products; optionally use SQL Server mode. |
| EF Core/LINQ query service | [`ProductService.cs`](../Services/ProductService.cs#L15), `SearchAsync` at L17, `GetAsync` at L28, `CreateAsync` at L32 | Search/paginate product endpoint; create then list. |
| JWT issue and validation | [`AuthController.cs`](../Controllers/AuthController.cs#L10), `CreateToken` at L13; `Program.cs` validation at L61 and policy at L72 | Obtain a token, then POST a product with the bearer token. |
| Bounded multipart file upload | [`FilesController.cs`](../Controllers/FilesController.cs#L6), `Upload` at L13 | Upload a small `.txt`, `.pdf`, `.png`, `.jpg`, or `.jpeg` file. |
| C# language examples | [`CSharpExamples.cs`](../Examples/CSharpExamples.cs#L3); [`LearningController.cs`](../Controllers/LearningController.cs#L11), `CSharp` | GET `/api/learning/csharp`. |
| LINQ to Objects examples | [`LearningController.cs`](../Controllers/LearningController.cs#L34), `Linq` | GET `/api/learning/linq`. |
| Generic collections | [`LearningController.cs`](../Controllers/LearningController.cs#L45), `Collections` | GET `/api/learning/collections`. |
| Async stream and cancellation | [`LearningController.cs`](../Controllers/LearningController.cs#L56), `AsyncStream` | GET `/api/learning/async-stream`; interrupt the request to cancel. |
| Memory cache | [`LearningController.cs`](../Controllers/LearningController.cs#L67), `CacheDemo`; `Program.cs`, `AddMemoryCache` at L44 | GET `/api/learning/cache` repeatedly within 30 seconds. |
| Global exception response | [`DemoExceptionMiddleware.cs`](../Middleware/DemoExceptionMiddleware.cs#L5), `InvokeAsync` at L7; `LearningController.Throw` at L80 | GET `/api/learning/throw`; inspect 500 Problem Details and trace ID. |
| Correlation ID | [`CorrelationIdMiddleware.cs`](../Middleware/CorrelationIdMiddleware.cs#L3), `InvokeAsync` at L5 | Send valid/invalid `X-Correlation-ID`; inspect response header. |
| Hosted background work / thread-safe count | [`DemoServices.cs`](../Services/DemoServices.cs#L3), `HeartbeatService.ExecuteAsync` at L14, `DemoCounters`; register at `Program.cs` L45-L47 | Watch console for a minute; `/api/learning/cache` returns successful rate-limited request count. |
| Concurrent rate-limit generator | [`RateLimitLoadTest/Program.cs`](../tools/RateLimitLoadTest/Program.cs#L1), top-level client code; [`Invoke-RateLimitDemo.ps1`](../scripts/Invoke-RateLimitDemo.ps1) | Run the C# utility or PowerShell script in a second terminal. |
| SQL query practice | [`sql-examples.sql`](sql-examples.sql#L1) | Run statements individually against a disposable SQL Server database after schema creation. |
| Question-to-code topic index | [`CONCEPTS.md`](CONCEPTS.md) | Use to map the extracted PDF questions to examples and topics needing conceptual study. |

## 4. API feature test recipes

### Swagger, health, and CORS

**Where:** Swagger generation and middleware are in `Program.cs` (`AddSwaggerGen`, `UseSwagger`, `UseSwaggerUI`). CORS policy `DemoClient` allows origin `http://localhost:4200`; Angular itself is not part of this project. The health route is mapped to `DatabaseHealthCheck` at `/health`.

**Test:**

```powershell
Invoke-WebRequest -UseBasicParsing http://localhost:5257/swagger/index.html
Invoke-RestMethod http://localhost:5257/health
```

Expected: Swagger page returns 200; health returns `Healthy` when the configured provider is reachable. For a preflight check:

```powershell
curl.exe -i -X OPTIONS http://localhost:5257/api/products `
  -H 'Origin: http://localhost:4200' `
  -H 'Access-Control-Request-Method: GET'
```

Look for `Access-Control-Allow-Origin: http://localhost:4200`. A different origin is not in the sample allowlist.

**Data/setup:** no request data is needed. If SQL Server mode is selected, start the database before the API; otherwise health can report unhealthy or the app may fail during `EnsureCreatedAsync`.

### Product API, binding, validation, pagination, EF Core, and LINQ

**Where:** routes and HTTP results are in `ProductsController.List`, `.Get`, and `.Create`. Request and response types are `CreateProductRequest` and `ProductDto` in `Models/Product.cs`. EF configuration and seed rows are in `LabDbContext.OnModelCreating`. Query composition and database calls are in `ProductService.SearchAsync`, `.GetAsync`, and `.CreateAsync`.

**List/filter/page test:**

```powershell
Invoke-RestMethod 'http://localhost:5257/api/products?q=ASP&page=1&pageSize=10'
```

Expected: one matching item, `ASP.NET Core Guide`, and paging fields `page`, `pageSize`, `total`, and `totalPages`. To see the seeded list without filtering, request `/api/products`.

**Other read tests:**

```powershell
Invoke-RestMethod http://localhost:5257/api/products/1
try { Invoke-RestMethod http://localhost:5257/api/products/999 } catch { $_.Exception.Message }
try { Invoke-RestMethod 'http://localhost:5257/api/products?page=0' } catch { $_.Exception.Message }
```

Expected: ID 1 returns `C# in Depth`; unknown ID returns 404; invalid pagination returns a validation problem response (400). `pageSize` must be from 1 through 100.

**Create test data:** first obtain a token using the JWT section below, then use it in this request:

```powershell
$tokenResponse = Invoke-RestMethod -Method Post `
  -Uri http://localhost:5257/api/auth/token `
  -ContentType 'application/json' `
  -Body '{"username":"reader","password":"demo-password"}'

$headers = @{ Authorization = "Bearer $($tokenResponse.accessToken)" }
$body = '{"name":"Demo Notebook","price":12.50,"stock":4,"categoryId":1}'
Invoke-RestMethod -Method Post -Uri http://localhost:5257/api/products `
  -Headers $headers -ContentType 'application/json' -Body $body
```

Expected: 201 Created with a generated ID and category name. Verify it with `GET /api/products/{id}` or the list endpoint. Try `categoryId: 999` to exercise the service's category check; it returns validation problem details. Try omitting the token to verify authorization returns 401. The POST request's validation annotations constrain name length, price, stock, and category ID.

**Cleanup:** with InMemory, restart the API to discard the created product and restore the seed set. In SQL Server mode, delete the inserted row from the disposable database or drop the disposable database after stopping the app.

### JWT and role authorization

**Where:** `AuthController.CreateToken` issues tokens; `Program.cs` configures validation and the `Readers` policy; `ProductsController.Create` requires that policy. `TokenRequest` is the JSON input model.

**Dummy credentials and request:**

```json
{"username":"reader","password":"demo-password"}
```

POST it to `/api/auth/token`. Expected response has `accessToken`, `tokenType: "Bearer"`, and `expiresInSeconds: 900`. Use the returned value as `Authorization: Bearer <accessToken>` when POSTing a product. Wrong credentials return 401; a missing or invalid token on product POST returns 401; a valid token without the required role would fail authorization (403).

The JWT is signed with the configured demo symmetric key and validated for issuer, audience, signing key, and lifetime. Credentials and the key in `appsettings.json` are training-only. No refresh token, revocation store, Identity, or external identity provider is configured. The OpenAPI document defines a Bearer scheme; if Swagger UI does not attach it to the protected operation, use the explicit header request above or `InterviewLab.http`.

**Cleanup:** no server-side session is created. Let the 15-minute token expire or discard it. Restarting the app does not invalidate a token while it remains valid because the configured demo signing key stays the same.

### Rate limiting by firing concurrent requests

**Where:** policy registration is `Program.cs`, `AddRateLimiter`; the endpoint is `/api/demo/rate-limited`; the singleton counter is `DemoCounters.Increment`. The preferred runner is `tools/RateLimitLoadTest/Program.cs`; the alternate is `scripts/Invoke-RateLimitDemo.ps1`.

Start the API in one terminal. From another terminal run:

```powershell
dotnet run --project tools/RateLimitLoadTest -- 20 http://localhost:5257
```

Expected in a fresh window: approximately `HTTP 200: 5` and `HTTP 429: 15`. The policy permits five requests in a 10-second fixed window with no queue. Wait at least ten seconds before repeating. The policy uses one constant partition key to make results reproducible locally; it is not a per-user distributed production limiter.

Alternative PowerShell runner:

```powershell
.\scripts\Invoke-RateLimitDemo.ps1 -Requests 20 -BaseUrl http://localhost:5257
```

It has the same limit and reports 200/429 counts. If the machine's script execution policy blocks `.ps1` files, use the C# command above.

**Data/setup/cleanup:** no database data is used. The counter is an in-memory singleton and only increments for accepted calls. Restarting the API resets it and the limiter window; otherwise wait for the window to replenish.

### File upload

**Where:** `FilesController.Upload` handles `multipart/form-data`. The controller caps a file at 5 MiB, allows `.txt`, `.pdf`, `.png`, `.jpg`, and `.jpeg`, writes under the app's `uploads/` directory with a generated name, and returns the stored ID and size. `uploads/` is gitignored. There is no download or delete route in this sample.

Create a temporary test file, upload it, then clean up the temporary and stored copies:

```powershell
Set-Content -Path .\sample-upload.txt -Value 'Interview Lab upload sample'
curl.exe -i -F 'file=@sample-upload.txt' http://localhost:5257/api/files
Remove-Item .\sample-upload.txt
```

Expected: 201 with JSON containing an opaque `id`, byte count, and original filename. Test rejection with an unsupported extension (400), missing/empty file (400), or a file larger than 5 MiB (413). Keep test uploads small; don't use sensitive files.

**Cleanup:** remove the `uploads/` folder under the project root after the demo. The API does not provide a cleanup endpoint.

### C# concepts, LINQ, collections, async streams, and cache

**Where:** endpoints are in `LearningController`; reusable language examples are in `Examples/CSharpExamples.cs`.

| Request | Topics / expected observation | Data needed |
|---|---|---|
| `GET /api/learning/csharp` | Record copy with `with`, init-only property, collection expression, tuple deconstruction, extension method, pattern matching, switch expression, lazy iterator. Output includes `iteratorEvenNumbers: [2,4,6]`, `scoreBand: A`, a `Topic: async/await` label, and quotient 3/remainder 2. | None; values are local to the method. |
| `GET /api/learning/linq` | LINQ filtering, ordering, indexed `Select`, grouping, `ToDictionary`, `Any`, deferred execution until materialization. Top scores are 100, 91, 88; score 72 appears twice. | Fixed `scores` array in the method. |
| `GET /api/learning/collections` | `HashSet` with case-insensitive comparer, dictionary, queue FIFO and stack LIFO. `queueNext` is `queued-1`; `stackNext` is `top`. | Fixed collection literals in the method. |
| `GET /api/learning/async-stream` | `IAsyncEnumerable<int>`, awaited delay, cancellation token. Values 1-5 arrive over roughly 750 ms. | None. Cancel the client request to stop enumeration. |
| `GET /api/learning/cache` | `IMemoryCache` absolute expiration and the accepted-rate-request singleton counter. `cachedAtUtc` stays constant for 30 seconds; after expiry it refreshes. | None. Counter resets when API restarts. |

`CheckedAdd` is present as a checked arithmetic example in `CSharpExamples.cs`; it is not currently called by a route.

### Exception handling and correlation ID

**Where:** middleware is registered in `Program.cs`. `CorrelationIdMiddleware.InvokeAsync` uses a valid incoming `X-Correlation-ID` GUID or generates one; `DemoExceptionMiddleware.InvokeAsync` logs the exception and returns a sanitized Problem Details body. `/api/learning/throw` intentionally throws.

**Test:**

```powershell
curl.exe -i http://localhost:5257/api/learning/throw
curl.exe -i http://localhost:5257/health -H 'X-Correlation-ID: 11111111-2222-3333-4444-555555555555'
```

Expected: the intentional error returns 500 with `Content-Type: application/problem+json`, a generic title, and `traceId`; detailed exception is only in server logs. The second response returns the same valid GUID in `X-Correlation-ID`. Send a non-GUID header to see a new generated ID.

**Data/setup/cleanup:** no dummy data. The exception route is intentionally failing by design; do not use it as a real health check. No state is left behind.

### Background service, health, logging, and configuration

**Where:** `HeartbeatService.ExecuteAsync` logs once a minute and observes the host stopping token. `DemoCounters` uses `Interlocked`. `DatabaseHealthCheck.CheckHealthAsync` probes the configured EF provider. Logging, settings, service lifetimes, and health registration are in `Program.cs`; settings are in `appsettings.json` and `appsettings.Development.json`.

**Test:** start the API and watch its console for the first heartbeat, then wait one minute for the next. Request `/health`. Press Ctrl+C and observe graceful host shutdown. The sample registers `IProductService` as scoped, `DemoCounters` as singleton, and `HeartbeatService` as hosted. It does not include a separate transient service or demonstrate scoped service creation inside a recurring background task.

**Data/setup/cleanup:** no test data beyond the configured database. The counter resets on process restart. Data Protection keys are written under ignored `obj/data-protection-keys` for this local sample; do not treat this local configuration as production key management.

### SQL examples

**Where:** `docs/sql-examples.sql` contains joins and grouping, offset pagination, a CTE with `ROW_NUMBER`, a transaction with `OUTPUT`, parameterized search, and an example supporting index.

**Setup:** run SQL Server mode as described above and use the database/schema created by `EnsureCreatedAsync`, or create a separate disposable schema with matching `dbo.Categories` and `dbo.Products` tables. The file is a set of teaching snippets, not one batch to execute blindly:

- `@Page` and `@PageSize` are declared in the pagination example.
- `@Quantity`, `@ProductId`, and `@SearchTerm` must be supplied/declared for the relevant statements.
- The transaction example intentionally ends with `ROLLBACK`; verify the `OUTPUT` row, then rollback. Do not change to `COMMIT` against valuable data.
- Run `CREATE INDEX` once only; skip it if that index already exists.

**Cleanup:** the transaction's update is rolled back. If you create test rows, delete them or drop the disposable database. Never run the file against production.

## 5. State and cleanup checklist

- Stop the Web API with Ctrl+C when finished.
- InMemory products and the rate-limit counter reset with the process; cache entries disappear with the process (or expire after 30 seconds).
- Remove the `uploads/` directory if you exercised file upload.
- Remove `sample-upload.txt` if it remains after a failed upload command.
- If SQL Server mode was used, remove the disposable database and any manually inserted test rows after stopping the app.
- No test project is included; validation here is through Swagger/HTTP requests and the standalone request-firing utility.

## 6. Deliberate limits of the sample

This is a compact local learning app. The included feature set does not implement API versioning, API-key auth, refresh-token rotation, Identity/OIDC, distributed rate limiting, response caching middleware, custom model binders, gRPC, SignalR/WebSockets, reverse proxy deployment, migrations, SQL concurrency handling, or cloud hosting. `CONCEPTS.md` maps those interview areas to what can be discussed or what external setup would be required. Angular remains excluded.
