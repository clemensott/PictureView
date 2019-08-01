using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PictureView
{
    static class Utils
    {
        public static BitmapImage LoadBitmap(string path)
        {
            return LoadBitmap(File.ReadAllBytes(path), IntSize.Empty);
        }

        public static async Task<BitmapImage> LoadBitmapAsync(string path)
        {
            byte[] data = await Task.Run(() => File.ReadAllBytes(path));

            return LoadBitmap(data, IntSize.Empty);
        }

        public static BitmapImage LoadBitmap(byte[] data)
        {
            return LoadBitmap(data, IntSize.Empty);
        }

        public static BitmapImage LoadBitmap(byte[] data, IntSize size)
        {
            MemoryStream mem = new MemoryStream(data);
            BitmapImage loadImg = new BitmapImage();

            loadImg.BeginInit();
            loadImg.CacheOption = BitmapCacheOption.Default;
            loadImg.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            loadImg.DecodePixelWidth = size.Width;
            loadImg.DecodePixelHeight = size.Height;
            loadImg.StreamSource = mem;
            loadImg.Rotation = Rotation.Rotate0;
            loadImg.EndInit();

            return loadImg;
        }

        public static void NormalizePath(ref string path)
        {
            path = NormalizePath(path);
        }

        public static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;

            path = Path.GetFullPath(path);
            if (path[path.Length - 1] != '\\') path += '\\';

            return path;
        }

        public static int CompareDirectoryPath(string path1, string path2)
        {
            path1 = NormalizePath(path1);
            path2 = NormalizePath(path2);

            if (path1 == path2) return 0;

            string[] path1Parts = path1.Split('\\');
            string[] path2Parts = path2.Split('\\');

            for (int i = 0; i < path1Parts.Length && i < path2Parts.Length; i++)
            {
                int compare = string.Compare(path1Parts[i], path2Parts[i], StringComparison.OrdinalIgnoreCase);

                if (compare != 0) return compare;
            }

            return path1Parts.Length.CompareTo(path2Parts.Length);
        }

        public static int CompareFilePath(string path1, string path2)
        {
            int compareDirs = CompareDirectoryPath(GetParent(path1), GetParent(path2));

            return compareDirs != 0 ? compareDirs : string.Compare(path1, path2, StringComparison.OrdinalIgnoreCase);
        }

        public static IEnumerable<string> GetPathSteps(string currentPath, string destPath)
        {
            NormalizePath(ref currentPath);
            NormalizePath(ref destPath);

            if (!destPath.StartsWith(currentPath)) throw new ArgumentException();

            return destPath.Substring(currentPath.Length).Split('\\').Where(p => !string.IsNullOrWhiteSpace(p));
        }

        public static IEnumerable<string> EnumerateFilesFromFolder(string directory,
            string beginFile, bool includeBeginFile, string[] extensions)
        {
            IEnumerable<string> dirFiles;
            int compareValue = includeBeginFile ? 1 : 0;

            if (beginFile == null) beginFile = string.Empty;

            try
            {
                dirFiles = Directory.GetFiles(directory).SkipWhile(p => CompareFilePath(beginFile, p) >= compareValue);

                FilterFiles(ref dirFiles, extensions);
            }
            catch
            {
                yield break;
            }

            foreach (string path in dirFiles)
            {
                yield return path;
            }

            try
            {
                dirFiles = Directory.GetFiles(directory).TakeWhile(p => CompareFilePath(beginFile, p) >= 0);

                FilterFiles(ref dirFiles, extensions);
            }
            catch
            {
                yield break;
            }

            foreach (string path in dirFiles)
            {
                yield return path;
            }
        }

        public static IEnumerable<string> EnumerateAllFiles(string root, string beginFile, bool startBeginFile, string[] extensions)
        {
            int compareValue = startBeginFile ? 1 : 0;
            string directory = GetParent(beginFile);

            if (beginFile == null) beginFile = string.Empty;

            if (directory.Length > 0)
            {
                IEnumerable<string> dirFiles;
                try
                {
                    dirFiles = EnumerateFilesRecursive(directory, extensions)
                        .SkipWhile(p => CompareFilePath(beginFile, p) >= compareValue);
                }
                catch
                {
                    yield break;
                }

                foreach (string path in dirFiles)
                {
                    yield return path;
                }

                NormalizePath(ref root);

                while (true)
                {
                    IEnumerable<string> parentDirs;
                    string parent = GetParent(directory);

                    if (parent.Length < root.Length) break;

                    try
                    {
                        parentDirs = Directory.GetDirectories(parent)
                            .SkipWhile(p => CompareDirectoryPath(directory, p) >= 0);
                    }
                    catch
                    {
                        directory = parent;
                        continue;
                    }

                    foreach (string brother in parentDirs)
                    {
                        foreach (string file in EnumerateFilesRecursive(brother, extensions))
                        {
                            yield return file;
                        }
                    }

                    directory = parent;
                }
            }

            foreach (string file in EnumerateFilesRecursive(root, extensions))
            {
                if (!string.IsNullOrWhiteSpace(beginFile) &&
                    CompareFilePath(file, beginFile) >= compareValue) yield break;

                yield return file;
            }
        }

        private static IEnumerable<string> EnumerateFilesRecursive(string dir, string[] extensions)
        {
            IEnumerable<string> enumeration;

            try
            {
                enumeration = Directory.GetFiles(dir);

                FilterFiles(ref enumeration, extensions);
            }
            catch
            {
                enumeration = new string[0];
            }

            foreach (string file in enumeration)
            {
                yield return file;
            }

            try
            {
                enumeration = Directory.GetDirectories(dir);
            }
            catch
            {
                enumeration = new string[0];
            }

            foreach (string subDir in enumeration)
            {
                foreach (string file in EnumerateFilesRecursive(subDir, extensions))
                {
                    yield return file;
                }
            }
        }

        public static IEnumerable<string> EnumerateFilesFromFolderReverse(string directory, string beginFile, string[] extensions)
        {
            IEnumerable<string> dirFiles;
            if (beginFile == null) beginFile = string.Empty;

            try
            {
                dirFiles = Directory.GetFiles(directory).TakeWhile(p => CompareFilePath(beginFile, p) > 0);

                FilterFiles(ref dirFiles, extensions);
            }
            catch
            {
                yield break;
            }

            foreach (string path in dirFiles.Reverse())
            {
                yield return path;
            }

            try
            {
                dirFiles = Directory.GetFiles(directory).SkipWhile(p => CompareFilePath(beginFile, p) >= 0);

                FilterFiles(ref dirFiles, extensions);
            }
            catch
            {
                yield break;
            }


            foreach (string path in dirFiles.Reverse())
            {
                yield return path;
            }
        }

        public static IEnumerable<string> EnumerateAllFilesReverse(string root, string beginFile, string[] extensions)
        {
            IEnumerable<string> dirFiles;
            string directory = GetParent(beginFile);

            if (beginFile == null) beginFile = string.Empty;

            if (directory.Length > 0)
            {
                try
                {
                    dirFiles = Directory.GetFiles(directory).TakeWhile(p => CompareFilePath(beginFile, p) > 0);

                    FilterFiles(ref dirFiles, extensions);
                }
                catch
                {
                    yield break;
                }

                foreach (string path in dirFiles.Reverse())
                {
                    yield return path;
                }

                NormalizePath(ref root);

                while (true)
                {
                    IEnumerable<string> parentDirs;
                    string parent = GetParent(directory);

                    if (parent.Length < root.Length) break;

                    try
                    {
                        parentDirs = Directory.GetDirectories(parent).Select(NormalizePath)
                            .TakeWhile(p => CompareDirectoryPath(directory, p) > 0);
                    }
                    catch
                    {
                        directory = parent;
                        continue;
                    }

                    foreach (string brother in parentDirs.Reverse())
                    {
                        foreach (string file in EnumerateFilesReverseRecursive(brother, extensions))
                        {
                            yield return file;
                        }
                    }

                    try
                    {
                        dirFiles = Directory.GetFiles(parent);

                        FilterFiles(ref dirFiles, extensions);
                    }
                    catch
                    {
                        yield break;
                    }

                    foreach (string file in dirFiles)
                    {
                        yield return file;
                    }

                    directory = parent;
                }
            }

            foreach (string file in EnumerateFilesReverseRecursive(root, extensions))
            {
                if (CompareFilePath(beginFile, file) >= 0) yield break;

                yield return file;
            }
        }

        private static IEnumerable<string> EnumerateFilesReverseRecursive(string dir, string[] extensions)
        {
            IEnumerable<string> enumeration;

            try
            {
                enumeration = Directory.GetDirectories(dir);
            }
            catch
            {
                enumeration = new string[0];
            }

            foreach (string subDir in enumeration.Reverse())
            {
                foreach (string file in EnumerateFilesReverseRecursive(subDir, extensions))
                {
                    yield return file;
                }
            }

            try
            {
                enumeration = Directory.GetFiles(dir);

                FilterFiles(ref enumeration, extensions);
            }
            catch
            {
                enumeration = new string[0];
            }

            foreach (string file in enumeration.Reverse())
            {
                yield return file;
            }
        }

        private static string GetDirectoryPath(string path)
        {
            path = Path.GetFullPath(path);

            if (File.Exists(path)) return GetParent(path);
            if (Directory.Exists(path)) return path;

            return GetParent(path);
        }

        private static string GetParent(string path)
        {
            path = path.TrimEnd('\\');

            if (!path.Contains('\\')) return string.Empty;

            return path.Remove(path.LastIndexOf('\\')) + '\\';
        }

        private static void FilterFiles(ref IEnumerable<string> files, string[] extensions)
        {
            if (extensions != null && extensions.Length > 0)
            {
                files = files.Where(f => extensions.Any(f.ToLower().EndsWith));
            }
        }
    }
}
