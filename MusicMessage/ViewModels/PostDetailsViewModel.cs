using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MusicMessage.Models;
using MusicMessage.Repository;
using MusicMessage.UserCtrls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MusicMessage.ViewModels
{
	public partial class PostDetailsViewModel : ObservableObject
	{
		private readonly IPostRepository _postRepository;
		private readonly IAuthService _authService;

		[ObservableProperty]
		private Post _currentPost;

		[ObservableProperty]
		private ObservableCollection<PostComment> _comments = new ObservableCollection<PostComment>();

		[ObservableProperty]
		private string _newCommentText;

		[ObservableProperty]
		private bool _isLoading;

		[ObservableProperty]
		private bool _isOpen;

		[ObservableProperty]
		private string _backButtonText = "← Назад к ленте";

		[ObservableProperty]
		private bool _cameFromProfile;
		[ObservableProperty]
		private PostComment _comment;
		[ObservableProperty]
		private int _sourceProfileId;
		

		public PostDetailsViewModel(IPostRepository postRepository, IAuthService authService)
		{
			_postRepository = postRepository;
			_authService = authService;
		}

		
		public void SetSource(bool fromProfile, int profileId = 0)
		{
			CameFromProfile = fromProfile;
			SourceProfileId = profileId;
			BackButtonText = fromProfile ? "← Назад к профилю" : "← Назад к ленте";
		}

		[RelayCommand]
		public async Task LoadPostDetailsAsync(int postId)
		{
			IsLoading = true;
			try
			{
				CurrentPost = await _postRepository.GetPostByIdAsync(postId);
				var comments = await _postRepository.GetPostCommentsAsync(postId);

				Comments.Clear();
				foreach (var comment in comments)
				{
					
					if (_authService.CurrentUser != null)
					{
						comment.IsLikedByCurrentUser = await _postRepository.IsCommentLikedByUserAsync(
							comment.CommentId, _authService.CurrentUser.UserId);
					}
					Comments.Add(comment);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки поста: {ex.Message}");
			}
			finally
			{
				IsLoading = false;
			}
		}

		[RelayCommand]
		private void ClosePostDetails()
		{
		
			ClosePostDetailsRequested?.Invoke(CameFromProfile, SourceProfileId);
		}


		[RelayCommand]
		private async Task AddCommentAsync()
		{
			if (string.IsNullOrWhiteSpace(NewCommentText) || CurrentPost == null) return;

			try
			{
				var comment = await _postRepository.AddCommentAsync(
					CurrentPost.PostId,
					_authService.CurrentUser.UserId,
					NewCommentText.Trim()
				);

				if (comment != null)
				{
					Comments.Add(comment);
					NewCommentText = string.Empty;
					CurrentPost.CommentsCount++;
					CurrentPost.OnPropertyChanged(nameof(CurrentPost.CommentsCount));
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка добавления комментария: {ex.Message}");
			}
		}

		[RelayCommand]
		private async Task ToggleLikeAsync()
		{
			if (CurrentPost == null) return;

			try
			{
				var isLiked = await _postRepository.IsPostLikedByUserAsync(
					CurrentPost.PostId, _authService.CurrentUser.UserId);

				if (isLiked)
				{
					await _postRepository.UnlikePostAsync(CurrentPost.PostId, _authService.CurrentUser.UserId);
					CurrentPost.LikesCount--;
				}
				else
				{
					await _postRepository.LikePostAsync(CurrentPost.PostId, _authService.CurrentUser.UserId);
					CurrentPost.LikesCount++;
				}

				CurrentPost.OnPropertyChanged(nameof(CurrentPost.LikesCount));
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}");
			}
		}

		[RelayCommand]
		private async Task DeletePostAsync()
		{
			if (CurrentPost.AuthorId != _authService.CurrentUser.UserId)
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
					await _postRepository.DeletePostAsync(CurrentPost.PostId, _authService.CurrentUser.UserId);
					ClosePostDetailsRequested?.Invoke(CameFromProfile, SourceProfileId);
					MessageBox.Show("Пост удален");
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка удаления: {ex.Message}");
				}
			}
		}
		[RelayCommand]
		private async Task ToggleCommentLikeAsync(PostComment comment)
		{
			if (comment == null || _authService.CurrentUser == null) return;

			try
			{
				var isLiked = await _postRepository.IsCommentLikedByUserAsync(
					comment.CommentId, _authService.CurrentUser.UserId);

				if (isLiked)
				{
					await _postRepository.UnlikeCommentAsync(comment.CommentId, _authService.CurrentUser.UserId);
					comment.LikesCount--;
					comment.IsLikedByCurrentUser = false;
				}
				else
				{
					await _postRepository.LikeCommentAsync(comment.CommentId, _authService.CurrentUser.UserId);
					comment.LikesCount++;
					comment.IsLikedByCurrentUser = true;
				}

				
				comment.OnPropertyChanged(nameof(comment.LikesCount));
				comment.OnPropertyChanged(nameof(comment.IsLikedByCurrentUser));
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка: {ex.Message}");
			}
		}
		[RelayCommand]
		public async Task LoadCommentsAsync()
		{
			if (CurrentPost == null) return;

			IsLoading = true;
			try
			{
				var comments = await _postRepository.GetPostCommentsAsync(CurrentPost.PostId);

				await Application.Current.Dispatcher.InvokeAsync(() =>
				{
					Comments.Clear();
					foreach (var comment in comments)
					{
						Comments.Add(comment);
					}
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки комментариев: {ex.Message}");
			}
			finally
			{
				IsLoading = false;
			}
		}

		public async void Open(Post post)
		{
			CurrentPost = post;
			IsOpen = true;
			await LoadCommentsAsync();
		}
		[RelayCommand]
		public void CommentOpenProfile(PostComment comment)
		{
			if (comment != null)
			{
				var navigationVM = App.ServiceProvider.GetService<NavigationViewModel>(); 
				navigationVM?.ShowFriendProfileCommand.Execute(comment.Author.UserId);
			}
		}
		
		public event Action<bool, int> ClosePostDetailsRequested;
	}
}