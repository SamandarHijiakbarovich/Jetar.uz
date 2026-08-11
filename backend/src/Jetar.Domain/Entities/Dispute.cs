using Jetar.Domain.Enums;

namespace Jetar.Domain.Entities;

/// <summary>Nizo — bitimga 1:1. Ochilganda pul escrowda muzlatiladi.</summary>
public class Dispute
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    /// <summary>Nizoni ochgan tomon.</summary>
    public Guid OpenedById { get; set; }
    public User? OpenedBy { get; set; }

    public string Reason { get; set; } = string.Empty;

    /// <summary>Skrinshot va boshqa dalillar — jsonb.</summary>
    public List<string> EvidenceUrls { get; set; } = new();

    public DisputeStatus Status { get; set; } = DisputeStatus.Open;

    public Guid? ResolvedById { get; set; }
    public User? ResolvedBy { get; set; }

    public string? ResolutionNote { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
}
