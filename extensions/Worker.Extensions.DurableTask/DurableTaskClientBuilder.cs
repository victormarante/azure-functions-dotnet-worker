// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.DurableTask
{
    using System;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using global::DurableTask;

    public sealed class DurableTaskClientBuilder
    {
        /// <summary>
        /// Gets or sets the base URL used for making Durable Task management APIs.
        /// </summary>
        /// <remarks>
        /// This property is configured automatically when the <see cref="DurableTaskClientBuilder"/>
        /// is bound to a function parameter.
        /// </remarks>
        public string? RpcBaseUrl { get; set; }

        /// <summary>
        /// Gets or sets an <see cref="IDataConverter"/> to use when serializing and deserializing data payloads.
        /// If not specified, <see cref="JsonDataConverter.Default"/> is used.
        /// </summary>
        public IDataConverter? DataConverter { get; set; }

        public TaskHubClient Build()
        {
            if (string.IsNullOrEmpty(this.RpcBaseUrl))
            {
                throw new InvalidOperationException($"The {nameof(this.RpcBaseUrl)} property is not configured! Was this object created outside of a binding context?");
            }

            return new FunctionsTaskHubClient(
                this.RpcBaseUrl,
                this.DataConverter ?? JsonDataConverter.Default);
        }

        public override string? ToString()
        {
            return this.RpcBaseUrl;
        }

        private sealed class FunctionsTaskHubClient : TaskHubClient
        {
            private static readonly HttpClient HttpClient = new();

            private readonly Uri rpcBaseUri;
            private readonly IDataConverter dataConverter;

            public FunctionsTaskHubClient(string rpcBaseUrl, IDataConverter dataConverter)
            {
                this.rpcBaseUri = new Uri(rpcBaseUrl, UriKind.Absolute);
                this.dataConverter = dataConverter;
            }

            public override ValueTask DisposeAsync() => ValueTask.CompletedTask;

            public override Task<OrchestrationMetadata?> GetInstanceMetadataAsync(string instanceId, bool getInputsAndOutputs = false)
            {
                throw new NotImplementedException();
            }

            public override Task RaiseEventAsync(string instanceId, string eventName, object? eventPayload)
            {
                throw new NotImplementedException();
            }

            public override async Task<string> ScheduleNewOrchestrationInstanceAsync(
                TaskName orchestratorName,
                string? instanceId = null,
                object? input = null,
                DateTimeOffset? startTime = null)
            {
                // TODO: Need to change this to be gRPC
                var pathBuilder = new StringBuilder("orchestrators/").Append(orchestratorName);
                if (!string.IsNullOrEmpty(instanceId))
                {
                    pathBuilder.Append('/').Append(instanceId);
                }

                // Reference: https://docs.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
                Uri uri = new Uri(this.rpcBaseUri, pathBuilder.ToString());
                string? serializedContent = this.dataConverter.Serialize(input);
                HttpContent? content = serializedContent != null ?
                    new StringContent(serializedContent, Encoding.UTF8, "application/json") :
                    null;
                using HttpResponseMessage response = await HttpClient.PostAsync(uri, content);
                string responseData = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    // TODO: Determine what the appropriate exception type is, use it here, and document it.
                    throw new InvalidOperationException($"Failed to schedule the instance. Raw response: {responseData}");
                }

                instanceId = JsonDocument.Parse(responseData).RootElement.GetProperty("id").GetString();
                return instanceId!;
            }

            public override Task TerminateAsync(string instanceId, object? output)
            {
                throw new NotImplementedException();
            }

            public override Task<OrchestrationMetadata> WaitForInstanceCompletionAsync(string instanceId, CancellationToken cancellationToken, bool getInputsAndOutputs = false)
            {
                throw new NotImplementedException();
            }

            public override Task<OrchestrationMetadata> WaitForInstanceStartAsync(string instanceId, CancellationToken cancellationToken, bool getInputsAndOutputs = false)
            {
                throw new NotImplementedException();
            }
        }
    }
}
