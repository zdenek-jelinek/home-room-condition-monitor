using Microsoft.Extensions.DependencyInjection;
using Rcm.Device.Connector.Api.Configuration;
using Rcm.Device.Connector.Api.Status;
using Rcm.Device.Connector.Api.Upload;
using Rcm.Device.Connector.Configuration;
using Rcm.Device.Connector.Status;
using Rcm.Device.Connector.Upload;

namespace Rcm.Device.Web.Configuration.Connectivity;

public class ConnectivityInstaller : IInstaller
{
    public void Install(IServiceCollection services)
    {
        services.AddHttpClient(MeasurementClient.HttpClientName);

        services
            .AddTransient<IConnectionStatusAccessor, BackendConnectionStatusAccessor>()
            .AddTransient<IMeasurementUploader, MeasurementUploader>()
            .AddTransient<IFileBackendStorageLocation, FileBackendStorageLocation>()
            .AddTransient<IConnectionConfigurationGateway, FileConnectionConfigurationGateway>()
            .AddTransient<IConnectionConfigurationReader, FileConnectionConfigurationGateway>()
            .AddTransient<IConnectionConfigurationWriter, FileConnectionConfigurationGateway>()
            .AddTransient<ILatestUploadedMeasurementReader, FileLatestUploadedMeasurementGateway>()
            .AddTransient<ILatestUploadedMeasurementWriter, FileLatestUploadedMeasurementGateway>();
    }
}