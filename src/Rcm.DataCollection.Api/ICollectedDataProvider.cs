using System;
using System.Collections.Generic;
using Rcm.Common;

namespace Rcm.DataCollection.Api
{
    public interface ICollectedDataProvider
    {
        IEnumerable<MeasurementEntry> GetCollectedDataAsync(DateTimeOffset start, DateTimeOffset end);
    }
}
