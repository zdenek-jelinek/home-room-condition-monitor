using Rcm.Common.Http;

namespace Rcm.Common.TestDoubles.Http
{
    public class StubHttpClientFactory : IHttpClientFactory
    {
        public IHttpClient Client { get; set; } = new StubHttpClient();

        public IHttpClient Create(string name) => Client;
    }
}
