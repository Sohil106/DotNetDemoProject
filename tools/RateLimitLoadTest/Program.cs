var requestCount = args.Length > 0 && int.TryParse(args[0], out var parsedCount) ? parsedCount : 20;
var baseUrl = args.Length > 1 ? args[1].TrimEnd('/') : "http://localhost:5257";
if (requestCount is < 1 or > 10_000)
{
    Console.Error.WriteLine("Request count must be between 1 and 10000.");
    return 2;
}

using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var url = $"{baseUrl}/api/demo/rate-limited";
Console.WriteLine($"Sending {requestCount} concurrent requests to {url}");
try
{
    // .NET Core & Web API Q18, Q38: concurrent HttpClient calls make rate-limit responses measurable.
    var tasks = Enumerable.Range(0, requestCount).Select(_ => client.GetAsync(url));
    var responses = await Task.WhenAll(tasks);
    var counts = responses.GroupBy(response => (int)response.StatusCode)
        .OrderBy(group => group.Key)
        .Select(group => new { Status = group.Key, Count = group.Count() });
    foreach (var item in counts) Console.WriteLine($"HTTP {item.Status}: {item.Count}");
    var allowed = responses.Count(response => (int)response.StatusCode == 200);
    Console.WriteLine($"Allowed: {allowed}; rate limited: {responses.Length - allowed}");
    return 0;
}
catch (HttpRequestException exception)
{
    Console.Error.WriteLine($"Could not reach the API: {exception.Message}");
    return 1;
}
