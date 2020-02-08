using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Rcm.Common.Http
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token);
    }
}
