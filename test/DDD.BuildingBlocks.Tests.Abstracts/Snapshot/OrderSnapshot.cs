using System;
using System.Collections.Generic;
using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Tests.Abstracts.Model;

namespace DDD.BuildingBlocks.Tests.Abstracts.Snapshot
{
    using Core.Persistence.SnapshotSupport;

    [Serializable]
	public class OrderSnapshot(
        string serializedAggregateId,
        int version,
        DateTime createdDate,
        string name,
        string description,
        IEnumerable<DomainRelation> orderItems,
        OrderState state,
        string? labelPrefix,
        string? labelCode
    ) : Snapshot(serializedAggregateId, version, "Order")
    {
		public DateTime CreatedDate
		{
			get;
		} = createdDate;

        public string Name
		{
			get;
		} = name;

        public string Description
		{
			get;
		} = description;

        public OrderState State
		{
			get;
		} = state;

        public IEnumerable<DomainRelation> OrderItems
		{
			get;
		} = orderItems;

        public string? LabelPrefix
		{
			get;
		} = labelPrefix;

        public string? LabelCode
		{
			get;
		} = labelCode;
    }
}
