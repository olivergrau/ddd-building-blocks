#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
release_version="${1:-2.1.1}"
package_root="${2:-$repository_root/artifacts/release/v$release_version/packages}"
smoke_root="$(mktemp -d)"
trap 'rm -rf -- "$smoke_root"' EXIT

dotnet new console --framework net10.0 --output "$smoke_root" --no-restore >/dev/null

sed -i "/<\\/Project>/i\\  <ItemGroup>\\n    <PackageReference Include=\"DDD.BuildingBlocks.Core\" Version=\"$release_version\" />\\n    <PackageReference Include=\"DDD.BuildingBlocks.DevelopmentPackage\" Version=\"$release_version\" />\\n    <PackageReference Include=\"DDD.BuildingBlocks.PostgreSQLPackage\" Version=\"$release_version\" />\\n    <PackageReference Include=\"DDD.BuildingBlocks.MSSQLPackage\" Version=\"$release_version\" />\\n    <PackageReference Include=\"DDD.BuildingBlocks.DI.Extensions\" Version=\"$release_version\" />\\n    <PackageReference Include=\"DDD.BuildingBlocks.Hosting.Background\" Version=\"$release_version\" />\\n    <PackageReference Include=\"DDD.BuildingBlocks.AzurePackage\" Version=\"$release_version\" />\\n  </ItemGroup>" "$smoke_root/$(basename "$smoke_root").csproj"

sed -i '1i using DDD.BuildingBlocks.Core.Persistence.SnapshotSupport;\nusing DDD.BuildingBlocks.DevelopmentPackage.Storage;\nusing DDD.BuildingBlocks.MSSQLPackage;\nusing DDD.BuildingBlocks.PostgreSQLPackage;' "$smoke_root/Program.cs"
sed -i '$a Console.WriteLine($"{typeof(SnapshotEnvelope).Name}:{typeof(InMemoryEventStoreProvider).Name}:{typeof(PostgreSqlEventStoreProvider).Name}:{typeof(SqlServerEventStoreProvider).Name}");' "$smoke_root/Program.cs"

dotnet restore "$smoke_root" \
  --source "$package_root" \
  --source "https://api.nuget.org/v3/index.json"
dotnet run --project "$smoke_root" --no-restore

echo "Consumer smoke test succeeded using packages from $package_root"
