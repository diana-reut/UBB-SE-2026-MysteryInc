using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;

namespace HospitalManagement.Converters
{
    public class DeceasedToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isDeceased = value is bool && (bool)value;

            // If deceased, return Red. If alive, return Black (or the theme's default).
            return isDeceased ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => null;
    }
}