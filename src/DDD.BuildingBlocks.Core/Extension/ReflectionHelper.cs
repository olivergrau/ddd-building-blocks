using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using DDD.BuildingBlocks.Core.Attribute;
using DDD.BuildingBlocks.Core.Domain;
using DDD.BuildingBlocks.Core.Event;

namespace DDD.BuildingBlocks.Core.Extension;

[ExcludeFromCodeCoverage]
public static class ReflectionHelper
{
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, string>>
        AggregateEventHandlerCache = new();

    public static Dictionary<Type, string> FindEventHandlerMethodsInAggregate(Type aggregateType)
    {
        if (AggregateEventHandlerCache.ContainsKey(aggregateType) == false)
        {
            var eventHandlers = new ConcurrentDictionary<Type, string>();

            var methods = aggregateType
                .GetMethodsBySignature(typeof(void), typeof(InternalEventHandlerAttribute), true, typeof(IDomainEvent))
                .ToList();

            if (methods.Count != 0)
            {
                if ((from m in methods
                        let parameter = m.GetParameters().First()
                        where eventHandlers.TryAdd(parameter.ParameterType, m.Name) == false
                        select m).Any())
                {
                    throw new System.Exception(
                        $"Multiple methods found handling same event in {aggregateType.Name}");
                }
            }

            if (AggregateEventHandlerCache.TryAdd(aggregateType, eventHandlers) == false)
            {
                if (!AggregateEventHandlerCache.ContainsKey(aggregateType))
                {
                    throw new System.Exception(
                        $"Error registering methods for handling events in {aggregateType.Name}");
                }
            }
        }

        return AggregateEventHandlerCache[aggregateType].ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static IEnumerable<MethodInfo> GetMethodsBySignature(this Type type,
        Type returnType,
        Type customAttributeType,
        bool matchParameterInheritance,
        params Type[] parameterTypes)
    {
        return type.GetRuntimeMethods().Where(m =>
        {
            if (m.ReturnType != returnType)
            {
                return false;
            }

            if (m.GetCustomAttributes(customAttributeType, true).Length != 0 == false)
            {
                return false;
            }

            var parameters = m.GetParameters();

            if (parameterTypes.Length == 0)
            {
                return parameters.Length == 0;
            }

            if (parameters.Length != parameterTypes.Length)
            {
                return false;
            }

            for (var i = 0; i < parameterTypes.Length; i++)
            {
                if ((parameters[i].ParameterType == parameterTypes[i] ||
                     matchParameterInheritance && parameterTypes[i].GetTypeInfo()
                         .IsAssignableFrom(parameters[i].ParameterType.GetTypeInfo())) == false)
                {
                    return false;
                }
            }

            return true;
        });
    }

    public static string GetTypeName(Type t)
    {
        return t.Name;
    }

    public static string GetTypeFullName(Type t)
    {
        return t.AssemblyQualifiedName!;
    }

    public static MethodInfo[] GetMethods(Type t)
    {
        return t.GetTypeInfo().DeclaredMethods.ToArray();
    }

    public static MethodInfo? GetInternalMethod(Type t, string methodName, Type[] paramTypes)
    {
        try
        {
            return t.GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Instance)!;
        }
        catch
        {
            return null;
        }
    }

    public static MemberInfo[] GetMembers(Type t)
    {
        return t.GetTypeInfo().DeclaredMembers.ToArray();
    }

    public static T CreateInstance<T, TKey>() where T : AggregateRoot<TKey> where TKey : EntityId<TKey>
    {
        return (T)Activator.CreateInstance(typeof(T))!;
    }
}
