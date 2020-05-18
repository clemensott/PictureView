using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using StdOttStandard.Linq;

namespace PictureView
{
    class FileSystemImage : IDisposable, INotifyPropertyChanged
    {
        private byte[] data;
        private bool isImageOutdated, isImageLoaded;
        private Rect? cropRect;
        private BitmapSource image;
        private ImageSource croppedImage;

        public FileInfo File { get; }

        public byte[] Data
        {
            get => data;
            private set
            {
                if (value.BothNullOrSequenceEqual(data)) return;

                data = value;
                OnPropertyChanged(nameof(Data));

                IsImageOutdated = true;
            }
        }

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
            Data = null;
            Image = null;
        }

        public void LoadBytes()
        {
            try
            {
                File.Refresh();

                Data = File.Length < 100000000 ? System.IO.File.ReadAllBytes(File.FullName) : new byte[0];
            }
            catch
            {
                Data = new byte[0];
            }
        }

        public void LoadImage()
        {
            try
            {
                Image = Utils.LoadBitmap(Data);
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
            Data = null;
            Image = null;
            CroppedImage = null;
        }
    }
}
