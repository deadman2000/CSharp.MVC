using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmbeddedMVC
{
    static class Log
    {
        public static void HandleException(Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
