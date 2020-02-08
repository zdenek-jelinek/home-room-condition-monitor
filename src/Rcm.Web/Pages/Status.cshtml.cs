using System;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rcm.Connector.Api.Status;

namespace Rcm.Web.Pages
{
    public class StatusModel : PageModel
    {
        private readonly IConnectionStatusAccessor _connectionStatus;

        public bool IsConfigured { get; private set; }
        public DateTimeOffset? LastUploadedMeasurementTime { get; private set; }

        public StatusModel(IConnectionStatusAccessor connectionStatus)
        {
            _connectionStatus = connectionStatus;
        }

        public void OnGet()
        {
            IsConfigured = _connectionStatus.IsConfigured;
            LastUploadedMeasurementTime = _connectionStatus.LastUploadedMeasurementTime;
        }
    }
}