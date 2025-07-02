using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Exception;

namespace DDD.BuildingBlocks.Core.Extension
{
    public static class DomainEventExtension
    {
        public static void InvokeOnAggregate<TKey>(this IDomainEvent @event, AggregateRoot<TKey> aggregate, string methodName) where TKey : EntityId<TKey>
        {
            var method =
                ReflectionHelper.GetInternalMethod(aggregate.GetType(), methodName, [@event.GetType()]); //Find the right method

            if (method != null)
            {
                method.Invoke(aggregate, [@event]); //invoke with the event as argument
            }
            else
            {
                throw new AggregateEventOnApplyMethodMissingException(
                    $"No event Apply method found on {aggregate.GetType()} for {@event.GetType()}");
            }
        }
    }
}