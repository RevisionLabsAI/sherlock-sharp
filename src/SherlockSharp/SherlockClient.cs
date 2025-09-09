using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SherlockSharp.Internal;
using SherlockSharp.Models;

namespace SherlockSharp;

/// <summary>
/// SherlockSharp client to check username availability across selected services.
/// </summary>
public sealed class SherlockClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly IReadOnlyDictionary<string, ServiceDefinition> _services;
    private readonly HashSet<string> _selected;
    private readonly bool _includeNsfw;

    /// <summary>
    /// Create a client with optional set of services to check. If null or empty, all non-NSFW services are used.
    /// </summary>
    public SherlockClient(IEnumerable<string>? services = null, TimeSpan? timeout = null, bool includeNsfw = false, HttpMessageHandler? handler = null)
    {
        _includeNsfw = includeNsfw;
        _http = handler != null ? new HttpClient(handler) : new HttpClient();
        _http.Timeout = timeout ?? TimeSpan.FromSeconds(30);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("SherlockSharp/0.1 (+https://github.com/your-org/SherlockSharp)");
        _services = DataLoader.LoadServices();
        _selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (services != null)
        {
            foreach (var s in services) _selected.Add(s);
        }
    }

    // Internal testing-only constructor allowing services injection without touching embedded resources.
    internal SherlockClient(IEnumerable<string>? services, TimeSpan? timeout, bool includeNsfw, HttpMessageHandler? handler, IReadOnlyDictionary<string, ServiceDefinition> servicesOverride)
    {
        _includeNsfw = includeNsfw;
        _http = handler != null ? new HttpClient(handler) : new HttpClient();
        _http.Timeout = timeout ?? TimeSpan.FromSeconds(30);
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("SherlockSharp/0.1 (+https://github.com/your-org/SherlockSharp)");
        _services = servicesOverride ?? throw new ArgumentNullException(nameof(servicesOverride));
        _selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (services != null)
        {
            foreach (var s in services) _selected.Add(s);
        }
    }

    /// <summary>
    /// Static convenience call without managing a client instance.
    /// </summary>
    public static Task<IReadOnlyList<UsernameCheckResult>> CheckAsync(string username, IEnumerable<string>? services = null, TimeSpan? timeout = null, bool includeNsfw = false, CancellationToken ct = default)
    {
        using var client = new SherlockClient(services, timeout, includeNsfw);
        return client.CheckAsync(username, ct);
    }

    /// <summary>
    /// Check a single username across configured services.
    /// </summary>
    public async Task<IReadOnlyList<UsernameCheckResult>> CheckAsync(string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required", nameof(username));

        var targets = SelectServices();
        var tasks = targets.Select(kvp => ProbeServiceAsync(kvp.Key, kvp.Value, username, ct));
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results;
    }

    /// <summary>
    /// Check multiple usernames across configured services.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<UsernameCheckResult>>> CheckManyAsync(IEnumerable<string> usernames, CancellationToken ct = default)
    {
        var dict = new Dictionary<string, IReadOnlyList<UsernameCheckResult>>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in usernames)
        {
            dict[u] = await CheckAsync(u, ct).ConfigureAwait(false);
        }
        return dict;
    }

    private Dictionary<string, ServiceDefinition> SelectServices()
    {
        var all = _services;
        var list = new Dictionary<string, ServiceDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in all)
        {
            if (!_includeNsfw && kv.Value.IsNsfw == true) continue;
            if (_selected.Count > 0 && !_selected.Contains(kv.Key)) continue;
            list[kv.Key] = kv.Value;
        }
        return list;
    }

    private async Task<UsernameCheckResult> ProbeServiceAsync(string name, ServiceDefinition sd, string username, CancellationToken ct)
    {
        var profileUrl = (sd.Url ?? string.Empty).Replace("{}", username);
        var url = profileUrl;
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            var status = (int)resp.StatusCode;
            var body = string.Empty;
            if (sd.ErrorType?.Equals("message", StringComparison.OrdinalIgnoreCase) == true)
            {
                body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            }

            var found = EvaluateFound(sd, status, body);
            return new UsernameCheckResult
            {
                ServiceName = name,
                Username = username,
                Found = found,
                ProfileUrl = profileUrl,
                StatusCode = status
            };
        }
        catch (TaskCanceledException)
        {
            return new UsernameCheckResult { ServiceName = name, Username = username, Found = false, ProfileUrl = profileUrl, Note = "timeout" };
        }
        catch (Exception ex)
        {
            return new UsernameCheckResult { ServiceName = name, Username = username, Found = false, ProfileUrl = profileUrl, Note = ex.GetType().Name };
        }
    }

    private static bool EvaluateFound(ServiceDefinition sd, int status, string body)
    {
        // Default heuristic: 200 => found, 404 => not found
        var errorType = sd.ErrorType?.ToLowerInvariant();
        if (errorType == "status_code")
        {
            // Many entries imply 404 means not found; treat 200/301/302 as found
            if (status == 404) return false;
            if (status >= 200 && status < 400) return true;
            return false;
        }
        if (errorType == "message")
        {
            var msgs = sd.ErrorMsg ?? Array.Empty<string>();
            // If body contains an error message, then NOT found; else assume found on 200
            if (!string.IsNullOrEmpty(body))
            {
                foreach (var m in msgs)
                {
                    if (!string.IsNullOrEmpty(m) && body.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0)
                        return false;
                }
            }
            return status >= 200 && status < 400;
        }
        // Fallback
        return status >= 200 && status < 400;
    }

    public void Dispose()
    {
        _http.Dispose();
    }
}
