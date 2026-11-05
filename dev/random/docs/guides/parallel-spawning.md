# Parallel spawning

Do not let multiple workers race over one mutable generator. Give each logical
worker a stable child stream instead.

```csharp
using random;

const int workerCount = 8;
const int valuesPerWorker = 1_000;

RandomFactory factory = new RandomFactory();
RandomGenerator root = factory.CreatePcg64Dxsm(
    (UInt128)1234,
    (UInt128)0,
    RandomMathMode.Portable);
RandomGenerator[] workers = root.SpawnMany(workerCount);
double[][] output = new double[workerCount][];

Parallel.For(0, workerCount, workerIndex =>
{
    double[] values = new double[valuesPerWorker];
    workers[workerIndex].FillDoubles(values);
    output[workerIndex] = values;
});
```

`Spawn(index)` derives a child from the root identity and complete path. It does
not depend on the parent's current output position, the order in which siblings
are created, or worker scheduling. A child uses the same named algorithm and
math mode. PCG children receive derived native stream selectors; Philox children
receive derived high-counter partitions.

Assign indexes from stable logical identities. If worker 3 processes a
different partition after a retry, replay changes even though the child itself
is stable.

## Hierarchical work

Children can spawn descendants:

```csharp
RandomGenerator partition = root.Spawn(12UL);
RandomGenerator batch = partition.Spawn(5UL);
```

The full path `[12, 5]` is part of the descendant identity. `SeedSequence`
provides the same indexed-path model when applications need derived seed bytes
before selecting an engine:

```csharp
RandomFactory factory = new RandomFactory();
SeedSequence seedRoot = factory.CreateSeedFromUInt64(1234UL);
SeedSequence batchSeed = seedRoot.SpawnPath([12UL, 5UL]);
byte[] material = batchSeed.GenerateBytes(32);
```

## Spawn, clone, jump, or seek?

- Use `Spawn` for stable application-level workers and nested tasks.
- Use `Clone` only when two computations must begin at the exact same current
  position; clones otherwise emit overlapping output.
- Use xoshiro `Jump`/`LongJump` for its documented fixed substream distances.
- Use PCG or SplitMix `Advance` when an exact transition distance is known.
- Use Philox `Seek` when the application owns an explicit counter-block mapping.

Query `Info().HasCapability(...)` before invoking engine-specific positioning.
All of these objects remain mutable and should have one owner at a time.
