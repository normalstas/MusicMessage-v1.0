using MusicMessage.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MusicMessage.Repository;
namespace MusicMessage.Converter
{
	public class IsOwnPostConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is int authorId)
			{
				
				try
				{
					var authService = App.ServiceProvider?.GetService<IAuthService>();
					if (authService?.CurrentUser != null)
					{
						return authorId == authService.CurrentUser.UserId ?
							Visibility.Visible : Visibility.Collapsed;
					}
				}
				catch
				{
					return Visibility.Collapsed;
				}
			}
			return Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
