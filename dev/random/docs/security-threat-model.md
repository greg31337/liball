# Security threat model

This document describes the security assumptions and failure boundaries of
`SecureRandomGenerator`. It is an engineering threat model, not a certification,
compliance statement, or substitute for an application security review. The
[behavioral contract](behavioral-contract.md) is normative for public API behavior.

## Scope and security objectives

The secure API is intended to:

- obtain bytes from the operating-system cryptographic random-number generator,
  either directly or as HMAC-DRBG seed material;
- prevent deterministic `RandomGenerator` instances from being passed into
  token, password, UUID, or secure bounded-integer helpers;
- avoid modulo bias in bounded integers and custom-alphabet selection;
- enforce HMAC-DRBG input, request, reseed, and lifecycle limits before output;
- avoid silent entropy fallbacks; and
- clear mutable secret intermediates when the runtime exposes a suitable operation.

The protected assets are unpredictable output, retained HMAC-DRBG key/value
state, caller-provided entropy and additional input while in use, and generated
tokens or passwords until the application deliberately releases them.

Availability, application authorization, token storage, protocol design,
rate-limiting, audit policy, and resistance to a fully compromised host are
outside the library's control.

## Security boundary

`SecureRandomGenerator` is the only API surface intended for security-sensitive
randomness. `RandomGenerator`, deterministic seeds and states, QMC sequences,
and spawned deterministic streams are predictable replay tools even when their
initial seed came from operating-system entropy or their output passes
statistical tests.

The secure surface has two modes:

| Factory | Retained random state | Entropy dependency | Replay/state API |
| --- | --- | --- | --- |
| `RandomFactory.CreateSystemSecureGenerator()` | No userspace PRNG state; `Uuid7` retains ordering metadata | Every nonempty random request uses the OS CSPRNG | None |
| `RandomFactory.CreateHmacDrbg()` | HMAC-SHA-256 DRBG key, value, and reseed counter | OS entropy and nonce at instantiation; OS entropy at reseed | None |
| `RandomFactory.CreateHmacDrbgFromSeedMaterial(...)` | Same HMAC-DRBG state | Caller is solely responsible for entropy and nonce quality | None |

The direct system source and HMAC-DRBG both rely on .NET cryptographic
primitives and the host operating system. The library does not replace or
independently health-test the operating-system entropy source.

## Trusted dependencies and assumptions

Security assumes all of the following:

- the operating system, .NET runtime, `RandomNumberGenerator`, HMAC-SHA-256,
  process memory isolation, and hardware execute correctly;
- OS random output is unpredictable and remains correctly initialized across
  startup, suspend/resume, virtualization, and process boundaries;
- caller-supplied entropy is genuinely unpredictable when that overload is used;
- the caller prevents unauthorized access to live process memory, crash dumps,
  debugger sessions, swap, logs, and generated secrets; and
- applications use sufficient token entropy and appropriate storage, expiry,
  comparison, transport, revocation, and rotation controls.

A compromised kernel, runtime, hypervisor, hardware random source, or process can
observe or alter inputs, state, and output and is outside the defended boundary.

## Operating-system entropy dependency

`CreateSystemSecureGenerator()` delegates each request to `RandomNumberGenerator.Fill` in
chunks. `CreateHmacDrbg()` obtains 32 entropy bytes and 16 nonce bytes from the
same source. `Reseed()` and prediction-resistant generation obtain 32 fresh
entropy bytes.

OS entropy failures propagate to the caller. The implementation never replaces
a failed request with a clock, process identifier, GUID, deterministic engine,
or previously cached bytes. Applications must treat such a failure as a failure
to generate the secret, not as permission to retry with a weaker source.

A fill that spans multiple OS or DRBG request chunks can fail after earlier
chunks changed the destination and, for HMAC-DRBG, advanced state. On any such
exception, discard or clear the entire destination; do not use its prefix as a
shorter secret. High-level token and password methods return no result unless
their complete operation succeeds.

The library cannot certify that caller-provided bytes contain the entropy their
length suggests. Additional input and personalization can provide domain
separation or context, but neither becomes entropy merely by being mixed into
HMAC-DRBG.

## HMAC-DRBG construction and limits

The retained generator is the library's versioned HMAC-SHA-256 DRBG targeting a
256-bit security strength. Public validation enforces these limits:

| Item | Limit or behavior |
| --- | --- |
| Entropy at instantiation or reseed | At least 32 bytes |
| Nonce at instantiation | At least 16 bytes |
| Personalization or additional input | At most 4,294,967,296 bytes by algorithm policy; practical .NET array and combined-operation limits are lower |
| Combined material in one operation | Must fit a single .NET array, currently at most `int.MaxValue` bytes |
| One `Generate` request | At most 65,536 output bytes |
| `FillBytes` larger than one request | Split into consecutive requests of at most 65,536 bytes |
| Reseed interval | `2^48` successful nonempty generate requests |
| Empty request | Validates inputs but produces nothing and does not advance or reseed state |

After instantiation or reseed, the reseed counter is one. Every successful
nonempty generation increments it. When the next ordinary public request would
exceed the `2^48` interval, `SecureRandomGenerator` first attempts an automatic
OS-backed reseed. Generation does not proceed if that reseed fails.

`Generate(destination, additionalInput, true)` obtains fresh OS entropy,
reseeds while mixing the additional input, and then generates. This is the
prediction-resistance request path. Additional input on an ordinary request is
mixed before and after output as defined by the DRBG update procedure, but it
does not compensate for weak or compromised entropy.

The DRBG is mutable and not implicitly thread-safe. Concurrent calls without
external synchronization can corrupt application assumptions about request
ordering and must not be used.

## Process cloning, forks, and snapshots

The library does not automatically detect process identifiers, `fork`, VM
snapshot restore, container checkpoint/restore, debugger cloning, or similar
duplication boundaries.

An HMAC-DRBG instance copied at such a boundary has identical key, value, and
reseed-counter state in every copy. If copies receive the same subsequent call
sequence, they can emit identical output. UUIDv7 ordering metadata is also
copied. Never assume that parent and child instances are independent.

Before any cloned process emits security-sensitive output, each resulting
process must do one of the following:

1. discard the inherited instance and create a new secure generator from the OS;
2. call `Reseed()` successfully on an inherited HMAC-DRBG; or
3. make its first HMAC-DRBG generation request prediction-resistant.

Recreating the generator is the clearest default. Perform this response in the
parent as well when the process model cannot guarantee that only the child
inherited a snapshot. Do not use process identifiers, timestamps, child indexes,
or additional input alone as a substitute for fresh entropy.

For `CreateSystemSecureGenerator()`, the library retains no userspace PRNG state, but security
still depends on the operating system handling the cloning boundary correctly.
Recreating the wrapper after a fork or restore avoids inherited UUIDv7 metadata
and makes the boundary explicit.

## State compromise and recovery

If an attacker obtains live HMAC-DRBG key/value state, they can predict future
output until a successful reseed incorporates fresh entropy unknown to them.
Changing personalization, adding public additional input, or continuing to
generate does not recover unpredictability.

After containment, retire exposed tokens and credentials, stop using the
compromised instance, and create a new generator or reseed from a trustworthy OS
source. A fresh reseed can restore future unpredictability only if the attacker
no longer observes the process and does not know the new entropy. It cannot make
already exposed output secret again.

HMAC-DRBG updates are designed to resist reconstruction of earlier output from
current state under their cryptographic assumptions, but this library does not
claim that prior secrets are safe after a process compromise. Managed runtimes,
application copies, crash dumps, logging, swap, or instrumentation may retain
state or output outside the buffers the library clears.

`Uninstantiate()` zeroes the current retained HMAC-DRBG key and value buffers and
makes later generation fail. It cannot erase copies made by the runtime,
debuggers, dumps, callers, or a cloned process. Direct-system generators have no
retained DRBG state and reject uninstantiation.

Generated strings are immutable .NET objects and cannot be reliably zeroized.
Keep tokens and passwords out of logs, exception messages, telemetry, command
lines, environment variables, and unnecessarily long-lived caches.

## High-level output limitations

- Encodings do not add entropy. Token entropy is `8 * byteCount`; custom-string
  entropy assumes independent uniform selection from the validated alphabet.
- Password policy constraints change the output space. A valid policy is not an
  application-specific strength assessment.
- UUIDv4 has 122 effective random bits after required fields are set. UUIDv7
  exposes time and ordering structure and is an identifier, not an opaque secret.
- High-level transformations and comparisons are not promised to be constant-time.
- Unbiased selection prevents modulo bias; it does not prevent application-level
  enumeration, online guessing, leakage, replay, or authorization flaws.

## Non-validation disclaimer

The library, its HMAC-DRBG implementation, its use of platform cryptography, and
its secure helpers have not been represented as FIPS 140 validated, Common
Criteria evaluated, independently certified, or approved for any particular
regulatory regime. Algorithm conformance tests do not validate the surrounding
runtime, operating system, entropy source, build, deployment, or application.

Consumers must determine whether the library and platform satisfy their legal,
contractual, cryptographic, and operational requirements. Where a validated
cryptographic module is required, use an approved module and integration path
rather than treating this library's algorithm name or test vectors as evidence
of validation.

## Deployment checklist

- Use `SecureRandomGenerator`, never `RandomGenerator`, for secrets.
- Prefer `CreateSystemSecureGenerator()` unless retained DRBG state or explicit DRBG controls are required.
- Size tokens from the application's online/offline guessing threat and lifetime.
- Recreate or reseed after every process-cloning or snapshot boundary.
- Restrict memory dumps, debugging, logs, telemetry, and secret storage.
- Treat entropy failures and reseed failures as generation failures.
- Retire outputs after suspected state or process compromise.
- Call `Uninstantiate()` when permanently retiring an HMAC-DRBG instance.
- Document application-level rotation, expiry, rate-limit, and incident-response policy.
- Obtain an independent review and use validated modules where required.
