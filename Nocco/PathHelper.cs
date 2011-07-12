using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Nocco
{
    // PathHelper is initialized with a root directory and can make other paths relative to it.
    public class PathHelper
    {
        public string RootPath { get; set; }

        public string MakeRelativePath(string toPath)
        {
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(RootPath);
            Uri toUri = new Uri(toPath);

            return fromUri.MakeRelativeUri(toUri).ToString().Replace('/', Path.DirectorySeparatorChar);
        }

        public PathHelper(string rootPath)
        {
            if (String.IsNullOrEmpty(rootPath)) throw new ArgumentNullException("absoluteTargetPath");

            RootPath = rootPath;
        }
    }
}

