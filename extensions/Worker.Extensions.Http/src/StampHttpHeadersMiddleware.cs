using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http
{
    public sealed class StampHttpHeadersMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly IMyFooService _myFooService;

        public StampHttpHeadersMiddleware(IMyFooService myFooService)
        {
            _myFooService = myFooService ?? throw new ArgumentNullException(nameof(myFooService));
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var logger = context.GetLogger<StampHttpHeadersMiddleware>();
            logger.LogInformation($"****** With SourceGen-StampHttpHeadersMiddleware {_myFooService.GetFooMessage()} *******");

            await next(context);
        }
    }
}
