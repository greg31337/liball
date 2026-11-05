# Memory and temporary-file spooling

`EmailTemporaryStore` is a bounded synchronous sink that begins in memory and
moves to one temporary file when its configured threshold is crossed. It does
not move back to memory. Existing bytes are copied before the write that caused
the transition, preserving exact order across arbitrary input chunks.

## Policy

`EmailStorageOptions` contains a cloned `EmailTemporaryStorageLimits`, an
existing absolute temporary directory, a sensitive-memory flag, and a positive
copy-buffer size. A store snapshots the complete options object at construction,
so later caller changes do not affect an active write.

Content remains in memory while its length is less than or equal to
`MemoryThresholdBytes`. A zero threshold accepts empty memory content and
spools the first byte. Once a file is required, `MaxTemporaryBytes` is checked
before creating or writing it. A failed size check leaves length and stored
bytes unchanged, allowing the caller to close the store or explicitly complete
the deterministic prefix.

Each store creates at most one uniquely named file with exclusive access in the
configured directory. The other temporary-storage count limits are available
for higher-level coordinators managing multiple simultaneous stores.

## Completion and cleanup

`Complete()` closes the sink and transfers its bytes to an owned repeatable
`EmailContent`. In-memory completion returns owned memory. File completion
flushes and closes the writer, then transfers deletion responsibility to a
`TemporaryFile` content source. Closing the completed content deletes that file.
Closing the already-completed store does not affect transferred content.

Closing an unfinished store releases memory, closes any file, and deletes its
temporary path. `Close()` and its `IDisposable.Dispose()` alias are idempotent
after successful cleanup. If cleanup reports an I/O failure, the store remains
eligible for another explicit close attempt.

Sensitive mode clears the full internal memory-stream buffer before releasing
it, both during spill and memory completion. It also marks completed owned
memory for clearing when the returned content is closed. Temporary-file secure
erasure is not promised; file close performs deletion according to filesystem
semantics.
