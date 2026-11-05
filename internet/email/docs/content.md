# Content sources

`EmailContent` represents retained bytes in one of four modes: memory, file,
caller stream, or owned temporary file. It exposes synchronous reads and never
fetches network content.

## Ownership

`EmailContentOwnership.Borrowed` leaves the source under caller ownership.
Closing borrowed memory does not modify its array, closing a borrowed stream
does not close it, and closing a borrowed file does not delete it.

`EmailContentOwnership.Owned` is an explicit transfer. Owned sensitive memory
is zeroed on close, owned streams are closed, and owned files are deleted.
`CreateMemoryCopy` creates a separate owned array and is the safe default when
the caller should retain independent mutable memory. A temporary-file content
source is always owned and deleted on close.

File paths are converted to absolute paths and must identify an existing file
at creation. The library retains no open handle between reads. Streams returned
for memory and file modes are new reader objects that callers close. Stream-mode
`OpenRead()` returns the supplied stream itself; its ownership does not transfer
to the caller merely because it was returned.

`Close()` is idempotent after successful cleanup, and `Dispose()` is its
`IDisposable` alias. Kind, ownership,
repeatability, known length, and file path are metadata available after close;
opening or materializing closed content throws `ObjectDisposedException`.

## Repeatability and length

Memory, file, and temporary-file modes are repeatable. Each file read opens the
stored absolute path again; external file changes remain the responsibility of
the owner. A seekable stream is repeatable from the position it had at content
creation, and each open resets it to that position. A non-seekable stream is
one-shot and rejects a second open.

Known length is captured at creation for memory, files, and seekable streams.
Non-seekable streams report unknown length. The value is metadata, not a lock
against caller mutation or external file replacement.

## Bounded materialization

`Materialize(maximumBytes)` always requires an explicit non-negative bound no
larger than `Int32.MaxValue`, because its result is one byte array. Known
oversize content is rejected before reading. Unknown or externally grown input
is counted while reading and throws `EmailLimitException` as soon as a read
would cross the bound. For a one-shot stream, bytes already read before that
failure remain consumed.

Materialization returns a caller-owned copy and never changes ownership of the
underlying source. Large-content streaming operations remain separate APIs and
do not imply unbounded materialization.
