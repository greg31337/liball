# Universal Identifier Library Development Plan

## Intent

Build a dependency-free .NET identifier library for UUIDs and related universal ID formats. The first priority is correct parsing, formatting, byte layout, generation, and timestamp extraction for widely used formats. The second priority is ergonomic APIs that still fit the simplified C# syntax policy in `syntax.md`.

Implementation must use only built-in .NET libraries. Useful built-ins include `System.Guid`, `System.DateTimeOffset`, `System.Security.Cryptography.RandomNumberGenerator`, `System.Security.Cryptography.MD5`, `System.Security.Cryptography.SHA1`, `System.Text.Encoding`, `System.Buffers.Binary.BinaryPrimitives`, and basic collections.

## Syntax Constraints To Respect

- Use file-scoped namespaces.
- Use classes, enums, and narrowly scoped interfaces; no structs, records, attributes, extension methods, properties, operators, class inheritance, or LINQ.
- Interfaces contain instance methods only, use an `I` followed by an uppercase letter, do not inherit other interfaces, and do not provide default implementations, fields, static abstract members, or generic methods.
- Use only `public`, `static`, `internal`, and `const` modifiers. Mark only the supported consumer-facing API as `public`; implementation types and members are `internal`.
- Add XML documentation to every public type, constructor, method, field, enum, and enum member. Use only the XML elements allowed by `syntax.md`, and do not add XML documentation to internal symbols.
- Avoid `private`, `readonly`, `sealed`, `virtual`, `override`, `out`, `ref`, `in`, `params`, default parameters, generic declarations, and generic methods.
- Prefer plain static helper methods and simple result classes over broad interfaces, operators, and `TryParse(..., out value)` patterns. Use interfaces where multiple implementations share a consumed contract or an implementation dependency needs a deterministic test double.
- Use explicit local types instead of `var`.
- Use ordinary block-bodied methods only.

## Research Notes

- RFC 9562 is the current UUID standard. It defines UUID versions 1, 3, 4, 5, 6, 7, and 8, plus Nil and Max UUID values. UUIDv7 uses a 48-bit Unix epoch millisecond timestamp followed by version, variant, and 74 bits of random or monotonic data. The RFC recommends UUIDv7 over v1 and v6 when possible.
- The `uuid` JavaScript package exposes a useful baseline API shape: constants for Nil and Max, `parse`, `stringify`, `validate`, `version`, generators for v1/v3/v4/v5/v6/v7, and converters between v1 and v6.
- .NET now exposes `Guid.CreateVersion7()` and `Guid.CreateVersion7(DateTimeOffset)`, showing UUIDv7 as a mainstream .NET scenario. This project can still implement its own version to keep consistent byte-order handling, monotonic options, parsing, tests, and older-target portability if needed.
- ULID libraries and the canonical ULID spec emphasize 128-bit UUID compatibility, 26-character Crockford Base32 text, 48-bit Unix millisecond timestamps, 80 bits of randomness, lexicographic sortability, case-insensitive parsing, URL-safe output, monotonic generation inside the same millisecond, and overflow rejection.
- KSUID reference implementations emphasize a 160-bit layout: 32-bit seconds since a custom epoch, 128 random bits, 20-byte binary form, 27-character Base62 text, lexicographic sortability, and timestamp inspection.
- Snowflake implementations emphasize compact 64-bit numeric IDs with 41 timestamp bits, 10 machine or worker bits, and 12 sequence bits. Critical features include custom epoch support, clock-backward handling, per-millisecond sequence rollover handling, decoding, and string serialization to avoid JavaScript integer precision loss.
- MongoDB ObjectId provides a useful database-oriented format: 12 bytes, 4-byte big-endian Unix seconds timestamp, 5-byte process-unique random value, and 3-byte big-endian counter.
- Nano ID demonstrates a compact random string family: secure random generation, URL-safe default alphabet, configurable size and alphabet, uniform random character selection without modulo bias, and collision-risk guidance.

Sources:

- https://www.rfc-editor.org/info/rfc9562/
- https://github.com/uuidjs/uuid
- https://learn.microsoft.com/dotnet/api/system.guid.createversion7
- https://github.com/ulid/spec
- https://github.com/RobThree/NUlid
- https://pkg.go.dev/github.com/segmentio/ksuid
- https://docs.x.com/fundamentals/x-ids
- https://www.nuget.org/packages/SnowflakeGenerator
- https://github.com/mongodb/specifications/blob/master/source/bson-objectid/objectid.md
- https://github.com/ai/nanoid

## Feature Inventory, Low Level To High Level

### Low-Level Primitives

- Byte-array validation for fixed-width identifiers: 12, 16, 20, and 8 bytes.
- Big-endian integer read/write helpers for 16-bit, 24-bit, 32-bit, 48-bit, 64-bit, and selected 128-bit operations.
- UUID canonical hex parser and formatter with hyphen checks.
- Hex parser variants for compact 32-character UUID strings and optional URN prefix.
- Crockford Base32 encoder and decoder for ULID timestamps, randomness, and full ULID strings.
- Base62 encoder and decoder for KSUID.
- URL-safe alphabet handling for Nano ID style strings.
- Uniform random index generation for arbitrary alphabets without modulo bias.
- Secure random byte generation using built-in .NET cryptography.
- Timestamp conversion helpers for Unix milliseconds and Unix seconds.
- Simple comparison helpers for byte arrays and known identifier classes.
- Parse result classes to replace `TryParse(..., out value)`.
- Error messages and failure codes for invalid length, invalid character, invalid variant, invalid version, timestamp overflow, random overflow, clock rollback, sequence overflow, and alphabet errors.

### UUID Features

- Represent a UUID as 16 canonical bytes.
- Constants for Nil UUID and Max UUID.
- Parse canonical UUID strings.
- Parse compact 32-character UUID strings.
- Parse `urn:uuid:` strings.
- Format canonical lowercase UUID strings.
- Format uppercase UUID strings.
- Format compact lowercase and uppercase UUID strings.
- Convert to and from `System.Guid`.
- Generate UUIDv4 using secure random bytes.
- Generate UUIDv7 using Unix epoch milliseconds and secure random bytes.
- Generate UUIDv7 from a supplied timestamp.
- Generate monotonic UUIDv7 values within the same millisecond.
- Extract UUID version.
- Extract UUID variant.
- Validate RFC 9562 variant bits.
- Validate version-specific UUID strings.
- Generate name-based UUIDv3 with built-in MD5 for compatibility.
- Generate name-based UUIDv5 with built-in SHA-1.
- Provide predefined namespace UUIDs for DNS, URL, OID, and X.500.
- Convert UUIDv1 byte layout to UUIDv6 byte layout.
- Convert UUIDv6 byte layout to UUIDv1 byte layout.
- Extract timestamps from UUIDv1, UUIDv6, and UUIDv7.
- Detect Nil and Max UUID values.
- Provide deterministic constructors from bytes for tests.
- Provide byte-copy APIs that do not expose mutable internal state.

### ULID Features

- Represent a ULID as 16 bytes in network byte order.
- Generate random ULID values with current Unix millisecond timestamp.
- Generate random ULID values from a supplied timestamp.
- Generate monotonic ULID values within the same millisecond.
- Parse canonical 26-character Crockford Base32 strings.
- Accept lowercase input and normalize output to uppercase.
- Reject ULID strings that decode above 128 bits.
- Format canonical 26-character strings.
- Extract timestamp as Unix milliseconds and `DateTimeOffset`.
- Extract randomness bytes.
- Convert to and from UUID bytes when caller wants raw 128-bit compatibility.
- Compare ULIDs lexicographically by binary or canonical text order.
- Detect overflow when monotonic randomness increments past 80 bits.

### KSUID Features

- Represent a KSUID as 20 bytes.
- Generate KSUID values with current adjusted epoch seconds and 128 random bits.
- Generate KSUID values from a supplied timestamp.
- Parse canonical 27-character Base62 strings.
- Format canonical 27-character Base62 strings.
- Extract adjusted timestamp seconds.
- Extract creation time as `DateTimeOffset`.
- Extract payload bytes.
- Compare KSUIDs lexicographically by binary or text order.
- Validate timestamp range for the KSUID epoch.

### Snowflake Features

- Represent a Snowflake ID as an unsigned 64-bit numeric value.
- Generate Twitter-style Snowflake IDs using custom epoch, machine ID, and sequence counter.
- Decode timestamp, machine ID, and sequence fields.
- Format as decimal string for safe wire transport.
- Parse decimal strings without precision loss.
- Validate machine ID bit limits.
- Validate sequence bit limits.
- Provide configurable machine and sequence bit widths while preserving a 41-bit timestamp field.
- Detect clock rollback.
- Provide a selectable clock rollback behavior: fail fast or wait until time catches up.
- Detect sequence exhaustion within a millisecond.
- Provide range helpers for all Snowflake IDs in a millisecond.

### ObjectId Features

- Represent a MongoDB-style ObjectId as 12 bytes.
- Generate ObjectIds with Unix seconds timestamp, process-unique random bytes, and a 24-bit counter.
- Parse 24-character hex strings.
- Format 24-character lowercase hex strings.
- Extract timestamp seconds and `DateTimeOffset`.
- Compare ObjectIds by byte order.
- Support counter rollover from `0xFFFFFF` to `0x000000`.

### Nano ID Style Features

- Generate URL-safe random IDs with default alphabet and size.
- Generate IDs with caller-supplied alphabet and size.
- Validate alphabet length from 2 through 256 characters.
- Reject duplicate alphabet characters.
- Use uniform random selection without modulo bias.
- Provide collision-space helper methods for alphabet length and ID size.

### Cross-Format High-Level Features

- Classify identifier strings by likely format.
- Validate identifier strings by requested kind.
- Parse identifier strings into a high-level result that carries kind and normalized text.
- Normalize strings without changing semantic value.
- Extract timestamp when the format supports it.
- Compare two identifiers of the same kind.
- Provide test vectors from standards or well-known implementations.
- Provide benchmark-ready code paths without adding benchmark dependencies.
- Document when each identifier type should be used.
- Document security and privacy considerations for timestamp-bearing IDs.

## Public API Completion Tasks

- [DONE] T001: Define stable public failure codes for invalid length, invalid character, invalid UUID variant or version, timestamp overflow, random-field overflow, clock rollback, sequence exhaustion, and alphabet errors; expose them alongside messages in every applicable parse, validation, and timestamp result, and use a coded custom exception for equivalent failures from throwing APIs.
- [DONE] T002: Expose immutable public Nil and Max UUID values through a static data class, preserving defensive byte copying and compatibility with parsing, formatting, comparison, variant, and detection operations.
- [DONE] T003: Add requested-kind validation to `IIdentifier` and `Identifier`, dispatching directly to the selected UUID, ULID, KSUID, Snowflake, ObjectId, or default Nano ID validator and rejecting `IdentifierKind.Unknown` without ambiguous auto-classification.
- [DONE] T004: Make `SnowflakeClockRollbackBehavior.Wait` actually wait for the system clock to catch up during real-time generation, while keeping explicit-timestamp generation deterministic, preventing duplicate values, and using only built-in .NET timing and synchronization facilities.
- [DONE] T005: Add complete behavioral coverage for the public core contracts, result objects, error codes and coded exception, requested-kind validation, classification precedence, normalization, parsing, timestamp extraction, and same-kind and cross-kind comparison, including every success, null, invalid, ambiguous, boundary, and documented exception path.
- [DONE] T006: Add complete behavioral coverage for all public UUID APIs, value operations, enums, namespaces, Nil and Max values, parsing and formatting forms, Guid conversion, versions 3/4/5/7 generation, monotonic overflow and rollback behavior, v1/v6 conversion, timestamp extraction, validation, comparison, defensive copying, RFC 9562 vectors, and all failure codes and documented exception paths.
- [DONE] T007: Add complete behavioral coverage for all public ULID and KSUID APIs, value operations, limits, constructors, generation, monotonic ULID behavior, parsing, normalization, formatting, UUID-byte conversion, timestamp and payload extraction, comparison, defensive copying, canonical specification vectors, range and overflow boundaries, failure codes, and documented exception paths.
- [DONE] T008: Add complete behavioral coverage for all public Snowflake APIs, settings, limits, generator and value operations, default and custom bit layouts and epochs, decimal round trips, decoding, timestamp and range helpers, sequence reset and exhaustion, fail-fast and true wait rollback behavior, settings isolation, failure codes, and documented exception paths without timing-dependent or flaky tests.
- [DONE] T009: Add complete behavioral coverage for all public ObjectId and Nano ID APIs, values, settings and limits, ObjectId parsing, formatting, generation, timestamp extraction, ordering, defensive copying and counter rollover, Nano ID default and custom generation, alphabet and size validation, unbiased selection boundaries, collision-space and entropy calculations, failure codes, authoritative vectors, and all documented exception paths.
