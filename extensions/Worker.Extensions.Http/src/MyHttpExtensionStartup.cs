using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
