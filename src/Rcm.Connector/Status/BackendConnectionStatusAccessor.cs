using System;
using Rcm.Connector.Api.Configuration;
using Rcm.Connector.Api.Status;
using Rcm.Connector.Configuration;

namespace Rcm.Connector.Status
{
    public class BackendConnectionStatusAccessor : IConnectionStatusAccessor
    {
        private readonly IConnectionConfigurationReader _connectionConfigurationReader;
        private readonly ILatestUploadedMeasurementReader _latestUploadedMeasurementReader;

        public bool IsConfigured => ReadConfiguration() != null;

        public DateTimeOffset? LastUploadedMeasurementTime =>
            _latestUploadedMeasurementReader.GetLatestUploadedMeasurementTime();

        public BackendConnectionStatusAccessor(
            IConnectionConfigurationReader connectionConfigurationReader,
            ILatestUploadedMeasurementReader latestUploadedMeasurementReader)
        {
            _connectionConfigurationReader = connectionConfigurationReader;
            _latestUploadedMeasurementReader = latestUploadedMeasurementReader;
        }

        private ConnectionConfiguration? ReadConfiguration() => _connectionConfigurationReader.ReadConfiguration();
    }
}
