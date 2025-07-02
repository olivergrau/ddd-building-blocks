using System;
using System.Collections.Generic;
using System.Linq;
using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Util;
using DDD.BuildingBlocks.Tests.Abstracts.Event;
using DDD.BuildingBlocks.Tests.Abstracts.Snapshot;
using AggregateException = DDD.BuildingBlocks.Core.Exception.AggregateException;
// ReSharper disable MemberCanBePrivate.Global
#pragma warning disable CS8618

namespace DDD.BuildingBlocks.Tests.Abstracts.Model
{
    using System.Globalization;
    using Core.Persistence.SnapshotSupport;

    public sealed class Order : AggregateRoot<OrderId>, ISnapshotEnabled
	{
		private List<DomainRelation> _orderItems = [];

		public IEnumerable<DomainRelation> OrderItems => _orderItems.AsReadOnly();

        public new IEnumerable<string> CorrelationIds => base.CorrelationIds;

		public OrderState State
		{
			get; private set;
		}

		public DateTime CreatedDate
		{
			get; private set;
		}

		public string Title
		{
			get; private set;
		}

		public string Comment
		{
			get; private set;
		}

		[UniqueDomainProperty]
		public Certificate? OptionalCertificate
		{
			get; private set;
		}

		#region "Constructor"

		public Order() : base(null!)
		{
			//Important: Aggregate roots must have a parameter-less constructor
			//to make it easier to construct from scratch.

			//The very first event in an aggregate is the creation event
			//which will be applied to an empty object created via this constructor
		}

        /// <summary>
        ///     Constructs an order aggregate
        /// </summary>
        /// <param name="orderNumber"></param>
        /// <param name="title"></param>
        /// <param name="comment"></param>
        /// <param name="state"></param>
        ///
        /// <remarks>
        ///     In this example the EntityId is constructed with the passed orderNumber parameter in the  base constructor.
        ///     It is also possible to model the orderNumber directly with the OrderId type.
        /// </remarks>
		public Order(string orderNumber, string title, string comment, OrderState state)
            : base(new OrderId(orderNumber))
		{
			if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(title));
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(comment));
            }

            // The following method are how external command interact with our aggregate
			// A command will result in following methods being executed and resulting events will be fired
			// Pattern: Create the event and call ApplyEvent(Event)
			ApplyEvent(new OrderCreatedEvent(Id.OrderNumber, CurrentVersion, title, comment, (int) state, ApplicationTime.Current));
		}

		#endregion

		#region Mutators

        public void CloseOrder()
        {
            ApplyEvent(new OrderClosedEvent(Id.OrderNumber, CurrentVersion));
        }

		public void SetOptionalCertificate(string prefix, string code)
		{
			ApplyEvent(new OrderCertificateChangedEvent(Id.OrderNumber, CurrentVersion, prefix, code));
		}

		public void Cancel()
		{
			ApplyEvent(new OrderCancelledEvent(Id.OrderNumber, CurrentVersion));
		}

		public void ChangeTitle(string newTitle)
		{
            ArgumentNullException.ThrowIfNull(newTitle);

            //Pattern: Create the event and call ApplyEvent(Event)
            if (Title != newTitle)
            {
                ApplyEvent(new OrderTitleChangedEvent(Id.OrderNumber, CurrentVersion, newTitle));
            }
        }

		public void ChangeComment(string newComment)
		{
			if (Comment != newComment)
            {
                ApplyEvent(new OrderCommentChangedEvent(Id.OrderNumber, CurrentVersion, newComment));
            }
        }

		public void ReferenceOrderItem(OrderItem item)
		{
			if (!_orderItems.Exists(q => q.AggregateId.Equals(item.Id.ToString(), StringComparison.Ordinal)))
            {
                ApplyEvent(new OrderItemAddedToOrderEvent(Id.ToString(), CurrentVersion, item.Id.ToString()));
            }
            else
            {
                throw new AggregateException(Id, "Item already added.");
            }
        }

		public void RemoveItem(OrderItem item)
		{
			if (_orderItems.Exists(q => q.AggregateId.Equals(item.Id.ToString(), StringComparison.Ordinal)))
            {
                ApplyEvent(new OrderItemRemovedFromOrderEvent(Id.ToString(), CurrentVersion, item.Id.ToString()));
            }
            else
            {
                throw new AggregateException(Id, "Item not in inventory.");
            }
        }

        #endregion

        #region "Apply Events"

        // Important
        // We mark the EventHandler method with the [InternalEventHandler] attribute.
        // This way the framework knows which method to invoke when an event happens

        [InternalEventHandler]
        internal void OnOrderClosed(OrderClosedEvent @event)
        {
            Deactivated = true;
        }

        [InternalEventHandler]
		internal void OnOrderCertificateChanged(OrderCertificateChangedEvent @event)
		{
			OptionalCertificate = new Certificate(@event.Prefix, @event.Code);
		}

		[InternalEventHandler]
		internal void OnOrderCancelled(OrderCancelledEvent @event)
		{
			State = OrderState.Deactivated;
		}

		[InternalEventHandler]
		internal void OnVariantAdded(OrderItemAddedToOrderEvent @event)
		{
			_orderItems.Add(new DomainRelation(@event.OrderItemId));
		}

		[InternalEventHandler]
		internal void OnItemRemoved(OrderItemRemovedFromOrderEvent @event)
		{
			var item = _orderItems.SingleOrDefault(q => q.AggregateId.Equals(@event.OrderItemId, StringComparison.Ordinal));

			if (item != null)
            {
                _orderItems.Remove(item);
            }
        }

		[InternalEventHandler]
		internal void OnOrderCreated(OrderCreatedEvent @event)
		{
			CreatedDate = @event.CreatedTime;
			Title = @event.Title;
			Comment = @event.Comment;
			State = Enum.Parse<OrderState>(@event.State.ToString(CultureInfo.InvariantCulture));
		}

		[InternalEventHandler]
		internal void OnTitleChanged(OrderTitleChangedEvent @event)
		{
			Title = @event.Title;
		}

		[InternalEventHandler]
		internal void OnCommentChanged(OrderCommentChangedEvent @event)
		{
			Comment = @event.Comment;
		}

		#endregion

		#region "Snapshots"

		public Snapshot TakeSnapshot()
		{
			// This method returns a snapshot which will be used to reconstruct the state
			// Attention: It is of utmost importance that you copy reference types.
			return new OrderSnapshot(
				Id.ToString(),
				CurrentVersion,
				CreatedDate,
				Title,
				Comment,
				OrderItems.Select(p => new DomainRelation(p.AggregateId))
					.ToList(), // Take a snapshot (a copy, not a reference)
				State,
				OptionalCertificate != null ? OptionalCertificate.Prefix : null,
				OptionalCertificate != null ? OptionalCertificate.Code : null);
		}

		public void ApplySnapshot(Snapshot snapshot)
		{
			//Important: State changes are done here.
			//Make sure you set the CurrentVersion and LastCommittedVersions here too

			var item = (OrderSnapshot) snapshot;

			Id = GetIdFromStringRepresentation(item.SerializedAggregateId) as OrderId ?? throw new InvalidOperationException();
			CurrentVersion = item.Version;
			LastCommittedVersion = item.Version;
			CreatedDate = item.CreatedDate;
			Title = item.Name;
			Comment = item.Description;
			_orderItems = item.OrderItems.Select(p => new DomainRelation(p.AggregateId)).ToList();
			State = item.State;
			if (item.LabelPrefix != null && item.LabelCode != null)
            {
                OptionalCertificate = new Certificate(item.LabelPrefix, item.LabelCode);
            }
        }

		#endregion

        protected override EntityId<OrderId> GetIdFromStringRepresentation(string value)
        {
            return new OrderId(value);
        }
    }
}
