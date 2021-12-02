// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DurableTask;
using DurableTask.Core;
using DurableTask.Core.History;
using Newtonsoft.Json;

namespace Microsoft.Azure.Functions.Worker;

// TODO: Documentation
public static class DurableOrchestrator
{
    // Using Newtonsoft instead of System.Text.Json because Newtonsoft supports a much broader
    // set of features required by DurableTask.Core data types.
    static readonly JsonSerializerSettings SerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
    };

    // TODO: Documentation
    public static Task<string> LoadAndRunAsync<TInput, TOutput>(string triggerStateJson, ITaskOrchestrator implementation)
    {
        return LoadAndRunAsync(triggerStateJson, implementation.RunAsync);
    }

    // TODO: Documentation
    public static async Task<string> LoadAndRunAsync(string triggerStateJson, Func<TaskOrchestrationContext, Task<object?>> orchestratorFunc)
    {
        if (string.IsNullOrEmpty(triggerStateJson))
        {
            throw new ArgumentNullException(nameof(triggerStateJson));
        }

        if (orchestratorFunc == null)
        {
            throw new ArgumentNullException(nameof(orchestratorFunc));
        }

        OrchestratorState state = JsonConvert.DeserializeObject<OrchestratorState>(triggerStateJson, SerializerSettings);
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
