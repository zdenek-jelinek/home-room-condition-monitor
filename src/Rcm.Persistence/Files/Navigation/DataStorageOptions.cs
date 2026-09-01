using System.ComponentModel.DataAnnotations;

namespace Rcm.Web.Configuration.Common;

public class DataStorageOptions
{
    [Required(AllowEmptyStrings = false)]
    public required string Path { get; set; }
}
