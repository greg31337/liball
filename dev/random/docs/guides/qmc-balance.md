# QMC balance

Quasi-Monte Carlo sequences are deterministic space-filling designs, not IID
random samples. Their benefit depends on retaining consecutive points and the
balance structure appropriate to the sequence.

## Balanced Sobol blocks

`SobolSequence` uses 64-bit Joe-Kuo D(6) direction numbers in Gray-code order
and supports dimensions 1 through 21,201. Point zero is the all-zero point.

When digital-net balance matters, request an aligned power-of-two block:

```csharp
using random;

RandomFactory factory = new RandomFactory();
SobolSequence sobol = factory.CreateUnscrambledSobol(3);
double[] points = sobol.FillPowerOfTwo(10); // 1,024 point-major points
```

The current index must be divisible by the requested block size. Consecutive
blocks of the same size remain aligned. Arbitrary `Fill` counts, `Skip`,
`FastForward`, thinning, or selecting only convenient points remain
deterministic but do not carry the same digital-net balance guarantee.

## Reproducible scrambling

Create a `DigitalScrambler` once from a deterministic generator, then attach it
to the Sobol sequence:

```csharp
RandomFactory factory = new RandomFactory();
RandomGenerator scrambleGenerator = factory.CreateDefaultGenerator(
    9001UL,
    RandomMathMode.Portable);
DigitalScrambler scrambler =
    factory.CreateLinearMatrixWithDigitalShift(scrambleGenerator, 3);
SobolSequence scrambled = new SobolSequence(3, scrambler);

double[] points = scrambled.FillPowerOfTwo(10);
```

The scrambler is fully precomputed. Sequence operations do not retain or
consume the generator afterward. Persist the scramble seed/path and construction
choice when reproducing randomized QMC estimates.

## Other designs

- `HaltonSequence` uses consecutive prime bases. Use `RandomFactory.CreateUnscrambledHalton` for
  the standard sequence or its constructor to supply one zero-preserving digit
  permutation per coordinate.
- `HammersleySet` is a finite set: its first coordinate is `i / N`, followed by
  radical-inverse coordinates. It cannot emit beyond its configured point count.
- `LatinHypercubeSampler` places exactly one point in each one-dimensional
  stratum; centered mode uses stratum midpoints, while the default jitters them.
- `StratifiedSampler` supports equal or per-dimension stratum counts.
- `PoissonDiskSampler` enforces a minimum separation subject to its explicit
  attempt budget; it is a randomized blue-noise design, not a low-discrepancy sequence.

All flat point arrays are point-major: coordinate `d` of point `p` is stored at
`points[p * dimension + d]`.
