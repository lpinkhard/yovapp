using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;
using Xamarin.Essentials;
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

            LoadMessageHistory();

            SendMessageCommand = new Command(ExecuteSendMessageCommand);
            LoadMessagesCommand = new Command(async () =>
                await ExecuteLoadMessagesCommand());
        }

        void LoadMessageHistory()
        {
            string messageList = Preferences.Get("messages_" + Contact.Username, "");

            try
            {
                XmlReader messageReader = XmlReader.Create(new StringReader(messageList));
                DataContractSerializer serializer =
                new DataContractSerializer(typeof(ObservableCollection<Message>));
                Messages = (ObservableCollection<Message>)serializer.ReadObject(messageReader);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        void SaveMessageHistory()
        {
            try
            {
                MemoryStream messageData = new MemoryStream();
                DataContractSerializer serializer = new
                            DataContractSerializer(typeof(ObservableCollection<Message>));
                serializer.WriteObject(messageData, Messages);

                messageData.Seek(0, SeekOrigin.Begin);
                StreamReader messageReader = new StreamReader(messageData);

                string messageList = messageReader.ReadToEnd();
                Preferences.Set("messages_" + Contact.Username, messageList);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
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

            SaveMessageHistory();

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

                    SaveMessageHistory();
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
