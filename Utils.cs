using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PictureView
{
    static class Utils
    {
        private const string _orientationQuery = "System.Photo.Orientation";

        public static BitmapImage LoadBitmap(Stream stream)
        {
            return LoadBitmap(stream, IntSize.Empty);
        }

        public static BitmapImage LoadBitmap(Stream stream, IntSize size)
        {
            Rotation rotation;
            try
            {
                stream.Seek(0, SeekOrigin.Begin);
                rotation = LoadImageOrientation(stream);
            }
            catch
            {
                rotation = Rotation.Rotate0;
            }

            stream.Seek(0, SeekOrigin.Begin);
            BitmapImage loadImg = new BitmapImage();

            loadImg.BeginInit();
            loadImg.CacheOption = BitmapCacheOption.Default;
            loadImg.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            loadImg.DecodePixelWidth = size.Width;
            loadImg.DecodePixelHeight = size.Height;
            loadImg.StreamSource = stream;
            loadImg.Rotation = rotation;
            loadImg.EndInit();

            return loadImg;
        }

        public static Rotation LoadImageOrientation(Stream stream)
        {
            BitmapFrame bitmapFrame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            BitmapMetadata bitmapMetadata = bitmapFrame.Metadata as BitmapMetadata;

            if (bitmapMetadata != null && bitmapMetadata.ContainsQuery(_orientationQuery))
            {
                object o = bitmapMetadata.GetQuery(_orientationQuery);

                switch ((ushort?)o)
                {
                    case 6:
                        return Rotation.Rotate90;
                    case 3:
                        return Rotation.Rotate180;
                    case 8:
                        return Rotation.Rotate270;
                }
            }

            return Rotation.Rotate0;
        }
    }
}
