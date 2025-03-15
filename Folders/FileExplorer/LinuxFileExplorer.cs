using System;
using System.Diagnostics;
using System.IO;

namespace PictureView.Folders.FileExplorer;

public class LinuxFileExplorer : IFileExplorer
{
    private string? GetDefaultFileExplorer()
    {
        try
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo("xdg-mime", "query default inode/directory")
            {
                RedirectStandardOutput = true
            };

            process.Start();
            return process.WaitForExit(1000)
                ? process.StandardOutput.ReadLine()
                : null;
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine(e);
            return null;
        }
    }

    public void Open(string path, bool select)
    {
        string? defaultFileExplorer = select ? GetDefaultFileExplorer() : null;
        switch (defaultFileExplorer)
        {
            case "nemo.desktop":
                Process.Start("nemo", path);
                break;

            default:
                if (select) path = Path.GetDirectoryName(path);
                Process.Start("xdg-open", path);
                break;
        }
    }
}