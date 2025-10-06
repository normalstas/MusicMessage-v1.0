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
	public partial class LoginUserControl : UserControl
	{
		public LoginUserControl()
		{
			InitializeComponent();
			Loaded += LoginUserControl_Loaded;
		}

		private void LoginUserControl_Loaded(object sender, RoutedEventArgs e)
		{
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

		private void UpdatePasswordBoxes()
		{
			if (DataContext is ViewModels.LoginViewModel viewModel)
			{
				LoginPasswordBox.Password = viewModel.Password;
				RegisterPasswordBox.Password = viewModel.Password;
			}
		}

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
