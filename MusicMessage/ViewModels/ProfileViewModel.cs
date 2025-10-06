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
using System.IO;

namespace MusicMessage.ViewModels
{
	public partial class ProfileViewModel : ObservableObject
	{
		private readonly IAuthService _authService;
		private readonly IFriendsRepository _friendsRepository;
		private readonly IProfileRepository _profileRepository;
		private readonly IPostRepository _postRepository;

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
		private ObservableCollection<Post> _userPosts = new ObservableCollection<Post>();

		[ObservableProperty]
		private string _newProfilePostContent;

		[ObservableProperty]
		private BitmapImage _selectedProfileImage;

		[ObservableProperty]
		private int _friendsCount;

		[ObservableProperty]
		private int _postsCount;

		public ProfileViewModel(IAuthService authService,
							  IFriendsRepository friendsRepository,
							  IProfileRepository profileRepository,
							  IPostRepository postRepository)
		{
			_authService = authService;
			_friendsRepository = friendsRepository;
			_profileRepository = profileRepository;
			_postRepository = postRepository;

			if (_authService?.IsLoggedIn == true)
			{
				CurrentUser = _authService.CurrentUser;
			}
		}

		[RelayCommand]
		public async Task LoadProfileAsync(int userId)
		{
			try
			{
				
				if (_authService?.CurrentUser == null)
				{
					MessageBox.Show("Пользователь не авторизован");
					return;
				}

				CurrentUser = _authService.CurrentUser;

				AvatarImage = null;
				CoverImage = null;

				ViewedUser = await _profileRepository.GetUserProfileAsync(userId);

				if (ViewedUser != null && CurrentUser != null)
				{
					IsOwnProfile = ViewedUser.UserId == CurrentUser.UserId;
				}
				else
				{
					IsOwnProfile = false;
				}


				if (!IsOwnProfile && ViewedUser != null && CurrentUser != null)
				{
					var friendship = await _friendsRepository.GetFriendshipStatusAsync(
						CurrentUser.UserId, ViewedUser.UserId);
					FriendshipStatus = friendship?.Status;
				}

			
				await LoadUserImagesAsync();

				
				await LoadUserPostsAsync();

				
				if (!IsOwnProfile)
				{
					await LoadMutualFriendsAsync();
				}

				
				await LoadStatisticsAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки профиля: {ex.Message}");
			}
		}

		
		[RelayCommand]
		private async Task ToggleLikeInProfileAsync(Post post)
		{
			if (post == null || _authService?.CurrentUser == null) return;

			try
			{
				var isLiked = await _postRepository.IsPostLikedByUserAsync(post.PostId, _authService.CurrentUser.UserId);

				if (isLiked)
				{
					await _postRepository.UnlikePostAsync(post.PostId, _authService.CurrentUser.UserId);
					post.LikesCount--;
				}
				else
				{
					await _postRepository.LikePostAsync(post.PostId, _authService.CurrentUser.UserId);
					post.LikesCount++;
				}

				post.OnPropertyChanged(nameof(post.LikesCount));
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}");
			}
		}

		
		[RelayCommand]
		private async Task ShowPostCommentsInProfileAsync(Post post)
		{
			if (post == null) return;
			ShowPostDetailsRequested?.Invoke(post.PostId);
		}

	
		[RelayCommand]
		private async Task DeletePostInProfileAsync(Post post)
		{
			if (post == null || _authService?.CurrentUser == null)
			{
				MessageBox.Show("Ошибка авторизации");
				return;
			}

			if (post.AuthorId != _authService.CurrentUser.UserId)
			{
				MessageBox.Show("Вы можете удалять только свои посты");
				return;
			}

			var result = MessageBox.Show("Вы уверены, что хотите удалить этот пост?",
				"Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

			if (result == MessageBoxResult.Yes)
			{
				try
				{
					await _postRepository.DeletePostAsync(post.PostId, _authService.CurrentUser.UserId);
					UserPosts.Remove(post);
					PostsCount = UserPosts.Count;
					MessageBox.Show("Пост удален");
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка удаления: {ex.Message}");
				}
			}
		}

		[RelayCommand]
		private void AddProfileImage()
		{
			var openFileDialog = new Microsoft.Win32.OpenFileDialog
			{
				Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*",
				Title = "Выберите изображение"
			};

			if (openFileDialog.ShowDialog() == true)
			{
				try
				{
					var filePath = openFileDialog.FileName;
					if (!File.Exists(filePath))
					{
						MessageBox.Show("Файл не найден");
						return;
					}

					var bitmap = new BitmapImage();
					bitmap.BeginInit();
					bitmap.UriSource = new Uri(filePath);
					bitmap.CacheOption = BitmapCacheOption.OnLoad;
					bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
					bitmap.EndInit();
					bitmap.Freeze();

					SelectedProfileImage = bitmap;
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
				}
			}
		}

		[RelayCommand]
		private void RemoveProfileImage()
		{
			SelectedProfileImage = null;
		}

		[RelayCommand]
		private async Task CreateProfilePostAsync()
		{
			if (_authService?.CurrentUser == null)
			{
				MessageBox.Show("Пользователь не авторизован");
				return;
			}

			if (string.IsNullOrWhiteSpace(NewProfilePostContent) && SelectedProfileImage == null)
			{
				MessageBox.Show("Введите текст или добавьте изображение");
				return;
			}

			try
			{
				string imagePath = null;
				if (SelectedProfileImage != null)
				{
					imagePath = await SavePostImageAsync(SelectedProfileImage);
					if (imagePath == null) return;
				}

				var post = await _postRepository.CreatePostAsync(
					_authService.CurrentUser.UserId, 
					NewProfilePostContent?.Trim(),
					imagePath
				);

				if (post != null)
				{
					UserPosts.Insert(0, post);
					NewProfilePostContent = string.Empty;
					SelectedProfileImage = null;
					PostsCount = UserPosts.Count;
					MessageBox.Show("Пост опубликован!");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка создания поста: {ex.Message}");
			}
		}

		[RelayCommand]
		private async Task LoadUserPostsAsync()
		{
			if (ViewedUser == null) return;

			try
			{
				var posts = await _postRepository.GetUserPostsAsync(ViewedUser.UserId);
				UserPosts.Clear();
				foreach (var post in posts)
				{
					UserPosts.Add(post);
				}
				PostsCount = UserPosts.Count;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки постов: {ex.Message}");
			}
		}

		private async Task LoadUserImagesAsync()
		{
			if (ViewedUser == null) return;

			try
			{
				if (!string.IsNullOrEmpty(ViewedUser.AvatarPath))
				{
					var image = await LoadImageAsync(ViewedUser.AvatarPath);
					AvatarImage = image;
				}

				if (!string.IsNullOrEmpty(ViewedUser.ProfileCoverPath))
				{
					var image = await LoadImageAsync(ViewedUser.ProfileCoverPath);
					CoverImage = image;
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
					if (string.IsNullOrEmpty(fileName)) return null;

					var possiblePaths = new[]
					{
						Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Avatars", fileName),
						Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Covers", fileName),
						Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
						fileName
					};

					foreach (var fullPath in possiblePaths)
					{
						if (File.Exists(fullPath))
						{
							var bitmap = new BitmapImage();
							bitmap.BeginInit();
							bitmap.UriSource = new Uri(fullPath);
							bitmap.CacheOption = BitmapCacheOption.OnLoad;
							bitmap.EndInit();
							bitmap.Freeze();
							return bitmap;
						}
					}
					return null;
				}
				catch
				{
					return null;
				}
			});
		}

		private async Task LoadMutualFriendsAsync()
		{
			if (IsOwnProfile || ViewedUser == null || CurrentUser == null) return;

			try
			{
				var mutualFriends = await _profileRepository.GetMutualFriendsAsync(
					CurrentUser.UserId, ViewedUser.UserId);

				MutualFriends.Clear();
				foreach (var friend in mutualFriends)
				{
					MutualFriends.Add(friend);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки общих друзей: {ex.Message}");
			}
		}

		private async Task LoadStatisticsAsync()
		{
			if (ViewedUser == null) return;

			try
			{
				FriendsCount = await _profileRepository.GetFriendsCountAsync(ViewedUser.UserId);
			
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}");
			}
		}

		private async Task<string> SavePostImageAsync(BitmapImage image)
		{
			if (image == null || _authService?.CurrentUser == null) return null;

			try
			{
				var postsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Posts");
				Directory.CreateDirectory(postsDir);

				var fileName = $"post_{_authService.CurrentUser.UserId}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
				var filePath = Path.Combine(postsDir, fileName);

				var encoder = new JpegBitmapEncoder { QualityLevel = 90 };
				encoder.Frames.Add(BitmapFrame.Create(image));

				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					encoder.Save(stream);
				}

				return fileName;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка сохранения изображения: {ex.Message}");
				return null;
			}
		}

		[RelayCommand]
		private async Task ShowFriendProfile(int friendId)
		{
			try
			{
				OnProfileRequested?.Invoke(friendId);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка открытия профиля: {ex.Message}");
			}
		}

		[RelayCommand]
		private void EditProfile()
		{
			OnEditRequested?.Invoke();
		}

		[RelayCommand]
		private void StartChat()
		{
			if (!IsOwnProfile && ViewedUser != null)
			{
				OnChatRequested?.Invoke(ViewedUser.UserId);
			}
		}

		public event Action<int> OnChatRequested;
		public event Action<int> OnProfileRequested;
		public event Action OnEditRequested;
		public event Action<int> ShowPostDetailsRequested;

		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged([CallerMemberName] string propertyname = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
	}
}