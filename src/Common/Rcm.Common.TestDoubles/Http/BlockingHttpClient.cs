using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Rcm.Common.Http;

namespace Rcm.Common.TestDoubles.Http
{
    public class BlockingHttpClient : IHttpClient
    {
        private readonly SemaphoreSlim _blockedSemaphore = new SemaphoreSlim(0);
        private readonly SemaphoreSlim _blockingSemaphore = new SemaphoreSlim(0);

        public Task Blocked => _blockedSemaphore.WaitAsync();

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            _blockedSemaphore.Release();

            await _blockingSemaphore.WaitAsync(token);
            return new HttpResponseMessage();
        }
    }
}
