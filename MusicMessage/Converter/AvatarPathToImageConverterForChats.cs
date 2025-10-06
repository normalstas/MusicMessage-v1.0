using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.IO;
using System.Windows.Media.Imaging;

namespace MusicMessage.Converter
{
	public class AvatarPathToImageConverterForChats : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string avatarPath && !string.IsNullOrEmpty(avatarPath))
			{
				try
				{
					var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, avatarPath);
					if (File.Exists(fullPath))
					{
						var bitmap = new BitmapImage();
						bitmap.BeginInit();
						bitmap.UriSource = new Uri(fullPath);
						bitmap.CacheOption = BitmapCacheOption.OnLoad;
						bitmap.EndInit();
						bitmap.Freeze();
						return bitmap;
					}
				}
				catch
				{
					
				}
			}

			return new BitmapImage(new Uri("pack://application:,,,/Assets/default-avatar.png"));
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
