# Loading and inspection

## Choose a load policy

Load a file with an explicit profile. A package owns its input bytes, so no
stream or file handle remains open after this call.

```csharp
FontLoadOptions options = new FontLoadOptions();
FontLoader loader = new FontLoader();
FontPackage package = loader.LoadFromFile("Example.ttf", options);

for (int index = 0; index < package.GetFaceCount(); index += 1)
{
    FontFace face = package.GetFace(index);
    FontDescription description = face.GetDescription("en");
    FontNameValue fullName = description.GetNames().GetFullName();
    System.Console.WriteLine(fullName == null ? "(unnamed)" : fullName.GetValue());
}
```

Use `CreateStrict()` when any normative structural or checksum error must fail.
Use `CreateInspectOnly()` for bounded forensic inspection of fonts that may not
be safe for semantic operations. Compatible mode is the normal ingestion
profile: it never relaxes resource bounds, and all accepted repairs appear in
`package.GetDiagnostics()`.

`Load(Stream, ...)` starts at the stream's current position, reads to its end,
and leaves it open. Array and span overloads copy. `ReadOnlyMemory<byte>` also
copies unless a custom option explicitly selects
`FontInputOwnership.RetainReadOnlyMemory`; that trusted mode requires the caller
to keep the memory alive and unchanged.

## Treat packages as containers

An SFNT or WOFF package normally has one face; a TTC/OTC package can have many.
Always select a face explicitly. `CopyOriginalBytes()` returns the original
container, while `CopySfntBytes(faceIndex)` returns the selected face as SFNT
bytes. Both return caller-owned copies.

`FontPackage`, `FontFamily`, and `FontSet` implement `IFontFaceCollection`, so
code that only enumerates faces can accept that interface instead of selecting a
concrete owner. `FontSet.Add(IFontFaceCollection)` snapshots any implementation
before changing the set; this supports application-defined collections and keeps
failed reads atomic. The existing `Add(FontPackage)` overload remains available.

```csharp
void PrintFaces(IFontFaceCollection collection)
{
    int count = collection.GetFaceCount();
    for (int index = 0; index < count; index += 1)
    {
        FontNameValue name = collection.GetFace(index).GetNames().GetFullName();
        System.Console.WriteLine(name == null ? "(unnamed)" : name.GetValue());
    }
}
```

Diagnostics are stable data, not formatted log messages:

```csharp
FontDiagnostic[] diagnostics = package.GetDiagnostics();
for (int index = 0; index < diagnostics.Length; index += 1)
{
    FontDiagnostic item = diagnostics[index];
    System.Console.WriteLine(
        item.GetCode() + ": " + item.GetSeverity() + ": " + item.GetMessage());
}
```

Branch on `GetCode()` and `GetSeverity()` in automation. Messages are intended
for people and may contain input context.

## Inspect semantic data

`FontFace` is the entry point for names, classification, coverage, metrics,
embedding rights, named instances, and glyph access through an instance.

```csharp
FontFace face = package.GetFace(0);
FontNames names = face.GetNames("en");
FontClassification classification = face.GetClassification();
FontCoverage coverage = face.GetCoverage();

FontNameValue familyName = names.GetFamilyName();
System.Console.WriteLine(familyName == null ? "(unnamed)" : familyName.GetValue());
System.Console.WriteLine(classification.GetWeightClass());
System.Console.WriteLine(coverage.Covers(0x1F600));

FontInstance instance = face.CreateDefaultInstance();
GlyphOutline outline = instance.GetGlyphOutline(12);
GlyphMetrics metrics = instance.GetGlyphMetrics(12);
```

Coverage is represented as merged scalar ranges; use `Covers` instead of
expanding all ranges. Glyph zero is the missing-glyph entry, not evidence that a
character mapped successfully.

## Inspect raw tables

Raw access is useful for pass-through metadata and diagnostics:

```csharp
if (face.HasTable(FontTags.Name))
{
    FontTableInfo info = face.FindTableInfo(FontTags.Name);
    byte[] nameTable = face.CopyTable(FontTags.Name);
    System.Console.WriteLine(info.GetLength() == nameTable.Length);
}
```

`CopyTable` requires raw-table preservation at load time and returns a copy.
Presence does not imply that the library supports a table's semantics. Prefer
semantic accessors for rendering or output decisions, because they apply bounds,
normalization, variation, and compatibility policies.

Catch `FontFormatException` for invalid or unsupported required semantics,
`FontLimitException` for configured resource bounds, and
`OperationCanceledException` for cancellation. Never continue an output
operation from a partially trusted inspect-only result.
