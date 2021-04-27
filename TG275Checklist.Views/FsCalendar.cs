using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TG275Checklist.Views
{
    /// <summary>
    /// Custom calendar control that supports date highlighting.
    /// </summary>
    public class FsCalendar : Calendar
    {
        #region Dependency Properties

        // The background brush used for the date highlight.
        public static DependencyProperty DateHighlightBrushProperty = DependencyProperty.Register
             (
                  "DateHighlightBrush",
                  typeof(Brush),
                  typeof(FsCalendar),
                  new PropertyMetadata(new SolidColorBrush(Colors.Red))
             );

        // The list of dates to be highlighted.
        public static DependencyProperty HighlightedDateTextProperty = DependencyProperty.Register
            (
                "HighlightedDateText",
                typeof(String[]),
                typeof(FsCalendar),
                new PropertyMetadata()
            );

        // Whether highlights should be shown.
        public static DependencyProperty ShowDateHighlightingProperty = DependencyProperty.Register
             (
                  "ShowDateHighlighting",
                  typeof(bool),
                  typeof(FsCalendar),
                  new PropertyMetadata(true)
             );

        // Whether tool tips should be shown with highlights.
        public static DependencyProperty ShowHighlightedDateTextProperty = DependencyProperty.Register
             (
                  "ShowHighlightedDateText",
                  typeof(bool),
                  typeof(FsCalendar),
                  new PropertyMetadata(true)
             );

        #endregion

        #region Constructors

        /// <summary>
        /// Static constructor.
        /// </summary>
        static FsCalendar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FsCalendar),
                 new FrameworkPropertyMetadata(typeof(FsCalendar)));
        }

        /// <summary>
        /// Instance constructor.
        /// </summary>
        public FsCalendar()
        {
            /* We initialize the HighlightedDateText property to an array of 31 strings,
			 * since 31 is the maximum number of days in any month. */

            // Initialize HighlightedDateText property
            this.HighlightedDateText = new string[31];
        }

        #endregion

        #region CLR Properties

        /// <summary>
        /// The background brush used for the date highlight.
        /// </summary>
        [Browsable(true)]
        [Category("Highlighting")]
        public Brush DateHighlightBrush
        {
            get { return (Brush)GetValue(DateHighlightBrushProperty); }
            set { SetValue(DateHighlightBrushProperty, value); }
        }

        /// <summary>
        /// The tool tips for highlighted dates.
        /// </summary>
        [Browsable(true)]
        [Category("Highlighting")]
        public String[] HighlightedDateText
        {
            get { return (String[])GetValue(HighlightedDateTextProperty); }
            set { SetValue(HighlightedDateTextProperty, value); }
        }

        /// <summary>
        /// Whether highlights should be shown.
        /// </summary>
        [Browsable(true)]
        [Category("Highlighting")]
        public bool ShowDateHighlighting
        {
            get { return (bool)GetValue(ShowDateHighlightingProperty); }
            set { SetValue(ShowDateHighlightingProperty, value); }
        }

        /// <summary>
        /// Whether tool tips should be shown with highlights.
        /// </summary>
        [Browsable(true)]
        [Category("Highlighting")]
        public bool ShowHighlightedDateText
        {
            get { return (bool)GetValue(ShowHighlightedDateTextProperty); }
            set { SetValue(ShowHighlightedDateTextProperty, value); }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refreshes the calendar highlighting
        /// </summary>
        public void Refresh()
        {
            var realDisplayDate = this.DisplayDate;
            this.DisplayDate = DateTime.MinValue;
            this.DisplayDate = realDisplayDate;
        }

        #endregion
    }
}