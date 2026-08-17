
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable EmptyConstructor

namespace DDD.BuildingBlocks.DevelopmentPackage.Tests.Integration
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Threading.Tasks;
    using System.Threading;
    using FluentAssertions;
    using Core.Event;
    using Core.Persistence.Repository;
    using BackgroundService;
    using EventPublishing;
    using Storage;
    using DDD.BuildingBlocks.Tests.Abstracts.Event;
    using DDD.BuildingBlocks.Tests.Abstracts.Model;
    using DDD.BuildingBlocks.DI.Extensions.Dispatching;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public sealed class EventSourcingRepositoryWithEventPublishingTableShould : IDisposable
    {
        [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
        public class TestSubscriberAlpha : ISubscribe<OrderCreatedEvent>, ISubscribe<OrderItemCreatedEvent>
        {
            public TestSubscriberAlpha()
            {
            }

            public static int WhenAsyncOrderCreatedCalls { get; private set; }
            public static int WhenAsyncOrderItemCreatedCalls { get; private set; }

            public Task HandleAsync(OrderCreatedEvent @event, CancellationToken cancellationToken)
            {
                WhenAsyncOrderCreatedCalls++;
                return Task.CompletedTask;
            }

            public Task HandleAsync(OrderItemCreatedEvent @event, CancellationToken cancellationToken)
            {
                WhenAsyncOrderItemCreatedCalls++;
                return Task.CompletedTask;
            }
        }

        [SuppressMessage("Design", "CA1034:Nested types should not be visible")]
        public class TestSubscriberBeta : ISubscribe<OrderItemCreatedEvent>
        {
            public TestSubscriberBeta()
            {
            }

            public static int WhenAsyncCalls { get; private set; }

            public Task HandleAsync(OrderItemCreatedEvent @event, CancellationToken cancellationToken)
            {
                WhenAsyncCalls++;
                return Task.CompletedTask;
            }
        }

        private readonly Order _order;
        private readonly ServiceProvider _serviceProvider;
        private readonly OrderItem _orderItem;

        private readonly EventSourcingRepository _eventSourcingRepository;

        private readonly Guid _identifier;

        private readonly string _defaultPrefix = "prefix";
        private readonly string _defaultCode = "code";

        private EventPublishingTable _eventPublishingTable = new ();

        private DomainEventProjectionDispatcher _eventWorker1;
        private DomainEventProjectionDispatcher _eventWorker2;

		public EventSourcingRepositoryWithEventPublishingTableShould()
        {
            _identifier = Guid.NewGuid();

            //This path is used to save in memory storage
            var strTempDataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data_" + _identifier);

            if (!Directory.Exists(strTempDataFolderPath))
            {
                Directory.CreateDirectory(strTempDataFolderPath);
            }

            var inMemoryEventStorePath = $@"{strTempDataFolderPath}/events.stream.dump";
            var inMemorySnapshotStorePath = $@"{strTempDataFolderPath}/events.snapshot.dump";

            var orderId = Guid.NewGuid();
            const string title = "Title A";
            const string comment = "Comment A";
            const OrderState orderState = OrderState.Deactivated;

            _order = new Order(orderId.ToString(), title, comment, orderState);
            _order.SetOptionalCertificate(_defaultPrefix, _defaultCode);

            var itemId = Guid.NewGuid();
            const string itemName = "Item Name A";
            const string itemDescription = "Item Description A";

            _orderItem = new OrderItem(1, itemId.ToString(), itemName, itemDescription);

            _order.ReferenceOrderItem(_orderItem);

            var eventCodec = DDD.BuildingBlocks.Tests.Abstracts.Event.TestEventCodec.Create();
            _eventSourcingRepository = new EventSourcingRepository(
                new PublishingEventStoreProviderDecorator(
                    new InMemoryEventStoreProvider(), eventCodec, _eventPublishingTable),
                eventCodec,
                new InMemorySnapshotStoreProvider(5),
                DDD.BuildingBlocks.Tests.Abstracts.Snapshot.TestSnapshotCodec.Create());

            var services = new ServiceCollection();
            services.AddDddBuildingBlocksDispatching(typeof(TestSubscriberAlpha).Assembly);
            _serviceProvider = services.BuildServiceProvider();
            var domainEventNotifier = _serviceProvider.GetRequiredService<IDomainEventNotifier>();

            _eventWorker1 = new DomainEventProjectionDispatcher(
                new InProcessDomainEventHandler(domainEventNotifier), _eventPublishingTable, "worker1");

            _eventWorker2 = new DomainEventProjectionDispatcher(
                new InProcessDomainEventHandler(domainEventNotifier), _eventPublishingTable, "worker2");

            _eventPublishingTable.RegisterWorkerId("worker1");
            _eventPublishingTable.RegisterWorkerId("worker2");
        }

        public void Dispose()
        {
            _serviceProvider.Dispose();
            var strTempDataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data_" + _identifier);
            var inMemoryEventStorePath = $@"{strTempDataFolderPath}/events.stream.dump";
            var inMemorySnapshotStorePath = $@"{strTempDataFolderPath}/events.snapshot.dump";

            File.Delete(inMemoryEventStorePath);
            File.Delete(inMemorySnapshotStorePath);
            Directory.Delete(strTempDataFolderPath, true);
        }

        [Fact(DisplayName = "Work properly together to publish saved events")]
        [Trait("Category", "Integrationtest")]
		public async Task Work_properly_together_to_publish_saved_events()
        {
			// Act
            await _eventSourcingRepository.SaveAsync(_order, System.Threading.CancellationToken.None);
            await _eventSourcingRepository.SaveAsync(_orderItem, System.Threading.CancellationToken.None);

            _eventPublishingTable.WorkerQueues["worker1"]
                .Count.Should()
                .Be(4);

			// Assert
            _order.GetUncommittedChanges().Should().HaveCount(0);
            _orderItem.GetUncommittedChanges().Should().HaveCount(0);

            await _eventWorker1.ProcessAsync();

            TestSubscriberAlpha.WhenAsyncOrderItemCreatedCalls.Should()
                .BeGreaterThan(0);

            _eventPublishingTable.WorkerQueues["worker1"]
                .Count.Should()
                .Be(0);

            _eventPublishingTable.WorkerQueues["worker2"]
                .Count.Should()
                .Be(4);

            await _eventWorker2.ProcessAsync();

            _eventPublishingTable.WorkerQueues["worker2"]
                .Count.Should()
                .Be(0);
        }
    }
}
