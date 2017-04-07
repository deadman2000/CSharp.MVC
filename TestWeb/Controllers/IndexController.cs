using EmbeddedMVC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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


        byte[] _belaz = null;
        public void Delay()
        {
            Thread.Sleep(10000);
            if (_belaz == null)
            {
                _belaz = ImageToByteArray(Images.BELAZ);
            }

            WriteContent(200, "image/png", _belaz);
        }
        byte[] ImageToByteArray(System.Drawing.Image imageIn)
        {
            using (var ms = new MemoryStream())
            {
                imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
        }

        public void Redirect()
        {
            Redirect("http://google.com");
        }
    }
}
