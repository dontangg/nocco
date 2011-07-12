using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Web.Razor;
using System.Text;

namespace Nocco
{
	public class EmbeddedResources
	{
		Type templateType = null;
		string[] clientFiles = { "prettify.js", "Nocco.css" };

		public void WriteClientFilesTo(string path)
		{
			Directory.CreateDirectory(path);

			foreach (string res in clientFiles)
			{
				string outputFile = Path.Combine(path, res);

				// Don't overwrite existing resource files
				if (File.Exists(outputFile))
					continue;

				try
				{
					using (var writer = File.CreateText(outputFile))
					{
						Stream s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Nocco.Resources." + res);
						if (s == null)
							throw new InvalidDataException("Could not find embedded resource 'Nocco.Resources." + res + "'");

						using (var reader = new StreamReader(s))
						{
							writer.Write(reader.ReadToEnd());
						}
					}
				}
				catch
				{
					try { File.Delete(outputFile); } catch { }
				}
			}
		}

		//### Template Setup

		// Setup the Razor templating engine so that we can quickly pass the data in
		// and generate HTML.
		//
		// The file `Resources\Nocco.cshtml` is read and compiled into a new dll
		// with a type that extends the `TemplateBase` class. This new assembly is
		// loaded so that we can create an instance and pass data into it
		// and generate the HTML.
		public TemplateBase CreateRazorTemplateInstance()
		{
			if (templateType == null)
			{
				var host = new RazorEngineHost(new CSharpRazorCodeLanguage());
				host.DefaultBaseClass = typeof(TemplateBase).FullName;
				host.DefaultNamespace = "RazorOutput";
				host.DefaultClassName = "Template";
				host.NamespaceImports.Add("System");

				GeneratorResults razorResult = null;

				var templateStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Nocco.Resources.Nocco.cshtml");

				if (templateStream == null)
					throw new FileNotFoundException("Could not find embedded resource 'Nocco.Resources.Nocco.cshtml'");

				using (var reader = new StreamReader(templateStream))
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
				compilerParams.ReferencedAssemblies.Add(typeof(Program).Assembly.CodeBase.Replace("file:///", "").Replace('/', Path.DirectorySeparatorChar));

				var codeProvider = new Microsoft.CSharp.CSharpCodeProvider();
				var results = codeProvider.CompileAssemblyFromDom(compilerParams, razorResult.GeneratedCode);

				// Check for errors that may have occurred during template generation
				if (results.Errors.HasErrors)
				{
					StringBuilder errors = new StringBuilder();
					foreach (var err in results.Errors.OfType<CompilerError>().Where(ce => !ce.IsWarning))
						errors.AppendFormat("Error compiling template: ({0}, {1}) {2}", err.Line, err.Column, err.ErrorText);

					throw new InvalidDataException(errors.ToString());
				}

				templateType = results.CompiledAssembly.GetType("RazorOutput.Template");
			}

			return (TemplateBase)Activator.CreateInstance(templateType);
		}
	}
}
