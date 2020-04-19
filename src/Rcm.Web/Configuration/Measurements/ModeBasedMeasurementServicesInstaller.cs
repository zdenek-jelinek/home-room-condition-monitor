﻿using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rcm.Bme280;
using Rcm.I2c;
using Rcm.Measurement.Api;
using Rcm.Measurement.Stubs;

namespace Rcm.Web.Configuration.Measurements
{
    public class ModeBasedMeasurementServicesInstaller : IConfigurableInstaller
    {
        public void Install(IServiceCollection services, IConfiguration configuration)
        {
            var mode = new MeasurementAccessModeReader().Get(configuration);
            switch (mode)
            {
                case MeasurementAccessMode.I2c:
                    InstallI2cServices(services, configuration);
                    break;

                case MeasurementAccessMode.Stub:
                    InstallStubServices(services);
                    break;

                default:
                    throw new NotSupportedException($"Measurement access mode {mode} is not supported");

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

        private void InstallI2cServices(IServiceCollection services, IConfiguration measurementI2cAccessConfiguration)
        {
            services
                .AddOptions<I2cAccessConfiguration>()
                .Bind(measurementI2cAccessConfiguration)
                .ValidateDataAnnotations();

            services
                .AddSingleton<IMeasurementProviderFactory, Bme280DeviceFactory>()
                .AddTransient<IBme280Configuration>(GetOptionValue<I2cAccessConfiguration>)
                .AddTransient<I2cBusFactory>();
        }

        private static T GetOptionValue<T>(IServiceProvider serviceProvider) where T : class, new()
        {
            return serviceProvider
                .GetRequiredService<IOptions<T>>()
                .Value;
        }
    }
}
