using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Razor;

namespace Nocco
{

    public static partial class Helpers
    {

        /// <summary>
        /// Holds the once-per-lifecycle instances
        /// </summary>
        private static Dictionary<string, AbTemplateGenerator> CompiledGenerators = new Dictionary<string, AbTemplateGenerator>();

        /// <summary>
        /// Thread locker to prevent more than one process from creating a template generator
        /// </summary>
        private readonly static object CompiledGeneratorLocker = new object();


        /// <summary>
        /// Setup the Razor templating engine so that we can quickly pass the data in
        /// and generate HTML.
        ///
        /// The file `Resources\Nocco.cshtml` is read and compiled into a new dll
        /// with a type that extends the `TemplateBase` class. This new assembly is
        /// loaded so that we can create an instance and pass data into it
        /// and generate the HTML.
        /// </summary>
        /// <returns></returns>
        public static T GetTemplateGenerator<T>(FileInfo template) where T : AbTemplateGenerator
        {
            var assembliesToInclude = new[] { 
                typeof(Nocco).Assembly, 
                typeof(System.IO.Path).Assembly 
            };

            string key = template.FullName.ToLowerInvariant();

            AbTemplateGenerator Generator = null;

            bool res = CompiledGenerators.TryGetValue(key, out Generator);

            if (res)
                return Generator as T;

            Monitor.Enter(CompiledGeneratorLocker);

            //double-check in lock
            res = CompiledGenerators.TryGetValue(key, out Generator);

            if (res)
                return Generator as T;

            var host = new RazorEngineHost(new CSharpRazorCodeLanguage());
            host.DefaultBaseClass = typeof(T).FullName;
            host.DefaultNamespace = "RazorOutput";
            host.DefaultClassName = "Template";
            host.NamespaceImports.Add("System");

            GeneratorResults razorResult = null;
            using (var reader = new StreamReader(template.FullName))
            {
                razorResult = new RazorTemplateEngine(host).GenerateCode(reader);
            }

            var compilerParams = new CompilerParameters
            {
                GenerateInMemory = true,
                GenerateExecutable = false,
                IncludeDebugInformation = false,
                CompilerOptions = "/target:library /optimize"
            };

            foreach (var ass in assembliesToInclude)
            {
                compilerParams.ReferencedAssemblies.Add(ass.CodeBase.Replace("file:///", "").Replace("/", "\\"));
            }                

            var codeProvider = new Microsoft.CSharp.CSharpCodeProvider();
            var results = codeProvider.CompileAssemblyFromDom(compilerParams, razorResult.GeneratedCode);

            // Check for errors that may have occurred during template generation
            if (results.Errors.HasErrors)
            {
                foreach (var err in results.Errors.OfType<CompilerError>().Where(ce => !ce.IsWarning))
                    Console.WriteLine("Error Compiling Template: ({0}, {1}) {2}", err.Line, err.Column, err.ErrorText);
            }

            Type compiledType = results.CompiledAssembly.GetType(string.Format("{0}.{1}", host.DefaultNamespace, host.DefaultClassName));


            Generator = Activator.CreateInstance(compiledType) as T;

            CompiledGenerators.Add(key, Generator);

            Monitor.Exit(CompiledGeneratorLocker);


            return Generator as T;

        }

    }
}