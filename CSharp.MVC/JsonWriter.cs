using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EmbeddedMVC
{
    public class JsonWriter
    {
        private StringBuilder sb;
        private List<bool> quotes; // Отвечает за признак первого элемента в стеке
        private List<JsonStack> _stack; // Тип данных стека
        public static CultureInfo defCI = new CultureInfo("en-US", false);

        public JsonWriter()
        {
            Clear();
        }

        public bool IsEmpty
        {
            get { return sb.Length > 0; }
        }

        private bool _utc;
        public bool UTC
        {
            get { return _utc; }
            set { _utc = value; }
        }

        public string GetText()
        {
            while (_stack.Count > 1)
            {
                switch (Stack)
                {
                    case JsonStack.Array:
                        EndArray();
                        break;
                    case JsonStack.Object:
                        EndObject();
                        break;
                }
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            return sb.ToString();
        }


        public void Clear()
        {
            sb = new StringBuilder();
            quotes = new List<bool>();
            quotes.Add(false);
            _stack = new List<JsonStack>();
            _stack.Add(JsonStack.StartJson);
        }

        private JsonStack Stack { get { return _stack.Count == 0 ? JsonStack.None : _stack[_stack.Count - 1]; } }

        private void Push(JsonStack s)
        {
            _stack.Add(s);
            quotes.Add(false);
        }

        private void Pull()
        {
            if (_stack.Count == 1)
                throw new Exception("JSON error: nothing to close");

            _stack.RemoveAt(_stack.Count - 1);
            if (_stack.Count == 1)
                _stack[0] = JsonStack.None;

            quotes.RemoveAt(quotes.Count - 1);
        }

        private void CheckStack(JsonStack mask)
        {
            if ((Stack & mask) == 0)
                throw new Exception("JSON error " + mask);
        }

        private void Comma()
        {
            int i = quotes.Count - 1;
            if (quotes[i])
                sb.Append(',');
            else
                quotes[i] = true;
        }

        #region Objects

        /// <summary>
        /// Начало записи объекта
        /// Только внутри массива или в начале записи JSON
        /// </summary>
        public void StartObject()
        {
            CheckStack(JsonStack.StartJson | JsonStack.Array);
            Comma();

            sb.Append('{');
            Push(JsonStack.Object);
        }

        /// <summary>
        /// Начало записи объекта-параметра
        /// Только внутри объектов
        /// </summary>
        /// <param name="name"></param>
        public void StartObject(string name)
        {
            StartParameter(name);

            sb.Append('{');
            Push(JsonStack.Object);
        }

        /// <summary>
        /// Завершение записи объекта
        /// Только в объектах
        /// </summary>
        public void EndObject()
        {
            CheckStack(JsonStack.Object);
            Pull();
            sb.Append('}');
        }

        #endregion

        #region Arrays

        /// <summary>
        /// Начало записи массива-параметра
        /// </summary>
        /// <param name="name"></param>
        public void StartArray(string name)
        {
            StartParameter(name);

            sb.Append('[');
            Push(JsonStack.Array);
        }

        /// <summary>
        /// Начало записи массива
        /// Только внутри массивов и в начале записи JSON
        /// </summary>
        public void StartArray()
        {
            CheckStack(JsonStack.StartJson | JsonStack.Array);
            Comma();

            sb.Append('[');
            Push(JsonStack.Array);
        }

        /// <summary>
        /// Окончание записи массива
        /// Только внутри массивов
        /// </summary>
        public void EndArray()
        {
            CheckStack(JsonStack.Array);
            Pull();
            sb.Append(']');
        }

        #endregion

        #region Values

        private void WriteValue(object val)
        {
            if (val == null)
            {
                WriteValue("");
                return;
            }

            switch (Type.GetTypeCode(val.GetType()))
            {
                case TypeCode.String: WriteValue((string)val); break;
                case TypeCode.Byte: WriteValue((byte)val); break;
                case TypeCode.SByte: WriteValue((sbyte)val); break;
                case TypeCode.Int16: WriteValue((short)val); break;
                case TypeCode.UInt16: WriteValue((ushort)val); break;
                case TypeCode.Int32: WriteValue((int)val); break;
                case TypeCode.UInt32: WriteValue((uint)val); break;
                case TypeCode.Int64: WriteValue((long)val); break;
                case TypeCode.UInt64: WriteValue((ulong)val); break;
                case TypeCode.Single: WriteValue((float)val); break;
                case TypeCode.Double: WriteValue((double)val); break;
                case TypeCode.DateTime: WriteValue((DateTime)val); break;
                case TypeCode.Boolean: WriteValue((bool)val); break;
                default: WriteValue(val.ToString()); break;
            }
        }

        private void WriteValue(string val)
        {
            sb.Append('"');
            sb.Append(JsonEncode(val));
            sb.Append('"');
        }

        private void WriteValue(bool val)
        {
            if (val)
                sb.Append("true");
            else
                sb.Append("false");
        }

        private void WriteValue(byte val)
        {
            sb.Append(val.ToString());
        }

        private void WriteValue(short val)
        {
            sb.Append(val.ToString());
        }

        private void WriteValue(int val)
        {
            sb.Append(val.ToString());
        }

        private void WriteValue(uint val)
        {
            sb.Append(val.ToString());
        }

        private void WriteValue(long val)
        {
            sb.Append(val.ToString());
        }

        private void WriteValue(ulong val)
        {
            sb.Append(val.ToString());
        }

        private void WriteValue(decimal val)
        {
            sb.Append(val.ToString());
        }

        private void WriteValue(float val)
        {
            sb.Append(val.ToString(defCI));
        }

        private void WriteValue(double val)
        {
            sb.Append(val.ToString(defCI));
        }

        private void WriteValue(DateTime val)
        {
            sb.Append(val.ToString(defCI));
        }

        #endregion

        #region Array Values

        public void WriteArrayValue(object val)
        {
            CheckStack(JsonStack.Array);
            Comma();
            WriteValue(val);
        }

        public void WriteArrayValue(string val)
        {
            CheckStack(JsonStack.Array);
            Comma();
            WriteValue(val);
        }

        public void WriteArrayValue(bool val)
        {
            CheckStack(JsonStack.Array);
            Comma();
            WriteValue(val);
        }

        public void WriteArrayValue(float val)
        {
            CheckStack(JsonStack.Array);
            Comma();
            WriteValue(val);
        }

        public void WriteArrayValue(double val)
        {
            CheckStack(JsonStack.Array);
            Comma();
            WriteValue(val);
        }

        public void WriteArrayValue(byte val)
        {
            CheckStack(JsonStack.Array);
            Comma();
            WriteValue(val);
        }

        public void WriteArrayValue(sbyte val)
        {
            CheckStack(JsonStack.Array);
            Comma();
            WriteValue(val);
        }

        public void WriteArrayValue(short val)
        {
            CheckStack(JsonStack.Array);
            Comma();
            WriteValue(val);
        }

        public void WriteArrayValue(ushort val)
        {
            CheckStack(JsonStack.Array);
            Comma();
            WriteValue(val);
        }

        public void WriteArrayValue(int val)
        {
            CheckStack(JsonStack.Array);
            Comma();
            WriteValue(val);
        }

        public void WriteArrayValue(uint val)
        {
            CheckStack(JsonStack.Array);
            Comma();
            WriteValue(val);
        }

        public void WriteArrayValue(long val)
        {
            CheckStack(JsonStack.Array);
            Comma();
            WriteValue(val);
        }

        public void WriteArrayValue(ulong val)
        {
            CheckStack(JsonStack.Array);
            Comma();
            WriteValue(val);
        }

        public void WriteArrayValue(decimal val)
        {
            CheckStack(JsonStack.Array);
            Comma();
            WriteValue(val);
        }

        #endregion

        #region Object Parameter

        private void StartParameter(string name)
        {
            CheckStack(JsonStack.Object);
            Comma();

            sb.Append('"');
            sb.Append(JsonEncode(name));
            sb.Append("\":");
        }

        /// <summary>
        /// Запись параметра объекта
        /// Только внутри объектов
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void WriteParameter(string name, object value)
        {
            StartParameter(name);
            WriteValue(value);
        }

        /// <summary>
        /// Запись параметра объекта
        /// Только внутри объектов
        /// </summary>
        /// <param name="param"></param>
        /// <param name="value"></param>
        public void WriteParameter(string param, [Localizable(true)] string value)
        {
            StartParameter(param);
            WriteValue(value);
        }

        public void WriteParameter(string param, bool value)
        {
            StartParameter(param);
            WriteValue(value);
        }

        public void WriteParameter(string name, byte value)
        {
            StartParameter(name);
            WriteValue(value);
        }

        public void WriteParameter(string name, sbyte value)
        {
            StartParameter(name);
            WriteValue(value);
        }

        public void WriteParameter(string name, short value)
        {
            StartParameter(name);
            WriteValue(value);
        }

        public void WriteParameter(string name, ushort value)
        {
            StartParameter(name);
            WriteValue(value);
        }

        public void WriteParameter(string name, int value)
        {
            StartParameter(name);
            WriteValue(value);
        }

        public void WriteParameter(string name, uint value)
        {
            StartParameter(name);
            WriteValue(value);
        }

        public void WriteParameter(string name, long value)
        {
            StartParameter(name);
            WriteValue(value);
        }

        public void WriteParameter(string name, ulong value)
        {
            StartParameter(name);
            WriteValue(value);
        }

        public void WriteParameter(string name, decimal value)
        {
            StartParameter(name);
            WriteValue(value);
        }

        public void WriteParameter(string name, float value)
        {
            StartParameter(name);
            WriteValue(value);
        }

        public void WriteParameter(string name, double value)
        {
            StartParameter(name);
            WriteValue(value);
        }

        public void WriteParameterDate(string name, DateTime value)
        {
            StartParameter(name);
            WriteValue(value);
        }

        #endregion

        private static string JsonEncode(string str)
        {
            if (str == null)
                return String.Empty;
            return str.Replace(@"\", @"\\").Replace("\"", "\\\"").Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        [Flags]
        private enum JsonStack
        {
            StartJson = 1,
            Object = 2,
            Array = 4,
            None = 8
        }
    }
}
