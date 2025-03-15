namespace PictureView.Folders.FileExplorer;

public interface IFileExplorer
{
    void Open(string path, bool select = false);
}