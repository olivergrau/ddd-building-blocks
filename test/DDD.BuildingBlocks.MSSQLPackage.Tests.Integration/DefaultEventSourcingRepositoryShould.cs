namespace DDD.BuildingBlocks.MSSQLPackage.Tests.Integration;

using System;
using System.Linq;
using System.Threading.Tasks;
using Core;
using FluentAssertions;
using Core.Exception;
using DDD.BuildingBlocks.Tests.Abstracts.Model;
using Xunit;

public class DefaultEventSourcingRepositoryShould : MSSQLTestBase
{
    private const string OrderTitle = "Prod Title";
    private const string OrderComment = "Great Order";
    private const string CertLabel = "Aristoteles";
    private const string CertCode = "My code";
    private const OrderState OrderState = BuildingBlocks.Tests.Abstracts.Model.OrderState.Deactivated;

    private const string OrderItemDescription = "---Item Desc---";
    private const string OrderItemName = "OrderItem Name#";
    private const decimal OrderItemSellingPrice = (decimal)149.92;

    private static string GetUniqueString(string prefix)
    {
        return prefix + "][" + Guid.NewGuid();
    }

    [Fact(DisplayName = "Correctly save and restore correlation ids")]
    [Trait("Category", "Integrationtest")]
    public async Task Correctly_save_and_restore_correlation_ids()
    {
        var correlationId = Guid.NewGuid()
            .ToString();

        using var correlatedScope = new CorrelatedScope(correlationId);

        // Arrange
        var orderId1 = Guid.NewGuid();
        var order1 = new Order(orderId1.ToString(), "Titel1", "Kommentar1", OrderState.Deactivated);

        var prefix = GetUniqueString("UniquePrefix");

        var code = "Code";
        order1.SetOptionalCertificate(prefix, code);

        var orderId2 = Guid.NewGuid();
        var order2 = new Order(orderId2.ToString(), "Titel2", "Kommentar2", OrderState.Deactivated);

        order2.SetOptionalCertificate(prefix + "-2", code);

        var repository = GetRepositoryWithoutSnapshotProvider();

        // now deactivate the aggregate
        order1.CloseOrder();

        await repository.SaveAsync(order1);
        await repository.SaveAsync(order2);

        var order1Repository = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId1.ToString()));

        order1Repository.Should()
            .NotBeNull();

        order1Repository!.CorrelationIds.Should()
            .Contain(correlationId);

        var order2Repository = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId2.ToString()));

        order2Repository.Should()
            .NotBeNull();

        order2Repository!.CorrelationIds.Should()
            .Contain(correlationId);

        var innerCorrelationId = Guid.NewGuid()
            .ToString();

        using var innerCorrelatedContext = new CorrelatedScope(innerCorrelationId);

        order2Repository.CloseOrder();

        await repository.SaveAsync(order2Repository);

        order2Repository = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId2.ToString()));

        order2Repository!.CorrelationIds.Should()
            .Contain(innerCorrelationId);

        order2Repository!.CorrelationIds.Should()
            .Contain(correlationId);

    }

    [Fact(DisplayName = "Correctly save and restore aggregates without correlation context")]
    [Trait("Category", "Integrationtest")]
    public async Task Correctly_save_and_restores_aggregates_without_correlation_context()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        var order1 = new Order(orderId1.ToString(), "Titel1", "Kommentar1", OrderState.Deactivated);

        var prefix = GetUniqueString("UniquePrefix");

        var code = "Code";
        order1.SetOptionalCertificate(prefix, code);

        var orderId2 = Guid.NewGuid();
        var order2 = new Order(orderId2.ToString(), "Titel2", "Kommentar2", OrderState.Deactivated);

        order2.SetOptionalCertificate(prefix + "-2", code);

        var repository = GetRepositoryWithoutSnapshotProvider();

        // now deactivate the aggregate
        order1.CloseOrder();

        await repository.SaveAsync(order1);
        await repository.SaveAsync(order2);

        var order1Repository = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId1.ToString()));

        order1Repository.Should()
            .NotBeNull();

        order1Repository!.CorrelationIds.Should()
            .HaveCount(0);

        var order2Repository = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId2.ToString()));

        order2Repository.Should()
            .NotBeNull();

        order2Repository!.CorrelationIds.Should()
            .HaveCount(0);
    }

    [Fact(DisplayName = "Allow saving two aggregates with same unique property only if the first one has been deactivated")]
    [Trait("Category", "Integrationtest")]
    public async Task Allow_saving_two_aggregates_with_same_unique_property_only_if_the_first_one_has_been_deactivated()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        var order1 = new Order(orderId1.ToString(), "Titel1", "Kommentar1", OrderState.Deactivated);

        var prefix = GetUniqueString("UniquePrefix");
        var code = "Code";
        order1.SetOptionalCertificate(prefix, code);

        var orderId2 = Guid.NewGuid();
        var order2 = new Order(orderId2.ToString(), "Titel2", "Kommentar2", OrderState.Deactivated);

        order2.SetOptionalCertificate(prefix, code);

        var repository = GetRepositoryWithoutSnapshotProvider();

        Func<Task> functor1 = async () => await repository.SaveAsync(order1);
        Func<Task> functor2 = async () => await repository.SaveAsync(order2);

        // Act + Assert
        await functor1.Should().NotThrowAsync("Because it saves the first time with that certificate");

        // now deactivate the aggregate
        order1.CloseOrder();

        await functor1.Should().NotThrowAsync("The aggregate should be deactivated");
        await functor2.Should().NotThrowAsync<ProviderException>();
    }

    [Fact(DisplayName = "Allow that unique properties on aggregates can be null")]
    public async Task Allow_that_unique_properties_on_aggregates_can_be_null()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        // Die unique property OptionalCertificate wird nicht gesetzt
        var order = new Order(orderId.ToString(), OrderTitle, OrderComment, OrderState);

        // Act + Assert
        var repository = GetRepositoryWithoutSnapshotProvider();
        await repository.SaveAsync(order);
        Func<Task> functor = async () => await repository.SaveAsync(order);
        await functor.Should().NotThrowAsync("Unique Properties are allowed to be null");
    }

    [Fact(DisplayName = "Not allow saving two aggregates with same unique property")]
    public async Task Not_allow_saving_two_aggregates_with_same_unique_property()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();

        var order1 = new Order(orderId1.ToString(), OrderTitle + "1", OrderComment + "1", OrderState);
        order1.SetOptionalCertificate(CertLabel, CertCode);

        var order2 = new Order(orderId2.ToString(), OrderTitle + "2", OrderComment + "2", OrderState);
        order2.SetOptionalCertificate(CertLabel, CertCode);

        // Act + Assert
        var repository = GetRepositoryWithoutSnapshotProvider();
        await repository.SaveAsync(order1);
        Func<Task> functor = async () => await repository.SaveAsync(order2);
        await functor.Should().ThrowAsync<ProviderException>();
    }

    [Fact(DisplayName = "Not allow saving two aggregates with the same id")]
    public async Task Not_allow_saving_two_aggregates_with_same_id()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        var order1 = new Order(orderId.ToString(), OrderTitle + "1", OrderComment + "1", OrderState);
        var order2 = new Order(orderId.ToString(), OrderTitle + "2", OrderComment + "2", OrderState);

        // Act + Assert
        var repository = GetRepositoryWithoutSnapshotProvider();
        await repository.SaveAsync(order1);
        Func<Task> functor = async () => await repository.SaveAsync(order2);
        await functor.Should().ThrowAsync<AggregateCreationException>();
    }

    [Fact(DisplayName = "Retrieve an aggregate after it was changed and saved")]
    public async Task Retrieve_an_aggregate_after_it_was_changed_and_saved()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(orderId.ToString(), OrderTitle, OrderComment, OrderState);
        const string newComment = "BromSulfit";

        // Act
        order.ChangeComment(newComment);
        var repository = GetRepositoryWithoutSnapshotProvider();
        await repository.SaveAsync(order);
        var reloadedOrder = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId.ToString()));

        // Assert
        reloadedOrder.Should().NotBeNull();
        reloadedOrder!.Title.Should().Be(OrderTitle);
        reloadedOrder.State.Should().Be(OrderState);
        reloadedOrder.Comment.Should().Be(newComment);
    }

    [Fact(DisplayName = "Retrieve an aggregate after it was changed and saved but only to a specific version")]
    public async Task Retrieve_an_aggregate_after_it_was_changed_and_saved_but_only_to_a_specific_version()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(orderId.ToString(), OrderTitle, OrderComment, OrderState);
        // Act
        for (var i = 0; i < 5; i++)
        {
            order.ChangeComment($"Comment {i}");
            order.ChangeTitle($"Title {i}");
        }

        var repository = GetRepositoryWithoutSnapshotProvider();
        await repository.SaveAsync(order);
        //var reloadedOrder = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId.ToString()));
        var reloadedOrder = await repository.GetByIdAsync(orderId.ToString(), typeof(Order), 5) as Order;

        // Assert
        reloadedOrder.Should().NotBeNull();
        reloadedOrder!.CurrentVersion.Should()
            .Be(5);

        reloadedOrder.Title.Should().Be("Title 1");
        reloadedOrder.Comment.Should().Be("Comment 2");
    }

    [Fact(DisplayName = "Retrieve an aggregate after it was changed and saved multiple times")]
    public async Task Retrieve_an_aggregate_after_it_was_changed_multiple_times_and_saved()
    {
        // Arrange
        var orderNumber = Guid.NewGuid();
        var order = new Order(orderNumber.ToString(), OrderTitle, OrderComment, OrderState);

        // Act
        order.ChangeComment("BromSulfit");
        order.ChangeComment("TriMagnesium-dinitrid");
        order.SetOptionalCertificate("Erle", "08/15");
        var repository = GetRepositoryWithoutSnapshotProvider();
        await repository.SaveAsync(order);
        var reloadedOrder = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderNumber.ToString()));

        // Assert
        reloadedOrder.Should().NotBeNull();
        reloadedOrder!.CurrentVersion.Should().Be(3);
        reloadedOrder.LastCommittedVersion.Should().Be(3);
        reloadedOrder.Title.Should().Be(OrderTitle);
        reloadedOrder.State.Should().Be(OrderState);
        reloadedOrder.Comment.Should().Be("TriMagnesium-dinitrid");
        reloadedOrder.OptionalCertificate!.Prefix.Should().Be("Erle");
        reloadedOrder.OptionalCertificate.Code.Should().Be("08/15");
    }

    [Fact(DisplayName = "Clear uncommitted changes after a correct save")]
    public async Task Clear_uncommitted_changes_after_a_correct_save()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(orderId.ToString(), OrderTitle, OrderComment, OrderState);
        const int numChanges = 103;

        // Act
        PerformMultipleChangeCommentOnAggregate(order, "BromSulfit", numChanges);
        var repository = GetRepositoryWithoutSnapshotProvider();
        await repository.SaveAsync(order);

        // Assert
        order.GetUncommittedChanges().Should().HaveCount(0);
    }

    [Fact(DisplayName = "Handle multiple aggregates without problems")]
    public async Task Handle_multiple_aggregates_without_problems()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(orderId.ToString(), OrderTitle, OrderComment, OrderState);
        var orderItem = new OrderItem(1, orderId.ToString(), OrderItemName, OrderItemDescription);

        // Act
        orderItem.SetBuyingPrice(OrderItemSellingPrice);
        orderItem.SetActive();
        order.ReferenceOrderItem(orderItem);
        order.ChangeComment("New Comment");

        var repository = GetRepositoryWithoutSnapshotProvider();
        await repository.SaveAsync(orderItem);
        await repository.SaveAsync(order);

        var reloadedOrder = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId.ToString()));
        var reloadedOrderItem = await repository.GetByIdAsync<OrderItem, OrderItemId>(
            new OrderItemId(1, orderId.ToString()));

        // Assert
        reloadedOrder.Should().NotBeNull();
        reloadedOrder!.Title.Should().Be(OrderTitle);
        reloadedOrder.Comment.Should().Be("New Comment");
        reloadedOrder.State.Should().Be(OrderState);
        reloadedOrder.OrderItems.Should().HaveCount(1);
        var reloadedOrderItemId = reloadedOrder.OrderItems.First().AggregateId;
        reloadedOrderItemId.Should().Be(new OrderItemId(1, orderId.ToString()).ToString());

        reloadedOrderItem.Should().NotBeNull();
        reloadedOrderItem!.Id.ToString().Should().Be(reloadedOrderItemId);
        reloadedOrderItem.Description.Should().Be(OrderItemDescription);
        reloadedOrderItem.Name.Should().Be(OrderItemName);
        reloadedOrderItem.SellingPrice.Should().Be(OrderItemSellingPrice);
    }

    [Fact(DisplayName = "Return null if a non existing aggregate with an invalid id is requested")]
    public async Task Return_null_if_a_non_existing_aggregate_with_an_invalid_id_is_requested()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        var repository = GetRepositoryWithoutSnapshotProvider();
        var order = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId.ToString()));

        // Assert
        order.Should().BeNull();
    }

    private static void PerformMultipleChangeCommentOnAggregate(Order order, string newComment, int count)
    {
        for (var i = 0; i < count; i++)
        {
            order.ChangeComment(newComment + i);
        }
    }
}
