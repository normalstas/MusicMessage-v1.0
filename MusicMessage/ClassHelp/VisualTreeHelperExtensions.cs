using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace MusicMessage.ClassHelp
{
	public static class VisualTreeHelperExtensions
	{
		public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
		{
			if (depObj == null) yield break;

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
				if (child != null && child is T t)
				{
					yield return t;
				}

				foreach (T childOfChild in FindVisualChildren<T>(child))
				{
					yield return childOfChild;
				}
			}
		}
	}
}
