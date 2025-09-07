using MusicMessage.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace MusicMessage.Converter
{
	public class FriendshipStatusToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (parameter == null) return Visibility.Collapsed;

			string param = parameter.ToString();

			// Для случая когда статус должен быть null
			if (param == "Null")
			{
				return value == null ? Visibility.Visible : Visibility.Collapsed;
			}

			// Для всех остальных случаев
			return value?.ToString() == param ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
