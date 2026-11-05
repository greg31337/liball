# T117 release-readiness verification

This record covers the T117 verification performed on 2026-08-20 with .NET
SDK 10.0.103. It complements the corpus compatibility evidence and does not
promote any experimental camera or writer profile.

## Cross-platform build and test gate

Release cross-compilation completed for `win-x64`, `linux-x64`, and
`osx-arm64`. Native test execution is automated by
`.github/workflows/ci.yml`, whose matrix restores, builds, tests, and packs on
the current GitHub-hosted Ubuntu, Windows, and macOS images. Pull requests,
pushes, and manual workflow runs all exercise the same matrix.

The local macOS arm64 Release suite passed 978 tests, skipped four opt-in
ExifTool/Exiv2 checks because those executables were unavailable, and failed
none. The focused non-seekable stream, layout, and determinism group passed
120 tests, with two of the same external-tool checks skipped.

## Concurrency and determinism

`RawProfileCatalogue` serializes each complete registration attempt and
publishes stable read/write snapshots. Selection, matching, and compatibility
queries consume snapshots, so a concurrent registration cannot expose a
partially updated list. A 32-way registration regression test verifies one
atomic winner for a shared identifier and stable diagnostics for all rejected
attempts.

Generic documents retain deep copies of their descriptors, channel models,
and CFA grids. Independently opened documents can therefore decode in
parallel without sharing mutable descriptor state. The focused regression
opens two documents, decodes them concurrently, compares every normalized
sample, and verifies that caller and sibling-document mutations remain
isolated.

Existing deterministic encoder, layout planner, parser, non-seekable input,
and preplanned non-seekable output tests remain part of the full suite.

## Package inspection

The Release package contains `lib/net10.0/raw.dll` and
`lib/net10.0/raw.xml`. The XML document is well formed. The generated NuGet
dependency group for `net10.0` is empty, `dotnet list src/raw.csproj package
--include-transitive` reports no packages, and production source contains no
P/Invoke/native-library loader or external-process integration. The package
contains no runtime-specific or native asset directories.

Commands used for the local verification included:

```sh
dotnet build raw.slnx --configuration Release --no-restore
dotnet test tests/raw.Tests.csproj --configuration Release --no-restore
dotnet pack src/raw.csproj --configuration Release --no-build --no-restore
dotnet list src/raw.csproj package --include-transitive
```
