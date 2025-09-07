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
			var friendsViewModel = ServiceProvider.GetService<FriendsViewModel>();
			var navigationViewModel = new NavigationViewModel(
				ServiceProvider.GetService<IAuthService>(),
				ServiceProvider.GetService<LoginViewModel>(),
				ServiceProvider,
				friendsViewModel
			);
			var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
			mainWindow.Show();
		}

		private void ConfigureServices(IServiceCollection services)
		{
			
			services.AddDbContextFactory<MessangerBaseContext>(options =>
	   options.UseSqlServer("Server=localhost; Database=MessangerBase; Trusted_Connection=True; MultipleActiveResultSets=true; TrustServerCertificate=true;encrypt=false"),
	   ServiceLifetime.Transient);

			services.AddSingleton<IAuthService, AuthService>();
			// Измените репозитории на Transient
			services.AddTransient<IMessageRepository, MessageRepository>();
			services.AddTransient<IChatRepository, ChatRepository>();
			services.AddTransient<IFriendsRepository, FriendsRepository>();
			services.AddScoped<FriendsViewModel>();
			services.AddScoped<LoginViewModel>();
			services.AddScoped<NavigationViewModel>();
			services.AddScoped<ChatsListViewModel>();
			services.AddScoped<ChatViewModel>();

			services.AddScoped<MainWindow>();
		}
	}

}
