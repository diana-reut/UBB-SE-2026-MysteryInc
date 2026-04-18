using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace HospitalManagement.Utils;

internal class StringVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string? selected = value as string;
        string? target = parameter as string;

        // If no parameter provided, show if selected has a value
        if (string.IsNullOrEmpty(target))
        {
            return !string.IsNullOrEmpty(selected) ? Visibility.Visible : Visibility.Collapsed;
        }

        // If parameter provided, show if selected contains or equals the parameter
        if (string.IsNullOrEmpty(selected))
        {
            return Visibility.Collapsed;
        }

        return target.Contains(selected, StringComparison.Ordinal) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new MyNotImplementedException();
    }
}

