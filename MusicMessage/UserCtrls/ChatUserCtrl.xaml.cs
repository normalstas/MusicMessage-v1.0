using MusicMessage.Models;
using MusicMessage.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
			var context = new MessangerBaseContext();
			var repository = new MessageRepository(context);
			DataContext = new ChatViewModel(repository);

			// Подписка на изменение коллекции сообщений
			var vm = (ChatViewModel)DataContext;
			vm.Messages.CollectionChanged += (s, e) =>
			{
				if (e.NewItems?.Count > 0)
					ScrollToLastMessage();
			};
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
	}
}
