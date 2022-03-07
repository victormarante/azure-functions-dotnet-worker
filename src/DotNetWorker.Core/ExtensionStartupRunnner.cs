using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Core
{
    internal class WorkerExtensionAssemblyLoader
    {
        internal static void LoadAssembliesFromDepsJSON()
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var depsFilePath = Directory.EnumerateFiles(basePath).First(a => a.EndsWith(".deps.json"));

            if (depsFilePath != null)
            {
                var reader = new DependencyContextJsonReader();

                using (var file = File.OpenRead(depsFilePath))
                {
                    // DependencyContext requires Microsoft.Extensions.DependencyModel package.
                    DependencyContext? depsContext = reader.Read(file);

                    var extensionAssemblies = depsContext.CompileLibraries
                         .Where(assembly => assembly.Name.Contains(".Worker.Extensions."))
                        .ToList();
                    foreach (var assembly in extensionAssemblies)
                    {
                        AppDomain.CurrentDomain.Load(assembly.Name);
                    }
                }
            }
        }
    }


    internal class ExtensionStartupRunnner
    {
        internal static void RunExtensionStartupCode(IFunctionsWorkerApplicationBuilder builder)
        {
            var extensionStartupInterfaceType = typeof(IWorkerExtensionStartup);

            WorkerExtensionAssemblyLoader.LoadAssembliesFromDepsJSON();

            var assemblyList = AppDomain.CurrentDomain.GetAssemblies().ToList();

            var asseblyList2 = assemblyList.Where(a => a.GetName().Name.Contains("Worker.Extensions.")).ToList();
            var allTypes = assemblyList.SelectMany(assembly => assembly.GetTypes()).ToList();
            var types = allTypes
                .Where(type => !type.IsInterface && extensionStartupInterfaceType.IsAssignableFrom(type))
                .ToList();

            foreach (var type in types)
            {
                var instanceAsInterface = Activator.CreateInstance(type) as IWorkerExtensionStartup;

                if (instanceAsInterface != null)
                {
                    instanceAsInterface.Configure(builder);
                }
            }
        }
    }
}
