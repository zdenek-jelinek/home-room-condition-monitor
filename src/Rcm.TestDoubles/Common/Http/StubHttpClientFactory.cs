using Rcm.Common.Http;

namespace Rcm.TestDoubles.Common.Http
{
    public class StubHttpClientFactory : IHttpClientFactory
    {
        public IHttpClient Client { get; set; } = new StubHttpClient();

        public IHttpClient Create(string name) => Client;
    }
}
