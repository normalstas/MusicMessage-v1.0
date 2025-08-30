using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MusicMessage.Models;
using MusicMessage.Repository;
using MusicMessage.ViewModels;
using System;
using System.Configuration;
using System.Data;
using System.Windows;
namespace MusicMessage
{
	public partial class App : Application
    {
		public static IServiceProvider ServiceProvider { get; private set; }
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			var serviceCollection = new ServiceCollection();
			ConfigureServices(serviceCollection);

			ServiceProvider = serviceCollection.BuildServiceProvider();

			var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
			mainWindow.Show();
		}

		private void ConfigureServices(IServiceCollection services)
		{
			services.AddDbContext<MessangerBaseContext>();
			services.AddSingleton<IAuthService, AuthService>();
			services.AddScoped<IMessageRepository, MessageRepository>();

			services.AddScoped<LoginViewModel>();
			services.AddScoped<NavigationViewModel>();
			services.AddScoped<Func<ChatViewModel>>(sp => () => sp.GetRequiredService<ChatViewModel>());
			services.AddScoped<ChatViewModel>();

			services.AddScoped<MainWindow>();
		}
	}

}
