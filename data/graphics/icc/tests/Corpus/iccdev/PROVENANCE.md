# iccDEV test profiles

These binary ICC fixtures were copied without modification from the official
International Color Consortium `iccDEV` repository:

- Repository: https://github.com/InternationalColorConsortium/iccDEV
- Commit: `67e90cffe5dc9cc9094a7ea48d4bd1c1dc7016d0`
- Retrieved: 2026-09-13
- License: BSD 3-Clause; reproduced in `LICENSE-BSD-3-Clause.txt`

The `valid` directory contains upstream reference or regression inputs intended
to be structurally readable. The `malformed` directory contains upstream fuzz,
regression, bounds, and calculator-safety inputs. Renamed files retain their
original source path and SHA-256 digest in `manifest.csv`.

Hand-built coverage for profile versions, classes, colour spaces, rendering
intents, tag types, and processing elements lives in `ProfileCorpusTests.cs`.
Those fixtures are original test data for this project and do not inherit the
iccDEV license.
