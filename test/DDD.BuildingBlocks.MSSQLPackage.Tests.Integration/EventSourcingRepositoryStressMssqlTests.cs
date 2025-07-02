namespace DDD.BuildingBlocks.MSSQLPackage.Tests.Integration;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Core.Util;
using DDD.BuildingBlocks.Tests.Abstracts.Model;
using Xunit;

public class EventSourcingRepositoryStressMssqlTests : MSSQLTestBase
{
    private const string OrderTitle = "Prod Title ";
    private const string OrderComment = "Great Comment";
    private const OrderState OrderState = BuildingBlocks.Tests.Abstracts.Model.OrderState.Deactivated;

    private const string OrderItemDescription = "---Item Desc---";
    private const string OrderItemName = "Item Name#";
    private const decimal OrderItemSellingPrice = (decimal)149.92;

    [Fact()]
    public async Task Loading_aggregate_is_fast_even_when_many_events_were_persisted_by_one_save()
    {
        // Arrange
        const int numChanges = 20000;
        const int snapshotFrequency = 1013;
        const int allowedTimeInMilliseconds = 600;

        var orderId = Guid.NewGuid();
        var order = new Order(orderId.ToString(), OrderTitle, OrderComment, OrderState);
        var orderItem = new OrderItem(1, orderId.ToString(), OrderItemName, OrderItemDescription);

        // Because we save nearly 100000 events at once, the saving process will take some time...
        orderItem.SetBuyingPrice(OrderItemSellingPrice);
        orderItem.SetActive();
        order.ReferenceOrderItem(orderItem);
        for (var i = 1; i < numChanges + 1; i++)
        {
            order.ChangeComment(OrderComment + i);
        }
        var repository = GetRepositoryWithActivatedSnapshotProvider(snapshotFrequency);
        await repository.SaveAsync(order);
        await repository.SaveAsync(orderItem);

        var startTime = ApplicationTime.Current;
        var orderToCheck = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId.ToString()));
        var endTime = ApplicationTime.Current;
        var elapsedTime = endTime - startTime;

        // The read out will be fast, because we are using the snapshot provider
        elapsedTime.Milliseconds.Should().BeLessThan(allowedTimeInMilliseconds);
        orderToCheck!.LastCommittedVersion.Should().Be(numChanges + 1);
        orderToCheck.Comment.Should().Be($"{OrderComment}{numChanges}");
    }

    [Fact()]
    public async Task Loading_aggregate_is_fast_even_when_many_events_were_persisted_by_many_saves()
    {
        // Arrange
        const int numChanges = 20983;
        const int saveFrequency = 103;
        const int snapshotFrequency = 1013;
        const int allowedTimeInMilliseconds = 900;

        var orderId = Guid.NewGuid();
        var order = new Order(orderId.ToString(), OrderTitle, OrderComment, OrderState);
        var orderItem = new OrderItem(1, orderId.ToString(), OrderItemName, OrderItemDescription);

        // Act
        orderItem.SetBuyingPrice(OrderItemSellingPrice);
        orderItem.SetActive();
        order.ReferenceOrderItem(orderItem);
        var repository = GetRepositoryWithActivatedSnapshotProvider(snapshotFrequency);

        for (var i = 1; i < numChanges + 1; i++)
        {
            order.ChangeComment(OrderComment + i);
            if (i % saveFrequency == 0)
            {
                await repository.SaveAsync(order);
                await repository.SaveAsync(orderItem);
            }
        }

        await repository.SaveAsync(order);
        await repository.SaveAsync(orderItem);

        var startTime = DateTime.Now;
        var _ = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId.ToString()));
        var endTime = DateTime.Now;
        var elapsedTime = endTime - startTime;

        // Assert
        elapsedTime.Milliseconds.Should().BeLessThan(allowedTimeInMilliseconds);
    }

    [Fact(Skip = "Because it is slow.")]
    public async Task Aggregate_loads_correctly_without_SnapshotProvider_after_many_events()
    {
        // Attention here: This test will take some time because we are working without a snapshot provider
        const int numIterations = 100000;
        const int saveFrequency = 103;
        var newComment = "New Comment";
        var orderId = Guid.NewGuid();
        var order = new Order(orderId.ToString(), OrderTitle, OrderComment, OrderState);

        // Act
        var repository = GetRepositoryWithoutSnapshotProvider();
        for (var i = 1; i <= numIterations; i++)
        {
            order.ChangeComment(newComment + i);
            if (i % saveFrequency == 0)
            {
                await repository.SaveAsync(order);
            }
        }
        await repository.SaveAsync(order);

        var reloadedOrder = await repository.GetByIdAsync<Order,OrderId>(new OrderId(orderId.ToString()));

        // Assert
        reloadedOrder.Should().NotBeNull();
        reloadedOrder!.Title.Should().Be(OrderTitle);
        reloadedOrder.Comment.Should().Be(newComment + numIterations);
        reloadedOrder.State.Should().Be(OrderState);
        reloadedOrder.OrderItems.Should().HaveCount(0);
    }

    [Fact]
    public async Task Three_tasks_can_create_and_load_aggregates_without_interfering()
    {
        // Arrange
        var exceptionList1 = new List<Exception>();
        var exceptionList2 = new List<Exception>();
        var exceptionList3 = new List<Exception>();

        // Act + Assert
        var tasks = new List<Task>
        {
            Task.Factory.StartNew(() => CreateAndLoadOrders(177, exceptionList1), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default),
            Task.Factory.StartNew(() => CreateAndLoadOrders(449, exceptionList2), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default),
            Task.Factory.StartNew(() => CreateAndLoadOrders(989, exceptionList3), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default),
        };

        await  Task.WhenAll(tasks.ToArray());

        var exceptionCount = exceptionList1.Count + exceptionList2.Count + exceptionList3.Count;
        exceptionCount.Should().BeLessThan(5);
    }

    private async Task CreateAndLoadOrders(int count, List<Exception> exceptionList)
    {
        var repository = GetRepositoryWithoutSnapshotProvider();

        for (var i = 1; i <= count; i++)
        {
            try
            {
                var orderId = Guid.NewGuid();
                var order = new Order(orderId.ToString(), OrderTitle + i, OrderComment + i, OrderState);
                var newComment = $"Comment Update: {i}";
                var newTitle = $"Title Update: {i}";
                order.ChangeTitle(newTitle);
                order.ChangeComment(newComment);
                await repository.SaveAsync(order);

                var reloadedOrder = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId.ToString()));
                reloadedOrder.Should().NotBeNull();
                reloadedOrder!.Title.Should().Be(newTitle);
                reloadedOrder.Comment.Should().Be(newComment);
                reloadedOrder.OrderItems.Should().HaveCount(0);
            }
            catch (Exception ex)
            {
                exceptionList.Add(ex);
            }
        }
    }
}
