using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MusicMessage.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using MusicMessage.Models;
using System.Windows;
using Microsoft.Win32;
namespace MusicMessage.ViewModels
{
	public partial class EditProfileViewModel : ObservableObject
	{
		private readonly IProfileRepository _profileRepository;
		private readonly IAuthService _authService;
		private readonly User _originalUser;
		[ObservableProperty]
		private bool _isCoverEditingMode;
		[ObservableProperty]
		private bool _isAvatarEditingMode;

		[ObservableProperty]
		private string _selectedAvatarPath;

		public event Action<string> OnCoverPathChanged;
		private string _selectedCoverPath;
		public string SelectedCoverPath
		{
			get => _selectedCoverPath;
			set
			{
				if (SetProperty(ref _selectedCoverPath, value))
				{
					Console.WriteLine($"SelectedCoverPath changed to: {value}");

					if (!string.IsNullOrEmpty(value))
					{
						Console.WriteLine($"New path exists: {File.Exists(value)}");
					}

					if (IsCoverEditingMode && !string.IsNullOrEmpty(value) && File.Exists(value))
					{
						// Здесь можно вызвать загрузку
						OnCoverPathChanged?.Invoke(value);
					}
				}
			}
		}


		[ObservableProperty]
		private User _editedUser;

		[ObservableProperty]
		private BitmapImage _avatarImage;
		[ObservableProperty]
		private BitmapImage _coverImage;

		public EditProfileViewModel(IProfileRepository profileRepository,
								  IAuthService authService)
		{
			_profileRepository = profileRepository;
			_authService = authService;

			// Сохраняем оригинал и создаем копию для редактирования
			_originalUser = _authService.CurrentUser;
			EditedUser = _originalUser.Clone();
			
			LoadImages();
		}

		private async void LoadAvatar()
		{
			if (!string.IsNullOrEmpty(EditedUser.AvatarPath))
			{
				AvatarImage = await LoadImageAsync(EditedUser.AvatarPath);
			}
		}


		[RelayCommand]
		private async void SaveCroppedAvatar(string croppedImagePath)
		{
			try
			{
				var fileName = $"avatar_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(croppedImagePath)}";
				var destinationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Avatars", fileName);

				Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
				File.Copy(croppedImagePath, destinationPath, true);

				EditedUser.AvatarPath = fileName;
				IsAvatarEditingMode = false;

				// ОБНОВЛЯЕМ изображение
				await LoadAvatarImageAsync();
				OnPropertyChanged(nameof(AvatarImage));

				MessageBox.Show("Аватарка успешно обновлена!");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при сохранении аватарки: {ex.Message}");
			}
		}
		private async Task UpdateAvatarImageAsync(string imagePath)
		{
			try
			{
				var image = await LoadImageAsync(imagePath);
				AvatarImage = image;

				// Принудительно обновляем привязку
				OnPropertyChanged(nameof(AvatarImage));
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка обновления аватарки: {ex.Message}");
			}
		}
		[RelayCommand]
		private void CancelAvatarEdit()
		{
			IsAvatarEditingMode = false;
			SelectedAvatarPath = null;
		}

		

		[RelayCommand]
		private void ChangeAvatar()
		{
			var openFileDialog = new OpenFileDialog
			{
				Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
				Title = "Выберите изображение для аватарки"
			};

			if (openFileDialog.ShowDialog() == true)
			{
				SelectedAvatarPath = openFileDialog.FileName;
				IsAvatarEditingMode = true;
			}
		}
		[RelayCommand]
		private void ChangeCover()
		{
			var openFileDialog = new OpenFileDialog
			{
				Filter = "Image files (*.jpg; *.jpeg; *.png)|*.jpg; *.jpeg; *.png|All files (*.*)|*.*",
				Title = "Выберите изображение для обложки"
			};

			if (openFileDialog.ShowDialog() == true)
			{
				try
				{
					var selectedPath = openFileDialog.FileName;
					Console.WriteLine($"Selected file: {selectedPath}");
					Console.WriteLine($"File exists: {File.Exists(selectedPath)}");

					// Копируем файл во временную папку
					var tempDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp");
					Directory.CreateDirectory(tempDir);

					var tempFilePath = Path.Combine(tempDir, $"temp_cover_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(selectedPath)}");
					File.Copy(selectedPath, tempFilePath, true);

					SelectedCoverPath = tempFilePath;
					IsCoverEditingMode = true;

					// УБРАТЬ ЭТУ СТРОКУ - ViewModel не должна обращаться к View
					// CoverCropper?.LoadImage(tempFilePath);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Ошибка загрузки обложки: {ex.Message}");
					Console.WriteLine($"Error: {ex.Message}");
				}
			}
		}


		[RelayCommand]
		private void RemoveCover()
		{
			EditedUser.ProfileCoverPath = null;
			CoverImage = null;
			OnPropertyChanged(nameof(CoverImage));
			MessageBox.Show("Обложка удалена!");
		}

		private string ProcessCoverUpload(string filePath)
		{
			try
			{
				if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
				{
					MessageBox.Show("Файл для загрузки не найден");
					return null;
				}

				var coversDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Covers");
				Directory.CreateDirectory(coversDir);

				var fileName = $"cover_{_originalUser.UserId}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
				var destinationPath = Path.Combine(coversDir, fileName);

				// Удаляем старую обложку если есть
				if (!string.IsNullOrEmpty(_originalUser.ProfileCoverPath))
				{
					var oldCoverPath = Path.Combine(coversDir, _originalUser.ProfileCoverPath);
					if (File.Exists(oldCoverPath))
					{
						File.Delete(oldCoverPath);
					}
				}

				File.Copy(filePath, destinationPath, true);
				return fileName;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка загрузки обложки: {ex.Message}");
				return null;
			}
		}

		public async void LoadImages()
		{
			if (!string.IsNullOrEmpty(EditedUser.AvatarPath))
			{
				await LoadAvatarImageAsync();
			}

			if (!string.IsNullOrEmpty(EditedUser.ProfileCoverPath))
			{
				await LoadCoverImageAsync();
			}
		}
		private async Task LoadAvatarImageAsync()
		{
			if (!string.IsNullOrEmpty(EditedUser.AvatarPath))
			{
				AvatarImage = await LoadImageAsync(EditedUser.AvatarPath);
			}
		}

		private async Task LoadCoverImageAsync()
		{
			if (!string.IsNullOrEmpty(EditedUser.ProfileCoverPath))
			{
				CoverImage = await LoadImageAsync(EditedUser.ProfileCoverPath);
			}
		}
		[RelayCommand]
		private void RemoveAvatar()
		{
			EditedUser.AvatarPath = null;
			AvatarImage = null;
			OnPropertyChanged(nameof(AvatarImage));
			MessageBox.Show("Аватарка удалена!");
		}

		[RelayCommand]
		private async Task SaveProfile()
		{
			try
			{
				// ВАЛИДАЦИЯ
				if (!ValidateUserData()) return;

				// ДЕБАГ: проверяем значения перед сохранением
				

				await _profileRepository.UpdateUserProfileAsync(EditedUser);

				// Обновляем текущего пользователя
				_originalUser.FirstName = EditedUser.FirstName;
				_originalUser.LastName = EditedUser.LastName;
				_originalUser.DateOfBirth = EditedUser.DateOfBirth;
				_originalUser.Gender = EditedUser.Gender;
				_originalUser.City = EditedUser.City;
				_originalUser.Country = EditedUser.Country;
				_originalUser.Bio = EditedUser.Bio;
				_originalUser.AvatarPath = EditedUser.AvatarPath;
				_originalUser.ProfileCoverPath = EditedUser.ProfileCoverPath; // ДОБАВЬТЕ

				

				MessageBox.Show("Профиль успешно сохранен!");
				OnProfileSaved?.Invoke();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка сохранения: {ex.Message}\n\n{ex.InnerException?.Message}");
			}
		}
		private bool ValidateUserData()
		{
			// Проверка длины полей
			if (!string.IsNullOrEmpty(EditedUser.Gender) && EditedUser.Gender.Length > 10)
			{
				MessageBox.Show("Значение поля 'Пол' слишком длинное. Максимум 10 символов.");
				return false;
			}

			if (!string.IsNullOrEmpty(EditedUser.FirstName) && EditedUser.FirstName.Length > 100)
			{
				MessageBox.Show("Имя слишком длинное. Максимум 100 символов.");
				return false;
			}

			if (!string.IsNullOrEmpty(EditedUser.LastName) && EditedUser.LastName.Length > 100)
			{
				MessageBox.Show("Фамилия слишком длинная. Максимум 100 символов.");
				return false;
			}

			if (!string.IsNullOrEmpty(EditedUser.City) && EditedUser.City.Length > 100)
			{
				MessageBox.Show("Название города слишком длинное. Максимум 100 символов.");
				return false;
			}

			if (!string.IsNullOrEmpty(EditedUser.Country) && EditedUser.Country.Length > 100)
			{
				MessageBox.Show("Название страны слишком длинное. Максимум 100 символов.");
				return false;
			}

			if (!string.IsNullOrEmpty(EditedUser.Bio) && EditedUser.Bio.Length > 500)
			{
				MessageBox.Show("Описание слишком длинное. Максимум 500 символов.");
				return false;
			}

			return true;
		}
		[RelayCommand]
		private void Cancel()
		{
			OnCancelled?.Invoke();
		}
		[RelayCommand]
		private async void SaveCroppedCover(string croppedImagePath)
		{
			try
			{
				var fileName = $"cover_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(croppedImagePath)}";
				var destinationPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Covers", fileName);

				Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
				File.Copy(croppedImagePath, destinationPath, true);

				EditedUser.ProfileCoverPath = fileName;
				IsCoverEditingMode = false;

				// ОБНОВЛЯЕМ изображение
				await LoadCoverImageAsync();
				OnPropertyChanged(nameof(CoverImage));

				MessageBox.Show("Обложка успешно обновлена!");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при сохранении обложки: {ex.Message}");
			}
		}

		[RelayCommand]
		private void CancelCoverEdit()
		{
			IsCoverEditingMode = false;
		}
		//private string ProcessAvatarUpload(string filePath)
		//{
		//	// Создаем папку для аватаров если нет
		//	var avatarsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Avatars");
		//	Directory.CreateDirectory(avatarsDir);

		//	// Генерируем уникальное имя файла
		//	var fileName = $"avatar_{_originalUser.UserId}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(filePath)}";
		//	var destinationPath = Path.Combine(avatarsDir, fileName);

		//	// Копируем файл
		//	File.Copy(filePath, destinationPath, true);

		//	// Возвращаем ТОЛЬКО имя файла, без пути
		//	return fileName;
		//}

		private async Task<BitmapImage> LoadImageAsync(string fileName)
		{
			return await Task.Run(() =>
			{
				try
				{
					if (string.IsNullOrEmpty(fileName)) return null;

					var possiblePaths = new[]
					{
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Avatars", fileName),
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Covers", fileName),
					Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
					fileName
				};

					foreach (var fullPath in possiblePaths)
					{
						if (File.Exists(fullPath))
						{
							var bitmap = new BitmapImage();
							bitmap.BeginInit();
							bitmap.UriSource = new Uri(fullPath);
							bitmap.CacheOption = BitmapCacheOption.OnLoad;
							bitmap.EndInit();
							bitmap.Freeze();
							return bitmap;
						}
					}
					return null;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"LoadImageAsync error: {ex.Message}");
					return null;
				}
			});
		}

		public event Action OnProfileSaved;
		public event Action OnCancelled;
	}
}
