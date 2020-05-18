using StdOttStandard.Linq.Sort;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureView.EnumerateFiles
{
    static class FolderSubBackward
    {
        public static IEnumerable<string> Get(string root, string beginFile, string[] extensions)
        {
            if (beginFile == null) beginFile = string.Empty;

            IEnumerable<string> dirFiles;
            string directory = Helper.GetParent(beginFile);

            if (directory.Length > 0)
            {
                try
                {
                    dirFiles = Directory.GetFiles(directory).Where(p => Helper.CompareFilePath(beginFile, p) > 0);

                    Helper.FilterFiles(ref dirFiles, extensions);
                }
                catch
                {
                    yield break;
                }

                foreach (string path in SortUtils.HeapSortDesc(dirFiles, Helper.CompareFilePath))
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
                        parentDirs = Directory.GetDirectories(parent).Select(Helper.NormalizeDirectoryPath)
                            .Where(p => Helper.CompareDirectoryPath(directory, p) > 0);
                    }
                    catch
                    {
                        directory = parent;
                        continue;
                    }

                    foreach (string brother in SortUtils.HeapSortDesc(parentDirs, Helper.CompareDirectoryPath))
                    {
                        foreach (string file in EnumerateFilesReverseRecursive(brother, extensions))
                        {
                            yield return file;
                        }
                    }

                    try
                    {
                        dirFiles = Directory.GetFiles(parent);

                        Helper.FilterFiles(ref dirFiles, extensions);
                    }
                    catch
                    {
                        yield break;
                    }

                    foreach (string file in SortUtils.HeapSortDesc(dirFiles, Helper.CompareFilePath))
                    {
                        yield return file;
                    }

                    directory = parent;
                }
            }

            foreach (string file in EnumerateFilesReverseRecursive(root, extensions))
            {
                if (Helper.CompareFilePath(beginFile, file) >= 0) yield break;

                yield return file;
            }
        }

        private static IEnumerable<string> EnumerateFilesReverseRecursive(string dir, string[] extensions)
        {
            IEnumerable<string> enummeration;

            try
            {
                enummeration = Directory.GetDirectories(dir);
            }
            catch
            {
                enummeration = new string[0];
            }

            foreach (string subDir in SortUtils.HeapSortDesc(enummeration, Helper.NormalizeDirectoryPath, Helper.CompareDirectoryPath))
            {
                foreach (string file in EnumerateFilesReverseRecursive(subDir, extensions))
                {
                    yield return file;
                }
            }

            try
            {
                enummeration = Directory.GetFiles(dir);

                Helper.FilterFiles(ref enummeration, extensions);
            }
            catch
            {
                enummeration = new string[0];
            }

            foreach (string file in SortUtils.HeapSortDesc(enummeration, Helper.CompareFilePath))
            {
                yield return file;
            }
        }
    }
}
