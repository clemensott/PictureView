using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PictureView;

public class FileSystemImage : ObservableObject, IDisposable
{
    private bool isImageOutdated, isImageLoaded;
    private MemoryStream? stream;
    private IImage? image;

    public FileInfo File { get; }

    public bool IsImageOutdated
    {
        get => isImageOutdated;
        private set
        {
            if (value == isImageOutdated) return;

            isImageOutdated = value;
            OnPropertyChanged();
        }
    }

    public bool IsImageLoaded
    {
        get => isImageLoaded;
        private set
        {
            if (value == isImageLoaded) return;

            isImageLoaded = value;
            OnPropertyChanged();
        }
    }

    public long? DataSize => stream?.Length;

    public IImage? Image
    {
        get => image;
        private set
        {
            if (value == image) return;

            image = value;
            OnPropertyChanged();
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
                MemoryStream destStream = new MemoryStream();
                await using Stream srcStream = System.IO.File.OpenRead(File.FullName);
                await srcStream.CopyToAsync(destStream);

                Stream? oldStream = stream;
                stream = destStream;

                IsImageOutdated = oldStream == null || !SequenceEqual(oldStream, destStream);
                await (oldStream?.DisposeAsync() ?? ValueTask.CompletedTask);
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

    public byte[]? GetImageBytes()
    {
        return stream?.ToArray();
    }

    public void Dispose()
    {
        stream?.Dispose();
        stream = null;
        Image = null;
    }
}