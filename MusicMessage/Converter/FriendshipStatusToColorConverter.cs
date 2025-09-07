using MusicMessage.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Windows.Data;
using System.Windows.Media;
namespace MusicMessage.Converter
{
	public class FriendshipStatusToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is FriendshipStatus status)
			{
				return status switch
				{
					FriendshipStatus.Pending => Brushes.Orange,
					FriendshipStatus.Accepted => Brushes.Green,
					FriendshipStatus.Rejected => Brushes.Red,
					FriendshipStatus.Blocked => Brushes.DarkRed,
					_ => Brushes.Gray
				};
			}
			return Brushes.Gray;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
