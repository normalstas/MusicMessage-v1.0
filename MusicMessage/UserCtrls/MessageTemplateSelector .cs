using MusicMessage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace MusicMessage.UserCtrls
{
	public class MessageTemplateSelector : DataTemplateSelector
	{
		public DataTemplate TextTemplate { get; set; }
		public DataTemplate VoiceTemplate { get; set; }
		public DataTemplate StickerTemplate { get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			if (item is Message message)
			{
				return message.MessageType switch
				{
					"Text" => TextTemplate,
					"Voice" => VoiceTemplate,
					"Sticker" => StickerTemplate,
					_ => base.SelectTemplate(item, container)
				};
			}
			return null;
		}
	}
}
