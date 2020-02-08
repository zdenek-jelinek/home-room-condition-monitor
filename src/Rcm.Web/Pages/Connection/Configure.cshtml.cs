using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Rcm.Connector.Api.Configuration;

namespace Rcm.Web.Pages.Connection
{
    public class ConfigureModel : PageModel
    {
        private readonly IConnectionConfigurationGateway _configurationGateway;

        public bool IsConfigured { get; private set; }

        [Url]
        [Required]
        [Display(Name = "Back-end URI")]
        [BindProperty]
        public string? BackendUri { get; set; }

        [Required]
        [Display(Name = "Device identifier")]
        [BindProperty]
        public string? DeviceIdentifier { get; set; }

        [Required]
        [Display(Name = "Device key")]
        [BindProperty]
        public string? DeviceKey { get; set; }

        public ConfigureModel(IConnectionConfigurationGateway configurationGateway)
        {
            _configurationGateway = configurationGateway;
        }

        public void OnGet()
        {
            var configuration = _configurationGateway.ReadConfiguration();

            IsConfigured = configuration != null;

            BackendUri = configuration?.BaseUri;
            DeviceIdentifier = configuration?.DeviceIdentifier;
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _configurationGateway.WriteConfiguration(
                new ConnectionConfiguration(
                    baseUri: BackendUri!,
                    deviceIdentifier: DeviceIdentifier!,
                    deviceKey: DeviceKey!));

            return RedirectToPage("/Status");
        }

        public IActionResult OnDelete()
        {
            _configurationGateway.EraseConfiguration();
            return new NoContentResult();
        }
    }
}
