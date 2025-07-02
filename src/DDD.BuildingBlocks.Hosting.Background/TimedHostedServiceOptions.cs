// ReSharper disable CollectionNeverUpdated.Global

using System;
using System.Collections.Generic;

namespace DDD.BuildingBlocks.Hosting.Background
{
    public sealed class TimedHostedServiceOptions
    {
        /// <summary>
        ///     Global timeout. If this is set, all instances of TimedHostedService respect this timeout.
        /// </summary>
        public int GlobalTriggerTimeoutInMilliseconds { get; set; } = 10000;

        /// <summary>
        ///     Selective timeouts for specific instances of TimedHostedService identified by type.
        /// </summary>
        public Dictionary<Type, int> SpecificTriggerTimeoutsInMilliseconds { get; } = new();
    }
}
