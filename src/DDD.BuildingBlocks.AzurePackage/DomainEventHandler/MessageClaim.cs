// ReSharper disable UnusedAutoPropertyAccessor.Global

using System;

namespace DDD.BuildingBlocks.AzurePackage.DomainEventHandler;

public sealed class MessageClaim(Guid claimId, string messagePreview)
{
    public Guid ClaimId { get; } = claimId;
    public string MessagePreview { get; } = messagePreview.Length > 5000 ? messagePreview[..5000] : messagePreview;
}
