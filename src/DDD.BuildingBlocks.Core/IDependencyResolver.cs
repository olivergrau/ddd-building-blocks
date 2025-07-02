using System;

namespace DDD.BuildingBlocks.Core
{
    public interface IDependencyResolver
    {
        T? Resolve<T>() where T : notnull;
        object Resolve(string name, Type type);
        object? Resolve(Type type);
    }
}
