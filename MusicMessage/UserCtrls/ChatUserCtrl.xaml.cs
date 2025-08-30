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
    /// <summary>
    /// Логика взаимодействия для ChatUserCtrl.xaml
    /// </summary>
    public partial class ChatUserCtrl : UserControl
    {
        public ChatUserCtrl()
        {
            InitializeComponent();
			var serviceProvider = App.ServiceProvider;
			var context = serviceProvider.GetService<MessangerBaseContext>();
			var repository = serviceProvider.GetService<IMessageRepository>();
			var authService = serviceProvider.GetService<IAuthService>();
			DataContext = new ChatViewModel(repository, authService);

			// Подписка на изменение коллекции сообщений
			var vm = (ChatViewModel)DataContext;
			vm.Messages.CollectionChanged += (s, e) =>
			{
				if (e.NewItems?.Count > 0)
					ScrollToLastMessage();
			};
			MessagesListView.PreviewMouseRightButtonDown += MessagesListView_PreviewMouseRightButtonDown;
		}

		private void ScrollToLastMessage()
		{
			// Ждем обновления UI перед скроллом
			Dispatcher.BeginInvoke(() =>
			{
				if (MessagesListView.Items.Count > 0)
				{
					MessagesListView.ScrollIntoView(MessagesListView.Items[^1]); // Используем индекс от конца
				}
			}, System.Windows.Threading.DispatcherPriority.Background);
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

			// Список популярных эмодзи для реакций
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
			// Добавляем кнопку редактирования (только для редактируемых текстовых сообщений)
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

			// Добавляем кнопку ответа
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

			// Основные опции удаления
			if (isMyMessage)
			{
				AddDeleteOptionsForMyMessage(contextMenu, viewModel, message);
			}
			else if (isReceivedMessage)
			{
				AddDeleteOptionsForReceivedMessage(contextMenu, viewModel, message);
			}

			// Опции в зависимости от типа сообщения
			AddMessageTypeSpecificOptions(contextMenu, viewModel, message);

			// Общие опции
			AddCommonOptions(contextMenu, viewModel, message);

			// Показываем меню
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
				Style = (Style)FindResource("DeleteMenuItemStyle") // Применяем стиль
			};

			var deleteForEveryoneItem = new MenuItem
			{
				Header = "🚫 Удалить для всех",
				Command = viewModel.DeleteMessageForEveryoneCommand,
				CommandParameter = message,
				ToolTip = "Сообщение удалится у всех участников чата",
				Style = (Style)FindResource("DeleteForAllMenuItemStyle") // Применяем стиль
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
				Style = (Style)FindResource("DeleteMenuItemStyle") // Применяем стиль
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
			// УБЕРИТЕ отсюда блок с редактированием - он уже добавлен выше
		}

		private void AddCommonOptions(ContextMenu contextMenu, ChatViewModel viewModel, Message message)
		{
			var copyTimeItem = new MenuItem
			{
				Header = "⏰ Скопировать время",
				Command = viewModel.CopyTimeCommand,
				CommandParameter = message.Timestamp,
				ToolTip = "Скопировать время отправки",
				Style = (Style)FindResource("ActionMenuItemStyle") // Применяем стиль
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
					// Прокручиваем к сообщению
					MessagesListView.ScrollIntoView(message);

					// Даем время для рендеринга
					Dispatcher.BeginInvoke(() =>
					{
						var item = MessagesListView.ItemContainerGenerator.ContainerFromItem(message) as ListViewItem;
						if (item != null)
						{
							// Сохраняем оригинальный фон
							var originalBackground = item.Background;

							// Создаем анимацию подсветки
							var highlightBrush = new SolidColorBrush(Colors.Yellow);
							item.Background = highlightBrush;

							// Анимация плавного исчезновения
							var animation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(2));
							animation.Completed += (s, e) =>
							{
								// Возвращаем оригинальный фон после анимации
								item.Background = originalBackground;
							};

							highlightBrush.BeginAnimation(Brush.OpacityProperty, animation);

							// Фокусируем на элементе
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

		//private void MessagesListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
		//{
		//	var listView = sender as ListView;
		//	if (listView == null) return;

		//	// Получаем элемент под курсором мыши
		//	var originalSource = e.OriginalSource as FrameworkElement;
		//	var message = originalSource?.DataContext as Message;

		//	if (message == null) return;

		//	// Создаем контекстное меню
		//	var contextMenu = new ContextMenu();

		//	// Проверяем, является ли сообщение нашим
		//	var viewModel = DataContext as ChatViewModel;
		//	if (viewModel == null) return;

		//	bool isMyMessage = message.SenderId == viewModel.CurrentUserId;

		//	if (isMyMessage)
		//	{
		//		// Добавляем пункты меню только для своих сообщений
		//		var deleteForMeItem = new MenuItem
		//		{
		//			Header = "Удалить для меня",
		//			Command = viewModel.DeleteMessageForMeCommand,
		//			CommandParameter = message
		//		};

		//		var deleteForEveryoneItem = new MenuItem
		//		{
		//			Header = "Удалить для всех",
		//			Command = viewModel.DeleteMessageForEveryoneCommand,
		//			CommandParameter = message
		//		};

		//		contextMenu.Items.Add(deleteForMeItem);
		//		contextMenu.Items.Add(deleteForEveryoneItem);
		//	}

		//	// Устанавливаем контекстное меню для ListView
		//	listView.ContextMenu = contextMenu;
		//	contextMenu.IsOpen = true;

		//	e.Handled = true;
		//}
	}
}
