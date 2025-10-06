using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MusicMessage.Converter
{
	public class DateTimeToLocalConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is DateTime dateTime)
			{
				if (dateTime.Kind == DateTimeKind.Utc)
				{
					return dateTime.ToLocalTime();
				}
				else if (dateTime.Kind == DateTimeKind.Local)
				{
					return dateTime;
				}
				else
				{
					return dateTime;
				}
			}
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
