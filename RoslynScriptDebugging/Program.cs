using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;


using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Emit;

namespace RoslynScriptDebugging
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length == 0)
                return;

            var sb = new StringBuilder();

            foreach (var path in args)
            {
                if (!File.Exists(path))
                    return;

                var lines = File.ReadAllLines(path);

                sb.AppendLine($"#line 1 \"{path}\"");
                foreach (var l in lines)
                {
                    if (l.StartsWith("write"))
                    {
                        var res = l.Substring(l.IndexOf(" ", StringComparison.Ordinal)).Trim();
                        sb.AppendLine($"System.Console.WriteLine(\"{res}\");");
                    }
                    else
                        sb.AppendLine(l);
                }
            }

            Execute(sb.ToString());

            Console.ReadLine();
        }

        /// <summary>
        /// Based on Cake.Scripting.XPlat.DebugXPlatScriptSession 
        /// </summary>
        /// <param name="script"></param>
        private static void Execute(string script)
        {
            var options = Microsoft.CodeAnalysis.Scripting.ScriptOptions.Default;
            var roslynScript = CSharpScript.Create(script, options);
            var compilation = roslynScript.GetCompilation();

            compilation = compilation.WithOptions(compilation.Options
                .WithOptimizationLevel(OptimizationLevel.Debug)
                .WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

            using (var assemblyStream = new MemoryStream())
            {
                using (var symbolStream = new MemoryStream())
                {
                    var emitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb);
                    var result = compilation.Emit(assemblyStream, symbolStream, options: emitOptions);
                    if (!result.Success)
                    {
                        var errors = string.Join(Environment.NewLine, result.Diagnostics.Select(x => x));
                        Console.WriteLine(errors);
                        return;
                    }

                    var assembly = Assembly.Load(assemblyStream.ToArray(), symbolStream.ToArray());
                    var type = assembly.GetType("Submission#0");
                    var method = type.GetMethod("<Factory>", BindingFlags.Static | BindingFlags.Public);

                    method.Invoke(null, new object[] {new object[2]});
                }
            }
        }
    }
}

