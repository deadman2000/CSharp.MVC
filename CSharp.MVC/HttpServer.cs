using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace EmbeddedMVC
{
    public class HttpServer
    {
        private HttpListener _listener;

        private string _contentDir = "../../public/";
        private string _viewDir = "../../view/";

        string[] _rootDocs = new[] { "index.html" };

        public HttpServer()
        {
            _listener = new HttpListener();
            _listener.IgnoreWriteExceptions = true;
        }

        public string ViewDir
        {
            get { return _viewDir; }
        }

        public void Start(int port)
        {
            Console.WriteLine("Start HTTP on port " + port);

            InitControllers();

            _listener.Prefixes.Add(String.Format("http://*:{0}/", port));
            _listener.Start();

            new Task(HandleRequest).Start();
        }

        public void Stop()
        {
            _listener.Stop();
        }

        private void HandleRequest()
        {
            Utils.NameThread("HandleRequest");
            while (_listener.IsListening)
            {
                try
                {
                    IAsyncResult result = _listener.BeginGetContext(ListenerCallback, _listener);
                    result.AsyncWaitHandle.WaitOne();
                }
                catch (Exception ex)
                {
                    Log.HandleException(ex);
                }
            }
        }

        private void ListenerCallback(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            if (!listener.IsListening)
                return;

            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerResponse response = context.Response;

            try
            {
                Utils.NameThread("HTTP-Provider Request " + context.Request.Url);

                if (ProcessController(context))
                    return;

                if (ProcessStatic(context))
                    return;

                response.StatusCode = 404;
                response.StatusDescription = "NOT FOUND";
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
            }
            finally
            {
                try
                {
                    response.Close();
                }
                catch (Exception ex)
                {
                    Log.HandleException(ex);
                }
            }
        }

        #region Controllers

        private Dictionary<string, ControllerDescription> _controllers;

        private void InitControllers()
        {
            _controllers = (from a in AppDomain.CurrentDomain.GetAssemblies().AsParallel()
                            where !a.GlobalAssemblyCache
                            from t in a.GetTypes()
                            where t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(HttpController))
                            select t).ToDictionary(t => t.Name.ToLower(), t => new ControllerDescription(this, t));
        }

        private bool ProcessController(HttpListenerContext context)
        {
            var segments = context.Request.Url.Segments;

            string ctrlName;

            if (segments.Length == 1)
                ctrlName = "indexcontroller";
            else
                ctrlName = segments[1].TrimEnd('/').ToLower() + "controller";

            ControllerDescription controller;
            if (!_controllers.TryGetValue(ctrlName, out controller))
                return false;

            return controller.Process(context);
        }

        #endregion

        #region Static content

        public bool ProcessStatic(HttpListenerContext context)
        {
            if (!context.Request.HttpMethod.Equals("GET")) return false;

            Uri uri = context.Request.Url;

            if (uri.LocalPath.Equals("/"))
                return ProcessRoot(context);

            string fileName = Path.GetFileName(uri.LocalPath); // Checking path is file name
            if (fileName.Length > 0)
                return ProcessFile(context.Response, uri.LocalPath);

            return false;
        }

        private bool ProcessRoot(HttpListenerContext context)
        {
            foreach (var doc in _rootDocs)
            {
                if (ProcessFile(context.Response, doc))
                    return true;
            }

            return false;
        }

        private bool ProcessFile(HttpListenerResponse response, string contentPath)
        {
            var content = GetContent(contentPath);
            if (content == null)
                return false;

            response.StatusCode = 200;
            response.StatusDescription = "OK";
            response.ContentType = content.MimeType;
            response.OutputStream.Write(content.Bytes, 0, content.Bytes.Length);
            return true;
        }

        private Dictionary<string, CachedDocument> _fileCache = new Dictionary<string, CachedDocument>();

        private CachedDocument GetContent(string contentPath)
        {
            if (contentPath[0] != '/') contentPath = "/" + contentPath;

            CachedDocument doc;
            if (_fileCache.TryGetValue(contentPath, out doc)) // TODO LastFileWrite checking
                return doc;

            lock (_fileCache)
            {
                if (_fileCache.TryGetValue(contentPath, out doc))
                    return doc;

                string filePath = _contentDir + contentPath;
                if (File.Exists(filePath))
                {
                    // TODO Don't cache large content
                    doc = new CachedDocument();
                    doc.Bytes = File.ReadAllBytes(filePath);
                    doc.MimeType = MimeMapping.GetMimeMapping(contentPath); //MimeMapping._mappingDictionary.AddMapping(string fileExtension, string mimeType)
                }
                else
                {
                    object obj = GetResource(contentPath);

                    if (obj is Image)
                        doc = new CachedDocument((Image)obj);
                    else
                        return null;
                }

                _fileCache.Add(contentPath, doc);
                return doc;
            }
        }

        #endregion

        #region Resources

        private List<ResourceFolder> _resourceFolders = new List<ResourceFolder>();

        /// <summary>
        /// Register resource as virtual folder
        /// </summary>
        /// <param name="resourceManager"></param>
        /// <param name="folderName"></param>
        public void AddResource(ResourceManager manager, string folderName)
        {
            _resourceFolders.Add(new ResourceFolder(manager, folderName));
        }

        public object GetResource(string path)
        {
            if (path.StartsWith("/")) path = path.TrimStart('/');

            string folderName;
            string fileName;

            int ind = path.LastIndexOf('/');
            if (ind > 0)
            {
                folderName = path.Substring(0, ind);
                fileName = path.Substring(ind + 1, path.Length - ind - 1);
            }
            else
            {
                folderName = "";
                fileName = path;
            }

            var resFolder = _resourceFolders.Find(f => f.FolderName.Equals(folderName));
            if (resFolder == null)
                return null;
            return resFolder.Manager.GetObject(fileName);
        }

        #endregion

        #region View

        private Dictionary<string, Type> _compiledResourceView = new Dictionary<string, Type>();
        private Type GetResourceView(string path)
        {
            Type t;
            if (_compiledResourceView.TryGetValue(path, out t))
                return t;

            lock (_compiledResourceView)
            {
                if (_compiledResourceView.TryGetValue(path, out t))
                    return t;

                object res = GetResource(path);
                if (res is string)
                {
                    t = new ViewCompiler((string)res).Build();
                    _compiledResourceView.Add(path, t);
                    return t;
                }
                return null;
            }
        }

        public HttpView GetView(string path)
        {
            if (path.StartsWith("?"))
            {
                path = path.Substring(1);
                Type t = GetResourceView(path);
                if (t == null)
                    throw new Exception(String.Format("Resource {0} is not found", path));
                return (HttpView)Activator.CreateInstance(t);
            }
            var filePath = Path.Combine(_viewDir, path);
            return ViewCompiler.CompileFile(filePath);
        }

        #endregion

        #region Sessions

        private Dictionary<string, HttpSession> _sessions = new Dictionary<string, HttpSession>();

        public void AddSession(HttpSession sess)
        {
            _sessions[sess.ID] = sess;
        }

        public HttpSession GetSession(IPAddress ip, string id)
        {
            HttpSession sess;
            if (_sessions.TryGetValue(id, out sess) && sess.IP.Equals(ip))
                return sess;
            return null;
        }

        public void RemoveSession(HttpSession session)
        {
            _sessions.Remove(session.ID);
        }

        #endregion
    }
}
