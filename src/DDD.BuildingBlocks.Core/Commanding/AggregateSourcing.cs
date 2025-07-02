using System;
using System.Linq;
using System.Threading.Tasks;
using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Exception;
using DDD.BuildingBlocks.Core.Persistence.Repository;

namespace DDD.BuildingBlocks.Core.Commanding
{
    /// <summary>
    ///     Default implementation for the sourcing of aggregates.
    /// </summary>
    public class AggregateSourcing(IEventSourcingRepository eventSourcingRepository) : IAggregateSourcing
    {
        private readonly IEventSourcingRepository _eventSourcingRepository = eventSourcingRepository ?? throw new ArgumentNullException(nameof(eventSourcingRepository));

        public virtual async Task<T> Source<T,TKey>(Command command, params object[] p)
            where T : AggregateRoot<TKey>, new() where TKey : EntityId<TKey>
        {
            ArgumentNullException.ThrowIfNull(command);

            TKey? key;

            try
            {
                key = Activator.CreateInstance(typeof(TKey), command.SerializedAggregateId) as TKey ?? throw new AggregateSourcingException("CreateInstance failed for key creation.");
            }
            catch (System.Exception e)
            {
                throw new AggregateSourcingException($"Could not instantiate entity key from serialized aggregate id: {command.SerializedAggregateId}", e);
            }

            var aggregate = await _eventSourcingRepository
                .GetByIdAsync<T,TKey>(key);

            if (aggregate != null && command.Mode == AggregateSourcingMode.Create)
            {
                throw new AggregateSourcingException(command.SerializedAggregateId,
                    $"{nameof(AggregateSourcingMode)} is {nameof(AggregateSourcingMode.Create)} but aggregate found for aggregate id");
            }

            if (aggregate == null && command.Mode == AggregateSourcingMode.Update)
            {
                throw new NotFoundException(command.SerializedAggregateId);
            }

            try
            {
                if (aggregate == null && (command.Mode == AggregateSourcingMode.CreateOrUpdate ||
                                          command.Mode == AggregateSourcingMode.Create))
                {
                    aggregate = Activator.CreateInstance(typeof(T),
                        new[] { key }.Union(p).ToArray()) as T;
                }
            }
            catch (System.Exception e)
            {
                throw new AggregateSourcingException(command.SerializedAggregateId, "Could not create aggregate instance.", e);
            }

            if (aggregate == null)
            {
                throw new NotFoundException(command.SerializedAggregateId);
            }

            return aggregate;
        }
    }
}
