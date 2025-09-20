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
using System.IO;
namespace MusicMessage.UserCtrls
{
	public partial class EditProfileView : UserControl
	{
		public EditProfileView()
		{
			InitializeComponent();
			Loaded += EditProfileView_Loaded;
		}

		private void EditProfileView_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is EditProfileViewModel vm)
			{
				// Подписываемся на изменение режима редактирования
				vm.PropertyChanged += (s, args) =>
				{
					if (args.PropertyName == nameof(vm.IsCoverEditingMode) && vm.IsCoverEditingMode)
					{
						if (!string.IsNullOrEmpty(vm.SelectedCoverPath))
						{
							CoverCropper.LoadImage(vm.SelectedCoverPath);
						}
					}
					if (args.PropertyName == nameof(vm.IsAvatarEditingMode) && vm.IsAvatarEditingMode)
					{
						if (!string.IsNullOrEmpty(vm.SelectedAvatarPath))
						{
							AvatarCropper.LoadImage(vm.SelectedAvatarPath);
						}
					}
				};

				// События для ОБЛОЖКИ
				CoverCropper.OnImageCropped += (croppedPath) =>
				{
					vm.SaveCroppedCoverCommand.Execute(croppedPath);
				};
				CoverCropper.OnCancelled += () =>
				{
					vm.CancelCoverEditCommand.Execute(null);
				};

				// События для АВАТАРА
				AvatarCropper.OnImageCropped += (croppedPath) =>
				{
					vm.SaveCroppedAvatarCommand.Execute(croppedPath);
				};
				AvatarCropper.OnCancelled += () =>
				{
					vm.CancelAvatarEditCommand.Execute(null);
				};
			}
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.Property == DataContextProperty && DataContext is EditProfileViewModel vm)
			{
				// Для обложки
				if (vm.IsCoverEditingMode && !string.IsNullOrEmpty(vm.SelectedCoverPath))
				{
					Dispatcher.BeginInvoke(new Action(() =>
					{
						CoverCropper?.LoadImage(vm.SelectedCoverPath);
					}), System.Windows.Threading.DispatcherPriority.Loaded);
				}
				// Для аватарки
				if (vm.IsAvatarEditingMode && !string.IsNullOrEmpty(vm.SelectedAvatarPath))
				{
					Dispatcher.BeginInvoke(new Action(() =>
					{
						AvatarCropper?.LoadImage(vm.SelectedAvatarPath);
					}), System.Windows.Threading.DispatcherPriority.Loaded);
				}
			}
			base.OnPropertyChanged(e);
		}
	}

}
