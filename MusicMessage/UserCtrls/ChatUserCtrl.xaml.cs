using CommunityToolkit.Mvvm.Input;
using MusicMessage.Models;
using MusicMessage.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MusicMessage.Repository;
using Microsoft.Extensions.DependencyInjection;
namespace MusicMessage.UserCtrls
{
    public partial class ChatUserCtrl : UserControl
    {
        public ChatUserCtrl()
		{
			InitializeComponent();

			Loaded += OnChatUserControlLoaded;
			MessagesListView.PreviewMouseRightButtonDown += MessagesListView_PreviewMouseRightButtonDown;
		}
		private void OnChatUserControlLoaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is ChatViewModel vm)
			{
				
				vm.ScrollToLastRequested += OnScrollToLastRequested;

				
				vm.Messages.CollectionChanged += (s, args) =>
				{
					if (args.NewItems?.Count > 0)
						ScrollToLastMessage();
				};
			}
		}

		private void OnScrollToLastRequested()
		{
			ScrollToLastMessage();
		}
		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is ChatViewModel vm)
			{
				vm.Messages.CollectionChanged += (s, args) =>
				{
					if (args.NewItems?.Count > 0)
						ScrollToLastMessage();
				};
			}
		}
		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is ChatViewModel vm)
			{
				vm.ScrollToLastRequested -= OnScrollToLastRequested;
			}
		}
		private async void ScrollToLastMessage()
		{
			await Task.Delay(50);
			Dispatcher.BeginInvoke(() =>
			{
				try
				{
					if (MessagesListView.Items.Count > 0)
					{
						var lastItem = MessagesListView.Items[^1];
						MessagesListView.ScrollIntoView(lastItem);

						// Ждем завершения рендеринга
						Dispatcher.BeginInvoke(() =>
						{
							var scrollViewer = GetScrollViewer(MessagesListView);
							if (scrollViewer != null)
							{
								scrollViewer.ScrollToEnd();
							}
						}, System.Windows.Threading.DispatcherPriority.Loaded);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Scroll error: {ex.Message}");
				}
			}, System.Windows.Threading.DispatcherPriority.Background);
		}
		private static ScrollViewer GetScrollViewer(DependencyObject depObj)
		{
			if (depObj is ScrollViewer scrollViewer)
				return scrollViewer;

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
			{
				var child = VisualTreeHelper.GetChild(depObj, i);
				var result = GetScrollViewer(child);
				if (result != null)
					return result;
			}
			return null;
		}
		private void MessagesListView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
		{
			var listView = sender as ListView;
			var hitTestResult = VisualTreeHelper.HitTest(listView, e.GetPosition(listView));
			var listViewItem = FindParent<ListViewItem>(hitTestResult.VisualHit);

			if (listViewItem != null)
			{
				var message = listViewItem.DataContext as Message;
				if (message != null)
				{
					ShowSmartContextMenu(message, e.GetPosition(listView));
					e.Handled = true;
				}
			}
		}
		private void ShowSmartContextMenu(Message message, Point position)
		{
			var contextMenu = new ContextMenu
			{
				Style = (Style)FindResource("SmartContextMenuStyle")
			};

			var viewModel = DataContext as ChatViewModel;
			if (viewModel == null) return;
			var myReactions = message.Reactions?
		.Where(r => r.UserId == viewModel.CurrentUserId)
		.Select(r => r.Emoji)
		.ToList() ?? new List<string>();
			var reactionsSubMenu = new MenuItem
			{
				Header = "🙂 Реакция",
				ToolTip = "Добавить реакцию"
			};

			
			var popularEmojis = new[] { "👍", "❤️", "😂", "😮", "😢", "😠", "👎" };

			foreach (var emoji in popularEmojis)
			{
				var isMyReaction = myReactions.Contains(emoji);

				var emojiItem = new MenuItem
				{
					Header = emoji, 
					Command = message.ToggleReactionCommand ?? viewModel.ToggleReactionCommand,
					CommandParameter = new object[] { message, emoji },
					
				};

				reactionsSubMenu.Items.Add(emojiItem);
			}
			contextMenu.Items.Add(reactionsSubMenu);
			contextMenu.Items.Add(new Separator());
			
			if (!message.IsVoiceMessage && message.IsEditable)
			{
				var editItem = new MenuItem
				{
					Header = "✏️ Редактировать",
					Command = viewModel.StartEditCommand,
					CommandParameter = message,
					ToolTip = "Редактировать сообщение (доступно 24 часа)",
					Style = (Style)FindResource("EditMenuItemStyle")
				};

				contextMenu.Items.Add(editItem);
				contextMenu.Items.Add(new Separator());
			}

			
			var replyItem = new MenuItem
			{
				Header = "↩️ Ответить",
				Command = viewModel.StartReplyCommand,
				CommandParameter = message,
				ToolTip = "Ответить на это сообщение",
				Style = (Style)FindResource("ActionMenuItemStyle")
			};

			contextMenu.Items.Add(replyItem);
			contextMenu.Items.Add(new Separator());

			bool isMyMessage = message.SenderId == viewModel.CurrentUserId;
			bool isReceivedMessage = message.ReceiverId == viewModel.CurrentUserId;

			
			if (isMyMessage)
			{
				AddDeleteOptionsForMyMessage(contextMenu, viewModel, message);
			}
			else if (isReceivedMessage)
			{
				AddDeleteOptionsForReceivedMessage(contextMenu, viewModel, message);
			}

			
			AddMessageTypeSpecificOptions(contextMenu, viewModel, message);

			
			AddCommonOptions(contextMenu, viewModel, message);

			
			contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
			contextMenu.IsOpen = true;
		}

		private void AddDeleteOptionsForMyMessage(ContextMenu contextMenu, ChatViewModel viewModel, Message message)
		{
			var deleteForMeItem = new MenuItem
			{
				Header = "🗑️ Удалить для меня",
				Command = viewModel.DeleteMessageForMeCommand,
				CommandParameter = message,
				ToolTip = "Сообщение останется у собеседника",
				Style = (Style)FindResource("DeleteMenuItemStyle")
			};

			var deleteForEveryoneItem = new MenuItem
			{
				Header = "🚫 Удалить для всех",
				Command = viewModel.DeleteMessageForEveryoneCommand,
				CommandParameter = message,
				ToolTip = "Сообщение удалится у всех участников чата",
				Style = (Style)FindResource("DeleteForAllMenuItemStyle")
			};
			
			contextMenu.Items.Add(deleteForMeItem);
			contextMenu.Items.Add(deleteForEveryoneItem);
			contextMenu.Items.Add(new Separator());
		}

		private void AddDeleteOptionsForReceivedMessage(ContextMenu contextMenu, ChatViewModel viewModel, Message message)
		{
			var deleteForMeItem = new MenuItem
			{
				Header = "🗑️ Удалить для себя",
				Command = viewModel.DeleteMessageForReceiverCommand,
				CommandParameter = message,
				ToolTip = "Сообщение удалится только в вашем чате",
				Style = (Style)FindResource("DeleteMenuItemStyle") 
			};

			contextMenu.Items.Add(deleteForMeItem);
			contextMenu.Items.Add(new Separator());
		}

		private void AddMessageTypeSpecificOptions(ContextMenu contextMenu, ChatViewModel viewModel, Message message)
		{
			if (message.IsVoiceMessage)
			{
				var saveAudioItem = new MenuItem
				{
					Header = "💾 Сохранить аудио",
					Command = viewModel.SaveVoiceMessageCommand,
					CommandParameter = message,
					ToolTip = "Сохранить аудиофайл на компьютер",
					Style = (Style)FindResource("ActionMenuItemStyle")
				};

				contextMenu.Items.Add(saveAudioItem);
				contextMenu.Items.Add(new Separator());
			}
			else if (!string.IsNullOrEmpty(message.ContentMess))
			{
				var copyTextItem = new MenuItem
				{
					Header = "📋 Копировать текст",
					Command = viewModel.CopyTextCommand,
					CommandParameter = message.ContentMess,
					ToolTip = "Скопировать текст сообщения",
					Style = (Style)FindResource("ActionMenuItemStyle")
				};

				contextMenu.Items.Add(copyTextItem);
				contextMenu.Items.Add(new Separator());
			}
			
		}

		private void AddCommonOptions(ContextMenu contextMenu, ChatViewModel viewModel, Message message)
		{
			var copyTimeItem = new MenuItem
			{
				Header = "⏰ Скопировать время",
				Command = viewModel.CopyTimeCommand,
				CommandParameter = message.Timestamp,
				ToolTip = "Скопировать время отправки",
				Style = (Style)FindResource("ActionMenuItemStyle") 
			};

			contextMenu.Items.Add(copyTimeItem);
		}

		private static T FindParent<T>(DependencyObject child) where T : DependencyObject
		{
			var parent = VisualTreeHelper.GetParent(child);
			while (parent != null && !(parent is T))
			{
				parent = VisualTreeHelper.GetParent(parent);
			}
			return parent as T;
		}

		public void ScrollToMessage(Message message)
		{
			if (message == null) return;

			Dispatcher.BeginInvoke(() =>
			{
				try
				{
					
					MessagesListView.ScrollIntoView(message);

					
					Dispatcher.BeginInvoke(() =>
					{
						var item = MessagesListView.ItemContainerGenerator.ContainerFromItem(message) as ListViewItem;
						if (item != null)
						{
							
							var originalBackground = item.Background;

							
							var highlightBrush = new SolidColorBrush(Colors.Yellow);
							item.Background = highlightBrush;

							
							var animation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(2));
							animation.Completed += (s, e) =>
							{
								
								item.Background = originalBackground;
							};

							highlightBrush.BeginAnimation(Brush.OpacityProperty, animation);

							
							item.Focus();
						}
					}, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Scroll error: {ex.Message}");
				}
			}, System.Windows.Threading.DispatcherPriority.Background);
		}

		
	}
}
