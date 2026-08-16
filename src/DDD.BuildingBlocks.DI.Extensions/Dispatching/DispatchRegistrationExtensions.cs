using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DDD.BuildingBlocks.Core.Commanding;
using DDD.BuildingBlocks.Core.Event;
using DDD.BuildingBlocks.Core.Exception;
using Microsoft.Extensions.DependencyInjection;

namespace DDD.BuildingBlocks.DI.Extensions.Dispatching;

public static class DispatchRegistrationExtensions
{
    public static IServiceCollection AddDddBuildingBlocksDispatching(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        var types = assemblies.Distinct().SelectMany(GetExportedTypes).ToArray();
        RegisterCommandHandlers(services, types);
        RegisterEventSubscribers(services, types);

        services.AddSingleton<ICommandDispatcher, ScopedCommandDispatcher>();
        services.AddSingleton<IDomainEventNotifier, ScopedDomainEventNotifier>();
        return services;
    }

    private static void RegisterCommandHandlers(IServiceCollection services, IReadOnlyCollection<Type> types)
    {
        var registrations = FindClosedImplementations(types, typeof(ICommandHandler<>)).ToArray();
        var duplicate = registrations.GroupBy(item => item.ServiceType).FirstOrDefault(group => group.Count() != 1);

        if (duplicate is not null)
        {
            throw new HandlerRegistrationException(
                $"Command {duplicate.Key.GenericTypeArguments[0].FullName} has {duplicate.Count()} handlers; exactly one is required.");
        }

        var handledCommands = registrations.Select(item => item.ServiceType.GenericTypeArguments[0]).ToHashSet();
        var commandTypes = types.Where(type => type is { IsAbstract: false, IsClass: true } && typeof(ICommand).IsAssignableFrom(type));
        var missing = commandTypes.FirstOrDefault(commandType => !handledCommands.Contains(commandType));

        if (missing is not null)
        {
            throw new HandlerRegistrationException($"Command {missing.FullName} has no registered handler.");
        }

        foreach (var registration in registrations)
        {
            services.AddScoped(registration.ServiceType, registration.ImplementationType);
        }
    }

    private static void RegisterEventSubscribers(IServiceCollection services, IReadOnlyCollection<Type> types)
    {
        var registrations = FindClosedImplementations(types, typeof(ISubscribe<>)).ToArray();
        var duplicate = registrations.GroupBy(item => (item.ServiceType, item.ImplementationType))
            .FirstOrDefault(group => group.Count() != 1);

        if (duplicate is not null)
        {
            throw new HandlerRegistrationException(
                $"Event subscriber {duplicate.Key.ImplementationType.FullName} is registered more than once for {duplicate.Key.ServiceType.GenericTypeArguments[0].FullName}.");
        }

        foreach (var registration in registrations)
        {
            services.AddScoped(registration.ServiceType, registration.ImplementationType);
        }
    }

    private static IEnumerable<(Type ServiceType, Type ImplementationType)> FindClosedImplementations(
        IEnumerable<Type> types,
        Type openGenericType)
    {
        return from type in types
            where type is { IsAbstract: false, IsClass: true }
            from serviceType in type.GetInterfaces()
            where serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == openGenericType
            select (serviceType, type);
    }

    private static IEnumerable<Type> GetExportedTypes(Assembly assembly)
    {
        try
        {
            return assembly.ExportedTypes;
        }
        catch (ReflectionTypeLoadException exception)
        {
            throw new HandlerRegistrationException(
                $"Could not inspect assembly {assembly.FullName}: {exception.Message}");
        }
    }
}
