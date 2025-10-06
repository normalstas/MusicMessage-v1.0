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
		private readonly NewsFeedViewModel _newsFeedViewModel;
		private readonly PostDetailsViewModel _postDetailsViewModel;

		[ObservableProperty]
		private bool _isLoggedIn;

		[ObservableProperty]
		private bool _isPostDetailsVisible;

		
		private int _lastProfileId;
		public NavigationViewModel(IAuthService authService, LoginViewModel loginViewModel,
			IServiceProvider serviceProvider, FriendsViewModel friendsViewModel, IPostRepository postRepository)
		{
			_authService = authService;
			_loginViewModel = loginViewModel;
			_serviceProvider = serviceProvider;
			_isLoggedIn = _authService.IsLoggedIn;

			_newsFeedViewModel = serviceProvider.GetService<NewsFeedViewModel>();
			_postDetailsViewModel = serviceProvider.GetService<PostDetailsViewModel>();

			
			_postDetailsViewModel.ClosePostDetailsRequested += OnClosePostDetails;
			_newsFeedViewModel.ShowPostDetailsRequested += OnShowPostDetails;

			friendsViewModel.OnChatRequested += OnChatRequestedFromFriends;
			friendsViewModel.OnProfileRequested += OnProfileRequestedFromFriends;
			_loginViewModel.OnLoginSuccessful += OnLoginSuccessful;

			CurrentView = _loginViewModel;
		}

		private async Task ShowPostDetailsAsync(int postId, bool fromProfile = false, int profileId = 0)
		{
			if (!_authService.IsLoggedIn) return;

			try
			{
				await _postDetailsViewModel.LoadPostDetailsAsync(postId);
				_postDetailsViewModel.SetSource(fromProfile, profileId);
				CurrentView = _postDetailsViewModel;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка открытия поста: {ex.Message}");
			}
		}
		private async void OnShowPostDetails(int postId)
		{
			await ShowPostDetailsAsync(postId, false);
		}


		private async void OnShowPostDetailsFromProfile(int postId)
		{
			
			int profileId = _currentProfileViewModel?.ViewedUser?.UserId ?? 0;
			await ShowPostDetailsAsync(postId, true, profileId);
		}

		private void OnClosePostDetails(bool fromProfile, int profileId)
		{
			if (fromProfile && profileId > 0)
			{
				
				_ = ShowFriendProfile(profileId);
			}
			else
			{
				
				ShowNewsFeed();
			}
		}

		[RelayCommand]
		private async Task ShowNewsFeed()
		{
			if (_authService.IsLoggedIn)
			{
				await _newsFeedViewModel.LoadNewsFeedAsync();
				CurrentView = _newsFeedViewModel;
			}
		}

		[RelayCommand]
		private async Task ShowProfile()
		{
			if (!_authService.IsLoggedIn || _authService.CurrentUser == null) return;

			var profileVM = _serviceProvider.GetService<ProfileViewModel>();
			if (profileVM != null)
			{
				if (_currentProfileViewModel != null)
				{
					_currentProfileViewModel.OnEditRequested -= OnEditProfileRequested;
					_currentProfileViewModel.OnChatRequested -= OnChatRequestedFromProfile;
					_currentProfileViewModel.OnProfileRequested -= OnProfileRequestedFromProfile;
					_currentProfileViewModel.ShowPostDetailsRequested -= OnShowPostDetailsFromProfile;
				}

				await profileVM.LoadProfileAsync(_authService.CurrentUser.UserId);

				profileVM.OnEditRequested += OnEditProfileRequested;
				profileVM.OnChatRequested += OnChatRequestedFromProfile;
				profileVM.OnProfileRequested += OnProfileRequestedFromProfile;
				profileVM.ShowPostDetailsRequested += OnShowPostDetailsFromProfile;

				_currentProfileViewModel = profileVM;
				CurrentView = profileVM;
			}
		}

		[RelayCommand]
		private async Task ShowFriendProfile(int userId)
		{
			try
			{
				if (!_authService.IsLoggedIn || _authService.CurrentUser == null) return;

				var profileVM = _serviceProvider.GetService<ProfileViewModel>();
				if (profileVM != null)
				{
					if (_currentProfileViewModel != null)
					{
						_currentProfileViewModel.OnEditRequested -= OnEditProfileRequested;
						_currentProfileViewModel.OnChatRequested -= OnChatRequestedFromProfile;
						_currentProfileViewModel.OnProfileRequested -= OnProfileRequestedFromProfile;
						_currentProfileViewModel.ShowPostDetailsRequested -= OnShowPostDetailsFromProfile;
					}

					await profileVM.LoadProfileAsync(userId);

					profileVM.OnEditRequested += OnEditProfileRequested;
					profileVM.OnChatRequested += OnChatRequestedFromProfile;
					profileVM.OnProfileRequested += OnProfileRequestedFromProfile;
					profileVM.ShowPostDetailsRequested += OnShowPostDetailsFromProfile;

					_currentProfileViewModel = profileVM;
					CurrentView = profileVM;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка открытия профиля: {ex.Message}");
			}
		}
		private async void OnProfileRequestedFromProfile(int userId)
		{
			await ShowFriendProfile(userId);
		}

		private async void OnChatRequestedFromProfile(int receiverId)
		{
			await OpenChatWithUser(receiverId);
		}

		[RelayCommand]
		private async Task ShowPostCommentsAsync(Post post)
		{
			if (post == null) return;

			_postDetailsViewModel.Open(post);
			IsPostDetailsVisible = true;

			await _postDetailsViewModel.LoadCommentsAsync();
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

		private void OnEditProfileRequested()
		{
			var editVM = _serviceProvider.GetService<EditProfileViewModel>();
			if (editVM != null)
			{
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
					profileVM.AvatarImage = null;
					profileVM.CoverImage = null;

					await profileVM.LoadProfileAsync(_authService.CurrentUser.UserId);
					profileVM.OnEditRequested += OnEditProfileRequested;
					CurrentView = profileVM;

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
			var profileVM = _serviceProvider.GetService<ProfileViewModel>();
			if (profileVM != null)
			{
				await profileVM.LoadProfileAsync(_authService.CurrentUser.UserId);
				CurrentView = profileVM;
			}
		}

		private async void OnChatSelected(int receiverId)
		{
			var chatViewModel = _serviceProvider.GetService<ChatViewModel>();
			chatViewModel.CurrentReceiverId = receiverId;

			await chatViewModel.LoadMessagesForCurrentReceiverAsync();
			CurrentView = chatViewModel;
			await Task.Delay(50);

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
				_currentProfileViewModel.ShowPostDetailsRequested -= OnShowPostDetailsFromProfile;
			}
		}
	}
}