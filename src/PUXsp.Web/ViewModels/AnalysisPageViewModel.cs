using System.ComponentModel.DataAnnotations;

namespace PUXsp.Web.ViewModels;

public sealed class AnalysisPageViewModel
{
    [Required(ErrorMessage = "Directory path is required.")]
    public string DirectoryPath { get; set; } = string.Empty;

    [Range(0, 5, ErrorMessage = "Retry count must be between 0 and 5.")]
    public int RetryCount { get; set; } = 2;

    public AnalysisResultViewModel? Result { get; set; }
}
