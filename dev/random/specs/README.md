# Downloaded specifications and references

Some local reference files have been replaced by links. See [Reference downloads](../REFERENCE-DOWNLOADS.md) for the current download-only files and their original paths. The inventory below also records historical acquisitions.

Retrieved on 2026-08-14 and supplemented on 2026-08-25. These files are reference inputs for implementation and tests; their presence does not grant this project ownership of them or imply standards conformance. Retain each document's original copyright and license notices. NIST test vectors can informally verify an implementation but do not replace CAVP validation.

| Local file | Upstream source | Why it is included |
| --- | --- | --- |
| `nist-sp800-90a-r1.pdf` | <https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-90Ar1.pdf> | Normative HMAC-DRBG instantiate, generate, reseed, update, limits, and security-strength requirements. |
| `nist-sp800-90c.pdf` | <https://nvlpubs.nist.gov/nistpubs/SpecialPublications/NIST.SP.800-90C.pdf> | Current NIST random-bit-generator construction guidance and the boundary between entropy sources and DRBGs. |
| `nist-drbg-test-vectors.zip` | <https://csrc.nist.gov/CSRC/media/Projects/Cryptographic-Algorithm-Validation-Program/documents/drbg/drbgtestvectors.zip> | Official CAVP DRBG known-answer vectors for prediction-resistance and reseed configurations. The outer archive contains three documented vector archives. |
| `rfc4086.txt` | <https://www.rfc-editor.org/rfc/rfc4086.txt> | Randomness and entropy requirements and common security failures. |
| `rfc2104.txt` | <https://www.rfc-editor.org/rfc/rfc2104.txt> | HMAC construction used by HMAC-DRBG through the built-in .NET cryptographic implementation. |
| `rfc9562.txt` | <https://www.rfc-editor.org/rfc/rfc9562.txt> | UUIDv4/v7 layout, version bits, variant bits, and generation guidance. |
| `rfc4648.txt` | <https://www.rfc-editor.org/rfc/rfc4648.txt> | Normative Base16, Base32, Base64, and Base64url alphabets, padding rules, and test vectors for token helpers. |
| `nist-fips180-4.pdf` | <https://csrc.nist.gov/files/pubs/fips/180-4/final/docs/fips180-4.pdf> | Normative SHA-256 definition underlying HMAC-SHA-256, state integrity digests, seed expansion, and fingerprints. |
| `pcg-family.pdf` | <https://www.cs.hmc.edu/tr/hmc-cs-2014-0905.pdf> | PCG family construction, streams, output permutations, advance, and statistical design. The implementation must use the separately documented PCG64-DXSM constants/output definition and lock them with vectors. |
| `pcg64dxsm-numpy-v2.4.6.h` | <https://raw.githubusercontent.com/numpy/numpy/v2.4.6/numpy/random/src/pcg64/pcg64.h> | Version-pinned NumPy/PCG reference definitions for the cheap multiplier, DXSM output, transition ordering, and advance operation. Retained as a reference, not compiled or copied into `src/`. |
| `random123-sc11.pdf` | <https://www.thesalmons.org/john/random123/papers/random123sc11.pdf> | Philox/Random123 counter-based construction and deterministic parallel partitioning. |
| `xoshiro-scrambled-linear.pdf` | <https://air.unimi.it/retrieve/dfa8b9a8-65cc-748b-e053-3a05fe0a3a96/xoroshiro.pdf> | xoshiro/xoroshiro transition and scrambler analysis. Official reference code/vectors must also be consulted when the engine task starts. |
| `xoshiro256starstar-reference.c` | <https://prng.di.unimi.it/xoshiro256starstar.c> | Authors' public-domain xoshiro256** 1.0 reference transition, jump, and long-jump constants. Retained as a reference, not compiled into `src/`. |
| `splitmix-oopsla14.pdf` | <https://gee.cs.oswego.edu/dl/papers/oopsla14.pdf> | SplitMix design and splittable-generator rationale. It also documents why this family is not for cryptographic use. |
| `mt19937.pdf` | <https://www.math.sci.hiroshima-u.ac.jp/m-mat/MT/ARTICLES/mt.pdf> | Original MT19937 algorithm and parameters for the compatibility engine. |
| `mt19937ar-reference.c` | <https://www.math.sci.hiroshima-u.ac.jp/m-mat/MT/MT2002/CODES/mt19937ar.c> | Authors' improved-initialization reference implementation and output driver. Its license notice is retained in the file; it is a reference and is not compiled into `src/`. |
| `lemire-ranged-integers-source.tar` | <https://export.arxiv.org/e-print/1805.10941> | Authoritative paper source and figures for unbiased multiply-high/rejection bounded-integer generation. The PDF endpoint returned an anti-bot placeholder, so the source archive is retained instead. Main text is `rangedrng.tex`. |
| `devroye-non-uniform-random-variate-generation.pdf` | <https://luc.devroye.org/LucDevroye-NonUniformRandomVariateGeneration-10.1007_978-1-4613-8643-8-1986.pdf> | Author-posted complete reference for inversion, rejection, alias/guide tables, continuous and discrete variates, geometry, Poisson processes, permutations, and random walks. The author's web edition explicitly permits copying. |
| `numpy-distributions-v2.4.6.c` | <https://raw.githubusercontent.com/numpy/numpy/v2.4.6/numpy/random/src/distributions/distributions.c> | Version-pinned NumPy distribution implementation used to identify exact parameter conventions, numerical branches, and compatible reference fixtures. It is retained as a reference and is not compiled or copied into `src/`. |
| `numpy-v2.4.6-LICENSE.txt` | <https://raw.githubusercontent.com/numpy/numpy/v2.4.6/LICENSE.txt> | License and bundled-code notices corresponding to both version-pinned NumPy source references in this directory. |
| `hormann-poisson-transformed-rejection.ps.gz` | <https://statmath.wu.ac.at/papers/92-04-13.wh.ps.gz> | Author-hosted preprint defining transformed rejection for large-mean Poisson variates. The upstream artifact is compressed PostScript. |
| `efraimidis-weighted-random-sampling.pdf` | <https://arxiv.org/pdf/1012.0256> | Weighted sampling without replacement over streams, including the exponential-key/reservoir algorithms and numerical considerations. |
| `mckay-latin-hypercube.pdf` | <https://www.asc.ohio-state.edu/statistics/comp_exp/jour.club/McKayConoverBeckman.pdf> | Original Latin-hypercube sampling construction and its stratification guarantees. |
| `bridson-poisson-disk.pdf` | <https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf> | Author-hosted fast Poisson-disk sampling construction in arbitrary dimensions. |
| `mezzadri-haar-random-matrices.pdf` | <https://arxiv.org/pdf/math-ph/0609050> | QR-based construction of Haar-distributed orthogonal and unitary matrices, including the sign correction required for the invariant measure. |
| `sobol-joe-kuo-notes.pdf` | <https://web.maths.unsw.edu.au/~fkuo/sobol/joe-kuo-notes.pdf> | Sobol primitive-polynomial/direction-number format and generation notes. |
| `sobol-new-joe-kuo-6.21201.txt` | <https://web.maths.unsw.edu.au/~fkuo/sobol/new-joe-kuo-6.21201> | Recommended Joe–Kuo D(6) Sobol direction-number parameters through dimension 21201. This is an implementation input, not a runtime file dependency. |

## SHA-256 checksums

```text
d9dcbff952053a7ae8b6dad1c9bfeaf35216aa5cb92a0fbccc4081f27921c980  lemire-ranged-integers-source.tar
7103bdc2230b2a09c13b3904c7ed32a1de8a57e97280efec2338d3829f1d823e  bridson-poisson-disk.pdf
aba2b989475851538cccc860a13c7995d6e4dddd8d0be6000cbfe952403c7d98  devroye-non-uniform-random-variate-generation.pdf
361bdecf21c8dd63ac75f672da70028f38e9c614b331296f7999a449f29f6e0a  efraimidis-weighted-random-sampling.pdf
5dc06222f83c17384bc2f8022b30e14c7d4cdc380ce7e1a12dd816d9f43e8dcb  hormann-poisson-transformed-rejection.ps.gz
cab402e8eba558e59c6f7ed2572cfd87f12b26f302826d746214cd3dad28193c  mckay-latin-hypercube.pdf
e2767b9a3ad8fa0ed82f643c7fc80b34f4ed8ef7756a6a74e7879719fa3ed050  mezzadri-haar-random-matrices.pdf
955a724f8325273c5569ff9cc76e539d8995a30ff4d45c9c47daa4baf0afe162  mt19937.pdf
9e446d09e048cadd63d0a39eaa2bdcbd4c8938dfb1848320d33d4c81df59bcf7  mt19937ar-reference.c
5f7e5658ebd5b4e6785a7b12fa32333511d2acc2f2d9c5ae1ffa16b699377769  nist-drbg-test-vectors.zip
5b88d70308c95106e713221c7085cbb8e89dc2eb6214d75c885af2f07ffc6b8a  nist-fips180-4.pdf
d58e3dac4aaa5971c9a6a20a9bd5c13e2f1930cd1a199c0c00f768e7327355e2  nist-sp800-90a-r1.pdf
22dc2de903b2fe602fa0729c38b9512927ad96ef5669a3da8a0ffb4d22c8e6d8  nist-sp800-90c.pdf
af37265f94fda047f5084d774929f6b53ad5b5506267145afcda5861f0f7de15  numpy-distributions-v2.4.6.c
1be1df33863f97a7bc1c4d67980bd6c69c9a6fef0a5ee76e6ad6cb91e56e8491  numpy-v2.4.6-LICENSE.txt
7e516ebe5de1a2c6d3ed16cc76e7b5cd5d501fbc7faeeb7bd7ff0cbc699ed7dd  pcg-family.pdf
2ce9cc9366a9993cfd786a1ee63eaad35d8761c0ff9744ee5508192156f33d4a  pcg64dxsm-numpy-v2.4.6.h
841f68114b052f436a818680c90d685ce796e492a448a1064e939adefbe596c6  random123-sc11.pdf
64d5245a9101929025336e470e3737f118704001249d503c85a86e19fe9fbb01  rfc2104.txt
d2c111e67e7628c3de41887c79ed87d0ba69ef1310eeb6698bdfb17fb9651589  rfc4086.txt
84e14418f795d503be5f34bf23ce4ebaa119e9ec7c9f667d8caeb111385b178f  rfc4648.txt
041870106b8701d9fa9261dcbc1679a6b7ab18cb179b37b69d7232431eac8042  rfc9562.txt
f4ebdf9c4370fea05519d4f026e9f8458678c8acce986d8fb2862d367eb9806d  sobol-joe-kuo-notes.pdf
68eedd2a4e3b659b9695e7aff0f8ac68718bcf620730fc3d3a8c65df2a067441  sobol-new-joe-kuo-6.21201.txt
b649fa7c2462ef14ac4647d1b50481400ea622fe58e23fae7a0651d36338bdc1  splitmix-oopsla14.pdf
69412730bae6a3479539b8862c8a7a17ba13e73ef456b3c38c742d4e323ad080  xoshiro-scrambled-linear.pdf
413bfcae1a0c9a0dc03f7de6c78106a67361dcce4dae15eef3195014d1dbe9ce  xoshiro256starstar-reference.c
```

## Usage notes

- Extract only the needed HMAC-SHA-256 vector subsets into `tests/TestData/`; do not make tests scan all 13 MB on every run.
- Generate `src/Quasi/SobolDirectionNumbers.cs` with a deterministic development script and test the source data checksum before generation. Do not read `specs/` at runtime.
- Treat papers as design descriptions, not automatically interoperable formats. Freeze every chosen variant, constant set, byte order, and seeding rule with a named algorithm version and known-answer fixtures.
- Re-check upstream errata and revisions before starting a security-sensitive implementation task.
