# Distributions

The library separates random engines from reusable samplers. Construct a
sampler once when drawing repeatedly from one parameter set:

```csharp
using random;

RandomFactory factory = new RandomFactory();
RandomGenerator generator = factory.CreateDefaultGenerator(
    42UL,
    RandomMathMode.Portable);
NormalSampler normal = new NormalSampler(generator, 10.0, 2.0);

double one = normal.Sample();
double[] batch = new double[1_000];
normal.Fill(batch);
```

`Fill` matches repeated scalar `Sample` calls in the same order. Constructing a
sampler explicitly keeps validation, parameter conventions, generator ownership,
and state consumption visible at the API boundary.

These APIs accept `IRandomSource`, so applications may provide another uniform
source without inheriting from `RandomGenerator`. The interface deliberately
contains only five primitive methods; bounded integers and floating endpoint
policies remain library transforms. See [custom random sources](custom-random-sources.md).

## Parameter conventions

Read the sampler's XML documentation before porting parameters from another
library. Important conventions include:

- `NormalSampler` takes mean and standard deviation.
- `ExponentialSampler` takes scale, not rate.
- `GammaSampler` uses shape and scale.
- `InverseGammaSampler` uses shape and scale.
- `NormalGammaSampler` uses a gamma shape-rate convention for precision.
- `GeometricSampler` exposes failures and trials through distinct methods.
- `ParetoSampler` is Pareto type I with a positive minimum; `LomaxSampler` is
  Pareto type II with location and scale.
- covariance and scale matrices are flat row-major arrays; methods named
  `FromCholesky` accept precomputed lower-triangular factors.

Probability parameters use `[0, 1]` unless a distribution documents a stricter
domain. Relative categorical, multinomial, and mixture weights need not sum to
one, but must be finite, nonnegative, nonempty, and not all zero.

## Endpoints and degenerate cases

The default uniform floating interval is `[0, 1)`. Explicit closed-open,
open-open, open-closed, and closed-closed methods are available on
`RandomGenerator`. Samplers choose endpoint variants that protect logarithms
and inverse transforms from singularities.

Documented degenerate parameters—such as a zero normal standard deviation or
zero exponential scale—return their point mass without consuming random state.
Invalid arguments are validated before random state changes. Some unbounded
distributions can still return infinity after representational overflow; check
the sampler's return contract when extreme parameters are possible.

## Reproducibility

Portable math mode is the bit-for-bit distribution replay mode. Store the named
engine version, transform version, seed or state, parameters, call sequence,
and math mode. System mode is statistically compatible but does not promise
last-bit identity or a portable state after sampling.

## Multivariate storage

Vector and matrix batches are sample-major. Each vector is contiguous; each
matrix is contiguous and row-major. Ask the API for exact storage lengths:

```csharp
DirichletSampler dirichlet = new DirichletSampler(
    generator,
    [0.5, 1.5, 3.0]);
double[] output = new double[dirichlet.ComponentCount()];
dirichlet.SampleInto(output);
```

The library generates variates; analytical PDF, CDF, fitting, and inference
APIs are outside its scope.
