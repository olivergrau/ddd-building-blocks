using System;
using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Util;
using DDD.BuildingBlocks.Tests.Abstracts.Event;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace DDD.BuildingBlocks.Tests.Abstracts.Model
{
    using System.Globalization;

    public class OrderItem : AggregateRoot<OrderItemId>
	{
		public OrderItemState State
		{
			get; private set;
		}

		public string Name
		{
			get; private set;
		} = null!;

        public string Description
		{
			get; private set;
		} = null!;

        public decimal SellingPrice
		{
			get; private set;
		}

		public DateTime RegistrationDate
		{
			get; private set;
		}

		#region "Constructor"

		public OrderItem() : base(null!)
		{
			//Important: Aggregate roots must have a parameterless constructor
			//to make it easier to construct from scratch.

			//The very first event in an aggregate is the creation event
			//which will be applied to an empty object created via this constructor
			State = OrderItemState.Active;
		}

		public OrderItem(int position, string orderNumber, string name, string description)
            : base(new OrderItemId(position, orderNumber))
		{
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(description));
            }

            ApplyEvent(new OrderItemCreatedEvent(Id.ToString(), CurrentVersion, name, description, ApplicationTime.Current));
		}

		#endregion

		#region "Mutators"

		public void SetBuyingPrice(decimal buyingPrice)
		{
			ApplyEvent(new OrderItemBuyingPriceChangedEvent(Id.ToString(), CurrentVersion, buyingPrice));
		}

		public void Cancel()
		{
			ApplyEvent(new OrderItemCancelledEvent(Id.ToString(), CurrentVersion));
		}

		public void SetActive()
		{
			ApplyEvent(new OrderItemStateChangedEvent(Id.ToString(), CurrentVersion, (int) OrderItemState.Active));
		}

		public void SetAsSold()
		{
			ApplyEvent(new OrderItemStateChangedEvent(Id.ToString(), CurrentVersion, (int) OrderItemState.Sold));
		}

		public void ChangeName(string newName)
		{
			if (Name != newName)
			{
				ApplyEvent(new OrderItemNameChangedEvent(Id.ToString(), CurrentVersion, newName));
			}
		}

		public void ChangeDescription(string newDescription)
		{
			if (Description != newDescription)
			{
				ApplyEvent(new OrderItemDescriptionChangedEvent(Id.ToString(), CurrentVersion, newDescription));
			}
		}

		#endregion

		#region "Apply Events"

		[InternalEventHandler]
		internal void OnOrderItemCancelled(OrderItemCancelledEvent @event)
		{
			State = OrderItemState.Deactivated;
		}

		[InternalEventHandler]
		internal void OnOrderItemStateChanged(OrderItemStateChangedEvent @event)
		{
			State = Enum.Parse<OrderItemState>(@event.State.ToString(CultureInfo.InvariantCulture));
		}

		[InternalEventHandler]
		internal void OnOrderItemCreated(OrderItemCreatedEvent @event)
		{
			RegistrationDate = @event.RegistrationDate;
			Name = @event.Name;
			Description = @event.Description;
		}

		[InternalEventHandler]
		internal void OnNameChanged(OrderItemNameChangedEvent @event)
		{
			Name = @event.Name;
		}

		[InternalEventHandler]
		internal void OnDescriptionChanged(OrderItemDescriptionChangedEvent @event)
		{
			Description = @event.Description;
		}

		[InternalEventHandler]
		internal void OnInventoryItemBuyingPriceSet(OrderItemBuyingPriceChangedEvent @event)
		{
			SellingPrice = @event.BuyingPrice;
		}

		#endregion

        protected override EntityId<OrderItemId> GetIdFromStringRepresentation(string value)
        {
            // $"{OrderNumber}:{Position}";
            var split = value.Split(":");
            return new OrderItemId(Convert.ToInt32(split[1], CultureInfo.InvariantCulture), split[0]);
        }
    }
}
