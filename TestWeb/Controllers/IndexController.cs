using EmbeddedMVC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestWeb.Controllers
{
    class IndexController : HttpController
    {
        public void Index()
        {
            View("?index");
        }

        public void Cycles()
        {
            Model.Threads = Process.GetCurrentProcess().Threads;
            View("cycles.cshtml");
        }

        public void Switch()
        {
            Model.Value = 3;
            View("switch.cshtml");
        }

        public void Redirect()
        {
            Redirect("http://google.com");
        }
    }
}
