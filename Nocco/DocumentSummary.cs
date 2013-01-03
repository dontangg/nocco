using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Nocco
{
    public sealed class DocumentSummary
    {
        public FileInfo DocumentFile { get; set; }
        public string Title { get; set; }
        public string RelativeUri { get; set; }
        public Section TopSection { get; set; }
        public int LinesOfCode { get; set; }
        public int LinesOfComment { get; set; }

        public string RelativeDirectory
        {
            get { return Path.GetDirectoryName(this.RelativeUri.Replace("./", string.Empty)); }
        }

        public string GetSectionCodeForDisplay()
        {
            return GetSectionForDisplay(this.TopSection.CodeHtml);
        }

        public string GetSectionDocForDisplay()
        {
            return GetSectionForDisplay(this.TopSection.DocsHtml);
        }


        private static string GetSectionForDisplay(string input)
        {
            string ret = Regex.Replace(input, @"<(.|\n)*?>", string.Empty);
            return ret.Substring(0, Math.Min(200, ret.Length));
        }

    }
}
