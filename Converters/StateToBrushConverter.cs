﻿using TheCreationist.App.Composite;
using TheCreationist.App.ViewModel;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TheCreationist.App.Converters
{
    [ValueConversion(typeof(ProjectViewModel), typeof(Brush))]
    public class StateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ProjectState state = (ProjectState)value;

            if (state.IsSaved == false && state.IsDirty == false)
            {
                return Brushes.Red;
            }
            else if (state.IsSaved == true && state.IsDirty == false)
            {
                return Brushes.Lime;
            }
            else if (state.IsSaved == false && state.IsDirty == true)
            {
                return Brushes.Red;
            }
            else if (state.IsSaved == true && state.IsDirty == true)
            {
                return Brushes.Orange;
            }
            else
            {
                return Brushes.Black;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}