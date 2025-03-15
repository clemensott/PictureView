using System;
using System.Globalization;
using Avalonia.Data.Converters;
using StdOttStandard;

namespace PictureView.Converters;

public class FileSizeFormatConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long byteCount) return StdUtils.GetFormattedFileSize(byteCount);
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}