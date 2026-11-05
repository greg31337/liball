# Public behavioral contract

This document is the normative behavioral contract for the public API of the
`random` library. It defines which results callers may persist or compare,
which interval endpoints are possible, how deterministic state remains
compatible, who owns mutable instances, and where the security boundary lies.
API documentation may add method-specific detail, but must not weaken these
guarantees without a major-version change.

The words **must**, **must not**, **should**, and **may** describe requirements
on the implementation. A named algorithm means an algorithm identifier plus
its algorithm version. A call sequence includes every operation that can
consume, discard, seek, or otherwise change random state.

## Determinism and replay

The library provides three levels of deterministic behavior.

### Level 1: engine and uniform-bit replay

For a named deterministic engine, the following values must be bit-for-bit
identical on every supported .NET 10 platform when the initial seed or state,
stream selector, and call sequence are identical:

- raw words, requested bits, booleans, and byte fills;
- bounded integer conversions and integer span fills;
- seed expansion, explicit indexed spawn paths, jumps, advances, and seeks;
- cloning and deterministic state export/import; and
- integer-to-floating conversions that do not invoke a math-mode operation.

Byte seed encoding, word-to-byte conversion, and persisted state use their
documented canonical byte order and never host byte order. Bulk methods must
consume the same logical sequence as repeated scalar calls unless their public
documentation explicitly defines a block or counter mapping.

Replay is guaranteed only for the same named algorithm version, transform
version, math mode where relevant, initial seed or accepted state, and call
sequence. It does not extend across different overloads merely because their
result types are convertible, nor across different algorithms with similar
names.

### Level 2: distribution replay

In `Portable` math mode, a distribution sample must be bit-for-bit identical
on every supported .NET 10 platform when its named engine and algorithm
version, transform version, seed or state, parameters, and call sequence are
identical. Portable algorithms use a fixed evaluation order and the library's
versioned mathematical kernels.

In `System` math mode, the same inputs must select the same documented
distribution and remain statistically equivalent, but the last bits and the
number of raw words consumed may differ between runtime or architecture
versions. Consequently, state reached after a `System`-mode distribution call
is not a cross-platform replay boundary. Exact replay users must select
`Portable` mode.

Sampler `Fill` operations must match repeated calls to that sampler's scalar
`Sample` method unless the method documentation explicitly defines a
block/counter mapping. Distribution parameters are part of the replay input.

### Level 3: default selection

`CreateDefault` selects a documented deterministic algorithm for the current
library major version. That choice must not change in a minor or patch release,
but may change at a documented major-version boundary. Persisted or long-lived
replays must therefore store and request the named algorithm, its algorithm
version, the transform version, math mode, and state or canonical seed rather
than relying on `CreateDefault`.

Results from operating-system entropy, a secure generator, wall-clock input,
or an unspecified external collection traversal order have no replay
guarantee. Passing the same explicit entropy to the HMAC-DRBG may be useful for
published conformance vectors, but does not move that API into the public
deterministic replay surface.

## Endpoint and range conventions

Every numeric method must document its interval before release. Floating unit
interval variants use the following exact definitions:

| Contract name | Mathematical interval | Zero possible | One possible | Intended use |
| --- | --- | --- | --- | --- |
| Closed-open | `[0, 1)` | Yes | No | Default unit sampling |
| Open-open | `(0, 1)` | No | No | Logarithms and inverse transforms with singular endpoints |
| Open-closed | `(0, 1]` | No | Yes | Survival-probability transforms |
| Closed-closed | `[0, 1]` | Yes | Yes | APIs that explicitly require both endpoints |

An excluded endpoint must remain excluded after conversion to the public
numeric type; rounding must not introduce it. `NaN` and infinities are never
valid random results from a finite unit-interval method.

Unless a method explicitly says otherwise, a one-argument non-negative integer
bound denotes `[0, bound)`, and a two-argument integer range denotes
`[minimum, maximum)`. Inclusive integer APIs must be named or documented as
inclusive and must handle the full underlying integer domain without wrapping.
Empty integer ranges are invalid. Equal bounds are valid only for APIs whose
documentation defines a degenerate interval; such an API returns the bound
without consuming entropy.

Finite floating ranged methods state their endpoint policy by reference to one
of the four unit intervals above. They must not produce a value outside the
declared mathematical range because an intermediate expression overflowed or
rounded across an excluded endpoint. Non-finite bounds are invalid unless the
specific distribution explicitly defines them. A documented degenerate
floating interval returns its sole value without consuming entropy.

Probability parameters represent the closed interval `[0, 1]`. A probability
of zero must return the impossible outcome and a probability of one must return
the certain outcome without consuming entropy. Distribution support endpoints
are separate from uniform-input endpoints and must be stated by each
distribution's public documentation.

All bounded integer selection, alphabet selection, collection indexing, and
discrete choice must be unbiased. A modulo reduction is permitted only when it
is mathematically unbiased for the source domain.

## Deterministic state compatibility

State export is available only for deterministic generators. A state envelope
contains, at minimum, a format version, algorithm identifier, algorithm
version, transform version, payload length, algorithm payload, and integrity
digest in a canonical little-endian representation.

Within a library major version, a valid exported state must either import and
resume at the exact next output or be rejected with the documented state-format
exception. Import must validate the complete envelope before mutating an
existing object or publishing a newly created generator. Truncation, trailing
data, an invalid digest, an unknown format or algorithm version, an invalid
engine state, and a payload/algorithm mismatch must be rejected. Failed import
must not partially consume or alter generator state.

The integrity digest detects accidental corruption; it is not authentication
or encryption. Exported deterministic state is public-equivalent material:
anyone who obtains it can reproduce past or future output as permitted by the
algorithm. Applications must persist the entire envelope rather than only its
engine payload.

Minor and patch releases must continue to read state envelopes they emitted
for the same major version and supported named algorithm. A major release may
retire an old decoder only when that incompatibility and a migration path are
documented. Changing a random transform without changing its transform version,
or changing a named engine without changing its algorithm version, is a
contract violation.

`Clone` duplicates the complete deterministic state, including buffered words
or bits, so the clone's next result matches the original's next result. `Reset`,
`Jump`, `LongJump`, `Advance`, `Seek`, and `Spawn` have distinct documented
meanings. An unsupported operation must fail before state mutation and must
never silently substitute another operation.

## Validation, failure, and atomicity

Public APIs validate all arguments and required capabilities before consuming
random input, advancing a sequence or process, changing reusable sampler state,
or writing to a caller-owned destination. When an operation needs temporary
calculation to prove that it can succeed, it completes that validation in
temporary storage before committing visible changes. Unless a method explicitly
documents partial progress for an external source or operating-system failure,
an exception leaves the generator and destination exactly as they were before
the call.

The public exception categories are:

| Exception | Meaning |
| --- | --- |
| `RandomArgumentException` | A null input, invalid scalar domain, inconsistent parameter combination, insufficient destination, invalid count, or invalid vector/matrix shape. `ArgumentName()` identifies the primary public argument. |
| `RandomStateException` | A malformed, corrupt, incompatible, unknown-version, or algorithm-mismatched deterministic state representation. |
| `RandomCapabilityException` | The selected algorithm does not implement the requested clone, state, jump, advance, seek, spawn, reservation, reseed, additional-input, prediction-resistance, or uninstantiate operation. |
| `RandomExhaustedException` | An engine counter, finite sequence, period, output capacity, or caller-selected rejection/placement attempt budget is exhausted. |
| `RandomReseedRequiredException` | A secure DRBG has reached a request or reseed limit and generation cannot continue until a successful reseed. |
| `RandomSecurityException` | Entropy, seed strength, password/token policy, lifecycle, or another security-specific rule rejects an operation. |

These library exceptions derive directly from `System.Exception`; callers that
need category-specific handling should catch the concrete type. Failures raised
by the runtime or operating system while actually obtaining entropy or writing
through an external object may propagate unchanged when translating them would
hide the original failure.

Validation applies to each API family as follows:

- Factories and seeding validate algorithm selection, math mode, seed material,
  stream selectors, and secure entropy requirements before creating or resetting
  an instance.
- State import and reset validate the complete representation and engine payload
  before replacing current state. Unsupported lifecycle and positioning methods
  check capability metadata before dispatch.
- Raw, bounded, floating, byte, bit, and span APIs validate bounds, endpoint
  policy, bit counts, destination length, and overlap rules before drawing a
  word. A valid empty fill performs no work and consumes nothing.
- Distribution and composed-sampler constructors validate their complete
  mathematical domain before retaining a generator. Scalar and bulk sampling
  validate call-specific arguments and destinations before sampling begins.
- Collection, weighted, resampling, geometry, and matrix APIs validate counts,
  weights, shapes, support, degeneracy, and destination capacity before shuffling,
  consuming input, or writing output.
- Quasi-random and process APIs validate dimensions, indexes, horizons, step
  counts, transition data, counter capacity, and destinations before advancing
  their current index or path state.
- Secure generation validates lifecycle state, request size, reseed limits,
  additional input, prediction-resistance requirements, alphabets, policies, and
  destinations before requesting or generating secret bytes. A reseed failure
  does not reset the reseed counter or authorize generation.

Invalid deterministic cases documented to consume no entropy, such as a
zero-length fill, a degenerate accepted interval, or a probability of zero or
one, still perform validation needed to establish that the case is valid.

## Ownership and concurrency

Deterministic generators, secure generators, reusable samplers, quasi-random
sequences, and process generators are mutable and are owned by one caller at a
time. Public methods are not implicitly safe for concurrent calls unless a
specific method explicitly says otherwise. Concurrent access to one instance
without caller-provided synchronization has no ordering or replay guarantee.

Callers that need parallel deterministic work should create one explicitly
spawned, split, or counter-partitioned generator per worker before scheduling
work. Explicit child indexes and complete spawn paths determine child identity;
worker creation order and scheduling order must not. Cloning is for replay, not
for creating statistically independent streams.

External locking can serialize access to an instance, but the resulting stream
depends on lock-acquisition order. An instance must not be used after an
operation that documents it as uninstantiated, disposed, exhausted, or otherwise
terminal.

After a process-cloning or fork-like boundary, a secure generator created in
the parent must not be assumed independent in the child. Each process must
obtain fresh operating-system entropy or perform the documented secure reseed
procedure before producing security-sensitive output.

## Secure and non-secure separation

`IRandomSource` is the public dependency boundary for distribution and ordinary
sampling transforms. Implementations must supply unbiased 32-bit words, 64-bit
words, booleans, and bytes, and must report the math mode used by transforms.
The interface itself promises no state format, cloning, reset, spawning,
algorithm identity, replay compatibility, or cryptographic security. Those
promises belong only to the concrete API that explicitly documents them.

Library transforms derive bounded integers and floating endpoint variants from
the primitive interface methods. Custom sources must not pre-apply bounds or
discard parts of their declared primitive domains. APIs whose atomic capacity
validation requires an exact cloned-stream preview may require
`RandomGenerator` instead of `IRandomSource`.

The [security threat model](security-threat-model.md) describes the trust
assumptions, process-cloning response, compromise consequences, and operational
limits behind this normative separation.

`RandomGenerator` and all deterministic engines are simulation and replay
tools. They must never claim cryptographic security, even when seeded from
operating-system entropy or when their output passes statistical tests.
Deterministic state, seeds, spawned children, and outputs must be assumed
predictable to an observer who learns enough state.

`SecureRandomGenerator` is the only security-sensitive random surface. It uses
either the operating-system cryptographic random-number generator directly or
the versioned HMAC-SHA-256 DRBG instantiated and reseeded with adequate entropy.
It must not expose deterministic state export, cloning, jumping, seeking,
spawning, or construction from a weak scalar seed. Failure of the operating
system entropy source must propagate as an error; it must never fall back to a
clock, process metadata, or a deterministic generator.

Secure bounded integers, alphabet characters, tokens, passwords, and identifiers
must use unbiased selection. UUID methods set their RFC-defined version and
variant fields after secure random filling. High-level secure transformations
are not promised to run in constant time. Secret intermediate buffers must be
cleared where the platform provides an appropriate primitive, and an
uninstantiated DRBG must reject further generation.

The secure API makes no FIPS 140 validation claim. HMAC-DRBG conformance to its
documented algorithm does not certify the surrounding library, runtime, entropy
source, operating system, or application. Consumers remain responsible for
protecting secrets, selecting suitable entropy and reseed policies, and meeting
their regulatory requirements.

Code must not accept a deterministic generator where a secure generator is
required, and secure helpers must not silently delegate to deterministic
engines. Moving bytes manually between the two surfaces does not transfer their
security or replay guarantees.

## Contract evolution

Public XML documentation must identify the applicable determinism level,
interval or support endpoints, state consumption on degenerate inputs, mutable
ownership requirements, and security classification where those facts are not
already unambiguous from this document.

Patch and minor releases may fix an implementation that violates this contract.
Such a correction may change output that was never contract-compliant and must
be called out in release notes. Any intentional change to a guaranteed output
sequence requires the corresponding algorithm or transform version to change;
any weakening of this document requires a documented major-version change.
