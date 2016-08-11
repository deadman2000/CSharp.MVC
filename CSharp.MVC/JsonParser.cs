using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmbeddedMVC
{
    public class JsonParser
    {
        private int _pos = 0;
        private string _str;

        private static bool DEBUG = false;

        private JsonParser(string str)
        {
            _str = str;
        }

        private char ReadSym()
        {
            char c;
            do
            {
                c = _str[_pos++];
            } while (c == ' ' || c == '\r' || c == '\n' || c == '\t');
            return c;
        }

        private char NextSym()
        {
            char c = ReadSym();
            _pos--;
            return c;
        }

        private void MoveTo(params char[] p)
        {
            char c;
            do
            {
                c = ReadSym();
            } while (!p.Contains(c));
            _pos--;
        }

        private string ReadString()
        {
            char c;
            bool esc;

            while (true)
            {
                c = ReadSym();

                if (c == '"')
                {
                    int startInd = _pos;
                    do
                    {
                        c = ReadSym();
                        if (c == '\\')
                        {
                            c = ReadSym();
                            esc = true;
                        }
                        else
                            esc = false;
                    } while (c != '"' || esc);

                    return Regex.Unescape(_str.Substring(startInd, _pos - startInd - 1));
                }
                else
                    throw new FormatException();
            }
        }

        private void ReadDivider()
        {
            char c = ReadSym();
            if (c != ':')
                throw new FormatException();
        }

        private bool TryReadSeparator()
        {
            char c = ReadSym();
            if (c == ',')
                return true;

            _pos--;
            return false;
        }

        private object ReadValue()
        {
            if (String.IsNullOrEmpty(_str)) return null;
            char c;
            while (true)
            {
                c = ReadSym();

                if (IsNumber(c))
                {
                    int startInd = _pos - 1;
                    do
                    {
                        c = ReadSym();
                    } while (IsNumber(c));
                    _pos--;

                    string substr = _str.Substring(startInd, _pos - startInd);
                    if (substr.Contains('.'))
                        return double.Parse(substr, defCI);
                    else
                        return long.Parse(substr, defCI);
                }

                if (c == '"')
                {
                    _pos--;
                    string val = ReadString();
                    return val;
                }

                if (c == '[') // Array
                {
                    c = NextSym();
                    if (c == ']')
                    {
                        _pos++;
                        return new object[0];
                    }

                    List<Object> values = new List<object>();
                    while (true)
                    {
                        object val = ReadValue();
                        values.Add(val);
                        if (!TryReadSeparator()) break;
                    }

                    c = ReadSym();
                    if (c != ']') throw new FormatException();

                    return values.ToArray();
                }

                if (c == '{')
                {
                    c = NextSym();
                    if (c == '}')
                    {
                        _pos++;
                        return new JObject();
                    }

                    JObject obj = ReadObject();

                    c = ReadSym();
                    if (c != '}') throw new FormatException();

                    return obj;
                }

                {
                    int startInd = _pos - 1;
                    MoveTo(',', ']', '}');

                    var word = _str.Substring(startInd, _pos - startInd);
                    switch (word)
                    {
                        case "null": return null;
                        case "false": return false;
                        case "true": return true;
                        default: return word;
                    }
                }
            }
        }

        private JObject ReadObject()
        {
            JObject jobj = new JObject();
            if (DEBUG) Console.WriteLine("Begin read object");

            while (true)
            {
                string key = ReadString();
                ReadDivider();
                object val = ReadValue();
                if (DEBUG) Console.WriteLine("\t" + key + " : " + val);
                jobj.Fields[key] = val;

                if (!TryReadSeparator()) break;
            }
            if (DEBUG) Console.WriteLine("End read object");

            return jobj;
        }

        public static object Parse(string str)
        {
            JsonParser json = new JsonParser(str);
            return json.ReadValue();
        }

        static bool IsNumber(char c)
        {
            return (c >= '0' && c <= '9') || c == '-' || c == '.';
        }

        public static CultureInfo defCI = new CultureInfo("en-US", false);
    }

    public class JObject
    {
        public JObject()
        {
        }

        private Dictionary<string, object> _fields = new Dictionary<string, object>();
        public Dictionary<string, object> Fields { get { return _fields; } }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            str.AppendLine("{");
            foreach (var key in _fields.Keys)
            {
                object value = _fields[key];
                var valStr = ObjectToStr(value);
                var lines = valStr.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                str.AppendLine(String.Format("\t{0}: {1}", key, lines[0]));
                for (int i = 1; i < lines.Length; i++)
                {
                    str.Append("\t");
                    str.AppendLine(lines[i]);
                }
            }
            str.AppendLine("}");
            return str.ToString();
        }

        private static string ObjectToStr(object value)
        {
            if (value is object[])
            {
                object[] arr = (object[])value;

                StringBuilder str = new StringBuilder();
                str.AppendLine("[");
                for (int i = 0; i < arr.Length; i++)
                {
                    var valStr = ObjectToStr(arr[i]);
                    var lines = valStr.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                    for (int j = 0; j < lines.Length; j++)
                    {
                        str.Append("\t");
                        str.Append(lines[j]);
                        if (j < lines.Length - 1)
                            str.AppendLine();
                    }

                    if (i < arr.Length - 1)
                        str.Append(',');
                    str.AppendLine();
                }
                str.AppendLine("]");
                return str.ToString();
            }
            
            if (value is string)
                return "\"" + value + "\"";

            return value.ToString();
        }

        public bool Contains(string key)
        {
            return _fields.ContainsKey(key);
        }

        public object GetValue(string key)
        {
            object o;
            if (!_fields.TryGetValue(key, out o))
                throw new Exception("Property " + key + " is not defined");
            return o;
        }

        public object GetValue(string key, object def)
        {
            object o;
            if (!_fields.TryGetValue(key, out o)) return def;
            return o;
        }

        public JObject GetObject(string key)
        {
            return (JObject)GetValue(key);
        }

        public string GetString(string key)
        {
            var val = GetValue(key);
            if (val == null) return null;
            return val.ToString();
        }

        public string GetString(string key, string def)
        {
            object o;
            if (!_fields.TryGetValue(key, out o))
                return def;
            if (o == null) return null;
            return o.ToString();
        }

        bool CastToBoolean(object o)
        {
            if (o is string)
            {
                string str = (string)o;
                return str.Equals("1") || str.Equals("true", StringComparison.CurrentCultureIgnoreCase);
            }
            if (o is int)
                return (int)o > 0;
            if (o is bool)
                return (bool)o;
            throw new Exception("Ожидался тип bool");
        }

        public bool GetBoolean(string key)
        {
            return CastToBoolean(GetValue(key));
        }

        public bool GetBoolean(string key, bool def)
        {
            object o;
            if (!_fields.TryGetValue(key, out o))
                return def;
            return CastToBoolean(o);
        }

        int CastToInt(object o)
        {
            if (o is double)
                return (int)(double)o;
            else if (o is int)
                return (int)o;
            else if (o is long)
                return (int)(long)o;
            else if (o is string)
                return int.Parse((string)o);
            else
                throw new NotImplementedException("Json int parse type: " + o.GetType());
        }

        public int GetInt(string key)
        {
            return CastToInt(GetValue(key));
        }

        public int GetInt(string key, int def)
        {
            object o;
            if (!_fields.TryGetValue(key, out o))
                return def;
            return CastToInt(o);
        }


        double CastToDouble(object o)
        {
            if (o is double)
                return (double)o;
            if (o is int)
                return (double)(int)o;
            else if (o is long)
                return (double)(long)o;
            else if (o is string)
                return double.Parse((string)o, JsonParser.defCI);
            else
                throw new NotImplementedException("Json double parse type: " + o.GetType());
        }

        public double GetDouble(string key)
        {
            return CastToDouble(GetValue(key));
        }

        public double GetDouble(string key, double def)
        {
            object o;
            if (!_fields.TryGetValue(key, out o))
                return def;
            return CastToDouble(o);
        }

        public object[] GetArray(string key)
        {
            object value;
            if (_fields.TryGetValue(key, out value))
                return (object[])value;
            return null;
        }

        public DateTime GetDate(string key)
        {
            return DateTime.Parse(GetString(key));
        }

        public DateTime GetDate(string key, DateTime def)
        {
            object o;
            if (!_fields.TryGetValue(key, out o))
                return def;
            return DateTime.Parse(o.ToString());
        }

        public DateTime? GetDateNullable(string key)
        {
            string str = GetString(key);
            if (str.Length == 0) return null;
            return DateTime.Parse(str);
        }

        public DateTime? GetDate(string key, DateTime? def)
        {
            object o;
            if (!_fields.TryGetValue(key, out o))
                return def;

            string str = o.ToString();
            if (str.Length == 0) return null;
            return DateTime.Parse(o.ToString());
        }

        public TimeSpan GetTime(string key)
        {
            var val = GetString(key);
            var parts = val.Split(':');
            if (parts.Length == 2)
                return new TimeSpan(int.Parse(parts[0]), int.Parse(parts[1]), 0);

            if (parts.Length == 3)
                return new TimeSpan(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));

            throw new FormatException();
        }

        public void FillAttributes(Dictionary<string, string> dict)
        {
            foreach (var key in _fields.Keys)
            {
                var val = _fields[key];

                if (val == null || String.IsNullOrEmpty(val.ToString()))
                    dict.Remove(key);
                else
                {
                    if (val is double)
                        val = ((double)val).ToString(CultureInfo.InvariantCulture);
                    else if (val is float)
                        val = ((float)val).ToString(CultureInfo.InvariantCulture);

                    dict[key] = val.ToString();
                }
            }
        }
    }
}
