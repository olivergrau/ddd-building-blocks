using System;

namespace DDD.BuildingBlocks.Core;

public class ErrorEventArgs(System.Exception? exception, string? errorInfo = null) : EventArgs
{
    public System.Exception? Exception { get; } = exception;
    public string? ErrorInfo { get; } = errorInfo;
}
