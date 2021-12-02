// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using DurableTask;
using DurableTask.Grpc;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.Functions.Worker;

/// <summary>
/// Concreate implementation of the <see cref="DurableClientContext"/> abstract class that
/// allows callers to start and manage orchestration instances.
/// </summary>
sealed class DefaultDurableClientContext : DurableClientContext
{
    internal DefaultDurableClientContext(TaskHubClient client, string taskHubName)
    {
        this.Client = client ?? throw new ArgumentNullException(nameof(client));
        this.TaskHubName = taskHubName ?? throw new ArgumentNullException(nameof(taskHubName));
    }

    /// <inheritdoc/>
    public override TaskHubClient Client { get; }

    /// <inheritdoc/>
    public override string TaskHubName { get; }

    /// <inheritdoc/>
    public override HttpResponseData CreateCheckStatusResponse(HttpRequestData request, string instanceId, bool returnInternalServerErrorOnFailure = false)
    {
        // TODO: Payload should include the management URLs.
        HttpResponseData response = request.CreateResponse(HttpStatusCode.Created);
        response.WriteString(instanceId);
        return response;
    }

    /// <summary>
    /// Input converter implementation for the Durable Client binding (i.e. functions with a <see cref="DurableClientAttribute"/>-decorated parameter)
    /// that translates an input JSON blob into an <see cref="DurableClientContext"/> object.
    /// </summary>
    internal class Converter : IInputConverter
    {
        readonly IServiceProvider? serviceProvider;

        // Constructor parameters are optional DI-injected services.
        public Converter(IServiceProvider? serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public ValueTask<ConversionResult> ConvertAsync(ConverterContext converterContext)
        {
            // The exact format of the expected JSON string data is controlled by the Durable Task WebJobs client binding logic.
            // It's never expected to be wrong, but we code defensively just in case.
            if (converterContext.Source is not string clientConfigText)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Failed(new InvalidOperationException($"Expected the Durable Task WebJobs SDK extension to send a string payload for {nameof(DurableClientAttribute)}.")));
            }

            DurableClientInputData? inputData = JsonSerializer.Deserialize<DurableClientInputData>(clientConfigText);
            if (string.IsNullOrEmpty(inputData?.rpcBaseUrl))
            {
                InvalidOperationException exception = new("Failed to parse the input binding payload data");
                return new ValueTask<ConversionResult>(ConversionResult.Failed(exception));
            }

            try
            {
                TaskHubGrpcClient.Builder builder = TaskHubGrpcClient.CreateBuilder();
                builder.UseAddress(inputData.rpcBaseUrl);
                if (this.serviceProvider != null)
                {
                    // The builder will use the host's service provider to look up DI-registered
                    // services, like IDataConverter, ILoggerFactory, IConfiguration, etc. This allows
                    // it to use the default host services and also allows it to use services injected
                    // by the application owner (e.g. IDataConverter for custom data conversion).
                    builder.UseServices(this.serviceProvider);
                }

                DefaultDurableClientContext clientContext = new(builder.Build(), inputData.taskHubName);
                return new ValueTask<ConversionResult>(ConversionResult.Success(clientContext));
            }
            catch (Exception innerException)
            {
                InvalidOperationException exception = new($"Failed to convert the input binding context data into a {nameof(DefaultDurableClientContext)} object. The data may have been delivered in an invalid format.", innerException);
                return new ValueTask<ConversionResult>(ConversionResult.Failed(exception));
            }
        }
    }

    // Serializer is case-sensitive and incoming JSON properties are camel-cased.
    record DurableClientInputData(string rpcBaseUrl, string taskHubName);
}
