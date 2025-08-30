using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicMessage.Models;
using MusicMessage.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MusicMessage.ViewModels
{
	public partial class NavigationViewModel : ObservableObject
	{
		[ObservableProperty]
		private object _currentView;

		private readonly IAuthService _authService;
		private readonly Func<ChatViewModel> _createChatViewModel;
		private readonly LoginViewModel _loginViewModel;

		public ICommand ShowChatCommand { get; }

		public NavigationViewModel(IAuthService authService, LoginViewModel loginViewModel, Func<ChatViewModel> createChatViewModel)
		{
			_authService = authService;
			_loginViewModel = loginViewModel;
			_createChatViewModel = createChatViewModel;

			// Подписываемся на событие успешного входа
			_loginViewModel.OnLoginSuccessful += ShowChats;

			CurrentView = _loginViewModel;
			ShowChatCommand = new RelayCommand(ShowChats);
		}

		public async void ShowChats()
		{
			try
			{
				if (_authService.IsLoggedIn)
				{
					// Создаем ChatViewModel асинхронно
					var chatVM = _createChatViewModel();

					// Даем время на инициализацию
					await Task.Delay(100);

					CurrentView = chatVM;
				}
				else
				{
					CurrentView = _loginViewModel;
					MessageBox.Show("Сначала войдите в систему");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка перехода к чату: {ex.Message}");
			}
		}
	}
}
