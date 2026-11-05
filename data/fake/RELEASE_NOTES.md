# Package release notes

## Unreleased

Prepared 2026-08-15.

### Construction APIs

- Organized every public type under `src/Api` by contract, implementation,
  data, enum, or exception category. Behavior-bearing classes now implement
  explicit public contracts.
- Removed public static factory methods from public types. Construct a root
  generator with the single `Fake(FakeOptions)` constructor and initialize
  `FakeOptions` through its public fields.

### Nullability

- Enabled nullable-reference analysis and annotated APIs that deliberately
  produce or store absent values, including `Maybe`, schema constants and
  results, optional language-tag components, optional time-zone comments, and
  optional phone safety ranges. Non-nullable record text and array fields now
  start with empty values instead of transient nulls.

### Interfaces

- Added the public `IChecksum` contract across all append-style checksum
  engines and `ChecksumProvider.Engine(ChecksumKind)` for kind-based selection
  without concrete-type branching.
- Decoupled standards generators from the concrete deterministic random
  provider through a minimal internal random-source contract, enabling exact
  scripted tests of entropy and digit selection while retaining seeded public
  behavior.

### Bundled data

- Locale data version: `1`, independently selected with
  `FakeOptions.DataVersion`.
- Locale coverage: 38 reviewed packs. All bundled packs declare data version
  `1` and resource schema version `1`.
- Locale source baseline: Unicode CLDR 47, released 2025-03-13. Supporting
  locale registries and native/fallback coverage were reviewed 2026-08-15.

The following is the release snapshot of the exact upstream version/date
evidence in `src/Data/manifest.json`. “Retrieved” is used where the authority
does not provide a separate immutable release identifier.

| Bundled dataset | Data version | Upstream version or date | Retrieved |
| --- | --- | --- | --- |
| Invariant reserved constants | `1` | RFC 2606 (June 1999), RFC 5737 (January 2010), RFC 3849 (July 2004), and NANPA page retrieved 2026-08-14 | 2026-08-14 |
| Invariant authored words | `1` | Repository review 2026-08-14 | 2026-08-14 |
| Currency codes | `1` | SIX List One published 2026-01-01 | 2026-08-14 |
| IBAN formats | `1` | schwifty 2026.7.3 | 2026-08-15 |
| Language tags | `1` | IANA registry file date 2026-08-08 | 2026-08-14 |
| Media types | `1` | IANA registry updated 2026-08-13 | 2026-08-14 |
| Service names and ports | `1` | Snapshot retrieved 2026-08-14 | 2026-08-14 |
| Safe network/contact ranges | `1` | RFC 2606 (June 1999), RFC 5737 (January 2010), RFC 3849 (July 2004), and NANPA page retrieved 2026-08-14 | 2026-08-14 |
| Root-zone TLDs | `1` | IANA serial 2026081400 | 2026-08-14 |
| Time-zone identifiers | `1` | IANA `zone1970.tab` annotation 2025-05-15 | 2026-08-14 |

Other reviewed standards and factual source-code tables in this package use
GS1 General Specifications 26.0, GSMA TS.06 28.1, ITU-T E.118 (09/2025),
ITU-T E.212 (06/2024), W3C CSS Color Module Level 4 Candidate Recommendation
Draft dated 2026-08-06, and the ninth edition of the BIPM SI Brochure (2019),
updated 2026. Their redistribution boundaries and review decisions are recorded
in `src/Data/SOURCES.md`, `src/Data/LICENSES.md`, and
`src/Data/THEMATIC-REVIEW.md`.

### Release process

Bundled data is acquired and transformed offline, admitted only after source,
license, safety, and locale/fallback review, and verified against committed
hashes and deterministic regeneration. The complete release gate and rollback
rules are documented in `src/Data/REFRESH.md`.
