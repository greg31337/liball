# Secure-token generation

Use `SecureRandomGenerator` for secrets. Deterministic `RandomGenerator`
instances are for simulation and replay and must not generate tokens,
passwords, reset links, API keys, or security-sensitive identifiers.

## Select a secure source

```csharp
using random;

RandomFactory factory = new RandomFactory();
SecureRandomGenerator secure = factory.CreateSystemSecureGenerator();
```

`CreateSystemSecureGenerator()` sends every request to the operating-system cryptographic
source. `CreateHmacDrbg()` instantiates HMAC-SHA-256 DRBG from fresh operating
system entropy and a nonce, then maintains mutable DRBG state. Operating-system
failures propagate; neither factory falls back to a clock or deterministic seed.

The overload accepting entropy, nonce, and personalization is intended for
callers that already operate a suitable entropy pipeline and for conformance
work. Length checks cannot prove unpredictability. Fixed test vectors are not
security-grade entropy.

## Tokens

```csharp
string resetToken = secure.TokenBase64Url(32); // 256 source bits
string hexKey = secure.TokenHex(32);           // 64 hex characters
string base32Code = secure.TokenBase32(20);
```

The byte count determines entropy. Encoding does not add entropy:

- `TokenHex` emits two uppercase hexadecimal characters per byte.
- `TokenBase32` emits uppercase RFC-style base32 without padding.
- `TokenBase64` includes standard padding.
- `TokenBase64Url` uses the URL-safe alphabet without padding.

Use `TokenEntropyBits(byteCount)` when displaying or enforcing a byte-based
entropy policy.

For a custom alphabet, every UTF-16 character must be distinct and must not be
a surrogate:

```csharp
string code = secure.String("ABCDEFGHJKLMNPQRSTUVWXYZ23456789", 20);
double bits = factory.StringEntropyBits(
    "ABCDEFGHJKLMNPQRSTUVWXYZ23456789",
    20);
```

Selection uses unbiased bounded integers; alphabet sizes need not be powers of two.

## Password policies

```csharp
string lower = "abcdefghijkmnopqrstuvwxyz";
string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
string digits = "23456789";
string alphabet = lower + upper + digits;

PasswordPolicy policy = new PasswordPolicy(
    20,
    alphabet,
    [lower, upper, digits],
    [1, 1, 2],
    true,
    true);

string password = secure.Password(policy);
```

Required groups must be subsets of the full alphabet, and their required counts
must fit the requested length. Policies can forbid repeated characters and can
disable the final shuffle, but those choices change the output space and may
reduce entropy. The policy is validated and snapshotted before entropy is
requested.

The returned token or password is an immutable .NET string and cannot be
reliably zeroized. Keep it out of logs, exception messages, telemetry, command
lines, and long-lived caches.

## UUIDs

`Uuid4()` uses 122 effective random bits after version and variant bits are set.
`Uuid7()` combines a millisecond timestamp, a monotonic per-instance counter,
and random bits. UUIDs are identifiers, not bearer secrets; their visible
structure and collision properties differ from opaque 256-bit tokens.

## HMAC-DRBG lifecycle

HMAC-DRBG generation requests are limited to 65,536 bytes; `FillBytes` splits
larger destinations into compliant requests. `Reseed()` obtains fresh operating
system entropy. Additional input is mixed into the DRBG but is not entropy.
Prediction-resistant requests reseed before generating. Call `Uninstantiate()`
when permanently retiring an HMAC-DRBG instance; later use throws
`RandomSecurityException`.

The library makes no FIPS 140 validation or independent certification claim.
Applications remain responsible for entropy quality, access control, token
lifetime, transport, storage, comparison behavior, and rotation. Read the
[security threat model](../security-threat-model.md) before deploying the secure API.
