using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ServiceStack.Text;

namespace Nocco
{
    public partial class Helpers
    {
        /// <summary>
        /// A list of the languages that Nocco supports, mapping the file extension to
        /// the symbol that indicates a comment. To add another language to Nocco's
        /// repertoire, add it to the languages.json file.
        ///
        /// You can also specify a list of regular expression patterns and replacements. This
        /// translates things like
        /// [XML documentation comments](http://msdn.microsoft.com/en-us/library/b2s063f7.aspx) into Markdown.
        /// </summary>
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
            //TODO: No point in loading them all into memory if we only ever plan on using one per lifecycle

            //TODO: error handling
            Languages = new Dictionary<string, LanguageConfig>();

            string LanguageFile = App.ResolveDirectory(App.Settings.LanguageConfigFile);

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
