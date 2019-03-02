using System;

namespace Rcm.Web.Presentation.Status
{
    public class StubStatusPagePresenter : IStatusPagePresenter
    {
        public ConnectivityStatusModel GetStatus()
        {
            return new ConnectivityStatusModel(ConnectionStatusModel.Active, new TimeSpan(5, 22, 14, 15, 845));
        }
    }
}
