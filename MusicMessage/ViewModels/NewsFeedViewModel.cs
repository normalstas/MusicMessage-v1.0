using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicMessage.Models;
using MusicMessage.Repository;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;

namespace MusicMessage.ViewModels
{
	public partial class NewsFeedViewModel : ObservableObject
	{
		private readonly IPostRepository _postRepository;
		private readonly IAuthService _authService;
		private readonly IProfileRepository _profileRepository;
		public event Action<int> ShowPostDetailsRequested;
		[ObservableProperty]
		private ObservableCollection<Post> _posts = new ObservableCollection<Post>();

		[ObservableProperty]
		private string _newPostContent;

		[ObservableProperty]
		private bool _isLoading;

		[ObservableProperty]
		private bool _hasMorePosts = true;

		[ObservableProperty]
		private BitmapImage _selectedImage;

		[ObservableProperty]
		private bool _isPostEditorVisible = true;

		
		[ObservableProperty]
		private bool _isCommentsVisible;
		private int _currentPage = 1;
		private const int PageSize = 10;

		public NewsFeedViewModel(IPostRepository postRepository, IAuthService authService, IProfileRepository profileRepository)
		{
			_postRepository = postRepository;
			_authService = authService;
			_profileRepository = profileRepository;
			
		}

		[RelayCommand]
		public async Task LoadNewsFeedAsync()
		{
			if (!_authService.IsLoggedIn) return;

			IsLoading = true;
			try
			{
				_currentPage = 1;
				var posts = await _postRepository.GetNewsFeedAsync(_authService.CurrentUser.UserId, _currentPage, PageSize);

				await Application.Current.Dispatcher.InvokeAsync(() =>
				{
					Posts.Clear();
					foreach (var post in posts)
					{
						Posts.Add(post);
					}
					HasMorePosts = posts.Count == PageSize;
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки ленты: {ex.Message}");
			}
			finally
			{
				IsLoading = false;
			}
		}
		[RelayCommand]
		private void TogglePostEditor()
		{
			IsPostEditorVisible = !IsPostEditorVisible;
		}

		[RelayCommand]
		private async Task ShowPostCommentsAsync(Post post)
		{
			if (post == null) return;

			ShowPostDetailsRequested?.Invoke(post.PostId);
		}
		[RelayCommand]
		private async Task LoadMorePostsAsync()
		{
			if (!HasMorePosts || IsLoading) return;

			IsLoading = true;
			try
			{
				_currentPage++;
				var posts = await _postRepository.GetNewsFeedAsync(_authService.CurrentUser.UserId, _currentPage, PageSize);

				await Application.Current.Dispatcher.InvokeAsync(() =>
				{
					foreach (var post in posts)
					{
						Posts.Add(post);
					}
					HasMorePosts = posts.Count == PageSize;
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки дополнительных постов: {ex.Message}");
				_currentPage--;
			}
			finally
			{
				IsLoading = false;
			}
		}

		[RelayCommand]
		private async Task CreatePostAsync()
		{
			if (string.IsNullOrWhiteSpace(NewPostContent) && SelectedImage == null)
			{
				MessageBox.Show("Введите текст или добавьте изображение");
				return;
			}

			try
			{
				string imagePath = null;

				
				if (SelectedImage != null)
				{
					imagePath = await SavePostImageAsync();
					if (imagePath == null)
					{
						MessageBox.Show("Ошибка при сохранении изображения");
						return;
					}
				}

				var post = await _postRepository.CreatePostAsync(
					_authService.CurrentUser.UserId,
					NewPostContent?.Trim(),
					imagePath
				);

				if (post != null)
				{
					await Application.Current.Dispatcher.InvokeAsync(() =>
					{
						Posts.Insert(0, post);
						NewPostContent = string.Empty;
						SelectedImage = null;
					});

					MessageBox.Show("Пост опубликован!");
				}
				else
				{
					MessageBox.Show("Ошибка при создании поста");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка создания поста: {ex.Message}");
			}
		}
		public int GetCurrentUserId()
		{
			return _authService.CurrentUser?.UserId ?? 0;
		}

		[RelayCommand]
		private async Task ToggleLikeAsync(Post post)
		{
			if (post == null) return;

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
		private void AddImage()
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

					SelectedImage = bitmap;
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
				}
			}
		}
		[RelayCommand]
		private void RemoveImage()
		{
			SelectedImage = null;
		}

		

		[RelayCommand]
		private async Task SharePostAsync(Post post)
		{
			
			MessageBox.Show("Функция репоста будет реализована в следующем обновлении");
		}

		[RelayCommand]
		private async Task DeletePostAsync(Post post)
		{
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

					await Application.Current.Dispatcher.InvokeAsync(() =>
					{
						Posts.Remove(post);
					});

					MessageBox.Show("Пост удален");
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка удаления: {ex.Message}");
				}
			}
		}

		private async Task<string> SavePostImageAsync()
		{
			if (SelectedImage == null) return null;

			try
			{
				var postsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Posts");
				Directory.CreateDirectory(postsDir);

				var fileName = $"post_{_authService.CurrentUser.UserId}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
				var filePath = Path.Combine(postsDir, fileName);

				
				var encoder = new JpegBitmapEncoder { QualityLevel = 90 };
				encoder.Frames.Add(BitmapFrame.Create(SelectedImage));

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

		public async Task<BitmapImage> LoadPostImageAsync(string imagePath)
		{
			if (string.IsNullOrEmpty(imagePath)) return null;

			return await Task.Run(() =>
			{
				try
				{
					var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Posts", imagePath);
					if (!File.Exists(fullPath)) return null;

					var bitmap = new BitmapImage();
					bitmap.BeginInit();
					bitmap.UriSource = new Uri(fullPath);
					bitmap.CacheOption = BitmapCacheOption.OnLoad;
					bitmap.EndInit();
					bitmap.Freeze();
					return bitmap;
				}
				catch
				{
					return null;
				}
			});
		}
	}
}
