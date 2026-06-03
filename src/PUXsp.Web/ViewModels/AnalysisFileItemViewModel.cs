namespace PUXsp.Web.ViewModels;

public sealed class AnalysisFileItemViewModel
{
    public string RelativePath { get; init; } = string.Empty;

    public int? PreviousVersion { get; init; }

    public int CurrentVersion { get; init; }
}
