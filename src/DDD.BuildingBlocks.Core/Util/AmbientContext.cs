// ReSharper disable UnusedType.Global

using System;

namespace DDD.BuildingBlocks.Core.Util;

using System.Collections.Generic;
using System.Threading;

public class AmbientContext<T> : IDisposable
{
    private bool _disposed;

    private static readonly AsyncLocal<Stack<T>> _ambientStack = new();

    private static Stack<T> AmbientStack => _ambientStack.Value ?? (_ambientStack.Value = new Stack<T>());

    public AmbientContext(T value)
    {
        AmbientStack.Push(value);
    }

    public static T? Current => AmbientStack.Count > 0 ? AmbientStack.Peek() : default;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if(disposing)
            {
                if (AmbientStack.Count > 0)
                {
                    AmbientStack.Pop();
                }

                _disposed = true;
            }
        }
    }

    ~AmbientContext()
    {
        Dispose(false);
    }
}
