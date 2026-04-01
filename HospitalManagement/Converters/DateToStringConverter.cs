using Microsoft.UI.Xaml.Data;
using System;

namespace HospitalManagement.Converters
{
    public class DateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime date)
            {
                return date.ToString("yyyy-MM-dd");
            }
            return ""; // Returns empty string if date is null/invalid
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}