using System;

namespace DDD.BuildingBlocks.Core.Attribute
{
    /// <summary>
    ///     This attribute marks an aggregate method as an internal event handling method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class InternalEventHandlerAttribute : System.Attribute
    {
    }
}
