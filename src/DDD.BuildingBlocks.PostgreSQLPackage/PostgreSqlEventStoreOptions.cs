using System;
using System.Text.RegularExpressions;

namespace DDD.BuildingBlocks.PostgreSQLPackage;

public sealed class PostgreSqlEventStoreOptions
{
    private static readonly Regex ValidIdentifier = new(
        "^[a-z_][a-z0-9_]*$",
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
                throw new ArgumentException(
                    "The schema must be an unquoted lower-case PostgreSQL identifier.",
                    nameof(value));
            }

            _schema = value;
        }
    }
}
