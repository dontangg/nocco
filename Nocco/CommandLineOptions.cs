using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;

namespace Nocco
{
    public sealed class CommandLineOptions
    {
        [Option("p", "path", HelpText = "The path from which to build documentation", Required = true)]
        public string Path { get; set; }

        [Option("t", "type", HelpText = "The type of files to process", Required = true)]
        public string FileType { get; set; }

        [Option("o", "output", HelpText = "The folder where the documentation will be created", Required = false)]
        public string OutputFolder { get; set; }

        [Option("i", "index", HelpText = "Filename to use for the index file", Required = false)]
        public string IndexFilename { get; set; }

        [Option("n", "name", DefaultValue = null, HelpText = "Name to be used in the documentation", Required = false)]
        public string ProjectName { get; set; }

        [Option("v", "verbose", DefaultValue = null, HelpText = "If set, job progress information will be displayed", Required = false)]
        public bool Verbose { get; set; }


    }
}
