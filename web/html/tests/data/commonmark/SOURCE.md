# Pinned CommonMark generation subset

- Upstream: https://spec.commonmark.org/0.31.2/spec.json
- Version: `0.31.2`
- Retrieved: 2026-08-11
- Upstream SHA-256: `d431b29d97b6f73e69d547109cf5081578fac931e72afe95639ebe766c1b2a20`
- License: CC BY-SA 4.0; see `LICENSE.md`
- Vendored checksum: see `../fixture-manifest.json`

`generation.json` is a manually normalized subset of upstream examples whose
HTML output directly constrains HTML-to-Markdown generation. Upstream example
numbers, sections, HTML, and Markdown are retained. `expected_markdown` records
the library's deterministic equivalent spelling without a trailing newline.
The reproducible update procedure is in `../UPDATING.md`.
