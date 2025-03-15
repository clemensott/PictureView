using System.Collections.Generic;
using System.IO;
using System.Linq;
using StdOttStandard.Linq.Sort;

namespace PictureView.EnumerateFiles;

static class FolderBackward
{
    public static IEnumerable<string> Get(string directory, string? beginFile, string[] extensions)
    {
        IEnumerable<string> dirFiles;
        beginFile ??= string.Empty;

        try
        {
            dirFiles = Directory.GetFiles(directory);

            Helper.FilterFiles(ref dirFiles, extensions);
        }
        catch
        {
            yield break;
        }

        int splitFilesIndex;
        string[] array = dirFiles.HeapSort(Helper.CompareFilePath).ToArray();

        for (splitFilesIndex = 0; splitFilesIndex < array.Length; splitFilesIndex++)
        {
            if (Helper.CompareFilePath(beginFile, array[splitFilesIndex]) <= 0) break;
        }

        for (int i = splitFilesIndex - 1; i >= 0; i--)
        {
            yield return array[i];
        }

        for (int i = array.Length - 1; i >= splitFilesIndex; i--)
        {
            yield return array[i];
        }
    }
}