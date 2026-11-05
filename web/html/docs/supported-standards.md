# Supported standards and versions

UniversalHtml targets browser-observable server-side behavior from the standards below. The package is currently `0.1.0-alpha.1`; a target in this document describes the intended conformance baseline, not a claim that an unfinished subsystem already passes its complete conformance suite. Current implementation status is recorded in [`todo.md`](../todo.md).

## Version policy

Living standards have no immutable release number. For them, the repository records a review date and pins any generated local input by SHA-256. A behavior change after that date is adopted only through a reviewed source or fixture update. Dated recommendations and versioned specifications use the named version directly.

Normal builds are offline. Machine-readable provenance for generated tables lives in [`standards.json`](../tools/GenerateTables/inputs/standards.json) and [`manifest.json`](../tools/GenerateTables/inputs/manifest.json). Generated headers repeat the input revision and checksum.

## Conformance baselines

| Area | Baseline | Repository status |
|---|---|---|
| HTML parsing, serialization rules, and named character references | [WHATWG HTML Living Standard](https://html.spec.whatwg.org/), snapshot reviewed 2026-08-09; HTML name, doctype, and foreign-adjustment data reviewed 2026-08-10 | Normative behavior target; implementation is staged in `todo.md` |
| DOM tree semantics | [WHATWG DOM Living Standard](https://dom.spec.whatwg.org/), snapshot reviewed 2026-08-09 | Server-side subset only; no browser event or shadow-DOM model |
| Web encoding labels and decoders | [WHATWG Encoding Living Standard](https://encoding.spec.whatwg.org/), snapshot reviewed 2026-08-09 | Normative decoding target; implementation is staged |
| URL parsing and resolution used by policy checks | [WHATWG URL Living Standard](https://url.spec.whatwg.org/), snapshot reviewed 2026-08-09 | Constrained to exposed URL discovery, resolution, and safety behavior |
| Selector parsing and matching | [Selectors Level 4](https://www.w3.org/TR/selectors-4/), snapshot reviewed 2026-08-09 | Deterministic tree-observable subset; unsupported browser-state features are rejected explicitly |
| Inline CSS tokenization for sanitization | [CSS Syntax Level 3, Candidate Recommendation Draft, 24 December 2021](https://www.w3.org/TR/2021/CRD-css-syntax-3-20211224/) | Tokenization baseline for the sanitizer subset, not a general CSS engine |
| Markdown output | [CommonMark 0.31.2](https://spec.commonmark.org/0.31.2/) | Output and escaping contract; this project does not parse Markdown |
| Optional GitHub-flavored Markdown output | [GitHub Flavored Markdown](https://github.github.com/gfm/), snapshot reviewed 2026-08-09 | Tables, task items, strikethrough, autolinks, and tag filtering only where enabled |

## Conformance claims

A subsystem may claim conformance only when its relevant upstream fixtures are pinned, attributed, and run in the repository. Percentages must be calculated from committed test results; documentation must not estimate them. A passing focused unit suite demonstrates the implemented contract but does not by itself establish full standards conformance.

Exact current results and explicit skipped-case reasons are published in the
machine-readable [`conformance-report.json`](../conformance-report.json). Its
scope and offline verification procedure are documented in
[`conformance.md`](conformance.md).

When behavior differs intentionally from a browser, the public documentation must state the difference. Server-only exclusions listed in [`architecture.md`](architecture.md) are intentional rather than conformance defects.

## Updating a baseline

1. Identify the upstream change and the exact affected algorithms or data.
2. Update the local pinned input or attributed fixture and its revision label.
3. Recalculate the SHA-256 entry in the generator manifest.
4. Regenerate committed source with `dotnet run --project tools/GenerateTables -- --write` when generated data changed.
5. Add or update behavioral tests for changed production behavior.
6. Run the full test suite and `GenerateTables --check`.
7. Record externally observable behavior or security changes in the changelog once that file exists.

Silent drift to the latest text of a living standard is not allowed. Review dates are advanced only when the corresponding behavior has actually been evaluated.
