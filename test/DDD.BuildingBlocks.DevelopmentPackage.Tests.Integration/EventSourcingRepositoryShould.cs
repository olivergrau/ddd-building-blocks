namespace DDD.BuildingBlocks.DevelopmentPackage.Tests.Integration
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Core.Persistence.Repository;
    using Storage;
    using DDD.BuildingBlocks.Tests.Abstracts.Model;
    using Xunit;

    public sealed class EventSourcingRepositoryShould : IDisposable
    {
        private readonly Order _order;
        private readonly OrderItem _orderItem;

        private readonly EventSourcingRepository _eventSourcingRepository;

        private readonly Guid _identifier;

        private readonly string _defaultPrefix = "prefix";
        private readonly string _defaultCode = "code";

		public EventSourcingRepositoryShould()
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

            _eventSourcingRepository = new EventSourcingRepository(new PureInMemoryEventStorageProvider(),
                new InMemorySnapshotStorageProvider(5, inMemorySnapshotStorePath));
        }

        public void Dispose()
        {
            var strTempDataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"App_Data_" + _identifier);
            var inMemoryEventStorePath = $@"{strTempDataFolderPath}/events.stream.dump";
            var inMemorySnapshotStorePath = $@"{strTempDataFolderPath}/events.snapshot.dump";

            File.Delete(inMemoryEventStorePath);
            File.Delete(inMemorySnapshotStorePath);
            Directory.Delete(strTempDataFolderPath, true);
        }

        [Fact(DisplayName = "Reset UncommittedChanges in aggregates to be saved")]
        [Trait("Category", "Integrationtest")]
		public async Task Reset_UncommittedChanges_in_aggregates_to_be_saved()
        {
			// Act
            await _eventSourcingRepository.SaveAsync(_order);
            await _eventSourcingRepository.SaveAsync(_orderItem);

			// Assert
            _order.GetUncommittedChanges().Should().HaveCount(0);
            _orderItem.GetUncommittedChanges().Should().HaveCount(0);
        }

        [Fact(DisplayName = "Save_and reload an aggregate correctly")]
        [Trait("Category", "Integrationtest")]
		public async Task Save_and_reload_an_aggregate_correctly()
        {
			// Act
            await _eventSourcingRepository.SaveAsync(_order);
            await _eventSourcingRepository.SaveAsync(_orderItem);

            var order = await _eventSourcingRepository.GetByIdAsync<Order, OrderId>(_order.Id);
            var orderItem = await _eventSourcingRepository.GetByIdAsync<OrderItem, OrderItemId>(_orderItem.Id);

			// Assert
			order.Should().NotBeNull();
			order!.Id.Should().Be(_order.Id);
            order.Title.Should().Be(_order.Title);
            order.Comment.Should().Be(_order.Comment);
            order.OptionalCertificate.Should().Be(new Certificate(_defaultPrefix, _defaultCode));
            order.OrderItems.Should().HaveCount(1);
            order.OrderItems.First().AggregateId.Should().Be(_orderItem.Id.ToString());

            orderItem.Should().NotBeNull();
            orderItem!.Id.Should().Be(_orderItem.Id);
            orderItem.Name.Should().Be(_orderItem.Name);
            orderItem.Description.Should().Be(_orderItem.Description);
        }
    }
}
