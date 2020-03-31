using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureView.EnumerateFiles
{
    static class Helper
    {
        public static void NormalizeDirectoryPath(ref string path)
        {
            path = NormalizeDirectoryPath(path);
        }

        public static string NormalizeDirectoryPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;

            path = Path.GetFullPath(path);
            if (path[path.Length - 1] != '\\') path += '\\';

            return path;
        }

        public static int CompareDirectoryPath(string path1, string path2)
        {
            if (path1 == path2) return 0;

            string[] path1Parts = path1.Split('\\');
            string[] path2Parts = path2.Split('\\');

            for (int i = 0; i < path1Parts.Length && i < path2Parts.Length; i++)
            {
                int compare = string.Compare(path1Parts[i], path2Parts[i], StringComparison.OrdinalIgnoreCase);

                if (compare != 0) return compare;
            }

            return 0;
        }

        public static int CompareFilePath(string path1, string path2)
        {
            int compareDirs = CompareDirectoryPath(GetParent(path1), GetParent(path2));

            return compareDirs != 0 ? compareDirs : string.Compare(path1, path2, StringComparison.OrdinalIgnoreCase);
        }

        public static IEnumerable<string> GetPathSteps(string currentPath, string destPath)
        {
            NormalizeDirectoryPath(ref currentPath);
            NormalizeDirectoryPath(ref destPath);

            if (!destPath.StartsWith(currentPath)) throw new ArgumentException();

            return destPath.Substring(currentPath.Length).Split('\\').Where(p => !string.IsNullOrWhiteSpace(p));
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
            path = path.TrimEnd('\\');

            if (!path.Contains('\\')) return string.Empty;

            return path.Remove(path.LastIndexOf('\\')) + '\\';
        }

        public static void FilterFiles(ref IEnumerable<string> files, string[] extensions)
        {
            if (extensions != null && extensions.Length > 0)
            {
                files = files.Where(f => extensions.Any(f.ToLower().EndsWith));
            }
        }
    }
}
