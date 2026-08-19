#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
release_version="${1:-2.1.0}"
release_root="$repository_root/artifacts/release/v$release_version"
package_root="$release_root/packages"
release_notes="$repository_root/RELEASE_NOTES-$release_version.md"

if [[ ! "$release_version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "Release version must be a stable semantic version such as 2.1.0." >&2
  exit 1
fi

if [[ ! -f "$release_notes" ]]; then
  echo "Release notes not found: $release_notes" >&2
  exit 1
fi

mkdir -p "$package_root"
find "$release_root" -mindepth 1 -maxdepth 2 -type f -delete

dotnet restore "$repository_root/DDD.BuildingBlocks.sln"
dotnet build "$repository_root/DDD.BuildingBlocks.sln" --no-restore --configuration Release
dotnet pack "$repository_root/DDD.BuildingBlocks.Packages.slnf" \
  --no-build --configuration Release --output "$package_root"

mapfile -t packages < <(find "$package_root" -maxdepth 1 -type f -name '*.nupkg' ! -name '*.snupkg' | sort)
mapfile -t symbols < <(find "$package_root" -maxdepth 1 -type f -name '*.snupkg' | sort)

if [[ "${#packages[@]}" -ne 7 || "${#symbols[@]}" -ne 7 ]]; then
  echo "Expected seven NuGet packages and seven symbol packages." >&2
  exit 1
fi

for package in "${packages[@]}"; do
  metadata="$(unzip -p "$package" '*.nuspec')"
  grep -q "<version>$release_version</version>" <<<"$metadata"
  grep -q '<license type="expression">MIT</license>' <<<"$metadata"
  grep -q '<repository type="git" url="https://github.com/olivergrau/ddd-building-blocks"' <<<"$metadata"
  unzip -Z1 "$package" | grep -qx 'PACKAGE_README.md'
  unzip -Z1 "$package" | grep -Eq '^lib/net10\.0/.+\.dll$'
  unzip -Z1 "$package" | grep -Eq '^lib/net10\.0/.+\.xml$'
done

cp "$repository_root/CHANGES.md" "$release_root/CHANGES.md"
cp "$repository_root/MIGRATION-2.0.md" "$release_root/MIGRATION-2.0.md"
cp "$release_notes" "$release_root/RELEASE_NOTES-$release_version.md"
cp "$repository_root/LICENSE" "$release_root/LICENSE"

(
  cd "$release_root"
  find . -type f ! -name 'SHA256SUMS' -print0 | sort -z | xargs -0 sha256sum > SHA256SUMS
  tar -czf "DDD.BuildingBlocks-v$release_version.tar.gz" \
    packages CHANGES.md MIGRATION-2.0.md "RELEASE_NOTES-$release_version.md" LICENSE SHA256SUMS
  sha256sum "DDD.BuildingBlocks-v$release_version.tar.gz" >> SHA256SUMS
)

echo "Release bundle created at $release_root"
