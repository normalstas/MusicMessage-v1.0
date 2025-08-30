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
	/// Логика взаимодействия для LoginUserControl.xaml
	/// </summary>
	public partial class LoginUserControl : UserControl
	{
		public LoginUserControl()
		{
			InitializeComponent();
			Loaded += LoginUserControl_Loaded;
		}

		private void LoginUserControl_Loaded(object sender, RoutedEventArgs e)
		{
			// Подписываемся на события изменения пароля после загрузки контрола
			LoginPasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
			RegisterPasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
		}

		private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			if (DataContext is ViewModels.LoginViewModel viewModel)
			{
				viewModel.Password = ((PasswordBox)sender).Password;
			}
		}

		// Очищаем пароль при переключении между режимами
		private void UpdatePasswordBoxes()
		{
			if (DataContext is ViewModels.LoginViewModel viewModel)
			{
				LoginPasswordBox.Password = viewModel.Password;
				RegisterPasswordBox.Password = viewModel.Password;
			}
		}

		// Можно добавить обработчик изменения DataContext для обновления паролей
		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.Property == DataContextProperty && e.NewValue is ViewModels.LoginViewModel)
			{
				UpdatePasswordBoxes();
			}
		}
	}
}
