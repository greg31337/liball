# email

`email` is a synchronous, dependency-free .NET 10 library for reading,
inspecting, editing, validating, composing, authenticating, and writing Internet
email messages. The repository is under active development; `todo.md` is the
implementation roadmap, not a claim that every described feature is available.

## Scope and model

The library treats an Internet message as bytes containing an ordered RFC 5322
header block and a MIME body. Its canonical editable model is an
`EmailMessage` with ordered headers and one root `MimeEntity`. A `MimeEntity`
may be a leaf, multipart container, nested message, or a recognized specialized
message form. Multipart children remain ordered.

The MIME tree is the source of truth. Plain-text bodies, HTML bodies, related
resources, alternatives, and attachments are convenient projections derived
from that tree; they do not replace it or flatten away unknown or repeated
parts. Unknown fields, media types, parameters, encodings, and MIME extensions
remain representable.

When this project uses the following terms, they have these meanings:

- **raw message**: the original message bytes supplied by the caller, including
  field order and casing, folding, line endings, MIME boundaries, and body
  transfer encoding;
- **preserving write**: copying unchanged source ranges exactly and regenerating
  only data made dirty by an edit;
- **canonical write**: deterministic standards-compliant regeneration under an
  explicit write policy; it does not promise the original byte representation;
- **MIME tree**: the ordered hierarchy rooted at the message body, with headers
  and byte-backed content retained on each entity;
- **body or attachment view**: a derived interpretation of entities for common
  application workflows, never an alternative storage model;
- **envelope**: delivery metadata such as envelope recipients and an mbox
  `From ` line, kept separate from visible RFC 5322 fields;
- **issue**: an ordered diagnostic describing malformed, unsupported, unsafe,
  or policy-relevant input and any recovery that was applied.

An untouched valid message is intended to round-trip byte-for-byte. After a
caller changes a field or entity, exact reproduction is promised only for
unaffected source ranges; changed data uses the selected deterministic writer
policy. Recoverable malformed input should retain as many useful original bytes
and diagnostics as configured limits safely allow.

## Public namespace and platform contract

All library-owned public types use the root namespace `Email`. Feature areas
may use child namespaces such as `Email.Security` or `Email.Formats`; no public
type is placed in the global namespace or in a namespace rooted at the lowercase
project name. Public type names use the `Email` prefix where it adds context
(`EmailMessage`, `EmailContent`, and `EmailParser`), while MIME-specific types
use the established `Mime` prefix (`MimeEntity` and `MimeEntityKind`).

The initial target is .NET 10. The library is synchronous and uses only the .NET
base class library: the production project has no NuGet package dependencies.
Callers interact through ordinary classes, enums, narrow behavior interfaces,
constructors, and methods;
the restricted source-language contract is documented in `syntax.md`.

Before a stable 1.0 release, public API and behavior may change between minor
versions and such changes must be called out in release notes. Starting with
1.0, semantic versioning applies: patch releases preserve public source and
binary compatibility, minor releases add compatible API or behavior, and
breaking public API or documented behavior requires a major version. A target
framework roll-forward that prevents existing applications from loading the
library is also a breaking change. Compatibility does not require preserving
undocumented behavior, exact exception text, or diagnostics explicitly marked
experimental, but stable issue codes are not reassigned to different meanings.

Format compatibility is policy-driven. Strict modes enforce the selected RFC
rules; tolerant and forensic modes may recognize documented real-world
deviations while preserving evidence and reporting issues. Tolerance never
means silently discarding unknown content, bypassing configured resource
limits, or promising that malformed input will serialize to identical bytes
after mutation.

## Included responsibilities

The project covers Internet message and MIME syntax, byte-preserving and
canonical serialization, streaming and materialized parsing, structured header
values, content codecs, tree traversal and editing, safe display projections,
single-message `.eml`, mbox and Maildir formats, and message-level cryptographic
formats when the caller supplies the necessary keys, certificates, and resolved
records. SMTP DATA formatting may be provided as a byte-formatting filter.

All parsing, decoding, storage, archive, HTML, and cryptographic work is bounded
by explicit limits. The library does not execute HTML, open attachments, or
interpret arbitrary attachment formats as trusted content.

## Standards and feature support

The matrix describes the implemented library surface, not every optional feature
or algorithm in each cited standard. Unknown and extension data is preserved
where the model permits it, but preservation alone does not mean that the
library interprets or generates that extension.

| Mark | Meaning |
| --- | --- |
| **Full** | Standards-conforming behavior is implemented for the scope stated in the row. |
| **Tolerant read** | Reading also recognizes documented malformed, obsolete, or legacy forms and reports recovery issues. Writers do not reproduce malformed forms as canonical output. |
| **Write** | Canonical construction or serialization is implemented. Preserving writes may also copy unchanged source bytes exactly. |
| **Caller key** | The operation is implemented only with keys, certificates, resolved records, or trust policy supplied by the caller. |
| **Unsupported algorithm** | Framing and metadata can be retained or diagnosed, but the cryptographic or compression primitive is not performed. |
| **Out of scope** | The behavior is intentionally not provided by this message-format library. |

### Internet message and MIME

| Standard or feature | Read / inspect | Generate / write | Notes |
| --- | --- | --- | --- |
| RFC 5322 Internet message syntax | **Full**, **Tolerant read** | **Write** | Ordered/repeated fields, comments, obsolete recoverable forms, dates, addresses, message identifiers, trace fields, and exact source ranges. |
| RFC 2045/2046 MIME | **Full**, **Tolerant read** | **Write** | Multipart trees, nested messages, defaults, preambles/epilogues, unknown media types and parameters, and bounded malformed-boundary recovery. |
| Base64, quoted-printable, 7bit, 8bit, and binary transfer encodings | **Full**, **Tolerant read** | **Write** | Incremental codecs, validation, configured transport constraints, and decoded-expansion limits. |
| RFC 2047 encoded words | **Full**, **Tolerant read** | **Write** | B/Q decoding and generation, adjacent words, charset retention, segmentation, and legal folding. |
| RFC 2231 extended MIME parameters | **Full**, **Tolerant read** | **Write** | Charset/language values, continuations, malformed gaps/duplicates, and deterministic splitting. |
| SMTPUTF8 and internationalized headers | **Full**, **Tolerant read** | **Write** | Explicit SMTPUTF8 parse/write policy plus Unicode and IDNA address-domain formatting; no SMTP transport is included. |
| `message/rfc822`, `message/global`, and `multipart/digest` | **Full**, **Tolerant read** | **Write** | Nested message parsing and canonical or preserving MIME serialization. |
| `message/partial` | **Full**, **Tolerant read** | **Write** | Metadata parsing, bounded collection/assembly, and generic MIME writing; transport-level fragment delivery is outside scope. |
| `message/external-body` | **Full**, **Tolerant read** | **Write** | Metadata and source representation only; referenced content is never fetched. |
| Legacy uuencode blocks | **Tolerant read** | **Write** | Isolated detection and bounded decoding; generation is explicit opt-in and is not selected as normal MIME output. |
| SMTP DATA byte formatting | — | **Write** | CRLF conversion, dot-stuffing, final CRLF, and terminator only; SMTP connection and submission are **Out of scope**. |

### Content, reports, and storage formats

| Standard or feature | Read / inspect | Generate / write | Notes |
| --- | --- | --- | --- |
| HTML mail processing | **Full**, **Tolerant read** | — | Bounded tokenizer, text projection, allowlist sanitization, CID handling, and data-URI extraction for the documented safe-display subset; browser conformance and execution are **Out of scope**. |
| `multipart/report` and DSN | **Full**, **Tolerant read** | **Write** | Generic reports plus delivery-status message/recipient blocks, internationalized variants, and original content. |
| MDN and receipt-request fields | **Full**, **Tolerant read** | **Write** | Message/disposition notifications and request-field parsing; sending receipts is **Out of scope**. |
| ARF feedback reports | **Full**, **Tolerant read** | **Write** | Feedback metadata and reported original headers/messages. |
| iCalendar/iMIP | **Full**, **Tolerant read** | **Write** | Bounded calendar method, participants, dates, zones, recurrence-adjacent raw extensions, and MIME composition; calendaring transport is **Out of scope**. |
| `.eml` | **Full** | **Write** | Explicit load/save helpers with overwrite and ownership policies. |
| mboxo and mboxrd | **Full**, **Tolerant read** | **Write** | Streaming envelopes, `From ` escaping, append support, offsets, limits, and deterministic corruption recovery. |
| Maildir | **Full** | **Write** | Enumeration, standard flags, safe names, and `tmp`/`new`/`cur` same-filesystem moves; mailbox synchronization is **Out of scope**. |
| TNEF / common MAPI properties | **Full**, **Tolerant read** | **Out of scope** | Bounded container/property extraction and TNEF-to-MIME conversion are implemented; TNEF generation is not. Unknown attributes remain available. |

### Authentication and end-to-end security

| Standard or feature | Verify / read | Sign, encrypt, or write | Notes |
| --- | --- | --- | --- |
| Authentication-Results and Received-SPF fields | **Full**, **Tolerant read** | **Write** | Structured values are parsed and formatted. SPF evaluation and DMARC policy decisions are **Out of scope**. |
| DKIM RSA-SHA256 | **Full**, **Caller key** | **Write**, **Caller key** | Simple/relaxed canonicalization, repeated-field selection, timestamps, expiration, body-length tags, and key policy. Callers supply parsed/resolved public keys or RSA private keys. |
| DKIM RSA-SHA1 | **Caller key** | **Unsupported algorithm** | Legacy verification is explicit opt-in; creation of new SHA-1 signatures is prohibited. |
| DKIM Ed25519-SHA256 | **Unsupported algorithm** | **Unsupported algorithm** | Tags and key metadata remain representable, but the target BCL lacks the required built-in signing/verification primitive. |
| ARC with RSA-SHA256 | **Full**, **Caller key** | **Write**, **Caller key** | Set collection, chain validation, Authentication-Results linkage, Message Signature verification, seal verification, and sealing. |
| CMS DER/BER structures | **Full**, **Tolerant read** | **Write** | Bounded ASN.1 reading and deterministic CMS structures for the supported S/MIME operations. Unknown or unavailable algorithms receive explicit results. |
| S/MIME signed data | **Full**, **Caller key** | **Write**, **Caller key** | Detached and opaque forms, multiple signers, RSA PKCS#1 or ECDSA with SHA-256/384/512, certificate metadata, and caller-configured chain policy. SHA-1 is verify-only opt-in. |
| S/MIME enveloped data | **Full**, **Caller key** | **Write**, **Caller key** | Multiple recipients; AES-128/256 CBC or GCM and RSA OAEP-SHA256. RSA PKCS#1 transport is decrypt-only by default and encryption requires explicit policy opt-in. |
| S/MIME compressed data | **Full** | **Write** | Bounded supported compression; unavailable compression algorithms are **Unsupported algorithm** and retained losslessly where possible. |
| OpenPGP armor and packet model | **Full**, **Tolerant read** | **Write** | ASCII armor/checksums, packet lengths including partial lengths, fingerprints, and supported packet types. Unknown packets and algorithms are retained or reported explicitly. |
| OpenPGP primitive subset | **Full**, **Caller key** | **Write**, **Caller key** | RSA/ECDSA, AES-128/192/256, ZIP/zlib, SHA-2, runtime-supported SHA-3/AES-GCM, and bounded S2K behavior. DSA, ElGamal, Ed25519/X25519, unsupported AEAD modes, Argon2 S2K, and unavailable runtime primitives are **Unsupported algorithm**. |
| PGP/MIME | **Full**, **Caller key** | **Write**, **Caller key** | Sign/verify and encrypt/decrypt with caller-supplied keys plus generic `multipart/signed` and `multipart/encrypted` invariant checking. |
| Inline and cleartext OpenPGP | **Tolerant read**, **Caller key** | **Out of scope** | Read-only interoperability is implemented; new inline/cleartext generation is not. |
| S/MIME and PGP protected headers | **Full** | **Write** | Construction/extraction and outer/inner mismatch reporting with signature-preserving mutation guards. |
| DNS/key-server lookup, certificate discovery, and network trust | **Out of scope** | **Out of scope** | Callers resolve DKIM/ARC records, locate OpenPGP keys and S/MIME certificates, and choose trust/revocation policy. |

The matrix does not claim blanket compatibility with every mail client. Strict,
tolerant, forensic, preserving, and untrusted-input policies make recovery and
resource behavior explicit; unsupported values remain inspectable whenever safe
preservation is possible.

## Explicit exclusions

This is a message-format library, not a mail transport, mailbox service, or
network client. It does not:

- connect to, authenticate with, send through, or listen as an SMTP or message
  submission server; SMTP DATA adaptation is formatting only;
- implement IMAP, POP3, JMAP, Exchange, Microsoft Graph, Gmail, or any other
  remote mailbox/provider API;
- synchronize mailboxes, manage server folders or flags, watch for new mail, or
  provide server-side search;
- perform DNS lookups, fetch DKIM/ARC keys, evaluate SPF, make DMARC policy
  decisions, discover certificates, contact key servers, or establish network
  trust; callers supply already-resolved security material;
- fetch attachment or body content from URLs, including `message/external-body`
  references and remote HTML resources;
- send calendar invitations, delivery reports, read receipts, bounces, or bulk
  mail; it may construct or parse their message representation only;
- provide contacts, templates, mailbox persistence, antivirus or spam engines,
  OCR, a browser, or a general document parser.

Applications may combine this library with separate transport, DNS, storage,
and security-policy components. Those integrations remain owned by the caller
and are intentionally outside this package's compatibility surface.

## Repository layout

- `src/` contains the .NET 10 library.
- `tests/` contains its tests.
- `tests/TEST_DATA.md` defines the privacy, licensing, and provenance policy for
  fixtures, generated vectors, corpora, fuzz seeds, and benchmark inputs.
- `benchmarks/` is reserved for the dependency-free benchmark harness and data.
- `syntax.md` defines the permitted C# syntax for production and test code.
- `docs/api.md` defines naming, null, option, result, issue, and exception
  conventions.
- `docs/issues.md` defines diagnostic severity, stable code ranges, locations,
  and recovery context.
- `docs/source-locations.md` defines checked byte ranges, counters, line
  accounting, and entity path behavior.
- `docs/limits.md` records secure resource ceilings and customization rules.
- `docs/cancellation-progress.md` defines synchronous cancellation and
  thread-safe progress reporting.
- `docs/byte-primitives.md` defines ASCII scanning, hexadecimal conversion,
  delimiter search, and checked size arithmetic.
- `docs/line-reader.md` defines incremental bounded line splitting and original
  newline preservation.
- `docs/buffer.md` defines pooled buffering, bounded growth, clearing, and
  deterministic release.
- `docs/content.md` defines content modes, ownership, repeatability, and bounded
  materialization.
- `docs/storage.md` defines threshold-based memory spooling and temporary-file
  cleanup.
- `docs/hash.md` defines exact-byte and caller-decoded streaming hashes.
- `docs/base64.md` defines incremental MIME Base64 encoding and strict or
  tolerant decoding.
- `docs/quoted-printable.md` defines incremental MIME body quoted-printable
  encoding and strict or tolerant decoding.
- `docs/identity-transfer.md` defines validation and streaming copy behavior
  for the 7bit, 8bit, and binary identity transfer encodings.
- `docs/uuencode.md` defines isolated detection, tolerant decoding, and
  explicit canonical generation for legacy uuencode blocks.
- `docs/ownership.md` defines resource ownership, explicit cleanup, and thread
  safety.
- `todo.md` defines the planned implementation sequence and completion state.
