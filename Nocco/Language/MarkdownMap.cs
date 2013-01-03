
namespace Nocco
{
    /// <summary>
    /// Maps of patterns and replacements to be performed on comments in the documentation
    /// </summary>
    public class MarkdownMap
    {
        /// <summary>
        /// Regex pattern with match groups
        /// </summary>
        public string FindPattern { get; set; }

        /// <summary>
        /// Templated pattern to consume the match groups from an execution of <see cref="FindPattern"/>
        /// </summary>
        public string Replacement { get; set; }
    }


}
