# Universal email library development plan

## Goal and completion definition

Build a synchronous, dependency-free .NET 10 library that can safely parse, inspect, transform, validate, authenticate, compose, and serialize Internet email messages. The canonical model is an ordered MIME tree backed by bytes; a convenient body/attachment view is derived from that tree and never replaces it.

The library is complete when it can:

- round-trip an untouched valid message byte-for-byte and preserve useful raw data from malformed messages;
- parse both complete and incrementally supplied messages without loading large bodies into memory;
- represent every RFC 5322 field and every MIME entity, including unknown fields, types, parameters, encodings, and extensions;
- decode, edit, normalize, compose, and serialize standards-compliant ASCII and SMTPUTF8 messages;
- expose safe high-level operations for bodies, alternatives, related resources, attachments, embedded messages, reports, calendar invitations, replies, forwards, search, redaction, and archives;
- enforce configurable limits and report all recoverable defects without losing usable content;
- sign and verify DKIM/ARC and process S/MIME and PGP/MIME using only cryptographic primitives available in the target .NET runtime;
- build `src/` using only the .NET BCL and pass conformance, corpus, fuzz, resource-limit, interoperability, and round-trip tests; test projects may use external test-only libraries.

## Scope boundaries

In scope:

- RFC 5322/6532 Internet message syntax, MIME, common de-facto malformed input, single-message `.eml`, mbox, and Maildir files;
- byte-preserving parsing, pull-based streaming parsing, an editable document model, codecs, validation, normalization, and generation;
- message-level authentication and end-to-end MIME security when caller-supplied keys/certificates are sufficient;
- synchronous `Stream`, `Memory`, and span-oriented APIs; bounded memory and optional temporary-file spooling;
- production functionality under `src/` uses only the .NET BCL and may not depend on external parser, MIME, HTML, DNS, cryptography, storage, or encoding libraries. Test projects may use external test-only libraries and tools.

Out of scope:

- SMTP, submission, IMAP, POP3, JMAP, Exchange, Graph, Gmail, or other network clients;
- DNS TXT lookup, SPF evaluation, DMARC policy evaluation, certificate discovery, key servers, and remote URL fetching; callers supply resolved DKIM/ARC keys and security material;
- mailbox synchronization, server flags, server-side search, contacts, templates, bulk delivery, bounce routing, and UI rendering;
- executing HTML, downloading external content, opening attachments, antivirus engines, spam classifiers, OCR, and full document-format parsers;
- byte-for-byte reproduction after a caller mutates the affected header/entity; deterministic normalized serialization applies after mutation.

## Implementation constraints from `syntax.md`

All implementation choices below are shaped around the restricted source syntax, not conventional C# library patterns.

- Source contains only file-scoped namespaces, classes, enums, fields, constructors, and ordinary block-bodied methods.
- The library declares no structs, records, delegates, generics, properties, indexers, events, operators, or extension methods. Narrow interfaces represent caller-supplied behavior and standard lifetime contracts; they contain instance methods only and do not inherit other interfaces.
- Model variation uses a concrete class plus a `Kind` enum, not inheritance or interfaces. For example, one `MimeEntity` class represents leaf, multipart, and nested-message nodes. Interfaces are not used merely to split stateful data models by kind.
- Public state is exposed through explicit `Get...`, `Set...`, `Add...`, `Remove...`, and `Clear...` methods; options and result classes replace optional, named, `ref`, `out`, tuple, and nullable parameters.
- Expected failures return result/issue objects. Exceptions are reserved for invalid API use, hard limits, and unrecoverable I/O/cryptographic failures because source cannot use `try`/`catch`/`finally`.
- APIs are synchronous because `await` is forbidden. Cancellation is checked explicitly through an `IEmailCancellation` implementation.
- Library types are non-generic; built-in generic reference types such as `List<T>`, `Dictionary<TKey,TValue>`, `ReadOnlyMemory<byte>`, `ReadOnlySpan<byte>`, and `Span<byte>` may be consumed where allowed.
- Spans are used only for immediate scanning, comparison, encoding, decoding, hashing, and copying. They are never retained in fields; retained slices use offsets into owned buffers or `ReadOnlyMemory<byte>`.
- Iteration uses returned `List<T>` values, count/get methods, or pull readers because iterator methods and `yield` are forbidden.
- Resource ownership is explicit through `Close`/`Dispose`; resource-retaining public classes implement `IDisposable` so callers can use standard lifetime handling.
- LINQ, pattern matching, local functions, and implicit exception recovery are prohibited; parsers use indexed loops, state enums, lookup tables, and small ordinary helper methods.
- Tests may use general C# syntax permitted by `syntax.md`, but the test runner, assertions, fuzz loops, and benchmarks must use only the .NET SDK and BCL.

## Research basis and lessons adopted

The feature inventory was derived from standards and established libraries rather than from `System.Net.Mail` alone:

| Source | Relevant feature surface | Decision adopted here |
|---|---|---|
| [MimeKit API](https://mimekit.net/docs/html/N_MimeKit.htm) and [message guide](https://mimekit.net/docs/html/Working-With-Messages.htm) | MIME tree, raw content, headers and structured values, body builder, traversal, related resources, reports, partial/nested messages, tolerant parsing, DKIM, ARC, S/MIME, PGP/MIME, and TNEF | Use a MIME tree as truth; provide convenience projections and advanced formats as separate layers. |
| [Jakarta Mail 2.1](https://jakarta.ee/specifications/mail/2.1/jakarta-mail-spec-2.1) | Message/part/multipart model, byte streams, content handlers, composition, folders/transports, flags, events, and search terms | Keep message/part semantics and message search; exclude transport/store/session abstractions from this format library. |
| [Python `email.parser`](https://docs.python.org/3/library/email.parser.html), [`email.generator`](https://docs.python.org/3/library/email.generator.html), and [`headerregistry`](https://docs.python.org/3/library/email.headerregistry.html) | Whole and feed parsers, policy-driven strictness, defect reporting, typed headers, content manager, MIME walking/body selection, generation, source refolding, and near-inverse byte parsing/generation | Make parse/write policy explicit, retain defects and raw forms, support feed parsing, and guarantee unchanged round trips. |
| [Nodemailer message API](https://nodemailer.com/message), [attachments](https://nodemailer.com/message/attachments), and [DKIM](https://nodemailer.com/dkim) | Simple high-level composition, text/HTML/AMP/calendar alternatives, CID resources, attachments from varied sources, raw nodes, automatic MIME structure, transfer-encoding choice, and DKIM | Add a high-level composer and source abstraction above the exact MIME tree; never fetch URLs inside the library. |
| [Apache Mime4j](https://james.apache.org/mime4j/index.html) and [streaming usage](https://james.apache.org/mime4j/usage.html) | Tolerant corpus-tested parser, token/event streaming, separate decoding, DOM, and temporary storage for large bodies | Separate structural parsing from decoding and offer both pull tokens and a materialized model with spooling. |
| [Go `net/mail`](https://pkg.go.dev/net/mail) | Small focused APIs for message, address-list, and date parsing, with documented tolerant deviations | Keep low-level parsers independently callable and document strict/tolerant behavior. |
| [.NET `MailMessage`](https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.mailmessage?view=net-10.0) | Basic address fields, custom headers, alternate views, linked resources, and attachments | Provide equivalent convenience composition without inheriting its sending focus or lossy MIME model. |

Primary standards baseline:

- Internet message syntax: [RFC 5322](https://www.rfc-editor.org/info/rfc5322/), its group-syntax update RFC 6854, and internationalized headers/message-global [RFC 6532](https://www.rfc-editor.org/info/rfc6532/).
- MIME: [RFC 2045](https://www.rfc-editor.org/info/rfc2045/), [RFC 2046](https://www.rfc-editor.org/info/rfc2046/), [RFC 2047](https://www.rfc-editor.org/info/rfc2047/), RFC 2049, parameter continuations [RFC 2231](https://www.rfc-editor.org/info/rfc2231/), disposition [RFC 2183](https://www.rfc-editor.org/info/rfc2183/), content IDs RFC 2392, aggregate/related content RFC 2557, and flowed text RFC 3676.
- Specialized messages: DSN RFC 3464/6533, MDN RFC 8098, abuse feedback RFC 5965, automatic responses RFC 3834, list headers RFC 2369/8058, iCalendar/iMIP RFC 5545/6047, and mbox RFC 4155.
- Authentication/security: DKIM [RFC 6376](https://www.rfc-editor.org/info/rfc6376/) plus RFC 8301/8463/8616, ARC [RFC 8617](https://www.rfc-editor.org/info/rfc8617/), Authentication-Results RFC 8601, CMS RFC 5652, S/MIME RFC 8551, PGP/MIME RFC 3156 with OpenPGP RFC 9580, and protected headers RFC 9788.

## Architecture and dependency order

1. Byte primitives, limits, diagnostics, and content ownership.
2. RFC lexical scanners and independent codecs.
3. Ordered raw headers and structured header parsers/formatters.
4. MIME entity model and strict/tolerant parsers.
5. Byte-preserving and normalized writers.
6. Tree processing, text/attachment projections, editing, and validation.
7. High-level composition and common message workflows.
8. Specialized reports, calendar, archive, and legacy interop.
9. DKIM/ARC, S/MIME, and PGP/MIME.
10. hardening, conformance, fuzzing, corpus tests, performance, and release documentation.

Parsing remains lossless by retaining raw source ranges and dirty flags. Decoded content is lazy and cached only within configured limits. Editing marks only the affected header or entity dirty. The preserving writer copies clean ranges and regenerates dirty ranges; the canonical writer regenerates everything predictably.

## Complete feature inventory

### F01 — Byte, line, and storage foundations

- Scan CRLF, bare LF, and bare CR input; preserve the original separator and optionally normalize output to CRLF.
- Span-based ASCII classification, case-insensitive comparison, token/atom/quoted-string/comment scanning, hexadecimal conversion, and delimiter search.
- Source ranges with absolute offsets, line/column tracking, bounded lookahead, and overflow-safe length arithmetic.
- Memory, caller stream, owned file, and temporary-file content sources with explicit ownership and repeatable reads where possible.
- Configurable thresholds for in-memory content and automatic spooling of large leaf bodies.
- Configurable hard limits for message bytes, headers, line length, nesting depth, parts, parameters, addresses, encoded words, decoded bytes, decompressed bytes, and crypto work.
- Cancellation checks, progress counters, deterministic cleanup, pooled-buffer use, and clearing of sensitive buffers.
- Stable issue codes with severity, source position, entity path, field name, and recovery action.

### F02 — Transfer and text codecs

- Strict and tolerant Base64 decoding; canonical Base64 generation with MIME line wrapping; incremental operation.
- Strict and tolerant quoted-printable body decoding/encoding, soft line breaks, trailing whitespace rules, and newline policy.
- Identity handling for 7bit, 8bit, and binary with constraint validation and optional safe re-encoding.
- Tolerant decoding of common `x-uuencode`/`uuencode` leaf bodies; generation remains opt-in legacy behavior.
- RFC 2047 B/Q encoded-word parsing, adjacent-word whitespace suppression, charset/language metadata, tolerant recovery, and context-correct generation/folding.
- Charset lookup aliases, BOM detection, ASCII/UTF-8/Unicode/Latin-1/code-page support available in the runtime, caller-registered mappings, replacement/fail policies, and preservation of undecodable bytes.
- Percent encoding and RFC 2231 parameter charset/language/continuation decoding and deterministic continuation generation.
- Content transfer-encoding choice based on data, line length, media type, and 7-bit/8-bit/binary target constraints.

### F03 — RFC 5322 lexical and structured values

- CFWS, nested comments, quoted pairs, atoms, dot-atoms, quoted strings, domain literals, obsolete syntax, and tolerant recovery.
- Mailboxes, display names, internationalized local/domain parts, source routes in tolerant mode, groups (including empty groups), lists, IDNA conversion helpers, and safe formatting.
- Date-time parsing with numeric/named zones, obsolete forms, leap-second tolerance, unknown `-0000` offset preservation, and canonical formatting.
- Message-ID, Content-ID, In-Reply-To, References, and lists of IDs with tolerant broken-form recovery.
- MIME media types, dispositions, transfer encodings, languages, and ordered parameters with duplicates and raw/decoded values.
- Received trace tokenization (`from`, `by`, `via`, `with`, `id`, `for`, date) while retaining unrecognized clauses verbatim.
- Unstructured Unicode/encoded-word values and phrase-context encoded words.
- Keywords, Return-Path, Resent blocks, Newsgroups/Followup-To, priority/importance, sensitivity, auto-submitted, list headers, and one-click unsubscribe values.
- Authentication-Results, Received-SPF, DKIM-Signature, ARC headers, and common provider trace headers as structured-but-lossless values.
- Header cardinality and context rules, including required fields, resent block consistency, Bcc stripping policy, and MIME-only field placement.

### F04 — Ordered header collection

- Preserve field order, duplicates, original name casing, raw folding, line endings, invalid bytes, and source ranges.
- Case-insensitive get-first/get-all/contains operations without collapsing duplicates.
- Add, insert, replace-first, replace-all, remove-first, remove-all, and reorder operations with header-injection rejection.
- Lazy typed parsing with cached structured values and per-field issues; mutation invalidates only affected caches.
- Header registry mapping standard names to value kinds without reflection, model inheritance, or caller code execution.
- Separate envelope `From ` line for mbox; never confuse it with the RFC `From` field.
- Raw, unfolded, decoded-Unicode, and canonical-form access.

### F05 — MIME document model

- `EmailMessage` root with ordered headers, one body entity, envelope metadata, source metadata, issues, and dirty state.
- One `MimeEntity` discriminated by `MimeEntityKind`: leaf, multipart, nested message, partial message, external body, delivery status, disposition notification, feedback report, calendar, TNEF, signed, or encrypted.
- Every entity has its own headers, raw/decoded content source, media type, disposition, transfer encoding, content ID/location/language/description, children, preamble, epilogue, and source range.
- Preserve unknown media types, multipart subtypes, encodings, fields, parameters, and malformed fragments.
- Correct defaults (`text/plain; charset=us-ascii`, `message/rfc822`, multipart/digest child defaults) without inventing raw fields.
- Convenience factories for text, binary leaf, multipart, and nested-message entities.
- Deep clone, detach source, materialize, replace content, and recalculate metadata operations.

### F06 — Complete and incremental parsing

- Parse `byte[]`, `ReadOnlyMemory<byte>`, seekable/non-seekable `Stream`, a header block, one MIME entity, one message, and consecutive mbox messages.
- Pull-based `EmailTokenReader` states for message/header/entity/multipart boundaries/body chunks/end, enabling low-allocation consumers without callbacks.
- `EmailFeedParser.Feed(ReadOnlySpan<byte>)` plus `Complete()` for fragmented socket/pipe input without `async`.
- Strict, conforming-tolerant, and forensic modes with explicit recovery policies.
- Header/body separator recovery, orphan continuations, invalid names, NUL/bare newline handling, overlong lines, duplicate singleton fields, and truncated input diagnostics.
- Boundary recognition across buffer edges; nested multiparts; quoted/unquoted/broken boundaries; preamble/epilogue; missing close boundary; boundary-prefix collision rules.
- Recursive parsing of message/rfc822, message/global, multipart/digest, message/partial metadata, and message/external-body metadata.
- Lazy leaf decoding, direct-to-storage streaming, body hash calculation, and skipping/discarding selected bodies.
- Source path and byte position on every entity/issue and deterministic partial results when recoverable limits are reached.

### F07 — Serialization and generation

- Preserving writer that copies untouched raw ranges exactly, including invalid folding and line endings.
- Canonical writer with CRLF, deterministic field formatting/folding, stable parameter ordering policy, and 78/998-character limits.
- ASCII/RFC 2047 output, SMTPUTF8/RFC 6532 output, and 7bit/8bit/binary body constraints.
- Correct multipart boundary generation using cryptographically strong randomness or deterministic seeded mode; scan content to prevent collisions.
- Correct preamble/epilogue, nested message, multipart/signed canonical-byte, Bcc omission, and terminal newline handling.
- Automatic or explicit content-transfer encoding; no double encoding; streaming encoder path for large content.
- Output to `Stream`, byte array, text for guaranteed-text messages, and counting/hash sinks.
- SMTP DATA adaptation (CRLF normalization, dot-stuffing, final terminator) as a format filter only, without an SMTP client.
- Deterministic serialization options for tests, caching, signatures, and reproducible builds.

### F08 — MIME traversal and convenient projections

- Depth-first and breadth-first pull traversal, parent/path lookup, filtering by media type/disposition/name/content ID, and maximum-depth enforcement.
- RFC-aware preferred body selection across multipart/alternative, related, mixed, signed, encrypted, and unknown containers.
- Plain-text and HTML body access; all alternatives; body candidates with quality/reason metadata.
- Attachment projection using disposition plus MIME-tree context and filename/name heuristics; distinguish inline resources, signatures, and true attachments.
- Attachment filename decoding, Unicode normalization option, path stripping, control-character removal, collision-safe filesystem name suggestion, and size/hash metadata.
- CID/content-location index and resolution for related resources; duplicate/missing ID diagnostics.
- Embedded message enumeration and safe recursive depth handling.
- Streaming decoded-content copy, hashing, size measurement, save, and prefix inspection without mandatory materialization.
- MIME extension/type mapping and conservative magic-byte mismatch diagnostics; never execute or deeply parse arbitrary attachments.

### F09 — Text processing and safe display

- Decode text using declared charset, BOM, limited safe heuristics, configurable fallback, and replacement diagnostics.
- RFC 3676 `format=flowed` and `DelSp` decoding/encoding with quote-depth preservation.
- Normalize newline, Unicode normalization, control/bidi-marker reporting, and optional safe removal.
- HTML-to-text conversion covering block breaks, lists, quotes, links, entities, preformatted text, hidden/script/style content, malformed tags, and output limits.
- Conservative HTML sanitization with tag/attribute allowlists, URL scheme checks, event/style/script removal, CID preservation, remote-content blocking, and diagnostics; no browser claim.
- Data-URI extraction into related MIME resources with byte/type/count limits and CID rewrites.
- Preview/snippet generation, quoted-reply/signature heuristics, and decoded-text search that never changes the source message.
- Display model containing selected text/HTML, alternatives, inline resources, attachments, security/report summary, and warnings.

### F10 — Editing, normalization, validation, and policy

- Message/entity/header mutation with dirty propagation and preservation of untouched siblings.
- Structural operations to wrap/unwrap multipart nodes, add/remove/reorder parts, set alternatives, relate resources, attach/detach files, and embed messages.
- Syntax validation separate from parsing; report errors/warnings/info without silently repairing.
- Configurable repair/normalization: line endings, folding, MIME-Version, missing content type, duplicate fields, invalid encoding, absent boundaries, charset labels, and filenames.
- Generate Date, Message-ID, MIME-Version, Content-ID, and safe boundaries only when requested.
- Header injection prevention, CR/LF validation, address/header cardinality enforcement, and serialization-time safety checks.
- Redaction/anonymization policies for addresses, message IDs, Received chains, IPs, authentication fields, subjects, bodies, and attachment names/content.
- Stable semantic fingerprint and exact-byte hash; canonical comparison that can ignore selected trace/security fields.
- Policy presets: Preserve, StrictInternet, TolerantInternet, Forensic, SevenBitTransport, SmtpUtf8, SafeDisplay, and DeterministicTest.

### F11 — High-level composition and workflows

- Fluent-free `EmailComposer` ordinary methods for sender/from/reply-to/to/cc/bcc, subject, date, IDs, priority, custom fields, and envelope recipients.
- Plain text, HTML, AMP HTML, Apple Watch HTML, arbitrary alternatives, iCalendar, and caller-supplied raw MIME nodes.
- Attach memory, stream, file path, or existing content source; inline CID resources; explicit or inferred media type; no URL fetching.
- Automatically produce correct mixed/alternative/related nesting while allowing callers to retain exact-tree control.
- Encoding, filename, disposition, language, description, CID, location, and per-part custom field control.
- Reply builder with reply/reply-all recipient logic, subject prefixes, References/In-Reply-To, body quoting, and attachment policy.
- Forward builder for inline or message/rfc822 forwarding with subject/body/attachment policies.
- Resent message builder, bounce wrapper, delivery-status/MDN/feedback-report builders, and calendar invitation helper.
- Envelope projection distinct from visible headers, including Bcc recipients without serialized Bcc leakage.
- Safe defaults and validation report before serialization.

### F12 — Search, threading, and analysis

- In-memory query object for field existence/equality/contains, address/domain, date range, media type, attachment name/size, body text, issue severity, and authentication state.
- Boolean AND/OR/NOT composition represented by a query-node class and enum rather than inheritance.
- Search evaluation with explicit limits, culture-invariant comparison by default, and decoded/raw selection.
- Thread identity extraction from Message-ID/References/In-Reply-To plus normalized-subject fallback; no mailbox-server algorithm claim.
- Conversation graph builder with duplicate IDs, missing parents, cycles, and subject fallback diagnostics.
- Message summary/statistics: raw/decoded sizes, part counts/depth, media types, recipients, dates, attachment totals, and issue/security counts.

### F13 — Specialized message/report formats

- Parse/generate multipart/report generically and specialize delivery-status (DSN), disposition-notification (MDN), and feedback-report (ARF) field blocks.
- Preserve per-recipient DSN blocks, original headers/message, internationalized delivery status, action/status/diagnostic values, and extension fields.
- Parse/generate MDN disposition modes/types, failure/error/warning fields, original recipient/message ID, and requested receipt headers.
- Parse/generate ARF feedback types, source IP, arrival date, authentication results, reported domain/URI, and original headers/message.
- Identify auto-generated/list/bounce messages using standards-based fields and return reasons/evidence rather than a single opaque boolean.
- Parse/generate iCalendar content sufficient for iMIP method, UID, organizer, attendees, start/end/time zone text, sequence, status, and raw-property preservation.
- Reassemble `message/partial` fragments with ID/number/total validation, duplicate/missing-fragment diagnostics, size limits, and reparsing.
- Represent `message/external-body` access metadata without retrieving external content.

### F14 — Archive and filesystem formats

- Streaming mbox reader/writer for mboxo/mboxrd conventions, envelope line parsing, `From ` escaping/unescaping, message offsets, append, and corruption diagnostics.
- Maildir enumerate/read/write with `tmp`/`new`/`cur`, unique safe names, atomic same-filesystem move, standard flags, and no server synchronization semantics.
- `.eml` load/save helpers with safe overwrite options supplied explicitly by the caller.
- Archive indexing records with offsets/paths, selected headers, sizes, hashes, and parse status; persistence format remains caller-owned.
- Batch operations return per-message results so one malformed message does not hide successful items; source cannot catch I/O exceptions, so caller controls retry/isolation boundaries.

### F15 — Legacy and interoperability support

- Parse TNEF (`winmail.dat`/`application/ms-tnef`) container structure, checksums, common MAPI properties, body variants, recipients, attachment names/data/CIDs, and embedded messages.
- Convert TNEF content into ordinary MIME parts while retaining original properties and diagnostics; TNEF generation is not required.
- Recognize Outlook/RFC deviations, Gmail/Exchange boundary quirks, broken encoded words/parameters, invalid charsets, duplicate fields, and common MIME type aliases through isolated compatibility rules.
- Optional legacy uuencode decode and common `Content-Type: application/octet-stream; name=...` attachment recovery.
- Compatibility behavior is selectable, issue-producing, corpus-tested, and never enabled invisibly in strict mode.

### F16 — DKIM and ARC

- Parse/format DKIM signature and DNS-key records losslessly, including unknown tags.
- DKIM simple/relaxed header and body canonicalization as streaming transforms; body length, repeated-field selection, empty-body, and trailing-line rules.
- Sign with RSA-SHA256 and runtime-supported Ed25519-SHA256; verify required/current algorithms and optionally legacy RSA-SHA1 with an explicit insecure-policy flag.
- Caller supplies parsed public-key records, so verification performs no DNS; enforce identity/domain, hash/algorithm, timestamp/expiration, key flags, minimum key size, and resource policy.
- Verify all signatures independently and return canonicalization/hash/signature/policy results with exact reasons.
- ARC set parsing, instance/order validation, AMS verification, seal verification, chain validation, and ARC sealing using caller-supplied keys.
- Preserve raw signed bytes and forbid transformations that would invalidate a signature unless the caller explicitly accepts invalidation.
- Parse and format Authentication-Results; do not claim independent SPF/DMARC validation.

### F17 — S/MIME and PGP/MIME

- Generic multipart/signed and multipart/encrypted modeling with exact canonical signed-byte access.
- CMS/S/MIME detached and opaque signature creation/verification, enveloped encryption/decryption, certificate-chain result capture, multiple signers/recipients, and algorithm policy.
- S/MIME compressed-data support only when implementable with BCL ASN.1/CMS/deflate primitives; otherwise retain the entity losslessly and return a precise unsupported-algorithm result.
- PGP/MIME signed/encrypted MIME framing, ASCII armor, packet lengths, literal/compressed/signature/key/session packets, fingerprints, and key lookup over caller-supplied key material.
- OpenPGP algorithms limited to primitives safely available in .NET; unsupported packets/algorithms are preserved and reported, never silently weakened.
- Cleartext signatures and inline PGP detection are read/verify interoperability features; new messages prefer PGP/MIME.
- Protected-header extraction/generation per current guidance, with outer/inner mismatch diagnostics and safe display policy.
- Zero sensitive buffers where possible, never log secrets, enforce crypto input/work limits, and return signer/recipient/certificate/key details in result objects.

### F18 — Quality, security, and operability

- Unit vectors from every implemented RFC section, including errata and adversarial edge cases.
- Golden corpus for major generators/parsers, malformed real-world samples, nested messages, signatures, and archives; fixtures contain no personal data.
- Parse-preserve-write equality, parse-canonical-write-reparse semantic equality, and streaming/chunk-boundary equivalence properties.
- Mutation fuzzing for headers, boundaries, codecs, structured values, HTML, TNEF, ASN.1/CMS, OpenPGP packets, mbox, and Maildir names.
- Resource exhaustion tests for depth, part count, line/header length, expansion ratios, large attachments, boundary attacks, hash/signature work, and temporary-file cleanup.
- Benchmarks for allocations and throughput on small/large messages, huge headers, many parts, Base64/QP, body selection, and preserve/canonical writing.
- Stable public issue codes, deterministic results, thread-safety documentation, ownership documentation, and no hidden global mutable state.
- XML documentation is required on every public type, constructor, method, field, enum, and enum member. Keep the broader Markdown ownership, security, compatibility, and examples documentation as complementary guidance.

## Current public API audit

The existing `src/Api` layer broadly exposes the planned F01-F17 capabilities through foundation, codec, structured-value, MIME model, parsing, writing, processing, composition, analysis, archive, authentication, and cryptographic types. The solution builds and its 1,583 tests pass, but the feature audit found several surface gaps: standard structured fields omitted from the header registry and value facade, no DKIM key-record formatter, no explicit metadata refresh after direct header edits, no guaranteed-text serialization convenience, incomplete Maildir state/flag operations, and no selectable provider-specific compatibility rules. The test suite also does not completely exercise the public layer; the audit baseline covered 85.99% of public-layer executable lines and 72.48% of its branches, with at least one public class wholly unexecuted.

## Public API completion tasks

- [DONE] T001: Complete structured-header coverage by adding lossless public models, strict/tolerant parsers, canonical formatters, header-registry entries, and value-facade operations for Return-Path, MIME-Version, Content-Transfer-Encoding, Content-Location, Newsgroups, Followup-To, List-ID, and the planned common trace fields, plus formatter/facade parity for the existing parse-only keyword, priority, importance, sensitivity, auto-submitted, list-management, one-click-unsubscribe, and resent values.
- [DONE] T002: Add a public MIME metadata refresh operation that reparses entity content headers after direct header mutation, updates derived media type, disposition, transfer encoding, identifiers, language, description, and specialized entity kind consistently, preserves unknown/raw values, and reports conflicts without mutating clean source ranges unnecessarily.
- [DONE] T003: Add bounded public text serialization operations and result/options types for messages or entities that are guaranteed to be textual, with explicit charset, newline, transfer-decoding, and failure behavior while retaining byte-oriented writing as the canonical path.
- [DONE] T004: Complete the Maildir public layer with validated and collision-safe operations to move entries among `tmp`, `new`, and `cur`, update standard flags in canonical order while preserving supported unknown information, and return per-operation paths, state, and issues for atomic same-filesystem changes.
- [DONE] T005: Expand compatibility options into explicit, independently selectable and issue-producing rules for the planned Outlook/Exchange and Gmail deviations, common MIME aliases, broken encoded words and RFC 2231 parameters, malformed boundaries, and legacy attachment recovery, with strict mode disabling every nonstandard recovery.
- [DONE] T006: Add a lossless DKIM DNS key-record formatter and facade operation that preserves unknown tags, emits deterministic canonical records, enforces injection and folding limits, and round-trips every key-record state exposed by `EmailDkimKey`.
- [DONE] T007: Complete behavioral tests for every public foundation, storage, limit, cancellation, progress, byte/span, stream, hash, Base64, quoted-printable, transfer-encoding, uuencode, charset, encoded-word, percent-encoding, and RFC 2231 operation, including all option states, result states, invalid arguments, limits, ownership transitions, and incremental chunk boundaries.
- [DONE] T008: Complete behavioral tests for every public lexical, structured-header, header-collection, registry, value-facade, message, MIME entity, content, clone, detach, dirty-state, factory, and metadata-refresh operation, covering every enum value and strict, tolerant, internationalized, malformed, duplicate, unknown-extension, and mutation path.
- [DONE] T009: Complete behavioral tests for every public whole/feed/token/header/entity parser and preserving/canonical/SMTP DATA writer operation across byte arrays, memory, seekable and non-seekable streams, storage modes, parse/write policies, partial results, cancellation, limits, all meaningful chunk splits, exact round trips, and semantic round trips.
- [DONE] T010: Complete behavioral tests for every public traversal, body selection, attachment, related-resource, filename, decoded-content, MIME inspection, text/flowed/HTML processing, display, editing, validation, normalization, generation, redaction, fingerprint, comparison, and policy operation, including safe-display and adversarial inputs.
- [DONE] T011: Complete behavioral tests for every public composer, attachment-source, reply, forward, resent, report-builder, calendar-builder, envelope, query, classification, summary, thread-identity, and thread-graph operation, including recipient/privacy rules, exact MIME tree construction, boolean query composition, cycles, duplicates, and all workflow policies.
- [DONE] T012: Complete behavioral tests for every public report, DSN, MDN, ARF, iCalendar/iMIP, partial-message, external-body, mbox, Maildir, `.eml`, archive-index, TNEF, and compatibility operation, including extension preservation, corrupt input, interrupted filesystem states, collision handling, path safety, and per-message partial success.
- [DONE] T013: Complete behavioral and official-vector tests for every public Authentication-Results, Received-SPF, DKIM, and ARC parser, formatter, canonicalizer, signer, verifier, sealer, key, option, and result operation, including repeated headers, key policies, time boundaries, body-length handling, mutation guards, downgrade attempts, and all declared status values.
- [DONE] T014: Complete behavioral and official-vector tests for every public ASN.1/CMS, S/MIME, OpenPGP, PGP/MIME, protected-header, certificate, key/credential, algorithm-policy, option, and result operation, including multiple parties, supported and unsupported algorithms, corrupt/truncated inputs, work limits, sensitive-data cleanup, and every declared failure status.
- [DONE] T015: Add test-only interoperability coverage against representative output from MimeKit, Jakarta Mail, Python `email`, Nodemailer, Mime4j, Go `net/mail`, Outlook/Exchange, Gmail, OpenSSL, GnuPG, and standards fixtures, using external dependencies only under `tests/`, and assert both intended compatibility and intentional rejection behavior.
- [DONE] T016: Close the public-layer coverage audit by mapping every public constructor, method, enum value, option transition, result state, and exception contract to behavioral tests and exercising every reachable executable line and branch under `src/Api`.
