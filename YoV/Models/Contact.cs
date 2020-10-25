using System;
using System.Diagnostics;
using Xamarin.Forms;

namespace YoV.Models
{
    public class Contact
    {
        public string DisplayName { get; set; }
        public string PhoneNumber { get; set; }
        public bool NewMessages { get; set; }
    }
}