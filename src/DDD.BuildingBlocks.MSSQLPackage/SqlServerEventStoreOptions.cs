using System;
using System.Text.RegularExpressions;

namespace DDD.BuildingBlocks.MSSQLPackage;

public sealed class SqlServerEventStoreOptions
{
    private static readonly Regex ValidIdentifier = new(
        "^[A-Za-z_][A-Za-z0-9_]*$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    private string _schema = "ddd_building_blocks";

    public string Schema
    {
        get => _schema;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (!ValidIdentifier.IsMatch(value))
            {
                throw new ArgumentException("The schema must be a regular SQL Server identifier.", nameof(value));
            }

            _schema = value;
        }
    }
}
