# Specification Corpus

Retrieved on 2026-08-25 for the universal text-template library described in
`prompt.md`. The repository previously contained no local specification files,
so it was not sufficient for implementation or compatibility verification.

The corpus is intentionally implementation-oriented:

- `mustache/` contains the official Mustache specification and JSON/YAML test
  suites at commit `e8ec001db7f594521e773c34866aca2b5d6b0037` from
  <https://github.com/mustache/spec>.
- `handlebars/` contains the official Handlebars language specification source
  at commit `4bbc909a76d90fd4998900a04c67f64931a5894e` from
  <https://github.com/handlebars-lang/spec>.
- `liquid/` contains the core, parser-error, and partial suites plus implementer
  notes from Liquid Spec at commit
  `8a308cb199a7d4e635ae399797560875732c8839` from
  <https://github.com/Shopify/liquid-spec>.
- `jinja/`, `django/`, `scriban/`, `nunjucks/`, `go/`, and `razor/` contain the
  official language or template-designer references linked from `todo.md`.
- `formats/` contains RFC 8259 (JSON), RFC 3986 (URI), RFC 4180 (CSV), the XML
  1.0 Recommendation, the relevant HTML parsing material, and the ECMAScript
  lexical grammar used to verify output encoders.

The downloaded HTML files are self-contained reference snapshots only in the
sense that their text is local; styles, scripts, images, and cross-page links may
still point to the originating site. `SHA256SUMS` records the exact downloaded
contents, excluding this README and the checksum file itself.

Licensing and copyright remain with each upstream publisher. Upstream license
files are retained where their specification repository provides them.
