using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.Azure.Functions.Worker.Core
{
    internal class ExtensionStartupRunnner
    {
        internal static void RunExtensionStartupCode(IFunctionsWorkerApplicationBuilder builder)
        {
            var extensionStartupInterfaceType = typeof(IWorkerExtensionStartup);

            WorkerExtensionAssemblyLoader.LoadAssembliesFromDepsJSON();

            var allLoadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .ToList();

            var extensionAssemblies = allLoadedAssemblies.Where(a => a.GetName().Name.Contains("Worker.Extensions."))
                .ToList();

            var allTypes = allLoadedAssemblies.SelectMany(assembly => assembly.GetTypes()).ToList();

            var extensionStartupTypes = allTypes
                .Where(type => !type.IsInterface && extensionStartupInterfaceType.IsAssignableFrom(type))
                .ToList();

            foreach (var extensionStartupType in extensionStartupTypes)
            {
                var instanceAsInterface = Activator.CreateInstance(extensionStartupType) as IWorkerExtensionStartup;

                if (instanceAsInterface != null)
                {
                    instanceAsInterface.Configure(builder);
                }
            }
        }
    }

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



}
