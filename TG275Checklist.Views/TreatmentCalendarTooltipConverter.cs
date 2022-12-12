using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows;
using static TG275Checklist.Model.HelperDataTypes;

namespace TG275Checklist.Views
{
    public class TreatmentCalendarTooltipConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] == null || values[1] == null || values[1] == DependencyProperty.UnsetValue)
                return "";

            var date = (DateTime)values[0];
            var appts = values[1] as List<TreatmentAppointmentInfo>;

            var tooltip = "";

            foreach (var apptDate in appts)
                if (apptDate.ApptTime.Date == date.Date)
                {
                    var text = $"{apptDate.ApptName} - {apptDate.ApptTime.ToShortTimeString()}\n{apptDate.ApptResource}";

                    if (tooltip == "")
                        tooltip = text;
                    else
                        tooltip += $"\n{text}";
                }

            return tooltip;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
