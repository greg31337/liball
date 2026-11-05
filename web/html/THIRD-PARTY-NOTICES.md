# Third-party notices

UniversalHtml includes the following third-party conformance data under
`tests/data`. These files are test inputs and do not add runtime dependencies.
The machine-readable pins and checksums are in
`tests/data/fixture-manifest.json`; update procedures are in
`tests/data/UPDATING.md`.

## CommonMark 0.31.2 generation examples

- Files: `tests/data/commonmark/generation.json`
- Source: https://spec.commonmark.org/0.31.2/spec.json
- Copyright: © 2014–2016 John MacFarlane
- License: Creative Commons Attribution-ShareAlike 4.0 International; see
  `tests/data/commonmark/LICENSE.md`
- Modifications: selected generation-relevant examples were normalized to
  JSON, while upstream example identifiers, sections, Markdown, and HTML were
  retained; deterministic expected output was added.

## GitHub Flavored Markdown 0.29 generation examples

- Files: `tests/data/gfm/generation.json`
- Source: https://github.com/github/cmark-gfm/blob/0.29.0.gfm.13/test/spec.txt
- Revision: `0.29.0.gfm.13`
- Copyright: the GFM specification incorporates the CommonMark specification,
  Copyright © 2014–2015 John MacFarlane
- License: Creative Commons Attribution-ShareAlike 4.0 International; see
  `tests/data/gfm/LICENSE.md`
- Modifications: selected generation-relevant examples were normalized to
  JSON; deterministic expected output was added.

## html5lib tokenizer tests

- Files: `tests/data/html5lib/tokenizer`
- Source: https://github.com/html5lib/html5lib-tests
- Revision: `224991ec10db04f056a89eed8b0bd8695fd2950e`
- Copyright: © 2006–2013 James Graham, Geoffrey Sneddon, and other contributors
- License: MIT; see `tests/data/html5lib/LICENSE`
- Modifications: fixture data is unchanged; the upstream `README.md` is named
  `FORMAT.md` locally.

## html5lib tree-construction tests

- Files: `tests/data/html5lib/tree-construction`
- Source: https://github.com/html5lib/html5lib-tests
- Revision: `9329e64694e7835d0dcff9811e22856ef6ad16f9`
- Copyright: © 2006–2013 James Graham, Geoffrey Sneddon, and other contributors
- License: MIT; see `tests/data/html5lib/LICENSE`
- Modifications: fixture data is unchanged; the upstream `README.md` is named
  `FORMAT.md` locally.

## WHATWG HTML named character references

- Files: `tests/data/entities/named-character-references.tsv`
- Source: https://html.spec.whatwg.org/entities.json
- Snapshot: 2026-08-10
- Copyright: © WHATWG (Apple, Google, Mozilla, Microsoft)
- License: Creative Commons Attribution 4.0 International; see
  `tests/data/entities/LICENSE.md`
- Modifications: the complete JSON object was sorted by name and normalized to
  a tab-separated name/Unicode-scalar representation.

## Web Platform Tests selector cases

- Files: `tests/data/selectors/wpt-selector-subset.tsv`
- Source: https://github.com/web-platform-tests/wpt
- Revision: `9bc6e2404bff5349e48d7962b0a495582bc5ade8`
- Copyright: © web-platform-tests contributors
- License: BSD 3-Clause; see `tests/data/selectors/LICENSE.md`
- Modifications: deterministic, tree-observable cases were manually normalized
  into tab-separated selector, fixture, and expected-ID records. Browser-state,
  layout, shadow-DOM, pseudo-element, and namespace-resolver cases are excluded.

