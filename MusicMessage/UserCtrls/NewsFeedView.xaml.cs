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
    public partial class NewsFeedView : UserControl
    {
        public NewsFeedView()
        {
            InitializeComponent();
			Loaded += async (s, e) =>
			{
				if (DataContext is ViewModels.NewsFeedViewModel vm)
				{
					await vm.LoadNewsFeedAsync();
				}
			};
		}
    }
}
