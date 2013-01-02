using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace Nocco
{

    public class MarkdownMap
    {
        public string FindPattern { get; set; }
        public string Replacement { get; set; }
    }



    /// <summary>
    /// Represents the configuration for a specific lanaguge
    /// </summary>
    public sealed class LanguageConfig
    {
        /// <summary>
        /// Friendly name of the targeted language
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// File extension of files for this language
        /// </summary>
        public string FileExtension { get; private set; }

        /// <summary>
        /// Regex fragment to determine if a line is a comment
        /// </summary>
        public string Symbol { get; private set; }

        /// <summary>
        /// Collection of replacements of comment lines to markdown-compatible markup
        /// </summary>
        public List<MarkdownMap> MarkdownMaps { get; set; }

        /// <summary>
        /// Filename endings to ignore
        /// </summary>
        public List<string> IgnoreFilenameEndings { get; private set; }

        /// <summary>
        /// Sub directories to ignore
        /// </summary>
        public List<string> IgnoreSubDirectories { get; private set; }

        /// <summary>
        /// Fully-formed regular expression for matching comment lines
        /// </summary>
        public Regex CommentMatcher { get { return new Regex(@"^\s*" + this.Symbol + @"\s?"); } }

        /// <summary>
        /// Fully-formed regular expression for matching 
        /// </summary>
        public Regex CommentFilter { get { return new Regex(@"(^#![/]|^\s*#\{)"); } }
    }

    public partial class Helpers
    {
        ///// <summary>
        ///// A list of the languages that Nocco supports, mapping the file extension to
        ///// the symbol that indicates a comment. To add another language to Nocco's
        ///// repertoire, add it to the languages.json file.
        /////
        ///// You can also specify a list of regular expression patterns and replacements. This
        ///// translates things like
        ///// [XML documentation comments](http://msdn.microsoft.com/en-us/library/b2s063f7.aspx) into Markdown.
        ///// </summary>
        private static Dictionary<string, LanguageConfig> Languages;


        /// <summary>
        /// Static constructor to build language collection from settings file
        /// </summary>
        static Helpers()
        {
            LoadLanguages();
        }

        /// <summary>
        /// Loads the languages from the config file
        /// </summary>
        private static void LoadLanguages()
        {
            //TODO: error handling
            Languages = new Dictionary<string, LanguageConfig>();

            string LanguageFile = Path.Combine(Nocco.ExecutingDirectory, "languages.json");

            foreach (var entry in File.ReadAllText(LanguageFile).FromJson<LanguageConfig[]>())
            {
                Languages.Add(entry.FileExtension.TrimStart(new[] { '.' }), entry);
            }
        }


        /// <summary>
        /// Get the current language we're documenting, based on the extension of <paramref name="file"/>
        /// </summary>
        /// <param name="extension"></param>
        /// <returns>The <see cref="LanguageConfig"/> configuration based on <paramref name="extension"/></returns>
        public static LanguageConfig GetLanguage(string extension)
        {
            if (extension.StartsWith("."))
                extension = extension.Substring(1);


            extension = extension.ToLowerInvariant();

            return Languages.ContainsKey(extension) ? Languages[extension] : null;
        }

    }

}
