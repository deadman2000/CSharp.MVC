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

        public bool IsPost
        {
            get { return _context.Request.HttpMethod.Equals("POST"); }
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

        protected HTTPRequestArgs args;
        public HTTPRequestArgs Request { get { return args; } }

        private void InitArguments()
        {
            var request = _context.Request;
            if (request.HttpMethod.Equals("GET"))
                args = new HTTPRequestArgs(request.QueryString);
            else if (request.HttpMethod.Equals("POST"))
            {
                string text;
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    text = reader.ReadToEnd();
                }
                text = text.Replace('+', ' ');
                args = new HTTPRequestArgs(text);
            }
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
                _session = _server.GetSession(_context.Request.RemoteEndPoint.Address, cookie.Value);
        }

        public void Finish()
        {
            if (_completed) return;

            if (_json != null)
                WriteContent(200, "application/json", _json.GetText());
            else
                WriteContent(504, "text/html", String.Empty);
        }

        private bool _completed = false;

        protected void WriteContent(int code, string contentType, string contentString)
        {
            if (_completed) throw new Exception("Answer is already send");

            _completed = true;

            var response = _context.Response;
            var request = _context.Request;

            //response.AddHeader("Access-Control-Allow-Credentials", "true");
            //response.AddHeader("Access-Control-Allow-Origin", "*");
            //response.AddHeader("Access-Control-Origin", "*");

            switch (code)
            {
                case 200: response.StatusDescription = "OK"; break;
                case 500: response.StatusDescription = "INTERNAL SERVER ERROR"; break;
                case 504: response.StatusDescription = "NOT IMPLEMENTED"; break;
            }
            response.StatusCode = code;

            response.ContentType = contentType + ";" + request.ContentEncoding.WebName;
            //response.ContentType = contentType + ";" + Encoding.UTF8.WebName;

            if (contentString == null) contentString = "";
            byte[] buffer = request.ContentEncoding.GetBytes(contentString);
            //byte[] buffer = Encoding.UTF8.GetBytes(contentString);

            bool gzip = false;
            var accept_encoding = request.Headers["Accept-Encoding"];
            if (accept_encoding != null)
            {
                var encodings = accept_encoding.Split(',').Select(e => e.Trim());
                gzip = encodings.Contains("gzip");
            }

            if (gzip)
            {
                response.AddHeader("Content-Encoding", "gzip");
                using (GZipStream refGZipStream = new GZipStream(response.OutputStream, CompressionMode.Compress, false))
                using (MemoryStream varByteStream = new MemoryStream(buffer))
                {
                    varByteStream.WriteTo(refGZipStream);
                    refGZipStream.Flush();
                }
            }
            else
            {
                try
                {
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                catch { }
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
            string result = view.Process();
            WriteContent(200, "text/html", result);
        }

        protected void Redirect(string url)
        {
            Response.Redirect(url);
            _completed = true;
        }

        public HttpSession OpenSession()
        {
            if (_completed)
                throw new Exception("Answer already send");

            if (_session != null)
                return _session;

            HttpSession sess = new HttpSession(_context.Request.RemoteEndPoint.Address);
            _server.AddSession(sess);

            Cookie cookie = new Cookie("__sess", sess.ID, "/");
            cookie.Expires = DateTime.Now.AddDays(7);
            _context.Response.SetCookie(cookie);
            return sess;
        }

        public void CloseSession()
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
