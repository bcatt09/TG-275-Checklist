using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TG275Checklist.Views
{
    public class TreatmentCalendarHighlightConverter : IMultiValueConverter
    {
        // TODO: This runs for every check, even if the Visibility is not set to Visible
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values[0] == null || values[1] == null || values[1] == DependencyProperty.UnsetValue)
                    return null;

                var date = (DateTime)values[0];
                var dates = values[1] as List<DateTime>;

                foreach (var apptDate in dates)
                    if (apptDate.Date == date.Date)
                        return true;

                return false;
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show($"{values[0]}\n{values[1]}\n{e.Message}");
                return false;
            }

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
