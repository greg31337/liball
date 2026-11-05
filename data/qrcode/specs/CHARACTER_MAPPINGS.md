# Character mapping table derivation

The generated tables in `src/Internal/Tables/ShiftJisTable.cs` and
`src/Internal/Tables/Gb2312Table.cs` are reproducible with:

```sh
python3 specs/tools/generate_character_tables.py
```

The generator verifies every input against `specs/SHA256SUMS`-equivalent
hard-coded hashes before writing output. This prevents an upstream or local
mapping change from silently altering the library tables.

## Shift JIS

The standard layer contains all 7,037 defined mappings in Unicode's historical
`SHIFTJIS.TXT`, including the single-byte repertoire. The separate vendor
layer is the delta from Microsoft's CP932 table: 854 double-byte additions or
code positions whose CP932 Unicode value differs from the standard layer.
Unassigned CP932 single-byte positions are not treated as extensions.

For example, standard Shift JIS maps `0x8160` to U+301C, while CP932 maps that
same code to U+FF5E; both values remain visible through their respective
layers. CP932-only `0x8740` to U+2460 is present only in the vendor layer.

## GB2312

The standard layer contains all 7,445 mappings in Unicode's historical
GB2312-80 table. The source expresses 7-bit row/cell codes, so the generator
adds `0x8080` to store the EUC-CN byte pair used by QR Hanzi processing. The
separate vendor layer is the 14,348 double-byte CP936 additions or conflicting
code positions. Single-byte ASCII/control mappings from CP936 are excluded
because they are not members of the GB2312 graphic-character table.

For example, standard GB2312 maps `0xA1A4` to U+30FB, while CP936 maps that
same code to U+00B7; CP936-only `0x8140` to U+4E02 is present only in the
vendor layer.

## Lookup behavior

Forward standard and vendor lookups are independent. A combined decode lookup
uses a vendor conflict when vendor mappings are enabled, then falls back to
the standard mapping. A combined encode lookup prefers a standard encoding
and uses the vendor table only when the Unicode value has no standard
encoding. Reverse lookup resolves duplicate Unicode values to the numerically
smallest encoded value within the selected layer.
