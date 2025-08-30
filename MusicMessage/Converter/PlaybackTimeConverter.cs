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
			if (values.Length >= 2)
			{
				var currentTime = values[1] as string;
				var duration = values[0] as TimeSpan?;

				// Если есть текущее время воспроизведения, показываем его
				if (!string.IsNullOrEmpty(currentTime))
					return currentTime;

				// Иначе показываем общую длительность
				if (duration.HasValue)
					return duration.Value.ToString("mm\\:ss");
			}
			return "00:00";
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
