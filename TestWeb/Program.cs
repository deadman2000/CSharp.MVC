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
        public static HttpServer HTTP;

        static void Main(string[] args)
        {
            HTTP = new HttpServer();
            HTTP.AddResource(Views.ResourceManager, "");
            HTTP.AddResource(Images.ResourceManager, "img/icons");
            HTTP.AddPrefix("http://*:90/");
            HTTP.Start();

            ConsoleManager.Run();
        }
    }
}
