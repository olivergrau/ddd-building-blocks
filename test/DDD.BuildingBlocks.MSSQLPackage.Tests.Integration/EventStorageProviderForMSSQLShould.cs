namespace DDD.BuildingBlocks.MSSQLPackage.Tests.Integration;

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Core.Exception;
using Core.Persistence.Repository;
using Provider;
using DDD.BuildingBlocks.Tests.Abstracts.Event;
using DDD.BuildingBlocks.Tests.Abstracts.Model;
using Xunit;

public class EventStorageProviderForMssqlShould : MSSQLTestBase
{
    private const string OrderTitle = "Order Title";
    private const string OrderComment = "Great Comment";
    private const string CertLabel = "Aristoteles";
    private const string CertCode = "My code";
    private const OrderState OrderState = BuildingBlocks.Tests.Abstracts.Model.OrderState.Deactivated;


    [Fact(DisplayName = "Create aggregate with 10 events and only load 5 events")]
    public async Task Create_10_events_and_only_load_5_events()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(orderId.ToString(), OrderTitle, OrderComment, OrderState);
        var storageProvider = new EventStorageProvider(new EventStorageProviderSettings { ConnectionString = ConnectionString! }.AsOptionsMonitor());
        var repository = new EventSourcingRepository(storageProvider);

        // Act
        for (int i = 0; i < 5; i++)
        {
            order.ChangeTitle($"Title {i+1}");
            order.ChangeComment($"Comment {i+1}");
        }

        await repository.SaveAsync(order);

        // Assert
        var eventList = await storageProvider
            .GetEventsAsync(typeof(Order), order.Id.ToString(), 0, 5);

        var domainEvents = eventList!.ToList();
        domainEvents.Should().NotBeNull();
        domainEvents.Should().HaveCount(5);
        domainEvents.First().Should().BeOfType<OrderCreatedEvent>();
        domainEvents.Last().Should().BeOfType<OrderCommentChangedEvent>();
    }

    [Fact(DisplayName = "Create two events of correct type when used in a repository")]
    public async Task Create_two_events_of_correct_type_when_used_in_a_repository()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(orderId.ToString(), OrderTitle, OrderComment, OrderState);
        var storageProvider = new EventStorageProvider(new EventStorageProviderSettings { ConnectionString = ConnectionString! }.AsOptionsMonitor());
        var repository = new EventSourcingRepository(storageProvider);

        // Act
        order.SetOptionalCertificate(CertLabel, CertCode);
        await repository.SaveAsync(order);

        // Assert
        var eventList = await storageProvider
            .GetEventsAsync(typeof(Order), order.Id.ToString(), 0, int.MaxValue);

        var domainEvents = eventList!.ToList();
        domainEvents.Should().NotBeNull();
        domainEvents.Should().HaveCount(2);
        domainEvents.First().Should().BeOfType<OrderCreatedEvent>();
        domainEvents.Last().Should().BeOfType<OrderCertificateChangedEvent>();
    }

    [Fact]
    public async Task Aggregate_save_creates_event_with_TargetVersion_minus_one()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(orderId.ToString(), OrderTitle, OrderComment, OrderState);
        var storageProvider = new EventStorageProvider(new EventStorageProviderSettings { ConnectionString = ConnectionString! }.AsOptionsMonitor());
        var repository = new EventSourcingRepository(storageProvider);

        // Act
        await repository.SaveAsync(order);

        // Assert
        var eventList = await storageProvider.GetEventsAsync(typeof(Order), order.Id.ToString(), 0, Int32.MaxValue);
        var domainEvents = eventList!.ToList();
        domainEvents.Should().NotBeNull();
        domainEvents.First().TargetVersion.Should().Be(-1);
    }

    [Fact]
    public async Task MethodCallOnAggregate_save_creates_second_event_with_TargetVersion_zero()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(orderId.ToString(), OrderTitle, OrderComment, OrderState);
        var storageProvider = new EventStorageProvider(new EventStorageProviderSettings { ConnectionString = ConnectionString! }.AsOptionsMonitor());
        var repository = new EventSourcingRepository(storageProvider);

        // Act
        order.SetOptionalCertificate(CertLabel, CertCode);
        await repository.SaveAsync(order);

        // Assert
        var eventList = await storageProvider.GetEventsAsync(typeof(Order), order.Id.ToString(), 0, int.MaxValue);
        var domainEvents = eventList!.ToList();
        domainEvents.Should().NotBeNull();
        domainEvents.Should().HaveCount(2);
        domainEvents.Last().TargetVersion.Should().Be(0);
    }

    [Fact]
    public async Task Reconstituting_event_with_invalid_type_throws_InvalidTypeException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order(orderId.ToString(), OrderTitle, OrderComment, OrderState);
        var storageProvider = new EventStorageProvider(new EventStorageProviderSettings { ConnectionString = ConnectionString! }.AsOptionsMonitor());
        var repository = new EventSourcingRepository(storageProvider);
        order.SetOptionalCertificate(CertLabel, CertCode);
        await repository.SaveAsync(order);
        await PrepareEventWithInvalidType();

        // Act
        try
        {
            var _ = await repository.GetByIdAsync<Order, OrderId>(new OrderId(orderId.ToString()));
            true.Should().BeFalse();
        }
        catch (ProviderException e)
        {
            e.InnerException.Should().BeOfType<InvalidTypeException>();
        }
    }
}
