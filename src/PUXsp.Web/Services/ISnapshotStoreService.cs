using PUXsp.Web.Models;

namespace PUXsp.Web.Services;

public interface ISnapshotStoreService
{
    Task<SnapshotStore> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(SnapshotStore snapshotStore, CancellationToken cancellationToken);
}
