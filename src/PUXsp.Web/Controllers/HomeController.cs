using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PUXsp.Web.Models;
using PUXsp.Web.Services;
using PUXsp.Web.Utilities;
using PUXsp.Web.ViewModels;

namespace PUXsp.Web.Controllers;

public class HomeController : Controller
{
    private readonly IDirectoryAnalysisService _directoryAnalysisService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IDirectoryAnalysisService directoryAnalysisService,
        ILogger<HomeController> logger)
    {
        _directoryAnalysisService = directoryAnalysisService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(new AnalysisPageViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Analyze(AnalysisPageViewModel model, CancellationToken cancellationToken)
    {
        if (!TryNormalizeDirectoryPath(model.DirectoryPath, out var normalizedPath, out var validationMessage))
        {
            ModelState.AddModelError(nameof(model.DirectoryPath), validationMessage);
        }
        else if (!Directory.Exists(normalizedPath))
        {
            ModelState.AddModelError(nameof(model.DirectoryPath), "The provided directory does not exist.");
        }

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        try
        {
            var analysisResult = await _directoryAnalysisService.AnalyzeAsync(
                normalizedPath!,
                model.RetryCount,
                cancellationToken);

            model.DirectoryPath = normalizedPath!;
            model.Result = MapResult(analysisResult);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Directory analysis request for {DirectoryPath} was cancelled.", normalizedPath);
            throw;
        }

        return View("Index", model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static bool TryNormalizeDirectoryPath(string? directoryPath, out string? normalizedPath, out string validationMessage)
    {
        normalizedPath = null;
        validationMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            validationMessage = "Directory path is required.";
            return false;
        }

        try
        {
            normalizedPath = PathUtility.NormalizeDirectoryPath(directoryPath);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            validationMessage = "The provided directory path is not valid.";
            return false;
        }
    }

    private static AnalysisResultViewModel MapResult(DirectoryAnalysisResult result)
    {
        return new AnalysisResultViewModel
        {
            Status = result.Status switch
            {
                AnalysisStatus.BaselineCreated => "Baseline created",
                AnalysisStatus.Completed => "Analysis completed",
                AnalysisStatus.CompletedWithWarnings => "Analysis completed with warnings",
                _ => "Analysis failed"
            },
            IsSuccess = result.Status is AnalysisStatus.BaselineCreated
                or AnalysisStatus.Completed
                or AnalysisStatus.CompletedWithWarnings,
            HasWarnings = result.Warnings.Count > 0,
            AnalyzedPath = result.RootPath,
            StartedAtLocal = result.StartedAtUtc.ToLocalTime(),
            CompletedAtLocal = result.CompletedAtUtc?.ToLocalTime(),
            Duration = result.Duration,
            RetryCountUsed = result.RetryCountUsed,
            ErrorMessage = result.ErrorMessage,
            RegisteredDirectoryCount = result.RegisteredDirectoryCount,
            RegisteredFileCount = result.RegisteredFileCount,
            NewFiles = result.NewFiles
                .Select(file => new AnalysisFileItemViewModel
                {
                    RelativePath = file.RelativePath,
                    CurrentVersion = file.CurrentVersion
                })
                .ToList(),
            ModifiedFiles = result.ModifiedFiles
                .Select(file => new AnalysisFileItemViewModel
                {
                    RelativePath = file.RelativePath,
                    PreviousVersion = file.PreviousVersion,
                    CurrentVersion = file.CurrentVersion
                })
                .ToList(),
            DeletedFiles = result.DeletedFiles,
            NewDirectories = result.NewDirectories,
            DeletedDirectories = result.DeletedDirectories,
            Warnings = result.Warnings
                .Select(warning => new AnalysisWarningViewModel
                {
                    RelativePath = warning.RelativePath,
                    Message = warning.Message
                })
                .ToList()
        };
    }
}
