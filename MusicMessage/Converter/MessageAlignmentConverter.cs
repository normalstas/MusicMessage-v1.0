using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using MusicMessage.Models;
namespace MusicMessage.Converter
{
	public class MessageAlignmentConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values.Length >= 2 && values[0] is int senderId && values[1] is int currentUserId)
			{
				return senderId == currentUserId ? HorizontalAlignment.Right : HorizontalAlignment.Left;
			}
			return HorizontalAlignment.Left;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
