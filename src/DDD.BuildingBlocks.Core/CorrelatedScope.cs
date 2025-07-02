using System;

namespace DDD.BuildingBlocks.Core;

using Util;

public class CorrelatedScope(string? value = null) : AmbientContext<string>(value ?? Guid.NewGuid()
    .ToString());
