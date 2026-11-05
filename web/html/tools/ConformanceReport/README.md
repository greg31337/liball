# Conformance report verifier

This dependency-free .NET tool verifies `conformance-report.json` against the
committed fixture manifest and fixture files. It does not execute the library;
the report's behavioral results come from the named fixture-driven tests.

```sh
dotnet test html.slnx --no-restore
dotnet run --project tools/ConformanceReport -- --verify
```

Verification fails when a required suite is omitted, a fixture revision or
case count drifts, pass/fail/skip totals do not balance, group totals do not
roll up, a local fixture checksum changes, or any skip lacks a reason and an
exact fixture-case identifier. The verifier independently discovers all
html5lib XML-coercion and script-execution cases eligible for skipping and
requires the report to account for exactly that set.
