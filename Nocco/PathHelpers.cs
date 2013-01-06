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
        public static string GetPathRelativeTo(DirectoryInfo from, DirectoryInfo to)
        {
            StringBuilder sb = new StringBuilder();
            PathRelativePathTo(sb, from.FullName, FileAttributes.Directory, to.FullName, FileAttributes.Directory);

            return sb.ToString();
        }

        /// <summary>
        /// Converts backslashes to forward slashes
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ConvertPathSeparator(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            return path.Replace('\\', '/');
        }

        /// <summary>
        /// Attempts to delete each file and subdirectory in <paramref name="path"/>. Not guaranteed to succeed, but will not throw an exception
        /// </summary>
        /// <param name="path"></param>
        public static void TryDeleteFolderRecursively(string path)
        {
            if (!Directory.Exists(path))
                return;

            
            foreach (string file in Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    File.Delete(file);
                }
                catch { /* carry on */ }
            }


            foreach (string directory in Directory.GetDirectories(path, "*.*", SearchOption.TopDirectoryOnly))
            {
                TryDeleteFolderRecursively(directory);
            }


            try
            {
                Directory.Delete(path);
            }
            catch  { /* carry on */}

        }


    }
}
