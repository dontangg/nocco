using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ServiceStack.Text;

namespace Nocco
{
    public class NoccoSettings
    {
        /// <summary>
        /// Verbosity of application during generation
        /// </summary>
        public bool BeVerbose { get; set; }

        /// <summary>
        /// Default location to output the generated files
        /// </summary>
        public string DefaultDocsFolderName { get; set; }
        
        /// <summary>
        /// Default file name for the generated index file
        /// </summary>
        public string DefaultIndexFileName { get; set; }


        /// <summary>
        /// Location of the language config file
        /// </summary>
        public string LanguageConfigFile { get; set; }
        
        
        /// <summary>
        /// Location of resource files to include with the generated HTML files
        /// </summary>
        public string ResourceDirectory { get; set; }
        
        
        /// <summary>
        /// Location of the Razor template for the documentation file
        /// </summary>
        public string DocumentTemplateFile { get; set; }

        /// <summary>
        /// Location of the Razor template for the index file
        /// </summary>
        public string IndexTemplateFile { get; set; }
    }


    public static class App
    {

        private static readonly NoccoSettings DefaultSettings = new NoccoSettings
        {
            BeVerbose = false,
            DefaultDocsFolderName = "docs",
            DefaultIndexFileName = "index.html",            
            DocumentTemplateFile = @".\Templates\document.cshtml",
            IndexTemplateFile = @".\Templates\index.cshtml",            
            LanguageConfigFile = @".\languages.json",
            ResourceDirectory = @".\Resources"
        };

        public static readonly string WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static readonly string ExecutableName = Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);

        /// <summary>
        /// Resolves a relative path to the current working directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ResolveDirectory(string path)
        {
            return Path.GetFullPath(Path.Combine(WorkingDirectory, path));
        }

        private static NoccoSettings _Settings;
        public static NoccoSettings Settings { get { return _Settings; } }

        static App()
        {
            string settingsFilename = Path.Combine(WorkingDirectory, ExecutableName + ".json");

            if (!File.Exists(settingsFilename))
            {
                _Settings = DefaultSettings;
            }
            else
            {
                try
                {
                    _Settings = File.ReadAllText(settingsFilename).FromJson<NoccoSettings>();
                }
                catch
                {
                    Helpers.LogMessages("Failed to load existing settings. Using default.");
                    _Settings = DefaultSettings;
                }

            }

            //ensure settings are not null
            _Settings.DefaultDocsFolderName = _Settings.DefaultDocsFolderName ?? DefaultSettings.DefaultDocsFolderName;
            _Settings.DocumentTemplateFile = _Settings.DocumentTemplateFile ?? DefaultSettings.DocumentTemplateFile;
            _Settings.DefaultIndexFileName = _Settings.DefaultIndexFileName ?? DefaultSettings.DefaultIndexFileName;
            _Settings.IndexTemplateFile = _Settings.IndexTemplateFile ?? DefaultSettings.IndexTemplateFile;
            _Settings.LanguageConfigFile = _Settings.LanguageConfigFile ?? DefaultSettings.LanguageConfigFile;
            _Settings.ResourceDirectory = _Settings.ResourceDirectory ?? DefaultSettings.ResourceDirectory;

            //update saved settings
            File.WriteAllText(settingsFilename, _Settings.ToJson());




        }
    }
}
