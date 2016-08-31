using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmbeddedMVC
{
    class ViewCompiler
    {
        private string cshtml;
        private static bool DEBUG = false;

        public ViewCompiler(ViewCompiler parent, string cshtml)
        {
            usings = parent.usings;
            this.cshtml = cshtml;
        }

        public ViewCompiler(string cshtml)
        {
            this.cshtml = cshtml;
        }

        public Type Build()
        {
            if (DEBUG) Console.WriteLine("Building...");
            string src = Generate();
            if (DEBUG)
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine(src);
                Console.WriteLine();
                Console.WriteLine();
            }
            //if (DEBUG) File.WriteAllText("D:\\Temp\\temp.cs", src);

            return Compile(src);
        }

        #region Generator

        private int offset;

        private bool IsAvailable
        {
            get { return offset < cshtml.Length; }
        }

        /// <summary>
        /// Generate cs code from cshtml
        /// </summary>
        /// <returns></returns>
        private string Generate()
        {
            string body = GenerateRender();

            StringBuilder sbClass = new StringBuilder();
            sbClass.AppendLine("using System;");
            foreach (var str in usings)
                sbClass.Append(str).AppendLine(";");
            sbClass.AppendLine("namespace CustomView{class CustomHttpView:EmbeddedMVC.HttpView{protected override void Render(){");
            sbClass.Append(body);
            sbClass.Append("}}}");
            return sbClass.ToString();
        }

        private StringBuilder csCode;

        private List<string> usings = new List<string>();


        /// <summary>
        /// Generate render method
        /// </summary>
        private string GenerateRender()
        {
            csCode = new StringBuilder();
            int htmlStart = 0;
            while (IsAvailable)
            {
                SkipTo('@');

                if (IsAvailable && cshtml[offset + 1] == '@')
                {
                    offset += 2;
                    continue;
                }

                string html = cshtml.Substring(htmlStart, offset - htmlStart);
                if (html.Length > 0)
                    WriteHtml(html);

                if (!IsAvailable)
                    break;

                offset++;
                if (cshtml[offset] == '{') // Code
                {
                    string code = ReadCode();
                    WriteCode(code);
                }
                else if (Char.IsLetter(cshtml[offset])) // Expression
                {
                    int exprStart = offset;
                    string expression = ReadExpression();
                    if (expression.Equals("if"))
                    {
                        SkipBracers();
                        SkipCode();
                        if (IsNextWord("else"))
                            SkipCode();
                        string code = cshtml.Substring(exprStart, offset - exprStart);
                        WriteCode(code);
                    }
                    else if (expression.Equals("for") || expression.Equals("foreach") || expression.Equals("switch"))
                    {
                        SkipBracers();
                        SkipCode();
                        string code = cshtml.Substring(exprStart, offset - exprStart);
                        if (DEBUG) Console.WriteLine(expression + " code: " + code);
                        WriteCode(code);
                    }
                    else if (expression.Equals("using"))
                    {
                        SkipLine();
                        string usingLine = cshtml.Substring(exprStart, offset - exprStart);
                        if (DEBUG) Console.WriteLine("using: " + usingLine);
                        usings.Add(usingLine.TrimEnd(';'));
                    }
                    else
                        WriteExpression(expression);
                }
                else
                    throw new FormatException("Unknown symbol " + cshtml[offset]);

                htmlStart = offset;
            }

            return csCode.ToString();
        }

        private bool IsNextWord(string pattern)
        {
            int start = -1;
            for (int i = offset; i < cshtml.Length; i++)
            {
                char c = cshtml[i];
                if (!Char.IsLetterOrDigit(c))
                {
                    if (start != -1)
                        return cshtml.Substring(start, i - start).Equals(pattern);
                }
                else
                {
                    if (start == -1)
                        start = i;
                }
            }
            return false;
        }

        /// <summary>
        /// Returning code body between { }
        /// </summary>
        /// <returns></returns>
        private string ReadCode()
        {
            int codeStart = offset + 1;
            SkipCode();
            return cshtml.Substring(codeStart, offset - codeStart - 1);
        }

        private void SkipCode()
        {
            if (!IsAvailable)
                return;

            int lvl = 0;
            for (; offset < cshtml.Length; offset++)
            {
                char c = cshtml[offset];
                switch (c)
                {
                    case '\'':
                    case '"':
                        SkipString();
                        break;
                    case '{':
                        lvl++;
                        //Console.WriteLine("OPEN " + lvl);
                        break;
                    case '}':
                        lvl--;
                        //Console.WriteLine("CLOSE " + lvl);
                        offset++; // Skip }
                        if (lvl == 0)
                            return;
                        break;
                }
            }
        }

        private string ReadExpression()
        {
            int codeStart = offset;
            for (; offset < cshtml.Length; offset++)
            {
                char c = cshtml[offset];
                if (c == '(')
                {
                    SkipBracers();
                    continue;
                }

                if (!Char.IsLetterOrDigit(c) && c != '.')
                    break;
            }

            return cshtml.Substring(codeStart, offset - codeStart);
        }

        private void SkipString()
        {
            char q = cshtml[offset];
            bool regular = offset == 0 || cshtml[offset - 1] != '@';
            offset++;
            int strStart = offset;
            for (; offset < cshtml.Length; offset++) // Searching closing quote
            {
                if (cshtml[offset] == '\\' && regular)
                {
                    offset++;
                    continue;
                }

                if (cshtml[offset] == q)
                {
                    offset++;
                    return;
                }
            }

            throw new Exception("End of string not found");
        }

        private void SkipLine()
        {
            for (; offset < cshtml.Length; offset++) // Searching closing bracer
            {
                switch (cshtml[offset])
                {
                    case '\r':
                    case '\n':
                        return;
                }
            }
        }

        private void SkipBracers()
        {
            int lvl = 0;
            for (; offset < cshtml.Length; offset++) // Searching closing bracer
            {
                switch (cshtml[offset])
                {
                    case '\'':
                    case '"':
                        SkipString();
                        offset--;
                        break;
                    case '(':
                        lvl++;
                        //Console.WriteLine("OPEN " + lvl);
                        break;
                    case ')':
                        lvl--;
                        //Console.WriteLine("CLOSE " + lvl);
                        if (lvl == 0)
                            return;
                        break;
                }
            }

            throw new Exception("Closing bracer not found");
        }

        private void SkipTo(char p)
        {
            if (!IsAvailable)
                return;

            for (; offset < cshtml.Length; offset++)
            {
                char c = cshtml[offset];
                if (p == c)
                    return;
            }
        }

        private void WriteHtml(string html)
        {
            html = html.Replace("\\", "\\\\").Replace("\n", "").Replace("\r", "").Replace("\"", "\\\"").Replace("@@", "@");
            if (html.Length > 0)
                csCode.AppendLine("Write(\"" + html + "\");");
        }

        private void WriteExpression(string expression)
        {
            csCode.AppendLine("Write(" + expression + ");");
        }

        private void WriteCode(string code)
        {
            bool htmlMode = false;
            //StringBuilder html = null;
            //string regexp = null; // Closing regular

            string[] lines = code.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0) continue;

                if (line[0] == '@') // Начинается с @ - у нас код
                {
                    if (line.Length == 1)
                        throw new Exception("Escape symbol error");

                    if (Char.IsLetter(line[1]))
                    {
                        if (DEBUG) Console.WriteLine("Compile line: " + line);
                        ViewCompiler htmlCompiler = new ViewCompiler(this, line);
                        string cs = htmlCompiler.GenerateRender();
                        csCode.Append(cs);
                    }
                    else if (line[1] == ':')
                    {
                        if (DEBUG) Console.WriteLine("Compile line: " + line.Substring(2));
                        ViewCompiler htmlCompiler = new ViewCompiler(this, line.Substring(2));
                        string cs = htmlCompiler.GenerateRender();
                        csCode.Append(cs);
                    }
                    else if (line[1] == '{')
                    {
                        if (DEBUG) Console.WriteLine("CS: " + line.Substring(1));
                        csCode.Append(line.Substring(1));
                    }
                    else
                        throw new Exception("Unknown escape");

                    continue;
                }


                if (line.StartsWith("<"))
                {
                    if (DEBUG) Console.WriteLine("HTML: " + line);
                    ViewCompiler htmlCompiler = new ViewCompiler(this, line);
                    string cs = htmlCompiler.GenerateRender();
                    csCode.Append(cs);
                }
                else
                {
                    if (DEBUG) Console.WriteLine("CS: " + line);
                    csCode.AppendLine(line);
                }

                /*if (!htmlMode)
                {
                    var matches = Regex.Matches(line, "^<\\w+");
                    if (matches.Count > 0)
                    {
                        var m = matches[0];
                        regexp = "<\\/" + m.Value.Substring(1) + ">";
                        htmlMode = true;

                        html = new StringBuilder();
                    }
                    else
                        csCode.AppendLine(line);
                }

                if (htmlMode)
                {
                    html.Append(line);
                    if (Regex.IsMatch(line, regexp))
                    {
                        ViewCompiler htmlCompiler = new ViewCompiler(this, html.ToString());
                        string cs = htmlCompiler.GenerateRender();
                        csCode.Append(cs);
                        htmlMode = false;
                        html = null;
                    }
                }*/
            }

            if (htmlMode)
                throw new Exception("Closing tag not found");
        }

        #endregion

        #region Compiler

        /// <summary>
        /// Compile generated cs code
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        private Type Compile(string src)
        {
            CSharpCodeProvider codeProvider = new CSharpCodeProvider(new Dictionary<String, String> { { "CompilerVersion", "v4.0" } });
            CompilerParameters parameters = new CompilerParameters();
            /*parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Core.dll");
            parameters.ReferencedAssemblies.Add("System.Data.dll");
            parameters.ReferencedAssemblies.Add("Microsoft.CSharp.dll");*/
            //parameters.ReferencedAssemblies.Add(typeof(ViewCompiler).Assembly.Location);
            //parameters.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            //parameters.ReferencedAssemblies.Add(Assembly.GetEntryAssembly().Location);
            var refs = AppDomain.CurrentDomain.GetAssemblies();
            HashSet<string> imported = new HashSet<string>();
            foreach (var a in refs)
            {
                if (a.IsDynamic) continue;
                if (imported.Contains(a.FullName)) continue;
                imported.Add(a.FullName);
                parameters.ReferencedAssemblies.Add(a.Location);
            }

            parameters.GenerateExecutable = false;
            parameters.GenerateInMemory = true;

            CompilerResults results = codeProvider.CompileAssemblyFromSource(parameters, src);
            if (results.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Compiling errors:");
                foreach (CompilerError compErr in results.Errors)
                    sb.AppendLine("Line: " + compErr.Line + ", Column: " + compErr.Column + ", Error Number: " + compErr.ErrorNumber + ", " + compErr.ErrorText);
                throw new Exception(sb.ToString());
            }

            return results.CompiledAssembly.GetTypes()[0];
        }

        #endregion

        #region Static compilers

        private static Dictionary<string, CachedView> _fileCache = new Dictionary<string, CachedView>();

        public static HttpView CompileFile(string filePath)
        {
            CachedView cached;
            DateTime fw = File.GetLastWriteTime(filePath);
            if (!_fileCache.TryGetValue(filePath, out cached) || cached.LastWrite != fw) // TODO Static mode without date checking
            {
                lock (_fileCache)
                {
                    _fileCache.TryGetValue(filePath, out cached);

                    if (cached == null || cached.LastWrite != fw)
                    {
                        string text = File.ReadAllText(filePath);

                        Type t = new ViewCompiler(text).Build();
                        cached = new CachedView(fw, t);
                        _fileCache[filePath] = cached;
                    }
                }
            }

            return (HttpView)Activator.CreateInstance(cached.TView);
        }

        public static HttpView CompileSource(string source)
        {
            Type t = new ViewCompiler(source).Build();
            return (HttpView)Activator.CreateInstance(t);
        }

        #endregion
    }
}
