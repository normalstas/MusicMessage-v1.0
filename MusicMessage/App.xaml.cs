using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MusicMessage.Models;
using MusicMessage.Repository;
using MusicMessage.UserCtrls;
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

			var navigationViewModel = ServiceProvider.GetService<NavigationViewModel>(); 

			var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
			mainWindow.DataContext = navigationViewModel;
			mainWindow.Show();
		}

		private void ConfigureServices(IServiceCollection services)
		{
			services.AddDbContextFactory<MessangerBaseContext>(options =>
				options.UseSqlServer("Server=localhost; Database=MessangerBase; Trusted_Connection=True; MultipleActiveResultSets=true; TrustServerCertificate=true;encrypt=false"),
				ServiceLifetime.Transient);

			services.AddSingleton<IAuthService, AuthService>();

			// Репозитории
			services.AddTransient<IMessageRepository, MessageRepository>();
			services.AddTransient<IChatRepository, ChatRepository>();
			services.AddTransient<IFriendsRepository, FriendsRepository>();
			services.AddTransient<IProfileRepository, ProfileRepository>();
			services.AddTransient<IPostRepository, PostRepository>();

			// ViewModels
			services.AddScoped<FriendsViewModel>();
			services.AddScoped<LoginViewModel>();
			services.AddScoped<NavigationViewModel>();
			services.AddScoped<ChatsListViewModel>();
			services.AddScoped<ChatViewModel>();
			services.AddScoped<ProfileViewModel>();
			services.AddScoped<EditProfileViewModel>();
			services.AddScoped<NewsFeedViewModel>();
			services.AddScoped<PostDetailsViewModel>();
			// Views
			services.AddTransient<ProfileView>();
			services.AddTransient<PostDetailsView>();
			// MainWindow
			services.AddScoped<MainWindow>();
		}
	}

}
