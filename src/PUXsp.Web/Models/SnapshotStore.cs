namespace PUXsp.Web.Models;

public sealed class SnapshotStore
{
    public List<TrackedRootSnapshot> Roots { get; init; } = [];
}
