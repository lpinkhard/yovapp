using System;
using Xamarin.Forms;
using YoV.Models;
using YoV.Views.Cells;

namespace YoV.Helpers
{
    public class ChatTemplateSelector : DataTemplateSelector
    {
        DataTemplate incomingMessage;
        DataTemplate outgoingMessage;

        public ChatTemplateSelector()
        {
            incomingMessage = new DataTemplate(typeof(IncomingMessageViewCell));
            outgoingMessage = new DataTemplate(typeof(OutgoingMessageViewCell));
        }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            Message message = (Message) item;
            if (message != null)
            {
                switch (message.Direction)
                {
                    case Message.MessageDirection.INCOMING:
                        return incomingMessage;
                    case Message.MessageDirection.OUTGOING:
                        return outgoingMessage;
                }
            }

            return null;
        }
    }
}
