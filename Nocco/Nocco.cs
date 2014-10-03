using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Nocco
{
    public class Nocco
    {
        private static readonly MarkdownSharp.Markdown MarkdownFormatter = new MarkdownSharp.Markdown();
              
        /// <summary>
        /// Generate the documentation for all files in <paramref name="job"/> 
        /// </summary>
        /// <param name="job"></param>
        public static void ProcessJob(NoccoJob job)
        {
            Helpers.LogMessages(
                    "Starting new job: ",
                    "Path: " + job.JobBaseDirectory.ToString(),
                    "Language: " + job.Language.Name
                );

            Helpers.LogMessages("Destination folder: " + job.OuputFolder.ToString());

            //truncate output to the best of our abilities
            if (job.TruncateOutputDirectory)
                Helpers.TryDeleteFolderRecursively(job.OuputFolder.FullName);

            //create docs folder 
            if (!job.OuputFolder.Exists)
                job.OuputFolder.Create();

            //deliver resources by cloning the directory
            string AbsoluteResourceDirectory = App.ResolveDirectory(App.Settings.ResourceDirectory);

            foreach (string file in Directory.GetFiles(AbsoluteResourceDirectory, "*.*", SearchOption.AllDirectories))
            {
                string destFile = file.Replace(AbsoluteResourceDirectory, job.OuputFolder.FullName);
                string destDir = Path.GetDirectoryName(destFile);

                if (!Directory.Exists(destDir))
                    Directory.CreateDirectory(destDir);

                File.Copy(file, destFile, true);
            }

            Helpers.LogMessages("Cloned base resources to destination");


            //summary of the generated documents
            List<DocumentSummary> GeneratedDocuments = new List<DocumentSummary>();

            //get the candidates
            List<FileInfo> JobCandidates = job.GetCandidates().ToList();

            //process each candidate file            
            foreach (FileInfo candidate in JobCandidates)
            {
                GeneratedDocuments.Add(GenerateDocumentHtml(candidate, job.OuputFolder, job, JobCandidates.Except(new[] { candidate })));
            }


            if (job.GenerateIndexFile)
            {
                //create the index file
                GenerateIndexHtml(job.OuputFolder, job, GeneratedDocuments);
            }

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
            var destinationFile = new FileInfo(Path.Combine(docsDirectory.FullName, job.IndexFilename));

            //the relative path from the destination file to the documation folder
            string docsRelative = Helpers.GetPathRelativeTo(destinationFile.Directory, docsDirectory);

            AbIndexTemplate TemplateGenerator = Helpers.GetTemplateGenerator<AbIndexTemplate>(
                new FileInfo(App.ResolveDirectory(App.Settings.IndexTemplateFile)));

            //setup template generator settings
            TemplateGenerator.Title = job.ProjectName ?? "index";
            TemplateGenerator.DocsRelative = docsRelative;

            var documents = generatedDocuments.ToArray();

            TemplateGenerator.GeneratedDocuments = documents;

            //generate documenation file
            TemplateGenerator.Generate(destinationFile.FullName);
        }

        /// <summary>
        /// Get the absolute path for a generated document
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="docsDirectory"></param>
        /// <param name="jobBaseDirectory"></param>
        /// <returns></returns>
        private static FileInfo GetAbsoluteDocDestination(FileInfo sourceFile, DirectoryInfo docsDirectory, DirectoryInfo jobBaseDirectory)
        {
            string subDirectory = sourceFile.DirectoryName.Replace(jobBaseDirectory.FullName, null).TrimStart('\\');

            //if nothing was replaced, then we have no sub directory
            if (subDirectory == sourceFile.DirectoryName)
                subDirectory = null;


            string docFilename = Path.ChangeExtension(sourceFile.Name, "html");


            //the absolute destination path for the documentation file
            return new FileInfo(Path.Combine(
                docsDirectory.FullName,
                subDirectory ?? string.Empty,
                docFilename));
        }


        /// <summary>
        /// Generates the HTML result for a single source file.
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="docsDirectory"></param>
        /// <param name="job"></param>
        /// <returns>Generated document FileInfo</returns>
        private static DocumentSummary GenerateDocumentHtml(FileInfo sourceFile, DirectoryInfo docsDirectory, NoccoJob job, IEnumerable<FileInfo> othersInSameJob)
        {
            DocumentSummary summary = new DocumentSummary() { DocumentFile = sourceFile };

            var destinationFile = GetAbsoluteDocDestination(sourceFile, docsDirectory, job.JobBaseDirectory);            

            //the relative path from the destination file to the documation folder
            string docsRelative = Helpers.GetPathRelativeTo(destinationFile.Directory, docsDirectory);
            
            //get the opposite direction for the index page
            summary.RelativeUri = Helpers.ConvertPathSeparator(Path.Combine(
                    Helpers.GetPathRelativeTo(docsDirectory, destinationFile.Directory), destinationFile.Name));

            AbDocumentTemplate TemplateGenerator = Helpers.GetTemplateGenerator<AbDocumentTemplate>(
                new FileInfo(App.ResolveDirectory(App.Settings.DocumentTemplateFile)));


            //setup template generator settings
            TemplateGenerator.Title = sourceFile.Name;
            summary.Title = TemplateGenerator.Title;

            if (job.GenerateInlineIndex)
            {
                //list of other files in this same job, relative to myself
                TemplateGenerator.OtherDocumentsInJob = othersInSameJob.DefaultIfEmpty()
                            .Select(o => 
                                {
                                    var abs = GetAbsoluteDocDestination(o, docsDirectory, job.JobBaseDirectory);
                                    return Path.Combine(Helpers.GetPathRelativeTo(destinationFile.Directory, abs.Directory), abs.Name);
                                })                               
                            .ToArray();
            }
            
            TemplateGenerator.Sections = ParseSections(sourceFile, job.Language, summary);
            TemplateGenerator.DocsRelative = docsRelative;


            if (job.GenerateIndexFile)
            {
                TemplateGenerator.IndexFile = Helpers.ConvertPathSeparator(Path.Combine(docsRelative, job.IndexFilename));
            }


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

            foreach (var result in Parser.Process(source, language.CommentDefinitions))
            {

                //if this line matches a comment line
                if (result.ResultType == ResultType.Comment)
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

                    docsText.AppendLine(result.MatchingDefinition.CleanComment(result.Result));
                }
                else //hit code or unknown line
                {
                    //update the summary
                    summary.LinesOfCode++;

                    hasCode = true;
                    codeText.AppendLine(result.Result);
                }

            
           }

            yield return GenerateSection(docsText, codeText);
        }
    }
}
