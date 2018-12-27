using System;
using Microsoft.Extensions.DependencyInjection;
using Rcm.Bme280;
using Rcm.I2c;
using Rcm.Measurement.Api;
using Rcm.Measurement.Stubs;

namespace Rcm.Web.Configuration.Measurements
{
    public class ModeBasedMeasurementServicesInstaller : IInstaller
    {
        public void Install(IServiceCollection services)
        {
            var mode = new ApplicationHardwareModeReader().Get();
            switch (mode)
            {
                case ApplicationHardwareMode.I2c:
                    InstallI2cServices(services);
                    break;

                case ApplicationHardwareMode.Stub:
                    InstallStubServices(services);
                    break;

                default:
                    throw new NotSupportedException($"Application hardware mode {mode} is not supported");

            }

            InstallCommonServices(services);
        }

        private void InstallCommonServices(IServiceCollection services)
        {
            services.AddTransient(s => s.GetRequiredService<IMeasurementProviderFactory>().Create());
        }

        private void InstallStubServices(IServiceCollection services)
        {
            services.AddSingleton<IMeasurementProviderFactory, StubMeasurementProviderFactory>();
        }

        private void InstallI2cServices(IServiceCollection services)
        {
            services
                .AddSingleton<IMeasurementProviderFactory, Bme280DeviceFactory>()
                .AddTransient<IBme280Configuration, EnvironmentBme280Configuration>()
                .AddTransient<I2cBusFactory>();
        }
    }
}
