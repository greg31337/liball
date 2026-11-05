# Bundled data licensing and notices

This file records licenses that are approved or expected for planned bundled
data. It is not a substitute for `manifest.json`: each shipped resource must
identify exactly which notice applies. The shipped resource records in that
manifest identify their source snapshots, versions, transformations, hashes,
reviewers, and risk decisions.

## Approved license families

### IANA protocol registries — CC0-1.0

IANA and the IETF intend protocol registry data to be freely usable for any
purpose and dedicate their applicable rights under the Creative Commons CC0
1.0 Universal dedication. This covers the shipped language-tag, media-type,
and service/port registry extracts. It does not expressly cover the separate
root-zone TLD list.

- Terms: https://www.iana.org/help/licensing-terms
- Dedication: https://creativecommons.org/publicdomain/zero/1.0/legalcode

Registry references to RFCs or other documents are not themselves covered by
this dedication.

### IANA root-zone TLD factual extract

The shipped TLD table is a reviewed extract of bare ASCII root-zone labels and
the source serial. The repository does not claim that IANA's protocol-registry
CC0 dedication covers this separate list. No accompanying prose, delegation
metadata, contacts, registrant data, WHOIS data, or trademark rights are copied
or granted. Registry presence is not evidence that a child domain is available,
unowned, reachable, trustworthy, or safe to contact.

- Source: https://data.iana.org/TLD/tlds-alpha-by-domain.txt

### IANA Time Zone Database `zone1970.tab` — public domain

The distributed `zone1970.tab` file declares itself public domain. The exact
tzdb release and the file hash must accompany a shipped extract.

- Source: https://data.iana.org/time-zones/tzdb/zone1970.tab

### Unicode and CLDR data — Unicode-3.0

Unicode data files, including CLDR data files unless specifically marked
otherwise, are provided under the Unicode License v3. A copy of the license is
stored at `specs/unicode-license.txt`; its copyright and permission notice must
be reproduced in documentation accompanying derived data.

- License: https://www.unicode.org/license.txt
- Terms: https://www.unicode.org/copyright.html
- CLDR releases: https://cldr.unicode.org/index/downloads

### libaddressinput data — CC-BY-4.0

The libaddressinput project states that its source code is Apache-2.0 and its
address metadata is CC-BY-4.0. Runtime address metadata may use only a pinned
data release/commit, must credit Google and the libaddressinput contributors,
must link the license, and must identify transformations.

- Project and license statement: https://github.com/google/libaddressinput
- License: https://creativecommons.org/licenses/by/4.0/legalcode

### W3C CSS Color Module Level 4 — W3C Software and Document License 2023

The color provider retains a reviewed subset of CSS named-color keywords and
their numeric sRGB values from the 6 August 2026 Candidate Recommendation
Draft. It copies no specification prose, examples, or full table. W3C requires
its notice and modification attribution when portions of the work are copied or
derived.

CSS Color Module Level 4:
https://www.w3.org/TR/2026/CRD-css-color-4-20260806/

Copyright © 2026 World Wide Web Consortium. All Rights Reserved. This work is
distributed under the W3C Software and Document License in the hope that it
will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

Modification notice: this project retains only sixteen keyword names and their
numeric sRGB values, represents them as parallel runtime arrays, and supplies
independently authored conversion code. No specification prose or examples are
included.

- Source: https://www.w3.org/TR/2026/CRD-css-color-4-20260806/
- License: https://www.w3.org/copyright/software-license-2023/

### BIPM SI Brochure — CC-BY-4.0

The science provider retains a small factual selection of quantity, unit,
symbol, SI-prefix, and exponent fields. It copies no explanatory prose or full
table. The source is the ninth edition of the SI Brochure, updated in 2026, and
is credited to the Bureau International des Poids et Mesures.

- Source: https://www.bipm.org/en/publications/si-brochure/
- License: https://creativecommons.org/licenses/by/4.0/legalcode

### United States federal regulations

Rules extracted from 49 CFR Part 565 are U.S. government material and are used
only as factual requirements for independently authored VIN logic/data. The
source and retrieval date must be cited. Editorial additions or third-party
material on the eCFR site are not presumed public domain.

- Source: https://www.ecfr.gov/current/title-49/subtitle-B/chapter-V/part-565

### SIX ISO 4217 factual registry extract

The shipped currency catalog retains only the current entity mappings, English
currency names, alphabetic and numeric codes, and minor-unit facts from the
dated List One XML snapshot. It does not reproduce ISO standard text or
explanatory SIX material, and it does not claim a license to either. SIX is
credited as the ISO 4217 Maintenance Agency in the resource manifest.

- Source: https://www.six-group.com/en/products-services/financial-information/market-reference-data/data-standards.html

### schwifty IBAN registry transcription — MIT

The shipped IBAN format catalog is transformed from the pinned schwifty 2026.7.3
`generated.json` transcription, which identifies the SWIFT ISO 13616 IBAN
Registry as its source. The exact MIT license is retained at
`specs/schwifty-2026.7.3-license.txt`. The runtime resource keeps only reviewed
format facts and does not reproduce SWIFT prose, examples, contact material, or
bank and customer account data. No SWIFT trademark rights are claimed.

- Project: https://github.com/mdomke/schwifty
- Pinned source: https://github.com/mdomke/schwifty/tree/2026.7.3/schwifty/iban_registry

## Conditional notices

These sources are not approved until the conditions in `SOURCES.md` are met.

### IETF RFC material

RFC documents are governed by the IETF Trust Legal Provisions, not the IANA
CC0 registry dedication. Bundled data may encode independently selected factual
constants from an RFC. It must not reproduce or adapt RFC prose. Any copied code
component requires the applicable Revised BSD notice and an explicit review.

- Terms: https://trustee.ietf.org/documents/trust-legal-provisions/

The invariant pack and safe-ranges table encode only the short factual
constants selected from RFC 2606, RFC 5737, and RFC 3849. They do not reproduce
or adapt explanatory RFC prose. The NANPA 555 entry likewise retains only the
factual 555-0100 through 555-0199 reservation and its citation.

### Google libphonenumber — Apache-2.0

If phone metadata is admitted, the exact upstream tag/commit, Apache-2.0
license, upstream copyright notice, NOTICE material, modifications, and the
safe-number filtering policy must be recorded. The current repository snapshot
is a design reference only.

- Project: https://github.com/google/libphonenumber
- License: https://www.apache.org/licenses/LICENSE-2.0

## Sources currently prohibited from bundling

No content from the following sources may be placed in runtime resources until
`SOURCES.md` is changed from **Hold** after license review:

- ITU E.164 tables: ITU's site asserts copyright and directs users to request
  reproduction permission.
- GS1 General Specifications tables, company prefixes, and allocation data.
- GSMA TS.06 text, tables, TAC allocation data, and device identity databases.
- Any ISO standard text or table obtained from a standards publication.
- Any comparative faker library's data, regardless of whether its API or source
  code has an open-source license.

Independent implementations of uncopyrightable algorithms or short factual
constants still require a source citation and review; this exception never
permits copying prose, examples, compilations, or allocation registries.

## Project-authored data

The repository does not yet contain a root license. Until one is selected,
project-authored invariant and locale data may be developed and reviewed but
must not be published as an independently redistributable data pack. The
shipped invariant pack currently contains only the standards-derived factual
constants covered above; its organization and metadata are project-authored.
Every original-data contribution must record author, date, source method, and
reviewer, and affirm that it is original rather than copied or scraped.

## Trademark and allocation disclaimer

Licensing a dataset does not license third-party trademarks or authorize use of
allocated identifiers. Generated names, codes, identifiers, accounts, device
numbers, telephone numbers, addresses, and domains are synthetic fixture data.
They must not be described as registered, assigned, owned, reachable,
deliverable, authenticated, financially usable, or suitable for production.
