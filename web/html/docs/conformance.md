# Conformance reporting

The canonical machine-readable snapshot is
[`conformance-report.json`](../conformance-report.json). It reports exact
pass, fail, and skip counts for every committed tokenizer, tree-construction,
named-entity, selector, CommonMark/GFM generation, and sanitizer-corpus case.
It is a report about the explicitly described fixture scope, not a claim of
complete conformance to every upstream standard.

## Accounting model

A fixture item can produce more than one reported case. html5lib tokenizer
items run once per declared initial state. Tree-construction items with no
fixed scripting mode run with scripting both disabled and enabled. The report's
`cases` value is therefore the number of adapter runs, not merely the number of
records or source sections.

`passed`, `failed`, and `skipped` are mutually exclusive and must add to
`cases`. A `pass` suite has no failures or skips; `partial` means the report
contains at least one of either. Known conformance failures remain failures even
when the aggregate regression test locks their current count. A green NUnit run
means the published accounting still matches observed behavior, not that a
partial upstream suite has become fully conformant.

Every skip has a stable reason code, explanatory text, and exact fixture-case
IDs. The verifier independently discovers the only skip-eligible inputs:
html5lib's optional XML-infoset coercion cases and tree fixtures that require
JavaScript execution. It rejects missing, extra, duplicate, or anonymous
skips. Unsupported cases excluded while curating a documented subset are part
of that subset's stated scope and are not counted as executed skips.

## Verification and updates

Run the behavioral suites and then validate the published snapshot:

```sh
dotnet test html.slnx --no-restore
dotnet run --project tools/ConformanceReport -- --verify
```

The verifier checks the report schema, required suite set, fixture-manifest
revisions, current fixture case counts, the repository-authored sanitizer
corpus checksum, all count rollups, derived suite status, and complete skip
accounting. It performs no network access.

When behavior or fixtures change, inspect the relevant adapter results, update
the report counts and partitions, retain newly passing cases as passes, and
describe any newly proposed skip explicitly. A failing case must never be
reclassified as skipped merely to improve the pass count. Fixture pin and
license changes follow [`tests/data/UPDATING.md`](../tests/data/UPDATING.md).

