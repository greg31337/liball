# Bundled data refresh runbook

This runbook is the only supported path for refreshing bundled data. It applies
to IANA registries, Unicode/CLDR, ISO 4217, SWIFT IBAN, ITU E.164, GS1, and the
other authorities inventoried in `SOURCES.md`.

The transformation boundary is offline. Acquisition writes to a temporary
staging directory, never to `src/Data`. Only reviewed snapshots may be passed to
`tools/data/refresh.py`, and only reviewed generated files and manifest changes
are committed. The product assembly contains committed JSON resources and has
no downloader, update hook, registry client, or runtime network dependency.

## Non-negotiable gates

1. Start from a clean worktree and use a new temporary staging directory.
2. Discover releases only on the authority's official site. Pin the exact
   release, file date, serial, tag, or publication edition; do not record
   `latest` as a version.
3. Record the final HTTPS URL, retrieval date in UTC, byte length, and SHA-256
   before parsing anything. Keep upstream archives and detached signatures when
   the authority publishes them.
4. Confirm the admission state in `SOURCES.md`. **Hold** means stop: the payload
   may be inspected in temporary staging, but it must not replace a `specs/`
   snapshot, enter `src/Data`, or supply values to tests or generated code.
5. Confirm the license and required notice against `LICENSES.md`. A free
   download, public specification, or open-source consumer does not establish
   redistribution permission for the dataset.
6. Transform only the fields permitted by `SOURCES.md`. Do not transitively copy
   contact data, examples, prose, allocation lists, or comparative faker data.
7. Review candidates as data and as a legal/safety change. Generation never
   constitutes approval.
8. Update every required manifest field, run all checks, inspect the staged
   diff, and commit the generated artifacts. Do not fetch in builds, tests,
   package installation, application startup, or provider calls.

## Tooling

The refresh tool uses Python 3.9 or later and only the standard library. It
does not perform network access and refuses to render directly into `src/Data`.

From the repository root, verify the committed state:

```sh
python3 tools/data/refresh.py verify
python3 tools/data/refresh.py check-generated
```

`verify` requires every shipped JSON resource to have a manifest record and
checks all recorded resource and source-snapshot hashes. `check-generated`
rebuilds every currently automated resource from committed `specs/` snapshots
in a temporary directory and requires byte-for-byte equality.

The automated dataset names are:

| Tool name | Reviewed input | Candidate output |
| --- | --- | --- |
| `currencies` | `iso-4217-current-currencies.xml` | `standards/currencies.json` |
| `iban-formats` | `schwifty-2026.7.3-iban-registry.json` | `standards/iban-formats.json` |
| `language-tags` | `iana-language-subtag-registry.txt` | `standards/language-tags.json` |
| `time-zones` | `iana-time-zones-zone1970.tab` | `standards/time-zones.json` |
| `media-types` | `iana-media-types.xml` | `standards/media-types.json` |
| `ports` | `iana-service-names-port-numbers.csv` | `standards/ports.json` |
| `safe-ranges` | the three named RFC snapshots and the NANPA 555 snapshot | `standards/safe-ranges.json` |
| `tlds` | `iana-tlds-alpha-by-domain.txt` | `standards/tlds.json` |
| `invariant` | the same reviewed safe-range snapshots | `invariant/core.json` |

An automated transform must be added to the tool and reviewed before another
generated resource can be admitted. A hand-written one-off conversion is not a
repeatable refresh process.

## Authority and admission matrix

The state in `SOURCES.md` controls if it differs from this summary.

| Authority/data | Official release discovery and version evidence | Current action |
| --- | --- | --- |
| IANA Language Subtag Registry | `https://www.iana.org/assignments/language-subtag-registry/language-subtag-registry`; use `File-Date` | **Approved.** Render `language-tags`. |
| IANA media types | `https://www.iana.org/assignments/media-types/media-types.xml`; use the root `updated` value | **Approved.** Render `media-types`. |
| IANA service names and ports | `https://www.iana.org/assignments/service-names-port-numbers/service-names-port-numbers.csv`; use retrieval date when no release identifier is supplied | **Approved.** Render `ports`. |
| IANA tzdb `zone1970.tab` | `https://data.iana.org/time-zones/tzdb/zone1970.tab`; pin the tzdb release and retain the dated file annotation | **Approved.** Render `time-zones`; this is not a replacement for transition rules. |
| IANA root-zone TLD list | `https://data.iana.org/TLD/tlds-alpha-by-domain.txt`; use the root-zone serial | **Approved for the reviewed factual extract only.** Render `tlds`; retain only bare ASCII labels and the serial, do not claim protocol-registry CC0 coverage, and default domain generation to RFC 2606 example domains. |
| Unicode CLDR | `https://cldr.unicode.org/index/downloads`; choose a numbered stable release and exact LDML or CLDR-JSON files, plus `https://www.unicode.org/license.txt` | **Conditional approval.** Pin the release/file and Unicode-3.0 notice before implementing a field-level transform. Never use the development branch or a moving latest link as the recorded source. |
| SIX ISO 4217 lists | `https://www.six-group.com/en/products-services/financial-information/market-reference-data/data-standards.html`; record the List One publication/effective date | **Approved for the reviewed factual extract.** Render `currencies`; retain attribution and no standard prose or explanatory SIX material. |
| SWIFT ISO 13616 IBAN Registry | Discover releases at `https://www.swift.com/standards/standards-resources`; the admitted machine input is the exact MIT-licensed schwifty transcription tag and retained license, not an unlicensed SWIFT download | **Approved through the pinned schwifty 2026.7.3 transcription only.** Render `iban-formats`; retain factual format fields only, omit examples and overwrite additions, and never import customer or bank account lists. |
| ITU-T E.164 assignments | select the exact dated E.164 Operational Bulletin annex from `https://www.itu.int/`; record recommendation/annex edition and bulletin number | **Hold.** ITU copyright requires an approved machine-readable source or permission before creating `calling-codes.json`. |
| GS1 General Specifications | `https://ref.gs1.org/standards/genspecs/` and its versioned archive; record the immutable version | **Hold for tables.** Independently authored checksum algorithms may be implemented, but allocation tables, company prefixes, examples, and prose must not be copied. |
| IETF RFC constants and NANPA 555 | the exact RFC Editor documents and `https://www.nanpa.com/numbering/555-line-numbers`; record RFC editions and retrieval date | **Approved only for the cited short factual constants.** Render `safe-ranges` and `invariant`; inspect errata/supersession before changing constants. |
| Unicode country names | the same pinned stable CLDR release, selecting exact territory files | **Conditional approval.** Implement and review a narrow transform before creating `countries.json`; ISO publications are not input files. |
| eCFR VIN rules | `https://www.ecfr.gov/current/title-49/subtitle-B/chapter-V/part-565`; record the title/part effective date and verify the staged response is regulation content | **Approved for U.S. government rule facts only.** Reject access-denied, challenge, or error pages; do not copy narrative text or WMI/VIN registries. |
| GSMA TS.06 and TAC data | `https://www.gsma.com/get-involved/working-groups/gsma_resources/ts-06/`; record the exact TS.06 edition | **Hold for tables and TAC data.** No device table or allocation-derived test values may be produced. |
| Google libaddressinput | `https://github.com/google/libaddressinput`; pin a full commit and retain CC-BY-4.0 attribution | **Conditional approval.** Admit only selected address-format metadata after the exact commit, notice, and transform are reviewed. |
| Google libphonenumber | `https://github.com/google/libphonenumber`; pin a release tag and commit and retain Apache-2.0 LICENSE/NOTICE | **Hold for phone metadata** until the release, notices, transformations, and per-locale safe-output policy are approved. Never import live example numbers as safe values. |
| BIPM SI data | select an exact SI Brochure edition from `https://www.bipm.org/` | **Hold** until the exact edition and redistribution basis for the selected table are recorded. |
| ISBN publications | `https://www.isbn-international.org/`; record the exact manual/range-message edition | **Reference only** unless a new row and redistribution review are added to `SOURCES.md`. Do not derive a runtime range table from the manual by default. |
| Comparative libraries | local snapshots listed in `specs/README.md` | **Reference only.** Never use their data as refresh input, regardless of their code license. |

## Acquisition into temporary staging

Create a new staging area and record the UTC retrieval date:

```sh
refresh_root="$(mktemp -d "${TMPDIR:-/tmp}/fake-data-refresh.XXXXXX")"
snapshot_root="$refresh_root/specs"
candidate_root="$refresh_root/candidates"
retrieved_on="$(date -u +%F)"
mkdir -p "$snapshot_root" "$candidate_root"
```

Download one exact official asset at a time. For example:

```sh
curl --fail --location --proto '=https' \
  --dump-header "$refresh_root/language-tags.headers" \
  --output "$snapshot_root/iana-language-subtag-registry.txt" \
  'https://www.iana.org/assignments/language-subtag-registry/language-subtag-registry'

wc -c "$snapshot_root/iana-language-subtag-registry.txt"
shasum -a 256 "$snapshot_root/iana-language-subtag-registry.txt"
```

Repeat with the exact filenames expected by the tool. Do not use cookies,
credentials, browser profiles, or authenticated customer downloads in this
repository workflow. If an authority requires authentication or presents a
challenge/error document, stop and record the blocker; an HTTP 200 response is
not evidence that the payload is valid.

For archives, retain the downloaded archive in staging, verify its published
checksum/signature when available, reject absolute paths and parent traversal,
and extract only the exact selected files into `snapshot_root`. Record both the
archive hash and each selected-file hash in the review notes.

Before transformation, inspect format markers and the claimed version. Examples
include `File-Date` for language tags, the XML `updated` value for media types,
the tzdb annotation/release, CLDR's numbered release/tag, SWIFT's release number,
and the GS1 immutable version. Reject HTML, login pages, access challenges,
truncated downloads, unexpected encodings, missing headers, and unexplained
format changes.

## Render and compare

Render only an admitted dataset. The tool writes candidates under the supplied
staging directory and refuses to overwrite an existing candidate:

```sh
python3 tools/data/refresh.py render language-tags \
  --snapshot-directory "$snapshot_root" \
  --output-directory "$candidate_root" \
  --retrieved-on "$retrieved_on"
```

Use `render all` only when staging contains every required approved snapshot;
it also requires `--reviewer` with the responsible data reviewer's stable
identifier because that value is embedded in invariant provenance. Supplying an
identifier names responsibility but does not make the candidate approved.
The renderer selects and omits the fields documented in `manifest.json`; a
source format change that cannot be represented must fail or receive an
explicit code review, never a permissive fallback.

Inspect structural and semantic changes before copying anything:

```sh
git diff --no-index --stat src/Data "$candidate_root"
git diff --no-index src/Data/standards/language-tags.json \
  "$candidate_root/standards/language-tags.json"
```

For large tables, also compare record counts, keys, duplicates, assignment-state
changes, removed records, and a deterministic sample from the beginning, middle,
and end. A small diff is not automatically safe, and a large upstream diff must
not be accepted only because generation succeeded.

## Human review and admission

Two distinct decisions are required:

- A source/licensing reviewer confirms the exact authority, version, retrieval
  evidence, redistribution terms, notice, attribution, and admission state.
- A data reviewer confirms the transform, omissions, record counts, schema,
  locale/fallback coverage, and the complete risk decision required by
  `SOURCES.md`.

The review must specifically consider privacy, contactability, allocation and
credential claims, trademarks, copyright, cultural/political bias, regulated
advice, unsafe operational use, and whether examples could identify or reach a
real person or system. Locale review must distinguish native coverage from
fallback coverage and identify the human reviewer for each locale pack.

Only after both decisions are recorded may the selected snapshot replace its
file under `specs/` and the reviewed candidate replace its path under
`src/Data`. Copy explicit files; never recursively copy the staging directory.

Every changed resource record in `manifest.json` must include:

1. path, resource schema version, data version, and candidate SHA-256;
2. exact upstream URL, release/file date, UTC retrieval date, snapshot path, and
   snapshot SHA-256;
3. SPDX identifier or exact non-SPDX permission, notice, attribution, and the
   reviewer who confirmed it;
4. `tools/data/refresh.py` as `transformationScript` when automated, plus a
   complete account of retained, removed, normalized, sorted, synthesized, and
   merged fields;
5. native/fallback coverage and human review for locale resources; and
6. the full prohibited-use risk decision.

Do not merely replace hashes. Update `SOURCES.md` and `LICENSES.md` first when a
source, license, transformation, or admission state changes. Increasing
`dataVersion` is required for a behaviorally incompatible pack; keep old-version
support or document the release migration before removing it.

## Final verification and commit

After explicit reviewed files and manifest records have been copied:

```sh
python3 tools/data/refresh.py verify
python3 tools/data/refresh.py check-generated
git diff --check
dotnet test tests/fake.Tests.csproj
git status --short
```

Review `git diff` and the staged diff before committing. The commit must contain
the selected snapshots, generated artifacts, manifest metadata, required
notices, transformation-tool changes, and relevant behavioral/data tests as one
reviewable change. It must not contain the temporary staging directory,
credentials, cookies, response headers with secrets, access-denied pages, or
unreviewed held-source content.

CI and releases may run `verify`, `check-generated`, and tests. They must never
download registry data or rewrite committed resources. A scheduled refresh is a
reminder to run this reviewed process, not an automated merge or runtime update.

## Package release gate

The package version and locale `dataVersion` are separate release decisions.
Package code follows the package's normal semantic versioning. Locale resources
use the value selected by `FakeOptions.DataVersion`, and all fragments in a
locale pack must agree on that value. `schemaVersion` identifies the serialized
resource shape and is not a substitute for either version.

For each package release that changes bundled or source-code data:

1. Decide whether seeded output, fallback coverage, normalization, or field
   meaning changed. If it did, assign a new locale data version to every
   affected pack and manifest record. Never alter the meaning of a published
   data version in place.
2. Preserve the previous data edition when compatibility is promised. If an
   edition is removed, treat the loss of selectable data as a compatibility
   change and document the required migration.
3. Add a section to `RELEASE_NOTES.md` naming the package release (or
   `Unreleased` while preparing it), every supported locale data version, and
   the exact upstream release/file date, serial, publication edition, or
   retrieval date for every changed dataset. Use the committed manifest and
   locale provenance; do not substitute the date on which notes were written.
4. Record license or notice changes and link material migrations. A source
   version without its date evidence, or a data change without its review
   record, blocks release.
5. Run the final verification commands above from a clean checkout of the
   release candidate and compare `RELEASE_NOTES.md` with `manifest.json`, every
   changed locale's provenance, `SOURCES.md`, and `LICENSES.md`.

A data rollback restores the complete prior edition and its manifest/provenance
records; it does not reuse the newer edition number for different content.
