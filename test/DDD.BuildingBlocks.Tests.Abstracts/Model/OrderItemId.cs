using System;
using System.Collections.Generic;
using DDD.BuildingBlocks.Core.Domain;

namespace DDD.BuildingBlocks.Tests.Abstracts.Model
{
    public class OrderItemId : EntityId<OrderItemId>
    {
        public int Position { get; }
        public string OrderNumber { get; }

        /// <summary>
        ///     Constructor with two example components for the order item id.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="orderNumber">the OrderId from the parent order as string.</param>
        public OrderItemId(int position, string orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(orderNumber));
            }

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(position);

            Position = position;
            OrderNumber = orderNumber;
        }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            return new object[] { OrderNumber, Position };
        }

        public override string ToString()
        {
            return $"{OrderNumber}:{Position}";
        }
    }
}
