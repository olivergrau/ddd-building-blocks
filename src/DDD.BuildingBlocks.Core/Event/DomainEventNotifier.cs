using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DDD.BuildingBlocks.Core.Util;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem
// ReSharper disable LogMessageIsSentenceProblem

namespace DDD.BuildingBlocks.Core.Event
{
    /// <summary>
    ///     Implementation of the IDomainEventNotifier interface for the notification of all subscribers which registered for a
    ///     particular domain event.
    /// </summary>
    public class DomainEventNotifier : IDomainEventNotifier
    {
        private readonly ILogger _log;
        private readonly string _searchPattern = "";

        private readonly Dictionary<Type, List<MethodInfo>> _detectedSubscribers = new();

        private IDependencyResolver? _optionalDependencyResolver;

        public event EventHandler? OnSubscriberException;

        public DomainEventNotifier(string searchPattern = "", ILoggerFactory? loggerFactory = null)
        {
            if (!string.IsNullOrWhiteSpace(searchPattern))
            {
                _searchPattern = searchPattern;
            }

            var nullLoggerFactory = new NullLoggerFactory();

            _log = loggerFactory != null ? loggerFactory.CreateLogger(nameof(DomainEventNotifier))
                : nullLoggerFactory.CreateLogger(nameof(DomainEventNotifier));

            _log.LogInformation(
                $"DomainEventNotifier discovering subscribers under usage of search path: {searchPattern}...");

            DiscoverSubscribersInternal();

            if (_detectedSubscribers.Keys.Count == 0)
            {
                _log.LogWarning(DomainEventNotifierTextConstants.NoSubscribersFound);
            }

            foreach (var (key, value) in _detectedSubscribers)
            {
                _log.LogInformation("Discovered {CountSubscribers} subscribers {AssemblyQualifiedName}",
                    key.AssemblyQualifiedName, value.Count);
            }

            nullLoggerFactory.Dispose();
        }

        public void SetDependencyResolver(IDependencyResolver dependencyResolver)
        {
            _optionalDependencyResolver = dependencyResolver;
        }

        public async Task NotifyAsync(IDomainEvent @event)
        {
            _log.LogInformation(
                $"Processing DomainEvent {@event.GetType().Name} /#{@event.TargetVersion + 1} on Aggregate {@event.SerializedAggregateId} [{@event.SerializedAggregateId}] @{ApplicationTime.Current.ToLongTimeString()}");

            try
            {
                await InvokeSubscriber(@event);
            }
            catch (System.Exception ex)
            {
                _log.LogError(ex, DomainEventNotifierTextConstants.ErrorExecutingSubscriber);
            }
        }

        private async Task InvokeSubscriber<T>(T @event) where T : IDomainEvent
        {
            if (!_detectedSubscribers.Any())
            {
	            _log.LogTrace("No subscribers available.");
				return;
            }

            var methodList = _detectedSubscribers.Where(q => q.Key == @event.GetType()).Select(p => p.Value)
                .SingleOrDefault();

            if (methodList == null || methodList.Count == 0)
            {
                _log.LogWarning("No subscribers for event {Event} found.", @event.GetType().AssemblyQualifiedName);
                return;
            }

            _log.LogTrace("Found {SubscribersCount} subscribers for {EventType}", methodList.Count, @event.GetType().AssemblyQualifiedName);

            foreach (var subscriber in methodList)
            {
                _log.LogInformation($"Trying to instantiate event subscriber for: {@event.GetType().Name}");

                object? instance;
                try
                {
                    instance = GetInstance(subscriber);
                }
                catch (System.Exception ex)
                {
                    _log.LogError(ex, "Could not instantiate subscriber for event {EventType}: {ExceptionMessage}",
                        @event.GetType().AssemblyQualifiedName, ex.Message);
                    continue;
                }

                _log.LogInformation(
                    $"Try to execute event subscriber: {subscriber.DeclaringType} / {subscriber.Name}");

                if (instance == null)
                {
                    _log.LogWarning("An instance could not be resolved or created for {Event}",
                        @event.GetType().AssemblyQualifiedName);
                }
                else
                {
                    try
                    {
                        await (Task) subscriber.Invoke(instance, [@event])!;
                    }
                    catch (System.Exception e)
                    {
                        // ReSharper disable once StructuredMessageTemplateProblem
                        _log.LogError(e, "Subscriber threw an exception: {@Exception}", e);
                        OnSubscriberException?.Invoke(this, new DomainEventNotifierSubscriberExceptionEventArgs(e));
                    }
                    
                    _log.LogInformation(
                        $"Executed event subscriber: {subscriber.DeclaringType} / {subscriber.Name}");
                }
            }
        }

        private object? GetInstance(MemberInfo subscriber)
        {
            object? instance = null;
            if (_optionalDependencyResolver != null)
            {
                _log.LogInformation("Optional dependency resolver found. Trying to get the instance with that resolver.");
                instance = _optionalDependencyResolver.Resolve(subscriber.DeclaringType!);

                if (instance == null)
                {
                    _log.LogWarning("Optional dependency resolver could not resolve subscriber type for {Event}",
                        subscriber.DeclaringType);
                }
            }

            if (instance != null)
            {
                return instance;
            }

            _log.LogTrace(
                "No dependency resolver set or subscriber could not be resolved. Trying to activate with empty constructor.");
            Debug.Assert(subscriber.DeclaringType != null, "subscriber.Value.DeclaringType != null");
            instance = Activator.CreateInstance(subscriber.DeclaringType);

            return instance;
        }

        private void DiscoverSubscribersInternal()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(
                q => q.FullName!.StartsWith(_searchPattern, StringComparison.InvariantCulture))
            )
            {
                try
                {
                    foreach (var type in assembly.ExportedTypes.Where(t => t.IsClass &&
                                                                           t.GetInterfaces().Any(a =>
                                                                               a.Name.StartsWith("ISubscribe", StringComparison.InvariantCulture))))
                    {
                        var subscribers = type
                            .GetMethods()
                            .Where(m => m.Name.Equals("WhenAsync", StringComparison.Ordinal))
                            .Select(m => new
                            {
                                Method = m,
                                Params = m.GetParameters()
                            })
                            .Where(x => x.Params.Any(pp => typeof(DomainEvent).IsAssignableFrom(pp.ParameterType)))
                            .Select(x => new {x.Method, EventType = x.Params.Single().ParameterType});

                        foreach (var subscriber in subscribers)
                        {
                            if (!_detectedSubscribers.ContainsKey(subscriber.EventType))
                            {
                                _detectedSubscribers.Add(subscriber.EventType, []);
                            }

                            _detectedSubscribers[subscriber.EventType].Add(subscriber.Method);
                        }
                    }
                }
                catch (FileLoadException loadEx)
                {
                    _log.LogWarning("Failed loading assembly: " + loadEx.Message);
                } // The Assembly has already been loaded.
                catch (BadImageFormatException imgEx)
                {
                    _log.LogWarning("Failed loading assembly (bad image format): " + imgEx.Message);
                }
            }
        }
    }
}
