using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Data;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.Kernel.Sketches;

namespace HospitalManagement.Utils
{
    public class AxisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Assumes value is Axis[] and needs to be IEnumerable<ICartesianAxis>
            if (value is Axis[] axes)
            {
                // Axis implements ICartesianAxis, so cast is safe
                return axes as IEnumerable<ICartesianAxis>;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}