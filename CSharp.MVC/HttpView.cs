using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EmbeddedMVC
{
    /// <summary>
    /// Base class for views rendering
    /// </summary>
    public abstract class HttpView
    {
        internal void Init(HttpController controller)
        {
            _controller = controller;
            _server = controller.Server;
            Console.WriteLine("Test88888");
            Console.WriteLine("Push");
        }

        internal void Init(HttpServer httpServer)
        {
            _server = httpServer;
        }

        #region Scope

        private HttpServer _server;
        public HttpServer Server
        {
            get { return _server; }
        }

        private HttpController _controller;
        public HttpController Controller
        {
            get { return _controller; }
        }

        public HttpListenerContext Context
        {
            get { return _controller.Context; }
        }

        private string _layout;
        public string Layout
        {
            get { return _layout; }
            set { _layout = value; }
        }

        private dynamic _page = new ExpandoObject();
        protected dynamic Page { get { return _page; } }

        private dynamic _viewBag = new ExpandoObject();
        protected dynamic ViewBag { get { return _viewBag; } }

        private dynamic _model = new ExpandoObject();
        public dynamic Model
        {
            get { return _model; }
            set { _model = value; }
        }

        public bool IsPost
        {
            get { return _controller.IsPost; }
        }

        public HTTPRequestArgs Request
        {
            get { return _controller.Request; }
        }

        public HttpSession Session
        {
            get { return _controller.Session; }
        }

        #endregion

        #region Rendering

        private StringBuilder sb;

        public string Process()
        {
            sb = new StringBuilder();
            Render();

            if (_layout == null)
            {
                return sb.ToString();
            }
            else
            {
                var layout = _server.GetView(_layout);
                layout.Init(_controller);
                return layout.ProcessLayout(sb.ToString());
            }
        }

        string _layoutBody;
        private string ProcessLayout(string body)
        {
            _layoutBody = body;
            sb = new StringBuilder();
            Render();
            return sb.ToString();
        }

        protected abstract void Render();

        protected void Write(object value)
        {
            sb.Append(value);
        }

        protected void Write(string text)
        {
            sb.Append(text);
        }

        #endregion

        #region Helpers

        protected object RenderPage(string fileName)
        {
            var view = _server.GetView(fileName);
            view.Init(_controller);
            sb.Append(view.Process());
            return null;
        }

        protected object RenderBody()
        {
            return _layoutBody;
        }

        #endregion
    }
}
