using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PictureView
{
    class FileSystemImage : IDisposable, INotifyPropertyChanged
    {
        private bool isImageOutdated, isImageLoaded;
        private Rect? cropRect;
        private Stream stream;
        private BitmapSource image;
        private ImageSource croppedImage;

        public FileInfo File { get; }

        public bool IsImageOutdated
        {
            get => isImageOutdated;
            private set
            {
                if (value == isImageOutdated) return;

                isImageOutdated = value;
                OnPropertyChanged(nameof(IsImageOutdated));
            }
        }

        public bool IsImageLoaded
        {
            get => isImageLoaded;
            set
            {
                if (value == isImageLoaded) return;

                isImageLoaded = value;
                OnPropertyChanged(nameof(IsImageLoaded));
            }
        }

        public long? DataSize => stream?.Length;

        public Rect? CropRect
        {
            get => cropRect;
            set
            {
                value = Limit(value, Image?.PixelWidth, Image?.PixelHeight);

                if (value != cropRect)
                {
                    cropRect = value;
                    OnPropertyChanged(nameof(CropRect));
                }

                CroppedImage = GetCroppedImage(Image, CropRect);
            }
        }

        public BitmapSource Image
        {
            get => image;
            private set
            {
                if (value == image) return;

                image = value;
                OnPropertyChanged(nameof(Image));

                CropRect = null;
            }
        }

        public ImageSource CroppedImage
        {
            get => croppedImage;
            private set
            {
                if (value == croppedImage) return;

                croppedImage = value;
                OnPropertyChanged(nameof(CroppedImage));
            }
        }

        public FileSystemImage(FileInfo file)
        {
            File = file;

            IsImageLoaded = false;
            stream = null;
            Image = null;
        }

        public async Task LoadBytes()
        {
            try
            {
                File.Refresh();

                if (File.Length < 100000000)
                {
                    Stream destStream = new MemoryStream();
                    using Stream srcStream = System.IO.File.OpenRead(File.FullName);
                    await srcStream.CopyToAsync(destStream);

                    Stream oldStream = stream;
                    stream = destStream;

                    IsImageOutdated = oldStream == null || !SequenceEqual(oldStream, destStream);
                    oldStream?.Dispose();
                }
                else Dispose();
            }
            catch
            {
                Dispose();
            }
        }

        private static bool SequenceEqual(Stream stream1, Stream stream2)
        {
            if (stream1.Length != stream2.Length) return false;

            stream1.Seek(0, SeekOrigin.Begin);
            stream2.Seek(0, SeekOrigin.Begin);

            const int bufferSize = 1000;
            byte[] buffer1 = new byte[bufferSize], buffer2 = new byte[bufferSize];

            while (stream1.Position < stream1.Length)
            {
                int read1 = stream1.Read(buffer1, 0, bufferSize);
                int read2 = stream2.Read(buffer2, 0, bufferSize);

                if (read1 != read2 || !buffer1.Take(read1).SequenceEqual(buffer2.Take(read2))) return false;
            }

            return true;
        }

        public void LoadImage()
        {
            try
            {
                Image = stream != null ? Utils.LoadBitmap(stream) : null;
            }
            catch
            {
                Image = null;
            }
            finally
            {
                IsImageLoaded = true;
            }

            IsImageOutdated = false;
        }

        private static Rect? Limit(Rect? rect, int? maxWidth, int? maxHeight)
        {
            if (!rect.HasValue) return null;

            Rect r = rect.Value;
            int maxW = maxWidth ?? 0;
            int maxH = maxHeight ?? 0;

            double x = Math.Max(r.X, 0);
            double y = Math.Max(r.Y, 0);
            double width = Math.Max(r.Width, 0);
            double height = Math.Max(r.Height, 0);

            if (width > maxW) width = maxW;
            if (height > maxH) height = maxH;

            if (x + width > maxW) x = maxW - width;
            if (y + height > maxH) y = maxH - height;

            return new Rect(x, y, width, height);
        }

        private static ImageSource GetCroppedImage(BitmapSource bmp, Rect? rect)
        {
            if (bmp == null) return null;
            if (!rect.HasValue) return bmp;

            try
            {
                return new CroppedBitmap(BitmapFrame.Create(bmp), Convert(rect.Value));
            }
            catch
            {
                return bmp;
            }
        }

        private static Int32Rect Convert(Rect rect)
        {
            return new Int32Rect((int)Math.Floor(rect.X), (int)Math.Floor(rect.Y),
                (int)Math.Floor(rect.Width), (int)Math.Floor(rect.Height));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Dispose()
        {
            stream?.Dispose();
            stream = null;
            Image = null;
            CroppedImage = null;
        }
    }
}
