using PUXsp.Web.Models;
using System.Text.Json;

namespace PUXsp.Web.Services;

public sealed class JsonSnapshotStoreService : ISnapshotStoreService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _snapshotFilePath;

    public JsonSnapshotStoreService(IWebHostEnvironment environment)
    {
        var dataDirectoryPath = Path.Combine(environment.ContentRootPath, "data");
        _snapshotFilePath = Path.Combine(dataDirectoryPath, "snapshots.json");
    }

    public async Task<SnapshotStore> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_snapshotFilePath))
        {
            return new SnapshotStore();
        }

        await using var stream = new FileStream(
            _snapshotFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        var snapshotStore = await JsonSerializer.DeserializeAsync<SnapshotStore>(stream, SerializerOptions, cancellationToken);
        return snapshotStore ?? new SnapshotStore();
    }

    public async Task SaveAsync(SnapshotStore snapshotStore, CancellationToken cancellationToken)
    {
        var directoryPath = Path.GetDirectoryName(_snapshotFilePath)!;
        Directory.CreateDirectory(directoryPath);

        var temporaryFilePath = $"{_snapshotFilePath}.tmp";
        var json = JsonSerializer.Serialize(snapshotStore, SerializerOptions);

        try
        {
            await File.WriteAllTextAsync(temporaryFilePath, json, cancellationToken);

            if (File.Exists(_snapshotFilePath))
            {
                File.Replace(temporaryFilePath, _snapshotFilePath, destinationBackupFileName: null);
            }
            else
            {
                File.Move(temporaryFilePath, _snapshotFilePath);
            }
        }
        finally
        {
            if (File.Exists(temporaryFilePath))
            {
                File.Delete(temporaryFilePath);
            }
        }
    }
}
