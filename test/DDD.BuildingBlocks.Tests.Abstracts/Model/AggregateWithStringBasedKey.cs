using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Event;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace DDD.BuildingBlocks.Tests.Abstracts.Model
{
    public class AggregateACreationEvent(StringBasedEntityKey entityId) : DomainEvent(entityId.ToString(), -1, _currentTypeVersion)
    {
        private static int _currentTypeVersion = 1;
    }

    public class AggregateWithStringBasedKey : AggregateRoot<StringBasedEntityKey>
    {
        public bool NiftyProperty { get; private set; }

        public AggregateWithStringBasedKey() : base(null!)
        {
            //Important: Aggregate roots must have a parameter-less constructor
            //to make it easier to construct from scratch.

            //The very first event in an aggregate is the creation event
            //which will be applied to an empty object created via this constructor
        }

        public AggregateWithStringBasedKey(StringBasedEntityKey key) : base(key)
        {
            ApplyEvent(new AggregateACreationEvent(key));
        }

        [InternalEventHandler]
        internal void OnCreated(AggregateACreationEvent @event)
        {
            NiftyProperty = true;
        }

        protected override EntityId<StringBasedEntityKey> GetIdFromStringRepresentation(string value)
        {
            return StringBasedEntityKey.GetNewId(value);
        }
    }
}
