# Pooled byte buffer

`EmailBuffer` is a reusable bounded byte accumulator backed by an
`ArrayPool<byte>`. It accepts spans only for immediate copying and never retains
them. `ToArray()` returns a caller-owned snapshot, and `CopyTo()` copies into a
caller-provided span without exposing the rented array.

## Capacity and reuse

The constructor receives an initial capacity, a hard maximum capacity, and a
sensitive-data flag. An initial capacity of zero defers renting until storage is
needed. Growth doubles usable capacity where possible but never permits logical
content beyond the configured maximum; an attempted excess throws
`EmailLimitException` before changing content.

`DiscardPrefix()` compacts unread bytes for incremental parsers. `Clear()`
retains the rental for reuse while resetting logical length. The buffer is
mutable and is not safe for concurrent use.

The pool may return an array larger than requested. `GetCapacity()` reports at
most the configured logical maximum, and append checks enforce that maximum
independently of physical array size.

## Sensitive data and release

When sensitive clearing is enabled, the entire physical array is zeroed before
every pool return, including returns caused by growth and final close. Clearing
the whole rental prevents stale bytes outside the current logical length from
surviving an earlier reuse operation. `Clear()` and `DiscardPrefix()` also zero
regions that stop being logical content.

`Close()` returns the current rental exactly once and is idempotent. `Dispose()`
is an alias for `Close()`, and the class implements `IDisposable` for standard
`using` support.
After close, content and capacity operations throw `ObjectDisposedException`.
`IsClosed()`, `GetMaximumCapacity()`, and `GetClearsSensitiveBytes()` remain
available as lifetime-independent metadata.

The non-sensitive mode does not clear on return and must be used only when
retained bytes are not confidential. Pool ownership is never transferred to the
buffer.
