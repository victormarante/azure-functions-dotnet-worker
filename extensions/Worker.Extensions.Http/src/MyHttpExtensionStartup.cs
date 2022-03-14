using Microsoft.Azure.Functions.Worker.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;

[assembly: WorkerExtensionStartup(typeof(MyHttpExtensionStartup))]

namespace Microsoft.Azure.Functions.Worker.Extensions.Http
{
    public sealed class MyHttpExtensionStartup : IWorkerExtensionStartup
    {
        public void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
        {
            applicationBuilder.UseMiddleware<StampHttpHeadersMiddleware>();
            applicationBuilder.Services.AddSingleton<IMyFooService, MyFooService>();
        }      
    }
}
