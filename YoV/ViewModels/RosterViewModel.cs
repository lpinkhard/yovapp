using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;
using Xamarin.Essentials;
using Xamarin.Forms;
using YoV.Helpers;
using YoV.Models;
using YoV.Views;

namespace YoV.ViewModels
{
    public class RosterViewModel : BaseViewModel
    {
        public ObservableCollection<ContactCircle> Contacts { get; set; }
        public Command LoadRosterCommand { get; set; }

        public bool RosterLoaded { get; set; }

        public RosterViewModel()
        {
            Title = "Contacts";
            Contacts = new ObservableCollection<ContactCircle>();

            RosterLoaded = false;

            LoadContactCache();

            LoadRosterCommand = new Command(async () => await ExecuteLoadRosterCommand());

            MessagingCenter.Subscribe<NewContactPage, ContactEntry>(this, "AddContact", (obj, item) =>
            {
                ContactEntry newContact = item;

                newContact.Contact.PhoneNumber =
                    (string) new PhoneDisplayConverter().ConvertBack(
                    newContact.Contact.PhoneNumber, null, null, null);

                for (int i = 0; i < Contacts.Count; i++)
                {
                    if (Contacts[i].Name.Equals(newContact.CircleName))
                    {
                        Contacts[i].Add(newContact.Contact);
                        XMPP.AddContactAsync(newContact);
                        return;
                    }
                }

                List<Contact> newList = new List<Contact>();
                newList.Add(newContact.Contact);
                Contacts.Add(new ContactCircle(newContact.CircleName, newList));
                XMPP.AddContactAsync(newContact);
            });
        }

        void LoadContactCache()
        {
            string contactList = Preferences.Get("contacts", "");

            try
            {
                XmlReader contactReader = XmlReader.Create(new StringReader(contactList));
                DataContractSerializer serializer =
                new DataContractSerializer(typeof(ObservableCollection<ContactCircle>));
                Contacts = (ObservableCollection<ContactCircle>)serializer.ReadObject(contactReader);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        internal void UpdateContactState(NotificationEventArgs notification)
        {
            for (int i = 0; i < Contacts.Count; i++)
            {
                for (int j = 0; j < Contacts[i].Count; j++)
                {
                    if (Contacts[i][j].DisplayName.Equals(notification.Title))
                    {
                        Contacts[i][j].NewMessages = true;
                    }
                }
            }
        }

        void SaveContactCache()
        {
            try
            {
                MemoryStream contactData = new MemoryStream();
                DataContractSerializer serializer = new
                            DataContractSerializer(typeof(ObservableCollection<ContactCircle>));
                serializer.WriteObject(contactData, Contacts);

                contactData.Seek(0, SeekOrigin.Begin);
                StreamReader contactReader = new StreamReader(contactData);

                string contactList = contactReader.ReadToEnd();
                Preferences.Set("contacts", contactList);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        async Task ExecuteLoadRosterCommand()
        {
            IsBusy = true;

            try
            {
                Contacts.Clear();
                var contacts = await XMPP.GetRosterAsync();
                foreach (var contact in contacts)
                {
                    Contacts.Add(contact);
                }

                Task<bool> result = XMPP.DidGetRoster();
                RosterLoaded = result.Result;

                SaveContactCache();
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