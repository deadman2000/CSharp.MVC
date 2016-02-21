using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;

namespace EmbeddedMVC
{
    /// <summary>
    /// Хранит список всех доступных методов. Передает запрос методу
    /// </summary>
    class ControllerDescription
    {
        private HttpServer _server;
        private Type _type;

        private Dictionary<string, MethodInfo> _methods;

        public ControllerDescription(HttpServer server, Type type)
        {
            _server = server;
            _type = type;
            _methods = (from m in type.GetMethods(BindingFlags.Public | BindingFlags.Instance).AsParallel()
                        select m).ToDictionary(t => t.Name.ToLower(), t => t);
        }

        public bool Process(HttpListenerContext context)
        {
            var segments = context.Request.Url.Segments;

            string action;
            if (segments.Length < 3)
                action = "index";
            else
                action = segments[2].TrimEnd('/').ToLower();

            MethodInfo mi;
            if (!_methods.TryGetValue(action, out mi))
                return false;

            HttpController ctrl = (HttpController)Activator.CreateInstance(_type);
            ctrl.Init(_server, context);
            try
            {
                mi.Invoke(ctrl, new object[0]);
            }
            catch (TargetInvocationException tex)
            {
                ctrl.WriteException(tex.InnerException);
            }
            catch (Exception ex)
            {
                ctrl.WriteException(ex);
            }
            ctrl.Finish();

            return true;
        }
    }
}
