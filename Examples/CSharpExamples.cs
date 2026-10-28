namespace InterviewLab.Examples;

public sealed record InterviewNote(string Topic, string Takeaway)
{
    // C# Q14: init-only property can be set during initialization or a record with-expression.
    public string Status { get; init; } = "sample";
}

public static class CSharpExamples
{
    // C# Q9: extension method adds a helper to string without changing string itself.
    public static string ToInterviewLabel(this string value) => $"Topic: {value.Trim()}";

    // C# Q18; LINQ Q224: yield creates a lazily evaluated iterator.
    public static IEnumerable<int> TakeEvens(IEnumerable<int> values)
    {
        foreach (var value in values)
            if (value % 2 == 0) yield return value;
    }

    // C# Q11, Q49: declaration and relational patterns validate type and range together.
    public static bool IsValidScore(object? value) => value is int score and >= 0 and <= 100;
    public static int CheckedAdd(int left, int right) { checked { return left + right; } }
    public static (int quotient, int remainder) Divide(int dividend, int divisor) =>
        (dividend / divisor, dividend % divisor);
}
