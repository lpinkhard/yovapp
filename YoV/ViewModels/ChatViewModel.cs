using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;
using YoV.Models;

namespace YoV.ViewModels
{
    public class ChatViewModel : BaseViewModel
    {
        public Contact Contact { get; set; }
        public ObservableCollection<Message> Messages { get; set; }
        public string TextToSend { get; set; }
        public Command SendMessageCommand { get; set; }
        public Command LoadMessagesCommand { get; set; }

        public ChatViewModel(Contact contact = null)
        {
            Title = contact?.DisplayName;
            Contact = contact;
            Messages = new ObservableCollection<Message>();
            SendMessageCommand = new Command(ExecuteSendMessageCommand);
            LoadMessagesCommand = new Command(async () =>
                await ExecuteLoadMessagesCommand());
        }

        public void ExecuteSendMessageCommand()
        {
            Message newMessage = new Message
            {
                Content = TextToSend,
                User = Contact.Username,
                Direction = Message.MessageDirection.OUTGOING
            };

            TextToSend = string.Empty;

            XMPP.SendMessageAsync(newMessage);
            Messages.Add(newMessage);

            OnPropertyChanged("TextToSend");
        }

        async Task ExecuteLoadMessagesCommand()
        {
            IsBusy = true;

            try
            {
                int oldCount = Messages.Count;
                var messages = await XMPP.GetMessagesAsync(Contact);
                if (messages.Count != oldCount)
                {
                    foreach (var msg in messages)
                    {
                        if (!Messages.Contains(msg))
                            Messages.Add(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

    }
}
