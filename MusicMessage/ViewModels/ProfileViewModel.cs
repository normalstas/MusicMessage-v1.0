using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicMessage.Models;
using MusicMessage.Repository;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;
using System.IO; // Для Path
using System;
namespace MusicMessage.ViewModels
{
	public partial class ProfileViewModel : ObservableObject
	{
		private readonly IAuthService _authService;
		private readonly IFriendsRepository _friendsRepository;
		private readonly IProfileRepository _profileRepository;

		[ObservableProperty]
		private User _currentUser;

		[ObservableProperty]
		private User _viewedUser;

		[ObservableProperty]
		private FriendshipStatus? _friendshipStatus;

		[ObservableProperty]
		private bool _isOwnProfile;

		

		[ObservableProperty]
		private BitmapImage _avatarImage;

		[ObservableProperty]
		private BitmapImage _coverImage;

		[ObservableProperty]
		private ObservableCollection<User> _mutualFriends = new();

		[ObservableProperty]
		private int _friendsCount;

		[ObservableProperty]
		private int _postsCount;
		public event Action<int> OnChatRequested;

		public ProfileViewModel(IAuthService authService,
							  IFriendsRepository friendsRepository,
							  IProfileRepository profileRepository)
		{
			_authService = authService;
			_friendsRepository = friendsRepository;
			_profileRepository = profileRepository;
			CurrentUser = _authService.CurrentUser;
		}

		[RelayCommand]
		public async Task LoadProfileAsync(int userId)
		{
			try
			{
				// СБРАСЫВАЕМ изображения перед загрузкой
				AvatarImage = null;
				CoverImage = null;

				// Загружаем данные пользователя
				ViewedUser = await _profileRepository.GetUserProfileAsync(userId);
				IsOwnProfile = ViewedUser.UserId == CurrentUser.UserId;

				// Загружаем статус дружбы
				if (!IsOwnProfile)
				{
					var friendship = await _friendsRepository.GetFriendshipStatusAsync(
						CurrentUser.UserId, ViewedUser.UserId);
					FriendshipStatus = friendship?.Status;
				}

				// Загружаем аватар и обложку с принудительным обновлением
				await LoadUserImagesAsync();

				// Загружаем общих друзей
				await LoadMutualFriendsAsync();

				// Загружаем статистику
				await LoadStatisticsAsync();

				// Принудительно обновляем привязки
				OnPropertyChanged(nameof(AvatarImage));
				OnPropertyChanged(nameof(CoverImage));
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки профиля: {ex.Message}");
			}
		}

		[RelayCommand]
		private async Task AddFriendAsync()
		{
			try
			{
				await _friendsRepository.SendFriendRequestAsync(
					CurrentUser.UserId, ViewedUser.UserId);
				FriendshipStatus = Models.FriendshipStatus.Pending;
				MessageBox.Show("Заявка в друзья отправлена!");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}");
			}
		}

		[RelayCommand]
		private void EditProfile()
		{
			// Переходим в режим редактирования
			OnEditRequested?.Invoke();
		}

		public event Action OnEditRequested;
		[RelayCommand]
		private void StartChat()
		{
			if (!IsOwnProfile && ViewedUser != null)
			{
				// Вызываем событие для открытия чата
				OnChatRequested?.Invoke(ViewedUser.UserId);
			}
		}
		private async Task LoadUserImagesAsync()
		{
			try
			{
				// Загружаем аватар
				if (!string.IsNullOrEmpty(ViewedUser.AvatarPath))
				{
					var image = await LoadImageAsync(ViewedUser.AvatarPath);
					AvatarImage = image;
					Console.WriteLine($"Avatar loaded: {ViewedUser.AvatarPath}, Success: {image != null}");
				}
				else
				{
					AvatarImage = null;
					Console.WriteLine("No avatar path");
				}

				// Загружаем обложку
				if (!string.IsNullOrEmpty(ViewedUser.ProfileCoverPath))
				{
					var image = await LoadImageAsync(ViewedUser.ProfileCoverPath);
					CoverImage = image;
					Console.WriteLine($"Cover loaded: {ViewedUser.ProfileCoverPath}, Success: {image != null}");
				}
				else
				{
					CoverImage = null;
					Console.WriteLine("No cover path");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"LoadUserImagesAsync error: {ex.Message}");
			}
		}

		private async Task<BitmapImage> LoadImageAsync(string fileName)
		{
			return await Task.Run(() =>
			{
				try
				{
					if (string.IsNullOrEmpty(fileName))
					{
						Console.WriteLine("FileName is null or empty");
						return null;
					}

					var possiblePaths = new[]
					{
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Avatars", fileName),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Covers", fileName),
				Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
				fileName
			};

					foreach (var fullPath in possiblePaths)
					{
						Console.WriteLine($"Checking path: {fullPath}");
						if (File.Exists(fullPath))
						{
							Console.WriteLine($"File found: {fullPath}");

							var bitmap = new BitmapImage();
							bitmap.BeginInit();
							bitmap.UriSource = new Uri(fullPath);
							bitmap.CacheOption = BitmapCacheOption.OnLoad;
							bitmap.EndInit();
							bitmap.Freeze();
							return bitmap;
						}
					}

					Console.WriteLine($"File not found in any location: {fileName}");
					return null;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"LoadImageAsync error: {ex.Message}");
					return null;
				}
			});
		}

		private async Task LoadMutualFriendsAsync()
		{
			if (IsOwnProfile) return;

			var mutualFriends = await _profileRepository.GetMutualFriendsAsync(
				CurrentUser.UserId, ViewedUser.UserId);

			MutualFriends.Clear();
			foreach (var friend in mutualFriends)
			{
				MutualFriends.Add(friend);
			}
		}

		private async Task LoadStatisticsAsync()
		{
			FriendsCount = await _profileRepository.GetFriendsCountAsync(ViewedUser.UserId);
			PostsCount = await _profileRepository.GetPostsCountAsync(ViewedUser.UserId);
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged([CallerMemberName] string propertyname = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
	}
}
