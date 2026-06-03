namespace PUXsp.Web.Utilities;

public static class PathUtility
{
    public static string NormalizeDirectoryPath(string path)
    {
        var normalizedPath = Path.GetFullPath(path.Trim().Trim('"'));
        var root = Path.GetPathRoot(normalizedPath);

        if (!string.IsNullOrEmpty(root) &&
            normalizedPath.Length > root.Length)
        {
            normalizedPath = normalizedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        return normalizedPath;
    }

    public static string GetRelativePath(string rootPath, string fullPath)
    {
        var relativePath = Path.GetRelativePath(rootPath, fullPath);
        return relativePath == "." ? string.Empty : relativePath;
    }
}
