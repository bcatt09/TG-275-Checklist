using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace TG275Checklist.Views
{
    public class HighlightDayConverter : IMultiValueConverter
    {
        // TODO: This runs for every check, even if the Visibility is not set to Visible
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var date = (DateTime)values[0];
            var dates = values[1] as List<DateTime>;

            if(date != null && dates != null)
                foreach (var apptDate in dates)
                    if (apptDate.Date == date.Date)
                        return true;

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
