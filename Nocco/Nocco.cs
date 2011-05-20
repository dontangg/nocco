// **Nocco** is a quick-and-dirty, hundred-line-long, literate-programming-style
// documentation generator. It produces HTML that displays your comments
// alongside your code. Comments are passed through
// [Markdown](http://daringfireball.net/projects/markdown/syntax), and code is
// passed through [Pygments](http://pygments.org/) syntax highlighting.
// This page is the result of running Nocco against its own source file.
//
// If you install Nocco, you can run it from the command-line:
//
//     nocco *.cs
//
// ...will generate linked HTML documentation for the named source files, saving
// it into a `docs` folder.
//
// The [source for Nocco](http://github.com/dontangg/nocco) is available on GitHub,
// and released under the MIT license.
//
// To install Docco, first make sure you have [Node.js](http://nodejs.org/),
// [Pygments](http://pygments.org/) (install the latest dev version of Pygments
// from [its Mercurial repo](http://dev.pocoo.org/hg/pygments-main)), and
// [CoffeeScript](http://coffeescript.org/). Then, with NPM:
//
//     sudo npm install docco
//
// If **.NET** doesn't run on your platform, or you'd prefer a more convenient
// package, get [Rocco](http://rtomayko.github.com/rocco/), the Ruby port that's
// available as a gem. If you're writing shell scripts, try
// [Shocco](http://rtomayko.github.com/shocco/), a port for the **POSIX shell**.
// Both are by [Ryan Tomayko](http://github.com/rtomayko). If Python's more
// your speed, take a look at [Nick Fitzgerald](http://github.com/fitzgen)'s
// [Pycco](http://fitzgen.github.com/pycco/).

// Import namespaces to allow us to type shorter type names.
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Razor;

namespace Nocco {
	class Nocco {
		private static string ExecutingDirectory;
		private static List<string> Files;
		private static Type TemplateType;

		//### Main Documentation Generation Functions

		// Generate the documentation for a source file by reading it in, splitting it
		// up into comment/code sections, highlighting them for the appropriate language,
		// and merging them into an HTML template.
		private static void GenerateDocumentation(string source) {
			var lines = File.ReadAllLines(source);
			var sections = Parse(source, lines);
			Hightlight(source, sections);
			GenerateHtml(source, sections);
		}

		// Given a string of source code, parse out each comment and the code that
		// follows it, and create an individual [Section](Section.html) for it.
		private static List<Section> Parse(string source, string[] lines) {
			List<Section> sections = new List<Section>();
			var language = GetLanguage(source);
			var hasCode = false;
			var docsText = new StringBuilder();
			var codeText = new StringBuilder();

			Action<string, string> save = (string docs, string code) => sections.Add(new Section() { DocsHtml = docs, CodeHtml = code });

			foreach (var line in lines) {
				if (language.CommentMatcher.IsMatch(line) && !language.CommentFilter.IsMatch(line)) {
					if (hasCode) {
						save(docsText.ToString(), codeText.ToString());
						hasCode = false;
						docsText = new StringBuilder();
						codeText = new StringBuilder();
					}
					docsText.AppendLine(language.CommentMatcher.Replace(line, ""));
				}
				else {
					hasCode = true;
					codeText.AppendLine(line);
				}
			}
			save(docsText.ToString(), codeText.ToString());

			return sections;
		}

		// Highlights a single chunk of code, using a web service for **Pygments**,
		// and runs the text of its corresponding comment through **Markdown**, using a
		// C# implementation called [MarkdownSharp](http://code.google.com/p/markdownsharp/).
		//
		// We process the entire file in a single call to Pygments by inserting little
		// marker comments between each section and then splitting the result string
		// wherever our markers occur. (I think I'll probably switch to [prettify](http://code.google.com/p/google-code-prettify/)
		private static void Hightlight(string source, List<Section> sections) {
			var language = GetLanguage(source);

			var request = (HttpWebRequest)WebRequest.Create("http://pygments.appspot.com/");
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";

			var allCodeText = string.Join(language.DividerText, sections.Select(s => s.CodeHtml));
			string postData = string.Format("lang={0}&code={1}", language.Name, System.Web.HttpUtility.UrlEncode(allCodeText));
			byte[] byteArray = Encoding.UTF8.GetBytes(postData);
			request.ContentLength = byteArray.Length;
			using (var stream = request.GetRequestStream()) {
				stream.Write(byteArray, 0, byteArray.Length);
			}

			var response = (HttpWebResponse)request.GetResponse();
			if (response.StatusCode == HttpStatusCode.OK) {
				using (var stream = response.GetResponseStream()) {
					var reader = new StreamReader(stream);
					allCodeText = reader.ReadToEnd();
				}
			}

			allCodeText = allCodeText.Replace("<div class=\"highlight\"><pre>", "").Replace("</pre></div>", "");
			var fragments = language.DividerHTML.Split(allCodeText);

			var markdown = new MarkdownSharp.Markdown();

			for (var i=0; i<sections.Count; i++) {
				var section = sections[i];
				section.DocsHtml = markdown.Transform(section.DocsHtml);
				section.CodeHtml = string.Format("<div class='highlight'><pre>{0}</pre></div>", fragments[i]);
			}
		}

		// Once all of the code is finished highlighting, we can generate the HTML file
		// and write out the documentation. Pass the completed sections into the template
		// found in `Resources/Nocco.cshtml`
		private static void GenerateHtml(string source, List<Section> sections) {
			int depth;
			var destination = GetDestination(source, out depth);
			
			string pathToRoot = "";
			for (var i = 0; i < depth; i++)
				pathToRoot = Path.Combine("..", pathToRoot);

			var htmlTemplate = Activator.CreateInstance(TemplateType) as TemplateBase;

			htmlTemplate.Title = Path.GetFileName(source);
			htmlTemplate.PathToCss = Path.Combine(pathToRoot, "nocco.css").Replace('\\', '/');
			htmlTemplate.GetSourcePath = (string s) => Path.Combine(pathToRoot, Path.ChangeExtension(s.ToLower(), ".html").Substring(2)).Replace('\\', '/');
			htmlTemplate.Sections = sections;
			htmlTemplate.Sources = Files;
			
			htmlTemplate.Execute();

			File.WriteAllText(destination, htmlTemplate.Buffer.ToString());
		}

		//### Helpers & Setup

		private static Type SetupRazorTemplate() {
			RazorEngineHost host = new RazorEngineHost(new CSharpRazorCodeLanguage());
			host.DefaultBaseClass = typeof(TemplateBase).FullName;
			host.DefaultNamespace = "RazorOutput";
			host.DefaultClassName = "Template";
			host.NamespaceImports.Add("System");
			RazorTemplateEngine templateEngine = new RazorTemplateEngine(host);

			GeneratorResults razorResult = null;
			using (var reader = new StreamReader(Path.Combine(ExecutingDirectory, "Resources", "Nocco.cshtml"))) {
				razorResult = templateEngine.GenerateCode(reader);
			}

			var codeProvider = new Microsoft.CSharp.CSharpCodeProvider();

			string outputAssemblyName = Path.Combine(Path.GetTempPath(), String.Format("Template_{0}.dll", Guid.NewGuid().ToString("N")));
			CompilerResults results = codeProvider.CompileAssemblyFromDom(
				new CompilerParameters(new string[] {
					typeof(Nocco).Assembly.CodeBase.Replace("file:///", "").Replace("/", "\\")
				}, outputAssemblyName),
				razorResult.GeneratedCode);

			// TODO: Error checking is for wooses
			if (results.Errors.HasErrors) {
				var err = results.Errors.OfType<CompilerError>().Where(ce => !ce.IsWarning).First();
				Console.WriteLine("Error Compiling Template: ({0}, {1}) {2}", err.Line, err.Column, err.ErrorText);
			}

			var asm = System.Reflection.Assembly.LoadFrom(outputAssemblyName);

			return asm.GetType("RazorOutput.Template");
		}

		// A list of the languages that Nocco supports, mapping the file extension to
		// the name of the Pygments lexer and the symbol that indicates a comment. To
		// add another language to Nocco's repertoire, add it here.
		private static Dictionary<string, Language> Languages = new Dictionary<string, Language> {
			{ ".js", new Language {
				Name = "javascript",
				Symbol = "//"
			}},
			{ ".cs", new Language {
				Name = "csharp",
				Symbol = "//"
			}},
			{ ".vb", new Language {
				Name = "vb.net",
				Symbol = "'"
			}}
		};

		// Get the current language we're documenting, based on the extension.
		private static Language GetLanguage(string source) {
			var extension = Path.GetExtension(source);
			return Languages.ContainsKey(extension) ? Languages[extension] : null;
		}

		// Compute the destination HTML path for an input source file path. If the source
		// is `Example.cs`, the HTML will be at `docs/example.html`
		private static string GetDestination(string filepath, out int depth) {
			var dirs = Path.GetDirectoryName(filepath).Substring(1).Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
			depth = dirs.Length;

			var dest = "docs";
			foreach(var dir in dirs)
				dest = Path.Combine(dest, dir);

			EnsureDirectory(dest);

			return Path.Combine(dest, Path.GetFileNameWithoutExtension(filepath).ToLower() + ".html");
		}

		// Ensure that the destination directory exists.
		private static void EnsureDirectory(string dir) {
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
		}

		public static void Generate(string[] targets) {
			if (targets.Length > 0) {
				EnsureDirectory("docs");

				ExecutingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
				File.Copy(Path.Combine(ExecutingDirectory, "Resources", "Nocco.css"), Path.Combine("docs", "nocco.css"), true);

				TemplateType = SetupRazorTemplate();

				Files = new List<string>();
				foreach (var target in targets)
					Files.AddRange(Directory.GetFiles(".", target, SearchOption.AllDirectories).Where(filename => GetLanguage(Path.GetFileName(filename)) != null));

				foreach (var file in Files)
					GenerateDocumentation(file);

			}
		}
	}
}
