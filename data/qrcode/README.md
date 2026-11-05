# qrcode

Initial scaffold created on 2026-08-27.

Run the behavioral test suite with `dotnet test tests/qrcode.Tests.csproj`.
Both the library and test project enable checked arithmetic and treat compiler
warnings as errors, including during coverage runs. Randomized fixtures use
fixed seeds so failing inputs can be reproduced.

To collect coverage and enforce the public API coverage requirement, run:

```sh
./scripts/test-coverage.sh
```

This requires the .NET 10 SDK and Bash. The command runs the full suite in a
fresh results directory, collects whole-library line and branch coverage, and
requires 100% of instrumented lines and branches in every public concrete
method and source-bearing constructor exposed by `src/Api`. The gate enumerates
the compiled API, so a missing method report fails the check. Abstract interface
declarations and implicit constructors without executable source have no
coverage points; the latter are counted separately in the summary.

Results are written under `artifacts/coverage/run.*`: TRX test results, Coverlet
JSON, Cobertura and OpenCover reports, and `api-coverage-summary.json` with
per-method totals and any uncovered lines or branches. Internal implementation
coverage is included in the full reports. Hit counts record whether each location
executed, rather than its execution frequency. Behavioral assertions remain necessary
for correctness; execution coverage alone does not prove correct results.

The Coverage GitHub Actions workflow runs this command on pushes and pull
requests, fails on test failures or incomplete public API coverage, and uploads
the reports even when the check fails.
