# Fixture manifest verifier

This dependency-free .NET tool validates the allowlisted third-party fixture
subsets in `tests/data/fixture-manifest.json`. It runs entirely offline.

```sh
dotnet run --project tools/FixtureImporter -- --verify
dotnet run --project tools/FixtureImporter -- --print
```

`--verify` fails for missing provenance or license files, duplicate/incomplete
manifest records, changed file counts, or checksum mismatches. `--print` uses
the same validation and prints current counts and checksums for a deliberately
updated snapshot. The checksum format and complete update workflow are defined
in `tests/data/UPDATING.md`.

