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
            HTTP.ErrorEvent += HTTP_ErrorEvent;
            HTTP.AddResource(Views.ResourceManager, "");
            HTTP.AddResource(Images.ResourceManager, "img/icons");
            HTTP.AddPrefix("http://*:80/");
            HTTP.Start();

            ConsoleManager.Run();
        }

        static void HTTP_ErrorEvent(string message)
        {
            Console.WriteLine(message);
        }
    }
}
