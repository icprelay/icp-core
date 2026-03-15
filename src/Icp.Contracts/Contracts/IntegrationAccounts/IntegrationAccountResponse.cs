namespace Icp.Contracts.IntegrationAccounts;

public sealed record IntegrationAccountResponse(
    Guid AccountId,
    string DisplayName,
    string ExternalCustomerId,
    bool Enabled,
    string InboundKeyHash,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
