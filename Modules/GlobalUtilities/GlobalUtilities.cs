using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Data;
using System.Management;
using System.Security.Cryptography;
using System.IO;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.Script.Serialization;
using System.Net;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using System.Xml;
using System.Drawing;
using Newtonsoft.Json;
using Microsoft.Owin;

namespace RaaiVan.Modules.GlobalUtilities
{
    public class ListMaker
    {
        protected static void _get_string_items(ref string input, ref List<string> lst, char delimiter)
        {
            //if (!string.IsNullOrEmpty(input)) input = input.Trim(); -> In Base64 strings if the end character is '+' it has been replaced with space character and trimming will damage its data
            if (string.IsNullOrEmpty(input)) return;
            lst = input.Split(delimiter).ToList();
        }

        public static List<string> get_string_items(string input, char delimiter)
        {
            List<string> lst = new List<string>();
            _get_string_items(ref input, ref lst, delimiter);
            return lst;
        }
    }

    public static class Base64
    {
        public static bool decode(string sourceString, ref string returnString)
        {
            try
            {
                sourceString = sourceString.Replace(' ', '+');

                UTF8Encoding encoder = new UTF8Encoding();
                Decoder utf8Decode = encoder.GetDecoder();

                byte[] toDecodeByte = Convert.FromBase64String(sourceString);
                int charCount = utf8Decode.GetCharCount(toDecodeByte, 0, toDecodeByte.Length);
                char[] decodeChar = new char[charCount];
                utf8Decode.GetChars(toDecodeByte, 0, toDecodeByte.Length, decodeChar, 0);

                returnString = new String(decodeChar);

                return true;
            }
            catch
            {
                returnString = sourceString;
                return false;
            }
        }

        public static string decode(string returnString)
        {
            if (string.IsNullOrEmpty(returnString)) return string.Empty;
            decode(returnString, ref returnString);
            return returnString;
        }
    }

    public static class Expressions
    {
        public enum Patterns
        {
            Tag,
            AutoTag,
            AdditionalID,
            HTMLTag
        }

        private static string _get_pattern(Patterns pattern)
        {
            switch (pattern.ToString().ToLower())
            {
                case "tag":
                    return @"(@)\[\[([a-zA-Z\d\-_]+):([\w\s\.\-]+):([0-9a-zA-Z\+\/\=]+)(:([0-9a-zA-Z\+\/\=]*))?\]\]";
                case "autotag":
                    return @"(~)\[\[([:\-\w]+)\]\]";
                case "additionalid":
                    return @"^([A-Za-z0-9_\-\/]|(~\[\[(((RND|(NCountS?(PY|GY)?))\d?)|[PG](Year|YearS|Month|Day)|(FormField:[A-Za-z0-9\-]{36})|(AreaID)|(DepID))\]\]))+$";
                case "htmltag":
                    return @"<.*?>";
                default:
                    return string.Empty;
            }
        }

        public static string replace(string input, Patterns pattern, string replacement = " ")
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (pattern == Patterns.HTMLTag) input = HttpUtility.HtmlDecode(input);
            return Regex.Replace(input, _get_pattern(pattern), replacement);
        }
    }

    public static class PublicMethods
    {   
        public static int? parse_int(string input, int? defaultValue = null)
        {
            if (string.IsNullOrEmpty(input)) return defaultValue;
            int retVal = 0;
            if (!int.TryParse(input, out retVal)) return defaultValue;
            return retVal;
        }

        public static double? parse_double(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            double retVal = 0;
            if (!double.TryParse(input, out retVal)) return null;
            return retVal;
        }

        public static bool? parse_bool(string input, bool? defaultValue = null)
        {
            if (string.IsNullOrEmpty(input)) return defaultValue;
            else if (input.ToLower() == "true" || input == "1") return true;
            else if (input.ToLower() == "false" || input == "0") return false;
            else return defaultValue;
        }

        public static string parse_string(string input, bool decode = true, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(input)) return defaultValue;
            return decode ? Base64.decode(input) : input;
        }
        
        private static Random _RND = new Random((int)DateTime.Now.Ticks);

        public static int get_random_number(int min, int max)
        {
            return _RND.Next(min, max + 1);
        }

        public static int get_random_number(int length = 5)
        {
            return get_random_number((int)Math.Pow(10, (double)length - 1), (int)Math.Pow(10, (double)length) - 1);
        }

        public static string verify_string(string str, bool removeHtmlTags = true)
        {
            if (removeHtmlTags && !string.IsNullOrEmpty(str)) str = Expressions.replace(str, Expressions.Patterns.HTMLTag, " ");
            return string.IsNullOrEmpty(str) ? str : str.Replace('ي', 'ی').Replace('ك', 'ک');
        }

        public static Dictionary<string, object> fromJSON(string json)
        {
            if (string.IsNullOrEmpty(json)) return new Dictionary<string, object>();
            try { return new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json); }
            catch { return new Dictionary<string, object>(); }
        }

        public static string map_path(string path)
        {
            return System.Web.Hosting.HostingEnvironment.MapPath(path);
        }
        
        public static ITenant get_current_tenant(IOwinRequest request, List<ITenant> tenants)
        {
            if (tenants.Count == 1) return tenants.First();
            
            string host = request.Uri.Host;
            int port = request.Uri.Port;
            string hostPort = host + (port > 0 ? ":" + port.ToString() : string.Empty);

            ITenant tnt = tenants.FirstOrDefault(x => x.Domain == hostPort);

            return tnt != null ? tnt : tenants.FirstOrDefault(x => x.Domain == host);
        }

        public static void set_timeout(Action action, int delay) {
            Thread th = new Thread(() =>
            {
                if (delay > 0) Thread.Sleep(delay);
                action.Invoke();
            });

            th.Priority = ThreadPriority.BelowNormal;

            th.Start();
        }
        
        public static void split_list<T>(List<T> lst, int partLength, Action<List<T>> action, int gap = 0)
        {
            int i = 0;
            int count = lst == null ? 0 : lst.Count;

            while (i * partLength < count)
            {
                List<T> newList = new List<T>();

                for (int x = 0, bs = partLength * i; x < partLength && x + bs < count; ++x)
                    newList.Add(lst[x + bs]);

                action(newList);
                
                ++i;

                if (gap > 0) Thread.Sleep(gap);
            }
        }
    }
}
