# Simple lightweight embedded MVC server

## Featues
* Embedding to application
* Basically supporting Razor syntax
* Routing to .NET resource
* Resource caching
* View pre-compiling and caching


## TODO
http://weblogs.asp.net/scottgu/asp-net-mvc-3-razor-s-and-lt-text-gt-syntax
http://www.w3schools.com/aspnet/webpages_folders.asp
View syntax
* @if
* @switch
* @for
* @foreach
* helpers
* expressions @( ... )
* support <text> tag
* support @:
@if (false){ <h1>TRUE</h1> } else { <a>asd</a>}
* comments: @* some comment here *@

Global
* _ViewStart.cshtml (or other way to apply layout to multiple views)
* 404 page
* Error page
* controllers routing