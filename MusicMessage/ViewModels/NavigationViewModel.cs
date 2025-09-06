using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
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
		private readonly LoginViewModel _loginViewModel;
		private readonly IServiceProvider _serviceProvider;

		[ObservableProperty]
		private bool _isLoggedIn;

		public NavigationViewModel(IAuthService authService, LoginViewModel loginViewModel, IServiceProvider serviceProvider)
		{
			_authService = authService;
			_loginViewModel = loginViewModel;
			_serviceProvider = serviceProvider;
			_isLoggedIn = _authService.IsLoggedIn;

			_loginViewModel.OnLoginSuccessful += OnLoginSuccessful;
			CurrentView = _loginViewModel;
		}

		private void OnLoginSuccessful()
		{
			IsLoggedIn = true;
			ShowChatsList();
		}

		private void ShowChatsList()
		{
			ShowChats();
		}

		private async void OnChatSelected(int receiverId)
		{
			var chatViewModel = _serviceProvider.GetService<ChatViewModel>();
			chatViewModel.CurrentReceiverId = receiverId;

			await chatViewModel.LoadMessagesForCurrentReceiverAsync();
			CurrentView = chatViewModel;
			await Task.Delay(50);
		}
		[RelayCommand]
		private void ShowChats()
		{
			if (_authService.IsLoggedIn)
			{
				var chatsListVM = _serviceProvider.GetService<ChatsListViewModel>();
				if (chatsListVM != null)
				{
					chatsListVM.OnChatSelected += OnChatSelected;
					CurrentView = chatsListVM;

					// ОБНОВЛЯЕМ чаты при переходе
					_ = chatsListVM.LoadChatsAsync();
				}
			}
		}
	}

}
