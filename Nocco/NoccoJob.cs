using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nocco
{

    
    /// <summary>
    /// Represents a grouped task of files to process in one logical area
    /// </summary>
    public sealed class NoccoJob 
    {
        public DirectoryInfo JobBaseDirectory { get; private set; }
        public LanguageConfig Language { get; private set; }
        public string TargetExtension { get; private set; }
        public DirectoryInfo OuputFolder { get; private set; }

        public string ProjectName { get; set; }
        public string IndexFilename { get; set; }
        public bool GenerateIndexFile { get; set; }
        public bool GenerateInlineIndex { get; set; }
        public bool TruncateOutputDirectory { get; set; }
        
        /// <summary>
        /// Creates a new job
        /// </summary>
        /// <param name="jobBaseDirectory"></param>
        /// <param name="language"></param>
        /// <param name="targetExtension"></param>
        /// <param name="outputFolder"></param>
        public NoccoJob(DirectoryInfo jobBaseDirectory, LanguageConfig language, string targetExtension, DirectoryInfo outputFolder)
        {

            if (jobBaseDirectory == null || !jobBaseDirectory.Exists)
                throw new ArgumentException("JobBaseDirectory is not a valid directory");

            if (language == null)
                throw new ArgumentException("Language is not valid");

            if (string.IsNullOrWhiteSpace(targetExtension))
                throw new ArgumentException("TargetExtension is not valid");


            this.JobBaseDirectory = jobBaseDirectory;
            this.Language = language;
            this.TargetExtension = targetExtension;
            this.OuputFolder = outputFolder;


        }


        /// <summary>
        /// Gets the relative path from <paramref name="file"/> to this job's <see cref="NoccoJob.JobBaseDirectory"/>
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public string GetJobRelativePath(FileInfo file)
        {
            return Helpers.GetPathRelativeTo(file.Directory, this.JobBaseDirectory);
        }


        /// <summary>
        /// Gets all candidate files for processing under the requirements of this job
        /// </summary>
        /// <returns></returns>
        public IEnumerable<FileInfo> GetCandidates()
        {


            foreach (FileInfo file in this.JobBaseDirectory.GetFiles("*" + this.TargetExtension, SearchOption.AllDirectories))
            {
                bool CanReturnFile = true;

                //check for ignored directories
                if (this.Language.IgnoreSubDirectories != null)
                {
                    foreach (string ignore in this.Language.IgnoreSubDirectories)
                    {
                        //check first directory level relative to the job
                        string AbsoIgnore = Path.Combine(this.JobBaseDirectory.FullName, ignore);

                        if (file.DirectoryName.StartsWith(AbsoIgnore, System.StringComparison.OrdinalIgnoreCase))
                        {
                            CanReturnFile = false;
                            break;
                        }
                    }
                }

                //check for ignored file endings
                if (this.Language.IgnoreFilenameEndings != null)
                {

                    foreach (string ignore in this.Language.IgnoreFilenameEndings)
                    {
                        if (file.Name.EndsWith(ignore, System.StringComparison.OrdinalIgnoreCase))
                        {
                            CanReturnFile = false;
                            break;
                        }
                    }
                }

                if (CanReturnFile)
                {
                    //return the candidate
                    yield return file;
                }
            }

        }

    }
}
