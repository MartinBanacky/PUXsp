using PUXsp.Web.Models;
using PUXsp.Web.Utilities;
using System.Security.Cryptography;

namespace PUXsp.Web.Services;

public sealed class DirectoryAnalysisService : IDirectoryAnalysisService
{
    private readonly ILogger<DirectoryAnalysisService> _logger;
    private readonly ISnapshotStoreService _snapshotStoreService;

    public DirectoryAnalysisService(
        ISnapshotStoreService snapshotStoreService,
        ILogger<DirectoryAnalysisService> logger)
    {
        _snapshotStoreService = snapshotStoreService;
        _logger = logger;
    }

    public async Task<DirectoryAnalysisResult> AnalyzeAsync(string rootPath, int retryCount, CancellationToken cancellationToken)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;

        try
        {
            var snapshotStore = await _snapshotStoreService.LoadAsync(cancellationToken);
            var previousSnapshot = snapshotStore.Roots.FirstOrDefault(root =>
                string.Equals(root.RootPath, rootPath, StringComparison.OrdinalIgnoreCase));

            var capture = await CaptureSnapshotAsync(rootPath, retryCount, previousSnapshot, cancellationToken);
            var completedAtUtc = DateTimeOffset.UtcNow;
            var updatedSnapshot = new TrackedRootSnapshot
            {
                RootPath = rootPath,
                LastAnalyzedUtc = completedAtUtc,
                Directories = capture.Directories.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList(),
                Files = capture.Files.Values
                    .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };

            var result = BuildResult(
                previousSnapshot,
                updatedSnapshot,
                capture.Warnings,
                rootPath,
                startedAtUtc,
                completedAtUtc,
                retryCount);

            var updatedStore = new SnapshotStore
            {
                Roots = snapshotStore.Roots
                    .Where(root => !string.Equals(root.RootPath, rootPath, StringComparison.OrdinalIgnoreCase))
                    .Append(updatedSnapshot)
                    .OrderBy(root => root.RootPath, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };

            await _snapshotStoreService.SaveAsync(updatedStore, cancellationToken);

            return result;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Directory analysis failed for {RootPath}.", rootPath);

            var completedAtUtc = DateTimeOffset.UtcNow;
            return new DirectoryAnalysisResult
            {
                RootPath = rootPath,
                StartedAtUtc = startedAtUtc,
                CompletedAtUtc = completedAtUtc,
                Duration = completedAtUtc - startedAtUtc,
                RetryCountUsed = retryCount,
                Status = AnalysisStatus.Failed,
                ErrorMessage = exception.Message
            };
        }
    }

    private static DirectoryAnalysisResult BuildResult(
        TrackedRootSnapshot? previousSnapshot,
        TrackedRootSnapshot updatedSnapshot,
        IReadOnlyCollection<AnalysisWarning> warnings,
        string rootPath,
        DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc,
        int retryCount)
    {
        if (previousSnapshot is null)
        {
            return new DirectoryAnalysisResult
            {
                RootPath = rootPath,
                StartedAtUtc = startedAtUtc,
                CompletedAtUtc = completedAtUtc,
                Duration = completedAtUtc - startedAtUtc,
                RetryCountUsed = retryCount,
                Status = AnalysisStatus.BaselineCreated,
                RegisteredDirectoryCount = updatedSnapshot.Directories.Count,
                RegisteredFileCount = updatedSnapshot.Files.Count,
                Warnings = warnings.ToList()
            };
        }

        var previousFiles = previousSnapshot.Files.ToDictionary(
            file => file.RelativePath,
            file => file,
            StringComparer.OrdinalIgnoreCase);
        var currentFiles = updatedSnapshot.Files.ToDictionary(
            file => file.RelativePath,
            file => file,
            StringComparer.OrdinalIgnoreCase);

        var newFiles = currentFiles.Values
            .Where(file => !previousFiles.ContainsKey(file.RelativePath))
            .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Select(file => new FileVersionChange
            {
                RelativePath = file.RelativePath,
                CurrentVersion = file.Version
            })
            .ToList();

        var modifiedFiles = currentFiles.Values
            .Where(file =>
                previousFiles.TryGetValue(file.RelativePath, out var previousFile) &&
                !string.Equals(previousFile.ContentHash, file.ContentHash, StringComparison.Ordinal))
            .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Select(file => new FileVersionChange
            {
                RelativePath = file.RelativePath,
                PreviousVersion = previousFiles[file.RelativePath].Version,
                CurrentVersion = file.Version
            })
            .ToList();

        var deletedFiles = previousFiles.Keys
            .Where(relativePath => !currentFiles.ContainsKey(relativePath))
            .OrderBy(relativePath => relativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var currentDirectories = updatedSnapshot.Directories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var previousDirectories = previousSnapshot.Directories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newDirectories = updatedSnapshot.Directories
            .Where(relativePath => !previousDirectories.Contains(relativePath))
            .OrderBy(relativePath => relativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var deletedDirectories = previousSnapshot.Directories
            .Where(relativePath => !currentDirectories.Contains(relativePath))
            .OrderBy(relativePath => relativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new DirectoryAnalysisResult
        {
            RootPath = rootPath,
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = completedAtUtc,
            Duration = completedAtUtc - startedAtUtc,
            RetryCountUsed = retryCount,
            Status = warnings.Count > 0 ? AnalysisStatus.CompletedWithWarnings : AnalysisStatus.Completed,
            RegisteredDirectoryCount = updatedSnapshot.Directories.Count,
            RegisteredFileCount = updatedSnapshot.Files.Count,
            NewFiles = newFiles,
            ModifiedFiles = modifiedFiles,
            DeletedFiles = deletedFiles,
            NewDirectories = newDirectories,
            DeletedDirectories = deletedDirectories,
            Warnings = warnings.ToList()
        };
    }

    private async Task<SnapshotCapture> CaptureSnapshotAsync(
        string rootPath,
        int retryCount,
        TrackedRootSnapshot? previousSnapshot,
        CancellationToken cancellationToken)
    {
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files = new Dictionary<string, TrackedFileSnapshot>(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<AnalysisWarning>();
        var previousFiles = previousSnapshot?.Files.ToDictionary(
            file => file.RelativePath,
            file => file,
            StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, TrackedFileSnapshot>(StringComparer.OrdinalIgnoreCase);

        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(rootPath);

        while (pendingDirectories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentDirectory = pendingDirectories.Pop();
            var childDirectories = EnumerateEntries(currentDirectory, searchForDirectories: true);
            var childFiles = EnumerateEntries(currentDirectory, searchForDirectories: false);

            foreach (var childDirectory in childDirectories.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var relativeDirectoryPath = PathUtility.GetRelativePath(rootPath, childDirectory);
                if (!string.IsNullOrWhiteSpace(relativeDirectoryPath))
                {
                    directories.Add(relativeDirectoryPath);
                }

                pendingDirectories.Push(childDirectory);
            }

            foreach (var childFile in childFiles.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var relativeFilePath = PathUtility.GetRelativePath(rootPath, childFile);
                previousFiles.TryGetValue(relativeFilePath, out var previousFile);

                var fileCapture = await CaptureFileAsync(
                    childFile,
                    relativeFilePath,
                    retryCount,
                    previousFile,
                    cancellationToken);

                if (fileCapture.Warning is not null)
                {
                    warnings.Add(fileCapture.Warning);
                }

                if (fileCapture.Snapshot is not null)
                {
                    files[fileCapture.Snapshot.RelativePath] = fileCapture.Snapshot;
                }
            }
        }

        return new SnapshotCapture
        {
            Directories = directories,
            Files = files,
            Warnings = warnings
        };
    }

    private static IEnumerable<string> EnumerateEntries(string directoryPath, bool searchForDirectories)
    {
        try
        {
            return searchForDirectories
                ? Directory.EnumerateDirectories(directoryPath)
                : Directory.EnumerateFiles(directoryPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or PathTooLongException)
        {
            throw new InvalidOperationException(
                $"Unable to access directory '{directoryPath}' during analysis.",
                exception);
        }
    }

    private async Task<FileCaptureResult> CaptureFileAsync(
        string filePath,
        string relativePath,
        int retryCount,
        TrackedFileSnapshot? previousFile,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt <= retryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var fileInfo = new FileInfo(filePath);
                fileInfo.Refresh();

                if (!fileInfo.Exists)
                {
                    return BuildUnstableFileResult(relativePath, previousFile, "File was not available when the analyzer attempted to read it.");
                }

                var lengthBefore = fileInfo.Length;
                var lastWriteTimeBeforeUtc = fileInfo.LastWriteTimeUtc;

                await using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 81920,
                    options: FileOptions.Asynchronous | FileOptions.SequentialScan);

                var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
                var contentHash = Convert.ToHexString(hashBytes);

                fileInfo.Refresh();
                if (!fileInfo.Exists ||
                    fileInfo.Length != lengthBefore ||
                    fileInfo.LastWriteTimeUtc != lastWriteTimeBeforeUtc)
                {
                    if (attempt < retryCount)
                    {
                        continue;
                    }

                    return BuildUnstableFileResult(relativePath, previousFile, "File changed during analysis. Previous snapshot was retained when available.");
                }

                var version = previousFile is null
                    ? 1
                    : string.Equals(previousFile.ContentHash, contentHash, StringComparison.Ordinal)
                        ? previousFile.Version
                        : previousFile.Version + 1;

                return new FileCaptureResult
                {
                    Snapshot = new TrackedFileSnapshot
                    {
                        RelativePath = relativePath,
                        ContentHash = contentHash,
                        Version = version
                    }
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                if (attempt < retryCount)
                {
                    continue;
                }

                _logger.LogWarning(
                    exception,
                    "Unable to analyze file {FilePath} after {RetryCount} retries.",
                    filePath,
                    retryCount);

                return BuildUnstableFileResult(relativePath, previousFile, "File could not be read reliably after retries. Previous snapshot was retained when available.");
            }
        }

        return BuildUnstableFileResult(relativePath, previousFile, "File could not be read reliably after retries. Previous snapshot was retained when available.");
    }

    private static FileCaptureResult BuildUnstableFileResult(
        string relativePath,
        TrackedFileSnapshot? previousFile,
        string message)
    {
        return new FileCaptureResult
        {
            Snapshot = previousFile,
            Warning = new AnalysisWarning
            {
                RelativePath = relativePath,
                Message = message
            }
        };
    }

    private sealed class SnapshotCapture
    {
        public HashSet<string> Directories { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, TrackedFileSnapshot> Files { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        public List<AnalysisWarning> Warnings { get; init; } = [];
    }

    private sealed class FileCaptureResult
    {
        public TrackedFileSnapshot? Snapshot { get; init; }

        public AnalysisWarning? Warning { get; init; }
    }
}
