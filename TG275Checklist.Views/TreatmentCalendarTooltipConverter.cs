using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows;
using static TG275Checklist.Model.EsapiService;

namespace TG275Checklist.Views
{
    public class TreatmentCalendarTooltipConverter : IMultiValueConverter
    {
        // TODO: This runs for every check, even if the Visibility is not set to Visible
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] == null || values[1] == null || values[1] == DependencyProperty.UnsetValue)
                return "";

            var date = (DateTime)values[0];
            var dates = values[1] as List<TreatmentAppointmentInfo>;

            var tooltip = "";

            foreach (var apptDate in dates)
                if (apptDate.ApptTime.Date == date.Date)
                    if (tooltip == "")
                        tooltip = apptDate.ApptName;
                    else
                        tooltip += $"\n{apptDate.ApptName}";

            return tooltip;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
