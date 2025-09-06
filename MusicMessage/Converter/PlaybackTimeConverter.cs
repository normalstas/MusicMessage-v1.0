using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MusicMessage.Converter
{
	public class PlaybackTimeConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values[0] is TimeSpan duration && values[1] is string currentTime)
			{
				return !string.IsNullOrEmpty(currentTime) ? currentTime : duration.ToString("mm\\:ss");
			}
			return values[0] is TimeSpan ts ? ts.ToString("mm\\:ss") : "00:00";
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
