// The entrance point for the program.  Just run Nocco!


using System;
using System.Text.RegularExpressions;
using CommandLine;
using ServiceStack.Text;

namespace Nocco
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new CommandLineOptions();
            var parser = new CommandLineParser(new CommandLineParserSettings { CaseSensitive = false, IgnoreUnknownArguments = true });
            var res = parser.ParseArguments(args, options);

            if (!res)
            {
                Console.WriteLine("check your parameters and try again");
                return;
            }
            
            //check to see if we have a language directive for the specified type
            LanguageConfig config = Helpers.GetLanguage(options.FileType);

            if (config == null)
            {
                Console.WriteLine("We don't know how to handle the requested type. You can add a definition to the language.json file and try again.");
                return;
            }

            Nocco.BeVerbose = options.Verbose;            

            Nocco.Generate(options.Path, options.FileType, options.ProjectName);
        }
    }
}
