using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicMessage.Models;
using MusicMessage.Repository;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Threading.Tasks;
namespace MusicMessage.ViewModels
{
	public partial class FriendsViewModel : ObservableObject
	{
		private readonly IFriendsRepository _friendsRepository;
		private readonly IAuthService _authService;
		private readonly IChatRepository _chatRepository;
		public event Action<int> OnChatRequested;
		[ObservableProperty]
		private ObservableCollection<UserSearchResult> _searchResults = new();

		[ObservableProperty]
		private ObservableCollection<Friendship> _friendRequests = new();

		[ObservableProperty]
		private ObservableCollection<User> _friends = new();

		[ObservableProperty]
		private string _searchTerm;

		[ObservableProperty]
		private bool _isLoading;

		[ObservableProperty]
		private string _currentSection = "All"; // All, Online, Pending, Search

		public FriendsViewModel(IFriendsRepository friendsRepository, IAuthService authService, IChatRepository chatRepository)
		{
			_friendsRepository = friendsRepository;
			_authService = authService;
			_chatRepository = chatRepository;
		}

		[RelayCommand]
		public async Task LoadFriendsData()
		{
			if (!_authService.IsLoggedIn) return;

			IsLoading = true;
			try
			{
				var friends = await _friendsRepository.GetFriendsAsync(_authService.CurrentUser.UserId);
				var requests = await _friendsRepository.GetFriendRequestsAsync(_authService.CurrentUser.UserId);

				await Application.Current.Dispatcher.InvokeAsync(() =>
				{
					Friends.Clear();
					foreach (var friend in friends)
					{
						Friends.Add(friend);
					}

					FriendRequests.Clear();
					foreach (var request in requests)
					{
						FriendRequests.Add(request);
					}
				});
			}
			finally
			{
				IsLoading = false;
			}
		}
		[RelayCommand]
		private async Task CancelFriendRequest(UserSearchResult user)
		{
			try
			{
				var friendship = await _friendsRepository.GetFriendshipStatusAsync(
					_authService.CurrentUser.UserId,
					user.UserId
				);

				if (friendship != null)
				{
					await _friendsRepository.CancelFriendRequestAsync(friendship.FriendshipId);

					// Обновляем статус локально
					user.FriendshipStatus = null;

					// Уведомляем UI об изменении
					user.OnPropertyChanged(nameof(user.FriendshipStatus));
					user.OnPropertyChanged(nameof(user.FriendshipStatusText));

					MessageBox.Show("Заявка отменена!");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}");
			}
		}

		[RelayCommand]
		private async Task UnblockUser(UserSearchResult user)
		{
			try
			{
				var friendship = await _friendsRepository.GetFriendshipStatusAsync(
					_authService.CurrentUser.UserId,
					user.UserId
				);

				if (friendship != null)
				{
					await _friendsRepository.UnblockUserAsync(friendship.FriendshipId);
					user.FriendshipStatus = null;
					MessageBox.Show("Пользователь разблокирован");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}");
			}
		}
		[RelayCommand]
		private async Task SearchUsers()
		{
			if (string.IsNullOrWhiteSpace(SearchTerm) || SearchTerm.Length < 2)
			{
				SearchResults.Clear();
				return;
			}

			IsLoading = true;
			try
			{
				var results = await _friendsRepository.SearchUsersAsync(
					_authService.CurrentUser.UserId,
					SearchTerm
				);

				await Application.Current.Dispatcher.InvokeAsync(() =>
				{
					SearchResults.Clear();
					foreach (var result in results)
					{
						SearchResults.Add(result);
					}
				});
			}
			finally
			{
				IsLoading = false;
			}
		}

		[RelayCommand]
		private async Task SendFriendRequest(UserSearchResult user)
		{
			try
			{
				await _friendsRepository.SendFriendRequestAsync(
					_authService.CurrentUser.UserId,
					user.UserId
				);

				// Обновляем только этого пользователя
				user.FriendshipStatus = FriendshipStatus.Pending;

				MessageBox.Show("Заявка отправлена!");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}");
			}
		}

		[RelayCommand]
		private async Task AcceptFriendRequest(Friendship request)
		{
			try
			{
				await _friendsRepository.AcceptFriendRequestAsync(request.FriendshipId);

				// Удаляем из заявок
				FriendRequests.Remove(request);

				// Добавляем в друзья
				var newFriend = request.RequesterId == _authService.CurrentUser.UserId
					? request.Addressee
					: request.Requester;

				Friends.Add(newFriend);

				// Обновляем статус в поиске если этот пользователь там есть
				UpdateSearchResultStatus(newFriend.UserId, FriendshipStatus.Accepted);

				MessageBox.Show($"{newFriend.UserName} добавлен в друзья!");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}");
			}
		}
		[RelayCommand]
		private async Task RejectFriendRequest(Friendship request)
		{
			try
			{
				await _friendsRepository.RejectFriendRequestAsync(request.FriendshipId);

				// Просто удаляем из заявок
				FriendRequests.Remove(request);

				MessageBox.Show("Заявка отклонена");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}");
			}
		}
		[RelayCommand]
		private async Task BlockUser(UserSearchResult user)
		{
			try
			{
				var friendship = await _friendsRepository.GetFriendshipStatusAsync(
					_authService.CurrentUser.UserId,
					user.UserId
				);

				if (friendship != null)
				{
					// Передаем оба параметра: currentUserId и targetUserId
					await _friendsRepository.BlockUserAsync(
						_authService.CurrentUser.UserId,
						user.UserId
					);
					user.FriendshipStatus = FriendshipStatus.Blocked;
					MessageBox.Show("Пользователь заблокирован");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}");
			}
		}


		private void UpdateSearchResultStatus(int userId, FriendshipStatus? newStatus)
		{
			var userToUpdate = SearchResults.FirstOrDefault(u => u.UserId == userId);
			if (userToUpdate != null)
			{
				userToUpdate.FriendshipStatus = newStatus;
			}
		}

		[RelayCommand]
		private async Task StartChatWithFriend(User friend)
		{
			// Создаем чат с другом
			await _chatRepository.CreateChatPreviewAsync(_authService.CurrentUser.UserId, friend.UserId);

			// Здесь можно вызвать событие для открытия чата
			// Например: OnChatWithFriendRequested?.Invoke(friend.UserId);
		}

		[RelayCommand]
		private void SwitchSection(string section)
		{
			CurrentSection = section;
		}
		[RelayCommand]
		private async Task StartChat(object parameter)
		{
			try
			{
				int userId = 0;
				string userName = "";

				if (parameter is User user)
				{
					userId = user.UserId;
					userName = user.UserName;
				}
				else if (parameter is UserSearchResult userSearchResult)
				{
					userId = userSearchResult.UserId;
					userName = userSearchResult.UserName;
				}
				else if (parameter is Friendship friendship)
				{
					userId = friendship.RequesterId == _authService.CurrentUser.UserId
						? friendship.AddresseeId
						: friendship.RequesterId;
					userName = friendship.RequesterId == _authService.CurrentUser.UserId
						? friendship.Addressee?.UserName
						: friendship.Requester?.UserName;
				}

				if (userId > 0)
				{
					// Создаем или получаем чат
					var chatPreview = await _chatRepository.GetOrCreateChatAsync(
						_authService.CurrentUser.UserId,
						userId
					);

					// Обновляем список чатов
					await LoadFriendsData();

					// Вызываем событие для перехода в чат
					OnChatRequested?.Invoke(userId);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка создания чата: {ex.Message}");
			}
		}
	}
}
