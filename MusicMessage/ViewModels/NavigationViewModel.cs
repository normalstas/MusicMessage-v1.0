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

		public NavigationViewModel(IAuthService authService, LoginViewModel loginViewModel, 
			IServiceProvider serviceProvider, FriendsViewModel friendsViewModel)
		{
			_authService = authService;
			_loginViewModel = loginViewModel;
			_serviceProvider = serviceProvider;
			_isLoggedIn = _authService.IsLoggedIn;
			friendsViewModel.OnChatRequested += OnChatRequestedFromFriends;
			_loginViewModel.OnLoginSuccessful += OnLoginSuccessful;
			CurrentView = _loginViewModel;
		}
		private async void OnChatRequestedFromFriends(int otherUserId)
		{
			var chatViewModel = _serviceProvider.GetService<ChatViewModel>();
			chatViewModel.CurrentReceiverId = otherUserId;

			await chatViewModel.LoadMessagesForCurrentReceiverAsync();
			CurrentView = chatViewModel;

			await Task.Delay(50);
		}
		~NavigationViewModel()
		{
			var friendsViewModel = _serviceProvider.GetService<FriendsViewModel>();
			if (friendsViewModel != null)
			{
				friendsViewModel.OnChatRequested -= OnChatRequestedFromFriends;
			}
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
		[RelayCommand]
		private void ShowFriends()
		{
			if (_authService.IsLoggedIn)
			{
				var friendsVM = _serviceProvider.GetService<FriendsViewModel>();
				if (friendsVM != null)
				{
					_ = friendsVM.LoadFriendsData();
					CurrentView = friendsVM;
				}
			}
		}
	}

}
