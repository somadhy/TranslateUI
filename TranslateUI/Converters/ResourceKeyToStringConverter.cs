using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace TranslateUI.Converters;

public sealed class ResourceKeyToStringConverter : IValueConverter
{
    public static readonly ResourceKeyToStringConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string key && Application.Current is { } app)
        {
            if (app.Resources.TryGetResource(key, null, out var resource) && resource is string text)
            {
                return text;
            }
        }

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
