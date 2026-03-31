using System.Net.Http;
using Icp.Contracts.Common;
using Icp.Contracts.EventTraces;
using Icp.Contracts.EventTypes;
using Icp.Contracts.IntegrationAccounts;
using Icp.Contracts.Instances;
using Icp.Contracts.IntegrationTargets;
using Icp.Contracts.Runs;
using Microsoft.Extensions.Options;

namespace Icp.Ui;

public sealed class IcpApiClient(HttpClient httpClient)
{
    public async Task PingHealthAsync(CancellationToken ct)
    {
        var resp = await httpClient.GetAsync("/health", ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<IntegrationAccountResponse> CreateIntegrationAccountAsync(
        string externalCustomerId,
        string displayName,
        bool enabled,
        string inboundKeyHash,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(externalCustomerId))
            throw new ArgumentException("externalCustomerId is required", nameof(externalCustomerId));

        var req = new
        {
            DisplayName = displayName ?? string.Empty,
            ExternalCustomerId = externalCustomerId,
            Enabled = enabled,
            InboundKeyHash = inboundKeyHash ?? string.Empty,
        };

        var resp = await httpClient.PostAsJsonAsync("/api/integrationaccounts", req, ct);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<IntegrationAccountResponse>(cancellationToken: ct);
        return body ?? throw new InvalidOperationException("API returned empty response.");
    }

    public async Task<IReadOnlyList<InstanceResponse>> ListCustomerInstancesAsync(string customerId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("customerId is required", nameof(customerId));

        var resp = await httpClient.GetFromJsonAsync<List<InstanceResponse>>(
            $"/api/customers/{Uri.EscapeDataString(customerId)}/instances",
            cancellationToken: ct);

        return resp ?? [];
    }

    public async Task<IReadOnlyList<InstanceResponse>> ListAllInstancesAsync(string? customerId, string? subscribedEventType, CancellationToken ct)
    {
        var url = "/api/instances";
        var qp = new List<string>();
        if (!string.IsNullOrWhiteSpace(customerId))
            qp.Add($"customerId={Uri.EscapeDataString(customerId.Trim())}");
        if (!string.IsNullOrWhiteSpace(subscribedEventType))
            qp.Add($"subscribedEventType={Uri.EscapeDataString(subscribedEventType.Trim())}");

        if (qp.Count > 0)
            url += $"?{string.Join("&", qp)}";

        var resp = await httpClient.GetFromJsonAsync<List<InstanceResponse>>(url, cancellationToken: ct);
        return resp ?? [];
    }

    public async Task<IReadOnlyList<InstanceResponse>> ListAllHumanInstancesAsync(string? customerId, string? subscribedEventType, CancellationToken ct)
    {
        var url = "/api/instances/humans";
        var qp = new List<string>();
        if (!string.IsNullOrWhiteSpace(customerId))
            qp.Add($"customerId={Uri.EscapeDataString(customerId.Trim())}");
        if (!string.IsNullOrWhiteSpace(subscribedEventType))
            qp.Add($"subscribedEventType={Uri.EscapeDataString(subscribedEventType.Trim())}");

        if (qp.Count > 0)
            url += $"?{string.Join("&", qp)}";

        var resp = await httpClient.GetFromJsonAsync<List<InstanceResponse>>(url, cancellationToken: ct);
        return resp ?? [];
    }

    public async Task<InstanceResponse> CreateInstanceAsync(string customerId, CreateInstanceRequest request, CancellationToken ct)
    {
        // Allow system instances: pass empty to map to Guid.Empty in the API route.
        var cid = string.IsNullOrWhiteSpace(customerId)
            ? Guid.Empty.ToString("D")
            : customerId;

        var resp = await httpClient.PostAsJsonAsync(
            $"/api/customers/{Uri.EscapeDataString(cid)}/instances",
            request,
            ct);

        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<InstanceResponse>(cancellationToken: ct);
        return body ?? throw new InvalidOperationException("API returned empty response.");
    }

    public async Task<InstanceResponse> UpdateInstanceAsync(Guid instanceId, UpdateInstanceRequest request, CancellationToken ct)
    {
        if (instanceId == Guid.Empty)
            throw new ArgumentException("instanceId is required", nameof(instanceId));

        var resp = await httpClient.PutAsJsonAsync($"/api/instances/{instanceId:D}", request, ct);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<InstanceResponse>(cancellationToken: ct);
        return body ?? throw new InvalidOperationException("API returned empty response.");
    }

    public async Task<InstanceResponse> SetInstanceSecretsAsync(Guid instanceId, Dictionary<string, string> secrets, CancellationToken ct)
    {
        if (instanceId == Guid.Empty)
            throw new ArgumentException("instanceId is required", nameof(instanceId));
        if (secrets is null || secrets.Count == 0)
            throw new ArgumentException("secrets is required", nameof(secrets));

        var req = new SetInstanceSecretsRequest(secrets);

        var resp = await httpClient.PostAsJsonAsync($"/api/instances/{instanceId:D}/secrets", req, ct);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<InstanceResponse>(cancellationToken: ct);
        return body ?? throw new InvalidOperationException("API returned empty response.");
    }

    public async Task<PagedResponse<RunResponse>> ListRunsPagedAsync(Guid instanceId, int page, int pageSize, CancellationToken ct)
    {
        if (instanceId == Guid.Empty)
            throw new ArgumentException("instanceId is required", nameof(instanceId));
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize));

        var resp = await httpClient.GetFromJsonAsync<PagedResponse<RunResponse>>(
            $"/api/instances/{instanceId:D}/runs?page={page}&pageSize={pageSize}",
            cancellationToken: ct);

        return resp ?? new PagedResponse<RunResponse>([], page, pageSize, TotalCount: 0);
    }

    public async Task<IReadOnlyList<RunResponse>> ListRunsAsync(Guid instanceId, CancellationToken ct)
    {
        if (instanceId == Guid.Empty)
            throw new ArgumentException("instanceId is required", nameof(instanceId));

        // Back-compat: ask for a large page.
        var paged = await ListRunsPagedAsync(instanceId, page: 1, pageSize: 200, ct);
        return paged.Items;
    }

    public async Task<InstanceResponse> EnableInstanceAsync(Guid instanceId, CancellationToken ct)
    {
        if (instanceId == Guid.Empty)
            throw new ArgumentException("instanceId is required", nameof(instanceId));

        var resp = await httpClient.PostAsync($"/api/instances/{instanceId:D}/enable", content: null, ct);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<InstanceResponse>(cancellationToken: ct);
        return body ?? throw new InvalidOperationException("API returned empty response.");
    }

    public async Task<InstanceResponse> DisableInstanceAsync(Guid instanceId, CancellationToken ct)
    {
        if (instanceId == Guid.Empty)
            throw new ArgumentException("instanceId is required", nameof(instanceId));

        var resp = await httpClient.PostAsync($"/api/instances/{instanceId:D}/disable", content: null, ct);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<InstanceResponse>(cancellationToken: ct);
        return body ?? throw new InvalidOperationException("API returned empty response.");
    }

    public async Task<RunResponse> StartRunAsync(Guid instanceId, string? correlationId, CancellationToken ct)
    {
        if (instanceId == Guid.Empty)
            throw new ArgumentException("instanceId is required", nameof(instanceId));

        var req = new StartRunRequest(correlationId);

        var resp = await httpClient.PostAsJsonAsync($"/api/instances/{instanceId:D}/invoke", req, ct);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<RunResponse>(cancellationToken: ct);
        return body ?? throw new InvalidOperationException("API returned empty response.");
    }

    public async Task<IReadOnlyList<IntegrationTargetResponse>> ListIntegrationTargetsAsync(CancellationToken ct)
    {
        var resp = await httpClient.GetFromJsonAsync<List<IntegrationTargetResponse>>("/api/integrationtargets", cancellationToken: ct);
        return resp ?? [];
    }

    public async Task<IReadOnlyList<IntegrationAccountResponse>> ListIntegrationAccountsAsync(CancellationToken ct)
    {
        var resp = await httpClient.GetFromJsonAsync<List<IntegrationAccountResponse>>("/api/integrationaccounts", cancellationToken: ct);
        return resp ?? [];
    }

    public async Task<IntegrationAccountResponse?> GetIntegrationAccountAsync(Guid accountId, CancellationToken ct)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("accountId is required", nameof(accountId));

        try
        {
            return await httpClient.GetFromJsonAsync<IntegrationAccountResponse>(
                $"/api/integrationaccounts/{accountId:D}",
                cancellationToken: ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<IntegrationAccountResponse> EnableIntegrationAccountAsync(Guid accountId, CancellationToken ct)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("accountId is required", nameof(accountId));

        var resp = await httpClient.PostAsync($"/api/integrationaccounts/{accountId:D}/enable", content: null, ct);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<IntegrationAccountResponse>(cancellationToken: ct);
        return body ?? throw new InvalidOperationException("API returned empty response.");
    }

    public async Task<IReadOnlyList<InstanceResponse>> ListIntegrationAccountInstancesAsync(Guid accountId, CancellationToken ct)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("accountId is required", nameof(accountId));

        var resp = await httpClient.GetFromJsonAsync<List<InstanceResponse>>(
            $"/api/integrationaccounts/{accountId:D}/instances",
            cancellationToken: ct);

        return resp ?? [];
    }

    public async Task<IntegrationAccountResponse> DisableIntegrationAccountAsync(Guid accountId, CancellationToken ct)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("accountId is required", nameof(accountId));

        var resp = await httpClient.PostAsync($"/api/integrationaccounts/{accountId:D}/disable", content: null, ct);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<IntegrationAccountResponse>(cancellationToken: ct);
        return body ?? throw new InvalidOperationException("API returned empty response.");
    }

    public async Task<IReadOnlyList<IntegrationTargetResponse>> ListIntegrationTargetsAsync(string? triggerType, CancellationToken ct)
    {
        var url = string.IsNullOrWhiteSpace(triggerType)
            ? "/api/integrationtargets"
            : $"/api/integrationtargets?triggerType={Uri.EscapeDataString(triggerType.Trim())}";

        var resp = await httpClient.GetFromJsonAsync<List<IntegrationTargetResponse>>(url, cancellationToken: ct);
        return resp ?? [];
    }

    public async Task<IReadOnlyList<EventTypeResponse>> ListEventTypesAsync(CancellationToken ct)
    {
        var resp = await httpClient.GetFromJsonAsync<List<EventTypeResponse>>("/api/eventtypes", cancellationToken: ct);
        return resp ?? [];
    }

    public async Task<IReadOnlyList<EventTypeResponse>> ListEventTypesAsync(string? triggerType, CancellationToken ct)
    {
        var url = string.IsNullOrWhiteSpace(triggerType)
            ? "/api/eventtypes"
            : $"/api/eventtypes?triggerType={Uri.EscapeDataString(triggerType.Trim())}";

        var resp = await httpClient.GetFromJsonAsync<List<EventTypeResponse>>(url, cancellationToken: ct);
        return resp ?? [];
    }

    public async Task<IReadOnlyList<ScheduleTimeZoneResponse>> ListScheduleTimeZonesAsync(CancellationToken ct)
    {
        var resp = await httpClient.GetFromJsonAsync<List<ScheduleTimeZoneResponse>>("/api/scheduletimezones", cancellationToken: ct);
        return resp ?? [];
    }

    public async Task<InstanceResponse?> GetInstanceAsync(Guid instanceId, CancellationToken ct)
    {
        if (instanceId == Guid.Empty)
            throw new ArgumentException("instanceId is required", nameof(instanceId));

        try
        {
            return await httpClient.GetFromJsonAsync<InstanceResponse>($"/api/instances/{instanceId:D}", cancellationToken: ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task DeleteInstanceAsync(Guid instanceId, CancellationToken ct)
    {
        if (instanceId == Guid.Empty)
            throw new ArgumentException("instanceId is required", nameof(instanceId));

        var resp = await httpClient.DeleteAsync($"/api/instances/{instanceId:D}", ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<InstanceResponse>> ListInstancesAsync(string customerId, string? subscribedEventType, CancellationToken ct)
    {
        var list = await ListCustomerInstancesAsync(customerId, ct);

        if (string.IsNullOrWhiteSpace(subscribedEventType))
            return list;

        var filter = subscribedEventType.Trim();
        return list
            .Where(x => string.Equals(x.SubscribedEventType, filter, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<InstanceResponse> CreateInstanceIfMissingAsync(
        string customerId,
        string customerName,
        string integrationTarget,
        string subscribedEventType,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(integrationTarget))
            throw new ArgumentException("integrationTarget is required", nameof(integrationTarget));
        if (string.IsNullOrWhiteSpace(subscribedEventType))
            throw new ArgumentException("subscribedEventType is required", nameof(subscribedEventType));

        var existing = await ListAllInstancesAsync(customerId, subscribedEventType, ct);
        var forTarget = existing.FirstOrDefault(x => string.Equals(x.IntegrationTarget, integrationTarget, StringComparison.OrdinalIgnoreCase));
        if (forTarget is not null)
            return forTarget;

        var req = new CreateInstanceRequest(
            IntegrationTarget: integrationTarget,
            SubscribedEventType: subscribedEventType,
            Enabled: true,
            IntegrationTargetParametersJson: "{}",
            EventParametersJson: "{}",
            SecretRefsJson: "{}",
            CustomerName: customerName,
            TriggerType: "Event");

        return await CreateInstanceAsync(customerId, req, ct);
    }

    public async Task<IReadOnlyList<RunResponse>> ListAllRunsLatestAsync(CancellationToken ct)
    {
        var resp = await httpClient.GetFromJsonAsync<List<RunResponse>>("/api/runs", cancellationToken: ct);
        return resp ?? [];
    }

    public async Task<byte[]> DownloadRunOutputAsync(string runId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(runId))
            throw new ArgumentException("runId is required", nameof(runId));

        var resp = await httpClient.GetAsync($"/api/runs/{runId}/output", cancellationToken: ct);
        resp.EnsureSuccessStatusCode();

        return await resp.Content.ReadAsByteArrayAsync();
    }

    public async Task<IReadOnlyList<EventTraceResponse>> ListEventTracesAsync(string? accountKey, string? status, int? limit, CancellationToken ct)
    {
        var url = "/api/event-traces";
        var qp = new List<string>();

        if (!string.IsNullOrWhiteSpace(accountKey))
            qp.Add($"accountKey={Uri.EscapeDataString(accountKey.Trim())}");
        if (!string.IsNullOrWhiteSpace(status))
            qp.Add($"status={Uri.EscapeDataString(status.Trim())}");
        if (limit.HasValue)
            qp.Add($"limit={limit.Value}");

        if (qp.Count > 0)
            url += $"?{string.Join("&", qp)}";

        var resp = await httpClient.GetFromJsonAsync<List<EventTraceResponse>>(url, cancellationToken: ct);
        return resp ?? [];
    }

    public async Task<EventTraceResponse?> GetEventTraceAsync(Guid eventId, CancellationToken ct)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("eventId is required", nameof(eventId));

        try
        {
            return await httpClient.GetFromJsonAsync<EventTraceResponse>(
                $"/api/event-traces/{eventId:D}",
                cancellationToken: ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<EventStepResponse>> ListEventStepsAsync(Guid eventId, CancellationToken ct)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("eventId is required", nameof(eventId));

        var resp = await httpClient.GetFromJsonAsync<List<EventStepResponse>>(
            $"/api/event-traces/{eventId:D}/steps",
            cancellationToken: ct);

        return resp ?? [];
    }
}
