# Updating vendored fixture subsets

`fixture-manifest.json` is the exhaustive allowlist of third-party data below
`tests/data`. Each entry pins an upstream version or commit, points to its
provenance and license files, and records a deterministic aggregate checksum.
The `pipeline`, `sanitization`, and `serialization` directories are original
UniversalHtml fixtures and therefore are not listed.

Normal builds and tests use only committed files and never access the network.
Network access is needed only when a maintainer deliberately updates a pin.

## Checksum procedure

From the repository root, verify all committed subsets offline:

```sh
dotnet run --project tools/FixtureImporter -- --verify
```

The verifier recursively sorts each subset's relative paths using ordinal
comparison. For every file except `SOURCE.md` and files beginning with
`LICENSE`, it hashes the UTF-8 relative path plus LF, the unmodified file bytes,
and a final LF. It prints the file count and lowercase SHA-256 digest.

After an intentional update, inspect the new files and run:

```sh
dotnet run --project tools/FixtureImporter -- --print
```

Copy the reviewed count and digest into `fixture-manifest.json`, then run
`--verify`. Never change a checksum merely to silence a mismatch: first review
the upstream diff, provenance, license, and normalization output.

## html5lib tokenizer and tree construction

1. Review the chosen commit in `html5lib/html5lib-tests` and its license.
2. For the tokenizer pin, copy the commit's `tokenizer/*.test` files unchanged
   and rename its tokenizer `README.md` to `FORMAT.md`.
3. For the tree-construction pin, recursively copy every `.dat` file unchanged
   and rename its tree-construction `README.md` to `FORMAT.md`.
4. Update the corresponding revision and import date in `html5lib/SOURCE.md`
   and `fixture-manifest.json`. Retain `html5lib/LICENSE`.
5. Run the checksum procedure above and the html5lib adapter tests.

The two subsets may intentionally use different commits; each has its own
manifest entry.

## WHATWG named character references

1. Download `https://html.spec.whatwg.org/entities.json` and record the raw
   file's SHA-256 and retrieval date in `entities/SOURCE.md`.
2. Parse the JSON object, sort property names ordinally, and write one UTF-8 LF
   terminated row per property as `name<TAB>first<TAB>second-or--1`. Preserve
   both semicolon and legacy no-semicolon property names.
3. Prepend the two metadata comment lines found in the current TSV and compare
   the resulting name/scalar pairs with the generated entity table.
4. Confirm the upstream license notice, then update the manifest and verify.

## CommonMark and GitHub Flavored Markdown

1. Download the exact source named in the subset's `SOURCE.md`; verify and
   record its raw SHA-256.
2. Retain only examples that directly constrain HTML-to-Markdown generation.
   Preserve upstream example numbers, section names, input Markdown, and HTML.
3. Review `expected_markdown` as UniversalHtml's deterministic equivalent
   output. This field is a documented local adaptation, not upstream text.
4. Update the version/revision, retrieval date, provenance, license if needed,
   and manifest digest. Run the CommonMark/GFM golden tests.

## Web Platform Tests selectors

1. Review and pin a full commit from `web-platform-tests/wpt`.
2. Record every consulted upstream path and its raw SHA-256 in
   `selectors/SOURCE.md`.
3. Normalize only deterministic, tree-observable selector cases supported by
   the library into `wpt-selector-subset.tsv`; preserve selectors and expected
   IDs. Keep the exclusion rationale in `SOURCE.md` current.
4. Retain the BSD 3-Clause license, update the manifest, verify the checksum,
   and run the selector conformance tests.

For every subset update, also update `THIRD-PARTY-NOTICES.md` when the source,
copyright, license, version, or nature of local modifications changes.

