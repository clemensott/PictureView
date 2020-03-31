using StdOttStandard;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureView.EnumerateFiles
{
    class FolderSubForward
    {
        public static IEnumerable<string> Get(string root, string beginFile, bool startWithBeginFile, string[] extensions)
        {
            if (beginFile == null) beginFile = string.Empty;

            int compareValue = startWithBeginFile ? 1 : 0;
            string directory = Helper.GetParent(beginFile);

            if (directory.Length > 0)
            {
                IEnumerable<string> dirFiles;
                try
                {
                    dirFiles = EnumerateFilesRecursive(directory, extensions)
                        .SkipWhile(p => Helper.CompareFilePath(beginFile, p) >= compareValue);
                }
                catch
                {
                    yield break;
                }

                foreach (string path in dirFiles)
                {
                    yield return path;
                }

                Helper.NormalizeDirectoryPath(ref root);

                while (true)
                {
                    IEnumerable<string> parentDirs;
                    string parent = Helper.GetParent(directory);

                    if (parent.Length < root.Length) break;

                    try
                    {
                        parentDirs = Directory.GetDirectories(parent)
                            .Where(p => Helper.CompareDirectoryPath(directory, p) < 0);
                    }
                    catch
                    {
                        directory = parent;
                        continue;
                    }

                    foreach (string brother in Sort.HeapSort(parentDirs, Helper.NormalizeDirectoryPath, Helper.CompareDirectoryPath))
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
                    Helper.CompareFilePath(file, beginFile) >= compareValue) yield break;

                yield return file;
            }
        }

        private static IEnumerable<string> EnumerateFilesRecursive(string dir, string[] extensions)
        {
            IEnumerable<string> enumeration;

            try
            {
                enumeration = Directory.GetFiles(dir);

                Helper.FilterFiles(ref enumeration, extensions);
            }
            catch
            {
                enumeration = new string[0];
            }

            foreach (string file in Sort.HeapSort(enumeration, Helper.CompareFilePath))
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

            foreach (string subDir in Sort.HeapSort(enumeration, Helper.NormalizeDirectoryPath, Helper.CompareDirectoryPath))
            {
                foreach (string file in EnumerateFilesRecursive(subDir, extensions))
                {
                    yield return file;
                }
            }
        }

    }
}
