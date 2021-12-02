// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DurableFunctionsApp;

using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

static class Program
{
    public static async Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .Build();
        await host.RunAsync();
    }
}
