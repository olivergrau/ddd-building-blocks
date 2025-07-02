namespace DDD.BuildingBlocks.MSSQLPackage.Tests.Integration;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Core.Exception;
using DDD.BuildingBlocks.Tests.Abstracts.Model;
using Xunit;

public class EventSourcingRepositoryParallelMssqlTests : MSSQLTestBase
{
    private const string OrderName = "Bratpfannen-Wender";
    private const string OrderDescription = "Aristoteles war kein Belgier";
    private const OrderState OrderState = BuildingBlocks.Tests.Abstracts.Model.OrderState.Sold;

    [Fact]
    public async Task Changing_two_Aggregate_instances_with_same_version_throws_ConcurrencyException()
    {
        // Arrange
        var orderId = await CreateAndSaveOrder();
        const string newComment1 = "Comment change of Aggregate 1";
        const string newComment2 = "Comment change of Aggregate 2";

        // Act
        var repository = GetRepositoryWithoutSnapshotProvider();
        var aggregateInstance1 = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId.ToString()));
        var aggregateInstance2 = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId.ToString()));
        aggregateInstance1!.ChangeComment(newComment1);
        aggregateInstance2!.ChangeComment(newComment2);
        await repository.SaveAsync(aggregateInstance2);

        // Act + Assert
        Func<Task> functor = async () => await repository.SaveAsync(aggregateInstance1);
        await functor.Should().ThrowAsync<ConcurrencyException>();
    }

    [Fact]
    public async Task Operation_sequence_of_load_change_save_load_change_save_on_one_aggregate_throws_no_exception()
    {
        // Arrange
        var orderId = await CreateAndSaveOrder();
        const string newComment1 = "Comment change of Aggregate 1";
        const string newComment2 = "Comment change of Aggregate 2";

        // Act
        var repository = GetRepositoryWithoutSnapshotProvider();
        var order = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId.ToString()));
        order!.ChangeComment(newComment1);
        await repository.SaveAsync(order);
        order = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId.ToString()));
        order!.ChangeComment(newComment2);

        // Act + Assert
        Func<Task> functor = async () => await repository.SaveAsync(order);
        await functor.Should().NotThrowAsync();
    }

    [Fact(Skip = "Must be reworked.")]
    public async Task Two_threads_change_same_aggregate_throws_ConcurrencyException()
    {
        // Arrange
        var orderId = await CreateAndSaveOrder();
        var exceptionList1 = new List<Exception>();
        var exceptionList2 = new List<Exception>();

        // Act
        var tasks = new List<Task>
        {
            PerformChangesOnAggregate(orderId, 1087, 47, exceptionList1),
            PerformChangesOnAggregate(orderId, 1117, 73, exceptionList2)
        };

        await Task.WhenAll(tasks.ToArray());

        // Act + Assert
        var exceptionCount = exceptionList1.Count + exceptionList2.Count;
        exceptionCount.Should().BeGreaterThan(0);
    }

    private async Task PerformChangesOnAggregate(Guid aggregateId, int count, int saveFrequency, List<Exception> exceptionList)
    {
        var repository = GetRepositoryWithoutSnapshotProvider();
        var order = await repository.GetByIdAsync<Order, OrderId>(new OrderId(aggregateId.ToString()));

        for (var i = 1; i <= count; i++)
        {
            order!.ChangeTitle($"Title Update: {i}");
            order.ChangeComment($"Comment Update: {i}");

            if (i % saveFrequency == 0)
            {
                try
                {
                    await repository.SaveAsync(order);
                }
                catch (Exception ex)
                {
                    exceptionList.Add(ex);
                }
                finally
                {
                    order = await repository.GetByIdAsync<Order, OrderId>(order.Id);
                }
            }
        }
    }

    private async Task<Guid> CreateAndSaveOrder()
    {
        var orderId = Guid.NewGuid();
        var order = new Order(orderId.ToString(), OrderName, OrderDescription, OrderState);
        var repository = GetRepositoryWithoutSnapshotProvider();
        await repository.SaveAsync(order);
        return orderId;
    }
}
