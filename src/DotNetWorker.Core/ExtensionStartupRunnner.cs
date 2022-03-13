using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Core
{
    // to do: Catch exception on each Configure call of exn startup type instance.
    internal class ExtensionStartupRunnner
    {
        //Microsoft.Azure.Functions.Worker.Extensions.Http.MyHttpExtensionStartup"
        //,"Microsoft.Azure.Functions.Worker.Extensions.Http, Version=4.0.5.0, Culture=neutral, PublicKeyToken=551316b6919f366c");

        internal static void RunExtensionStartupCode(IFunctionsWorkerApplicationBuilder builder)
        {
            var extensionStartupInterfaceType = typeof(IWorkerExtensionStartup);

            var allLoadedAssemblies1 = AppDomain.CurrentDomain.GetAssemblies()
    .ToList();

            var extensionAssemblies1 = allLoadedAssemblies1.Where(a => a.GetName().Name.Contains("Worker.Extensions."))
                .ToList();

            WorkerExtensionAssemblyLoader.LoadAssemblies(builder);
            //WorkerExtensionAssemblyLoader.LoadAssembliesFromDepsJSON();

            var allLoadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .ToList();

            var extensionAssemblies = allLoadedAssemblies.Where(a => a.GetName().Name.Contains("Worker.Extensions."))
                .ToList();

            var allTypes = allLoadedAssemblies.SelectMany(assembly => assembly.GetTypes()).ToList();

            //var extensionStartupTypes = allTypes
            //    .Where(type => !type.IsInterface && extensionStartupInterfaceType.IsAssignableFrom(type))
            //    .ToList();

            //foreach (var extensionStartupType in extensionStartupTypes)
            //{
            //    var instanceAsInterface = Activator.CreateInstance(extensionStartupType) as IWorkerExtensionStartup;

            //    if (instanceAsInterface != null)
            //    {
            //        instanceAsInterface.Configure(builder);
            //    }
            //}
        }
    }


    internal class WorkerExtensionAssemblyLoader
    {
        internal static void LoadAssemblies(IFunctionsWorkerApplicationBuilder builder)
        {
            var cex = Assembly.GetEntryAssembly();
            if (cex != null)
            {
                var definedtypes = cex.DefinedTypes.ToList();
                var allt = cex.GetTypes().ToList();

                Type? startupInfoGeneratedType = cex.GetTypes()
                    .FirstOrDefault(v => v.GetCustomAttributes().Any(at => at.GetType().Name == "StartupInfoAttribute"));

                if (startupInfoGeneratedType != null)
                {
                    var method = startupInfoGeneratedType.GetMethod("GetItems");
                    // throw if multiple methods with same name. (Refer StartupLoader.cs in aspnetcore)
                    var instance = Activator.CreateInstance(startupInfoGeneratedType);
                    var result = method!.Invoke(instance, null);

                    if (result is Dictionary<string, string> values)
                    {
                        foreach (var kvp in values)
                        {
                            var assemblyName = kvp.Value;
                            var startupTypeName = kvp.Key;

                            // Should it be current domain
                            //https://docs.microsoft.com/en-us/dotnet/framework/app-domains/how-to-create-an-application-domain
                            var loadedAssembly = AppDomain.CurrentDomain.Load(assemblyName);
                            if (loadedAssembly == null)
                            {
                                throw new InvalidOperationException($"The assembly '{assemblyName}' failed to load.");
                            }
                            
                            var startupType = loadedAssembly.GetType(startupTypeName)!;

                            if (Activator.CreateInstance(startupType) is IWorkerExtensionStartup startupTypeInstance)
                            {
                                startupTypeInstance.Configure(builder);
                            }
                        }
                    }    
                }

            }

            var allLoadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
               .ToList();
            var allTypes = allLoadedAssemblies.SelectMany(assembly => assembly.GetTypes()).ToList();

            var extensionStartupTypes = allTypes
                .Where(type => !type.IsInterface
                )
                .ToList();
        }
        //internal static void LoadAssembliesFromDepsJSON()
        //{
        //    var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        //    var depsFilePath = Directory.EnumerateFiles(basePath).First(a => a.EndsWith(".deps.json"));

        //    if (depsFilePath != null)
        //    {
        //        var reader = new DependencyContextJsonReader();

        //        using (var file = File.OpenRead(depsFilePath))
        //        {
        //            // DependencyContext requires Microsoft.Extensions.DependencyModel package.
        //            DependencyContext? depsContext = reader.Read(file);

        //            var extensionAssemblies = depsContext.CompileLibraries
        //                 .Where(assembly => assembly.Name.Contains(".Worker.Extensions."))
        //                .ToList();
        //            foreach (var assembly in extensionAssemblies)
        //            {
        //                AppDomain.CurrentDomain.Load(assembly.Name);
        //            }
        //        }
        //    }
        //}
    }



}
