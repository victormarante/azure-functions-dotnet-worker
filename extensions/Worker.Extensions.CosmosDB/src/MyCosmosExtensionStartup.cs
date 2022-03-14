using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.CosmosDB;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: WorkerExtensionStartup(typeof(MyCosmosExtensionStartup))]

namespace Microsoft.Azure.Functions.Worker.Extensions.CosmosDB
{
    public class MyCosmosExtensionStartup : IWorkerExtensionStartup
    {
        public void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
        {
            applicationBuilder.UseMiddleware<MyCosmosProcessingMiddleware>();
            applicationBuilder.Services.AddSingleton<IMyCosmosFooService, MyCosmosFooService>();
        }
    }

    public sealed class MyCosmosProcessingMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly IMyCosmosFooService _myFooService;

        public MyCosmosProcessingMiddleware(IMyCosmosFooService myFooService)
        { 
            _myFooService = myFooService ?? throw new ArgumentNullException(nameof(myFooService));
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var logger = context.GetLogger<MyCosmosProcessingMiddleware>();
            logger.LogInformation($"****** With SourceGen-MyCosmosProcessingMiddleware {_myFooService.GetFooMessage()} *******");

            await next(context);
        }
    }
    public interface IMyCosmosFooService
    {
        string GetFooMessage();
    }

    public class MyCosmosFooService : IMyCosmosFooService
    {
        ILogger<MyCosmosFooService> _logger;

        public MyCosmosFooService(ILogger<MyCosmosFooService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string GetFooMessage() => "Hello from MyCosmosFooService";
    }
}
