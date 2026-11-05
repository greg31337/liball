# Custom random sources

Distribution, collection, geometry-sampler, and quasi-random scrambling APIs
accept `IRandomSource`. `RandomGenerator` is the standard implementation, so
existing calls continue to work:

```csharp
RandomFactory factory = new RandomFactory();
IRandomSource source = factory.CreateDefaultGenerator(
    42UL,
    RandomMathMode.Portable);

double value = new NormalSampler(source, 0.0, 1.0).Sample();
```

The interface has five methods: `MathMode`, `NextUInt32`, `NextUInt64`,
`NextBoolean`, and `FillBytes`. An implementation must return unbiased values
over each method's complete output domain. Calls mutate the source unless its
implementation explicitly documents otherwise, and concurrent use requires
the same external synchronization as `RandomGenerator`.

Bounded integers, floating-point endpoint variants, probabilities, and other
transforms are intentionally absent. The library derives those operations from
the primitive methods, which keeps custom implementations and test doubles
small and keeps bias-sensitive algorithms in one implementation.

`IRandomSource` does not promise state export, cloning, reset, spawning, or
named-algorithm replay. APIs that must clone a stream to preview variable-size
output continue to require `RandomGenerator`. Implementing `IRandomSource`
also does not make a source cryptographically safe. Secrets must still use
`SecureRandomGenerator` and its dedicated helpers.

In tests, a scripted implementation can return selected words or bits and count
calls. This directly exercises rejection boundaries, endpoint handling,
degenerate cases, and exact entropy consumption without constructing a real
engine or manufacturing a serialized engine state.
