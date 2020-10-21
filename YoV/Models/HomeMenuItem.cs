using System;
using System.Collections.Generic;
using System.Text;

namespace YoV.Models
{
    public enum MenuItemType
    {
        Chat,
        About
    }
    public class HomeMenuItem
    {
        public MenuItemType Id { get; set; }

        public string Title { get; set; }
    }
}
