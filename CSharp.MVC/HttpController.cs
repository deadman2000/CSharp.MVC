using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace EmbeddedMVC
{
    public abstract class HttpController
    {
        private HttpServer _server;
        private HttpListenerContext _context;

        public void Init(HttpServer server, HttpListenerContext context)
        {
            _server = server;
            _context = context;

            InitArguments();
            InitSession();
        }

        public HttpServer Server
        {
            get { return _server; }
        }

        public HttpListenerContext Context
        {
            get { return _context; }
        }

        public HttpListenerResponse Response
        {
            get { return _context.Response; }
        }

        protected HTTPRequestArgs args;
        public HTTPRequestArgs Request { get { return args; } }

        private object _body;
        /// <summary>
        /// Распарсенное тело запроса
        /// </summary>
        public object Body
        {
            get { return _body; }
        }

        private byte[] _rawBody;
        /// <summary>
        /// Текст тела запроса
        /// </summary>
        public byte[] RawBody
        {
            get { return _rawBody; }
            set { _rawBody = value; }
        }



        public bool IsPost
        {
            get { return _context.Request.HttpMethod.Equals("POST"); }
        }


        private Encoding _responseEncoding = Encoding.UTF8;
        public Encoding ResponseEncoding
        {
            get { return _responseEncoding; }
            set { _responseEncoding = value; }
        }

        private JsonWriter _json;
        public JsonWriter JSON
        {
            get
            {
                if (_json == null)
                    _json = new JsonWriter();
                return _json;
            }
        }

        private string _responseText;
        public string ResponseText
        {
            get { return _responseText; }
            set { _responseText = value; }
        }


        private void InitArguments()
        {
            var request = _context.Request;
            if (request.HttpMethod.Equals("GET"))
                args = new HTTPRequestArgs(request.QueryString);
            else if (request.HttpMethod.Equals("POST"))
            {
                try
                {
                    _rawBody = ReadToEnd(request.InputStream);
                }
                catch
                {
                    return;
                }

                /*using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    bodyStr = reader.ReadToEnd(); // Старый вариант
                }*/

                var contentType = request.ContentType.Split(';')[0];
                if (contentType == "application/json")
                {
                    string bodyStr = request.ContentEncoding.GetString(_rawBody);
                    try
                    {
                        _body = JsonParser.Parse(bodyStr);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("JSON Parse error: {0}\n{1}\nBody: {2}", ex, _context.Request.RawUrl, bodyStr);
                    }
                }
                else if (contentType == "application/x-www-form-urlencoded")
                {
                    string bodyStr = request.ContentEncoding.GetString(_rawBody);
                    bodyStr = bodyStr.Replace('+', ' ');
                    args = new HTTPRequestArgs(bodyStr);
                }

                if (args == null)
                    args = new HTTPRequestArgs(request.QueryString);
            }
        }

        private static byte[] ReadToEnd(System.IO.Stream stream)
        {
            byte[] readBuffer = new byte[4096];

            int totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
            {
                totalBytesRead += bytesRead;

                if (totalBytesRead == readBuffer.Length)
                {
                    int nextByte = stream.ReadByte();
                    if (nextByte != -1)
                    {
                        byte[] temp = new byte[readBuffer.Length * 2];
                        Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                        Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                        readBuffer = temp;
                        totalBytesRead++;
                    }
                }
            }

            byte[] buffer = readBuffer;
            if (readBuffer.Length != totalBytesRead)
            {
                buffer = new byte[totalBytesRead];
                Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
            }
            return buffer;
        }

        private HttpSession _session;
        public HttpSession Session
        {
            get { return _session; }
        }

        private void InitSession()
        {
            var cookie = _context.Request.Cookies["__sess"];
            if (cookie != null)
            {
                string val = cookie.Value;
                if (val.Length > 2 && val.StartsWith("\"") && val.EndsWith("\""))
                    val = val.Substring(1, val.Length - 2); // Remove quotes
                _session = _server.GetSession(val);
            }
        }

        public virtual void WriteException(Exception ex)
        {
            WriteContent(500, "text/html", ex.ToString());
        }

        internal void _ProcessCompleted()
        {
            Finish();
        }

        private bool _completed = false;
        private bool _isAsync = false;

        public bool IsAsync
        {
            get { return _isAsync; }
        }

        protected void FinishAsync()
        {
            _isAsync = true;
        }

        protected void Finish()
        {
            if (_completed) return;

            if (_json != null)
                WriteContent(200, "application/json", _json.GetText());
            else if (_responseText != null)
                WriteContent(200, "text/html", _responseText);
            else
                WriteContent(504, "text/html", String.Empty);
        }

        protected void WriteContent(int code, string contentType, string contentString)
        {
            if (contentString == null) contentString = "";
            byte[] buffer = _responseEncoding.GetBytes(contentString);
            WriteContent(code, contentType, buffer);
        }

        protected void WriteContent(int code, string contentType = null, byte[] buffer = null)
        {
            if (_completed) throw new Exception("Answer is already send");
            _completed = true;

            var response = _context.Response;
            var request = _context.Request;

            //response.AddHeader("Access-Control-Allow-Credentials", "true");
            //response.AddHeader("Access-Control-Allow-Origin", "*");
            //response.AddHeader("Access-Control-Origin", "*");

            response.StatusCode = code;
            switch (code)
            {
                case 200: response.StatusDescription = "OK"; break;
                case 404: response.StatusDescription = "NOT FOUND"; break;
                case 500: response.StatusDescription = "INTERNAL SERVER ERROR"; break;
                case 504: response.StatusDescription = "NOT IMPLEMENTED"; break;
            }

            if (buffer != null)
            {
                response.ContentType = contentType + ";" + _responseEncoding.WebName;

                bool gzip = false;
                if (!Server.NoGZip)
                {
                    var accept_encoding = request.Headers["Accept-Encoding"];
                    if (accept_encoding != null)
                    {
                        var encodings = accept_encoding.Split(',').Select(e => e.Trim());
                        gzip = encodings.Contains("gzip");
                    }
                }

                if (gzip)
                {
                    response.AddHeader("Content-Encoding", "gzip");
                    buffer = GZIP(buffer);

                    /*using (GZipStream refGZipStream = new GZipStream(response.OutputStream, CompressionMode.Compress, false))
                    using (MemoryStream varByteStream = new MemoryStream(buffer))
                    {
                        varByteStream.WriteTo(refGZipStream);
                        refGZipStream.Flush();
                    }*/
                }

                response.ContentLength64 = buffer.Length;
                try
                {
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                catch { }
            }

            try
            {
                response.Close();
            }
            catch
            {
            }
        }

        public static byte[] GZIP(byte[] raw)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
                    gzip.Write(raw, 0, raw.Length);
                return memory.ToArray();
            }
        }

        private dynamic _model = new ExpandoObject();
        public dynamic Model { get { return _model; } }

        protected void View(string fileName)
        {
            _json = null;

            HttpView view = _server.GetView(fileName);
            view.Init(this);
            view.Model = _model;

            try
            {
                string result = view.Process();
                WriteContent(200, "text/html", result);
            }
            catch (Exception ex)
            {
                _server.HandleException(ex);
                WriteContent(500, "text/html", ex.ToString());
            }
        }

        protected void Redirect(string url)
        {
            Response.Redirect(url);
            _completed = true;

            try
            {
                _context.Response.Close();
            }
            catch
            {
            }
        }

        public HttpSession OpenSession()
        {
            if (_completed)
                throw new Exception("Answer already send");

            if (_session == null)
            {
                _session = new HttpSession(_context.Request.RemoteEndPoint.Address);
                _server.AddSession(_session);
                BindSession(_session);
            }

            return _session;
        }

        public HttpSession CreateSession()
        {
            if (_completed)
                throw new Exception("Answer already send");

            _session = new HttpSession(_context.Request.RemoteEndPoint.Address);
            _server.AddSession(_session);
            BindSession(_session);
            return _session;
        }

        public void BindSession(HttpSession sess)
        {
            Cookie cookie = new Cookie("__sess", sess.ID, "/");
            cookie.Expires = DateTime.Now.AddDays(7);
            _context.Response.SetCookie(cookie);
        }

        public void CloseSession() // TODO Filter from external call
        {
            Cookie cookie = new Cookie("__sess", "", "/");
            cookie.Expired = true;
            _context.Response.SetCookie(cookie);

            if (_session == null)
                return;

            _server.RemoveSession(_session);
            _session = null;
        }
    }
}
