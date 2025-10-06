using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MusicMessage.Converter
{
	public class TimeAgoConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is DateTime dateTime)
			{
				var timeSpan = DateTime.Now - dateTime;

				if (timeSpan.TotalMinutes < 1)
					return "только что";
				if (timeSpan.TotalMinutes < 60)
					return $"{(int)timeSpan.TotalMinutes} мин назад";
				if (timeSpan.TotalHours < 24)
					return $"{(int)timeSpan.TotalHours} ч назад";
				if (timeSpan.TotalDays < 7)
					return $"{(int)timeSpan.TotalDays} дн назад";

				return dateTime.ToString("dd.MM.yyyy");
			}
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
