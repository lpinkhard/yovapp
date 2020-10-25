using System;
using System.Globalization;
using Xamarin.Forms;

namespace YoV.Helpers
{
    public class RosterFontAttribute : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = (bool) value;
            if (boolValue)
                return FontAttributes.Bold;
            return FontAttributes.None;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            FontAttributes faValue = (FontAttributes)value;
            if (faValue == FontAttributes.Bold)
                return true;
            return false;
        }
    }
}
