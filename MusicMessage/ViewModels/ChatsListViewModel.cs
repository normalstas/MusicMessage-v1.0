using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MusicMessage.Models;
using MusicMessage.Repository;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MusicMessage.ViewModels
{
	public partial class ChatsListViewModel : ObservableObject
	{
		public event Action<int> OnChatSelected;

		private readonly IChatRepository _chatRepository;
		private readonly IAuthService _authService;
		[ObservableProperty]
		private ChatPreview _selectedChat;
		private bool _isViewActive;


		[ObservableProperty]
		private ObservableCollection<ChatPreview> _chats = new ObservableCollection<ChatPreview>();

		[ObservableProperty]
		private bool _isLoading;

		public ChatsListViewModel(IChatRepository chatRepository, IAuthService authService)
		{
			_chatRepository = chatRepository;
			_authService = authService;
			_ = LoadChatsAsync();
		}
		
		public async void OnViewActivated()
		{
			if (_isViewActive) return;

			_isViewActive = true;
			await LoadChatsAsync();
		}

		
		public void OnViewDeactivated()
		{
			_isViewActive = false;
		}

		[RelayCommand]
		public async Task LoadChatsAsync()
		{
			if (!_authService.IsLoggedIn) return;

			IsLoading = true;
			try
			{
				
				var chatRepository = App.ServiceProvider.GetService<IChatRepository>();
				if (chatRepository != null)
				{
					await chatRepository.UpdateAllChatsLastMessagesAsync(_authService.CurrentUser.UserId);
				}

			
				var chats = await _chatRepository.GetUserChatsAsync(_authService.CurrentUser.UserId);

				
				await Application.Current.Dispatcher.InvokeAsync(() =>
				{
					Chats.Clear();
					foreach (var chat in chats)
					{
						Chats.Add(chat);
					}
				});
			}
			catch (Exception ex)
			{
				
				MessageBox.Show($"Ошибка загрузки чатов: {ex.Message}\n\n" +
							   "Это может быть связано с параллельными изменениями в базе данных. " +
							   "Попробуйте обновить список чатов.");
				Debug.WriteLine($"LoadChatsAsync error: {ex.Message}");
			}
			finally
			{
				IsLoading = false;
			}
		}
		[RelayCommand]
		private void OpenChat(ChatPreview chat)
		{
			if (chat != null)
			{
				OnChatSelected?.Invoke(chat.OtherUserId);
				SelectedChat = chat;
			}
		}

		[RelayCommand]
		private void StartNewChat()
		{
			MessageBox.Show("Функция начала нового чата будет реализована позже");
		}
	}
}
