using System.Collections.Generic;

namespace YoV.Models
{
    public class ContactCircle : List<Contact>
    {
        public string Name { get; private set; }

        public ContactCircle() : base()
        {
            Name = "Default";
        }

        public ContactCircle(string name, List<Contact> contacts) : base(contacts)
        {
            Name = name;
        }
    }
}
