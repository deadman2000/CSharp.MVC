using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EmbeddedMVC
{
    public class HTTPRequestArgs
    {
        private static readonly CultureInfo CI = new CultureInfo("en-US", false);
        private static readonly Encoding ENC1251 = Encoding.GetEncoding(1251);
        private static readonly Encoding DEFAULT_ENCODING = new UTF8Encoding(false);

        private Dictionary<string, string> requestArgs = new Dictionary<string, string>();

        public HTTPRequestArgs(string keys)
        {
            string[] parts = keys.Split('&');
            foreach (string kvLine in parts)
            {
                string[] kv = kvLine.Split(new[] { '=' }, 2);
                if (kv.Length == 2)
                    requestArgs.Add(Uri.UnescapeDataString(kv[0]), Uri.UnescapeDataString(kv[1]));
            }
        }

        public HTTPRequestArgs(NameValueCollection keys)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys.AllKeys[i];
                if (key != null)
                    requestArgs.Add(key, DecodeUTF(keys[i]));
            }

            string noKey = keys[null];
            if (noKey != null)
            {
                string[] noKeys = noKey.Split(',');
                foreach (string k in noKeys)
                    requestArgs.Add(k, null);
            }
        }

        public override string ToString()
        {
            return string.Join("  ", requestArgs.Select(x => x.Key + "=" + x.Value).ToArray());
        }

        public string this[string key]
        {
            get
            {
                string val;
                if (requestArgs.TryGetValue(key, out val))
                    return val;
                throw new ArgumentException("Не задан параметр '" + key + "'");
            }
        }

        public bool Contains(string key)
        {
            return requestArgs.ContainsKey(key);
        }

        public DateTime GetDateTime(string key)
        {
            DateTime dt;
            if (!DateTime.TryParse(GetString(key), CultureInfo.CurrentCulture.DateTimeFormat, DateTimeStyles.AdjustToUniversal, out dt))
                throw new ArgumentException("Неверный формат даты '" + key + "'");
            return dt;
        }

        public DateTime GetDateTime(string key, DateTime def)
        {
            string val;
            if (!requestArgs.TryGetValue(key, out val)) return def;

            DateTime dt;
            if (!DateTime.TryParse(val, CultureInfo.CurrentCulture.DateTimeFormat, DateTimeStyles.AdjustToUniversal, out dt))
                throw new ArgumentException("Неверный формат даты '" + key + "'");
            return dt;
        }

        public TimeSpan GetTimeSpan(string key)
        {
            return TimeSpan.Parse(requestArgs[key]);
        }

        // Int

        public int GetInt(string key)
        {
            string str;
            if (requestArgs.TryGetValue(key, out str))
                return int.Parse(str);
            throw new ArgumentException("Не задан параметр '" + key + "'");
        }

        public int GetInt(string key, int val)
        {
            string str;
            if (requestArgs.TryGetValue(key, out str))
                return int.Parse(str);
            return val;
        }

        // Double

        public double GetDouble(string key)
        {
            return Double.Parse(requestArgs[key], CI);
        }

        public double GetDouble(string key, double val)
        {
            string str;
            if (requestArgs.TryGetValue(key, out str))
                return Double.Parse(str, CI);
            return val;
        }

        public bool GetBoolean(string key)
        {
            string str;
            if (requestArgs.TryGetValue(key, out str))
            {
                if (str == null) return true;
                str = str.ToLower();
                return str.Equals("1") || str.Equals("true");
            }
            throw new ArgumentException("Не задан параметр '" + key + "'");
        }

        public bool GetBoolean(string key, bool def)
        {
            string str;
            if (requestArgs.TryGetValue(key, out str))
            {
                if (str == null) return true;
                str = str.ToLower();
                return str.Equals("1") || str.Equals("true");
            }
            return def;
        }

        public Guid GetGuid(string key)
        {
            return new Guid(requestArgs[key]);
        }

        // String

        public string GetString(string key)
        {
            string val;
            if (requestArgs.TryGetValue(key, out val))
                return val;
            throw new ArgumentException("Не задан параметр '" + key + "'");
        }

        public string GetString(string key, string defaultValue)
        {
            string val;
            if (requestArgs.TryGetValue(key, out val))
                return val;
            return defaultValue;
        }

        private string DecodeUTF(string text)
        {
            return Encoding.UTF8.GetString(ENC1251.GetBytes(text));
        }
    }
}
