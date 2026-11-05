# Reference sources

Retrieved on 2026-08-27. These files are research and generator inputs; they are not compiled into `src/` directly. Update automation must pin a source release or revision, verify checksums, and preserve the applicable license.

## Frozen dataset policy (version 1)

This section freezes the intake policy for the first generated dataset as of 2026-08-30. A source update may change revisions and checksums without changing this policy, but adding a source, adding a locale, expanding a source's role, or changing precedence requires a reviewed policy-version change. The generator must retain source and upstream-key provenance for every accepted field and must fail on an unexplained conflict instead of silently selecting whichever input was processed last.

### First-snapshot scope

| Source | Included role | Explicitly excluded from the first snapshot |
| --- | --- | --- |
| Debian `iso-codes` ISO 3166-1 | Current alpha-2, alpha-3, numeric-3, short name, and available official name | The upstream flag field; flag emoji is derived from an accepted alpha-2 code at runtime |
| Debian `iso-codes` ISO 3166-2 | Current subdivision code, name, type, and available parent | Translations and data from other standards in the upstream repository |
| Debian `iso-codes` ISO 3166-3 | Former alpha-2, alpha-3, numeric-3, alpha-4, name, and available withdrawal date | Inferred successor links or change kinds not present in the pinned input |
| UN M49 | Numeric country/area identity cross-checks; world, region, subregion, and intermediate-region hierarchy; country membership; and names actually present in the pinned tables | Statistical indicators, economic classifications, maps, and any subject outside country/area identity and geographic hierarchy |
| CLDR | English territory/subdivision display names, territory and subdivision aliases/deprecations, and subdivision containment from the three pinned CLDR files | Additional locales, time zones, currencies, telephone data, flags, and all other CLDR data |
| IANA Language Subtag Registry | Records needed to validate and canonicalize BCP 47 language tags, including deprecated and preferred values | Registration-form prose, unrelated IANA registries, and copies of RFC prose |
| Reviewed overrides | Minimal aliases, transition/replacement links, source-delta explanations, and temporary corrections that satisfy the override rules below | New countries or subdivisions, invented identifiers, geopolitical opinions, and changes to an authoritative upstream identifier |

The first snapshot therefore covers ISO 3166 country, subdivision, and historical identifiers; M49 geographic hierarchy; English CLDR name enrichment; and locale-tag mechanics. More CLDR locales and broader translated-name coverage require a later policy version. Currency, calling-code, capital, time-zone, border, coordinate, population, and image data remain out of scope.

### Field-level source precedence

Precedence is specific to a field's authority; it is not one global source order. A lower-precedence value may fill an absent optional field or be retained as an explicitly typed alias, but it may not replace a conflicting higher-precedence value.

| Field or decision | Precedence and reconciliation rule |
| --- | --- |
| Current ISO country membership and alpha-2/alpha-3 tuple | Debian `iso-codes`; the reviewed ISO material may confirm or reject a release during human review but is never a generator input. Overrides cannot change the tuple. |
| Numeric-3 for a current country/area | Debian `iso-codes` and UN M49 must agree after preserving three-digit zero padding. Any disagreement is a hard reconciliation failure; neither source wins silently. |
| Current ISO country canonical and official names | Debian `iso-codes`. UN and CLDR names remain separately typed, localized, and attributed; an override may add an alias but may not rewrite the canonical ISO-derived name. |
| ISO subdivision code, country prefix, canonical name, and type | Debian `iso-codes`. CLDR may add an English display name or alias. Overrides cannot change the identifier or country prefix. |
| ISO subdivision parent | Debian `iso-codes` when present; CLDR containment validates or fills an absent relationship. A conflict fails unless a reviewed, expiring override explains a known upstream defect without changing an identifier. |
| Former country identifiers, canonical name, and withdrawal date | Debian `iso-codes`. CLDR aliases may add replacement evidence; reviewed overrides may add missing transition kind or links but cannot rewrite an identifier. |
| M49 hierarchy, levels, region names, and country membership | UN M49. ISO-derived numeric values are cross-checks only, and aggregate M49 regions are never promoted to ISO countries. |
| Locale-tag validity, deprecation, and preferred subtag | IANA Language Subtag Registry. CLDR locale aliases may be used only as additional application-level aliases and cannot contradict IANA. |
| English localized territory/subdivision display name and CLDR deprecation mapping | CLDR. Debian names remain canonical ISO-derived names rather than being overwritten. |
| Curated common alias, unexplained source-delta disposition, or missing transition metadata | A reviewed override may supplement upstream data only when no authoritative value is displaced. |

All accepted alternatives retain their distinct name kind, locale, and provenance. An absent value is not a conflict. Duplicate identifiers, contradictory authoritative values, unknown parents, and undocumented source deletions are errors even when the table above names a nominal precedence.

### Reviewed override rules

Every override must identify its target field, source/upstream key, reason, evidence, reviewer, and an expiry condition tied to an upstream revision or policy decision. Overrides are applied after authoritative inputs only so they can supplement or explain them; they must never mutate alpha-2, alpha-3, numeric-3, alpha-4, or subdivision identities. Conflicting overrides and expired overrides are hard failures. Removing an override requires the same review as adding one so that provenance is not lost.

### License and redistribution gate

This is an engineering intake decision, not a replacement for legal review. "Package eligible" below means that the identified source terms permit the planned use provided their notice, attribution, source-availability, and other conditions are satisfied. The source manifest and distributed package notices must carry the exact applicable identifiers and notices.

| Material | Recorded terms | Repository input | Generated package output |
| --- | --- | --- | --- |
| Debian `iso-codes` JSON | `LGPL-2.1-or-later`, as declared for `data/*json` in the pinned upstream [`REUSE.toml`](https://salsa.debian.org/iso-codes-team/iso-codes/-/blob/abb091ac802dc34ff0375bdf153c4e633a2f44d3/REUSE.toml) | Allowed with the upstream copyright and license material retained | Conditional: retain attribution and the LGPL notice, make the covered preferred source/input available as required, and do not apply incompatible restrictions to covered derived data |
| CLDR XML data | `Unicode-3.0`, stated in the pinned files, [`CLDR-LICENSE.txt`](unicode/CLDR-LICENSE.txt), and the [Unicode licensing policy](https://www.unicode.org/policies/licensing_policy.html) | Allowed with the license file retained | Allowed when the Unicode copyright and permission notice accompanies the distributed data/derivative |
| IANA Language Subtag Registry | `CC0-1.0`; [IANA and IETF's 2021 joint statement](https://www.iana.org/help/licensing-terms) applies CC0 to protocol registry data | Allowed; record the registry URL and file date | Allowed for the selected registry facts; retain source attribution for traceability even though CC0 does not require it |
| UN M49 web tables | [United Nations website terms](https://www.un.org/en/about-us/terms-of-use) allow personal, non-commercial copying but do not grant general redistribution or derivative-work rights | Review and generator-validation input only | Not eligible from these snapshots. Do not emit copied UN names, prose, HTML, or table arrangement until express redistribution permission or a compatible source-specific license is recorded. Independently sourced factual identifiers still require normal provenance and review. |
| Normative ISO standards, OBP content, newsletters, or licensed exports | [ISO copyright terms](https://www.iso.org/copyright.html) and the licence attached to the acquired copy | Local human review only; never commit, upload, cache in CI, or place in a generator fixture | Never copy or derive publication text/tables into source or package without express permission. ISO's [free-of-charge use of country codes](https://www.iso.org/iso-3166-country-codes.html) does not authorize copying an ISO publication. |
| RFC 4647/5646 text | IETF Trust Legal Provisions applicable to each RFC | Review/reference input only | Implement the specification; do not reproduce RFC prose except under its applicable notice and licence |
| Reviewed overrides | Original repository contributions; contributors must have the right to submit the facts and text | Allowed after review | Eligible under the repository/package license once that license is declared; third-party text may not be pasted into an override |

The distribution gate is fail-closed: generator inputs and generated rows must declare provenance, and anything with missing, conflicting, or package-incompatible terms is excluded from package output. In particular, UN-derived package fields remain disabled under this version of the policy until the manifest records suitable permission or a replacement source with compatible terms. License notices are package artifacts, not generated runtime API data.

### Normative ISO review boundary

A reviewer may compare proposed rows with a lawfully obtained ISO publication or licensed ISO export and record only a pass/fail result, a source citation, and an original explanation of a discrepancy. The licensed file stays outside the repository tree and outside build, test, generator, CI, cache, release, and package artifacts. It must not be converted into fixtures, screenshots, extracts, or generated tables. If review finds a discrepancy, update or reject the redistributable upstream input; do not transcribe the ISO material into an override.

## ISO 3166

The normative ISO publications are not copied here. ISO makes code use free of charge, but the standards, previews, and public newsletter archive are copyright-controlled and the archive grants read-only access. Obtain any required licensed copy separately.

- ISO 3166 overview and maintenance information: https://www.iso.org/iso-3166-country-codes.html
- ISO 3166 glossary, including assigned, reserved, user-assigned, former, and subdivision code definitions: https://www.iso.org/glossary-for-iso-3166.html
- ISO 3166-1:2020 metadata and abstract: https://www.iso.org/standard/72482.html
- ISO 3166-2:2020 metadata and abstract: https://www.iso.org/standard/72483.html
- ISO 3166-3:2020 metadata and abstract: https://www.iso.org/standard/72484.html
- ISO Online Browsing Platform: https://www.iso.org/obp/ui/#home

## Debian iso-codes

Pinned to commit `abb091ac802dc34ff0375bdf153c4e633a2f44d3` dated 2026-08-26. Source: https://salsa.debian.org/iso-codes-team/iso-codes

- `iso-codes/iso_3166-1.json` — current ISO 3166-1 records.
- `iso-codes/iso_3166-2.json` — current ISO 3166-2 subdivisions, types, and parents.
- `iso-codes/iso_3166-3.json` — withdrawn ISO 3166-1 records and alpha-4 codes.
- `iso-codes/README.md`, `iso-codes/LGPL-2.1-or-later.txt`, and `iso-codes/FSFAP.txt` — upstream description and licenses.

## United Nations M49

Current online methodology snapshots from the United Nations Statistics Division:

- `un/un-m49-methodology.html` — country/area codes, history, methodology, and multilingual names.
- `un/un-m49-overview.html` — world, region, subregion, and intermediate-region hierarchy with ISO alpha mappings.

Sources: https://unstats.un.org/unsd/methodology/m49/ and https://unstats.un.org/unsd/methodology/m49/overview/

## Unicode and CLDR

- `unicode/uax15-normalization-17.0.html` — Unicode Standard Annex #15 revision 57, Unicode normalization forms.
- `unicode/uts35-ldml.html` — Unicode Technical Standard #35 version 48.2, locale identifiers and CLDR data semantics.
- `unicode/cldr-48.2-en.xml` — English territory and subdivision display names.
- `unicode/cldr-48.2-supplementalMetadata.xml` — territory and subdivision aliases/deprecations.
- `unicode/cldr-48.2-subdivisions.xml` — territory/subdivision containment data.
- `unicode/CLDR-LICENSE.txt` — Unicode License v3 covering the pinned CLDR files.

CLDR data is pinned to the `release-48-2` tag: https://github.com/unicode-org/cldr/tree/release-48-2

## IETF and IANA

- `ietf/rfc5646.txt` — BCP 47 language-tag syntax and canonical form.
- `ietf/rfc4647.txt` — BCP 47 language-range matching.
- `ietf/language-subtag-registry.txt` — IANA Language Subtag Registry snapshot, file date 2026-08-08.

Sources: https://www.rfc-editor.org/rfc/rfc5646, https://www.rfc-editor.org/rfc/rfc4647, and https://www.iana.org/assignments/language-subtag-registry/

## SHA-256 checksums

```text
be21e91b6851f750a7b1a687f11209d46ad5a8471d6b10a1efc8d1dac4c8a926  ietf/language-subtag-registry.txt
999e4e23ca5b8925ac5f154855af0f2948a5d26d14853249c9fc857e469ecb49  ietf/rfc4647.txt
5d9515f053163c80e7294a84d18217dc83471acc7caa9773da2ac40deeec8228  ietf/rfc5646.txt
99cb91dacd5714a701749ec47239015726beec5fc86a5c94a4873778824b16d7  iso-codes/FSFAP.txt
5749785c8bdefafcb5d798270ed0a967036fe2ca63dcedade1627565dfef81d2  iso-codes/LGPL-2.1-or-later.txt
aba3f5d3473a382c11f71b3ae567624cae6e45362ecc72192210e20e462b2e51  iso-codes/README.md
f01b812b57fba9f31ff621bf33e7c7570a01964dbeb5be2167e94decf538c89f  iso-codes/iso_3166-1.json
78c90ef7fc25b5c2631aac5f089bc9ff6ec22c025c05b6ddbc087a1f1be2e46a  iso-codes/iso_3166-2.json
eb92d1cce3e352559f610e60e2acb23687eb1cf07b23675fb112863a5741a6fa  iso-codes/iso_3166-3.json
4d5cabcb10ad9ef184553b5e64c0a34ba8deeb20dab2e02cd0be3599b93bb01d  un/un-m49-methodology.html
b9048114f6e7f2abda83bf03d4263c9d7cd1bd7230e3d0461025ee7839a7a1fb  un/un-m49-overview.html
b49d0e9f8ead51ca8b7df6fec89cc3ae6809198b4b9d43c74216da0118f23f5b  unicode/CLDR-LICENSE.txt
67607f4c9cf57157e70564987d8ae92a9ddcd73c00a2f1cd6abf79524a969cbd  unicode/cldr-48.2-en.xml
63095a3b5bd4e0a00d9e1cf96853808cacd0ec7c826dbcbfd31755c6cb9b6c55  unicode/cldr-48.2-subdivisions.xml
36e807ce72b15304dd993f132216f408351be1c4376edaa2fe9e9547e2efcac1  unicode/cldr-48.2-supplementalMetadata.xml
c0c05f91e1c4f9be3d987e27d76cf254b30003b6be41eb94977cc3fe148d4c4e  unicode/uax15-normalization-17.0.html
c04d92e36279b1b353cf4e6964c28383567709026c9fa69420e093961fe774dc  unicode/uts35-ldml.html
```
