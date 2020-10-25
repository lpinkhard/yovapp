using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Xamarin.Forms;

namespace YoV.Helpers
{
    public class PhoneDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string phoneValue = (string) value;
            Regex cleanE164 = new Regex(@"[^\d]");
            string phone = cleanE164.Replace(phoneValue, "");

            StringBuilder output = new StringBuilder();

            int countryCodeLen = 0;
            foreach (string countryTest in validCountries)
            {
                if (phone.StartsWith(countryTest))
                {
                    countryCodeLen = countryTest.Length;
                    break;
                }
            }

            if (countryCodeLen == 0)
                return "";

            string countryCode = phone.Substring(0, countryCodeLen);
            output.Append(countryCode + " ");

            string internalNumber = phone.Substring(countryCodeLen);

            int internalLength = internalNumber.Length;

            if (internalLength > 15)
                return "";  // Not valid

            int index = 0;
            if (internalLength % 3 == 0)
            {
                while (internalLength - index > 0)
                {
                    output.Append(internalNumber.Substring(index, 3) + " ");
                    index += 3;
                }
            }
            else if (internalLength % 4 == 0)
            {
                while (internalLength - index > 0)
                {
                    output.Append(internalNumber.Substring(index, 4) + " ");
                    index += 4;
                }
            }
            else if ((internalLength - 3) % 4 == 0)
            {
                output.Append(internalNumber.Substring(0, 3) + " ");
                index = 3;
                while (internalLength - index > 0)
                {
                    output.Append(internalNumber.Substring(index, 4) + " ");
                    index += 4;
                }
            }
            else if ((internalLength - 2) % 4 == 0)
            {
                output.Append(internalNumber.Substring(0, 2) + " ");
                index = 2;
                while (internalLength - index > 0)
                {
                    output.Append(internalNumber.Substring(index, 4) + " ");
                    index += 4;
                }
            }
            else if (internalLength % 2 == 0)
            {
                while (internalLength - index > 0)
                {
                    output.Append(internalNumber.Substring(index, 2) + " ");
                    index += 2;
                }
            }
            else if ((internalLength - 3) % 2 == 0)
            {
                output.Append(internalNumber.Substring(0, 3) + " ");
                index = 3;
                while (internalLength - index > 0)
                {
                    output.Append(internalNumber.Substring(index, 2) + " ");
                    index += 2;
                }
            }
            else
            {
                output.Append(internalNumber);
            }

            return output.ToString().TrimEnd();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string phoneValue = (string) value;
            Regex cleanE164 = new Regex(@"[^\d]");
            return cleanE164.Replace(phoneValue, "");
        }

        private static string[] validCountries = {
            // USA, Canada & Carribean
            "1",
            // Africa, Greenland, Aruba, Faroe Islands &
            // British Indian Ocean Territory
            "20", "211", "212", "213", "216", "218", "220", "221", "222",
            "221", "222", "223", "224", "225", "226", "227", "228", "229",
            "230", "231", "232", "233", "234", "235", "236", "237", "238",
            "239", "240", "241", "242", "243", "244", "245", "246", "247",
            "248", "249", "250", "251", "252", "253", "254", "255", "256",
            "257", "258", "260", "261", "262", "263", "264", "265", "266",
            "267", "268", "269", "27", "290", "291", "297", "298", "299",
            // Europe
            "30", "31", "32", "33", "34", "350", "351", "352", "353", "354",
            "355", "356", "357", "358", "359", "36", "370", "371", "372",
            "373", "374", "375", "376", "377", "378", "379", "380", "381",
            "382", "383", "385", "386", "387", "389", "39", "40", "41", "420",
            "421", "423", "43", "44", "45", "46", "47", "48", "49",
            // Americas
            "500", "501", "502", "503", "504", "505", "506", "507", "508",
            "509", "51", "52", "53", "54", "55", "56", "57", "58", "590",
            "591", "592", "593", "594", "595", "596", "597", "598", "599",
            // Southeast Asia & Oceania
            "60", "61", "62", "63", "64", "65", "66", "670", "672", "673",
            "674", "675", "676", "677", "678", "679", "680", "681", "682",
            "683", "685", "686", "687", "688", "689", "690", "691", "692",
            // Former Soviet Union
            "7",
            // East Asia
            "81", "82", "84", "850", "852", "853", "855", "856", "86", "880",
            "886",
            // Middle East & Southern Asia
            "90", "91", "92", "93", "94", "95", "960", "961", "962", "963",
            "964", "965", "966", "967", "968", "970", "971", "972", "973",
            "974", "975", "976", "977", "98", "992", "993", "994", "995",
            "996", "998"
        };
    }
}
