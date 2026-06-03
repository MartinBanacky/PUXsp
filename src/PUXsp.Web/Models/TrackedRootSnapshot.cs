namespace PUXsp.Web.Models;

public sealed class TrackedRootSnapshot
{
    public string RootPath { get; init; } = string.Empty;

    public DateTimeOffset LastAnalyzedUtc { get; init; }

    public List<string> Directories { get; init; } = [];

    public List<TrackedFileSnapshot> Files { get; init; } = [];
}
