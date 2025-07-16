using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PictureView.Converters;

public class FileSystemCollisionConverter : IValueConverter
{
    private object currentValue;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not FileSystemCollision fileCollision) return false;

        FileSystemCollision reference = Enum.Parse<FileSystemCollision>(parameter as string ?? string.Empty, true);
        if (reference != fileCollision) return false;

        currentValue = fileCollision;
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
        {
            currentValue = Enum.Parse<FileSystemCollision>(parameter as string ?? string.Empty, true);
        }

        return currentValue;
    }
}