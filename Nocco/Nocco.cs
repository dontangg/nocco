// **Nocco** is a quick-and-dirty, literate-programming-style documentation
// generator. It is a C# port of [Docco](http://jashkenas.github.com/docco/),
// which was written by [Jeremy Ashkenas](https://github.com/jashkenas) in
// Coffescript and runs on node.js.
//
// Nocco produces HTML that displays your comments alongside your code.
// Comments are passed through
// [Markdown](http://daringfireball.net/projects/markdown/syntax), and code is
// highlighted using [google-code-prettify](http://code.google.com/p/google-code-prettify/)
// syntax highlighting. This page is the result of running Nocco against its
// own source files.
//
// Currently, to build Nocco, you'll have to have Visual Studio 2010. The project
// depends on [MarkdownSharp](http://code.google.com/p/markdownsharp/) and you'll
// have to install [.NET MVC 3](http://www.asp.net/mvc/mvc3) to get the
// System.Web.Razor assembly.
//
// To use Nocco, run it from the command-line:
//
//     nocco *.cs
//
// ...will generate linked HTML documentation for the named source files, saving
// it into a `docs` folder.
//
// The [source for Nocco](http://github.com/dontangg/nocco) is available on GitHub,
// and released under the MIT license.
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
		private static List<string> Files;
		private static EmbeddedResources EmbeddedResources = new EmbeddedResources();

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
		// follows it, and create an individual `Section` for it.
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

		// Prepares a single chunk of code for HTML output and runs the text of its
		// corresponding comment through **Markdown**, using a C# implementation
		// called [MarkdownSharp](http://code.google.com/p/markdownsharp/).
		private static void Hightlight(string source, List<Section> sections) {
			var markdown = new MarkdownSharp.Markdown();

			for (var i=0; i<sections.Count; i++) {
				var section = sections[i];
				section.DocsHtml = markdown.Transform(section.DocsHtml);
				section.CodeHtml = System.Web.HttpUtility.HtmlEncode(section.CodeHtml);
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

            var htmlTemplate = EmbeddedResources.CreateRazorTemplateInstance();

			htmlTemplate.Title = Path.GetFileName(source);
            htmlTemplate.GetResourcePath = (string s) => Path.Combine(pathToRoot, s).Replace('\\', '/');
			htmlTemplate.GetSourcePath = (string s) => Path.Combine(pathToRoot, Path.ChangeExtension(s.ToLower(), ".html").Substring(2)).Replace('\\', '/');
			htmlTemplate.Sections = sections;
			htmlTemplate.Sources = Files;
			
			htmlTemplate.Execute();

			File.WriteAllText(destination, htmlTemplate.Buffer.ToString());
		}

		// A list of the languages that Nocco supports, mapping the file extension to
		// the symbol that indicates a comment. To add another language to Nocco's
		// repertoire, add it here.
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

			var dest = Path.Combine("docs", string.Join(Path.DirectorySeparatorChar.ToString(), dirs)).ToLower();
			Directory.CreateDirectory(dest);

			return Path.Combine("docs", Path.ChangeExtension(filepath, "html").ToLower());
		}

		// Find all the files that match the pattern(s) passed in as arguments and
		// generate documentation for each one.
		public static void Generate(string[] targets) {
			if (targets.Length > 0) {
				Directory.CreateDirectory("docs");

				EmbeddedResources.WriteClientFilesTo("docs");

				Files = new List<string>();
				foreach (var target in targets)
					Files.AddRange(Directory.GetFiles(".", target, SearchOption.AllDirectories).Where(filename => GetLanguage(Path.GetFileName(filename)) != null));

				foreach (var file in Files)
					GenerateDocumentation(file);
			}
		}
	}
}
