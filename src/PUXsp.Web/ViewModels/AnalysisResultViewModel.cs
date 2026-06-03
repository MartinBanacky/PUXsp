namespace PUXsp.Web.ViewModels;

public sealed class AnalysisResultViewModel
{
    public string Status { get; init; } = string.Empty;

    public bool IsSuccess { get; init; }

    public bool HasWarnings { get; init; }

    public string AnalyzedPath { get; init; } = string.Empty;

    public DateTimeOffset StartedAtLocal { get; init; }

    public DateTimeOffset? CompletedAtLocal { get; init; }

    public TimeSpan Duration { get; init; }

    public int RetryCountUsed { get; init; }

    public string? ErrorMessage { get; init; }

    public int RegisteredFileCount { get; init; }

    public int RegisteredDirectoryCount { get; init; }

    public List<AnalysisFileItemViewModel> NewFiles { get; init; } = [];

    public List<AnalysisFileItemViewModel> ModifiedFiles { get; init; } = [];

    public List<string> DeletedFiles { get; init; } = [];

    public List<string> NewDirectories { get; init; } = [];

    public List<string> DeletedDirectories { get; init; } = [];

    public List<AnalysisWarningViewModel> Warnings { get; init; } = [];
}
