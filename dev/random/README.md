# random

`random` is a .NET 10 library for versioned deterministic random engines,
portable distribution replay, parallel stream derivation, quasi-random point
sets, and deliberately separate cryptographic generation.

```csharp
using random;

RandomFactory factory = new RandomFactory();
RandomGenerator generator = factory.CreateDefaultGenerator(
    42UL,
    RandomMathMode.Portable);

double value = new NormalSampler(generator, 10.0, 2.0).Sample();
```

Start with the [user-guide index](docs/guides/README.md), especially
[engine selection](docs/guides/engine-selection.md) and
[seed handling](docs/guides/seed-handling.md). Security-sensitive callers
should use `SecureRandomGenerator`, not `RandomGenerator`; see
[secure-token generation](docs/guides/secure-token-generation.md) and the
[security threat model](docs/security-threat-model.md).

The normative public behavior planned for the library is defined in the
[behavioral contract](docs/behavioral-contract.md). The implementation backlog
is tracked in [todo.md](todo.md).
