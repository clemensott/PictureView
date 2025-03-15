using System;

namespace PictureView.Folders.FileExplorer;

public static class FileExplorerHelper
{
    public static IFileExplorer? GetFileExplorer()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsFileExplorer();
        }

        if (OperatingSystem.IsLinux())
        {
            return new LinuxFileExplorer();
        }

        if (OperatingSystem.IsMacOS())
        {
            return new MacFileExplorer();
        }

        return null;
    }
}