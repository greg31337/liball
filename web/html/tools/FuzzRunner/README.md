# Deterministic grammar fuzz runner

`FuzzRunner` generates bounded HTML, selectors, inline CSS declarations, and
URLs. It exercises the supported public parsing, selector, URL, and sanitizer
workflows, plus selected internal CSS and sanitizer decision stages through the
assembly's test-only friend access. Those internal calls increase fuzz coverage
but are not supported package APIs. The runner has no third-party dependencies
and performs no network access.

```sh
dotnet run --project tools/FuzzRunner -- --seed 0x123456789abcdef0 --cases 1000
dotnet run --project tools/FuzzRunner -- --target selector --seed 42 --cases 500
```

The custom SplitMix-based random stream is stable across .NET versions. Every
case seed is derived from the root seed, target, and case index, so a run is
reproducible without depending on execution timing or `System.Random`.

## Generated grammars

- HTML generation recursively combines document types, normal/void/raw-text
  elements, table/select/template and foreign-content tags, attributes,
  character references, comments, Unicode, and deliberate error-recovery
  boundaries such as missing or mismatched end tags.
- Selector generation builds lists, combinators, compounds, namespace/type,
  ID/class/attribute selectors, escapes, structural and logical pseudo-classes,
  `An+B`, relative selectors, and bounded malformed variants.
- CSS generation builds declaration lists, comments, property/value pairs,
  functions, dimensions, colors, escaped values, `url()`, `!important`, and
  malformed delimiter recovery.
- URL generation covers absolute and relative references, authority/user-info,
  IPv4/IPv6/IDN-looking hosts, ports, paths, query/fragment data, `data:` URLs,
  encoded/control-obfuscated schemes, and malformed components.

## Budgets

`--max-depth` and `--max-input-length` bound each generated value.
`--work-budget` bounds grammar productions, executions, and shrink attempts for
the run. `--time-budget-ms` bounds overall wall time, while
`--case-timeout-ms` supplies cancellation to cancellable APIs and rejects an
operation that returns after its deadline. Reaching the overall time or work
budget ends the run cleanly and reports how many requested cases executed.

Use `--help` for all options. The default run is intentionally a short,
deterministic smoke pass over all four targets.

## Checked invariants

Every target must finish without an unexpected exception or case deadline.
The executor also checks that all returned strings and result collections stay
within the configured bound and that repeating an operation produces the same
result. HTML cases receive additional structural checks:

- every child, parent, first/last-child, and previous/next-sibling link agrees;
  nodes are neither cyclic nor shared and all owner-document links are correct;
- repeated parsing and canonical serialization are deterministic;
- rich-text sanitizer output is complete, bounded, and idempotent;
- sanitized output reparses into a consistent HTML-only tree, contains no
  dangerous element or event attribute, retains only URLs accepted by the same
  compiled policy, and serializes back to the exact sanitized string.

An invariant violation is reported as `invariant:<stable-code>` and follows the
same shrinking and replay workflow as a crash or timeout.

## Failure shrinking and replay

An unexpected exception or per-case timeout is a fuzz failure. The runner uses
deterministic delta debugging to delete contiguous input ranges while retaining
the same failure kind. `--shrink-steps` bounds evaluations; `--no-shrink`
preserves the original input.

Failures are written by default below `artifacts/fuzz-failures` using a stable
target/case-seed/index filename. Each JSON file contains both inputs, root and
case seeds, target, failure signature, shrink count, relevant limits, and a
replay command:

```sh
dotnet run --project tools/FuzzRunner -- --replay artifacts/fuzz-failures/example.json
```

Exit code `0` means the generated run completed without a detected failure,
`1` means a failure was found or successfully replayed, and `2` means command
usage, artifact loading, or replay reproduction failed. Failure artifacts are
ignored by Git until a maintainer deliberately promotes a minimized reproducer
to a regression test.
