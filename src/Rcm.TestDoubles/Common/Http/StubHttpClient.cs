using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Rcm.Common.Http;

namespace Rcm.TestDoubles.Common.Http
{
    public class StubHttpClient : IHttpClient
    {
        public HttpResponseMessage Response { get; set; } = new HttpResponseMessage();

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            return Task.FromResult(Response);
        }
    }
}