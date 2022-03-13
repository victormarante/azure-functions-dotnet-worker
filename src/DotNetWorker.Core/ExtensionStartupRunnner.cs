// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Core
{
    // to do: Catch exception on each Configure call of exn startup type instance.
    internal class ExtensionStartupRunnner
    {
        private const string StartupDataAttributeName = "ExtensionStartupData";

        internal static void RunExtensionStartupCode(IFunctionsWorkerApplicationBuilder builder)
        {
            // Find the auto(source) generated class which has extension startup type name and assembly names.
            Type? startupDataProviderGeneratedType = (Assembly.GetEntryAssembly())!.GetTypes()
                .FirstOrDefault(v => v.GetCustomAttributes().Any(at => at.GetType().Name == StartupDataAttributeName));

            if (startupDataProviderGeneratedType == null)
            {
                return;
            }

            var method = startupDataProviderGeneratedType.GetMethod("GetStartupTypes");
            if (method == null)
            {
                throw new InvalidOperationException($"Types decorated with {StartupDataAttributeName} must have a GetStartupTypes method.");
            }

            var extensionStartupType = Activator.CreateInstance(startupDataProviderGeneratedType);
            var methodInvocationResult = method!.Invoke(extensionStartupType, parameters: null);

            if (!(methodInvocationResult is IDictionary<string, string> typesDict))
            {
                return;
            }

            foreach (var typeAndAssemblyPair in typesDict)
            {
                var startupTypeName = typeAndAssemblyPair.Key;
                var assemblyName = typeAndAssemblyPair.Value;

                // Should it be current domain?
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
