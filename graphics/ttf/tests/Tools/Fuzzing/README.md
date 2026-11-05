# Font fuzzing entry points

This test-only executable exposes independent correctness and security targets
for container loading, face inspection, glyph outlines, shaping, variable-font
instancing, subsetting, and PDF font-resource finalization. It does not measure
performance.

Build it with:

```sh
dotnet build tests/Tools/Fuzzing/ttf.Fuzzing.csproj
```

The `container-load` target consumes a font container directly. Every deeper
target consumes a 32-byte control envelope followed by the font container. Use
`--raw-font` to add a zeroed envelope to a seed font during smoke testing:

```sh
dotnet run --project tests/Tools/Fuzzing/ttf.Fuzzing.csproj -- \
  glyph-outline --raw-font tests/Assets/ColorFonts/samples-sbix.ttf
```

For fuzzing, pass mutated input on standard input or as the final file argument:

```sh
dotnet run --project tests/Tools/Fuzzing/ttf.Fuzzing.csproj -- \
  container-load < candidate.ttf
```

Available target names are:

- `container-load`
- `face-inspection`
- `glyph-outline`
- `shaping`
- `instancing`
- `subsetting`
- `pdf-finalization`

Inputs larger than 2 MiB are skipped. Expected format, resource-limit, and
embedding-policy failures are swallowed. Any other exception escapes the
process so a coverage-guided runner can retain and minimize the input. Keep
separate corpora for each target because only `container-load` omits the
control envelope.
