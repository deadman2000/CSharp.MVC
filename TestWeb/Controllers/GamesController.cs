using EmbeddedMVC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TestWeb.Controllers
{
    class GamesController : HttpController
    {
        public void Index()
        {
            View("?index");
        }

        public void Json()
        {
            JSON.StartObject();
            JSON.WriteParameter("result", "OK");
            JSON.WriteParameter("content", "Привет мир=)");
            JSON.EndObject();
        }

        public void Test()
        {
            View("test.cshtml");
        }

        public void Test2()
        {
            View("test2.cshtml");
        }

        public void Post()
        {
            View("post.cshtml");
        }
    }
}
