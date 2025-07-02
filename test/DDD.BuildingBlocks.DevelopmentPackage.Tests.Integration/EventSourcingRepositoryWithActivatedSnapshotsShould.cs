#pragma warning disable CS8602

namespace DDD.BuildingBlocks.DevelopmentPackage.Tests.Integration
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Core.Persistence.Repository;
    using Storage;
    using DDD.BuildingBlocks.Tests.Abstracts.Model;
    using Xunit;

    [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly")]
    public sealed class EventSourcingRepositoryWithActivatedSnapshotsShould : IDisposable
	{
		private readonly string _orderTitle = "Order Title ";
		private readonly string _orderComment = "Great Order";
		private readonly OrderState _orderState = OrderState.Deactivated;

		private readonly string _orderItemDescription = "---Item Desc---";
		private readonly string _orderItemName = "OrderItem Name#";
		private readonly decimal _orderItemSellingPrice = (decimal) 149.92;

		private readonly string _eventStorageFile = "events";
		private readonly string _snapshotFile = "snapshots";

        private Guid _identifier;

        public EventSourcingRepositoryWithActivatedSnapshotsShould()
        {
            _identifier = Guid.NewGuid();
            var strTempDataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data_" + _identifier);

            if (!Directory.Exists(strTempDataFolderPath))
            {
                Directory.CreateDirectory(strTempDataFolderPath);
            }

            _eventStorageFile = $@"{strTempDataFolderPath}/events.stream.dump";
            _snapshotFile = $@"{strTempDataFolderPath}/events.snapshot.dump";
        }
		[Theory(DisplayName = "Load an aggregate correctly with usage of snapshots")]
		[InlineData(113, 17, 19)]
		[InlineData(40, 10, 10)]
		[InlineData(5, 5, 5)]
		[InlineData(1, 1, 1)]
		[Trait("Category", "Integrationtest")]
		public async Task Load_an_aggregate_correctly_with_usage_of_snapshots(
			int numIterations, int snapshotFrequency, int saveFrequency)
		{
			// Arrange
			var newComment = "New Comment";
			var orderId = Guid.NewGuid();
			var order = new Order(orderId.ToString(), _orderTitle, _orderComment, _orderState);
			DeleteDumpFiles();

			// Act
			order.ChangeTitle(_orderTitle);

			var repository = GetRepositoryWithInMemorySnapshotProvider(snapshotFrequency);
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

		[Theory(DisplayName = "Load multiple aggregates correctly with usage of snapshots")]
		[InlineData(167, 17, 11, 13)]
		[InlineData(20, 10, 10, 10)]
		[InlineData(40, 5, 10, 20)]
		[InlineData(5, 5, 5, 5)]
		[InlineData(52, 5, 10, 15)]
		[InlineData(1, 1, 1, 1)]
		[Trait("Category", "Integrationtest")]
		public async Task Load_multiple_aggregates_correctly_with_usage_of_snapshots(
			int numIterations, int snapshotFrequency, int saveFrequencyOrder, int saveFrequencyVariant)
		{
			// Arrange
			var newName = "New Comment";
			var newTitle = "New Title";
			var orderId = Guid.NewGuid();
			var order = new Order(orderId.ToString(), _orderTitle, _orderComment, _orderState);
			var orderItem = new OrderItem(1, orderId.ToString(), _orderItemName, _orderItemDescription);
			DeleteDumpFiles();

			// Act
			orderItem.SetBuyingPrice(_orderItemSellingPrice);
			orderItem.SetActive();
			order.ReferenceOrderItem(orderItem);
			order.ChangeComment(_orderComment);

			var repository = GetRepositoryWithInMemorySnapshotProvider(snapshotFrequency);
			for (var i = 1; i <= numIterations; i++)
			{
				order.ChangeTitle(newTitle + i);
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
			reloadedOrder.Title.Should().Be(newTitle + numIterations);
			reloadedOrder.Comment.Should().Be(_orderComment);
			reloadedOrder.State.Should().Be(_orderState);
			reloadedOrder.OrderItems.Should().HaveCount(1);
			var reloadedOrderItemId = reloadedOrder.OrderItems.First().AggregateId;
			reloadedOrderItemId.Should().Be(new OrderItemId(1, orderId.ToString()).ToString());

			reloadedOrderItem.Should().NotBeNull();
			reloadedOrderItem.Description.Should().Be(_orderItemDescription);
			reloadedOrderItem.Name.Should().Be(newName + numIterations);
			reloadedOrderItem.SellingPrice.Should().Be(_orderItemSellingPrice);
		}

        public void Dispose()
        {
            DeleteDumpFiles();

            var strTempDataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"App_Data_" + _identifier);

            if(Directory.Exists(strTempDataFolderPath))
            {
                Directory.Delete(strTempDataFolderPath, true);
            }
        }

		private void DeleteDumpFiles()
		{
            var strTempDataFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"App_Data_" + _identifier);
            var inMemoryEventStorePath = $@"{strTempDataFolderPath}/events.stream.dump";
            var inMemorySnapshotStorePath = $@"{strTempDataFolderPath}/events.snapshot.dump";

            if(File.Exists(inMemoryEventStorePath))
            {
                File.Delete(inMemoryEventStorePath);
            }

            if(File.Exists(inMemorySnapshotStorePath))
            {
                File.Delete(inMemorySnapshotStorePath);
            }
        }

		private EventSourcingRepository GetRepositoryWithInMemorySnapshotProvider(int snapshotFrequency)
		{
			return new EventSourcingRepository(
				new PureInMemoryEventStorageProvider(),
				new InMemorySnapshotStorageProvider(snapshotFrequency, _snapshotFile));
		}
	}
}
