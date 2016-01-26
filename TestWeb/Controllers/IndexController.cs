using EmbeddedMVC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestWeb.Controllers
{
    class IndexController : HttpController
    {
        public void Index()
        {
            View("?index");
        }
    }
}
