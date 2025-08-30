using MusicMessage.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using MusicMessage.Models;

namespace MusicMessage.Converter
{
	public class MessageContainerStyleSelector : StyleSelector
	{
		public Style MyMessageStyle { get; set; }
		public Style OtherMessageStyle { get; set; }

		public override Style SelectStyle(object item, DependencyObject container)
		{
			if (item is Message message && container is FrameworkElement element)
			{
				var viewModel = element.DataContext as ChatViewModel;
				if (viewModel != null)
				{
					return message.SenderId == viewModel.CurrentUserId ? MyMessageStyle : OtherMessageStyle;
				}
			}
			return base.SelectStyle(item, container);
		}
	}
}
