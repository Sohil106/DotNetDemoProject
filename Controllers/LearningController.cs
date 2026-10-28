using InterviewLab.Services;
using InterviewLab.Examples;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace InterviewLab.Controllers;

[ApiController, Route("api/learning")]
public sealed class LearningController(IMemoryCache cache, DemoCounters counters) : ControllerBase
{
    [HttpGet("csharp")]
    public IActionResult CSharp()
    {
        // C# Q13, Q14, Q38, Q40, Q49, Q57: records, init, pattern matching, with, and collection expressions.
        var original = new InterviewNote("records", "Value-based equality") { Status = "draft" };
        var published = original with { Status = "ready" };
        int[] numbers = [1, 2, 3, 4, 5, 6];
        var (quotient, remainder) = CSharpExamples.Divide(17, 5);
        var scoreBand = 92 switch { >= 90 => "A", >= 80 => "B", _ => "Needs work" };
        return Ok(new
        {
            patternMatch = CSharpExamples.IsValidScore(92),
            scoreBand,
            iteratorEvenNumbers = CSharpExamples.TakeEvens(numbers),
            recordOriginal = original,
            recordCopy = published,
            extensionMethod = "async/await".ToInterviewLabel(),
            tuple = new { quotient, remainder },
            collectionExpression = numbers
        });
    }

    [HttpGet("linq")]
    public IActionResult Linq()
    {
        // LINQ Q224, Q226, Q238, Q251: deferred operators, grouping, Any, and readable ordering/projection.
        var scores = new[] { 91, 72, 88, 72, 64, 100 };
        var report = scores.Where(score => score >= 80).OrderByDescending(score => score)
            .Select((score, index) => new { rank = index + 1, score }).ToArray(); // Deferred until ToArray.
        var frequency = scores.GroupBy(score => score).ToDictionary(group => group.Key, group => group.Count());
        return Ok(new { topScores = report, scoreFrequency = frequency, anyPerfect = scores.Any(score => score == 100) });
    }

    [HttpGet("collections")]
    public IActionResult Collections()
    {
        // Collections Q263-Q265, Q268, Q278: dictionary/hash-set lookup, queue/stack behavior, and comparer choice.
        var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "C#", "LINQ", "C#" };
        var lookup = new Dictionary<string, int> { ["array"] = 3, ["list"] = 4 };
        var queue = new Queue<string>(["queued-1", "queued-2"]);
        var stack = new Stack<string>(["bottom", "top"]);
        return Ok(new { hashSet = unique, dictionary = lookup, queueNext = queue.Peek(), stackNext = stack.Peek() });
    }

    [HttpGet("async-stream")]
    public async IAsyncEnumerable<int> AsyncStream([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        // Async/Await Q297, Q299, Q304: stream values asynchronously and stop when the caller cancels.
        for (var i = 1; i <= 5; i++)
        {
            await Task.Delay(150, ct);
            yield return i;
        }
    }

    [HttpGet("cache")]
    public object CacheDemo()
    {
        // C# & Collections Q69: IMemoryCache stores a value with a bounded absolute expiration.
        var value = cache.GetOrCreate("lab:cached-at", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            return DateTimeOffset.UtcNow;
        });
        return new { cachedAtUtc = value, rateLimitedEndpointPassedRequests = counters.Current };
    }

    [HttpGet("throw")]
    public IActionResult Throw() => throw new InvalidOperationException("Intentional exception to demonstrate global problem response.");
}
