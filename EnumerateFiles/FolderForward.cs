using StdOttStandard.Linq.Sort;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PictureView.EnumerateFiles
{
    static class FolderForward
    {
        public static IEnumerable<string> Get(string directory, string beginFile, bool startWithBeginFile, string[] extensions)
        {
            IEnumerable<string> dirFiles;
            int compareValue = startWithBeginFile ? 1 : 0;

            if (beginFile == null) beginFile = string.Empty;

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
            string[] array = SortUtils.HeapSort(dirFiles, Helper.CompareFilePath).ToArray();

            for (splitFilesIndex = 0; splitFilesIndex < array.Length; splitFilesIndex++)
            {
                if (Helper.CompareFilePath(beginFile, array[splitFilesIndex]) < compareValue) break;
            }

            for (int i = splitFilesIndex; i < array.Length; i++)
            {
                yield return array[i];
            }

            for (int i = 0; i < splitFilesIndex; i++)
            {
                yield return array[i];
            }
        }

    }
}
