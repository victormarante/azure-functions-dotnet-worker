# Durable Functions in .NET Isolated

Durable Functions is not yet officially supported in the .NET Isolated worker. However, it's being actively worked on and is available as an _alpha-quality_ worker extension. See below for getting started instructions using the current sample project.

## Getting started

It's recommended that you start by copying the sample in this directory and making the necessary changes for it to run independently.

Many of the NuGet packages required for Durable Functions in .NET Isolated are not yet available on nuget.org. Instead, early alpha packages can be found on myget.org. The recommended way to get access to these packages is to add the following to your nuget.config file.

```xml
<add key="azure_app_service" value="https://www.myget.org/F/azure-appservice/api/v2" />
```

The following NuGet packages can be found on this feed:

* microsoft.azure.durabletask.core.2.7.0-ooproc.1.nupkg
* microsoft.azure.functions.worker.extensions.durabletask.0.1.0-alpha.nupkg
* microsoft.azure.webjobs.extensions.durabletask.2.7.0-dotnetisolated.1.nupkg
* microsoft.durabletask.0.1.0-alpha.nupkg
* microsoft.durabletask.generators.0.1.0.nupkg
* microsoft.durabletask.sidecar.0.1.0-alpha.nupkg
* microsoft.durabletask.protobuf.0.1.0-alpha.nupkg

To get all the packages you need, add the following to your .NET Isolated csproj file:

```xml
<PackageReference Include="Microsoft.Azure.Functions.Worker" Version="1.7.0-preview1" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Abstractions" Version="1.1.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http" Version="3.0.13" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.DurableTask" Version="0.1.0-alpha" />
```

If you're copying the sample project, be sure to remove all the existing `<ProjectReference />` elements from the copied csproj file. These are unnecessary once you've added the above `<PackageReference />` elements.

If you've followed all the above steps correctly, you should be able to start debugging locally. If your IDE (Visual Studio or VS Code) hasn't helped you with this already, be sure to download the latest v4.x version of the [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local).

## Breaking changes from in-process Durable Functions

The existing version of Durable Functions relied heavily on deep integration with the Azure Functions WebJobs-based runtime. The WebJobs runtime doesn't exist in the Azure Functions .NET Isolated worker, so several breaking changes were required to get Durable Functions working. In some cases, we also decided to make additional breaking changes for long-term strategic reasons. It's therefore best to think of this as *Durable Functions v3*.

A quick summary of the changes:

* **New NuGet packages, DLLs, and namespaces**: Just like with all the other bindings, .NET Isolated mandates a different naming convention for all triggers and bindings.
* **.NET 6 is required**: The new implementation _requires_ .NET 6 or newer. .NET Standard and earlier versions of .NET Core are not supported.
* **New types and APIs**: The names of the bindings are the same, but the types and APIs associated with these bindings have changed significantly. In general, all the same capabilities exist, but you won't be able to copy/paste code from existing Durable Functions apps or samples to the .NET Isolated experience (though we plan to do work to improve compatibility as we get closer to a stable release).
* **JSON serialization changes**: The existing version of Durable Functions used [Newtonsoft.Json](https://www.newtonsoft.com/json) for serialization. The .NET Isolated version instead uses the newer [System.Text.Json](https://docs.microsoft.com/dotnet/api/system.text.json) serializer. See [Microsoft's existing guidance](https://docs.microsoft.com/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to?pivots=dotnet-6-0) for  migrating from Newtonsoft.Json to System.Text.Json.
* **No Durable Entities**: This is coming later. No ETAs yet, but definitely before GA.

We don't yet have migration documentation in place so we recommend using Durable Functions for .NET Isolated only in new function apps.

It's important to note that the .NET Isolated worker represents the future of how functions will be written in .NET, as discussed in the [Azure Functions Roadmap](https://techcommunity.microsoft.com/t5/apps-on-azure-blog/net-on-azure-functions-roadmap/ba-p/2197916). Specifically, the out-of-process .NET worker host will permanently replace the in-process .NET model starting in the .NET 7 timeframe.

The good news is that there are also several new features available in the new .NET Isolated experience for Durable Functions, which are discussed below.

## Type-safe orchestrations

Durable Functions for .NET Isolated supports type-safe invocation of orchestrator and activity functions. Type safety is achieved by leveraging [.NET Source Generators](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/) to generate type-safe methods for invoking activity and orchestrator functions in your project. For example, suppose you have an activity function defined as follows:

```csharp
[Function(nameof(SayHello))]
public static string SayHello([ActivityTrigger] string cityName)
{
    return $"Hello, {cityName}!";
}
```

Rather than invoking this activity by its name and explicitly deserializing the result to a string (e.g. `string greeting = context.CallActivityAsync<string>("SayHello", "Tokyo")`), you can use a generated, type-safe extension method that does this for you:

```csharp
string greeting = await context.CallSayHelloAsync("Tokyo");
```

Similarly, orchestrators and sub-orchestrators can be started using dynamically generated type-safe extension methods.

```csharp
string instanceId = await client.ScheduleNewHelloCitiesInstanceAsync();
```

The source generator actively runs as you write your code, so the generated methods are available instantly when you define or rename your functions.

## Class-based syntax

Instead of declaring activity and orchestrator functions as methods, you also have the ability to declare them as classes. This is useful when you want to leverage object-oriented patterns for implementing orchestration logic.

> **NOTE**: The class-based syntax is also useful for hosting orchestrations outside of Azure Functions, which is a topic that will be discussed at a later time.

The following is an example of an orchestrator function implemented as a class that derives from `TaskOrchestratorBase<TInput, TOutput>`:

```csharp
[DurableTask]
class HelloCitiesTyped : TaskOrchestratorBase<string, string>
{
    protected override async Task<string> OnRunAsync(string? input)
    {
        string result = "";
        result += await this.Context.CallSayHelloTypedAsync("Tokyo") + "; ";
        result += await this.Context.CallSayHelloTypedAsync("London") + "; ";
        result += await this.Context.CallSayHelloTypedAsync("Seattle");
        return result;
    }
}
```

> **NOTE**: You must use the class-based syntax for orchestrations to get source-generator support. Activities support source-generation whether they use class-based syntax or the traditional functional syntax.

Activity functions can be defined as classes deriving from `TaskActivityBase<TInput, TOutput>`. When using the class-based programming model, activities can also take advantage of constructor-based dependency injection, as shown in the following example where `ILoggerFactory` is injected into the activity class constructor:

```csharp
[DurableTask(nameof(SayHelloTyped))]
class SayHelloTyped : TaskActivityBase<string, string>
{
    readonly ILogger logger;

    public SayHelloTyped(ILoggerFactory loggerFactory)
    {
        this.logger = loggerFactory.CreateLogger<SayHelloTyped>();
    }

    protected override string OnRun(string? cityName)
    {
        this.logger.LogInformation("Saying hello to {cityName}", cityName);
        return $"Hello, {cityName}!";
    }
}
```

> **NOTE**: By design, class-based orchestrators do not support constructor injection since dependency injection is inherently non-deterministic.

