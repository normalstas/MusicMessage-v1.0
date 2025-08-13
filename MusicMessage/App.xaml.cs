using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MusicMessage.Models;
using MusicMessage.ViewModels;
using System.Configuration;
using System.Data;
using System.Windows;
namespace MusicMessage
{
    public partial class App : Application
    {
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			var services = new ServiceCollection();

			services.AddDbContext<MessangerBaseContext>(options =>
				options.UseSqlServer("Ваша_строка_подключения"));

			services.AddScoped<IMessageRepository, MessageRepository>();
			services.AddTransient<ChatViewModel>();
			services.AddSingleton<NavigationView>();
			var provider = services.BuildServiceProvider();

			var mainWindow = new MainWindow
			{
				DataContext = provider.GetRequiredService<NavigationView>()
			};
			mainWindow.Show();
		}
	}

}
