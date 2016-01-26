using EmbeddedMVC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestWeb.Controllers
{
    class AuthController : HttpController
    {
        public void Login()
        {
            if (IsPost)
            {
                var login = Request["login"];
                var pass = Request["password"];
                Console.WriteLine("Login: {0} - {1}", login, pass);

                var sess = OpenSession();
                sess.Data.Login = login;
                Redirect("/");
            }
            else
            {
                View("login.cshtml");
            }
        }

        public void Logout()
        {
            Console.WriteLine("Logout");
            CloseSession();
            Redirect("/");
        }
    }
}
