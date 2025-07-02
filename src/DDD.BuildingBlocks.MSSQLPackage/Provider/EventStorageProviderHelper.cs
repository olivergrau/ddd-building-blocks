// ReSharper disable TemplateIsNotCompileTimeConstantProblem

using System;
using System.Collections.Generic;
using System.Linq;

namespace DDD.BuildingBlocks.MSSQLPackage.Provider;

using System.Collections.Immutable;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using Core;
using Core.Attribute;
using Core.Event;
using Core.Exception;
using Newtonsoft.Json;

internal static class EventStorageProviderHelper
{
    private static readonly IDictionary<string, Type> DeserializationTypes = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
        from type in assembly.GetTypes()
        where type.IsDefined(typeof(DomainEventTypeAttribute))
        select type).ToImmutableDictionary(p => p.Name );

    private static readonly JsonSerializerSettings? SerializerSetting = new() { TypeNameHandling = TypeNameHandling.None };

    internal static void ReconstituteEvent(IDataRecord reader, ICollection<IDomainEvent> events)
    {
        (Guid id, byte[]? data, int version, string type, string key) eventData = (Guid.Empty, null, 0, "", "");
        eventData.id = reader.GetGuid(0);

        eventData.version = reader.GetInt32(2);
        eventData.type = reader.GetString(3);
        eventData.key = reader.GetString(5);
        var startIndex = 0;
        const int bufferSize = 255;
        var buffer = new byte[255];

        var retrieval = reader.GetBytes(1, startIndex, buffer, 0, bufferSize);
        eventData.data = eventData.data.Combine(buffer.Take(Convert.ToInt32(retrieval)).ToArray());

        while (retrieval == bufferSize)
        {
            startIndex += bufferSize;
            retrieval = reader.GetBytes(1, startIndex, buffer, 0, bufferSize);
            eventData.data = eventData.data.Combine(buffer.Take(Convert.ToInt32(retrieval)).ToArray());
        }

        if (eventData.data != null)
        {
            if (!DeserializationTypes.ContainsKey(eventData.type))
            {
                throw new InvalidTypeException(string.Format(CultureInfo.InvariantCulture, CoreErrors.CannotCreateEventOfType, eventData.type));
            }

            events.Add(DeserializeEvent(eventData.data, DeserializationTypes[eventData.type]));
        }
    }

    private static IDomainEvent DeserializeEvent(byte[] eventData, Type? clrType) =>
#pragma warning disable CS8600
            ((IDomainEvent)JsonConvert.DeserializeObject(
                Encoding.UTF8.GetString(eventData),
                clrType,
                SerializerSetting))!;
#pragma warning restore CS8600

    internal static (byte[] eventData, Type? clrType) SerializeEvent(IDomainEvent @event) =>
        (
            // ToDo: Check if we need the FullType property of the event for serialization (instead of the DomainEventType attribute)
            eventData: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event, SerializerSetting)),
            clrType: Type.GetType(@event.FullType)
        );
}
