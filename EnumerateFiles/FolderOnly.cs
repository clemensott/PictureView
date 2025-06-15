using System.Collections.Generic;
using System.IO;
using System.Linq;
using StdOttStandard.Linq.Sort;

namespace PictureView.EnumerateFiles;

static class FolderOnly
{
    private static (IEnumerable<string> beforeSplitFiles, IEnumerable<string> afterSplitFiles) SplitFiles(
        string directory, string? beginFile, string[] extensions, int compareValue)
    {
        IEnumerable<string> dirFiles;
        beginFile ??= string.Empty;
        beginFile = Helper.UniformDirectorySeparatorChar(beginFile);

        try
        {
            dirFiles = Directory.GetFiles(directory);

            Helper.FilterFiles(ref dirFiles, extensions);
        }
        catch
        {
            dirFiles = [];
        }

        List<string> afterSplitFiles = new List<string>();
        List<string> beforeSplitFiles = new List<string>();

        foreach (string file in dirFiles.Select(Helper.UniformDirectorySeparatorChar))
        {
            if (Helper.CompareFilePath(beginFile, file) < compareValue) afterSplitFiles.Add(file);
            else beforeSplitFiles.Add(file);
        }

        return (beforeSplitFiles, afterSplitFiles);
    }

    public static IEnumerable<string> GetForward(string directory, string? beginFile, bool startWithBeginFile,
        string[] extensions)
    {
        int compareValue = startWithBeginFile ? 1 : 0;
        var (beforeSplitFiles, afterSplitFiles) = SplitFiles(directory, beginFile, extensions, compareValue);
        
        foreach (string file in afterSplitFiles.HeapSort(Helper.CompareFilePath))
        {
            yield return file;
        }
        
        foreach (string file in beforeSplitFiles.HeapSort(Helper.CompareFilePath))
        {
            yield return file;
        }
    }

    public static IEnumerable<string> GetBackwards(string directory, string? beginFile, string[] extensions)
    {
        var (beforeSplitFiles, afterSplitFiles) = SplitFiles(directory, beginFile, extensions, 1);

        foreach (string file in beforeSplitFiles.HeapSortDesc(Helper.CompareFilePath))
        {
            yield return file;
        }
        
        foreach (string file in afterSplitFiles.HeapSortDesc(Helper.CompareFilePath))
        {
            yield return file;
        }
    }
}