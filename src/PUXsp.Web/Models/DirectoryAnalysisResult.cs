namespace PUXsp.Web.Models;

public sealed class DirectoryAnalysisResult
{
    public string RootPath { get; init; } = string.Empty;

    public DateTimeOffset StartedAtUtc { get; init; }

    public DateTimeOffset? CompletedAtUtc { get; init; }

    public TimeSpan Duration { get; init; }

    public int RetryCountUsed { get; init; }

    public AnalysisStatus Status { get; init; }

    public int RegisteredFileCount { get; init; }

    public int RegisteredDirectoryCount { get; init; }

    public string? ErrorMessage { get; init; }

    public List<FileVersionChange> NewFiles { get; init; } = [];

    public List<FileVersionChange> ModifiedFiles { get; init; } = [];

    public List<string> DeletedFiles { get; init; } = [];

    public List<string> NewDirectories { get; init; } = [];

    public List<string> DeletedDirectories { get; init; } = [];

    public List<AnalysisWarning> Warnings { get; init; } = [];
}
