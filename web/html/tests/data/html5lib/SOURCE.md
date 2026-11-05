# html5lib parser fixtures

- Upstream: `https://github.com/html5lib/html5lib-tests`
- Revision: `224991ec10db04f056a89eed8b0bd8695fd2950e`
- Imported directory: `tokenizer/`
- Imported on: 2026-08-10
- License: MIT; see `LICENSE`
- Vendored checksum: see `../fixture-manifest.json`

The `.test` files are vendored unchanged. `tokenizer/FORMAT.md` is the
upstream tokenizer format description renamed from `README.md`.

The current adapter compares the upstream token `output` arrays. Upstream
parse-error metadata remains vendored, but UniversalHtml uses its own stable
diagnostic taxonomy and does not claim html5lib parse-error-code conformance.
The `xmlViolationTests` group requires optional XML-infoset coercion and is
reported explicitly as skipped rather than omitted.

## Tree construction

- Upstream: `https://github.com/html5lib/html5lib-tests`
- Revision: `9329e64694e7835d0dcff9811e22856ef6ad16f9`
- Imported directory: `tree-construction/`
- Imported on: 2026-08-11
- License: MIT; see `LICENSE`
- Vendored checksum: see `../fixture-manifest.json`

This is the final html5lib tree-construction snapshot before revision
`224991ec10db04f056a89eed8b0bd8695fd2950e` moved maintenance of these
fixtures to web-platform-tests. The `.dat` files are vendored unchanged;
`tree-construction/FORMAT.md` is the upstream format description renamed
from `README.md`. Fixtures below `tree-construction/scripted/` require script
execution and are reported explicitly as skipped because UniversalHtml keeps
scripts inert by design.

The reproducible update procedure for both pins is in `../UPDATING.md`.
