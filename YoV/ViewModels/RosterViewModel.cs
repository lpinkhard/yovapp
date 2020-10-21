using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

using Xamarin.Forms;

using YoV.Models;
using YoV.Views;

namespace YoV.ViewModels
{
    public class RosterViewModel : BaseViewModel
    {
        public ObservableCollection<Contact> Contacts { get; set; }
        public Command LoadRosterCommand { get; set; }

        public RosterViewModel()
        {
            Title = "Contacts";
            Contacts = new ObservableCollection<Contact>();
            LoadRosterCommand = new Command(async () => await ExecuteLoadRosterCommand());

            MessagingCenter.Subscribe<NewContactPage, Contact>(this, "AddContact", (obj, item) =>
            {
                Contact newContact = item;
                Contacts.Add(newContact);
                XMPP.AddContactAsync(newContact);
            });
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