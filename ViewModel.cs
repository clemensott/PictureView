using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FolderFile;
using StdOttStandard;

namespace PictureView
{
    class ViewModel : INotifyPropertyChanged
    {
        private const int opacityStepDelay = 5;
        private const double opacityStep = 0.05;
        private const string applicationName = "Picview";

        private readonly SemaphoreSlim updateImagesSlowSem;
        private int updateCount, lastUpdateOffset;
        private bool isUpdatingImages, viewControls, isUpdatingOpacity,
            useSource, isFullscreen, isApplyingFullscreen, isDeleteDirect;
        private double controlsOpacity;
        private WindowState windowState;
        private WindowStyle windowStyle;
        private string extensions;
        private string[] sources;
        private Color backgroundColor;
        private readonly FiFoBuffer<string, FileSystemImage> buffer;
        private FileSystemImage currentImage, previousImage, nextImage;
        private Folder source, destination;
        private FileSystemCollision copyCollision;

        public bool IsDeleteDirect
        {
            get => isDeleteDirect;
            set
            {
                if (value == isDeleteDirect) return;

                isDeleteDirect = value;
                OnPropertyChanged(nameof(IsDeleteDirect));
            }
        }

        public bool ViewControls
        {
            get => viewControls;
            set
            {
                if (value == viewControls) return;

                viewControls = value;
                OnPropertyChanged(nameof(ViewControls));

                AdjustControlsOpacity();
            }
        }

        public bool UseSource
        {
            get => useSource;
            set
            {
                if (value == useSource) return;

                useSource = value;
                OnPropertyChanged(nameof(UseSource));
            }
        }

        public bool IsFullscreen
        {
            get => isFullscreen;
            set
            {
                if (value == isFullscreen) return;

                isFullscreen = value;
                OnPropertyChanged(nameof(IsFullscreen));

                if (IsFullscreen)
                {
                    isApplyingFullscreen = true;
                    OnPropertyChanged(nameof(WindowState));
                    isApplyingFullscreen = false;
                }

                OnPropertyChanged(nameof(WindowStyle));
                OnPropertyChanged(nameof(WindowState));
            }
        }

        public double ControlsOpacity
        {
            get => controlsOpacity;
            private set
            {
                if (Math.Abs(value - controlsOpacity) < 0.01) return;

                controlsOpacity = value;
                OnPropertyChanged(nameof(ControlsOpacity));
            }
        }

        public WindowState WindowState
        {
            get
            {
                if (isApplyingFullscreen) return WindowState.Normal;

                return IsFullscreen ? WindowState.Maximized : windowState;
            }

            set
            {
                if (value == windowState) return;

                windowState = value;
                OnPropertyChanged(nameof(WindowState));

                IsFullscreen = false;
            }
        }

        public WindowStyle WindowStyle
        {
            get => IsFullscreen ? WindowStyle.None : windowStyle;
            set
            {
                if (value == windowStyle) return;

                windowStyle = value;
                OnPropertyChanged(nameof(WindowStyle));

                IsFullscreen = false;
            }
        }

        public string Extensions
        {
            get => extensions;
            set
            {
                if (value == extensions) return;

                extensions = value;
                OnPropertyChanged(nameof(Extensions));
            }
        }

        public string[] Sources
        {
            get => sources;
            set
            {
                if (value == sources) return;

                sources = value;
                OnPropertyChanged(nameof(Sources));

                if (sources == null || sources.Length == 0) ViewControls = true;

                if (sources == null || sources.Length != 1)
                {
                    Source = null;
                    UpdateImages();
                }
                else UpdateImages(0, sources[0]);
            }
        }

        public Color BackgroundColor
        {
            get => backgroundColor;
            set
            {
                if (value == backgroundColor) return;

                backgroundColor = value;

                OnPropertyChanged(nameof(BackgroundColor));
                OnPropertyChanged(nameof(BackgroundBrush));
            }
        }

        public Brush BackgroundBrush => new SolidColorBrush(BackgroundColor);

        public FileSystemImage CurrentImage
        {
            get => currentImage;
            private set
            {
                if (value == currentImage) return;

                currentImage = value;
                OnPropertyChanged(nameof(CurrentImage));
                OnPropertyChanged(nameof(ApplicationTitle));

                ResetZoom();
            }
        }

        public FileSystemImage PreviousImage
        {
            get => previousImage;
            private set
            {
                if (value == previousImage) return;

                previousImage = value;
                OnPropertyChanged(nameof(PreviousImage));
            }
        }

        public FileSystemImage NextImage
        {
            get => nextImage;
            private set
            {
                if (value == nextImage) return;

                nextImage = value;
                OnPropertyChanged(nameof(NextImage));
            }
        }

        public Folder Source
        {
            get => source;
            set
            {
                if (value == source) return;

                if (source != null) source.PropertyChanged -= Source_PropertyChanged;
                source = value;
                if (source != null) source.PropertyChanged += Source_PropertyChanged;

                OnPropertyChanged(nameof(Source));

                UpdateImages();

                UseSource = Source != null;
            }
        }

        public Folder Destination
        {
            get => destination;
            set
            {
                if (value == destination) return;

                destination = value;
                OnPropertyChanged(nameof(Destination));
            }
        }

        public FileSystemCollision CopyCollision
        {
            get => copyCollision;
            set
            {
                if (value == copyCollision) return;

                copyCollision = value;
                OnPropertyChanged(nameof(CopyCollision));
            }
        }

        public string ApplicationTitle => GetTitle(CurrentImage);

        public ViewModel()
        {
            updateImagesSlowSem = new SemaphoreSlim(1);
            buffer = new FiFoBuffer<string, FileSystemImage>(30);

            ViewControls = true;
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.SingleBorderWindow;
            BackgroundColor = Colors.White;
            CopyCollision = FileSystemCollision.Ask;
            Extensions = ".jpg|.jpeg|.jpe|.gif|.tiff|.ico|.png|.bmp";
        }

        private async void AdjustControlsOpacity()
        {
            if (isUpdatingOpacity) return;
            isUpdatingOpacity = true;

            while (true)
            {
                if (ViewControls)
                {
                    if (1 - ControlsOpacity <= opacityStep)
                    {
                        ControlsOpacity = 1;
                        break;
                    }

                    ControlsOpacity += opacityStep;
                    await Task.Delay(opacityStepDelay);
                }
                else
                {
                    if (ControlsOpacity <= opacityStep)
                    {
                        ControlsOpacity = 0;
                        break;
                    }

                    ControlsOpacity -= opacityStep;
                    await Task.Delay(opacityStepDelay);
                }
            }

            isUpdatingOpacity = false;
        }

        private void Source_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Source.SubType)) return;

            UpdateImages();
        }

        public void ResetZoom()
        {
            if (CurrentImage != null) CurrentImage.CropRect = null;
        }

        public void UpdateImages(int offset = 0)
        {
            UpdateImages(offset, null);
        }

        private async void UpdateImages(int offset, string path)
        {
            lastUpdateOffset = offset;

            if (isUpdatingImages) return;
            isUpdatingImages = true;

            try
            {
                FileSystemImage oldCurrentImage;

                if (path == null)
                {
                    oldCurrentImage = CurrentImage;

                    UpdateImagesFast(oldCurrentImage, offset);
                }
                else
                {
                    string dirPath = GetDirectoryPath(path);

                    Source = new Folder(dirPath, Source?.SubType ?? SubfolderType.This);
                    oldCurrentImage = dirPath == path ? null : GetImage(path);
                }

                CurrentImage = FindCurrentImage(oldCurrentImage, offset);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }

            isUpdatingImages = false;

            await Task.Delay(100);

            if (updateCount > 0) return;

            updateCount++;
            await updateImagesSlowSem.WaitAsync();

            if (updateCount == 1) await UpdateImagesSlow();

            updateCount--;
            updateImagesSlowSem.Release();
        }

        private IEnumerable<string> EnumerateFiles(string beginFile, bool startWithBeginFile)
        {
            if (Source != null)
            {
                return Source.SubType == SubfolderType.All ?
                    Utils.EnumerateAllFiles(Source.FullName, beginFile, startWithBeginFile, GetExtensions()) :
                    Utils.EnumerateFilesFromFolder(Source.FullName, beginFile, startWithBeginFile, GetExtensions());
            }

            if (Sources != null && Sources.Length > 1)
            {
                int index = Sources.IndexOf(beginFile);

                if (index == -1) index = 0;
                if (!startWithBeginFile) index = StdUtils.CycleIndex(index + 1, Sources.Length);

                return Sources.Skip(index).Concat(Sources.Take(index));
            }

            try
            {
                if (Sources == null || Sources.Length == 0) return new string[0];

                return Utils.EnumerateFilesFromFolder(GetDirectoryPath(Sources[0]),
                    beginFile, startWithBeginFile, GetExtensions());
            }
            catch
            {
                return new string[0];
            }
        }

        private IEnumerable<string> EnumerateFilesReverse(string beginFile)
        {
            if (Source != null)
            {
                return Source.SubType == SubfolderType.All ?
                    Utils.EnumerateAllFilesReverse(Source.FullName, beginFile, GetExtensions()) :
                    Utils.EnumerateFilesFromFolderReverse(Source.FullName, beginFile, GetExtensions());
            }

            if (Sources.Length > 1)
            {
                int index = Sources.IndexOf(beginFile);

                if (index == -1) index = 0;

                return Sources.Take(index + 1).Reverse().Concat(Sources.Skip(index + 1).Reverse());
            }

            try
            {
                if (Sources.Length == 0) return new string[0];

                return Utils.EnumerateFilesFromFolderReverse(GetDirectoryPath(Sources[0]), beginFile, GetExtensions());
            }
            catch
            {
                return new string[0];
            }
        }

        private static string GetDirectoryPath(string path)
        {
            path = Path.GetFullPath(path);

            if (File.Exists(path)) return GetParent(path);
            if (Directory.Exists(path)) return path;

            return GetParent(path);
        }

        private static string GetParent(string path)
        {
            return path.Remove(path.TrimEnd('\\').LastIndexOf('\\')) + '\\';
        }

        private void UpdateImagesFast(FileSystemImage oldCurrentImage, int offset)
        {
            switch (offset)
            {
                case 1:
                    if (NextImage?.Image != null) CurrentImage = NextImage;
                    PreviousImage = oldCurrentImage;
                    NextImage = null;
                    break;

                case 2:
                    PreviousImage = NextImage;
                    NextImage = null;
                    break;

                case -1:
                    if (PreviousImage?.Image != null) CurrentImage = PreviousImage;
                    NextImage = oldCurrentImage;
                    PreviousImage = null;
                    break;

                case -2:
                    NextImage = PreviousImage;
                    PreviousImage = null;
                    break;
            }
        }

        private FileSystemImage GetImage(string file)
        {
            if (file == null) return null;

            FileSystemImage image;
            if (!buffer.TryGetValue(file, out image)) image = new FileSystemImage(new FileInfo(file));

            buffer.Buffer(file, image);

            return image;
        }

        private FileSystemImage FindCurrentImage(FileSystemImage oldCurrentImage, int offset)
        {
            IEnumerable<string> files;

            if (offset == 0) files = EnumerateFiles(oldCurrentImage?.File?.FullName, true);
            else if (offset > 0) files = EnumerateFiles(oldCurrentImage?.File?.FullName, false);
            else
            {
                files = EnumerateFilesReverse(oldCurrentImage?.File?.FullName ?? string.Empty);
                offset = -offset - 1;
            }

            return GetImage(files, offset, oldCurrentImage?.File?.FullName);
        }

        private async Task UpdateImagesSlow()
        {
            string currentFilePath = CurrentImage?.File?.FullName ?? string.Empty;

            await UpdateImage(CurrentImage);

            // possible if UpdateImages() was called during previous await
            if (currentFilePath != CurrentImage?.File?.FullName)
            {
                PreviousImage = NextImage = null;
                return;
            }

            if (CurrentImage?.Image == null)
            {
                IEnumerable<string> currentFiles = lastUpdateOffset >= 0 ?
                    EnumerateFiles(currentFilePath, true) : EnumerateFilesReverse(currentFilePath);

                FileSystemImage currentImg = await GetImage(currentFiles, 0, currentFilePath, CurrentImage);

                if (CurrentImage?.Image == null || currentFilePath != CurrentImage?.File?.FullName)
                {
                    PreviousImage = NextImage = null;
                    return;
                }

                CurrentImage = currentImg;
                currentFilePath = CurrentImage?.File?.FullName;
            }

            IEnumerable<string> nextFiles = EnumerateFiles(currentFilePath, false);
            Task<FileSystemImage> nextImageTask = GetImage(nextFiles, 1, currentFilePath, NextImage);
            IEnumerable<string> previousFiles = EnumerateFilesReverse(currentFilePath);
            Task<FileSystemImage> previousImageTask = GetImage(previousFiles, 1, currentFilePath, PreviousImage);

            FileSystemImage nextImg = await nextImageTask;
            FileSystemImage previousImg = await previousImageTask;

            if (currentFilePath != CurrentImage?.File?.FullName)
            {
                PreviousImage = NextImage = null;
                return;
            }

            NextImage = nextImg;
            PreviousImage = previousImg;
        }

        private FileSystemImage GetImage(IEnumerable<string> files, int offset, string offsetFile)
        {
            using (IEnumerator<string> enumerator = files.GetEnumerator())
            {
                if (!enumerator.MoveNext()) return null;

                if (enumerator.Current != offsetFile) offset--;

                Queue<string> skippedFiles = new Queue<string>(Math.Max(offset, 1));

                do
                {
                    if (offset-- > 0)
                    {
                        skippedFiles.Enqueue(enumerator.Current);
                        continue;
                    }

                    return GetImage(enumerator.Current);

                } while (enumerator.MoveNext());

                while (skippedFiles.Count > 0)
                {
                    return GetImage(skippedFiles.Dequeue());
                }
            }

            return null;
        }

        private async Task<FileSystemImage> GetImage(IEnumerable<string> files,
            int offset, string offsetFile, FileSystemImage refImg)
        {
            using (AsyncEnumerator<string> enumerator = new AsyncEnumerator<string>(files))
            {
                if (!await enumerator.MoveNext()) return null;

                if (enumerator.Current != offsetFile) offset--;

                Queue<string> skippedFiles = new Queue<string>(Math.Max(offset, 1));

                do
                {
                    if (offset-- > 0)
                    {
                        skippedFiles.Enqueue(enumerator.Current);
                        continue;
                    }

                    FileSystemImage image = GetImage(enumerator.Current);
                    image = await GetUpdatedImage(image, refImg);

                    if (image?.Image != null) return image;

                } while (await enumerator.MoveNext());

                while (skippedFiles.Count > 0)
                {
                    FileSystemImage image = GetImage(skippedFiles.Dequeue());
                    image = await GetUpdatedImage(image, refImg);

                    if (image?.Image != null) return image;
                }
            }

            return null;
        }

        private static async Task<FileSystemImage> GetUpdatedImage(FileSystemImage newImage, FileSystemImage refImage)
        {
            if (newImage?.File?.FullName == refImage?.File?.FullName || refImage?.Image == null)
            {
                await UpdateImage(refImage);

                if (refImage?.Image != null) return refImage;
            }

            await UpdateImage(newImage);

            return newImage;
        }

        private static async Task UpdateImage(FileSystemImage image)
        {
            if (image == null) return;

            await ByteLoader.Current.Load(image);

            if (image.IsImageOutdated) image.LoadImage();
        }

        private static string GetTitle(FileSystemImage image)
        {
            return image?.File?.Name == null ? applicationName : (image.File.Name + " - " + applicationName);
        }

        private string[] GetExtensions() => Extensions.Split('|').Select(e => e.ToLower()).ToArray();

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
