namespace DDD.BuildingBlocks.MSSQLPackage.Tests.Integration;

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using DDD.BuildingBlocks.Tests.Abstracts.Model;
using Xunit;

public class EventSourcingRepositorySnapshotMssqlTests : MSSQLTestBase
{
    private readonly string _orderTitle = "Order Title ";
    private readonly string _orderComment = "Great Comment";
    private readonly OrderState _orderState = OrderState.Deactivated;

    private readonly string _orderItemDescription = "---Item Desc---";
    private readonly string _orderItemName = "OrderItem Name#";
    private readonly decimal _orderItemSellingPrice = (decimal) 149.92;


    [Theory]
    [InlineData(73, 7, 19)]
    [InlineData(20, 10, 10)]
    [InlineData(5, 5, 3)]
    [InlineData(1, 1, 1)]
    public async Task SingleAggregate_loads_correctly_with_database_snapshots(
        int numIterations, int snapshotFrequency, int saveFrequency)
    {
        // Arrange
        const string newComment = "New Comment";
        var orderId = Guid.NewGuid();
        var order = new Order(orderId.ToString(), _orderTitle, _orderComment, _orderState);

        // Act
        order.ChangeTitle(_orderTitle);

        var repository = GetRepositoryWithActivatedSnapshotProvider(snapshotFrequency);
        for (int i = 1; i <= numIterations; i++)
        {
            order.ChangeComment(newComment + i);
            if (i % saveFrequency == 0)
            {
                await repository.SaveAsync(order);
            }
        }

        await repository.SaveAsync(order);

        var reloadedOrder = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId.ToString()));

        // Assert
        reloadedOrder.Should().NotBeNull();
        reloadedOrder!.Title.Should().Be(_orderTitle);
        reloadedOrder.Comment.Should().Be(newComment + numIterations);
        reloadedOrder.State.Should().Be(_orderState);
        reloadedOrder.OrderItems.Should().HaveCount(0);
    }

    [Theory]
    [InlineData(97, 17, 11, 13)]
    [InlineData(20, 10, 10, 10)]
    [InlineData(52, 5, 10, 15)]
    [InlineData(40, 5, 10, 20)]
    [InlineData(1, 1, 1, 1)]
    public async Task MultipleAggregates_load_correctly_with_database_snapshots(
        int numIterations, int snapshotFrequency, int saveFrequencyOrder, int saveFrequencyVariant)
    {
        // Arrange

        const string newName = "New Name";
        const string newTitle = "New Title";
        const string newComment = "New Comment";
        var orderId = Guid.NewGuid();
        var order = new Order(orderId.ToString(), _orderTitle, _orderComment, _orderState);
        var orderItem = new OrderItem(1, orderId.ToString(), _orderItemName, _orderItemDescription);

        // Act
        orderItem.SetBuyingPrice(_orderItemSellingPrice);
        orderItem.SetActive();
        order.ReferenceOrderItem(orderItem);
        order.ChangeTitle(newTitle);

        var repository = GetRepositoryWithActivatedSnapshotProvider(snapshotFrequency);
        for (int i = 1; i <= numIterations; i++)
        {
            order.ChangeTitle(newTitle + i);
            order.ChangeComment(newComment + i);
            orderItem.ChangeName(newName + i);
            if (i % saveFrequencyOrder == 0)
            {
                await repository.SaveAsync(order);
            }

            if (i % saveFrequencyVariant == 0)
            {
                await repository.SaveAsync(orderItem);
            }
        }

        await repository.SaveAsync(order);
        await repository.SaveAsync(orderItem);

        var reloadedOrder = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId.ToString()));
        var reloadedOrderItem = await repository.GetByIdAsync<OrderItem, OrderItemId>(
            new OrderItemId(1, orderId.ToString()));

        // Assert
        reloadedOrder.Should().NotBeNull();
        reloadedOrder!.Title.Should().Be(newTitle+numIterations);
        reloadedOrder.Comment.Should().Be(newComment + numIterations);
        reloadedOrder.State.Should().Be(_orderState);
        reloadedOrder.OrderItems.Should().HaveCount(1);
        var reloadedOrderItemId = reloadedOrder.OrderItems.First().AggregateId;
        reloadedOrderItemId.Should().Be(new OrderItemId(1, orderId.ToString()).ToString());

        reloadedOrderItem.Should().NotBeNull();
        reloadedOrderItem!.Description.Should().Be(_orderItemDescription);
        reloadedOrderItem.Name.Should().Be(newName + numIterations);
        reloadedOrderItem.SellingPrice.Should().Be(_orderItemSellingPrice);
    }
}
