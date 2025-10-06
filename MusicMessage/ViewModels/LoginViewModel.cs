using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicMessage.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using MusicMessage.Models;
using MusicMessage.UserCtrls;
using MusicMessage.ViewModels;
using System.Windows;
namespace MusicMessage.ViewModels
{
	public partial class LoginViewModel : ObservableObject
	{
		private readonly IAuthService _authService;

		[ObservableProperty]
		private string _username;

		[ObservableProperty]
		private string _password;

		[ObservableProperty]
		private string _email;
		[ObservableProperty]
		private string _firstname;
		[ObservableProperty]
		private string _lastname;
		[ObservableProperty]
		private bool _isLoginMode = true;

		public event Action OnLoginSuccessful;
		[ObservableProperty]
		private bool _isLoading;
		public LoginViewModel(IAuthService authService)
		{
			_authService = authService;
		}

		[RelayCommand]
		private async Task Login()
		{
			try
			{
				if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
				{
					MessageBox.Show("Заполните все поля");
					return;
				}

				IsLoading = true;

				var user = await _authService.LoginAsync(Username, Password)
					.ConfigureAwait(true); 

				if (user != null)
				{
					OnLoginSuccessful?.Invoke();
				}
				else
				{
					MessageBox.Show("Неверное имя пользователя или пароль");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка входа: {ex.Message}");
			}
			finally
			{
				IsLoading = false;
			}
		}

		[RelayCommand]
		private async Task Register()
		{
			if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password)
				|| string.IsNullOrWhiteSpace(Email) ||
				(string.IsNullOrWhiteSpace(Firstname) || string.IsNullOrWhiteSpace(Lastname)))
			{
				MessageBox.Show("Заполните все поля");
				return;
			}

			var user = await _authService.RegisterAsync(Username, Email, Password, Firstname, Lastname);
			if (user != null)
			{
				MessageBox.Show("Регистрация успешна! Теперь вы можете войти.");
				IsLoginMode = true;
				Password = "";
			}
			else
			{
				MessageBox.Show("Пользователь с таким именем или email уже существует");
			}
		}

		[RelayCommand]
		private void SwitchToRegister() => IsLoginMode = false;

		[RelayCommand]
		private void SwitchToLogin() => IsLoginMode = true;
	}
}
