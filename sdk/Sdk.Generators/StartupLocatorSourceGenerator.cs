using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    [Generator]
    public class StartupLocatorSourceGenerator : ISourceGenerator
    {
        private string attributeFullTypeName = "Microsoft.Azure.Functions.Worker.Extensions.Abstractions.WorkerExtensionStartupAttribute";
        private string attributeClassName = "WorkerExtensionStartupAttribute";

        public void Execute(GeneratorExecutionContext context)
        {
            //// Find the main method
            var mainMethod = context.Compilation.GetEntryPoint(context.CancellationToken);
            
            var assemblies = context.Compilation.SourceModule.ReferencedAssemblySymbols
                                                .Where(s => s.GetAttributes()
                                                             .Any(a => a.AttributeClass?.Name == attributeClassName &&
                                                                       //Call GetFullName only if class name matches.
                                                                       a.AttributeClass.GetFullName() == attributeFullTypeName
                                                                    ));


            // type and assembly names;
            var typeAndAssemblyDict = new Dictionary<string, string>();


            foreach (var assembly in assemblies)
            {
                var startupAttr = assembly.GetAttributes()
                                          .First(a => a.AttributeClass?.Name == attributeClassName &&
                                                      a.AttributeClass.GetFullName() == attributeFullTypeName);

                TypedConstant type = startupAttr.ConstructorArguments[0];
                if (type.Value is ITypeSymbol typeSymbol1)
                {
                    var fullTypeName = typeSymbol1.ToDisplayString();
                    var assemblyName = assembly.ToDisplayString();
                    typeAndAssemblyDict.Add(fullTypeName, assemblyName);
                }
            }


            SourceText sourceText;
            using (var stringWriter = new StringWriter())
            using (var indentedTextWriter = new IndentedTextWriter(stringWriter))
            {
                indentedTextWriter.WriteLine("// Auto-generated code");
                indentedTextWriter.WriteLine("using System.Collections.Generic;");
                indentedTextWriter.WriteLine($"namespace {mainMethod?.ContainingNamespace?.ToDisplayString() ?? "MyUnitTestNamespace"}");
                indentedTextWriter.WriteLine("{");
                indentedTextWriter.Indent++;
                indentedTextWriter.WriteLine("public class ExtensionStartupDataProvider");
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
        }
    }
}
