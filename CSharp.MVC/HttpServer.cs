using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace EmbeddedMVC
{
    public class HttpServer
    {
        private readonly Task[] _workers;
    //    private readonly Task _listenerTask;
        private readonly Thread _listenerThread;
        private readonly ManualResetEvent _stop, _ready;
        private Queue<HttpListenerContext> _queue;
        private ReaderWriterLockSlim _queueLock;

        public HttpServer(int maxThreads = 1024)
        {
            InitResources();

            int workerThreadsMin, completionPortThreadsMin;
            ThreadPool.GetMinThreads(out workerThreadsMin, out completionPortThreadsMin);
            int workerThreadsMax, completionPortThreadsMax;
            ThreadPool.GetMaxThreads(out workerThreadsMax, out completionPortThreadsMax);

            ThreadPool.SetMinThreads(workerThreadsMax, completionPortThreadsMin + maxThreads + 15);

            _workers = new Task[maxThreads];
            _queueLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _queue = new Queue<HttpListenerContext>();
            _stop = new ManualResetEvent(false);
            _ready = new ManualResetEvent(false);
        //    _listenerTask = new Task(HandleRequest);
            _listenerThread = new Thread(HandleRequest);

            _listener = new HttpListener();
            _listener.IgnoreWriteExceptions = true;
        }

        #region Configs

        private string[] _rootDocs = new[] { "index.html", "index.htm" };

        private string _contentDir = "../../public/";
        public string ContentDir
        {
            get { return _contentDir; }
            set { _contentDir = value; }
        }

        private string _viewDir = "../../view/";
        public string ViewDir
        {
            get { return _viewDir; }
            set { _viewDir = value; }
        }

        private string _notFoundPage = "?mvc/page404";
        public string NotFoundPage
        {
            get { return _notFoundPage; }
            set { _notFoundPage = value; }
        }

        private bool _noGZip = false;
        public bool NoGZip
        {
            get { return _noGZip; }
            set { _noGZip = value; }
        }

        #endregion

        #region Listening

        private HttpListener _listener;

        public void Start()
        {
            Console.WriteLine("Start HTTP on ports " + string.Join(",", _listener.Prefixes));

            InitControllers();

         //   _listenerTask.Start();

            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i] = new Task(Worker);
                _workers[i].Start();
            }
            _listener.Start();
            _listenerThread.Start();
        }

        public void AddPrefix(string prefix)
        {
            _listener.Prefixes.Add(prefix);
        }

        //http://stackoverflow.com/questions/11403333/httplistener-with-https-support

        public void Dispose()
        {
            Stop();
        }

        public void Stop()
        {
            HandleInfo("Stopping listener");
            _listener.Stop();
            _stop.Set();
          /*  if (!_listenerTask.Wait(1000))
            {
                HandleError("Listener task can't stop!");
            }*/

            HandleInfo("Stopping workers");
            foreach (Task worker in _workers)
            {
                if (!worker.Wait(1000))
                {
                    HandleError("Task " + worker.Id + " can't stop!");
                    try
                    {
                        worker.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
            HandleInfo("Stopped");
        }

        private void HandleRequest()
        {
            Utils.NameThread("HandleRequest");
            while (_listener.IsListening)
            {
                try
                {
                    var context = _listener.BeginGetContext(ContextReady, _listener);
                    //context.AsyncWaitHandle.WaitOne();

                    if (0 == WaitHandle.WaitAny(new[] { _stop, context.AsyncWaitHandle }))
                        return;
                }
                catch (Exception ex)
                {
                    HandleException(ex);
                }
            }
        }

        private void ContextReady(IAsyncResult ar)
        {
            _queueLock.EnterWriteLock();
            try
            {
                _queue.Enqueue(_listener.EndGetContext(ar));
                _ready.Set();
            }
            catch { }
            finally
            {
                _queueLock.ExitWriteLock();
            }
        }

        //    int t = 0;

        private void Worker()
        {
            Utils.NameThread("HTTP-Server Worker");
            WaitHandle[] wait = new[] { _ready, _stop };
            while (0 == WaitHandle.WaitAny(wait))
            {
                if (!_listener.IsListening) return;

                HttpListenerContext context;
                _queueLock.EnterWriteLock();
                try
                {
                    if (_queue.Count > 0)
                        context = _queue.Dequeue();
                    else
                    {
                        _ready.Reset();
                        continue;
                    }
                }
                finally
                {
                    _queueLock.ExitWriteLock();
                }
                /* Task.Run(() =>
                   {
                       ProcessRequest(context);
                   });*/
                if (context != null)
                    ProcessRequest(context);
            }
        }

        public event HttpRequestEventHandler NewRequest;

        private void ProcessRequest(HttpListenerContext context)
        {
            HttpListenerResponse response = context.Response;

            try
            {
                if (NewRequest != null)
                    NewRequest(this, context);
            }
            catch { }

            try
            {
                if (ProcessController(context))
                    return;
                if (ProcessStatic(context))
                {
                    response.Close();
                    return;
                }

                response.StatusCode = 404;
                response.StatusDescription = "NOT FOUND";
                response.ContentType = "text/html;utf-8";

                string html;
                if (_notFoundPage != null)
                {
                    var view = GetView(_notFoundPage);
                    view.Init(this);
                    html = view.Process();

                }
                else
                {
                    html = "<h1>NOT FOUND</h1>";
                }

                var bytes = Encoding.UTF8.GetBytes(html);
                response.OutputStream.Write(bytes, 0, bytes.Length);
                response.Close();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        /*     static async void MyFunc(HttpListenerResponse response, HttpView view)
             {
                 string message = await GetDataAsync(view);
                 var bytes = Encoding.UTF8.GetBytes(message);
                 response.OutputStream.Write(bytes, 0, bytes.Length);
                 response.Close();
             }

             static Task<string> GetDataAsync(HttpView view)
             {
                 return Task.Run(() =>
                 {
                     string html = view.Process();
                     return html;
                 });
             }*/

        #endregion

        #region Controllers

        private Dictionary<string, ControllerDescription> _controllers = new Dictionary<string, ControllerDescription>();

        private void InitControllers()
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (a.GlobalAssemblyCache) continue;

                try
                {
                    //Console.WriteLine(a.GetName());
                    var classes = (from t in a.GetTypes()
                                   where t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(HttpController))
                                   select t);

                    foreach (var c in classes)
                        _controllers[c.Name.ToLower()] = new ControllerDescription(this, c);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed load {0}: {1}", a.GetName(), ex.Message);
                }
            }

            /*var classes = (from a in AppDomain.CurrentDomain.GetAssemblies().AsParallel()
                           where !a.GlobalAssemblyCache
                           from t in a.GetTypes()
                           where t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(HttpController))
                           select t);
            _controllers = classes.ToDictionary(t => t.Name.ToLower(), t => new ControllerDescription(this, t));*/
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
            try
            {
                response.OutputStream.Write(content.Bytes, 0, content.Bytes.Length);
            }
            catch
            {
            }
            return true;
        }

        private Dictionary<string, CachedDocument> _fileCache = new Dictionary<string, CachedDocument>();

        private CachedDocument GetContent(string contentPath)
        {
            if (contentPath[0] != '/') contentPath = "/" + contentPath;

            CachedDocument doc = null;
            if (_fileCache.TryGetValue(contentPath, out doc)) // TODO LastFileWrite checking
                return doc;

            lock (_fileCache)
            {
                if (_fileCache.TryGetValue(contentPath, out doc))
                    return doc;

                string filePath = _contentDir + contentPath;
                if (Directory.Exists(filePath))
                {
                    foreach (var file in _rootDocs)
                    {
                        doc = GetContent(contentPath + "/" + file);
                        if (doc != null)
                            break;
                    }
                }
                else if (File.Exists(filePath))
                {
                    // TODO Don't cache large content
                    doc = new CachedDocument();
                    doc.Bytes = File.ReadAllBytes(filePath);
                    doc.MimeType = MimeTypes.GetMimeMapping(contentPath);
                }
                else
                {
                    object obj = GetResource(contentPath);

                    if (obj is Image)
                        doc = new CachedDocument((Image)obj);
                    else if (obj is string)
                    {
                        string mimeType = "text/plain";
                        if (contentPath.EndsWith("_css"))
                            mimeType = "text/css";
                        doc = new CachedDocument((string)obj, mimeType);
                    }
                }

                if (doc != null)
                    _fileCache.Add(contentPath, doc);
                return doc;
            }
        }

        #endregion

        #region Resources

        private void InitResources()
        {
            _resourceFolders = new List<ResourceFolder>();
            AddResource(Views.ResourceManager, "mvc");
        }

        private List<ResourceFolder> _resourceFolders;

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

            var resFolder = _resourceFolders.FindLast(f => f.FolderName.Equals(folderName));
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
            if (sess == null)
                throw new ArgumentNullException("Session is null");
            lock (_sessions)
            {
                _sessions[sess.ID] = sess;
            }
        }

        public HttpSession GetSession(string id)
        {
            HttpSession sess;
            lock (_sessions)
            {
                if (_sessions.TryGetValue(id, out sess))
                    return sess;
            }
            return null;
        }

        public void RemoveSession(HttpSession session)
        {
            lock (_sessions)
            {
                _sessions.Remove(session.ID);
            }
        }

        public HttpSession[] GetSessions()
        {
            lock (_sessions)
            {
                return _sessions.Values.ToArray();
            }
        }

        #endregion

        public event HttpServerMessageHandler ErrorEvent;
        public event HttpServerMessageHandler InfoEvent;

        internal void HandleException(Exception ex)
        {
            HandleError(ex.ToString());
        }

        internal void HandleError(string message)
        {
            if (ErrorEvent != null)
                ErrorEvent(message);
        }

        internal void HandleInfo(string message)
        {
            if (InfoEvent != null)
                InfoEvent(message);
        }
    }

    public delegate void HttpServerMessageHandler(string message);
}
