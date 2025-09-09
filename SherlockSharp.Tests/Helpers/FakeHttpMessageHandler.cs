using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SherlockSharp.Tests.Helpers;

public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    public class Response
    {
        public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;
        public string Content { get; init; } = string.Empty;
        public Dictionary<string,string> Headers { get; init; } = new();
        public TimeSpan? Delay { get; init; }
    }

    private readonly Func<HttpRequestMessage, Response> _resolver;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, Response> resolver)
    {
        _resolver = resolver;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var r = _resolver(request);
        if (r.Delay is { } d)
        {
            await Task.Delay(d, cancellationToken);
        }
        var resp = new HttpResponseMessage(r.StatusCode)
        {
            Content = new StringContent(r.Content)
        };
        foreach (var kv in r.Headers)
        {
            resp.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        }
        return resp;
    }
}