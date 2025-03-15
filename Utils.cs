using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace PictureView;

public static class Utils
{
    public static IImage LoadBitmap(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        return new Bitmap(stream);
    }
}