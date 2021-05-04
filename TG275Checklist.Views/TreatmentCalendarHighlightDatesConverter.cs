using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using static TG275Checklist.Model.EsapiService;

namespace TG275Checklist.Views
{
    public class TreatmentCalendarHighlightDatesConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] == null || values[1] == null || values[1] == DependencyProperty.UnsetValue)
                return null;

            var date = (DateTime)values[0];
            var appts = values[1] as List<TreatmentAppointmentInfo>;

            foreach (var apptDate in appts)
                if (apptDate.ApptTime.Date == date.Date)
                    return true;

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
