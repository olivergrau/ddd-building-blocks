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

            _eventSourcingRepository = new EventSourcingRepository(new PureInMemoryEventStorageProvider(),
                new InMemorySnapshotStorageProvider(5, inMemorySnapshotStorePath));
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

            await _eventSourcingRepository.SaveAsync(order);
            order.CloseOrder();
            await _eventSourcingRepository.SaveAsync(order);

			// Act + Assert
			var reloadedOrder = await _eventSourcingRepository.GetByIdAsync<Order, OrderId>(order.Id);

            reloadedOrder.Should()
                .NotBeNull();

            Action functor = () => reloadedOrder!.ChangeTitle("Should not work");
            functor.Should().Throw<AggregateException>();

            reloadedOrder!.GetStreamState().Should().Be(StreamState.StreamClosed);
            reloadedOrder.HasUncommittedChanges().Should().BeFalse();
        }

        [Fact(DisplayName = "Allow saving of aggregates with unique constraints based values multiple times")]
        [Trait("Category", "Integrationtest")]
		public void Allow_saving_of_aggregates_with_unique_constraints_based_values_multiple_times()
        {
            // Arrange
            var (order, _) = PrepareTwoAggregates(
                GetUniqueString("Order"), GetUniqueString("Item"));

            var uniquePrefix = GetUniqueString("UniquePrefix");

            order.SetOptionalCertificate(uniquePrefix, "Fixed");

            Func<Task> functor = async () => await _eventSourcingRepository.SaveAsync(order);

            // Act + Assert
            functor.Should().NotThrowAsync("Because it saves the first time with that certificate");
            functor.Should().NotThrowAsync("Because the the same object saves the same value");
        }

        [Fact(DisplayName = "Not allow saving two aggregates with the same value of a unique constraint based property")]
        [Trait("Category", "Integrationtest")]
		public void Not_allow_saving_two_aggregates_with_the_same_value_of_a_unique_constraint_based_property()
        {
            // Arrange
            var (order1, _) = PrepareTwoAggregates(
                GetUniqueString("Order1"), GetUniqueString("Item1.1"));

            var (order2, _) = PrepareTwoAggregates(
                GetUniqueString("Order2"), GetUniqueString("Item2.1"));

            var uniquePrefix = GetUniqueString("UniquePrefix");

            order1.SetOptionalCertificate(uniquePrefix, "Fixed");
            order2.SetOptionalCertificate(uniquePrefix, "Fixed");

            Func<Task> functor1 = async () => await _eventSourcingRepository.SaveAsync(order1);
            Func<Task> functor2 = async () => await _eventSourcingRepository.SaveAsync(order2);

            // Act + Assert
            functor1.Should().NotThrowAsync("Because it saves the first time with that certificate");
            functor2.Should().ThrowAsync<ProviderException>("Because the second aggregate wants to save a certificate which already exists.");
        }

        [Fact(DisplayName = "Save a single aggregate and that leads to zero uncommitted changes")]
        [Trait("Category", "Integrationtest")]
		public async Task Save_a_single_aggregate_and_that_leads_to_zero_uncommitted_changes()
        {
			// Act
            await _eventSourcingRepository.SaveAsync(_target);

			// Assert
            _target.GetUncommittedChanges().Should().HaveCount(0);
        }

        [Fact(DisplayName = "Save and load an aggregate correctly")]
        [Trait("Category", "Integrationtest")]
		public async Task Save_and_load_an_aggregate_correctly()
        {
			// Act
            await _eventSourcingRepository.SaveAsync(_target);
            var target = await _eventSourcingRepository.GetByIdAsync<Order, OrderId>(_target.Id);

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

                await _eventSourcingRepository.SaveAsync(_target);

                var reloadedOrder = await _eventSourcingRepository.GetByIdAsync<Order, OrderId>(_target.Id);
                AssertOrder(reloadedOrder!, i);
            }
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

            Func<Task> functor1 = async () => await _eventSourcingRepository.SaveAsync(order1);
            Func<Task> functor2 = async () => await _eventSourcingRepository.SaveAsync(order2);

            // Act + Assert
            await functor1.Should().NotThrowAsync("Because it saves the first time with that certificate");

            // now deactivate the aggregate
            order1.CloseOrder();

            await functor1.Should().NotThrowAsync("The aggregate should be deactivated");
            await functor2.Should().NotThrowAsync<ProviderException>();
        }

		[Fact(DisplayName = "Not allow saving two aggregates with same unique property")]
		[Trait("Category", "Integrationtest")]
		public async Task Not_allow_saving_two_aggregates_with_same_unique_property()
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

			Func<Task> functor1 = async () => await _eventSourcingRepository.SaveAsync(order1);
			Func<Task> functor2 = async () => await _eventSourcingRepository.SaveAsync(order2);

			// Act + Assert
			await functor1.Should().NotThrowAsync("Because it saves the first time with that certificate");
			await functor2.Should().ThrowAsync<ProviderException>();
			var reloadedOrder = await _eventSourcingRepository.GetByIdAsync<Order, OrderId>(new OrderId(orderId2.ToString()));
			reloadedOrder.Should().BeNull();
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
