using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
namespace Sdk.Generators
{
    [Generator]
    public class StartupLocatorSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            //var typeSymbol = "WorkerExtensionStartupAttribute";

            //// Find the main method
            var mainMethod = context.Compilation.GetEntryPoint(context.CancellationToken);

            //var assemblies = context.Compilation.SourceModule.ReferencedAssemblySymbols
            //    .Where(s => s.GetAttributes().Any(c => c.AttributeClass.Name == typeSymbol))
            //    .ToList();


            // type and assembly names;
            var typeAndAssemblyDict = new Dictionary<string, string>();
            //foreach (var a in assemblies)
            //{
            //    var startupAttr = a.GetAttributes().First(a => a.AttributeClass.Name == typeSymbol);

            //    TypedConstant type = startupAttr.ConstructorArguments[0];
            //    if (type.Value is ITypeSymbol typeSymbol1)
            //    {
            //        // Full type name (with namespace)
            //        var fullTypeName = typeSymbol1.ToDisplayString();
            //        var assemblyName = a.ToDisplayString();
            //        typeAndAssemblyDict.Add(fullTypeName, assemblyName);
            //    }
            //}

            typeAndAssemblyDict.Add("Foo2", "Bar2");

            SourceText sourceText;
            using (var stringWriter = new StringWriter())
            using (var indentedTextWriter = new IndentedTextWriter(stringWriter))
            {
                indentedTextWriter.WriteLine("// Auto-generated code");
                indentedTextWriter.WriteLine("using System.Collections.Generic;");
                indentedTextWriter.WriteLine($"namespace {mainMethod?.ContainingNamespace?.ToDisplayString() ?? "MyTestNamespace"}");
                indentedTextWriter.WriteLine("{");
                indentedTextWriter.Indent++;
                indentedTextWriter.WriteLine("public class MyExtensionStartupInfoProvider");
                indentedTextWriter.WriteLine("{");
                indentedTextWriter.Indent++;
                indentedTextWriter.WriteLine("public IEnumerable<KeyValuePair<string, string>> GetItems()");
                indentedTextWriter.WriteLine("{");
                indentedTextWriter.Indent++;
                indentedTextWriter.WriteLine($"var dict = new Dictionary<string, string>({typeAndAssemblyDict.Count});");

                foreach (var kp in typeAndAssemblyDict)
                {
                    indentedTextWriter.WriteLine($"dict.Add(\"{kp.Key}\",\"{kp.Value}\");");
                }
                indentedTextWriter.WriteLine("return dict;");
                indentedTextWriter.Indent--;
                indentedTextWriter.WriteLine("}");
                indentedTextWriter.Indent--;
                indentedTextWriter.WriteLine("}");
                indentedTextWriter.Indent--;
                indentedTextWriter.WriteLine("}");

                indentedTextWriter.Flush();
                sourceText = SourceText.From(stringWriter.ToString(), encoding: Encoding.UTF8);
            }

            // Add the source code to the compilation
            context.AddSource($"ExtensionStartupDataProvider.g.cs", sourceText);
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // No initialization required for this one
        }
    }
}
