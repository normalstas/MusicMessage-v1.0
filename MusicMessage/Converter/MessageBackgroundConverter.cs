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
	public class MessageBackgroundConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			// values[0] - SenderId, values[1] - CurrentUserId
			if (values.Length == 2 && values[0] is int senderId && values[1] is int currentUserId)
			{
				return senderId == currentUserId
					? Brushes.LightBlue
					: Brushes.LightGreen;
			}
			return Brushes.Transparent;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
