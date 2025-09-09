SherlockSharp

A small C# class library that checks username availability across many services using the Sherlock project's site list.

Quick start

- Install the library into your project (build from source or package it as a NuGet).
- Use either the static helper or an instance with configured services.

Example

// Static one-off check
var results = await SherlockSharp.SherlockClient.CheckAsync("user123", new[]{"GitHub","Twitter"});
foreach (var r in results)
{
    Console.WriteLine($"{r.ServiceName}: {(r.Found ? "FOUND" : "NOT FOUND")} -> {r.ProfileUrl}");
}

// Reusable client with all non-NSFW services and a 15s timeout
using var client = new SherlockSharp.SherlockClient(timeout: TimeSpan.FromSeconds(15));
var many = await client.CheckManyAsync(new[]{"alice","bob"});

API surface

- class SherlockClient
  - ctor(IEnumerable<string>? services = null, TimeSpan? timeout = null, bool includeNsfw = false, HttpMessageHandler? handler = null)
  - static Task<IReadOnlyList<UsernameCheckResult>> CheckAsync(string username, IEnumerable<string>? services = null, TimeSpan? timeout = null, bool includeNsfw = false, CancellationToken ct = default)
  - Task<IReadOnlyList<UsernameCheckResult>> CheckAsync(string username, CancellationToken ct = default)
  - Task<IReadOnlyDictionary<string, IReadOnlyList<UsernameCheckResult>>> CheckManyAsync(IEnumerable<string> usernames, CancellationToken ct = default)

Notes

- This is a minimal port focusing on GET-based probing and two common error types in the Sherlock data.json:
  - errorType: "status_code" -> 404 means not found; 2xx/3xx means found
  - errorType: "message" + errorMsg -> if page body contains any errorMsg, treat as not found; otherwise found on 2xx/3xx
- More advanced patterns (POST probes, response_url checks, complex headers) can be added iteratively.
- Default behavior excludes NSFW sites unless includeNsfw = true.
