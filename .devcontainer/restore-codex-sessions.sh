#!/usr/bin/env bash

set -euo pipefail

readonly migration_source="/workspaces/local-nuget-feed/.codex-session-migration/ddd-building-blocks"
readonly codex_data="/home/vscode/.codex"
readonly migration_marker="${codex_data}/.ddd-building-blocks-session-migration-complete"

if [[ -f "${migration_marker}" || ! -d "${migration_source}/sessions" ]]; then
  exit 0
fi

mkdir -p "${codex_data}/sessions"
cp -a "${migration_source}/sessions/." "${codex_data}/sessions/"

if [[ -f "${migration_source}/session_index.jsonl" ]]; then
  cp "${migration_source}/session_index.jsonl" "${codex_data}/session_index.jsonl"
fi

touch "${migration_marker}"
echo "Restored Codex session history from the pre-rebuild migration backup."
