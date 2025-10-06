using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace MusicMessage.Converter
{
	public class MessageStyleMultiConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Length < 2 || !(values[0] is int senderId) || !(values[1] is int currentUserId))
				return null;

			try
			{
				if (senderId == currentUserId)
				{
					return Application.Current.FindResource("MyMessageBorderStyle") as Style;
				}
				else
				{
					return Application.Current.FindResource("OtherMessageBorderStyle") as Style;
				}
			}
			catch
			{
				return CreateFallbackStyle(senderId == currentUserId);
			}
		}

		private Style CreateFallbackStyle(bool isMyMessage)
		{
			return new Style(typeof(Border))
			{
				Setters =
				{
					new Setter(Border.BackgroundProperty,
						isMyMessage ? Brushes.LightBlue : Brushes.LightGreen),
					new Setter(Border.CornerRadiusProperty,
						isMyMessage ? new CornerRadius(10, 10, 0, 10) : new CornerRadius(10, 10, 10, 0)),
					new Setter(Border.PaddingProperty, new Thickness(10)),
					new Setter(Border.MaxWidthProperty, 300.0)
				}
			};
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
