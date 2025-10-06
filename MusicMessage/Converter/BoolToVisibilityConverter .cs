using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Collections;

namespace MusicMessage.Converter
{
	public class BoolToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) return Visibility.Collapsed;

			bool isVisible;

			switch (value)
			{
				case bool boolValue:
					isVisible = boolValue;
					break;
				case int intValue:
					isVisible = intValue != 0;
					break;
				case string stringValue:
					isVisible = !string.IsNullOrEmpty(stringValue);
					break;
				case ICollection collection:
					isVisible = collection?.Count > 0;
					break;
				case IEnumerable enumerable:
					isVisible = enumerable?.GetEnumerator().MoveNext() == true;
					break;
				default:
					isVisible = value != null;
					break;
			}

			bool invert = parameter?.ToString() == "Invert";
			if (invert) isVisible = !isVisible;

			return isVisible ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
