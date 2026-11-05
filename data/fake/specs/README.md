# Reference material

Some local reference files have been replaced by links. See [Reference downloads](../REFERENCE-DOWNLOADS.md) for the current download-only files and their original paths. The inventory below also records historical acquisitions.

Initially downloaded on 2026-08-14 and reviewed and extended on 2026-08-24 for offline design and conformance work. These files are reference inputs, not runtime dependencies. Their original licenses and terms continue to apply. Prefer the dated or versioned document named here when writing conformance tests; re-check live registries before refreshing bundled data.

## Normative and authoritative references

| Area | Local files | Upstream source |
| --- | --- | --- |
| UUID | `rfc9562-uuid.txt` | https://www.rfc-editor.org/rfc/rfc9562.txt |
| URI, DNS, IDNA, Punycode, and safe example domains | `rfc3986-uri.txt`, `rfc1034-domain-concepts.txt`, `rfc1035-domain-implementation.txt`, `rfc3492-punycode.txt`, `rfc5890-idna-definitions.txt`, `rfc5891-idna-protocol.txt`, `rfc2606-reserved-dns-names.txt` | https://www.rfc-editor.org/ |
| Email and internationalized email | `rfc5322-email-format.txt`, `rfc6531-international-email.txt` | https://www.rfc-editor.org/ |
| IP addresses, CIDR, and documentation ranges | `rfc4632-ipv4-cidr.txt`, `rfc4291-ipv6-addressing.txt`, `rfc5952-ipv6-text.txt`, `rfc5737-ipv4-documentation-ranges.txt`, `rfc3849-ipv6-documentation-prefix.txt` | https://www.rfc-editor.org/ |
| HTTP, JWT, and base encodings | `rfc9110-http-semantics.txt`, `rfc7519-jwt.txt`, `rfc4648-base-encodings.txt` | https://www.rfc-editor.org/ |
| Date/time and vCard | `rfc3339-date-time.txt`, `rfc6350-vcard.txt` | https://www.rfc-editor.org/ |
| JSON, CSV, XML, and YAML | `rfc8259-json.txt`, `rfc4180-csv.txt`, `w3c-xml-1.0-fifth-edition.html`, `yaml-1.2.2.html` | https://www.rfc-editor.org/, https://www.w3.org/TR/xml/, and https://yaml.org/spec/1.2.2/ |
| Language tags and matching | `rfc5646-language-tags.txt`, `rfc4647-language-tag-matching.txt`, `iana-language-subtag-registry.txt` | https://www.rfc-editor.org/ and https://www.iana.org/assignments/language-subtag-registry/ |
| Internet registries | `iana-tlds-alpha-by-domain.txt`, `iana-media-types.xml`, `iana-service-names-port-numbers.csv`, `iana-time-zones-zone1970.tab` | https://www.iana.org/ and https://data.iana.org/ |
| Unicode, CLDR, and Unicode security | `unicode-uts35-ldml.html`, `unicode-uax15-normalization.html`, `unicode-uax29-text-segmentation.html`, `unicode-uts39-security.html`, `unicode-uts51-emoji.html`, `unicode-license.txt` | https://www.unicode.org/reports/ |
| CSS color spaces and serialization | `w3c-css-color-4-20220705.html` | https://www.w3.org/TR/2022/CR-css-color-4-20220705/ |
| Currency codes | `iso-4217-current-currencies.xml` | SIX, the ISO 4217 Maintenance Agency: https://www.six-group.com/en/products-services/financial-information/market-reference-data/data-standards.html |
| IBAN | `schwifty-2026.7.3-iban-registry.json`, `schwifty-2026.7.3-license.txt`; exact generated registry transcription and MIT license extracted from the pinned schwifty 2026.7.3 distribution. The transcription identifies SWIFT as upstream; the official download endpoint remained unreliable for the command-line client. | https://github.com/mdomke/schwifty/tree/2026.7.3/schwifty/iban_registry and https://www.swift.com/swift_resource/9606 |
| ISBN | `isbn-users-manual-7th.docx`, `isbn-users-manual-index.html` | https://www.isbn-international.org/index.php/content/isbn-users-manual/29 |
| GS1 identifiers and barcodes | `gs1-general-specifications-26.0.pdf` | https://ref.gs1.org/standards/genspecs/ |
| Machine-readable passports and North American driving licences | `icao-doc9303-part3-2021.pdf`, `icao-doc9303-part4-2021.pdf`, `aamva-dl-id-card-design-2025.pdf` | https://www.icao.int/publications/doc-series/doc-9303 and https://www.aamva.org/topics/driver-license-and-identification-standards |
| Telephone country codes, ICCID, IMSI, and safe NANP examples | `itu-e164-country-codes-2016.pdf`, `itu-e118-iccid-2025.pdf`, `itu-e212-imsi-2024.pdf`, `nanpa-555-reserved-numbers.html` | https://www.itu.int/rec/T-REC-E.118/en, https://www.itu.int/rec/T-REC-E.212/en, and https://nanpa.com/numbering/555-line-numbers |
| IMEI | `gsma-ts06-imei-v28.1.pdf` | https://www.gsma.com/get-involved/working-groups/gsma_resources/ts-06/ |
| VIN | `ecfr-49-cfr-part-565-vin.html` | https://www.ecfr.gov/current/title-49/subtitle-B/chapter-V/part-565 |
| Semantic versions and ULIDs | `semver-2.0.0.html`, `ulid-spec.md` | https://semver.org/spec/v2.0.0.html and https://github.com/ulid/spec |

## Comparative implementation research

The following snapshots are descriptive, not specifications: `fakerjs-api.html`, `python-faker-standard-providers.html`, `bogus-readme.md`, `datafaker-basefaker.html`, `mimesis-providers.html`, and `mockneat-docs.html`. They were used to compare provider breadth, deterministic generation, localization, composition, uniqueness, templating, and export features.

`libphonenumber-readme.md`, `libphonenumber-falsehoods.md`, and `libaddressinput-readme.md` document practical international phone/address behavior and common modeling mistakes. They are design references only; neither project will be linked or shipped under `src/`.
