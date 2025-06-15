using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StdOttStandard.Linq.Sort;

namespace PictureView.EnumerateFiles;

static class FolderSubBackward
{
    public static IEnumerable<string> Get(string root, string? beginFile, string[] extensions)
    {
        IEnumerable<string> dirFiles;
        beginFile ??= string.Empty;
        beginFile = Helper.UniformDirectorySeparatorChar(beginFile);
        string directory = Helper.GetParent(beginFile);

        if (directory.Length > 0)
        {
            try
            {
                dirFiles = Directory.GetFiles(directory)
                    .Select(Helper.UniformDirectorySeparatorChar)
                    .Where(p => Helper.CompareFilePath(beginFile, p) > 0);

                Helper.FilterFiles(ref dirFiles, extensions);
            }
            catch
            {
                yield break;
            }

            foreach (string path in dirFiles.HeapSortDesc(Helper.CompareFilePath))
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
                        .Select(Helper.NormalizeDirectoryPath)
                        .Where(p => Helper.CompareDirectoryPath(directory, p) > 0);
                }
                catch
                {
                    directory = parent;
                    continue;
                }

                foreach (string brother in parentDirs.HeapSortDesc(Helper.CompareDirectoryPath))
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

                foreach (string file in dirFiles.HeapSortDesc(Helper.CompareFilePath))
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
        IEnumerable<string> enumeration;

        try
        {
            enumeration = Directory.GetDirectories(dir);
        }
        catch
        {
            enumeration = Array.Empty<string>();
        }

        foreach (string subDir in enumeration.HeapSortDesc(Helper.NormalizeDirectoryPath, Helper.CompareDirectoryPath))
        {
            foreach (string file in EnumerateFilesReverseRecursive(subDir, extensions))
            {
                yield return file;
            }
        }

        try
        {
            enumeration = Directory.GetFiles(dir);

            Helper.FilterFiles(ref enumeration, extensions);
        }
        catch
        {
            enumeration = Array.Empty<string>();
        }

        foreach (string file in enumeration.HeapSortDesc(Helper.CompareFilePath))
        {
            yield return file;
        }
    }
}