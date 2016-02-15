# Simple lightweight embedded MVC server

## Featues
* Embedding to application
* Partial supporting Razor syntax
* Routing to .NET resource
* Resource caching
* View pre-compiling and caching


## TODO
http://weblogs.asp.net/scottgu/asp-net-mvc-3-razor-s-and-lt-text-gt-syntax
http://www.w3schools.com/aspnet/webpages_folders.asp
View syntax
* helpers
* expressions @( ... )
* support <text> tag
* One line html+code: @if (false){ <h1>TRUE</h1> } else { <a>asd</a> }
* comments: @* some comment here *@
* cshtml directly access

Global
* _ViewStart.cshtml (or other way to apply layout to multiple views)
* 404 page
* Error page
* controllers routing