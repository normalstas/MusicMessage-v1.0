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
	public partial class NavigationViewModel : ObservableObject, IDisposable
	{
		[ObservableProperty]
		private object _currentView;

		private readonly IAuthService _authService;
		private readonly LoginViewModel _loginViewModel;
		private readonly IServiceProvider _serviceProvider;
		private ProfileViewModel _currentProfileViewModel;
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
			friendsViewModel.OnProfileRequested += OnProfileRequestedFromFriends;
			_loginViewModel.OnLoginSuccessful += OnLoginSuccessful;

			CurrentView = _loginViewModel;
		}
		private async void OnProfileRequestedFromProfile(int userId)
		{
			await ShowFriendProfile(userId);
		}
		private async void OnChatRequestedFromProfile(int receiverId)
		{
			await OpenChatWithUser(receiverId);
		}
		private async Task OpenChatWithUser(int receiverId)
		{
			var chatViewModel = _serviceProvider.GetService<ChatViewModel>();
			chatViewModel.CurrentReceiverId = receiverId;

			await chatViewModel.LoadMessagesForCurrentReceiverAsync();
			CurrentView = chatViewModel;

			await Task.Delay(50);
		}
		private async void OnChatRequestedFromFriends(int otherUserId)
		{
			var chatViewModel = _serviceProvider.GetService<ChatViewModel>();
			chatViewModel.CurrentReceiverId = otherUserId;

			await chatViewModel.LoadMessagesForCurrentReceiverAsync();
			CurrentView = chatViewModel;

			await Task.Delay(50);
		}
		private async void OnProfileRequestedFromFriends(int userId)
		{
			await ShowFriendProfile(userId);
		}
		~NavigationViewModel()
		{
			var friendsViewModel = _serviceProvider.GetService<FriendsViewModel>();
			if (friendsViewModel != null)
			{
				friendsViewModel.OnChatRequested -= OnChatRequestedFromFriends;
				friendsViewModel.OnProfileRequested -= OnProfileRequestedFromFriends;
			}
			var profileViewModel = _serviceProvider.GetService<ProfileViewModel>();
			if (profileViewModel != null)
			{
				profileViewModel.OnProfileRequested -= OnProfileRequestedFromProfile;
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
		[RelayCommand]
		private async Task ShowProfile()
		{
			if (!_authService.IsLoggedIn) return;

			var profileVM = _serviceProvider.GetService<ProfileViewModel>();
			if (profileVM != null)
			{
				// Отписываемся от предыдущего экземпляра
				if (_currentProfileViewModel != null)
				{
					_currentProfileViewModel.OnEditRequested -= OnEditProfileRequested;
					_currentProfileViewModel.OnChatRequested -= OnChatRequestedFromProfile;
					_currentProfileViewModel.OnProfileRequested -= OnProfileRequestedFromProfile;
				}

				await profileVM.LoadProfileAsync(_authService.CurrentUser.UserId);

				// Подписываемся на события нового экземпляра
				profileVM.OnEditRequested += OnEditProfileRequested;
				profileVM.OnChatRequested += OnChatRequestedFromProfile;
				profileVM.OnProfileRequested += OnProfileRequestedFromProfile;

				_currentProfileViewModel = profileVM; // Сохраняем ссылку
				CurrentView = profileVM;
			}
		}
		private void OnEditProfileRequested()
		{
			var editVM = _serviceProvider.GetService<EditProfileViewModel>();
			if (editVM != null)
			{
				// Загружаем изображения при инициализации
				editVM.LoadImages();
				editVM.OnProfileSaved += OnProfileSaved;
				editVM.OnCancelled += OnEditCancelled;
				CurrentView = editVM;
			}
		}

		private async void OnProfileSaved()
		{
			try
			{
				var profileVM = _serviceProvider.GetService<ProfileViewModel>();
				if (profileVM != null)
				{
					// СБРАСЫВАЕМ кэш изображений ПРАВИЛЬНО
					profileVM.AvatarImage = null;
					profileVM.CoverImage = null;

					// ПЕРЕЗАГРУЖАЕМ данные
					await profileVM.LoadProfileAsync(_authService.CurrentUser.UserId);
					profileVM.OnEditRequested += OnEditProfileRequested;
					CurrentView = profileVM;

					// Принудительное обновление
					profileVM.OnPropertyChanged(nameof(profileVM.AvatarImage));
					profileVM.OnPropertyChanged(nameof(profileVM.CoverImage));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при обновлении профиля: {ex.Message}");
			}
		}

		private async void OnEditCancelled()
		{
			// Восстанавливаем оригинальные данные
			var profileVM = _serviceProvider.GetService<ProfileViewModel>();
			if (profileVM != null)
			{
				// ПЕРЕЗАГРУЖАЕМ данные из базы, чтобы восстановить оригинальные значения
				await profileVM.LoadProfileAsync(_authService.CurrentUser.UserId);
				CurrentView = profileVM;
			}
		}
		// Также добавь метод для открытия профиля друга
		[RelayCommand]
		private async Task ShowFriendProfile(int userId)
		{
			try
			{
				var profileVM = _serviceProvider.GetService<ProfileViewModel>();
				if (profileVM != null)
				{
					// Отписываемся от предыдущего экземпляра
					if (_currentProfileViewModel != null)
					{
						_currentProfileViewModel.OnEditRequested -= OnEditProfileRequested;
						_currentProfileViewModel.OnChatRequested -= OnChatRequestedFromProfile;
						_currentProfileViewModel.OnProfileRequested -= OnProfileRequestedFromProfile;
					}

					await profileVM.LoadProfileAsync(userId);

					// Подписываемся на события нового экземпляра
					profileVM.OnEditRequested += OnEditProfileRequested;
					profileVM.OnChatRequested += OnChatRequestedFromProfile;
					profileVM.OnProfileRequested += OnProfileRequestedFromProfile;

					_currentProfileViewModel = profileVM; // Сохраняем ссылку
					CurrentView = profileVM;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка открытия профиля: {ex.Message}");
			}
		}
		private async void OnChatSelected(int receiverId)
		{
			var chatViewModel = _serviceProvider.GetService<ChatViewModel>();
			chatViewModel.CurrentReceiverId = receiverId;

			await chatViewModel.LoadMessagesForCurrentReceiverAsync();
			CurrentView = chatViewModel;
			await Task.Delay(50);

			// Обновляем шапку чата
			await chatViewModel.UpdateChatHeaderInfo();
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
				var profileVM = _serviceProvider.GetService<ProfileViewModel>();
				if (profileVM != null)
				{
					profileVM.OnChatRequested -= OnChatRequestedFromProfile;
				}
			}
		}
		public void Dispose()
		{
			var friendsViewModel = _serviceProvider.GetService<FriendsViewModel>();
			if (friendsViewModel != null)
			{
				friendsViewModel.OnChatRequested -= OnChatRequestedFromFriends;
				friendsViewModel.OnProfileRequested -= OnProfileRequestedFromFriends;
			}

			if (_currentProfileViewModel != null)
			{
				_currentProfileViewModel.OnEditRequested -= OnEditProfileRequested;
				_currentProfileViewModel.OnChatRequested -= OnChatRequestedFromProfile;
				_currentProfileViewModel.OnProfileRequested -= OnProfileRequestedFromProfile;
			}
		}
	}

}
