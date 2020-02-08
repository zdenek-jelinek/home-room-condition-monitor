using Rcm.Common.Http;

namespace Rcm.Web.Configuration.Common
{
    public class HttpClientFactoryAdapter : IHttpClientFactory
    {
        private readonly System.Net.Http.IHttpClientFactory _wrappedFactory;

        public HttpClientFactoryAdapter(System.Net.Http.IHttpClientFactory wrappedFactory)
        {
            _wrappedFactory = wrappedFactory;
        }

        public IHttpClient Create(string name)
        {
            return new HttpClientAdapter(_wrappedFactory.CreateClient(name));
        }
    }
}
