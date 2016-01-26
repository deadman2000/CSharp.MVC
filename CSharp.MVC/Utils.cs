using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace EmbeddedMVC
{
    public static class Utils
    {
        public static void NameThread(string name)
        {
            Thread.CurrentThread.Name = name;
        }

        public static string Render(string cshtml)
        {
            return ViewCompiler.CompileSource(cshtml).Process();
        }
    }
}
