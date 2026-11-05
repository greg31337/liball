# Ownership and thread safety

This document defines resource lifetime, buffer ownership, and concurrency
rules for the `Email` library. Feature-specific APIs may choose among the
ownership modes described here, but must document the choice and must not rely
on an implicit transfer.

## Ownership rules

Passing a stream, byte array, memory value, file path, key, certificate, or
other caller-created object does not transfer ownership by default. The caller
must keep borrowed input usable for the documented duration and remains
responsible for closing or clearing it. A library operation closes a supplied
resource only when the API has an explicit take-ownership setting enabled.

The library owns resources that it creates, including temporary files, opened
file handles, rented buffers, and internally created streams. It releases those
resources when the owning operation finishes or when the retaining object is
closed. Temporary storage is not transferred merely because a caller receives
a message, entity, or result that refers to it; that object's documentation
identifies which object controls the lifetime.

Returned arrays and mutable collections are snapshots owned by the caller
unless documented otherwise. `ReadOnlyMemory<byte>` may refer to library-owned
or caller-owned storage; the producing method documents its lifetime. Retained
content never stores a span. Methods named `DetachSource` or `Materialize`
create owned content according to their options so it no longer depends on the
borrowed source, subject to configured limits.

Copying a reference to an `EmailContent`, message, or entity does not clone its
resources. A deep `Clone` documents whether content is copied, independently
opened, or shared read-only. Closing one object must not invalidate an
independent clone, but may invalidate another object that explicitly shares the
same owner.

## Close and Dispose convention

Resource-retaining library classes expose a public `Close()` method. They do
implement `IDisposable`, making them usable through the standard C# `using`
statement and generic .NET lifetime infrastructure. `Dispose()` is an exact
alias for `Close()` when the class exposes both methods.

`Close()` has the following contract:

- it is idempotent and may be called more than once;
- it releases owned streams, files, temporary storage, rented buffers, and
  cryptographic material, clearing sensitive writable buffers where practical;
- it does not close borrowed resources;
- after it returns, methods that require released content throw
  `ObjectDisposedException`;
- metadata getters that are explicitly documented as available after close
  remain usable;
- it does not suppress an I/O failure from the first close when reporting that
  failure is necessary to establish whether output was completed.

Callers normally use a `using` statement or an equivalent explicit block to
ensure `Dispose()` or `Close()` is called. APIs that fully consume temporary resources inside a single method
clean them up before returning and do not require a caller-visible close.

## Thread safety

Mutable instances, readers, writers, parsers, option objects, messages, MIME
entities, collections, and resource owners are not safe for concurrent use
unless their type documentation explicitly says otherwise. A caller must
synchronize mutation, reading during mutation, and closing. In particular,
`Close()` must not race with another method on the same instance.

Independent instances may be used concurrently. Static helpers with no mutable
global state are thread-safe. Immutable completed result data and caller-owned
snapshots may be read concurrently, but an object graph containing a mutable
message, shared content owner, lazy cache, or live stream retains that object's
non-thread-safe contract.

The library does not use hidden global mutable policy. Caller-registered
registries document their own locking and snapshot behavior; registration on
one registry instance does not mutate process-wide defaults. Thread safety does
not expand ownership: concurrent readers must still keep every borrowed source
alive until all reads complete.
