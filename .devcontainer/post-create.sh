#!/usr/bin/env bash
set -euo pipefail

# Resolve the repository root even when this script is invoked from elsewhere.
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

# The engine and its nested dependencies are required by the MSBuild imports.
git submodule update --init --recursive

# Restore NuGet packages after the RobustToolbox submodule is available.
dotnet restore
