using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PictureView.Converters;

public class RoundNumberConverter : IValueConverter
{
    public int Decimals { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return null;

        double number = System.Convert.ToDouble(value);
        return Math.Round(number, Decimals);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}