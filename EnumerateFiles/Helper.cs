using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureView.EnumerateFiles;

static class Helper
{
    private static readonly StringComparison comparisonType = GetStringComparison();

    private static StringComparison GetStringComparison()
    {
        return OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    }

    public static string UniformDirectorySeparatorChar(string path)
    {
        return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }

    public static void NormalizeDirectoryPath(ref string path)
    {
        path = NormalizeDirectoryPath(path);
    }

    public static string NormalizeDirectoryPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;

        path = UniformDirectorySeparatorChar(path);
        path = Path.GetFullPath(path);
        if (path[^1] != Path.DirectorySeparatorChar) path += Path.DirectorySeparatorChar;

        return path;
    }

    public static int CompareDirectoryPath(string path1, string path2)
    {
        path1 = UniformDirectorySeparatorChar(path1);
        path2 = UniformDirectorySeparatorChar(path2);

        if (path1 == path2) return 0;

        string[] path1Parts = path1.Split(Path.DirectorySeparatorChar);
        string[] path2Parts = path2.Split(Path.DirectorySeparatorChar);

        for (int i = 0; i < path1Parts.Length && i < path2Parts.Length; i++)
        {
            int compare = string.Compare(path1Parts[i], path2Parts[i], comparisonType);

            if (compare != 0) return compare;
        }

        return path1Parts.Length.CompareTo(path2Parts.Length);
    }

    public static int CompareFilePath(string path1, string path2)
    {
        int compareDirs = CompareDirectoryPath(GetParent(path1), GetParent(path2));

        return compareDirs != 0 ? compareDirs : string.Compare(path1, path2, comparisonType);
    }

    public static IEnumerable<string> GetPathSteps(string currentPath, string destPath)
    {
        NormalizeDirectoryPath(ref currentPath);
        NormalizeDirectoryPath(ref destPath);

        if (!destPath.StartsWith(currentPath)) throw new ArgumentException();

        return destPath[currentPath.Length..]
            .Split(Path.DirectorySeparatorChar)
            .Where(p => !string.IsNullOrWhiteSpace(p));
    }

    public static string GetDirectoryPath(string path)
    {
        path = Path.GetFullPath(path);

        if (File.Exists(path)) return GetParent(path);
        if (Directory.Exists(path)) return path;

        return GetParent(path);
    }

    public static string GetParent(string path)
    {
        path = UniformDirectorySeparatorChar(path)
            .TrimEnd(Path.DirectorySeparatorChar);

        if (!path.Contains(Path.DirectorySeparatorChar)) return string.Empty;

        return path.Remove(path.LastIndexOf(Path.DirectorySeparatorChar)) + Path.DirectorySeparatorChar;
    }

    public static void FilterFiles(ref IEnumerable<string> files, string[]? extensions)
    {
        if (extensions?.Length > 0)
        {
            files = files.Where(f => extensions.Any(f.ToLower().EndsWith));
        }
    }
}