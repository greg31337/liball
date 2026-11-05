# Seed handling

A seed identifies a stream; it is not a measure of randomness quality. Store
the seed type and the selected factory along with the seed value. The integer
`42`, the text `"42"`, and bytes containing those characters are deliberately
different canonical seeds.

## Direct factory seeds

Use direct factories when the application already has the engine's native seed
material:

```csharp
using random;

RandomFactory factory = new RandomFactory();
RandomGenerator pcg = factory.CreatePcg64Dxsm(
    (UInt128)42,
    (UInt128)9,
    RandomMathMode.Portable);

RandomGenerator mt = factory.CreateMt19937(
    42U,
    RandomMathMode.Portable);
```

PCG's stream selector is part of stream identity. Philox's key and counter,
MT19937's scalar-versus-array initializer, and the distinction between
reference and NumPy MT19937 are also significant. Do not silently convert one
form into another when replay matters.

## Canonical seed sequences

`SeedSequence` accepts integers, bytes, text, unsigned-word arrays, or operating
system entropy. It derives bytes deterministically from the root material and
the complete unsigned spawn path.

```csharp
using System.Buffers.Binary;
using random;

RandomFactory factory = new RandomFactory();
SeedSequence root = factory.CreateSeedFromText("experiment-2026-08");
SeedSequence child = root.SpawnPath([3UL, 17UL]);
byte[] material = child.GenerateBytes(8);
ulong seed = BinaryPrimitives.ReadUInt64LittleEndian(material);

RandomGenerator generator = factory.CreateSplitMix64(
    seed,
    RandomMathMode.Portable);
```

Use an explicit byte order when converting derived bytes. `BitConverter` uses
host byte order and is inappropriate for a persisted cross-platform mapping.

Child identity depends on the root and full path, not on the order in which
children are requested. Give indexes stable application meaning—for example,
dataset partition and worker number—instead of assigning them from completion
order.

## Entropy and confidentiality

`RandomFactory.CreateSeedFromSystemEntropy()` obtains 256 bits directly from the operating
system CSPRNG. Its result is intentionally not replayable unless the resulting
root material is persisted elsewhere by the application. Operating-system
failures propagate; there is no time-based or deterministic fallback.

Deterministic seed material is not a secret-generator API. `Fingerprint()` is
useful for diagnosing whether two roots match, but it reveals that equality and
must not be used as a secret. For cryptographic output, use
`SecureRandomGenerator`.

## What to persist

For replay from the beginning, persist:

- the factory or `RandomAlgorithm` identity and algorithm version;
- the exact typed seed, key, stream selector, or canonical root material;
- the complete spawn path;
- `RandomMathMode`; and
- the library transform version when it is part of a long-lived compatibility record.

To resume in the middle of a stream, persist `RandomState` instead; see
[state persistence](state-persistence.md).
