using DDD.BuildingBlocks.Core.Event.Serialization;

namespace DDD.BuildingBlocks.Tests.Abstracts.Event;

public static class TestEventCodec
{
    public static IEventCodec Create()
    {
        var registry = new EventTypeRegistry()
            .Register<OrderCancelledEvent>("order.cancelled")
            .Register<OrderCertificateChangedEvent>("order.certificate-changed")
            .Register<OrderClosedEvent>("order.closed")
            .Register<OrderCommentChangedEvent>("order.comment-changed")
            .Register<OrderCreatedEvent>("order.created")
            .Register<OrderItemAddedToOrderEvent>("order.item-added")
            .Register<OrderItemBuyingPriceChangedEvent>("order-item.buying-price-changed")
            .Register<OrderItemCancelledEvent>("order-item.cancelled")
            .Register<OrderItemCreatedEvent>("order-item.created")
            .Register<OrderItemDescriptionChangedEvent>("order-item.description-changed")
            .Register<OrderItemNameChangedEvent>("order-item.name-changed")
            .Register<OrderItemRemovedFromOrderEvent>("order.item-removed")
            .Register<OrderItemStateChangedEvent>("order-item.state-changed")
            .Register<OrderTitleChangedEvent>("order.title-changed");

        return new SystemTextJsonEventCodec(registry);
    }
}
