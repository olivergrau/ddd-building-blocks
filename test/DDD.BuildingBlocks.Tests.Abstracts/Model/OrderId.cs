using System;
using System.Collections.Generic;
using DDD.BuildingBlocks.Core.Domain;

namespace DDD.BuildingBlocks.Tests.Abstracts.Model
{
    public class OrderId : EntityId<OrderId>
    {
        public string OrderNumber { get; }

        public OrderId(string orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(orderNumber));
            }

            OrderNumber = orderNumber;
        }

        protected override IEnumerable<object> GetAttributesToIncludeInEqualityCheck()
        {
            return new[] { OrderNumber };
        }

        public override string ToString()
        {
            return OrderNumber;
        }
    }
}