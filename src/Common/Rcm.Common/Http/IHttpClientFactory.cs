namespace Rcm.Common.Http
{
    public interface IHttpClientFactory
    {
        IHttpClient Create(string name);
    }
}
