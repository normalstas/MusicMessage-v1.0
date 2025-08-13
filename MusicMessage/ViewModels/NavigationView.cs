using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicMessage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MusicMessage.ViewModels
{
    public partial class NavigationView : ObservableObject
    {
		[ObservableProperty]
		private object _CurrentView; // Изменили на CurrentView (с большой V)

		private readonly IMessageRepository _postman;
		private readonly MessangerBaseContext _context;
		private readonly ChatViewModel _chatViewModel;

		public ICommand ShowChatCommand { get; } // Изменили на ShowChatCommand

		public NavigationView(ChatViewModel chatViewModel)
		{

			_chatViewModel = chatViewModel;
			ShowChatCommand = new RelayCommand(ShowChats);
		}

		private void ShowChats()
		{
			CurrentView = _chatViewModel;
		}

	}
}
