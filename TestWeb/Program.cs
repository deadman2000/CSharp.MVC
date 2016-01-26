using EmbeddedMVC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TestWeb
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpServer http = new HttpServer();
            http.AddResource(Views.ResourceManager, "");
            http.AddResource(Images.ResourceManager, "img/icons");
            http.Start(90);

            ConsoleManager.Run();
        }
    }
}
