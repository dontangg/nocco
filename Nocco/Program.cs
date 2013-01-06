using System;
using System.IO;
using CommandLine;

/*
* **Nocco** is a quick-and-dirty, literate-programming-style documentation
* generator. It is a C# port of [Docco](http://jashkenas.github.com/docco/),
* which was written by [Jeremy Ashkenas](https://github.com/jashkenas) in
* Coffescript and runs on node.js.
*
* Nocco produces HTML that displays your comments alongside your code.
* Comments are passed through
* [Markdown](http://daringfireball.net/projects/markdown/syntax), and code is
* highlighted using [google-code-prettify](http://code.google.com/p/google-code-prettify/)
* syntax highlighting. This page is the result of running Nocco against its
* own source files.
*
* Currently, to build Nocco, you'll have to have Visual Studio 2010. The project
* depends on [MarkdownSharp](http://code.google.com/p/markdownsharp/) and you'll
* have to install [.NET MVC 3](http://www.asp.net/mvc/mvc3) to get the
* System.Web.Razor assembly. The MarkdownSharp is a NuGet package that will be
* installed automatically when you build the project.
*
* To use Nocco, run it from the command-line:
*
*     nocco *.cs
*
* ...will generate linked HTML documentation for the named source files, saving
* it into a `docs` folder.
*
* The [source for Nocco](http://github.com/dontangg/nocco) is available on GitHub,
* and released under the MIT license.
*
* If **.NET** doesn't run on your platform, or you'd prefer a more convenient
* package, get [Rocco](http://rtomayko.github.com/rocco/), the Ruby port that's
* available as a gem. If you're writing shell scripts, try
* [Shocco](http://rtomayko.github.com/shocco/), a port for the **POSIX shell**.
* Both are by [Ryan Tomayko](http://github.com/rtomayko). If Python's more
* your speed, take a look at [Nick Fitzgerald](http://github.com/fitzgen)'s
* [Pycco](http://fitzgen.github.com/pycco/).
*/


namespace Nocco
{
    class Program
    {

        static void Main(string[] args)
        {
            var options = new CommandLineOptions();
            var parser = new CommandLineParser(new CommandLineParserSettings { CaseSensitive = false, IgnoreUnknownArguments = true });
            var res = parser.ParseArguments(args, options);

            if (!res)
            {
                Console.WriteLine("check your parameters and try again");
                return;
            }

            //check to see if we have a language directive for the specified type
            LanguageConfig Language = Helpers.GetLanguage(options.FileType);

            if (Language == null)
            {
                Console.WriteLine("We don't know how to handle the requested type. You can add a definition to the language.json file and try again.");
                Environment.Exit(1);
            }

            //override settings
            App.Settings.BeVerbose = options.Verbose;

            DirectoryInfo OutputDirectory = null;

            if (!string.IsNullOrWhiteSpace(options.OutputFolder))
            {

                if (Path.IsPathRooted(options.OutputFolder))
                    OutputDirectory = new DirectoryInfo(options.OutputFolder);
                else
                    OutputDirectory = new DirectoryInfo(Path.Combine(options.Path, options.OutputFolder));
            }
            else
            {
                OutputDirectory = new DirectoryInfo(Path.Combine(options.Path, App.Settings.DefaultDocsFolderName));
            }


            //get a new job
            NoccoJob Job = new NoccoJob(new DirectoryInfo(options.Path), Language, options.FileType, OutputDirectory)
            {
                ProjectName = options.ProjectName,
                IndexFilename = !string.IsNullOrWhiteSpace(options.IndexFilename) ? options.IndexFilename : App.Settings.DefaultIndexFileName,
                GenerateInlineIndex = options.GenerateInlineIndex,
                GenerateIndexFile = options.GenerateFullIndex,
                TruncateOutputDirectory = options.Truncate
            };

            //begin processing
            Nocco.ProcessJob(Job);


            Helpers.LogMessages("Finished processing");
            
            Environment.Exit(0);
        }
    }
}
