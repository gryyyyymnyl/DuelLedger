using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DuelLedger.UI.Services;
using Xunit;

public class SvgIconCacheTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            Calls++;
            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<svg></svg>")
            };
            return Task.FromResult(resp);
        }
    }

    [Fact]
    public async Task FetchesOnceAndCachesPath()
    {
        var handler = new StubHandler();
        var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var cache = new SvgIconCache(tmp, new HttpClient(handler));

        string? readyPath = null;
        var tcs = new TaskCompletionSource<string?>();
        cache.IconReady += (k, p) => { readyPath = p; tcs.TrySetResult(p); };

        var path = cache.Get("Test", "http://example/icon.svg");
        Assert.Null(path);

        await tcs.Task; // wait for download
        Assert.Equal(1, handler.Calls);
        Assert.NotNull(readyPath);
        Assert.True(File.Exists(readyPath!));

        path = cache.Get("Test", "http://example/icon.svg");
        Assert.Equal(readyPath, path);
        Assert.Equal(1, handler.Calls); // still one fetch
    }
}

