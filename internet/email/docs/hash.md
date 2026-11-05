# Streaming hashes

`EmailHash` computes hashes with .NET cryptographic primitives and supports
SHA-256, SHA-384, and SHA-512. It intentionally omits weak general-purpose hash
choices. Any legacy algorithm required by a later protocol implementation is
kept behind that protocol's explicit insecure-compatibility policy.

## Exact and decoded inputs

`ComputeExact` hashes the bytes exposed by an `EmailContent` without newline,
header, transfer-encoding, or MIME normalization. Memory and file read streams
opened internally are closed after hashing. A stream-mode content source is the
caller's retained stream and is not closed; seekable content resets to its
captured start position through `OpenRead`, while non-seekable content remains
one-shot.

`ComputeDecoded` hashes a caller-supplied stream or span that already contains
decoded bytes. It does not infer or apply a content-transfer encoding. The
stream is borrowed, begins at its current position, and remains open. Codec and
MIME layers can therefore feed their decoded output without materializing it.

## Bounds and results

Streaming entry points require an explicit non-negative maximum byte count and
offer a default 81,920-byte transfer buffer or a positive caller-selected
buffer size. Known oversize inputs fail before a read. Unknown-length inputs are
counted while streaming and fail with `EmailLimitException` when a read crosses
the bound. The temporary transfer array is zeroed on normal completion and on a
reported size-limit failure.

`EmailHashResult` records the algorithm, number of bytes hashed, and a defensive
copy of the digest. It provides lowercase hexadecimal output and fixed-time
digest comparison through `CryptographicOperations.FixedTimeEquals`. Returned
digest arrays are snapshots owned by the caller.

