# Universal Random Library Development Plan

Status: public API completion audit. The target framework is .NET 10 and the implementation project is `src/random.csproj`.

## 1. Product definition

Build a self-contained C# library for:

- reproducible pseudo-random bit streams and numeric values;
- cryptographically secure random bytes, bounded integers, tokens, and identifiers;
- scalar and bulk sampling from a broad set of continuous, discrete, multivariate, empirical, and process distributions;
- unbiased collection sampling, permutations, weighted choices, geometric sampling, and low-discrepancy sequences;
- deterministic parallel work through explicit child streams, stream selectors, jumps, and counter partitioning;
- stable, versioned state interchange for non-cryptographic deterministic engines.

“Deterministic” has three explicit levels:

1. Named deterministic engines, raw words, integer conversions, byte filling, state snapshots, jumps, and spawn paths are bit-for-bit stable across supported .NET 10 platforms.
2. Distribution sampling in `Portable` math mode is bit-for-bit stable for the same algorithm version, seed/state, and call sequence. `System` math mode is statistically equivalent but not promised to match the last bit across runtime or architecture changes.
3. The convenience `CreateDefault` algorithm may change only at a documented major-version boundary. Applications that need replay must select a named engine and persist its algorithm/version with the state.

Security and simulation are separate API surfaces. `RandomGenerator` is deterministic and never claims cryptographic security. `SecureRandomGenerator` uses the operating-system CSPRNG or HMAC-DRBG and does not expose state, cloning, jumping, or weak integer seeds. Passing statistical tests is not treated as evidence of unpredictability.

## 2. Hard constraints from `syntax.md`

- Source uses only built-in .NET APIs; `src/random.csproj` receives no package references.
- Source types are classes, enums, and narrowly scoped interfaces. Interfaces contain instance methods only and classes implement them directly; there are no structs, records, delegates, properties, indexers, operators, generic declarations, class inheritance hierarchies, or iterator methods.
- Public state and metadata objects use documented fields plus ordinary methods instead of properties.
- Public collection APIs use `Array`, `System.Collections.IList`, `object`, and explicitly typed array/`Span<T>` overloads because project-defined generic methods are forbidden.
- Bulk APIs use `Span<byte>`, `Span<int>`, `Span<long>`, `Span<float>`, `Span<double>`, and corresponding unsigned spans where appropriate. APIs never require `ref`, `out`, `in`, `params`, ranges, or unsafe code.
- All public members receive the XML documentation permitted by `syntax.md`.
- Implementation code avoids LINQ, `yield`, nullable annotations, attributes, exception handling syntax, and external native/runtime assets.
- Joe–Kuo Sobol data is converted at build-development time into a generated C# constants class; the shipped library has no data-file or network dependency.
- Tests may use external packages, but this plan does not add syntax-policy/source-scanning tests or performance/benchmark tests.

## 3. Explicit non-goals

- No FIPS 140 validation claim, entropy-source implementation, hardware-RNG health testing, or claim that a custom DRBG wrapper is a validated cryptographic module.
- No global mutable RNG singleton and no implicit time-only seeding.
- No transparent shared-instance thread safety for deterministic generators; use one spawned/partitioned generator per worker.
- No custom user-supplied deterministic engine plug-in point. Named algorithms and their state formats remain curated for replay compatibility; extensibility is provided at the higher `IRandomSource` sampling boundary instead.
- No exact compatibility with every legacy library seeding convention. Compatibility engines and seed modes are named explicitly.
- No PDF/CDF/quantile/statistical-fitting library. This project generates variates; only mathematical helpers required by correct sampling are included.
- No infinite lazy sequences, because iterator methods are excluded. Bulk generation fills caller-provided spans or arrays.
- No benchmarks or tests that enforce the syntax policy.

## 4. Research summary and design consequences

| Library or platform | Features reviewed | Adopted consequence |
| --- | --- | --- |
| NumPy Random | Separate `BitGenerator` and `Generator`, `SeedSequence`, spawning, raw words, bulk output, permutations, choice, many distributions, Philox/PCG64DXSM/MT19937 | Separate engine internals from transforms; support named engines, seed expansion, spawn trees, bulk fills, and a NumPy-sized baseline distribution catalog. Unlike NumPy's general `Generator`, named streams here have a stated compatibility contract. |
| Apache Commons RNG 1.7 | Uniform provider, restorable state, fixed/arbitrary jumps, split providers, factories, alias sampling, collection sampling, geometric shapes | Add explicit capabilities, copy/jump/advance/spawn operations, reusable precomputed samplers, and shape sampling. Use an internal common engine interface plus separate optional-capability interfaces so adding an engine does not expand a central dispatcher. |
| Math.NET Numerics 5 | Multiple deterministic/system/crypto sources, distribution classes, scalar and array fill, discrete/continuous/multivariate catalog | Provide C#-natural scalar and span APIs and cover its useful extra distributions, while keeping analytical PDF/CDF APIs out of scope. |
| Python `random` and `secrets` | Sequence selection/shuffle/sample, state replay, MT compatibility, strict split between simulation and secure generation | Include familiar high-level collection operations and visibly separate secure APIs. |
| .NET `System.Random` and `RandomNumberGenerator` | Bounded primitives, byte/span fill, shuffle/get-items, OS CSPRNG | Offer unsurprising .NET names and span overloads, but add named algorithms, portable state, unbiased mappings, distributions, and parallel stream control. |
| Java `RandomGenerator` | Factory metadata, splittable, jumpable, leapable, arbitrarily jumpable generators | Model capabilities as enum/metadata fields and methods; fail before mutation when an operation is unsupported. |
| Rust `rand` / `rand_distr` | Core RNG versus distributions, fixed seed material, OS RNG, named reproducible algorithms, weighted and sequence sampling | Keep raw generation small, require adequate byte seeds for secure construction, and put richer sampling in separate classes. |
| Boost.Random and C++ `<random>` | Engine/distribution separation, seed sequences, pseudo/non-deterministic/quasi concepts, distribution parameter objects | Use reusable concrete sampler objects where setup is material, and treat quasi-random engines as a separate family rather than pretending they are PRNGs. |
| R RNG facilities | Explicit RNG/normal/sample algorithm selection and version selection for reproducibility | Persist algorithm and transform versions; never let a change in a normal or bounded-integer algorithm silently alter a named compatibility stream. |
| SciPy QMC | Sobol, Halton, Latin hypercube, scrambling, power-of-two balance warnings | Include a distinct quasi-random layer, Joe–Kuo direction numbers, scrambling, skip/fast-forward, and API guardrails around balance properties. |

Primary online documentation consulted:

- NumPy Random overview and generator: <https://numpy.org/doc/stable/reference/random/> and <https://numpy.org/doc/stable/reference/random/generator.html>
- NumPy parallel generation: <https://numpy.org/doc/stable/reference/random/parallel.html>
- Apache Commons RNG guide and sampling API: <https://commons.apache.org/proper/commons-rng/userguide/rng.html> and <https://commons.apache.org/proper/commons-rng/commons-rng-sampling/apidocs/>
- Math.NET random and distribution namespaces: <https://numerics.mathdotnet.com/api/MathNet.Numerics.Random/index.htm> and <https://numerics.mathdotnet.com/api/MathNet.Numerics.Distributions/index.htm>
- Python `random`: <https://docs.python.org/3/library/random.html>
- Java random-generator protocols: <https://docs.oracle.com/en/java/javase/26/docs/api/java.base/java/util/random/RandomGenerator.html>
- Rust Rand Book: <https://rust-random.github.io/book/>
- Boost.Random reference: <https://www.boost.org/doc/libs/release/doc/html/boost_random.html>
- R random-number generation: <https://stat.ethz.ch/R-manual/R-devel/library/base/help/Random.html>
- SciPy quasi-Monte Carlo: <https://docs.scipy.org/doc/scipy/reference/stats.qmc.html>
- .NET interface and abstraction design: <https://learn.microsoft.com/dotnet/standard/design-guidelines/interface> and <https://learn.microsoft.com/dotnet/standard/design-guidelines/abstractions-abstract-types-and-interfaces>
- .NET unit-testing guidance for controllable interface-backed dependencies: <https://learn.microsoft.com/dotnet/core/testing/unit-testing-best-practices>

Downloaded implementation specifications, papers, data, test vectors, provenance, and checksums are cataloged in `specs/README.md`.

## 5. Architecture and invariants

### 5.1 Layers

1. **Core arithmetic and codecs:** rotations, multiply-high, deterministic floating helpers, endian-independent state encoding, validation, and zeroization.
2. **Entropy and seeding:** OS entropy, canonical seed encoding, deterministic seed expansion, spawn paths, stream IDs, and domain separation.
3. **Engines:** a small curated set of named algorithms behind `IRandomEngine`, with separate interfaces for advance, jump, and seek capabilities.
4. **Uniform transforms:** unbiased integer intervals, arbitrary-size integers, floating interval variants, booleans, bytes, and bulk fills.
5. **Distributions:** reusable samplers and one-shot facades driven by the five-method `IRandomSource`; lifecycle and state remain on `RandomGenerator`.
6. **High-level sampling:** collections, weighted tables, geometry, QMC, processes, and security-specific token helpers.

### 5.2 Engine selection

- `Pcg64DxsmV1`: default deterministic general-purpose engine; 128-bit state/stream, seekable, spawnable, not secure.
- `Xoshiro256StarStarV1`: compact high-throughput engine with fixed jump and long-jump operations, not secure.
- `Philox4x64TenV1`: counter-based engine for deterministic random access and parallel partitions, not secure.
- `Mt19937V1`: compatibility engine with standard reference initialization and a separately named NumPy-compatible seed mode, not the default.
- `SplitMix64V1`: seed expansion/reference compatibility and lightweight streams; exposed but marked unsuitable for security and very large simulations.
- `SystemCryptoV1`: direct `System.Security.Cryptography.RandomNumberGenerator` source, secure and non-replayable.
- `HmacDrbgSha256V1`: SP 800-90A HMAC-DRBG using built-in `HMACSHA256`, with instantiate/reseed/generate limits, optional additional input, and automatic OS reseeding. It is not described as FIPS validated.

### 5.3 State and compatibility

- Deterministic snapshots use a canonical little-endian binary envelope: magic, format version, algorithm ID, algorithm version, transform version, payload length, payload, and SHA-256 integrity digest.
- State import rejects unknown versions, truncation, trailing bytes, digest mismatch, invalid all-zero states, and algorithm/payload mismatches before mutating a generator.
- Exported state is not secret and provides integrity, not authentication. Secure generator state is never exportable.
- `Clone` reproduces the exact next output. `Jump`, `LongJump`, `Advance`, `Seek`, and `Spawn` have separate documented semantics and never silently substitute for one another.
- Seed material is encoded with type and length tags. Byte seeds are never interpreted with host endianness. Text seeds use UTF-8 plus domain separation.
- Spawned streams derive from root seed material plus the complete unsigned spawn path. Creation order is not part of the child identity when explicit child indexes are used.

### 5.4 Correctness and safety

- Bounded integers use rejection/multiply-high algorithms and never `% bound` alone unless divisibility proves it unbiased.
- Floating APIs state endpoints explicitly: `[0,1)`, `(0,1)`, `(0,1]`, and `[0,1]`; finite ranged methods document overflow handling.
- Every distribution validates its mathematical domain before consuming random bits. Invalid input leaves generator state unchanged.
- Scalar `Sample()` and `Fill(Span<T>)` consume the same sequence as repeated scalar calls unless a method explicitly documents a block/counter mapping.
- Algorithms avoid hidden thread-local/global state. Mutable samplers and generators document that concurrent calls require external synchronization.
- Rejection loops include analysis for termination and numeric boundary handling, not arbitrary retry caps that bias output.
- Secure APIs use fixed-time framework primitives where available, zero temporary secret buffers with `CryptographicOperations.ZeroMemory`, enforce request/reseed limits, and never fall back to deterministic entropy.
- UUIDs set variant/version bits after random fill per RFC 9562. Password/token helpers use unbiased alphabet selection and reject insufficient/duplicate alphabets where policy requires uniqueness.

## 6. Complete target feature catalog

### 6.1 Metadata, lifecycle, and errors

- Algorithm IDs, semantic algorithm versions, transform versions, math modes, security flag, state size, word size, period description, and capability flags.
- Factory creation from OS entropy, `ulong`, byte array, text, `SeedSequence`, explicit state, stream selector, and algorithm-specific parameters where safe.
- Clone, snapshot export/import, deterministic reset, fixed jump, long jump, arbitrary advance, absolute counter seek, indexed spawn, and bulk child creation where supported.
- Dedicated argument, state-format, unsupported-capability, exhausted-period, reseed-required, and security-policy exceptions.
- No hidden default/global instance; ordinary mutable instances and documented per-worker ownership.

### 6.2 Seed and parallel-stream management

- Canonical seed material from `ulong`, `long`, UTF-8 string, bytes, `uint[]`, and OS entropy.
- SHA-256-based deterministic expansion to any engine state size with library/algorithm/version domain separation.
- Spawn-key trees, explicit child indexes, stable child derivation, root-seed fingerprinting, collision guidance, and child metadata.
- PCG stream selection and logarithmic advance; xoshiro jump/long-jump; Philox key/counter partition, absolute seek, and block reservation.
- Fork/process guidance: create fresh secure generators after a fork-like boundary and derive deterministic worker streams before scheduling.

### 6.3 Raw and uniform values

- Raw `uint`/`ulong` words; signed full-range `int`/`long`; booleans from independent bits; requested bit counts; byte arrays and byte spans.
- Unbiased inclusive/exclusive bounded overloads for byte, signed/unsigned 32-bit and 64-bit integers, plus `BigInteger` ranges.
- `float` and `double` unit intervals with all four endpoint policies, finite ranged values, and standard open-unit values safe for logarithmic transforms.
- `decimal` on a documented 96-bit uniform grid and bounded decimal intervals.
- Scalar methods plus typed span filling; deterministic word-to-byte ordering and a small bit reservoir for booleans/short requests.
- Random sign and Rademacher values; probability-weighted booleans with exact handling of zero and one.

### 6.4 Continuous univariate distributions

- Uniform, normal/Gaussian, log-normal, exponential, gamma, Erlang, beta, scaled beta, chi, chi-square, Student's t, and Fisher–Snedecor F.
- Cauchy, Laplace, logistic, Gumbel, generalized normal/error, skewed generalized error, and skewed generalized t.
- Weibull, Rayleigh, Pareto type I, Lomax/Pareto type II, power, Burr XII, Lévy, inverse gamma, and inverse Gaussian/Wald.
- Triangular, von Mises, symmetric/asymmetric stable, truncated normal, noncentral chi-square, and noncentral F.
- Standard forms and location/scale forms; scalar sampling and span filling; reusable samplers for algorithms with precomputed tables/constants.

### 6.5 Discrete distributions

- Bernoulli, Rademacher, discrete uniform, categorical, and arbitrary finite enumerated probabilities.
- Binomial, beta-binomial, geometric in both “failures before success” and “trial count” conventions, negative binomial, and Poisson.
- Hypergeometric, multivariate hypergeometric, multinomial, logarithmic-series, Zipf, and Conway–Maxwell–Poisson.
- Small/large parameter algorithm selection that preserves exact domains and handles degenerate cases without consuming unnecessary entropy.
- Alias-table and guide-table preprocessing for repeated categorical draws.

### 6.6 Multivariate and matrix distributions

- Dirichlet, multivariate normal from covariance or Cholesky factor, and multinomial/multivariate hypergeometric vector fills.
- Wishart, inverse Wishart, matrix normal, and normal-gamma distributions.
- Built-in array-based matrix validation, Cholesky decomposition, triangular solves, symmetry/positive-definite checks, and deterministic loop order.
- Destination-array APIs with explicit row-major shape parameters; no dependency on a matrix package.

### 6.7 Composition and empirical sampling

- Empirical distribution from equally weighted observations or weights.
- Mixture sampling from weighted `Func<double>` components.
- Inverse-transform sampling from a caller-supplied inverse CDF delegate.
- Bounded rejection sampling from caller-supplied proposal/value/acceptance delegates with explicit iteration budget diagnostics; no silent biased fallback.
- Truncation wrappers for supported built-in samplers and deterministic affine transforms.

### 6.8 Collections, permutations, and resampling

- Uniform choice with replacement, in-place Fisher–Yates shuffle, shuffled copy, random permutation of `[0,n)`, and partial shuffle.
- Sampling without replacement using partial shuffle, sparse index mapping, or reservoir algorithms according to input/access pattern.
- Streaming reservoir sampling through `IEnumerable`/`IList` consumption without project iterator methods.
- Weighted choice with replacement via cumulative/guide/alias tables; weighted sampling without replacement using exponential keys.
- Random subset/combination, derangement, stratified resampling, systematic resampling, residual resampling, and bootstrap indexes/samples.
- `Array`/`IList` general APIs plus strongly typed overloads for primitive/string arrays where return typing materially improves usability.

### 6.9 Geometry and directional sampling

- Uniform point on/in unit circle, disk, sphere, and n-dimensional ball.
- Uniform point on a line segment and inside axis-aligned box, simplex, triangle, and tetrahedron.
- Uniform unit vector, random rotation/quaternion in 3D, and Haar orthogonal matrix using deterministic array math.
- Polygon sampling after validated area triangulation.

### 6.10 Quasi-random and space-filling designs

- Van der Corput, Halton, Hammersley, and Sobol sequences with skip, reset, current index, fast-forward, and span/row-major block output.
- Sobol dimensions 1–21201 using the downloaded Joe–Kuo D(6) direction data; base-2 block method protects balance properties.
- Optional digital shift and linear-matrix/Owen-style scrambling driven by an explicit deterministic generator.
- Latin hypercube (plain and centered), orthogonalized stratification where supported, regular stratified grids, and Poisson-disk space filling.
- Discrepancy calculation helpers used for validation and user diagnostics, not for performance measurement.

### 6.11 Stochastic processes and high-level generators

- Integer and floating random walks, Brownian increments/path, Brownian bridge, geometric Brownian motion, and Ornstein–Uhlenbeck paths.
- Homogeneous Poisson arrival process and finite-state Markov-chain paths from a validated transition matrix.
- Deterministic path/batch filling with explicit time-step and initial state; no lazy enumerable API.

### 6.12 Security-specific helpers

- Direct OS-secure bytes and bounded integers; HMAC-DRBG instantiate, reseed, generate, additional input, personalization, prediction-resistance request, request counter, and uninstantiate/zeroization.
- Base16, base32, base64, and base64url tokens; unbiased custom-alphabet strings; password generation from caller policy without reducing entropy through modulo bias.
- RFC 9562 UUIDv4 and UUIDv7, including version/variant masks and monotonic-within-millisecond counter policy for UUIDv7.
- Security documentation covering entropy size, seed secrecy, state compromise, reseeding, process cloning, variable-time high-level transforms, and the absence of FIPS validation.

## 7. Verification strategy

- Known-answer vectors are the primary test for engines, seed expansion, state encoding, HMAC-DRBG, Sobol points, and UUID bit layout.
- Round-trip/state tests cover clone equivalence, import atomicity, corruption rejection, advance/jump identities, counter boundaries, and serialization endianness.
- Exhaustive small-domain tests prove bounded integer and shuffle reachability/count behavior without relying on probabilistic pass/fail thresholds.
- Distribution tests use fixed seeds, support/boundary assertions, deterministic scalar-versus-fill equivalence, and conservative moment/quantile/goodness-of-fit checks sized to avoid flaky tails.
- Cross-library tests under `tests/` may use Math.NET Numerics and Python/NumPy fixture files as independent references. They do not add dependencies to `src/`.
- Security tests consume NIST CAVP vectors, test reseed/request limits, verify zeroization-visible state transitions where test hooks allow it, and confirm secure state export/weak-seed APIs do not exist.
- CI covers .NET 10 on Windows, Linux, and macOS, with x64 and arm64 where available, to enforce portable-mode replay.
- No timing assertions, microbenchmarks, benchmark project, syntax-policy tests, or source-code scanning tasks are part of this plan.

## 8. Public API completion backlog

This backlog contains only gaps found by comparing the current public types under `src/Api` with the target feature catalog above. Existing API-shape notes, planned file lists, completed implementation history, documentation work, release work, performance work, and syntax/source-policy checks are intentionally excluded. Production code must use only built-in .NET libraries; tests may use external libraries as independent references.

- [DONE] T001: Complete algorithm metadata by exposing the transform version and a stable period or exhaustion description for every deterministic and secure algorithm through the public metadata contract and implementation.
- [DONE] T002: Add public factory paths that create each applicable named deterministic generator from a `SeedSequence`, canonical byte seed, text seed, unsigned-word seed array, and operating-system entropy, with algorithm/version domain separation and no weak fallback.
- [DONE] T003: Expose Philox counter-block reservation through `IRandomGenerator` and `RandomGenerator`, returning the reserved range identity and advancing atomically while rejecting unsupported algorithms and counter exhaustion before mutation.
- [DONE] T004: Complete uniform primitive coverage with unbiased bounded and ranged byte generation on deterministic and secure generators and a deterministic bulk requested-bit fill whose consumption order is defined against repeated scalar calls.
- [DONE] T005: Complete public multivariate and matrix bulk sampling with required-length queries and row-major `Span<int>` or `Span<double>` batch fills for Dirichlet, multinomial, multivariate hypergeometric, multivariate normal, Wishart, inverse Wishart, matrix normal, and normal-gamma sampling.
- [DONE] T006: Add interface-backed public collection sampling under `src/Api` for `Array` and `IList`: choice with replacement, destination fills, shuffle, partial shuffle, shuffled copy, permutation, subset, combination, derangement, sample without replacement, and streaming reservoir sampling, with uniquely named typed-array methods where useful.
- [DONE] T007: Add interface-backed public weighted and resampling operations under `src/Api` for arbitrary enumerated values, weighted sampling without replacement, bootstrap indexes and samples, and stratified, systematic, and residual resampling, reusing the existing alias, guide-table, and exponential-key implementations.
- [DONE] T008: Add interface-backed public geometry operations under `src/Api` for lines, boxes, circles, disks, spheres, n-balls, unit vectors, simplexes, triangles, tetrahedra, 3D rotations and quaternions, polygon sampling, and batch destination fills while retaining the existing orthogonal-matrix sampler.
- [DONE] T009: Expose centered, L2-star, and bounded exact star discrepancy calculations through an interface-backed public API under `src/Api`, accepting row-major arrays and `ReadOnlySpan<double>` inputs with explicit point and dimension counts.
- [DONE] T010: Add interface-backed public stochastic-process operations under `src/Api` for integer and floating random walks, Brownian increments and paths, Brownian bridges, geometric Brownian motion, Ornstein-Uhlenbeck paths, homogeneous Poisson arrivals and count paths, and single/batch Markov-chain paths with required-output-length queries.
- [DONE] T011: Expose the HMAC-DRBG reseed/request counter and applicable request-limit status through the secure public contract without exposing secret state, and define unsupported behavior for the direct operating-system source.
- [DONE] T012: Complete the continuous-distribution surface with explicitly named standard and location/scale construction paths for every applicable sampler that currently exposes only one planned form, without method overloading or ambiguous parameter conventions.
- [DONE] T013: Add complete behavior tests for public metadata, factory, seed, lifecycle, state, spawn, jump, advance, seek, Philox reservation, raw/uniform conversion, byte bounds, bit fills, endpoint policies, scalar-versus-fill consumption, and pre-consumption validation.
- [DONE] T014: Add complete behavior tests for both secure-generator modes, including every public integer and token method, RFC 4648 vectors, password policies, RFC 9562 UUID bit layouts and UUIDv7 monotonic behavior, DRBG counters and limits, prediction resistance, reseeding, uninstantiation, zeroization-visible behavior, and absence of deterministic lifecycle operations.
- [DONE] T015: Add complete behavior tests for every public continuous and discrete sampler, covering all construction forms, parameter validation before entropy consumption, support and degenerate cases, scalar-versus-fill replay, numeric boundaries, algorithm-regime boundaries, and conservative fixed-seed distribution checks.
- [DONE] T016: Add complete behavior tests for every public multivariate, matrix, empirical, mixture, inverse-transform, rejection, truncation, and affine sampler, including shape/aliasing validation, row-major layouts, batch equivalence, simplex and matrix invariants, delegate failures, diagnostics, and exhaustion paths.
- [DONE] T017: Add complete behavior tests for the public collection and weighted/resampling layer, including exhaustive small-domain reachability, typed and untyped parity, source traversal rules, uniqueness and count invariants, zero-weight behavior, destination validation, and deterministic scalar/bulk replay.
- [DONE] T018: Add complete behavior tests for all public geometry, quasi-random, scrambling, space-filling, and discrepancy operations, including geometric invariants, degenerate-input atomicity, reference points, skip/reset/index exhaustion, Sobol power-of-two guardrails, scramble replay, stratification, minimum-distance guarantees, and discrepancy bounds.
- [DONE] T019: Add complete behavior tests for all public stochastic-process operations and `MarkovChain`, covering output lengths and layouts, scalar/batch replay, endpoint conditions, fixed-seed increment and transition invariants, invalid-input atomicity, Poisson horizon handling, and process limit/exhaustion behavior.
- [DONE] T020: Close remaining behavior-coverage gaps in existing public factories, public constants, exception constructors, metadata/data defensive copies, interfaces, and concrete implementations, using reference fixtures from `specs/` or test-only external libraries where they provide an independent oracle.
