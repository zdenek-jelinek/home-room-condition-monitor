using Microsoft.Extensions.DependencyInjection;
using Rcm.Connector.Api.Configuration;
using Rcm.Connector.Api.Status;
using Rcm.Connector.Api.Upload;
using Rcm.Connector.Configuration;
using Rcm.Connector.Status;
using Rcm.Connector.Upload;

namespace Rcm.Web.Configuration.Connectivity
{
    public class ConnectivityInstaller : IInstaller
    {
        public void Install(IServiceCollection services)
        {
            services.AddHttpClient(MeasurementClient.HttpClientName);

            services
                .AddTransient<IConnectionStatusAccessor, BackendConnectionStatusAccessor>()
                .AddTransient<IMeasurementUploader, MeasurementUploader>()
                .AddTransient<IConnectionConfigurationGateway, FileConnectionConfigurationGateway>()
                .AddTransient<IConnectionConfigurationReader, FileConnectionConfigurationGateway>()
                .AddTransient<IConnectionConfigurationWriter, FileConnectionConfigurationGateway>()
                .AddTransient<ILatestUploadedMeasurementReader, FileLatestUploadedMeasurementGateway>()
                .AddTransient<ILatestUploadedMeasurementWriter, FileLatestUploadedMeasurementGateway>();
        }

    }
}
