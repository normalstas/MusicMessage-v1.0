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
	public class PostImageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string imagePath && !string.IsNullOrEmpty(imagePath))
			{
				try
				{
				
					var possiblePaths = new[]
					{
						Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Posts", imagePath),
						Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath),
						imagePath
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
					
				}
			}

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
