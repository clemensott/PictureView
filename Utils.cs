using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Directory = MetadataExtractor.Directory;

namespace PictureView;

public static class Utils
{
    public static IImage LoadBitmap(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        Bitmap bitmap = new Bitmap(stream);

        stream.Seek(0, SeekOrigin.Begin);
        IReadOnlyList<Directory> directories = ImageMetadataReader.ReadMetadata(stream);
        int orientation = directories
            .OfType<ExifIfd0Directory>()
            .FirstOrDefault()?.GetUInt16(ExifDirectoryBase.TagOrientation) ?? 1;

        return ApplyOrientation(bitmap, orientation);
    }

    private static Bitmap ApplyOrientation(Bitmap source, int orientation)
    {
        // no transformation needed
        if (orientation == 1) 
            return source;

        // calculate target size
        PixelSize size = orientation is 6 or 8
            ? new PixelSize(source.PixelSize.Height, source.PixelSize.Width)
            : source.PixelSize;

        RenderTargetBitmap target = new RenderTargetBitmap(size, source.Dpi);

        using DrawingContext ctx = target.CreateDrawingContext();
        Matrix transform = orientation switch
        {
            2 => Matrix.CreateScale(-1, 1) *
                 Matrix.CreateTranslation(size.Width, 0),             // Flip H
            3 => Matrix.CreateRotation(Math.PI) *
                 Matrix.CreateTranslation(size.Width, size.Height),   // 180°
            4 => Matrix.CreateScale(1, -1) *
                 Matrix.CreateTranslation(0, size.Height),            // Flip V
            5 => Matrix.CreateRotation(Math.PI / 2) *
                 Matrix.CreateScale(-1, 1),                                     // 90° CW + Flip H
            6 => Matrix.CreateRotation(Math.PI / 2) *
                 Matrix.CreateTranslation(size.Width, 0),             // 90° CW
            7 => Matrix.CreateRotation(3 * Math.PI / 2) *
                 Matrix.CreateScale(-1, 1) *
                 Matrix.CreateTranslation(size.Width, size.Height),   // 270° CW + Flip H
            8 => Matrix.CreateRotation(3 * Math.PI / 2) *
                 Matrix.CreateTranslation(0, size.Height),            // 270° CW
            _ => Matrix.Identity
        };

        ctx.PushTransform(transform);
        ctx.DrawImage(source, new Rect(source.Size));

        return target;
    }
}