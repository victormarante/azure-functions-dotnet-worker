// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using DurableTask;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace DurableFunctionsApp.OrderProcessingSample;

// This static class is intended to host the HTTP triggers used in this sample.
// The orchestration and activities are seperate classes.
static class OrderProcessingHttpTriggers
{
    /// <summary>
    /// HTTP-triggered function that starts the <see cref="ProcessOrderOrchestrator"/> orchestration.
    /// </summary>
    /// <param name="req">The HTTP request that was used to trigger this function.</param>
    /// <param name="durableContext">The Durable Functions client binding context object that is used to start and manage orchestration instances.</param>
    /// <param name="executionContext">The Azure Functions execution context, which is available to all function types.</param>
    /// <returns>Returns an HTTP response with more information about the started orchestration instance.</returns>
    [Function(nameof(StartOrderProcessingWorkflow))]
    public static async Task<HttpResponseData> StartOrderProcessingWorkflow(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [DurableClient] DurableClientContext durableContext,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(StartOrderProcessingWorkflow));
        OrderInfo? order = null;
        try
        {
            if (req.Body.Length > 0)
            {
                order = await req.ReadFromJsonAsync<OrderInfo>();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read the order info from the request payload");
        }

        if (order == null)
        {
            HttpResponseData badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            badRequestResponse.Headers.Add("Content-Type", "text/plain");
            badRequestResponse.WriteString(@"Please specify a valid order details JSON payload. Example: {""Item"":""catfood"",""Quantity"":10,""Price"":29.99}");
            return badRequestResponse;
        }

        // Instance IDs can be auto-generated or specified explicitly. Here, we use an explicitly specified ID.
        string instanceId = $"order-{order.Item.ToLowerInvariant()}-{Guid.NewGuid().ToString()[..6]}";

        // Source generators are used to generate type-safe extension methods for scheduling class-based
        // orchestrators that are defined in the current project. The name of the generated extension methods
        // are based on the names specified in the [DurableTask] attributes. Note that the source generator
        // will *not* generate type-safe extension methods for non-class-based orchestrator functions.
        await durableContext.Client.ScheduleNewProcessOrderWorkflowInstanceAsync(instanceId, input: order);
        logger.LogInformation("Created new orchestration with instance ID = {instanceId}", instanceId);

        // Return a payload that contains pointers for how to interact with this orchestration.
        // This is mainly to make the demo a bit more interesting and interactive.
        StringBuilder instructions = new();
        instructions.AppendLine($"A new ProcessOrderWorkflow orchestration with ID = {instanceId} has started.");
        if (order.Price >= ProcessOrderOrchestrator.RequiresApprovalCostThreshold)
        {
            instructions.Append($"The cost (${order.Price:C}) exceeds the auto-approval threshold of ");
            instructions.AppendLine($"{ProcessOrderOrchestrator.RequiresApprovalCostThreshold:C}, so it will require manager approval. ");
            instructions.AppendLine();

            string approveUrl = $"{req.Url.GetLeftPart(UriPartial.Authority)}/api/approvals/{instanceId}?action=approve";
            instructions.AppendLine($"If you are a manager, you can approve this purchase order by POST'ing to the following URL: ");
            instructions.AppendLine(approveUrl);
            instructions.AppendLine();

            string rejectUrl = $"{req.Url.GetLeftPart(UriPartial.Authority)}/api/approvals/{instanceId}?action=reject";
            instructions.AppendLine($"Alternatively, you can reject this purchase order by POST'ing to the following URL: ");
            instructions.AppendLine(rejectUrl);
            instructions.AppendLine();

            instructions.AppendLine($"If no approval is received in {ProcessOrderOrchestrator.ApprovalTimeout}, the order will be auto-rejected.");
        }
        else
        {
            instructions.Append($"No manual approval is required since the cost (${order.Price:C}) is below ");
            instructions.AppendLine($"the threshold of {ProcessOrderOrchestrator.RequiresApprovalCostThreshold:C}.");
        }

        string statusUrl = $"{req.Url.GetLeftPart(UriPartial.Authority)}/api/status/{instanceId}";
        instructions.AppendLine();
        instructions.AppendLine($"You can check the status of this order anytime at: {statusUrl}");

        HttpResponseData response = req.CreateResponse(HttpStatusCode.Created);
        response.Headers.Add("Location", statusUrl);
        response.Headers.Add("Content-Type", "text/plain");
        response.WriteString(instructions.ToString());
        return response;
    }

    [Function(nameof(GetOrderStatus))]
    public static async Task<HttpResponseData> GetOrderStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status/{instanceId}")] HttpRequestData req,
        string instanceId,
        [DurableClient] DurableClientContext durableContext,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(GetOrderStatus));

        OrchestrationMetadata? instanceMetadata = await durableContext.Client.GetInstanceMetadataAsync(
            instanceId,
            getInputsAndOutputs: true);

        if (instanceMetadata == null)
        {
            HttpResponseData notFound = req.CreateResponse(HttpStatusCode.NotFound);
            notFound.Headers.Add("Content-Type", "text/plain");
            notFound.WriteString($"No order with ID '{instanceId}' was found.");
            return notFound;
        }

        logger.LogInformation(
            "The workflow for purchase order {orderId} is in the {status} status and was last updated at {lastUpdated}.",
            instanceId,
            instanceMetadata.RuntimeStatus,
            instanceMetadata.LastUpdatedAt.ToString("s"));

        var statusPayload = new
        {
            id = instanceMetadata.InstanceId,
            orderDetails = instanceMetadata.ReadInputAs<object>(),
            statusDetails = instanceMetadata.ReadCustomStatusAs<object>(),
            isProcessed = instanceMetadata.IsCompleted,
        };

        HttpResponseData res = req.CreateResponse(HttpStatusCode.OK);
        await res.WriteAsJsonAsync(statusPayload);
        return res;
    }

    [Function(nameof(ApproveOrReject))]
    public static async Task<HttpResponseData> ApproveOrReject(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "approvals/{instanceId}")] HttpRequestData req,
        string instanceId,
        [DurableClient] DurableClientContext durableContext,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(ApproveOrReject));

        ApprovalEvent approval;

        NameValueCollection queryParams = HttpUtility.ParseQueryString(req.Url.Query);
        string? action = queryParams["action"]?.Trim()?.ToLowerInvariant();
        if (action == "approve")
        {
            logger.LogInformation("Purchase order {orderId} was approved!", instanceId);
            approval = new ApprovalEvent { Approver = "The Manager", IsApproved = true };
        }
        else if (action == "reject")
        {
            logger.LogInformation("Purchase order {orderId} was rejected!", instanceId);
            approval = new ApprovalEvent { Approver = "The Manager", IsApproved = false };
        }
        else
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            badRequest.Headers.Add("Content-Type", "text/plain");
            badRequest.WriteString($"Unknown approval action: '{action}'");
            return badRequest;
        }

        // This will deliver an external event to the orchestrator and resume it if it was waiting.
        await durableContext.Client.RaiseEventAsync(instanceId, eventName: "Approve", eventPayload: approval);

        return req.CreateResponse(HttpStatusCode.Accepted);
    }
}

// Note that the name specified in the [DurableTask] attribute controls both the name of the
// orchestrator as well as the source-generated extension method names on clients.
[DurableTask("ProcessOrderWorkflow")]
class ProcessOrderOrchestrator : TaskOrchestratorBase<OrderInfo, OrderStatus>
{
    // WARNING: Changing this value while orchestrations are actively running
    // can cause in-flight orchestrations to fail during their next replay because
    // it could cause them to take a different code path. For these types of
    // consequential changes, it's best to deploy a new copy of the orchestration.
    internal const double RequiresApprovalCostThreshold = 1000.00;

    // NOTE: It's safe to change the approval timeout even for existing orchestrations
    // since it will not impact the execution path during a replay.
    internal static readonly TimeSpan ApprovalTimeout = TimeSpan.FromSeconds(30);

    protected override async Task<OrderStatus> OnRunAsync(OrderInfo? orderInfo)
    {
        if (orderInfo == null)
        {
            // Unhandled exceptions transition the orchestration into a failed state. 
            throw new InvalidOperationException("Failed to read the order info!");
        }

        // Call the following activity operations in sequence.
        OrderStatus orderStatus = new();
        if (await this.Context.CallCheckInventoryAsync(orderInfo))
        {
            // Orders over $1,000 require manual approval. We use a custom status
            // value to communicate this back to the client application.
            bool requiresApproval = orderInfo.Price >= RequiresApprovalCostThreshold;
            this.Context.SetCustomStatus(new { requiresApproval });

            if (requiresApproval)
            {
                orderStatus.RequiresApproval = true;

                ApprovalEvent approvalEvent;
                try
                {
                    // Wait for the client application to send an approval event.
                    // Auto-reject if an approval isn't received in 30 seconds.
                    approvalEvent = await this.Context.WaitForExternalEvent<ApprovalEvent>(
                        eventName: "Approve",
                        timeout: ApprovalTimeout);
                }
                catch (TaskCanceledException)
                {
                    approvalEvent = new ApprovalEvent { IsApproved = false };
                }

                this.Context.SetCustomStatus(approvalEvent);

                orderStatus.Approval = approvalEvent;
                if (!approvalEvent.IsApproved)
                {
                    return orderStatus;
                }
            }

            await this.Context.CallChargeCustomerAsync(orderInfo);
            await this.Context.CallCreateShipmentAsync(orderInfo);

            return orderStatus;
        }

        return orderStatus;
    }
}


[DurableTask("CheckInventory")]
class CheckInventoryActivity : TaskActivityBase<OrderInfo, bool>
{
    readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckInventoryActivity"/> class.
    /// This class is initialized once for every activity execution.
    /// </summary>
    /// <remarks>
    /// Activity class constructors support constructor-based dependency injection.
    /// The injected services are provided by the function's <see cref="FunctionContext.InstanceServices"/> property.
    /// </remarks>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> injected by the Azure Functions runtime.</param>
    public CheckInventoryActivity(ILoggerFactory loggerFactory)
    {
        this.logger = loggerFactory.CreateLogger<CheckInventoryActivity>();
    }

    protected override async Task<bool> OnRunAsync(OrderInfo? orderInfo)
    {
        if (orderInfo == null)
        {
            throw new ArgumentException("Failed to read order info!");
        }

        this.logger.LogInformation(
            "{instanceId}: Checking inventory for '{item}'...found some!",
            this.Context.InstanceId,
            orderInfo.Item);

        // Simulate work being done
        await Task.Delay(TimeSpan.FromSeconds(1));
        return true;
    }
}


[DurableTask("CreateShipment")]
class CreateShipmentActivity : TaskActivityBase<OrderInfo, object>
{
    readonly ILogger logger;

    public CreateShipmentActivity(ILoggerFactory loggerFactory)
    {
        this.logger = loggerFactory.CreateLogger<CreateShipmentActivity>();
    }

    protected override async Task<object?> OnRunAsync(OrderInfo? orderInfo)
    {
        this.logger.LogInformation(
            "{instanceId}: Shipping customer order of {quantity} {item}(s)...",
            this.Context.InstanceId,
            orderInfo?.Quantity ?? 0,
            orderInfo?.Item);

        // Simulate some work being done (e.g. calling an external API).
        await Task.Delay(TimeSpan.FromSeconds(3));
        return null;
    }
}


[DurableTask("ChargeCustomer")]
class ChargeCustomerActivity : TaskActivityBase<OrderInfo, object>
{
    readonly ILogger logger;

    // Dependencies are injected from ASP.NET host service container
    public ChargeCustomerActivity(ILogger<ChargeCustomerActivity> logger)
    {
        this.logger = logger;
    }

    protected override async Task<object?> OnRunAsync(OrderInfo? orderInfo)
    {
        this.logger.LogInformation(
            "{instanceId}: Charging customer {price:C}'...",
            this.Context.InstanceId,
            orderInfo?.Price ?? 0.0);

        // Simulate some work being done (e.g. calling an external API).
        await Task.Delay(TimeSpan.FromSeconds(3));
        return null;
    }
}


public record OrderInfo(string Item, int Quantity, double Price);


public class OrderStatus
{
    public bool RequiresApproval { get; set; }
    public ApprovalEvent? Approval { get; set; }
}


public class ApprovalEvent
{
    public bool IsApproved { get; set; }

    public string? Approver { get; set; }
}
