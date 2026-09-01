using System.ComponentModel.DataAnnotations;

namespace Rcm.Persistence.Files.Navigation;

public class DataStorageOptions
{
    [Required(AllowEmptyStrings = false)]
    public required string Path { get; set; }
}
