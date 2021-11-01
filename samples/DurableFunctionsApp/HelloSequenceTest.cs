// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace DurableFunctionsApp
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using DurableTask;
    using Microsoft.Azure.Functions.DurableTask;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.Functions.Worker.Http;
    using Microsoft.Extensions.Logging;

    public static class HelloSequenceTest
    {
        [Function("Function1")]
        public static async Task<HttpResponseData> HttpStartHelloSequence(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClientBuilder builder,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("Function1");
            logger.LogInformation("C# HTTP trigger function processed a request.");
            logger.LogInformation("DurableTaskClient binding data: {json}", builder);

            TaskHubClient client = builder.Build();
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(HelloSequence));

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.WriteString($"Welcome to Durable Functions! Your instance ID is '{instanceId}'.");

            return response;
        }

        [Function(nameof(HelloSequence))]
        public static Task<string> HelloSequence(
            [OrchestrationTrigger] string orchestratorState)
        {
            return DurableOrchestrator.LoadAndRunAsync(orchestratorState, async context =>
            {
                string result = "";
                result += await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo") + ", ";
                result += await context.CallActivityAsync<string>(nameof(SayHello), "London") + ", ";
                result += await context.CallActivityAsync<string>(nameof(SayHello), "Seattle");
                return result;
            });
        }

        [Function(nameof(SayHello))]
        public static string SayHello([ActivityTrigger] string cityName)
        {
            return $"Hello, {cityName}!";
        }
    }
}
