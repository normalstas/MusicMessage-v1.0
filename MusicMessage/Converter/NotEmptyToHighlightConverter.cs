using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace MusicMessage.Converter
{
	public class NotEmptyToHighlightConverter : IValueConverter
	{
		public static NotEmptyToHighlightConverter Default { get; } = new NotEmptyToHighlightConverter();

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string text && !string.IsNullOrWhiteSpace(text))
			{
				return new SolidColorBrush(Color.FromArgb(30, 25, 118, 210));
			}
			return Brushes.Transparent;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
