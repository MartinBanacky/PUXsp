namespace PUXsp.Web.Models;

public sealed class TrackedFileSnapshot
{
    public string RelativePath { get; init; } = string.Empty;

    public string ContentHash { get; init; } = string.Empty;

    public int Version { get; init; }
}
