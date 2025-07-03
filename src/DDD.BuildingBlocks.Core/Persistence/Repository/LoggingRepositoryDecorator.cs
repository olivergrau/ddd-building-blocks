using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Event;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem


namespace DDD.BuildingBlocks.Core.Persistence.Repository
{
    /// <summary>
    ///     A repository decorator which applies a logging on every repository operation.
    /// </summary>
    public class LoggingRepositoryDecorator : IEventSourcingRepository
    {
        private readonly IEventSourcingRepository _eventSourcingRepository;
        private DateTime _commitStartTime;

        private readonly ILogger _log;

        public LoggingRepositoryDecorator(IEventSourcingRepository eventSourcingRepository, ILoggerFactory? loggerFactory = null)
        {
            var nullLoggerFactory = new NullLoggerFactory();

            _log = loggerFactory != null ? loggerFactory.CreateLogger(nameof(LoggingRepositoryDecorator))
                : nullLoggerFactory.CreateLogger(nameof(DomainEventNotifier));

            _eventSourcingRepository = eventSourcingRepository;

            nullLoggerFactory.Dispose();
        }

        public async Task<object?> GetByIdAsync(string id, Type type, int version = -1)
        {
            BeforeLoadAggregate(id);
            var result = await _eventSourcingRepository.GetByIdAsync(id, type, version);
            AfterLoadingAggregate(id, result);
            return result;
        }

        public virtual async Task<T?> GetByIdAsync<T,TKey>(TKey id) where T : AggregateRoot<TKey> where TKey : EntityId<TKey>
        {
            BeforeLoadAggregate(id);
            var result = await _eventSourcingRepository.GetByIdAsync<T,TKey>(id);
            AfterLoadingAggregate(id, result);
            return result;
        }

        public virtual async Task SaveAsync(IEventSourcingBasedAggregate aggregate)
        {
            try
            {
                var events = aggregate.GetUncommittedChanges().ToList();

                BeforeSaveAggregate(aggregate, events);
                await _eventSourcingRepository.SaveAsync(aggregate);
                AfterSavingAggregate(aggregate, events);
            }
            catch (System.Exception ex)
            {
                _log.LogError("{Exception}", ex);
                throw;
            }
        }

        protected virtual void BeforeLoadAggregate(object id)
        {
            _log.LogDebug("Loading aggregate with {Id} ...", id);
        }

        protected virtual void AfterLoadingAggregate<T>(object id, T aggregate)
        {
            _log.LogDebug(aggregate != null ? "Loaded {Type}" : "Aggregate {Id} not found", aggregate?.GetType(), id);
        }

        protected virtual void BeforeSaveAggregate<T>(T aggregate, IEnumerable<IDomainEvent> events)
        {
            _commitStartTime = DateTime.Now;
            _log.LogDebug($"Trying to commit {events.Count()} aggregate event(s) to underlying storage");
        }

        protected virtual void AfterSavingAggregate<T>(T aggregate, IEnumerable<IDomainEvent?> events)
        {
            _log.LogDebug(
                $"Committed {events.Count()} aggregate event(s) in {DateTime.Now.Subtract(_commitStartTime).TotalMilliseconds} ms");
        }
    }
}
