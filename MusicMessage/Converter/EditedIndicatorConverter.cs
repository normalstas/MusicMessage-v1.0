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
	public class EditedIndicatorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			// Этот конвертер будет показывать иконку редактирования
			// В реальной реализации вы можете добавить флаг "IsEdited" в модель Message
			return Visibility.Collapsed; // По умолчанию скрываем
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
