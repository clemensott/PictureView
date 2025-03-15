using System.Diagnostics;
using System.IO;

namespace PictureView.Folders.FileExplorer;

public class MacFileExplorer : IFileExplorer
{
    public void Open(string path, bool select)
    {
        if (select) path = Path.GetDirectoryName(path);
        Process.Start("open", path);
    }
}