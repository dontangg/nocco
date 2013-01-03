using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Nocco
{
    public partial class Helpers
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
        static extern bool PathRelativePathTo(
             [Out] StringBuilder pszPath,
             [In] string pszFrom,
             [In] FileAttributes dwAttrFrom,
             [In] string pszTo,
             [In] FileAttributes dwAttrTo
        );

        /// <summary>
        /// Returns the relative path from <paramref name="from"/> to <paramref name="to"/>
        /// </summary>
        /// <param name="from">The directory from which a relative path is needed</param>
        /// <param name="to">The target directory to create a relative path to</param>
        /// <returns></returns>
        public static string GetPathRelativePathTo(DirectoryInfo from, DirectoryInfo to)
        {
            StringBuilder sb = new StringBuilder();
            PathRelativePathTo(sb, from.FullName, FileAttributes.Directory, to.FullName, FileAttributes.Directory);
            return sb.ToString();
        }

        public static string ConvertPathSeparator(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            return path.Replace('\\', '/');
        }

    }
}
