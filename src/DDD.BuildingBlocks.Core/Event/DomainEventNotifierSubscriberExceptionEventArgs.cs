using System;

namespace DDD.BuildingBlocks.Core.Event
{
    public class DomainEventNotifierSubscriberExceptionEventArgs(System.Exception exception) : EventArgs
    {
        public System.Exception Exception { get; } = exception;
    }
}