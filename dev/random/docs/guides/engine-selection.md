# Engine selection

Choose security first. `RandomGenerator` engines are deterministic and are not
suitable for secrets. For tokens, passwords, session identifiers, or other
security-sensitive values, use `SecureRandomGenerator` as described in the
[secure-token guide](secure-token-generation.md).

## Deterministic engines

| Factory | Good default use | Native positioning features |
| --- | --- | --- |
| `CreateDefault` / `CreatePcg64Dxsm` | General simulation and reproducible application work | Arbitrary `Advance` |
| `CreateXoshiro256StarStar` | Fast sequential simulation with conventional substream jumps | `Jump`, `LongJump` |
| `CreatePhilox4x64Ten` | Counter-based workloads and explicit block positioning | `Seek`, block reservation metadata |
| `CreateMt19937` | Compatibility with reference-initialized MT19937 data | State and clone |
| `CreateMt19937Numpy` | NumPy `SeedSequence` initialization and raw MT19937 word compatibility | State and clone |
| `CreateSplitMix64` | Small, simple streams and exact arbitrary advancement modulo its cycle | Arbitrary `Advance` |

`CreateDefault` currently selects PCG64 DXSM with stream selector zero. Its
choice is stable for the library major version, but persisted or long-lived
work should record the named algorithm instead of relying on the default.

NumPy compatibility applies only where explicitly documented. Matching the
NumPy MT19937 bit generator does not imply that higher-level floating or
distribution transforms match NumPy's `Generator` methods.

## Select the math mode explicitly

```csharp
using random;

RandomFactory factory = new RandomFactory();
RandomGenerator exactReplay = factory.CreatePcg64Dxsm(
    (UInt128)1234,
    (UInt128)7,
    RandomMathMode.Portable);

RandomGenerator platformMath = factory.CreatePcg64Dxsm(
    (UInt128)1234,
    (UInt128)7,
    RandomMathMode.System);
```

`Portable` uses versioned math kernels. With the same named engine version,
transform version, seed or state, parameters, and call sequence, distribution
results replay bit-for-bit on supported .NET 10 platforms.

`System` uses platform math functions. It preserves the distribution and
statistical behavior, but does not promise identical last bits or the same
engine state after a distribution call. Raw words and conversions that do not
invoke mathematical kernels remain exact in either mode.

## Discover capabilities

```csharp
RandomAlgorithmInfo info = exactReplay.Info();

if (info.HasCapability(RandomCapability.Advance))
{
    exactReplay.Advance((UInt128)1_000_000);
}
```

Every `RandomGenerator` supports `State`, `Clone`, `Reset`, and deterministic
indexed `Spawn` at the generator layer. Engine-specific methods such as
`Advance`, `Seek`, `Jump`, and `LongJump` throw `RandomCapabilityException`
when unsupported. Query `Info()` when code accepts a caller-selected engine.

## Ownership

A generator is mutable. Do not share one instance between concurrent callers
without synchronization: scheduling would make consumption order part of the
result. Prefer one deterministic child per worker; see
[parallel spawning](parallel-spawning.md).
