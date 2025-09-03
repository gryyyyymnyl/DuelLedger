using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DuelLedger.Infra.Config;
using Xunit;

public class AppConfigProviderTests
{
    [Fact]
    public async Task RemoteFailureFallsBackToLocal()
    {
        var tmp = Path.GetTempFileName();
        await File.WriteAllTextAsync(tmp, "{\"Assets\":{\"TemplateRoot\":\"Local\"}}\n");
        var provider = await AppConfigProvider.LoadAsync(tmp, "http://example.com/remote.json", new FailingHandler());
        Assert.Equal("Local", provider.Value.Assets.TemplateRoot);
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromException<HttpResponseMessage>(new HttpRequestException("fail"));
    }
}
