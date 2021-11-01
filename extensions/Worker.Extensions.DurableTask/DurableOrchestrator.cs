// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.DurableTask
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::DurableTask;
    using global::DurableTask.Core;
    using global::DurableTask.Core.History;
    using Newtonsoft.Json;

    public static class DurableOrchestrator
    {
        static readonly JsonSerializerSettings SerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
        };

        public static async Task<string> LoadAndRunAsync(string jsonState, Func<TaskOrchestrationContext, Task<object>> orchestratorFunc)
        {
            if (string.IsNullOrEmpty(jsonState))
            {
                throw new ArgumentNullException(nameof(jsonState));
            }

            if (orchestratorFunc == null)
            {
                throw new ArgumentNullException(nameof(orchestratorFunc));
            }

            // Using Newtonsoft instead of System.Text.Json because Newtonsoft supports a much broader
            // set of features required by DurableTask.Core data types.
            OrchestratorState state = JsonConvert.DeserializeObject<OrchestratorState>(jsonState, SerializerSettings);
            if (state.PastEvents == null || state.NewEvents == null)
            {
                throw new InvalidOperationException("Invalid data was received from the orchestration binding. This indicates a mismatch between the binding extension used by this app and the WebJobs binding extension being used by the Functions host.");
            }

            FunctionsWorkerContext workerContext = new(JsonDataConverter.Default);

            // Re-construct the orchestration state from the history.
            OrchestrationRuntimeState runtimeState = new(state.PastEvents);
            foreach (HistoryEvent newEvent in state.NewEvents)
            {
                runtimeState.AddEvent(newEvent);
            }

            TaskName orchestratorName = new TaskName(runtimeState.Name, runtimeState.Version);

            TaskOrchestrationWrapper<object> orchestrator = new(workerContext, orchestratorName, orchestratorFunc);
            TaskOrchestrationExecutor executor = new(runtimeState, orchestrator, BehaviorOnContinueAsNew.Carryover);
            OrchestratorExecutionResult result = await executor.ExecuteAsync();

            return JsonConvert.SerializeObject(result, SerializerSettings);

        }

        sealed class OrchestratorState
        {
            public string? InstanceId { get; set; }

            public IList<HistoryEvent>? PastEvents { get; set; }

            public IList<HistoryEvent>? NewEvents { get; set; }

            internal int? UpperSchemaVersion { get; set; }
        }

        sealed class FunctionsWorkerContext : IWorkerContext
        {
            public FunctionsWorkerContext(IDataConverter dataConverter)
            {
                this.DataConverter = dataConverter;
            }

            public IDataConverter DataConverter { get; }
        }
    }
}
