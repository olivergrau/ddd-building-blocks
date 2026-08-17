# Prerequisites and installation

## Requirements

- .NET SDK `10.0.400` or a compatible .NET 10 SDK
- a .NET project targeting `net10.0`
- access to the DDD.BuildingBlocks `v2.0.0` GitHub Release assets

Database tutorials additionally require Docker for Testcontainers or a reachable PostgreSQL/SQL Server instance.

## Download the packages

Download the `.nupkg` files from the `v2.0.0` GitHub Release. You may download individual packages or extract the complete release archive.

Keep all DDD.BuildingBlocks packages on the same version. Version `2.0.0` is a coordinated major release; mixing it with 1.x packages is unsupported.

## Add a local NuGet source

Place the `.nupkg` files in one directory and register it as a package source:

```bash
dotnet nuget add source /absolute/path/to/ddd-building-blocks/packages \
  --name ddd-building-blocks-local
```

Verify the source:

```bash
dotnet nuget list source
```

## Add packages

For the in-memory quickstart:

```bash
dotnet add package DDD.BuildingBlocks.Core --version 2.0.0
dotnet add package DDD.BuildingBlocks.DevelopmentPackage --version 2.0.0
```

For Microsoft dependency-injection dispatch:

```bash
dotnet add package DDD.BuildingBlocks.DI.Extensions --version 2.0.0
```

For a production event store, add exactly one or both provider packages as required by your deployment topology:

```bash
dotnet add package DDD.BuildingBlocks.PostgreSQLPackage --version 2.0.0
dotnet add package DDD.BuildingBlocks.MSSQLPackage --version 2.0.0
```

## Verify restore

```bash
dotnet restore
dotnet list package
```

The application's project file should show explicit `2.0.0` references. Third-party dependencies continue to restore from NuGet.org unless your organization uses another mapped source.

## Source mapping recommendation

For a controlled environment, use NuGet package source mapping so `DDD.BuildingBlocks.*` can only resolve from the intended local or private source. This reduces dependency-confusion risk. Keep standard framework and third-party dependencies mapped to your approved NuGet.org proxy or feed.

## Next step

Continue with the [quickstart](quickstart.md).
