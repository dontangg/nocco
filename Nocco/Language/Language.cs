using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;


namespace Nocco
{

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
        /// Collection of comment definitions for this language
        /// </summary>
        public List<CommentDefinition> CommentDefinitions { get; private set; }

        /// <summary>
        /// Collection of replacements of comment lines to markdown-compatible markup
        /// </summary>
        public List<MarkdownMap> MarkdownMaps { get; private set; }

        /// <summary>
        /// Filename endings to ignore
        /// </summary>
        public List<string> IgnoreFilenameEndings { get; private set; }

        /// <summary>
        /// Sub directories to ignore
        /// </summary>
        public List<string> IgnoreSubDirectories { get; private set; }

    }

  

}
