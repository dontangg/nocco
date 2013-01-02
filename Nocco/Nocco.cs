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
// System.Web.Razor assembly. The MarkdownSharp is a NuGet package that will be
// installed automatically when you build the project.
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


using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Razor;

namespace Nocco
{
    public class Nocco
    {
        public static bool BeVerbose = false;

        public static readonly string ExecutingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        private static readonly MarkdownSharp.Markdown MarkdownFormatter = new MarkdownSharp.Markdown();

        //TODO: these should be in a config file or come from the command line
        private static readonly string DocsFolderName = "docs";
        private static readonly string IndexFileName = "index.html";
        private static readonly string AbsoluteResourceDirectory = Path.Combine(ExecutingDirectory, "Resources");
        private static readonly FileInfo DocumentTemplateFile = new FileInfo(Path.Combine(ExecutingDirectory, "Templates", "document.cshtml"));
        private static readonly FileInfo IndexTemplateFile = new FileInfo(Path.Combine(ExecutingDirectory, "Templates", "index.cshtml"));


        /// <summary>
        /// Characters to trim from the start of comment lines.
        /// </summary>
        private static readonly char[] CommentLineTrim = new[] { '\t', ' ' };


        /// <summary>
        /// Writes <paramref name="messages"/> to the console if <see cref="Nocco.BeVerbose"/> is true
        /// </summary>
        /// <param name="message"></param>
        public static void LogMessages(params string[] messages)
        {
            if (!BeVerbose)
                return;

            foreach (string message in messages)
                Console.WriteLine(message);
        }


        /// <summary>
        /// Find all the files that match the pattern(s) passed in as arguments and
        /// generate documentation for each one.
        /// </summary>
        /// <param name="targets"></param>
        public static void Generate(string targetDirectory, string fileType, string projectName)
        {
            //create new job
            NoccoJob Job = new NoccoJob(new DirectoryInfo(targetDirectory), Helpers.GetLanguage(fileType), fileType, projectName);

            LogMessages(
                    "Starting new job: ",
                    "Path: " + Job.JobBaseDirectory.ToString(),
                    "Language: " + Job.Language.Name
                );

            //process the job
            ProcessJob(Job);
        }


        /// <summary>
        /// Generate the documentation for all files in <paramref name="job"/> 
        /// </summary>
        /// <param name="job"></param>
        private static void ProcessJob(NoccoJob job)
        {
            //create docs folder and deliver resources
            DirectoryInfo DocsFolder = new DirectoryInfo(Path.Combine(job.JobBaseDirectory.FullName, DocsFolderName));

            LogMessages("Destination folder: " + DocsFolder.ToString());

            //create docs folder 
            if (!DocsFolder.Exists)
                DocsFolder.Create();

            //deliver resources by cloning the directory
            foreach (string file in Directory.GetFiles(AbsoluteResourceDirectory, "*.*", SearchOption.AllDirectories))
            {
                string destFile = file.Replace(AbsoluteResourceDirectory, DocsFolder.FullName);
                string destDir = Path.GetDirectoryName(destFile);

                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                File.Copy(file, destFile, true);
            }

            LogMessages("Cloned base resources to destination");


            List<DocumentSummary> GeneratedDocuments = new List<DocumentSummary>();

            //process each candidate file            
            foreach (FileInfo candidate in job.GetCandidates())
            {
                GeneratedDocuments.Add(GenerateDocumentHtml(candidate, DocsFolder, job));
            }


            //create the index file
            GenerateIndexHtml(DocsFolder, job, GeneratedDocuments);


        }


        /// <summary>
        /// Generates the index HTML for a job.
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="docsDirectory"></param>
        /// <param name="job"></param>
        private static void GenerateIndexHtml(DirectoryInfo docsDirectory, NoccoJob job, ICollection<DocumentSummary> generatedDocuments)
        {
            //the absolute destination path for the index file
            var destinationFile = new FileInfo(Path.Combine(docsDirectory.FullName, IndexFileName));

            //the relative path from the destination file to the documation folder
            string docsRelative = Helpers.GetPathRelativePathTo(destinationFile.Directory, docsDirectory);

            AbIndexTemplate TemplateGenerator = Helpers.GetTemplateGenerator<AbIndexTemplate>(IndexTemplateFile);

            //setup template generator settings
            TemplateGenerator.Title = job.ProjectName ?? "index";
            TemplateGenerator.DocsRelative = docsRelative;

            var documents = generatedDocuments.ToArray();

            TemplateGenerator.GeneratedDocuments = documents;

            //generate documenation file
            TemplateGenerator.Generate(destinationFile.FullName);
        }


        /// <summary>
        /// Generates the HTML result for a single source file.
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="docsDirectory"></param>
        /// <param name="job"></param>
        /// <returns>Generated document FileInfo</returns>
        private static DocumentSummary GenerateDocumentHtml(FileInfo sourceFile, DirectoryInfo docsDirectory, NoccoJob job, ICollection<FileInfo> othersInSameJob = null)
        {
            DocumentSummary summary = new DocumentSummary() { DocumentFile = sourceFile };

            string subDirectory = sourceFile.DirectoryName.Replace(job.JobBaseDirectory.FullName, null);

            //if nothing was replaced, then we have no sub directory
            if (subDirectory == sourceFile.DirectoryName)
                subDirectory = null;
            

            string docFilename  = Path.ChangeExtension(sourceFile.Name, "html");


            //the absolute destination path for the documentation file
            var destinationFile = new FileInfo(Path.Combine(
                docsDirectory.FullName,
                subDirectory ?? string.Empty,
                docFilename));

            //the relative path from the destination file to the documation folder
            string docsRelative = Helpers.GetPathRelativePathTo(destinationFile.Directory, docsDirectory);
            
            //get the opposite direction for the index page
            summary.RelativeUri = Helpers.ConvertPathSeparator(Path.Combine(
                    Helpers.GetPathRelativePathTo(docsDirectory, destinationFile.Directory), docFilename));

            AbDocumentTemplate TemplateGenerator = Helpers.GetTemplateGenerator<AbDocumentTemplate>(DocumentTemplateFile);

            //setup template generator settings
            TemplateGenerator.Title = sourceFile.Name;
            summary.Title = TemplateGenerator.Title;

            TemplateGenerator.Sections = ParseSections(sourceFile, job.Language, summary);
            TemplateGenerator.DocsRelative = docsRelative;
            TemplateGenerator.IndexFile = Helpers.ConvertPathSeparator(Path.Combine(docsRelative, IndexFileName));

            //generate documenation file
            TemplateGenerator.Generate(destinationFile.FullName);

            return summary;
        }

        /// <summary>
        /// Create separated and formatted sections for comments and code for use in documenation file 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        private static IEnumerable<Section> ParseSections(FileInfo source, LanguageConfig language, DocumentSummary summary)
        {
            var sections = new List<Section>();
            var hasCode = false;
            var docsText = new StringBuilder();
            var codeText = new StringBuilder();

            bool OkayToReplaceSection = true;

            //generates a section of comment and code
            Func<StringBuilder, StringBuilder, Section> GenerateSection = (docs, code) =>
            {
                var docsString = docs.ToString();

                //highlight comments if required
                if (language.MarkdownMaps != null)
                {
                    docsString = language.MarkdownMaps.Aggregate(docsString,
                        (currentDocs, map) =>
                            Regex.Replace(currentDocs, map.FindPattern, map.Replacement, RegexOptions.Multiline)
                        );
                }

                var ret = new Section
                {
                    DocsHtml = MarkdownFormatter.Transform(docsString),
                    CodeHtml = System.Web.HttpUtility.HtmlEncode(code.ToString())
                };


                //set the top section for the summary, just in case, but ideally we want the second block 
                //or a block after the first which has both sections
                if (summary.TopSection == null)
                {
                    //easy winner
                    summary.TopSection = ret;
                }
                else if (OkayToReplaceSection)
                {
                    summary.TopSection = ret;
                    OkayToReplaceSection = false;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(summary.TopSection.CodeHtml) ||
                        string.IsNullOrWhiteSpace(summary.TopSection.DocsHtml))
                    {
                        if (!string.IsNullOrWhiteSpace(ret.CodeHtml) &&
                            !string.IsNullOrWhiteSpace(ret.DocsHtml))
                        {
                            //winner by content superiority
                            summary.TopSection = ret;
                        }
                    }
                }

                return ret;

            };

            foreach (var line in File.ReadAllLines(source.FullName))
            {
                //if this line matches a comment line
                if (language.CommentMatcher.IsMatch(line) && !language.CommentFilter.IsMatch(line))
                {
                    //if we hit this comment line after already processing code, we need to make a new section
                    if (hasCode)
                    {
                        yield return GenerateSection(docsText, codeText);

                        hasCode = false;
                        docsText = new StringBuilder();
                        codeText = new StringBuilder();
                    }

                    //update the summary
                    summary.LinesOfComment++;

                    docsText.AppendLine(language.CommentMatcher.Replace(line, "").Trim(CommentLineTrim));
                }
                else //hit code line
                {
                    //update the summary
                    summary.LinesOfCode++;

                    hasCode = true;
                    codeText.AppendLine(line);
                }
            }

            yield return GenerateSection(docsText, codeText);
        }
    }
}
