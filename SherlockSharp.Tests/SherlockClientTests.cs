using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using SherlockSharp;
using SherlockSharp.Models;
using SherlockSharp.Tests.Helpers;
using Xunit;

public class SherlockClientTests
{
    private static IReadOnlyDictionary<string, ServiceDefinition> MakeServices()
    {
        return new Dictionary<string, ServiceDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["alpha"] = new ServiceDefinition { Url = "https://example.com/alpha/{}", ErrorType = "status_code" },
            ["beta"] = new ServiceDefinition { Url = "https://example.com/beta/{}", ErrorType = "message", ErrorMsg = new[]{"Not found","no user"} },
            ["gamma"] = new ServiceDefinition { Url = "https://example.com/gamma/{}", ErrorType = "status_code" }
        };
    }


    [Fact]
    public async Task CheckAsync_Throws_On_Empty_Username()
    {
        var services = MakeServices();
        using var client = new SherlockClient(null, null, new FakeHttpMessageHandler(_ => new FakeHttpMessageHandler.Response()), services);
        await Assert.ThrowsAsync<ArgumentException>(() => client.CheckAsync(" "));
    }

    [Fact]
    public async Task StatusCode_ErrorType_Uses_404_As_NotFound()
    {
        var services = MakeServices();
        var handler = new FakeHttpMessageHandler(req =>
        {
            var url = req.RequestUri!.ToString();
            return new FakeHttpMessageHandler.Response
            {
                StatusCode = url.Contains("alpha/") ? HttpStatusCode.OK : HttpStatusCode.NotFound
            };
        });
        using var client = new SherlockClient(null, TimeSpan.FromSeconds(10), handler, services);
        var results = await client.CheckAsync("john");
        results.Should().HaveCount(3); // NSFW no longer excluded
        results.Single(r=>r.ServiceName=="alpha").Found.Should().BeTrue();
        results.Single(r=>r.ServiceName=="gamma").Found.Should().BeFalse();
    }

    [Fact]
    public async Task Message_ErrorType_Uses_Body_To_Detect_NotFound()
    {
        var services = MakeServices();
        var handler = new FakeHttpMessageHandler(req =>
        {
            var url = req.RequestUri!.ToString();
            if (url.Contains("beta/"))
                return new FakeHttpMessageHandler.Response { StatusCode = HttpStatusCode.OK, Content = "User Not Found" };
            return new FakeHttpMessageHandler.Response { StatusCode = HttpStatusCode.OK };
        });
        using var client = new SherlockClient(new[]{"beta"}, TimeSpan.FromSeconds(10), handler, services);
        var results = await client.CheckAsync("jane");
        results.Should().HaveCount(1);
        results[0].ServiceName.Should().Be("beta");
        results[0].Found.Should().BeFalse();
    }

    [Fact]
    public async Task Explicit_Service_Selection_Is_Respected()
    {
        var services = MakeServices();
        var handler = new FakeHttpMessageHandler(_ => new FakeHttpMessageHandler.Response{ StatusCode = HttpStatusCode.OK});
        using var client = new SherlockClient(new[]{"gamma"}, TimeSpan.FromSeconds(10), handler, services);
        var results = await client.CheckAsync("foo");
        results.Select(r=>r.ServiceName).Should().BeEquivalentTo(new[]{"gamma"});
    }

    [Fact]
    public async Task CheckManyAsync_Aggregates_Per_Username()
    {
        var services = MakeServices();
        var handler = new FakeHttpMessageHandler(_ => new FakeHttpMessageHandler.Response{ StatusCode = HttpStatusCode.OK});
        using var client = new SherlockClient(new[]{"alpha"}, TimeSpan.FromSeconds(10), handler, services);
        var dict = await client.CheckManyAsync(new[]{"a","b"});
        dict.Keys.Should().BeEquivalentTo(new[]{"a","b"});
        dict["a"].Should().HaveCount(1);
        dict["b"].Should().HaveCount(1);
    }

    [Fact]
    public async Task Timeout_Yields_Result_With_Note()
    {
        var services = MakeServices();
        var handler = new FakeHttpMessageHandler(_ => new FakeHttpMessageHandler.Response{ StatusCode = HttpStatusCode.OK, Delay = TimeSpan.FromMilliseconds(200)});
        using var client = new SherlockClient(new[]{"alpha"}, TimeSpan.FromMilliseconds(50), handler, services);
        var res = await client.CheckAsync("slow");
        res.Should().ContainSingle();
        res[0].Note.Should().Be("timeout");
        res[0].Found.Should().BeFalse();
    }

    [Fact]
    public async Task Exception_Is_Captured_In_Note()
    {
        var services = MakeServices();
        var badHandler = new ThrowingHandler();
        using var client = new SherlockClient(new[]{"alpha"}, TimeSpan.FromSeconds(1), badHandler, services);
        var res = await client.CheckAsync("boom");
        res[0].Note.Should().Be(nameof(InvalidOperationException));
        res[0].Found.Should().BeFalse();
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("boom");
    }
}
