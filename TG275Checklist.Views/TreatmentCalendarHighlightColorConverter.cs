using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows;
using static TG275Checklist.Model.HelperDataTypes;
using System.Linq;

namespace TG275Checklist.Views
{
    public class TreatmentCalendarHighlightColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] == null || values[1] == null || values[1] == DependencyProperty.UnsetValue)
                return "";

            var date = (DateTime)values[0];
            var appts = values[1] as List<TreatmentAppointmentInfo>;

            var brushConverter = new System.Windows.Media.BrushConverter();

            foreach (var apptDate in appts)
            {
                var otherNonWhiteAppts = appts.Where(x => x.ApptTime.Date == apptDate.ApptTime.Date && x.ApptColor != "#FFFFFF");

                if (apptDate.ApptTime.Date == date.Date)
                {
                    if (otherNonWhiteAppts.Any())
                        return brushConverter.ConvertFrom(otherNonWhiteAppts.First().ApptColor);
                    else
                        return brushConverter.ConvertFrom(apptDate.ApptColor);
                }
            }

            return "";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
