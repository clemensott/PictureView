using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PictureView
{
    static class Utils
    {
        public static BitmapImage LoadBitmap(string path)
        {
            return LoadBitmap(File.ReadAllBytes(path), IntSize.Empty);
        }

        public static async Task<BitmapImage> LoadBitmapAsync(string path)
        {
            byte[] data = await Task.Run(() => File.ReadAllBytes(path));

            return LoadBitmap(data, IntSize.Empty);
        }

        public static BitmapImage LoadBitmap(byte[] data)
        {
            return LoadBitmap(data, IntSize.Empty);
        }

        public static BitmapImage LoadBitmap(byte[] data, IntSize size)
        {
            MemoryStream mem = new MemoryStream(data);
            BitmapImage loadImg = new BitmapImage();

            loadImg.BeginInit();
            loadImg.CacheOption = BitmapCacheOption.Default;
            loadImg.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            loadImg.DecodePixelWidth = size.Width;
            loadImg.DecodePixelHeight = size.Height;
            loadImg.StreamSource = mem;
            loadImg.Rotation = Rotation.Rotate0;
            loadImg.EndInit();

            return loadImg;
        }
    }
}
