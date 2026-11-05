# Research sources

Downloaded on 2026-08-27. These files are research inputs and test fixtures, not runtime dependencies.

## Normative and interoperability references

| Local file | Source | Purpose |
| --- | --- | --- |
| ECMA-404-2nd-edition.pdf | https://www.ecma-international.org/publications-and-standards/standards/ecma-404/ | Normative JSON grammar. ECMA-404 2nd edition is technically aligned with ISO/IEC 21778:2017. |
| RFC-8259.txt | https://www.rfc-editor.org/rfc/rfc8259.txt | Internet JSON grammar, UTF-8 transport rule, and interoperability guidance. |
| RFC-6901.txt | https://www.rfc-editor.org/rfc/rfc6901.txt | JSON Pointer syntax, escaping, URI fragments, and evaluation. |
| RFC-7464.txt | https://www.rfc-editor.org/rfc/rfc7464.txt | Record-separator framed JSON text sequences. |
| RFC-7493.txt | https://www.rfc-editor.org/rfc/rfc7493.txt | I-JSON profile: UTF-8, Unicode scalar strings, unique names, and interoperable numbers. |
| RFC-8785.txt | https://www.rfc-editor.org/rfc/rfc8785.txt | JSON Canonicalization Scheme, including UTF-16 property ordering and ECMAScript-compatible number output. |
| Unicode-17-Core-Specification.pdf | https://www.unicode.org/versions/Unicode17.0.0/UnicodeStandard-17.0.pdf | Unicode conformance, UTF-8, UTF-16, code points, scalar values, and surrogate handling. |
| Unicode-UAX-15-Normalization.html | https://www.unicode.org/reports/tr15/ | Normalization reference; JSON strings and JCS keys are intentionally not normalized. |

ISO/IEC 21778:2017 remains current after its 2023 confirmation. Its catalog record is at https://www.iso.org/standard/71616.html. The ISO text is not redistributed here; ECMA-404 is the freely available aligned grammar.

## Conformance corpus

JSONTestSuite is from https://github.com/nst/JSONTestSuite at commit 1ef36fa01286573e846ac449e8683f8833c5b26a. The local subset contains its license, README, all 318 parsing fixtures, and all 22 transform fixtures. It is retained under specs/JSONTestSuite for later test integration.

## Existing-library research

The library-research directory contains documentation snapshots from the projects' official documentation repositories or sites:

- Jackson core and databind: streaming parser/generator, tree model, constraints, diagnostics, and JSON Pointer paths.
- System.Text.Json: incremental UTF-8 reader, mutable and immutable DOM approaches, skip/capture behavior, and UTF-8 writer.
- Newtonsoft.Json: token reader/writer, LINQ-to-JSON DOM, formatting, and load behavior.
- RapidJSON: DOM, SAX, streams, raw numbers, Unicode validation/transcoding, and pretty writing.
- serde_json: generic Value model, exact/raw-value concepts, streaming deserialization, ordering, and depth controls.
- Gson: tree, binding, token streaming, skip/path behavior, strictness, depth, and formatting.
- simdjson: DOM, forward-only on-demand traversal, multiple-document streams, pointer navigation, and validation tradeoffs.

These files inform the feature inventory only. No third-party implementation code will be copied into src/.

## SHA-256

| File | SHA-256 |
| --- | --- |
| ECMA-404-2nd-edition.pdf | 760a6f77c38172f4de333412292eaed092f33852592ca0cce9b7535a6e415fcf |
| RFC-6901.txt | 12f9f4fc13d686aa4fbe7d144d70166cd3fcd4ab89174c2b13bda8f4b55e49da |
| RFC-7464.txt | a8eadbcdce5fc508b9e014299621562da041232af13848632092164d50b3f482 |
| RFC-7493.txt | a44ccf1451a7bf4f4bbb78b39e10ed0e9a27ff2ee5b639c6458042b5a7197062 |
| RFC-8259.txt | 61a5378f4255c720beb2a4b4a63b29540147c140f36988bf086291989b4cd2d7 |
| RFC-8785.txt | 63d52294eb0e3f0014174288186d388b4ddbf2c67d1ce8af1d9726eb0c3ab240 |
| Unicode-17-Core-Specification.pdf | d394812f8eba54745c7dd067ca7b4db196c8f1aee84a72d51b0d8f3c5afe082a |
| Unicode-UAX-15-Normalization.html | c0c05f91e1c4f9be3d987e27d76cf254b30003b6be41eb94977cc3fe148d4c4e |
