using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace MusicMessage.Converter
{
	public class MultiBoolToVisibilityConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values == null || values.Length == 0)
				return Visibility.Collapsed;

			bool show = true;
			foreach (var value in values)
			{
				if (value is bool boolValue)
				{
					show &= boolValue;
				}
				else
				{
					show = false;
					break;
				}
			}

			bool invert = parameter?.ToString() == "Invert";
			if (invert) show = !show;

			return show ? Visibility.Visible : Visibility.Collapsed;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
