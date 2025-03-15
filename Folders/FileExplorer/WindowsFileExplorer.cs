using System.Diagnostics;

namespace PictureView.Folders.FileExplorer;

public class WindowsFileExplorer: IFileExplorer
{
    public void Open(string path, bool select)
    {
        string args = select ? $"/select,\"{path}\"" : $"\"{path}\"";
        Process.Start("explorer.exe", args);
    }
}