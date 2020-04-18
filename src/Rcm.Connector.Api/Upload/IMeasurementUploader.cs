using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rcm.Common;

namespace Rcm.Connector.Api.Upload
{
    public interface IMeasurementUploader
    {
        Task UploadAsync(IReadOnlyCollection<MeasurementEntry> measurements, CancellationToken token);
    }
}