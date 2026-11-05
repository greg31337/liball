# Curated test fixtures

`manifest.json` is the authoritative inventory for reusable fixture data. Every fixture entry records category tags, media type and encoding, provenance, a pinned source revision, source URL, copyright, SPDX license identifier (or a scoped `LicenseRef`), the corresponding license file, the expected loading posture, and a SHA-256 checksum.

The corpus intentionally includes:

- an accepted SVG 1.1 Second Edition test copied verbatim from W3C;
- pinned SVG 2 and CSS cases copied verbatim from web-platform-tests;
- a pinned rendering regression copied verbatim from resvg;
- a base64-encoded PNG payload suitable for deterministic in-memory decoding;
- multiscript Unicode, bidi, and real redistributable Noto font data;
- declarative animation, accessibility, and hostile-input fixtures authored for this project.

External fixtures marked `verbatim` must not be edited in place. Update them from a new pinned upstream revision or add a separately named derived fixture with new provenance. WPT and W3C reference links may point outside this curated subset; they remain in the input to exercise foreign metadata and reference handling, not to provide a browser reftest harness.

Decode files whose manifest encoding is `base64` before supplying their bytes to an image decoder. Never resolve the external entity, network URLs, file URL, script, event handler, or cyclic reference in the security fixture. Its expected default result is a DTD-policy rejection.

Project-authored fixture data is made available under CC0-1.0. External license notices are retained under `licenses/`; the existing Noto fonts use the OFL-1.1 text in `fonts/LICENSE.txt`.
