namespace DDD.BuildingBlocks.MSSQLPackage.Tests.Integration;

using Microsoft.Extensions.Options;
using NSubstitute;

public static class OptionsExtensions
{
    public static IOptionsMonitor<T> AsOptionsMonitor<T>(this T value)
    {
        var monitor = Substitute.For<IOptionsMonitor<T>>();
        monitor.CurrentValue.Returns(value);
        return monitor;
    }
}
