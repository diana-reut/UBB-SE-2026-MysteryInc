using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace HospitalManagement.Converters
{
    // This class translates "Is the object null?" into "Should I show the UI?"
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // If value is NOT null, show the element. If it IS null, hide it.
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}