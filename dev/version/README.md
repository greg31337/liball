# version

Dependency-free C# library for parsing, comparing, matching, and selecting package versions across multiple ecosystem dialects.

## Status

Implemented dialect coverage includes strict and loose SemVer, node-semver ranges and helpers, NuGet versions/ranges/floating versions plus finite restore metadata, Maven versions/ranges/metadata/resolution helpers, Go module versions and module-path validation, Cargo requirements and finite resolver helpers, RubyGems versions/requirements/platform/resolver helpers, and Python/PEP 440 versions/specifiers/formatting.

Public facade classes are available for common entry points:

- `SemVer`
- `NodeSemVer`
- `NuGetVersions`
- `MavenVersions`
- `GoVersions`
- `CargoVersions`
- `RubyGemsVersions`
- `PythonVersions`

The public API consists of these facades, the shared version and range APIs, and the option, model, result, enum, and diagnostic-code types needed to exchange data with them. Concrete parsers, comparers, dialect-specific formatters, scanners, validators, and dependency-resolution engines are implementation details and are `internal`; use a facade or a shared API such as `UniversalVersion`, `VersionRange`, `VersionFormatter`, `VersionRangeAlgebra`, or `VersionSelector` instead. Every public symbol carries XML documentation, while internal symbols intentionally do not form a supported compatibility surface.

For consumers that use dependency injection, `VersionService` provides the same shared operations through five focused interfaces: `IVersionParsing`, `IVersionRangeParsing`, `IVersionComparison`, `IVersionSelection`, and `IVersionFormatting`. The static APIs remain the convenient entry points and are fully compatible; the interfaces provide narrow seams that application tests can replace with small fakes. Internally, dialect parsing, comparison, formatting, and range-syntax selection use interface-backed strategies so ecosystem-specific behavior is isolated behind one dispatch point.

Full test suite status:

```text
dotnet test version.slnx
Passed: 876, Failed: 0, Skipped: 0
```

## Compatibility Gaps

This library is not yet a drop-in replacement for the upstream package manager libraries. Current known gaps:

- npm/node-semver: parsing, coercion, valid/clean helpers, primitive comparators, `||`, hyphen, wildcard, tilde, caret, prerelease gating, comparison helpers, diff, incrementing, finite range algebra, and finite candidate selection are covered. Remaining limitations are around exact drop-in npm API parity, npm's full input guard surface, and APIs that require registry-scale or streaming behavior.
- NuGet: version parsing, normalization, interval ranges, floating versions, stable/floating candidate selection, nuspec metadata parsing, dependency graph resolution, source ordering, target framework filtering, and resolver diagnostics are covered for finite in-memory metadata. This is not a replacement for NuGet client restore, network feeds, lock files, or MSBuild integration.
- Maven: tokenization, qualifier ordering, version comparison, interval ranges, artifact coordinates, maven-metadata.xml parsing, snapshot timestamp resolution, dependency mediation, exclusions, optional dependencies, scope behavior, and resolver diagnostics are covered for finite in-memory metadata. Full Maven project-building, repository transport, plugin behavior, and parent/BOM edge cases beyond the modeled APIs remain outside scope.
- Go: module-version parsing, canonicalization, major/prerelease/build extraction, comparison, sorting, module path validation, semantic import versioning, and `+incompatible` handling are covered. Full Go proxy/module cache behavior and VCS lookup are outside scope.
- Cargo: caret/default, tilde, wildcard, comparison, comma-intersection, prerelease requirement behavior, manifest dependency parsing, feature resolution, candidate filtering, yanked versions, alternate registries, source replacement, target-specific dependencies, and finite dependency resolution are covered. Full Cargo workspace, lockfile, registry index, and resolver edge-case parity remain outside scope.
- RubyGems: version comparison, prerelease detection, default and nested requirement input forms, comparison requirements, `!=`, `~>`, platform matching, finite dependency resolution, source handling, and conflict diagnostics are covered. This is not a replacement for the RubyGems client, installed gem activation, remote sources, or all Bundler resolver behavior.
- Python/PEP 440: version parsing, comparison, specifier sets, compatible release, wildcard equality, exclusions, prerelease policy, candidate filtering, and dialect-native original/normalized/canonical/display formatting are covered. Remaining limitations are around acting as a complete packaging library replacement for every marker, wheel tag, environment, and distribution metadata API.
- Formatting: original/display formatting is preserved for parsed inputs, and normalized/canonical formatting now has dialect-native support for SemVer-family, NuGet, Maven, RubyGems, and Python. Remaining limitations are primarily dialects or metadata shapes not represented by the parsed model.
- Options: `VersionParseOptions`, `VersionRangeParseOptions`, `VersionComparisonOptions`, and `VersionSelectionOptions` use a single canonical constructor per class plus named factories, with explicit overloads on core and facade APIs. Options cover shared parsing, range parsing, comparison, prerelease, max length, and invalid-version ordering behavior; some ecosystem-specific flags still live on dedicated dialect option/resolver classes.
- Dependency policy: production source is audited to remain dependency-free, using only built-in .NET APIs and rejecting production `PackageReference`s, LINQ, NuGet client libraries, Maven resolver libraries, Cargo libraries, RubyGems libraries, and Python packaging libraries.
