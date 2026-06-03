using PUXsp.Web.Models;

namespace PUXsp.Web.Services;

public interface IDirectoryAnalysisService
{
    Task<DirectoryAnalysisResult> AnalyzeAsync(string rootPath, int retryCount, CancellationToken cancellationToken);
}
