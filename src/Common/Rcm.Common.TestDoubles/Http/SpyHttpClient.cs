using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Rcm.Common.Http;

namespace Rcm.Common.TestDoubles.Http;

public class SpyHttpClient : IHttpClient
{
    public HttpRequestMessage? SentRequest { get; private set; }
    public HttpResponseMessage Response { get; set; } = new HttpResponseMessage();

    public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        SentRequest = request;

        return Task.FromResult(Response);
    }
}