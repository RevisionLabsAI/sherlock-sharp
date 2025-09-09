using System.Collections.Generic;

namespace SherlockSharp.Models;

public sealed class UsernameCheckResult
{
    public string ServiceName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public bool Found { get; init; }
    public string? ProfileUrl { get; init; }
    public int? StatusCode { get; init; }
    public string? Note { get; init; }
}

public sealed class UsernameCheckSummary
{
    public string Username { get; init; } = string.Empty;
    public IReadOnlyList<UsernameCheckResult> Results { get; init; } = new List<UsernameCheckResult>();
}
