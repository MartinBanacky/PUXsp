namespace PUXsp.Web.Models;

public sealed class FileVersionChange
{
    public string RelativePath { get; init; } = string.Empty;

    public int? PreviousVersion { get; init; }

    public int CurrentVersion { get; init; }
}
