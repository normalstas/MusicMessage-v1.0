using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;
namespace MusicMessage.Converter
{
	public class AvatarPathToImageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string avatarPath && !string.IsNullOrEmpty(avatarPath))
			{
				try
				{
					// Проверяем разные возможные пути
					var possiblePaths = new[]
					{
						Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Avatars", avatarPath),
						Path.Combine(AppDomain.CurrentDomain.BaseDirectory, avatarPath),
						avatarPath
					};

					foreach (var fullPath in possiblePaths)
					{
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
				}
				catch
				{
					// В случае ошибки возвращаем заглушку
				}
			}

			// Возвращаем заглушку если аватар не найден
			return CreateDefaultAvatarImage();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		private BitmapImage CreateDefaultAvatarImage()
		{
			try
			{
				var bitmap = new BitmapImage();
				bitmap.BeginInit();
				bitmap.UriSource = new Uri("");
				bitmap.CacheOption = BitmapCacheOption.OnLoad;
				bitmap.EndInit();
				bitmap.Freeze();
				return bitmap;
			}
			catch
			{
				// Если ресурс не найден, создаем пустое изображение
				return null;
			}
		}
	}
}
