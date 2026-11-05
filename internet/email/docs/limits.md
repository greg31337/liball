# Resource limits

`EmailLimits` groups independent parsing, decoding, HTML, archive, temporary
storage, and cryptographic policies. Every constructor creates concrete bounded
defaults; there is no implicit unlimited mode. Operations should clone or
snapshot the aggregate before work begins so later caller mutation cannot alter
an in-progress operation.

All maxima must be positive. The temporary-storage memory threshold alone may
be zero, which requests immediate spooling rather than disabling enforcement.
Setters reject invalid values with `ArgumentOutOfRangeException`, and aggregate
setters reject null. `Clone()` makes a deep copy of every group.

## Default parsing limits

| Setting | Default |
| --- | ---: |
| Message bytes | 100 MiB |
| Header-block bytes | 256 KiB |
| Header fields | 1,000 |
| Physical line bytes | 65,536 |
| MIME nesting depth | 30 |
| MIME parts | 1,000 |
| Parameters per header | 100 |
| Addresses per header | 1,000 |
| Encoded words per header | 256 |

The 65,536-byte parsing line ceiling is a safety bound, not a statement that an
overlong line conforms to RFC 5322. Validation and writer limits remain
separate from tolerant input storage limits.

## Default decoding and HTML limits

Decoded and decompressed output are each limited to 100 MiB, expansion to 100
times the encoded input, decoded text to 10,000,000 characters, and charset
labels to 128 characters.

HTML input and output are each limited to 5,000,000 characters, with at most
1,000,000 tokens, nesting depth 128, 128 attributes per tag, 8,192 characters
per URL, 255 characters per extracted data-URI media type, 100 extracted data
URIs, and 10 MiB of extracted data-URI bytes.

Text analysis defaults to 10,000,000 characters, 100,000 lines, and a
200-character snippet. Decoded-text search defaults to a 1,024-character query,
10,000,000 searched characters, 1,000 matches, and 120 characters of context
per match, in addition to traversal and per-entity decoding limits.

## Default archive and temporary-storage limits

Archives are limited to 1 GiB, 100 MiB per entry, and 100,000 entries. Partial
messages are limited to 1,000 fragments and 100 MiB after reassembly. TNEF is
limited to 10,000 attributes and filesystem paths to 4,096 characters.

Content crosses the default in-memory threshold at 1 MiB. Temporary storage is
limited to 1 GiB across 64 files and 64 open streams. A single materialization
is limited to 100 MiB. These values constrain library-created storage; caller
ownership rules remain those in `docs/ownership.md`.

## Default cryptographic limits

Cryptographic input and decompressed output are each limited to 100 MiB. An
operation accepts at most 32 signatures, 1,000 recipients, 4 MiB of certificate
data, 1 MiB of key data, ASN.1 depth 64, 100,000 ASN.1 elements, 100,000 OpenPGP
packets, and 256 cryptographic work operations.

These are resource ceilings, not algorithm-security policy. Key-size minima,
allowed algorithms, trust, time validity, and legacy-algorithm decisions belong
to explicit cryptographic policy objects.
