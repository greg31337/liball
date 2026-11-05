# Test organization

The test project uses NUnit and mirrors the product's functional areas:

- `Core/` contains randomness, selection, state, and uniqueness tests.
- `Text/` contains Unicode, patterns, templates, and generated-text tests.
- `Standards/` contains checksum, identifier, and standards-vector tests.
- `Locale/` contains tag, fallback, coverage, and locale-data tests.
- `Providers/` contains focused provider behavior tests.
- `Composition/` contains profile, rule, schema, correlation, export, edge,
  and safety tests.

Tests cover observable behavior implemented under `src/`. Do not add source
syntax scanners, project-file tests, documentation tests, benchmarks, or timing
assertions.

## Fixed fixtures

`Fixtures/Standards/`, `Fixtures/Unicode/`, and `Fixtures/Locale/` contain only
pinned, deterministic test inputs. Fixture files are copied below `Fixtures/`
in the test output directory and can be located from
`TestContext.CurrentContext.TestDirectory`.

Each non-trivial fixture must identify its upstream source, version or retrieval
date, license, and any transformation in an adjacent Markdown file. Tests never
refresh fixtures from the network. Third-party validators and oracle packages
may be referenced only by `fake.Tests.csproj`; generated oracle output belongs
under `Fixtures/Standards/` and must record the package and version used.
