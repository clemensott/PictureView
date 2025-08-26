using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PictureView.EnumerateFiles;
using PictureView.Folders;
using PictureView.Models;
using StdOttStandard;
using StdOttStandard.Linq;

namespace PictureView.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private const int opacityStepDelay = 5;
    private const double opacityStep = 0.05;
    private const string applicationName = "Picview";

    private readonly ByteLoader byteLoader;
    private readonly SemaphoreSlim updateImagesSlowSem;
    private int updateCount, lastUpdateOffset;

    private bool isUpdatingImages,
        viewControls,
        isUpdatingOpacity,
        useSource,
        isFullscreen,
        isDeleteDirect;

    private double controlsOpacity;
    private string extensions;
    private string[] sources;
    private WindowState windowState;
    private readonly FiFoImagesBuffer buffer;
    private Rect? cropRect;
    private Thickness imgMargin;
    private FileSystemImage? currentImage, previousImage, nextImage;
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
            OnPropertyChanged(nameof(WindowState));
        }
    }

    public WindowState WindowState
    {
        get => IsFullscreen ? WindowState.FullScreen : windowState;
        set
        {
            if (value == windowState || IsFullscreen) return;

            windowState = value;
            OnPropertyChanged();
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

            UseSource = sources.Length <= 1;
            if (sources.Length != 1) UpdateImages();
            else UpdateImages(0, sources[0]);
        }
    }
    
    public ObservableCollection<ColorName> BackgroundColors { get; }

    public Thickness ImgMargin
    {
        get => imgMargin;
        set
        {
            if(value == imgMargin) return;
            
            imgMargin = value;
            OnPropertyChanged(nameof(ImgMargin));
        }
    } 

    public FileSystemImage? CurrentImage
    {
        get => currentImage;
        private set
        {
            if (value == currentImage) return;

            if (currentImage != null) currentImage.PropertyChanged -= CurrentImage_PropertyChanged;
            currentImage = value;
            if (currentImage != null) currentImage.PropertyChanged += CurrentImage_PropertyChanged;

            OnPropertyChanged(nameof(CurrentImage));
            OnPropertyChanged(nameof(ApplicationTitle));

            ResetZoom();
            CroppedImage.Source = value?.Image;
        }
    }

    public Rect? CropRect
    {
        get => cropRect;
        set
        {
            value = Limit(value, CurrentImage?.Image?.Size);

            if (value != cropRect)
            {
                cropRect = value;
                OnPropertyChanged(nameof(CropRect));
            }

            if (value != null)
            {
                CroppedImage.SourceRect =
                    new PixelRect((int)value?.X!, (int)value?.Y!, (int)value?.Width!, (int)value?.Height!);
            }
            else CroppedImage.SourceRect = new PixelRect();

            ForceRerender();
        }
    }

    public CroppedBitmap CroppedImage { get; }

    public FileSystemImage? PreviousImage
    {
        get => previousImage;
        private set
        {
            if (value == previousImage) return;

            previousImage = value;
            OnPropertyChanged(nameof(PreviousImage));
        }
    }

    public FileSystemImage? NextImage
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

            bool pathChanged = value.Path != source?.Path;
            source = value;
            OnPropertyChanged(nameof(Source));

            if (pathChanged) UpdateImages();
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

    public MainWindowViewModel()
    {
        byteLoader = new ByteLoader();
        updateImagesSlowSem = new SemaphoreSlim(1);
        buffer = new FiFoImagesBuffer(3, 15, 20_000_000);

        ViewControls = true;
        BackgroundColors = [
            new ColorName("White", Brushes.White),
            new ColorName("Black", Brushes.Black),
            new ColorName("Gray", Brushes.Gray),
            new ColorName("Blue", Brushes.Blue),
            new ColorName("Green", Brushes.Green),
            new ColorName("Red", Brushes.Red),
            new ColorName("Yellow", Brushes.Yellow),
            new ColorName("Orange", Brushes.Orange),
            new ColorName("Purple", Brushes.Purple),
        ];
        CopyCollision = FileSystemCollision.Ask;
        Extensions = ".jpg|.jpeg|.jpe|.gif|.tiff|.ico|.png|.bmp";
        Sources = Array.Empty<string>();
        Source = new Folder(string.Empty, SubfolderType.This);
        Destination = new Folder(string.Empty, SubfolderType.This);
        CroppedImage = new CroppedBitmap();
    }

    private void CurrentImage_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CurrentImage.Image))
        {
            CroppedImage.Source = CurrentImage?.Image;
            ForceRerender();
        }
    }

    private static Rect? Limit(Rect? rect, Size? maxSize)
    {
        if (!rect.HasValue) return null;

        Rect r = rect.Value;
        double maxW = maxSize?.Width ?? 0;
        double maxH = maxSize?.Height ?? 0;

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

    private void ForceRerender()
    {
        // force image to recalculate possible size
        ImgMargin = ImgMargin.Left > 0 ? new Thickness() : new Thickness(0.1);
    }

    public void ResetZoom()
    {
        CropRect = null;
    }

    public void UpdateImages(int offset = 0)
    {
        UpdateImages(offset, null);
    }

    private async void UpdateImages(int offset, string? path)
    {
        lastUpdateOffset = offset;

        if (isUpdatingImages) return;
        isUpdatingImages = true;

        try
        {
            FileSystemImage? oldCurrentImage;

            if (path == null)
            {
                oldCurrentImage = CurrentImage;

                UpdateImagesFast(oldCurrentImage, offset);
            }
            else
            {
                string dirPath = GetDirectoryPath(path);

                Source = Source with { Path = dirPath };
                UseSource = true;
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

    private IEnumerable<string> EnumerateFiles(string? beginFile, bool startWithBeginFile)
    {
        if (UseSource)
        {
            return Source.SubType == SubfolderType.All
                ? FolderSubForward.Get(Source.Path, beginFile, startWithBeginFile, GetExtensions())
                : FolderOnly.GetForward(Source.Path, beginFile, startWithBeginFile, GetExtensions());
        }

        string[] sources = Sources;
        if (sources.Length == 0) return Array.Empty<string>();
        if (sources.Length > 1)
        {
            int index = sources.IndexOf(beginFile);

            if (index == -1) index = 0;
            if (!startWithBeginFile) index = StdUtils.CycleIndex(index + 1, sources.Length);

            return sources.Skip(index).Concat(sources.Take(index));
        }

        try
        {
            return FolderOnly.GetForward(GetDirectoryPath(sources[0]),
                beginFile, startWithBeginFile, GetExtensions());
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private IEnumerable<string> EnumerateFilesReverse(string? beginFile)
    {
        if (UseSource)
        {
            return Source.SubType == SubfolderType.All
                ? FolderSubBackward.Get(Source.Path, beginFile, GetExtensions())
                : FolderOnly.GetBackwards(Source.Path, beginFile, GetExtensions());
        }

        string[] sources = Sources;
        if (sources.Length == 0) return Array.Empty<string>();
        if (sources.Length > 1)
        {
            int index = sources.IndexOf(beginFile);
            if (index == -1) index = 0;

            return sources.Take(index).Reverse().Concat(sources.Skip(index).Reverse());
        }

        try
        {
            return FolderOnly.GetBackwards(GetDirectoryPath(sources[0]), beginFile, GetExtensions());
        }
        catch
        {
            return Array.Empty<string>();
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
        path = Helper.UniformDirectorySeparatorChar(path);
        return path.Remove(path.TrimEnd(Path.DirectorySeparatorChar).LastIndexOf(Path.DirectorySeparatorChar)) +
               Path.DirectorySeparatorChar;
    }

    private void UpdateImagesFast(FileSystemImage? oldCurrentImage, int offset)
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
        if (!buffer.TryGetValue(file, out var image)) image = new FileSystemImage(new FileInfo(file));

        buffer.Buffer(file, image);

        return image;
    }

    private FileSystemImage? FindCurrentImage(FileSystemImage? oldCurrentImage, int offset)
    {
        IEnumerable<string> files;

        if (offset == 0) files = EnumerateFiles(oldCurrentImage?.File.FullName, true);
        else if (offset > 0) files = EnumerateFiles(oldCurrentImage?.File.FullName, false);
        else
        {
            files = EnumerateFilesReverse(oldCurrentImage?.File.FullName ?? string.Empty);
            offset = -offset - 1;
        }

        return GetImage(files, offset, oldCurrentImage?.File.FullName);
    }

    private async Task UpdateImagesSlow()
    {
        string? currentFilePath = CurrentImage?.File?.FullName ?? string.Empty;

        await UpdateImage(CurrentImage);

        // possible if UpdateImages() was called during previous await
        if (currentFilePath != CurrentImage?.File?.FullName)
        {
            PreviousImage = NextImage = null;
            return;
        }

        if (CurrentImage?.Image == null)
        {
            IEnumerable<string> currentFiles = lastUpdateOffset >= 0
                ? EnumerateFiles(currentFilePath, true)
                : EnumerateFilesReverse(currentFilePath);

            FileSystemImage? currentImg = await GetImage(currentFiles, 0, currentFilePath, CurrentImage);

            if (CurrentImage?.Image == null || currentFilePath != CurrentImage?.File.FullName)
            {
                PreviousImage = NextImage = null;
                return;
            }

            CurrentImage = currentImg;
            currentFilePath = CurrentImage?.File.FullName;
        }

        IEnumerable<string> nextFiles = EnumerateFiles(currentFilePath, false);
        Task<FileSystemImage?> nextImageTask = GetImage(nextFiles, 1, currentFilePath, NextImage);
        IEnumerable<string> previousFiles = EnumerateFilesReverse(currentFilePath);
        Task<FileSystemImage?> previousImageTask = GetImage(previousFiles, 1, currentFilePath, PreviousImage);

        FileSystemImage? nextImg = await nextImageTask;
        FileSystemImage? previousImg = await previousImageTask;

        if (currentFilePath != CurrentImage?.File.FullName)
        {
            PreviousImage = NextImage = null;
            return;
        }

        NextImage = nextImg;
        PreviousImage = previousImg;
    }

    private FileSystemImage? GetImage(IEnumerable<string> files, int offset, string? offsetFile)
    {
        using IEnumerator<string> enumerator = files.GetEnumerator();
        if (!enumerator.MoveNext()) return null;

        if (enumerator.Current != offsetFile) offset--;

        Queue<string> skippedFiles = new Queue<string>(Math.Max(offset, 1));

        do
        {
            if (offset-- <= 0) return GetImage(enumerator.Current);

            skippedFiles.Enqueue(enumerator.Current);
        } while (enumerator.MoveNext());

        while (skippedFiles.Count > 0)
        {
            return GetImage(skippedFiles.Dequeue());
        }

        return null;
    }

    private async Task<FileSystemImage?> GetImage(IEnumerable<string> files,
        int offset, string? offsetFile, FileSystemImage? refImg)
    {
        using AsyncEnumerator<string> enumerator = new AsyncEnumerator<string>(files);
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

            FileSystemImage? image = GetImage(enumerator.Current);
            image = await GetUpdatedImage(image, refImg);

            if (image?.Image != null) return image;
        } while (await enumerator.MoveNext());

        while (skippedFiles.Count > 0)
        {
            FileSystemImage? image = GetImage(skippedFiles.Dequeue());
            image = await GetUpdatedImage(image, refImg);

            if (image?.Image != null) return image;
        }

        return null;
    }

    private async Task<FileSystemImage?> GetUpdatedImage(FileSystemImage? newImage, FileSystemImage? refImage)
    {
        if (newImage?.File?.FullName == refImage?.File.FullName || refImage?.Image == null)
        {
            await UpdateImage(refImage);

            if (refImage?.Image != null) return refImage;
        }

        await UpdateImage(newImage);

        return newImage;
    }

    private async Task UpdateImage(FileSystemImage? image)
    {
        if (image == null) return;

        await byteLoader.Load(image);

        if (image.IsImageOutdated) image.LoadImage();
    }

    private static string GetTitle(FileSystemImage? image)
    {
        return image?.File.Name == null ? applicationName : $"{image.File.Name} - {applicationName}";
    }

    private string[] GetExtensions() => Extensions.Split('|').Select(e => e.ToLower()).ToArray();
}