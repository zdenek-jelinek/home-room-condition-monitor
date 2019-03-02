using Microsoft.AspNetCore.Mvc.RazorPages;
using Rcm.Web.Presentation.Status;

namespace Rcm.Web.Pages
{
    public class StatusModel : PageModel
    {
        private readonly IStatusPagePresenter _presenter;

        public ConnectivityStatusModel? Connectivity { get; private set; }

        public StatusModel(IStatusPagePresenter presenter)
        {
            _presenter = presenter;
        }

        public void OnGet()
        {
            Connectivity = _presenter.GetStatus();
        }
    }
}