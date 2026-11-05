# Reference material

Retrieved on 2026-08-27 for offline implementation and conformance work.

## Primary specifications

- `rfcs/rfc3986.txt` — URI generic syntax, reference resolution, normalization, and comparison.
- `rfcs/rfc3987.txt` — IRI syntax, URI/IRI conversion, normalization, comparison, and bidi guidance.
- `rfcs/rfc6570.txt` — URI Template syntax and Level 1–4 expansion.
- `rfcs/rfc8141.txt` — URN syntax, optional components, and equivalence.
- `rfcs/rfc8288.txt` — Web Link model, Link header serialization, and parsing algorithms.
- `errata/` — RFC Editor errata pages for each primary specification.

The RFC text and errata were retrieved from `https://www.rfc-editor.org/`.

## Supporting specifications

- `rfcs/rfc3492.txt` — Punycode.
- `rfcs/rfc3629.txt` — UTF-8.
- `rfcs/rfc5234.txt` and `rfcs/rfc7405.txt` — ABNF and case-sensitive string notation.
- `rfcs/rfc5646.txt` — language tags used by Web Link `hreflang`.
- `rfcs/rfc5890.txt` through `rfcs/rfc5893.txt` — IDNA2008 definitions, protocol, code-point rules, and bidi rules.
- `rfcs/rfc5952.txt` — canonical IPv6 text representation.
- `rfcs/rfc7230.txt` — HTTP grammar referenced normatively by RFC 8288.
- `rfcs/rfc7320.txt` and `rfcs/rfc8820.txt` — updates to RFC 3986 concerning URI design and ownership.
- `rfcs/rfc8187.txt` — extended HTTP parameter values used by Web Links.
- `rfcs/rfc9110.txt` — current HTTP field semantics and combined field-value rules.
- `rfcs/rfc9844.txt` — current status of IPv6 zone identifiers; it obsoletes RFC 6874 and restores the unmodified RFC 3986 URI grammar.

## Registries

- `registries/iana-link-relations.xml`
- `registries/iana-uri-schemes.xml`
- `registries/iana-urn-namespaces.xml`

These snapshots were retrieved from `https://www.iana.org/assignments/`. They are research and test inputs, not generated public constants; the registries change over time.

## Comparison libraries

- `libraries/uriparser-overview.html` and `libraries/uriparser-api.html`
- `libraries/python-rfc3986-overview.html` and `libraries/python-rfc3986-uri-api.html`
- `libraries/python-uritemplate-api.html`
- `libraries/uri-js-api.html`
- `libraries/ada-readme.md`

These local snapshots support feature-surface research. They are not source dependencies and no code from them is included in `src/`.

## Conformance data

`test-data/uri-template/` is the reusable `uri-templates/uritemplate-test` corpus from `https://github.com/uri-templates/uritemplate-test`, including its license. It contains both RFC examples and extended positive and negative cases.

`unicode/` contains the Unicode Standard Annex #15 normalization reference, Unicode Technical Standard #46, the Unicode 17.0 IDNA mapping and test data, the Unicode 17.0 normalization test data, and the Unicode data license. These are reference/test inputs; the production library may use only built-in .NET facilities and code or tables maintained inside this repository.
