#pragma warning disable CS8602

namespace DDD.BuildingBlocks.DevelopmentPackage.Tests.Integration
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Core.Domain;
    using Core.Exception;
    using Core.Persistence.Repository;
    using Storage;
    using DDD.BuildingBlocks.Tests.Abstracts.Model;
    using Xunit;
    using AggregateException = Core.Exception.AggregateException;

    /// <summary>
    ///     Tests the integration of the EventSourcingRepository and the InMemoryProvider Implementations.
    /// </summary>
    public sealed class AnotherEventSourcingRepositoryShould : IDisposable
    {
        private readonly Order _target;
        private readonly EventSourcingRepository _eventSourcingRepository;

        private readonly Guid _identifier;
        private readonly string _defaultPrefix = "prefix";
        private readonly string _defaultCode = "code";


		public AnotherEventSourcingRepositoryShould()
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

            _target = new Order(orderId.ToString(), title, comment, orderState);
            _target.SetOptionalCertificate("prefix", "code");

            _eventSourcingRepository = new EventSourcingRepository(
                new InMemoryEventStoreProvider(),
                DDD.BuildingBlocks.Tests.Abstracts.Event.TestEventCodec.Create(),
                new InMemorySnapshotStoreProvider(5),
                DDD.BuildingBlocks.Tests.Abstracts.Snapshot.TestSnapshotCodec.Create());
        }

        public void Dispose()
        {
            var strTempDataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"App_Data_" + _identifier);
            var inMemoryEventStorePath = $@"{strTempDataFolderPath}/events.stream.dump";
            var inMemorySnapshotStorePath = $@"{strTempDataFolderPath}/events.snapshot.dump";

            File.Delete(inMemoryEventStorePath);
            File.Delete(inMemorySnapshotStorePath);
            File.Delete(inMemorySnapshotStorePath + "._unique");
            File.Delete(inMemoryEventStorePath + "._unique");
            File.Delete(inMemoryEventStorePath + "._mappings");
            Directory.Delete(strTempDataFolderPath);
        }

        [Fact(DisplayName = "Not allow changes to an deactivated aggregate")]
        [Trait("Category", "Integrationtest")]
        public async Task Not_allow_changes_to_an_deactivated_aggregate()
        {
            // Arrange
            var (order, _) = PrepareTwoAggregates(
                GetUniqueString("Order"), GetUniqueString("Item"));

            await _eventSourcingRepository.SaveAsync(order, System.Threading.CancellationToken.None);
            order.CloseOrder();
            await _eventSourcingRepository.SaveAsync(order, System.Threading.CancellationToken.None);

			// Act + Assert
			var reloadedOrder = await _eventSourcingRepository.GetByIdAsync<Order, OrderId>(order.Id, System.Threading.CancellationToken.None);

            reloadedOrder.Should()
                .NotBeNull();

            Action functor = () => reloadedOrder!.ChangeTitle("Should not work");
            functor.Should().Throw<AggregateException>();

            reloadedOrder!.GetStreamState().Should().Be(StreamState.StreamClosed);
            reloadedOrder.HasUncommittedChanges().Should().BeFalse();
        }

        [Fact(DisplayName = "Allow saving the same aggregate multiple times")]
        [Trait("Category", "Integrationtest")]
		public void Allow_saving_the_same_aggregate_multiple_times()
        {
            // Arrange
            var (order, _) = PrepareTwoAggregates(
                GetUniqueString("Order"), GetUniqueString("Item"));

            var uniquePrefix = GetUniqueString("UniquePrefix");

            order.SetOptionalCertificate(uniquePrefix, "Fixed");

            Func<Task> functor = async () => await _eventSourcingRepository.SaveAsync(order, System.Threading.CancellationToken.None);

            // Act + Assert
            functor.Should().NotThrowAsync("Because it saves the first time with that certificate");
            functor.Should().NotThrowAsync("Because the the same object saves the same value");
        }

        [Fact(DisplayName = "Save a single aggregate and that leads to zero uncommitted changes")]
        [Trait("Category", "Integrationtest")]
		public async Task Save_a_single_aggregate_and_that_leads_to_zero_uncommitted_changes()
        {
			// Act
            await _eventSourcingRepository.SaveAsync(_target, System.Threading.CancellationToken.None);

			// Assert
            _target.GetUncommittedChanges().Should().HaveCount(0);
        }

        [Fact(DisplayName = "Save and load an aggregate correctly")]
        [Trait("Category", "Integrationtest")]
		public async Task Save_and_load_an_aggregate_correctly()
        {
			// Act
            await _eventSourcingRepository.SaveAsync(_target, System.Threading.CancellationToken.None);
            var target = await _eventSourcingRepository.GetByIdAsync<Order, OrderId>(_target.Id, System.Threading.CancellationToken.None);

			// Assert
            target.Should().NotBeNull();
            target.Id.Should().Be(_target.Id);
            target.Title.Should().Be(_target.Title);
            target.Comment.Should().Be(_target.Comment);
            target.OptionalCertificate.Should().Be(new Certificate(_defaultPrefix, _defaultCode));
        }

        [Fact(DisplayName = "Save an aggregate and reload it multiple times with DomainRelations")]
        [Trait("Category", "Integrationtest")]
		public async Task Save_an_aggregate_and_reload_it_multiple_times_with_DomainRelations()
        {
            for (var i = 0; i < 10; i++)
            {
                var itemId = Guid.NewGuid();
                var itemName = $"ItemName A{i}";
                var itemDescription = $"ItemDescription A{i}";

                var orderItem = new OrderItem(i+1, itemId.ToString(), itemName, itemDescription);

                _target.ReferenceOrderItem(orderItem);

                await _eventSourcingRepository.SaveAsync(_target, System.Threading.CancellationToken.None);

                var reloadedOrder = await _eventSourcingRepository.GetByIdAsync<Order, OrderId>(_target.Id, System.Threading.CancellationToken.None);
                AssertOrder(reloadedOrder!, i);
            }
        }

		private void AssertOrder(Order order, int iteration)
		{
			order.Should().NotBeNull();
			order.Id.Should().Be(_target.Id);
			order.Title.Should().Be(_target.Title);
			order.Comment.Should().Be(_target.Comment);
			order.OrderItems.Should().HaveCount(iteration + 1);
		}

		private static Tuple<Order, OrderItem> PrepareTwoAggregates(string orderTitle, string orderItemName)
		{
			var orderId = Guid.NewGuid();
			const string comment = "Comment A";
			const OrderState orderState = OrderState.Deactivated;

			var order = new Order(orderId.ToString(), orderTitle, comment, orderState);
			order.SetOptionalCertificate(GetUniqueString("UniquePrefix"), "Code");

			var orderItemId = Guid.NewGuid();
			const string orderItemDescription = "Item Description A";

			var orderItem = new OrderItem(1, orderItemId.ToString(), orderItemName, orderItemDescription);
			order.ReferenceOrderItem(orderItem);

			return new Tuple<Order, OrderItem>(order, orderItem);
		}

		private static string GetUniqueString(string prefix)
		{
			return prefix + "][" + Guid.NewGuid();
		}
	}
}
