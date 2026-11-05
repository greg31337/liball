# unicode

Initial scaffold created on 2026-08-03.

## Repository Policy

Runtime code uses the file-scoped namespace `unicode;` in every source file
under `src/`. Public API names use PascalCase for classes, interfaces, enums,
enum members, fields, and methods. Method parameters and local variables use
camelCase. Constants use PascalCase when they are public API members.
The `public` modifier is reserved for the supported consumer API. Runtime
implementation types and members, including generated lookup tables, use
`internal` visibility.

## Supported Unicode Version

The runtime data baseline is Unicode 17.0.0. The runtime exposes this through
the static metadata fields on `UnicodeVersion` and the instance operations on
`UnicodeData`, including provenance records for the Unicode version page, UCD
files, and emoji data files.

The pinned data source URLs are:

- `https://www.unicode.org/versions/Unicode17.0.0/`
- `https://www.unicode.org/Public/17.0.0/ucd/`
- `https://www.unicode.org/Public/17.0.0/ucd/emoji/`

## Runtime Dependencies

The runtime library targets .NET 10.0 through `net10.0`. Runtime code must use
only built-in .NET APIs plus project-owned Unicode tables committed in this
repository. The runtime project must not include runtime `PackageReference`
entries.

Test projects and development tools may depend on test, parser, and generator
packages when those dependencies do not flow into the runtime package.

## Runtime Syntax Restrictions

Runtime source under `src/` follows the simplified C# subset in `syntax.md`.
That subset is intentionally stricter than normal C#: classes, enums, and
method-only interfaces; file-scoped namespaces only; no properties, attributes,
structs, LINQ, `ref`/`out`/`params`, or `try`; and only the modifiers allowed by
the syntax policy. Interfaces are reserved for genuine behavioral or input-source
boundaries. Every public type, constructor, method, field, enum, enum member, and
interface method has XML documentation; internal implementation symbols do not.

## Public API Boundaries

Public runtime declarations live under `src/Api/` and are separated into
contracts, field-based data classes, static metadata, enums, exceptions, and
interface-backed implementation classes. Unicode operations are instance
members on implementation classes and are also declared by matching interfaces,
so applications can construct the built-in implementation directly or depend on
the contract:

- `IUnicodeIdentifierProfile` lets applications define identifier preparation,
  comparison, and validation policy. `UnicodeIdentifierProfile` is the built-in
  constructor-configured implementation.
- `IUnicodePdfGlyphMap` lets font tooling provide mapping snapshots directly to
  CMap generation, validation, and diagnostics. `UnicodePdfGlyphMap` remains the
  built-in in-memory implementation.
- Operation contracts such as `IUnicodeNormalization`, `IUnicodeSegments`, and
  `IUnicodeSecurity` describe the corresponding built-in implementation classes.
- `IUnicodeProfiles` exposes instance-based factories for the built-in identifier
  and PRECIS-style profile configurations.

Public operations accept these interfaces, so tests and applications can use
small purpose-built implementations without subclassing library classes.

Project-owned generated runtime source lives under `src/Generated/` and uses
the same namespace, target framework, runtime dependency, and simplified
syntax rules as handwritten runtime source. Generated table classes and their
members are internal implementation details. Generated tables must be
deterministic, committed as C# source, and regenerated only from the pinned
Unicode data baseline documented in `todo.md`.

## Unicode Release Checklist

When updating this library to a future Unicode version:

- Update `UnicodeVersion` constants, README version text, and pinned data
  directory names together.
- Download the new Unicode Character Database, emoji data, and security data
  files into a versioned `data/UnicodeX.Y.Z/` directory.
- Record and verify checksums in the versioned data manifest before generating
  source.
- Run `dotnet run --project tools/UnicodeDataGenerator -- write-core-tables
  data/UnicodeX.Y.Z src/Generated` and review deterministic generated-source
  diffs.
- Regenerate conformance and regression fixtures that depend on Unicode data,
  including normalization, casing, segmentation, identifier, security, emoji,
  and PDF table coverage.
- Confirm generated runtime source still follows `syntax.md` and does not add
  runtime package dependencies.
- Run `dotnet test unicode.slnx` and resolve all failures before updating the
  baseline version in a release.
