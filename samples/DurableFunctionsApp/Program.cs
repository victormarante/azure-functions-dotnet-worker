namespace DurableFunctionsApp
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;

    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .Build();
            await host.RunAsync();
        }
    }
}
