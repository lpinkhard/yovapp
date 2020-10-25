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
using YoV.Views;

namespace YoV.ViewModels
{
    public class RosterViewModel : BaseViewModel
    {
        public ObservableCollection<Contact> Contacts { get; set; }
        public Command LoadRosterCommand { get; set; }
        public Command UpdateStateCommand { get; set; }

        private bool rosterLoaded;

        public RosterViewModel()
        {
            Title = "Contacts";
            Contacts = new ObservableCollection<Contact>();

            rosterLoaded = false;

            LoadContactCache();

            LoadRosterCommand = new Command(async () => await ExecuteLoadRosterCommand());
            UpdateStateCommand = new Command(async () => await ExecuteUpdateStateCommand());

            MessagingCenter.Subscribe<NewContactPage, Contact>(this, "AddContact", (obj, item) =>
            {
                Contact newContact = item;
                Contacts.Add(newContact);
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
                new DataContractSerializer(typeof(ObservableCollection<Contact>));
                Contacts = (ObservableCollection<Contact>)serializer.ReadObject(contactReader);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        void SaveContactCache()
        {
            try
            {
                MemoryStream contactData = new MemoryStream();
                DataContractSerializer serializer = new
                            DataContractSerializer(typeof(ObservableCollection<Contact>));
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
                rosterLoaded = result.Result;

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

        async Task ExecuteUpdateStateCommand()
        {
            if (!rosterLoaded)
            {
                await ExecuteLoadRosterCommand();
            }
        }
    }
}