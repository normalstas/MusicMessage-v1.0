using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MusicMessage.ViewModels;
namespace MusicMessage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(NavigationViewModel navigationViewModel)
        {
            InitializeComponent();
			DataContext = navigationViewModel;
		}

		private void Thumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
		{
			var newWidth = LeftColumn.Width.Value + e.HorizontalChange;
			newWidth = Math.Max(LeftColumn.MinWidth, Math.Min(LeftColumn.MaxWidth, newWidth));
			LeftColumn.Width = new GridLength(newWidth);
		}
	}
}