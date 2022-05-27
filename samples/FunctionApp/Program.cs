// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FunctionApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // string crashFile = "crashworker.txt";
            // if (File.Exists(crashFile))
            // {
            //     File.Delete(crashFile);
            //     Console.WriteLine("Crashing worker");
            //     System.Environment.FailFast("Lilian error happened");
            // }
            // else
            // {
            //     await File.WriteAllTextAsync(crashFile, "die");
            // }

            // #if DEBUG
            //          Debugger.Launch();
            // #endif
            //<docsnippet_startup>
            var host = new HostBuilder()
                //<docsnippet_configure_defaults>
                .ConfigureFunctionsWorkerDefaults()
                //</docsnippet_configure_defaults>
                //<docsnippet_dependency_injection>
                .ConfigureServices(s =>
                {
                    s.AddSingleton<IHttpResponderService, DefaultHttpResponderService>();
                })
                //</docsnippet_dependency_injection>
                .Build();
            //</docsnippet_startup>

            //<docsnippet_host_run>

            await host.RunAsync();

            // async (msg) =>
            // {

            //     await host.RunAsync();
            // // awaitProcessMessageAsync(msg, stoppingToken);

            // });

            // CancellationTokenSource source = new CancellationTokenSource();
            // var t = Task.Run(async delegate {
            //     await Task.Delay(1000, source.Token); return 42;
            // });
            // source.Cancel();

            // Console.WriteLine("Timeout worker");
            // System.Threading.Thread.Sleep(150000);
            // return 200;
            //</docsnippet_host_run>
        }
    }
}
