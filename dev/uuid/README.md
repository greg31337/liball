# uuid

A dependency-free .NET identifier library for UUIDs and related sortable or compact ID formats.

The library supports UUID, ULID, KSUID, Snowflake, ObjectId, and Nano ID style identifiers with explicit parsing, validation, normalization, timestamp extraction, comparison, and generation APIs.

## Choosing an Identifier Format

| Format | Choose it when | Avoid it when |
| --- | --- | --- |
| UUIDv4 | You need a widely supported random identifier with no embedded timestamp or coordination requirement. It is a safe default for public IDs when sort order is not important. | You need insertion-order locality, compact text, or timestamp extraction. |
| UUIDv7 | You want a standards-based UUID that sorts roughly by creation time and still fits UUID storage, indexes, and APIs. It is a good default for new database IDs that benefit from time locality. | You must hide creation time or need compatibility with systems that only accept older UUID versions. |
| ULID | You want a 26-character, URL-safe, lexicographically sortable text ID with millisecond timestamp extraction and UUID-compatible 128-bit bytes. | You need a formal UUID string, or you cannot expose creation time. |
| KSUID | You want a 27-character sortable text ID with second-level timestamp extraction and a large 128-bit random payload. It is useful for distributed events and logs. | You need millisecond precision, UUID compatibility, or the shortest possible text. |
| Snowflake | You need compact 64-bit numeric IDs with ordered generation, a custom epoch, machine ID bits, and per-millisecond sequence control. | You cannot coordinate machine IDs, need more than the configured per-millisecond sequence capacity, or need opaque random IDs. |
| ObjectId | You need MongoDB-style 12-byte IDs with a seconds timestamp and canonical 24-character hex text. It is most useful for MongoDB interoperability. | You are not interoperating with ObjectId-based systems, need millisecond precision, or need an identifier with no embedded timestamp. |
| Nano ID | You need compact random text with a configurable alphabet and size, especially for URL-friendly public tokens. | You need timestamp extraction, lexicographic time ordering, or a fixed binary layout. |

## Practical Defaults

- Use `new Uuid().NewV7()` for new database primary keys when UUID compatibility and index locality both matter.
- Use `new Uuid().NewV4()` for opaque identifiers when creation time should not be visible.
- Use `new Ulid().New()` when you want sortable, compact text and millisecond timestamps.
- Use `new ObjectId().New()` only when ObjectId compatibility is required.
- Use `new NanoId().New()` for compact URL-safe tokens where sort order and timestamp extraction are not needed.

## Security And Operational Tradeoffs

| Concern | Guidance |
| --- | --- |
| Security | Treat identifiers as identifiers, not secrets. UUIDv4 and Nano ID are random, but use separate cryptographic tokens for authentication, password resets, API keys, or bearer credentials. |
| Privacy | UUIDv7, ULID, KSUID, Snowflake, and ObjectId expose creation time. Snowflake can also expose deployment topology through machine ID bits. Use UUIDv4 or a sufficiently large Nano ID when creation time should stay hidden. |
| Predictability | Snowflake and ObjectId include counters, so nearby IDs can be easier to infer even when the full value is not trivially guessable. UUIDv4 and Nano ID provide the least structural predictability when generated with secure randomness. |
| Sortability | UUIDv7, ULID, KSUID, Snowflake, and ObjectId sort by time when stored in their canonical binary or fixed-width text forms. UUIDv4 and Nano ID do not provide time ordering. |
| Database indexes | Random IDs such as UUIDv4 spread inserts across an index. Time-ordered IDs such as UUIDv7, ULID, KSUID, Snowflake, and ObjectId improve insertion locality, but can create hot ranges under very high write rates. |
| Storage | Snowflake is compact at 64 bits. ObjectId is 12 bytes. UUID and ULID are 16 bytes. KSUID is 20 bytes. Nano ID storage depends on configured text size and encoding. |
| Interoperability | Use the format native to the system boundary when compatibility matters: UUID for UUID columns/APIs, ObjectId for MongoDB-style systems, and decimal Snowflake strings for JavaScript-safe wire transport. |

## High-Level Helpers

Create an `Identifier` instance and use `Classify`, `Validate`, `NormalizeText`, `TimestampText`, and `CompareText` when input can contain multiple supported identifier formats. For dependency injection, depend on `IIdentifier` instead.

For example, `new Identifier().Classify(text)` detects the format and returns normalized text without throwing.

`new Identifier().Parse(text)` returns both canonical text and an `IIdentifierValue`. The common value exposes its `IdentifierKind` and canonical text without requiring a second format-specific parse. Use `NormalizeValue`, `TimestampValue`, and `CompareValues` for common values. Cast the value to `UuidValue`, `UlidValue`, `KsuidValue`, `ObjectIdValue`, or `SnowflakeValue` only when format-specific members are needed.

## Public API And Compatibility

The supported API is declared under `src/Api` and divided into contracts, interface-backed implementation classes, mutable data objects, static constants, enums, and exceptions. Encoding, byte-order, random-number, timestamp-conversion, parser, generator-state, and other implementation helpers are `internal` and can change without compatibility guarantees.

Use instances of `Uuid`, `Ulid`, `Ksuid`, `ObjectId`, and `NanoId` for format-specific work, or depend on their corresponding interfaces. Values and parse results are returned as `UuidValue`, `UlidValue`, `KsuidValue`, `ObjectIdValue`, and their matching result classes. Concrete value classes implement `IIdentifierValue` plus a format-specific value interface. Snowflake generation is stateful: `new Snowflake().DefaultGenerator()` creates a default generator, while `CreateGenerator(settings)` uses custom `SnowflakeSettings`.

Public symbols include XML documentation describing accepted formats, copied versus mutable data, timestamp precision, normalization, ordering, failure results, and thrown exceptions. Internal symbols intentionally do not carry XML API documentation.
