#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"
mkdir -p artifacts/coverage
# A fresh directory prevents a previous successful report from satisfying the gate.
results_dir="$(mktemp -d "$repo_root/artifacts/coverage/run.XXXXXX")"
dotnet test tests/qrcode.Tests.csproj --configuration Debug \
  --settings coverage.runsettings --collect 'XPlat Code Coverage' \
  --results-directory "$results_dir" --logger 'trx;LogFileName=tests.trx' \
  --verbosity minimal
dotnet run --project tools/ApiCoverageGate/ApiCoverageGate.csproj --configuration Release -- \
  "$repo_root/src/bin/Debug/net10.0/qrcode.dll" "$results_dir"
