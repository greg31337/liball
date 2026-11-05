# Universal Version Library Development Plan

## Goal

Build a dependency-free C# library for parsing, normalizing, comparing, sorting, matching, and selecting versions and version ranges across common package ecosystems. The core should be SemVer 2.0.0-compatible, with explicit dialect modes for ecosystem-specific behavior instead of one ambiguous parser.

Implementation must use only built-in .NET APIs and must conform to `syntax.md`: classes, enums, and restricted interfaces; only `static`, `public`, `internal`, and `const` modifiers; and no structs, properties, records, operators, extension methods, LINQ, class inheritance, attributes, `var`, `try/catch`, named arguments, optional parameters, or generic type/method declarations. Interfaces contain instance methods only and use names beginning with `I` followed by an uppercase letter; classes may implement interfaces. Only supported API symbols are `public`; implementation types are explicitly `internal`. Public API types and members require XML documentation using only the elements allowed by `syntax.md`.

## Research Notes

- SemVer 2.0.0 defines strict `MAJOR.MINOR.PATCH`, optional prerelease and build metadata, numeric prerelease ordering, ASCII lexical prerelease ordering, and build metadata ignored for precedence: https://semver.org/
- node-semver provides strict and loose parsing, cleaning, coercion, comparison helpers, increments, sorting, primitive comparators, whitespace AND ranges, `||` OR ranges, hyphen ranges, x/wildcard ranges, partial ranges, tilde ranges, caret ranges, prerelease gating, range simplification, subset/intersection helpers, and min/max satisfying selection: https://www.npmjs.com/package/semver
- Maven `ComparableVersion` supports mixed dot, hyphen, underscore, digit-letter split points, unlimited components, integer/string/list comparison, case-insensitive known qualifier order, unknown qualifier lexical order, and trailing null trimming: https://maven.apache.org/ref/3-LATEST/apidocs/org/apache/maven/artifact/versioning/ComparableVersion.html and https://maven.apache.org/pom.html
- Maven ranges use interval notation such as `[1.0,2.0)`, open-ended bounds, exact bounds, and comma-separated unions/exclusions: https://maven.apache.org/pom.html
- NuGet follows SemVer 2.0.0 in current clients but accepts 1 to 4 numeric components, normalizes missing components to zero, omits zero revision in normalized form, removes build metadata for normalized package identity, uses case-insensitive prerelease comparison, supports interval ranges, and supports floating versions with `*`: https://learn.microsoft.com/en-us/nuget/concepts/package-versioning and https://learn.microsoft.com/en-us/nuget/concepts/dependency-resolution
- Go module semver requires a leading `v`, permits `vMAJOR` and `vMAJOR.MINOR` shorthands, exposes canonicalization, major extraction, prerelease/build extraction, compare, validity checks, and sorting: https://pkg.go.dev/golang.org/x/mod/semver
- Cargo uses SemVer requirements with default caret compatibility, tilde requirements, wildcards, comparison requirements, comma-separated intersections, and prerelease exclusion unless prerelease is explicitly requested: https://doc.rust-lang.org/cargo/reference/specifying-dependencies.html
- RubyGems supports comparable numeric/alphanumeric versions, prerelease detection, operators `=`, `!=`, `>`, `<`, `>=`, `<=`, and pessimistic `~>` requirements: https://ruby-doc.org/3.3/stdlibs/rubygems/Gem/Version.html and https://docs.ruby-lang.org/en/3.3/Gem/Requirement.html
- Python packaging/PEP 440-style specifiers provide comma-separated specifier sets, prerelease policy controls, filtering over candidates, `!=` exclusions, compatible release `~=`, wildcard equality, unsatisfiable detection, subset/superset/disjoint checks, and set algebra views: https://packaging.pypa.io/en/stable/specifiers.html

## Feature List, Low Level To High Level

### Text And Token Infrastructure

- ASCII character classification for digits, letters, hyphen, dot, plus, underscore, star, x/X, v/V, brackets, parentheses, commas, pipes, comparison operators, and whitespace.
- Cursor-based scanner for deterministic parsing without regular-expression dependence in core paths.
- Token model for numeric identifiers, text identifiers, separators, operators, wildcard markers, range delimiters, and end-of-input.
- Parse diagnostics with error code, message, source text, index, line, and column.
- Result classes for parse success/failure that avoid `out` parameters and exceptions as the normal control path.
- Overflow-safe numeric comparison for unbounded version numbers using string length/digit comparison or built-in .NET numeric helpers where appropriate.
- Case-sensitive and case-insensitive ordinal comparison helpers.
- Canonical join/split helpers for numeric components, prerelease identifiers, build identifiers, qualifiers, and range clauses.

### Version Model

- Immutable-by-convention `UniversalVersion` class with public methods and constructor-controlled fields.
- Original input retention for diagnostics and round-tripping.
- Normalized string retention or lazy construction through ordinary methods.
- Numeric core components with support for variable component counts and optional fourth revision component.
- Prerelease identifier sequence with numeric/text classification.
- Build metadata identifier sequence retained separately from precedence.
- Dialect tag identifying which parser/comparer produced the version.
- Stable/prerelease detection.
- Build metadata detection.
- Missing component count tracking for partial versions and shorthand inputs.
- Leading `v` prefix tracking for Go/node-compatible behavior.
- Revision tracking for NuGet/System.Version-compatible versions.
- Maven token tree/list representation for comparable non-SemVer versions.
- RubyGems-style alphanumeric segment representation.
- PEP 440-compatible representation placeholder for epochs, release segments, pre/dev/post/local labels if that dialect is added after the SemVer-family core.

### Version Parsing And Normalization

- Strict SemVer 2.0.0 parser.
- SemVer loose parser for leading `v`/`=`, whitespace, and common not-quite-strict inputs.
- SemVer coercion from surrounding text into a valid SemVer tuple.
- Partial SemVer parser for range grammar inputs.
- Go module parser requiring `v`, plus canonicalization that fills missing minor/patch and ignores build for canonical equality.
- NuGet parser accepting 1 to 4 numeric components plus prerelease/build labels.
- NuGet normalization that removes leading zeroes, fills omitted numeric parts, omits zero revision, and removes build metadata from normalized identity.
- Maven parser for arbitrary version text with separator-aware tokenization, digit-letter boundaries, known qualifier aliases, and null-item trimming.
- RubyGems parser for dot-separated numeric and alphanumeric segments.
- PEP 440 parser as a later dialect stage, separated from SemVer parsing to avoid accidental behavior blending.
- Validation modes: strict, loose, coerce, and dialect-specific.
- Canonical formatting separate from display/original formatting.

### Version Comparison

- SemVer precedence comparison.
- SemVer identity comparison with build metadata considered.
- SemVer normalized/package identity comparison with build metadata ignored.
- Numeric core comparison with arbitrary component counts and zero padding where the dialect requires it.
- Prerelease comparison with numeric identifiers lower than text identifiers for SemVer.
- Case-insensitive prerelease comparison for NuGet.
- Maven comparison with integer/string/list/null rules, qualifier ordering, aliases, and separator importance.
- RubyGems comparison with alphanumeric splitting and prerelease ordering.
- Go comparison where invalid versions sort below valid versions if using Go-compatible helper behavior.
- Sort helpers for ascending and descending order.
- Min/max helpers.
- Stability-aware comparison or selection helpers where ecosystems prefer stable versions.

### Range Model

- Primitive comparator model for `<`, `<=`, `>`, `>=`, `=`, and `!=`.
- Comparator set model for AND/intersection.
- Range union model for OR/disjunction.
- Interval model with inclusive/exclusive lower and upper bounds.
- Exclusion model for `!=` and interval holes.
- Empty range and universal range representations.
- Floating pattern model for NuGet-style `*`.
- Prerelease policy model: default, include, exclude, explicit-only, auto-if-no-stable-candidate.
- Range diagnostics that identify which clause failed to parse.
- Normalized range string output.

### Range Parsing

- npm/node-semver primitive comparators.
- npm/node-semver whitespace AND and `||` OR.
- npm/node-semver hyphen ranges.
- npm/node-semver x-ranges and wildcards using `x`, `X`, and `*`.
- npm/node-semver partial ranges.
- npm/node-semver tilde ranges.
- npm/node-semver caret ranges with special `0.x` compatibility behavior.
- npm/node-semver prerelease gating and `includePrerelease` option.
- Cargo comma-separated intersections.
- Cargo default caret behavior for bare version requirements.
- Cargo tilde, wildcard, comparison, and prerelease exclusion behavior.
- NuGet interval ranges: `[1.0]`, `[1.0,)`, `(,1.0]`, `[1.0,2.0)`, and related forms.
- NuGet floating versions: `*`, `6.*`, `6.0.*`, `*-*`, `1.1.*-*`, prerelease floating labels.
- Maven interval ranges and comma-separated unions/exclusions.
- RubyGems requirements including `~>` pessimistic constraints and `!=` exclusions.
- PEP 440-style specifier sets including comma intersection, `~=`, `==`, `!=`, wildcard equality, and prerelease controls in a later stage.

### Range Algebra And Matching

- `Contains` for a parsed version object.
- `ContainsText` for parse-and-check convenience.
- Intersection of ranges.
- Union of ranges.
- Difference and complement for exclusion-heavy ranges.
- Unsatisfiable detection.
- Subset, superset, and disjoint checks.
- Bound normalization and redundant comparator removal.
- Minimum possible version for a range when representable.
- Range simplification against a known finite candidate list.
- Explanation API that reports why a version did or did not satisfy a range.

### Candidate Selection

- Filter candidate versions by range.
- Sort candidate versions under a selected dialect/comparison mode.
- Minimum satisfying candidate.
- Maximum satisfying candidate.
- Best stable candidate.
- NuGet-style lowest applicable version selection.
- NuGet-style floating version highest matching selection.
- npm-style min/max satisfying selection.
- Prerelease-aware candidate filtering where prereleases are excluded unless explicitly allowed or no stable version is available, depending on dialect.

### Public API Usability

- Small facade classes for common dialects: SemVer, NodeSemVer, NuGetVersions, MavenVersions, GoVersions, CargoVersions, RubyGemsVersions, and PythonVersions.
- Shared universal APIs for callers that want dialect selection by enum.
- Parse options class.
- Range parse options class.
- Comparison options class.
- Selection options class.
- Diagnostic/result classes for non-throwing calls.
- Throwing parse helpers for callers that prefer fail-fast behavior.
- Formatting helpers for original, normalized, canonical, and dialect display forms.
- Equality helpers that make precedence equality, identity equality, and normalized package identity explicit.

### Testing And Quality

- Syntax and API-policy conformance tests must continue to pass, including explicit internal visibility for implementation details and allowed XML documentation on every public symbol.
- Golden SemVer precedence examples from the SemVer specification.
- node-semver compatibility fixture set for ranges, prerelease gates, coercion, and increments.
- Maven qualifier and token comparison examples from Maven documentation.
- NuGet normalization, interval, floating, stable/pre-release selection, and revision-component tests.
- Go leading-`v`, shorthand, canonical, and invalid comparison tests.
- Cargo caret/default, tilde, wildcard, comma intersection, and prerelease behavior tests.
- RubyGems comparison and `~>` requirement tests.
- PEP 440 parser/specifier tests when that dialect is implemented.
- Fuzz-style parser tests for invalid inputs, empty components, very large numeric identifiers, and odd separators.
- Round-trip tests for parse, normalize, and display.
- Range algebra property tests over fixed generated candidate sets.
- No external package dependencies in `src`.

## Open Tasks

- [DONE] T001: Add a public version-inspection contract implemented by `VersionService` for stable/prerelease and build detection, component and identifier access, dialect and shorthand metadata inspection, and explicit precedence, identity, and normalized-identity equality using only built-in .NET APIs.
- [DONE] T002: Add complete public-API tests for version inspection and equality, including defensive array behavior, missing and revision components, leading `v`, Python metadata, very large numeric identifiers, and all formatting modes.
- [DONE] T003: Add a public implementation of `IVersionSyntaxInspection` that exposes tokenization with source locations and interval-to-comparator conversion without exposing scanners or parser engines.
- [DONE] T004: Add complete public-API tests for tokenization and interval conversion across all token kinds, whitespace and line tracking, inclusive, exclusive, unbounded, exclusion-hole, malformed, and boundary cases.
- [DONE] T005: Extend the shared public comparison and selection contracts and `VersionService` with ascending and descending sorting, minimum, maximum, and stable-aware ordering for parsed and textual candidates under explicit dialect and comparison options.
- [DONE] T006: Add complete public-API tests for shared sorting and extrema across every dialect, precedence and identity modes, case policies, invalid-version ordering, prereleases, equal-precedence build variants, empty inputs, and unbounded numeric components.
- [DONE] T007: Add a public range-operations contract and implementation for empty and universal ranges, parsed-version and text membership, explicit prerelease handling, and structured match explanations.
- [DONE] T008: Add complete public-API tests for range construction, membership, prerelease gating, floating patterns, exclusions, match explanations, null inputs, and diagnostic propagation across every supported range syntax.
- [DONE] T009: Expose range intersection, union, difference, complement, simplification, finite-candidate simplification, unsatisfiable detection, subset, superset, and disjoint checks through the public range-operations implementation.
- [DONE] T010: Add complete public-API example and fixed-candidate property tests for every range-algebra operation, including empty, universal, touching, open and closed bound, exclusion-hole, disjunction, contradictory, and prerelease cases.
- [DONE] T011: Implement public normalized range formatting and minimum-possible-version queries for every supported range syntax, preserving dialect comparison semantics and returning an explicit no-value result when no minimum is representable.
- [DONE] T012: Add complete public-API tests for normalized range output, parse-format-parse equivalence, redundant comparator removal, dialect-specific floating and wildcard forms, and representable and unrepresentable minima.
- [DONE] T013: Add a public SemVer operations contract and implementation for strict and loose parsing, coercion, precedence comparison, identity comparison, normalized package identity, canonical formatting, and fail-fast and diagnostic result paths.
- [DONE] T014: Add complete public SemVer operation tests from the SemVer 2.0.0 examples and grammar, including precedence chains, build identity, leading zero rejection, loose/coerce boundaries, round trips, oversized identifiers, and malformed input diagnostics.
- [DONE] T015: Expand `INodeSemVerOperations` and add its public implementation for valid and clean results, coercion, accessors, primitive and build-aware comparisons, diff, all increment modes, range helpers, sorting, and finite candidate selection.
- [DONE] T016: Add complete public node-semver operation tests from the upstream fixture behavior for input guards, loose parsing, coercion direction, comparison, diff, increment identifiers, advanced ranges, prerelease gates, outside and subset relations, sorting, and min/max satisfying selection.
- [DONE] T017: Add a public NuGet operations implementation covering version parsing, normalization, interval and floating range parsing, case-insensitive comparison, lowest-applicable and highest-floating candidate selection, package metadata parsing, target-framework compatibility, and the existing `INuGetResolution` contract.
- [DONE] T018: Add complete public NuGet operation tests for one-to-four-part versions, revisions, SemVer 2 labels, package identity normalization, intervals, floating prerelease patterns, source ordering, rejection policies, target frameworks, dependency groups, graph conflicts, and diagnostic failures.
- [DONE] T019: Add a public Maven operations implementation covering `ComparableVersion` parsing, normalization, comparison, interval unions and exclusions, candidate selection, repository and snapshot metadata, artifact version resolution, and the existing `IMavenResolution` contract.
- [DONE] T020: Add complete public Maven operation tests for separators, digit-letter boundaries, unlimited integers, qualifier aliases and ordering, null trimming, soft and hard requirements, interval unions and holes, metadata XML, snapshots, dependency mediation, exclusions, scopes, and diagnostic failures.
- [DONE] T021: Expand `IGoModuleVersioning` and add its public implementation for validity, parsing, canonicalization, major, prerelease and build extraction, comparison, ascending and descending sorting, module-path parsing, semantic import versioning, `+incompatible`, and finite candidate selection.
- [DONE] T022: Add complete public Go operation tests from the module and `x/mod/semver` references for shorthand and canonical forms, invalid ordering, extraction helpers, build handling, module path escaping, major suffixes, gopkg.in paths, semantic import mismatches, `+incompatible`, sorting, and selection.
- [DONE] T023: Expand `ICargoResolution` and add its public implementation for Cargo version parsing and comparison, caret/default, tilde, wildcard and comparison requirements, comma intersections, prerelease candidate filtering, manifest parsing, feature resolution, yanked and source policies, and finite dependency resolution.
- [DONE] T024: Add complete public Cargo operation tests from the Cargo reference for zero-major compatibility, requirement intersections, prerelease upgrades, build metadata, candidate selection, manifest dependency forms, features, targets, registries, source replacement, yanked versions, resolver modes, conflicts, and diagnostics.
- [DONE] T025: Expand `IRubyGemsResolution` and add its public implementation for version parsing, normalization, prerelease detection, comparison, all requirement operators and nested input forms, platform filtering and selection, source policies, and finite dependency resolution.
- [DONE] T026: Add complete public RubyGems operation tests from `Gem::Version` and `Gem::Requirement` for alphanumeric ordering, trailing zeros, default and prerelease requirements, pessimistic bounds, exclusions, nested requirements, platforms, sources, resolver backtracking, conflicts, and malformed inputs.
- [DONE] T027: Add a public Python operations contract and implementation for PEP 440 parsing, normalization, canonical and display formatting, comparison, specifier parsing, wildcard and compatible-release matching, prerelease policy, finite candidate filtering and selection, and range relations.
- [DONE] T028: Add complete public Python operation tests from PEP 440 and the current packaging specification for epochs, release padding, pre, post and dev releases, local versions, normalization aliases, ordering, every specifier operator, prerelease fallback, range algebra, selection, round trips, and invalid input diagnostics.
- [DONE] T029: Add public result and failure-path tests covering every exposed operation's null, empty, whitespace, malformed, over-length, invalid enum, missing-result, array-element, and exception-versus-diagnostic behavior.
- [DONE] T030: Add an end-to-end consumer test assembly without internals access that constructs every public service, invokes every public contract through representative success and failure paths, and verifies all planned functionality is reachable using only the built library.
