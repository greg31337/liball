# GenerateTables

`GenerateTables` is the offline, dependency-free source generator for committed UniversalHtml lookup data. Normal library builds never run it and never access the network.

## Commands

Run commands from the repository root:

```sh
dotnet run --project tools/GenerateTables -- --check
dotnet run --project tools/GenerateTables -- --verify
dotnet run --project tools/GenerateTables -- --write
```

- `--check` verifies every pinned input checksum and fails when committed generated output differs. It is the default and does not write files.
- `--verify` verifies pinned input checksums without comparing output.
- `--write` verifies inputs and atomically replaces known generated outputs.

All paths come from `inputs/manifest.json`, are resolved beneath the repository root, and cannot escape it. Output is ordinally sorted UTF-8 without a BOM, uses LF line endings, and contains no timestamp or machine-specific path.

## Updating inputs

1. Obtain data from the upstream URL and revision recorded by the relevant input file.
2. Normalize it into the committed input format without deleting provenance or license information.
3. Compute `shasum -a 256 <input>` and update the lowercase checksum in `inputs/manifest.json`.
4. Run `--write`, inspect the generated diff and header, then run `--check` and the complete test suite.

A checksum change is an explicit review boundary. Never update a checksum merely to silence a mismatch; verify the changed data and its provenance first.
