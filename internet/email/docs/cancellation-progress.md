# Cancellation and progress

Long-running synchronous operations accept caller-created coordination objects
instead of delegates, events, tasks, or asynchronous APIs.

## Cancellation

Long-running APIs accept `IEmailCancellation`. `EmailCancellation` is the
library-provided thread-safe implementation; it begins in the non-cancelled
state. `Cancel()` atomically and
permanently requests cancellation; repeated calls are harmless.
`IsCancellationRequested()` is safe to call from the worker thread while a
different thread calls `Cancel()`.

Loops check cancellation at documented bounded intervals and return their
normal result type with `OperationCancelled` and any deterministic partial value
when recovery is meaningful. The cancellation object itself does not throw and
cannot be reset, preventing an observed request from being accidentally lost.
A null cancellation setting explicitly disables cancellation checks.

## Progress

Long-running APIs report through `IEmailProgress`, whose methods receive
non-negative counter increments. This narrow surface lets an application send
updates to its own metrics or UI adapter and lets a test record calls without
depending on the library counter implementation.

`EmailProgress` is the library-provided implementation. It contains independent signed 64-bit counters for source bytes
read, output bytes written, decoded bytes produced, headers processed, entities
processed, and work operations. All counters start at zero. Additions must be
non-negative and throw before mutation if they would exceed `Int64.MaxValue`.

Updates, reads, reset, and snapshot creation are synchronized, so a caller may
poll progress from another thread. `GetSnapshot()` captures all six counters in
one consistent `EmailProgressSnapshot`; later changes do not affect it. A new
progress object can start from a snapshot, which supports explicit aggregation
or continuation without sharing mutable state.

Progress is observational. Supplying or polling it does not change limits,
cancellation, parser recovery, or serialization output. A null progress setting
disables reporting and lets a hot loop avoid counter updates.
