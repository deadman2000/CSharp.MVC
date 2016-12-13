﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;

namespace EmbeddedMVC
{
    public class HttpSession
    {
        private string _id;
        private IPAddress _ip;
        private DateTime _dateOpen;
        private dynamic _data = new ExpandoObject();

        private static Random rnd = new Random();

        public HttpSession(IPAddress iPAddress)
        {
            _id = rnd.Next().ToString();
            _ip = iPAddress;
            _dateOpen = DateTime.Now;
        }

        public string ID { get { return _id; } }

        public DateTime DateOpen { get { return _dateOpen; } }

        public IPAddress IP { get { return _ip; } }

        public dynamic Data { get { return _data; } }

        private Dictionary<string, object> _values = new Dictionary<string,object>();

        public object this[string key]
        {
            get
            {
                object v;
                if (_values.TryGetValue(key, out v)) return v;
                return null;
            }
            set
            {
                _values[key] = value;
            }
        }

        public bool ContainsKey(string key)
        {
            return _values.ContainsKey(key);
        }
    }
}
