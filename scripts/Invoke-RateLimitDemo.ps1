param(
    [int]$Requests = 20,
    [string]$BaseUrl = "http://localhost:5257"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Net.Http
$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds(30)
$url = "$($BaseUrl.TrimEnd('/'))/api/demo/rate-limited"
Write-Host "Sending $Requests concurrent requests to $url"

try {
    $tasks = @(
        for ($i = 0; $i -lt $Requests; $i++) {
            $client.GetAsync($url)
        }
    )
    $null = [System.Threading.Tasks.Task]::WhenAll($tasks).GetAwaiter().GetResult()
    $statuses = @($tasks | ForEach-Object { [int]$_.Result.StatusCode })
    $allowed = @($statuses | Where-Object { $_ -eq 200 }).Count
    $limited = @($statuses | Where-Object { $_ -eq 429 }).Count
    $other = @($statuses | Where-Object { $_ -ne 200 -and $_ -ne 429 })

    Write-Host "Allowed (200): $allowed"
    Write-Host "Rate limited (429): $limited"
    if ($other.Count -gt 0) { Write-Host "Other status codes: $($other -join ', ')" }
    Write-Host "Policy: 5 requests per 10 seconds per process/IP partition. Wait 10 seconds before rerunning."
}
finally {
    $client.Dispose()
}
