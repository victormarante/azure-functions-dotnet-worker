using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;

[assembly: WorkerExtensionStartup(typeof(MyHttpExtensionStartup), "Http for Worker")]

namespace Microsoft.Azure.Functions.Worker.Extensions.Http
{
    public class MyHttpExtensionStartup : IWorkerExtensionStartup
    {
        public void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
        {
            applicationBuilder.UseMiddleware<StampHttpHeadersMiddleware>();
            applicationBuilder.Services.AddSingleton<IMyFooService, MyFooService>();
        }      
    }
}
