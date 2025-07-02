#pragma warning disable CS8602

namespace DDD.BuildingBlocks.Core.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using Domain;
    using Event;
    using Exception;
    using Util;
    using DDD.BuildingBlocks.Tests.Abstracts.Event;
    using DDD.BuildingBlocks.Tests.Abstracts.Model;
    using Xunit;
    using AggregateException = Exception.AggregateException;

    public class AggregateRootShould
	{
		private readonly Order _defaultAggregateRoot;
		private readonly string _defaultTitle = "Title A";
		private readonly string _defaultDescription = "Desc A";
		private readonly string _defaultComment = "Comment A";
		private readonly OrderState _defaultTargetState = OrderState.Deactivated;

		public AggregateRootShould()
		{
			_defaultAggregateRoot = new Order(Guid.NewGuid().ToString(), _defaultTitle, _defaultDescription, _defaultTargetState);
		}

        [Fact(DisplayName = "Be created and a property update after its deactivation must throw an AggregateException and leaves properties unchanged")]
        [Trait("Category", "Unittest")]
        public void Be_created_and_a_property_update_after_deactivation_throws_AggregateException_and_leaves_properties_unchanged()
        {
            // Arrange
            var targetId = Guid.NewGuid();
            var target = new Order(targetId.ToString(), _defaultTitle, _defaultComment, _defaultTargetState);

            // Act
            target.CloseOrder();
            Action functor = () => target.ChangeTitle("Should not work");

            // Assert
            target.GetStreamState().Should().Be(StreamState.StreamClosed);
            functor.Should().Throw<AggregateException>();

            target.Title.Should().Be(_defaultTitle);
        }

        [Fact(DisplayName = "Not be created with empty guid")]
		[Trait("Category", "Unittest")]
		public void Not_be_created_with_empty_guid()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				// ReSharper disable once UnusedVariable
				var _ = new Order(string.Empty, _defaultTitle, _defaultComment, OrderState.Open);
			});
		}

		[Fact(DisplayName = "Not be created with empty name")]
		[Trait("Category", "Unittest")]
		public void Not_be_created_with_empty_name()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				// ReSharper disable once UnusedVariable
				var _ = new Order(Guid.NewGuid().ToString(), string.Empty, _defaultComment, OrderState.Open);
			});
		}

		[Fact(DisplayName = "Not be created with empty description")]
		[Trait("Category", "Unittest")]
		public void Not_be_created_with_empty_description()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				// ReSharper disable once UnusedVariable
				var _ = new Order(Guid.NewGuid().ToString(), _defaultTitle, string.Empty, OrderState.Open);
			});
		}

		[Fact(DisplayName = "Populate its properties correctly")]
		[Trait("Category", "Unittest")]
		public void Populate_its_properties_correctly()
		{
			// Arrange
			var targetId = Guid.NewGuid();

			// Act
			var target = new Order(targetId.ToString(), _defaultTitle, _defaultComment, _defaultTargetState);

			// Assert
			target.Id.Should().Be(new OrderId(targetId.ToString()));
			target.Title.Should().Be(_defaultTitle);
			target.Comment.Should().Be(_defaultComment);
			target.State.Should().Be(_defaultTargetState);
		}

		[Fact(DisplayName = "Should produce one uncommitted change for one property update")]
		[Trait("Category", "Unittest")]
		public void Produce_one_uncommitted_change_for_one_property_update()
		{
			// Arrange
			var targetId = Guid.NewGuid();

			// Act
			var target = new Order(targetId.ToString(), _defaultTitle, _defaultComment, _defaultTargetState);

			// Assert

			// StreamState is -1 if the aggregate is totally fresh (not even an InventoryCreatedEvent has been applied)
			target.GetStreamState().Should().Be(StreamState.HasStream);

			// Should contain one element: the OrderCreatedEvent
			target.GetUncommittedChanges().Count().Should().Be(1);
			var targetCreationEvent = target.GetUncommittedChanges().First();
			targetCreationEvent.Should().BeOfType<OrderCreatedEvent>();
		}

		[Fact(DisplayName = "Correctly set after a new ValueObject is passed for a property change")]
		[Trait("Category", "Unittest")]
		public void Be_correctly_set_after_a_new_ValueObject_is_passed_for_a_property_change()
		{
			// Arrange
			var prefix = "prefix";
			var code = "code";

			// Act
			_defaultAggregateRoot.SetOptionalCertificate(prefix, code);

			// Assert
			// reminder: Label is of type value object, so it must behave like a primitive value type.
			_defaultAggregateRoot.OptionalCertificate.Should().Be(new Certificate(prefix, code));
			_defaultAggregateRoot.OptionalCertificate.Code.Should().Be(code);
			_defaultAggregateRoot.OptionalCertificate.Prefix.Should().Be(prefix);
		}

		[Fact(DisplayName = "Be created and calling one business method should result in two uncommitted changes")]
		[Trait("Category", "Unittest")]
		public void Be_created_and_calling_one_business_method_should_result_in_two_uncommitted_changes()
		{
			// Arrange
			var prefix = "prefix";
			var code = "code";

			// Act
			_defaultAggregateRoot.SetOptionalCertificate(prefix, code);

			// Assert
			_defaultAggregateRoot.GetStreamState().Should().Be(StreamState.HasStream);
			_defaultAggregateRoot.GetUncommittedChanges().Should().HaveCount(2);

			var changes = _defaultAggregateRoot.GetUncommittedChanges();

			var domainEvents = changes as IDomainEvent[] ?? changes.ToArray();
			domainEvents.Should().NotBeEmpty();
			domainEvents.First().Should().BeOfType<OrderCreatedEvent>();
			domainEvents.Last().Should().BeOfType<OrderCertificateChangedEvent>();
		}


		[Fact(DisplayName = "Be created and its title should be correctly set")]
		[Trait("Category", "Unittest")]
		public void Be_created_and_its_title_should_be_correctly_set()
		{
			// Arrange
			var newTitle = "newTitle";

			// Act
			_defaultAggregateRoot.ChangeTitle(newTitle);

			// Assert
			_defaultAggregateRoot.Title.Should().Be(newTitle);
		}

		[Fact(DisplayName = "Contain two uncommitted changes after one property has been changed")]
		[Trait("Category", "Unittest")]
		public void Contain_two_uncommitted_changes_after_one_property_has_been_changed()
		{
			// Arrange
			var newTitle = "newTitle";

			// Act
			_defaultAggregateRoot.ChangeTitle(newTitle);

			// Assert
			_defaultAggregateRoot.GetStreamState().Should().Be(StreamState.HasStream);
			_defaultAggregateRoot.GetUncommittedChanges().Should().HaveCount(2);

			var changes = _defaultAggregateRoot.GetUncommittedChanges();
			var domainEvents = changes as IDomainEvent[] ?? changes.ToArray();
			domainEvents.Should().NotBeEmpty();
			domainEvents.First().Should().BeOfType<OrderCreatedEvent>();
			domainEvents.Last().Should().BeOfType<OrderTitleChangedEvent>();
		}

		[Fact(DisplayName = "Not be updated if a property is updated with an already set value")]
		[Trait("Category", "Unittest")]
		public void Not_be_updated_if_a_property_is_updated_with_an_already_set_value()
		{
			// Act
			_defaultAggregateRoot.ChangeTitle(_defaultAggregateRoot.Title);

			// Assert
			_defaultAggregateRoot.Title.Should().Be(_defaultAggregateRoot.Title);

            // only the creation event is stored in the uncommitted changes
            _defaultAggregateRoot.GetUncommittedChanges()
                .Should()
                .HaveCount(1);

        }

		[Fact(DisplayName = "Store a DomainRelation only if another AggregateRoot is passed as a reference")]
		[Trait("Category", "Unittest")]
		public void Store_a_domainRelation_only_if_another_aggregateRoot_is_passed_as_a_reference()
		{
			// Arrange
			var itemId = Guid.NewGuid();
			var itemName = "ItemName A";
			var itemDescription = "ItemDescription A";

			// Act
			var item = new OrderItem(1, itemId.ToString(), itemName, itemDescription);

			// <remarks>
			//  Attention here: OrderItem is of type aggregate and we add a domain relation to the order item aggregate.
			//  It is not the responsibility of the order item aggregate to save the order item aggregate.
			//  The order item aggregate saves only a weak domain relation in form of the id.
			// </remarks>
			_defaultAggregateRoot.ReferenceOrderItem(item);

			// Assert
			_defaultAggregateRoot.OrderItems.Should().HaveCount(1);
			_defaultAggregateRoot.OrderItems.First().AggregateId.Should().Be(item.Id.ToString());
		}

		[Fact(DisplayName = "Contain two uncommitted changes after adding another aggregateRoot instance")]
		[Trait("Category", "Unittest")]
		public void Contain_two_uncommitted_changes_after_adding_another_aggregateRoot_instance()
		{
			// Arrange
			var itemId = Guid.NewGuid();
			var itemName = "ItemName A";
			var itemDescription = "ItemDescription A";

			// Act
			var item = new OrderItem(1, itemId.ToString(), itemName, itemDescription);
			_defaultAggregateRoot.ReferenceOrderItem(item);

			// Assert
			_defaultAggregateRoot.GetStreamState().Should().Be(StreamState.HasStream);
			_defaultAggregateRoot.GetUncommittedChanges().Should().HaveCount(2);

			var changes = _defaultAggregateRoot.GetUncommittedChanges();

			var domainEvents = changes as IDomainEvent[] ?? changes.ToArray();
			domainEvents.Should().NotBeEmpty();
			domainEvents.First().Should().BeOfType<OrderCreatedEvent>();
			domainEvents.Last().Should().BeOfType<OrderItemAddedToOrderEvent>();
		}

		[Fact(DisplayName = "Not get the same external aggregateRoot reference twice")]
		[Trait("Category", "Unittest")]
		public void Not_get_the_same_external_aggregateRoot_reference_twice()
		{
			// Arrange
			var itemId = Guid.NewGuid();
			var itemName = "ItemName A";
			var itemDescription = "ItemDescription A";

			// Act + Assert
			var item = new OrderItem(1, itemId.ToString(), itemName, itemDescription);
			_defaultAggregateRoot.ReferenceOrderItem(item);

			Assert.Throws<AggregateException>(() =>
			{
				_defaultAggregateRoot.ReferenceOrderItem(item);
			});
		}

		[Fact(DisplayName = "Hold n DomainRelations after n AggregateRoot references are added")]
		[Trait("Category", "Unittest")]
		public void Hold_n_domainRelations_after_n_aggregateRoot_references_are_added()
		{
			// Arrange
			var N = 10;
			var items = new List<OrderItem>();

			for (var i = 0; i < N; i++)
			{
				var itemId = Guid.NewGuid();
				var itemName = $"ItemName {i}";
				var itemDescription = $"ItemDescription {i}";

				items.Add(new OrderItem(i+1, itemId.ToString(), itemName, itemDescription));
			}

			// Act
			foreach (var item in items)
			{
				_defaultAggregateRoot.ReferenceOrderItem(item);
			}

			// Assert
			_defaultAggregateRoot.OrderItems.Should().HaveCount(N);
		}

		[Fact(DisplayName = "Have N plus 1 uncommitted changes after N changes")]
		[Trait("Category", "Unittest")]
		public void Have_N_plus_1_uncommitted_changes_after_N_changes()
		{
			// Arrange
			var N = 10;
			var items = new List<OrderItem>();

			for (var i = 0; i < N; i++)
			{
				var itemId = Guid.NewGuid();
				var itemName = $"ItemName {i}";
				var itemDescription = $"ItemDescription {i}";

				items.Add(new OrderItem(i+1, itemId.ToString(), itemName, itemDescription));
			}

			// Act
			foreach (var item in items)
			{
				_defaultAggregateRoot.ReferenceOrderItem(item);
			}

			// Assert
			_defaultAggregateRoot.GetStreamState().Should().Be(StreamState.HasStream);
			_defaultAggregateRoot.GetUncommittedChanges().Should().HaveCount(N + 1);

			var changes = _defaultAggregateRoot.GetUncommittedChanges();

			var domainEvents = changes as IDomainEvent[] ?? changes.ToArray();
			domainEvents.Should().NotBeEmpty();
			domainEvents.First().Should().BeOfType<OrderCreatedEvent>();
			domainEvents.Last().Should().BeOfType<OrderItemAddedToOrderEvent>();
		}

		[Fact(DisplayName = "Have uncommitted changes if properties are indeed changed")]
		[Trait("Category", "Unittest")]
		public void Have_uncommitted_changes_if_properties_are_indeed_changed()
		{
			// Arrange
			var aggregateRoot = new Order(Guid.NewGuid().ToString(), _defaultTitle, _defaultComment, OrderState.Open);

			// Act
			var result = aggregateRoot.HasUncommittedChanges();

			// Assert
			result.Should().BeTrue();
		}

		[Fact(DisplayName = "Not have uncommitted changes if there are no changes")]
		[Trait("Category", "Unittest")]
		public void Not_have_uncommitted_changes_if_there_are_no_changes()
		{
			// Arrange
			var aggregateRoot = new Order(Guid.NewGuid().ToString(), _defaultTitle, _defaultComment, OrderState.Open);

			// Act
			aggregateRoot.MarkChangesAsCommitted();
			var result = aggregateRoot.HasUncommittedChanges();

			// Assert
			result.Should().BeFalse();
		}

		[Fact(DisplayName = "Have uncommitted changes if there are some changes")]
		[Trait("Category", "Unittest")]
		public void Have_uncommitted_changes_if_there_are_some_changes()
		{
			// Arrange
			var aggregateRoot = new Order(Guid.NewGuid().ToString(), _defaultTitle, _defaultComment, OrderState.Open);
			aggregateRoot.ChangeTitle("NewTitle");

			// Act
			var uncommittedChanges = aggregateRoot.GetUncommittedChanges().ToList();

			// Assert
			uncommittedChanges.Should().HaveCount(2);
			uncommittedChanges.First().Should().BeOfType<OrderCreatedEvent>();
			uncommittedChanges.Last().Should().BeOfType<OrderTitleChangedEvent>();
		}

		[Fact(DisplayName = "Not have uncommitted changes if existing changes are marked as committed")]
		[Trait("Category", "Unittest")]
		public void Not_have_uncommitted_changes_if_existing_changes_are_marked_as_committed()
		{
			// Arrange
			var aggregateRoot = new Order(Guid.NewGuid().ToString(), _defaultTitle, _defaultComment, OrderState.Open);
			aggregateRoot.ChangeTitle("NewTitle");
			aggregateRoot.MarkChangesAsCommitted();

			// Act
			var uncommittedChanges = aggregateRoot.GetUncommittedChanges().ToList();

			// Assert
			uncommittedChanges.Should().BeEmpty();
		}

		[Fact(DisplayName = "Have no new uncommitted changes after a snapshot was taken")]
		[Trait("Category", "Unittest")]
		public void Have_no_new_uncommitted_changes_after_a_snapshot_was_taken()
		{
			// Arrange
			var aggregateRoot = new Order(Guid.NewGuid().ToString(), _defaultTitle, _defaultComment, OrderState.Open);
			aggregateRoot.ChangeTitle("NewTitle");
			var _ = aggregateRoot.TakeSnapshot();

			// Act
			var uncommittedChanges = aggregateRoot.GetUncommittedChanges().ToList();

			// Assert
			uncommittedChanges.Should().HaveCount(2);
			uncommittedChanges.First().Should().BeOfType<OrderCreatedEvent>();
			uncommittedChanges.Last().Should().BeOfType<OrderTitleChangedEvent>();
		}

		[Fact(DisplayName = "Not have changes in uncommitted changes when snapshot was applied")]
		[Trait("Category", "Unittest")]
		public void Not_have_changes_in_uncommitted_changes_when_snapshot_was_applied()
		{
			// Arrange
			var aggregateRoot = new Order(Guid.NewGuid().ToString(), _defaultTitle, _defaultComment, OrderState.Open);
			aggregateRoot.ChangeTitle("NewTitle");
			var snapshot = aggregateRoot.TakeSnapshot();
			aggregateRoot.ApplySnapshot(snapshot);

			// Act
			var uncommittedChanges = aggregateRoot.GetUncommittedChanges().ToList();

			// Assert
			uncommittedChanges.Should().HaveCount(2);
			uncommittedChanges.First().Should().BeOfType<OrderCreatedEvent>();
			uncommittedChanges.Last().Should().BeOfType<OrderTitleChangedEvent>();
		}

		[Fact(DisplayName = "Have correct state after event stream was applied")]
		[Trait("Category", "Unittest")]
		public void Have_correct_state_after_event_stream_was_applied()
		{
			// Arrange
			var orderId = Guid.NewGuid();
			var changedTitle = "Neuer Titel";
			var changedComment = "Neuer Kommentar";
			var eventList = CreateEventListForOrder(orderId, changedComment, changedTitle);

			// Act
			var aggregateRoot = new Order();
			aggregateRoot.ReplayEvents(eventList);

			// Assert
			aggregateRoot.Title.Should().Be(changedTitle);
			aggregateRoot.Comment.Should().Be(changedComment);
		}

		[Fact(DisplayName = "Not accept events in wrong order when event stream is applied")]
		[Trait("Category", "Unittest")]
		public void Not_accept_events_in_wrong_order_when_event_stream_is_applied()
		{
			// Arrange
			var orderId = Guid.NewGuid();
			var eventList = CreateEventListForOrder(orderId, "Neuer Titel", "Neuer Kommentar");
			eventList.Reverse(1,2);

			// Act + Assert
			var aggregateRoot = new Order();
			Action action = () => aggregateRoot.ReplayEvents(eventList);
			action.Should().Throw<AggregateStateMismatchException>();
		}

		[Fact(DisplayName = "Throw exception when no corresponding method exists for event to be applied")]
		[Trait("Category", "Unittest")]
		public void Throw_exception_when_no_corresponding_method_exists_for_event_to_be_applied()
		{
			// Arrange
			var orderId = Guid.NewGuid();
			var eventList = CreateEventListForOrder(orderId, "Neuer Titel", "Neuer Kommentar");
			var nonMatchingEvent = new OrderItemCreatedEvent(orderId.ToString(), 2, "Name", "Description", ApplicationTime.Current);
			eventList.Add(nonMatchingEvent);

			// Act + Assert
			var aggregateRoot = new Order();
			Action action = () => aggregateRoot.ReplayEvents(eventList);
			action.Should().Throw<AggregateEventOnApplyMethodMissingException>();
		}

		[Fact(DisplayName = "Return correct last committed version when events have been applied")]
		[Trait("Category", "Unittest")]
		public void Return_correct_last_committed_version_when_events_have_been_applied()
		{
			// Arrange
			var orderId = Guid.NewGuid();
			var eventList = CreateEventListForOrder(orderId, "Neuer Titel", "Neuer Kommentar");
			var aggregateRoot = new Order();
			aggregateRoot.ReplayEvents(eventList);

			// Act
			var version = aggregateRoot.LastCommittedVersion;

			// Assert
			version.Should().Be(2);
		}

        [Fact(DisplayName = "Have correct EntityId key")]
        [Trait("Category", "Unittest")]
        public void Have_correct_entityId_key()
        {
            var lookupKey = StringBasedEntityKey.GetNewId("myid");

            var aggregateRoot = new AggregateWithStringBasedKey(lookupKey);

            aggregateRoot.Id.Should().Be(lookupKey);
            aggregateRoot.Id.Should().Be(StringBasedEntityKey.GetNewId("myid"));
        }

		private static List<IDomainEvent> CreateEventListForOrder(Guid orderId, string changedComment, string changedTitle)
		{
			return
            [
                new OrderCreatedEvent(orderId.ToString(),
                    -1,
                    "Titel",
                    "Kommentar",
                    1,
                    ApplicationTime.Current),
                new OrderCommentChangedEvent(orderId.ToString(), 0, changedComment), new OrderTitleChangedEvent(orderId.ToString(), 1, changedTitle)
            ];
		}
	}
}
