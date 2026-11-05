# Universal Configuration Loading and Merging Library Plan

## Goal

Build a dependency-free .NET configuration library that loads configuration from many local sources, normalizes it into one hierarchical value tree, merges sources with predictable precedence, exposes ergonomic typed reads, and remains implementable using the simplified C# syntax rules in `syntax.md`.

The implementation must use only built-in .NET APIs. No NuGet runtime dependencies, no LINQ, no properties, no records, no class inheritance, no generic type or method declarations, no attributes, no optional parameters, and no `try`/`catch`. Method-only interfaces that follow `syntax.md` may define focused boundaries. Only defined consumer API symbols are public; all implementation symbols are internal. Every public symbol requires XML documentation restricted to the constructs listed in `syntax.md`.

## Research Baseline

- Viper: defaults, explicit overrides, files, env vars, flags, buffers, key aliases, source precedence, case-insensitive keys, env key rewriting, and live reload are core ideas. Source: https://github.com/spf13/viper
- Microsoft.Extensions.Configuration: provider ordering is central; later providers override earlier providers. JSON, INI, XML, environment variables, command line, key-per-file, optional files, reload-on-change, sections, binding, and `__` environment hierarchy mapping are important patterns. Source: https://learn.microsoft.com/dotnet/core/extensions/configuration-providers
- Lightbend Config: immutable config objects, dependency-free core, merging with fallback, includes, substitutions, path access, duration/size parsing, type conversion, and strong diagnostics are important for mature file-centric configuration. Source: https://github.com/lightbend/config
- node-config: deployment environments, multiple config directories, local overrides, custom environment-variable mappings, command-line JSON overrides, hostname and instance-specific files, and clear precedence are important operational features. Source: https://github.com/node-config/node-config/wiki/Environment-Variables
- dotenv libraries: `.env` files are expected for local development, should not be committed with secrets, can load multiple files, may have safe/optional loading, and often support variable expansion. Sources: https://www.dotenv.org/docs/security/env.html and https://dotenv.readthedocs.io/en/latest/
- Dynaconf: environment switching, default environment fallbacks, includes, `.env` loading, env var auto-casting, secrets files, validators, and forced test environments are useful higher-level capabilities. Source: https://www.dynaconf.com/configuration/
- Rust `config`: defaults, layered sources, environment variables, string literals, files, manual overrides, live watching, and deep path access are common cross-language expectations. Source: https://docs.rs/config/latest/config/
- koanf: clean separation between providers, parsers, delimiters, strict merge mode, custom merge strategies, flattened path access, and unmarshalling are useful design lessons. Source: https://pkg.go.dev/github.com/knadh/koanf

## Critical Must-Have Direction

- Deterministic precedence must be explicit and testable.
- The core data model must represent null, booleans, numbers, strings, arrays, and objects without losing source information.
- Deep object merge must be predictable; scalar and array behavior must be configurable enough for common app needs.
- Source adapters must be small and independent from parsing so new sources can be added without disturbing the merge engine.
- Diagnostics must identify source name, path, line, column when available, and the config key involved.
- Env vars and command-line args must override files cleanly and support hierarchical keys.
- The public API must stay simple enough to use directly in small apps, while leaving room for advanced composition.

## Necessary Features From Low Level To High

### Value Model

- Config value kinds: missing, null, string, number, boolean, array, and object.
- Immutable loaded `Config` snapshots after build.
- Public mutable `ConfigMap` and `ConfigArray` containers for constructing in-memory inputs and inspecting copied values; snapshots protect their state by returning deep copies.
- Source metadata per value: source name, precedence rank, original key, line, and column when available.
- Stable string representation for diagnostics and tests.
- Case sensitivity mode: ordinal exact by default, optional case-insensitive keys.
- Configurable path delimiter, default `.`.
- Escaped path segments for keys containing the delimiter.
- Path normalization helpers for object traversal.
- Key alias table for migrations and backwards-compatible renames.

### Parsing

- JSON object parsing through `System.Text.Json`.
- JSON arrays and scalar roots when loading from strings or streams.
- INI parser implemented in-library.
- Java `.properties` parser implemented in-library.
- `.env` parser implemented in-library.
- Key-per-file loader for secret directories.
- Flat key-value parser for `key=value` streams.
- Optional minimal XML loader through built-in `System.Xml`.
- Parser selection by explicit format enum.
- Parser selection by file extension.
- Parse errors with source location where available.
- Duplicate key handling policy: error, last wins, or merge objects.
- UTF-8 file loading by default.
- Configurable text encoding.
- Comment handling for INI, properties, dotenv, and flat key-value formats.
- Basic quoted string unescaping for dotenv and properties.
- Optional dotenv variable expansion after base parsing.

### Sources

- In-memory source for defaults and tests.
- Programmatic override source.
- File source.
- Optional file source.
- Directory source that loads matching files in stable order.
- Stream source.
- String source.
- Environment variable source.
- Environment variable prefix filtering.
- Environment variable key normalization, including `__` to `.`.
- Environment variable allow-empty policy.
- Explicit environment variable mapping to config paths.
- Command-line source.
- Command-line switch mapping aliases.
- Command-line forms: `--key value`, `--key=value`, `-k value`, and `/key=value` where platform-appropriate.
- Key-per-file source for container and orchestrator secrets.
- Current working directory search source.
- Parent-directory search for `.env` or named config files.
- Environment-name source layering such as `appsettings.json` then `appsettings.Development.json`.
- Hostname or instance-name layering as a later feature.

### Merging

- Ordered source stack where later sources override earlier sources by default.
- Explicit precedence labels in diagnostics.
- Deep object merge.
- Scalar replacement.
- Null replacement policy.
- Array replacement by default.
- Optional array append policy.
- Optional strict merge mode that errors on incompatible types.
- Fallback merge mode similar to `withFallback`.
- Source-by-source merge trace for debugging.
- Conflict reporting.
- Missing required source reporting.
- Optional source groups for defaults, files, env, command line, and overrides.

### Resolution

- Path lookup.
- Section lookup.
- List child keys.
- Existence checks.
- Missing value support distinct from present null.
- Alias resolution.
- Optional substitutions such as `${path.to.value}`.
- Optional environment substitutions such as `${HOME}`.
- Optional substitution syntax for missing values.
- Cycle detection for substitutions.
- Substitution depth limit.
- Include processing for supported local file formats.
- Include cycle detection.
- Relative include paths based on including file.

### Type Conversion

- String reads.
- Boolean reads accepting common textual forms.
- Integer reads.
- Long reads.
- Decimal reads.
- Double reads.
- TimeSpan reads from invariant strings and friendly duration forms.
- Byte-size reads from friendly size forms.
- String array reads.
- Numeric array reads.
- Enum reads by name when target enum type is supplied through non-generic binder helpers.
- Culture-invariant conversion.
- Default value overloads.
- Required value methods that return diagnostics or throw explicit config exceptions.
- Conversion diagnostics that preserve source path and raw value.

### Binding And Validation

- Bind to objects with public fields using reflection.
- Bind nested objects with public parameterless constructors.
- Bind arrays and `List<T>` from .NET generic library types where feasible without declaring generic methods.
- Field name matching with configurable case sensitivity.
- Explicit key mapping table for fields when attributes are unavailable.
- Required key validation.
- Type validation.
- Range validation for numbers.
- Length validation for strings and arrays.
- Allowed values validation.
- Custom validation through registered named validator classes or delegates if syntax allows.
- Validation result object with all errors.
- Fail-fast and collect-all validation modes.

### Reload And Runtime Behavior

- Manual reload.
- File reload-on-change via `FileSystemWatcher`.
- Debounced reload.
- Snapshot replacement after successful reload.
- Preserve previous valid snapshot after failed reload.
- Change callbacks through simple callback registration.
- Thread-safe reads through immutable snapshots and locks around reload.
- Fresh-read option for selected keys.
- No automatic remote key-value stores in v1 because they require external systems and usually external dependencies.

### Writing And Export

- Export merged config to JSON.
- Export flattened key-value pairs.
- Safe write that refuses to overwrite existing files.
- Explicit overwrite write.
- Write only in-memory snapshots, not source-specific round-tripping in v1.
- Redacted diagnostics and export for secret-like keys.

### Security And Operations

- Secret key redaction based on configurable tokens such as `password`, `secret`, `token`, `key`, and `connectionstring`.
- `.env` guidance in docs that secrets should not be committed.
- Deterministic source names for logs.
- No network loading in v1.
- Clear exception types for load, parse, merge, conversion, validation, and reload errors.
- Built-in test fixtures for malformed input and secret redaction.

### Developer Ergonomics

- Single-call default loader for common app setup.
- Builder API for explicit source order.
- Static convenience methods for loading JSON, dotenv, env, and command line.
- Small API surface with overloads instead of optional parameters.
- Diagnostic dump for understanding final values and their sources.
- Documentation examples for console apps, services, tests, containers, and local development.

## Public API Completion Tasks

These tasks cover the remaining v1 feature gaps in the current `src/Api`
surface. Runtime implementation must use only built-in .NET APIs; span types
should be used for parsing and conversion work where they simplify bounded,
allocation-free input handling. The stated v1 non-goals remain out of scope.

- [DONE] T001: Expose parser policies for duplicate keys (error, last wins, and deep object merge), dotenv expansion, and missing expansion values, and make every text parser report malformed input through consistent source-aware diagnostics.
- [DONE] T002: Add general file-source entry points for explicit-format and extension-detected loading, including flat key-value files, and support caller-selected System.Text.Encoding across files, streams, directories, key-per-file values, and recursively included files while retaining UTF-8 defaults.
- [DONE] T003: Expose environment-source controls for keeping or ignoring empty values and for explicit environment-variable-to-config-path mappings, with deterministic behavior for process, ConfigMap, and Dictionary inputs.
- [DONE] T004: Add current-directory and bounded parent-directory search sources for named configuration files and dotenv files, with deterministic nearest-file selection, optional versus required behavior, and environment-name layering.
- [DONE] T005: Expose consumer path operations for delimiter cleanup, segment escaping and parsing, normalization, joining, and comparison so keys containing delimiters can be constructed without reproducing internal path rules.
- [DONE] T006: Add optional source-group labels for defaults, files, environment, command line, and overrides, and expand public merge traces to record each source rank, replacement or fallback decision, strict conflict, and final supplier.
- [DONE] T007: Make substitution and include resolution configurable for config-path lookup, environment fallback, optional missing values, required versus optional local includes, and depth limits, and surface missing references, cycles, depth exhaustion, invalid include roots, and unsupported formats through diagnostics.
- [DONE] T008: Add culture-invariant numeric-array reads and non-generic enum reads supplied with System.Type, including ordinary, fallback, and required forms with consistent scalar and array conversion rules.
- [DONE] T009: Add exception-less typed read operations that distinguish missing, null, and malformed values while returning source-aware diagnostics, and make required typed reads reject null and conversion failures instead of returning type defaults.
- [DONE] T010: Complete field binding for supported scalars, enums, nested objects, arrays, and List<T>, honor configured case matching and explicit field maps throughout nested collections, and report every unsupported target or failed conversion instead of silently preserving defaults.
- [DONE] T011: Extend validation with array-length rules and application-defined named validators through a focused public interface, and expose direct validity, error-count, and independent-copy inspection on validation results and rules.
- [DONE] T012: Add an explicit fresh-read facility for selected keys or sources that does not mutate an immutable Config snapshot and coordinates safely with reload-controller snapshot replacement.
- [DONE] T013: Add dependency-free consumer convenience entry points for one-call default application loading and direct JSON, dotenv, environment, and command-line loading, using policy-compliant instance APIs rather than public static methods.
- [DONE] T014: Add public configuration exception types for load, parse, merge, conversion, validation, and reload failures, preserve source name, config path, line, and column when available, and use them consistently for throwing API paths while retaining diagnostic alternatives for frequent failures.
- [DONE] T015: Add public-contract tests for ConfigValue, ConfigValues, ConfigMap, ConfigArray, path operations, aliases, case modes, source metadata, deterministic display text, deep-copy boundaries, immutable snapshots, and every corresponding interface member.
- [DONE] T016: Add public builder and source tests covering every format and root shape, explicit and detected formats, strings, streams, encodings, duplicate policies, includes, directory ordering, key-per-file, search sources, optional and required inputs, environment controls, command-line forms, switch maps, custom IConfigSource implementations, and conventional source stacks.
- [DONE] T017: Add public merge tests covering source precedence, source groups, deep objects, scalar replacement, null policies, array replacement and append, strict conflicts, fallback behavior, complete source-by-source traces, metadata preservation, and non-object roots.
- [DONE] T018: Add public resolution and read tests covering escaped paths, sections and children, aliases, substitutions, environment fallback, missing references, cycles, depth limits, all scalar and array conversions, enum reads, defaults, required reads, exception-less results, diagnostics, and configuration exceptions.
- [DONE] T019: Add public binding and validation tests covering new and existing targets, nested objects, arrays, List<T>, enums, case matching, field maps, unsupported targets, conversion failures, required and type rules, numeric ranges, string and array lengths, allowed values, custom validators, collect-all, fail-fast, and validation-result inspection.
- [DONE] T020: Add public diagnostics, export, writing, and redaction tests covering all collection accessors and invalid indexes, deterministic and redacted display, JSON and flat output, copied return values, safe-create refusal, explicit overwrite, secret-token matching, and custom replacement text.
- [DONE] T021: Add public reload tests covering manual and watched reload, start and stop state, debounce, duplicate file-system events, callback ordering, last diagnostics, successful atomic replacement, preservation after failed reload, custom sources, fresh reads, and concurrent reads during reload.
- [DONE] T022: Collect line and branch coverage for src/Api and add public-consumer tests until every reachable constructor, method, enum value, data field behavior, success path, failure path, and boundary branch exposed by the public layer is exercised.
