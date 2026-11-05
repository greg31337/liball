# Universal fake-data library development plan

## Goal and definition of complete

Build a dependency-free-under-`src/`, deterministic, locale-aware .NET fake-data library that can generate low-level random values, standards-shaped identifiers, localized domain values, correlated records, and deliberately difficult edge cases. “Universal” means one consistent API and data-pack format, graceful BCP 47 fallback, broad bundled locale coverage, and a documented path for adding any language or region; it does not mean claiming that every culture has identical fields or that generated identifiers were officially issued.

A feature is complete only when it has a public XML-documented API, deterministic seeded behavior, argument and exhaustion behavior, locale fallback where applicable, valid and invalid test vectors, a synthetic-safe behavior where contactable data is involved, and tests against the relevant material in `specs/`.

## Non-negotiable constraints

- Target `net10.0` and follow `syntax.md` without changing it.
- Source may use only built-in .NET APIs. No NuGet package, copied third-party implementation, runtime network call, or external process may be required by `src/`.
- Test-only oracle/validation packages are allowed under `tests/`; they must not leak into the product assembly.
- Public implementation types are classes, interfaces, and enums with ordinary methods. Interfaces are reserved for stable behavioral contracts that remove concrete coupling; public data remains represented by classes with public fields. Do not design around properties, records, structs, delegates, authored generic types/methods, optional/default parameters, nullable value types, LINQ, class inheritance, `ref`/`out`/`in`, iterators, or exception handling syntax forbidden by `syntax.md`.
- Use overloads instead of optional parameters. Use built-in `Func<...>` types where callbacks are useful. Use `object`, arrays, and built-in generic collections at API boundaries that would normally need authored generics.
- Use `Span<T>`/`ReadOnlySpan<T>` by value for checksum, encoding, formatting, and destination-writing paths where they simplify allocation-free work; do not rely on prohibited `stackalloc`, `ref`, or `scoped` syntax.
- All public members receive the XML documentation allowed by `syntax.md`.
- Do not create syntax-policy/source-scanning tests. Do not create performance or benchmark tests.
- Never imply that checksum-valid values are allocated, owned, deliverable, or safe to use as credentials. Default contact data to reserved/example ranges.

## Feature catalog, low level to high level

### 1. Core state, reproducibility, and selection

- Stable, documented pseudo-random stream whose seeded output is reproducible across process runs and supported .NET patch versions.
- Unseeded convenience creation and a cryptographic-random mode based only on `System.Security.Cryptography` for tokens that request it.
- Seed from integer, long, string, and byte array; reset; state snapshot/restore; named child streams; independent per-provider streams; deterministic reference date/time.
- Uniform booleans, bytes, signed/unsigned integral types, `BigInteger`, float/double/decimal, finite-only floating values, inclusive/exclusive bounds, and rejection sampling without modulo bias.
- Probability, ratio, weighted choice, multiple choice with/without replacement, shuffle, sample, sequence, cycle, optional/maybe, repeat, and bounded uniqueness with explicit exhaustion.
- Uniform, normal, log-normal, exponential, triangular, and weighted-discrete distributions; range and argument validation.
- Choice from strings and objects without authored generic methods; random enum value through a `Type`/object-based API.
- Named generation scopes so adding a call in one provider does not perturb an unrelated provider’s deterministic sequence.

### 2. Primitive numbers, strings, bytes, and text mechanics

- Digit, binary, octal, decimal, hexadecimal, base-32, base-36, base-58, and base-64 strings; signed/unsigned and padded numeric strings.
- ASCII letter, Unicode letter, alphanumeric, symbol, whitespace, printable, custom-alphabet, and locale-script strings with fixed or bounded length.
- Correct distinction between UTF-16 code units, Unicode scalar values, and grapheme clusters; never split a surrogate pair or grapheme in truncate/clamp operations.
- Normalization choices (none/NFC/NFD/NFKC/NFKD), casing, transliteration where explicitly supported by data, ASCII folding, slug generation, title casing, and identifier-safe text.
- Format-mask replacement (`#` digit, `?` letter, `*` alphanumeric, escapes), mustache-style named templates, bounded regular-expression subset generation, and template-cycle detection.
- Random bytes, hexadecimal/base encoding, hashes of requested display length, secrets/passwords with required character classes, passphrases, PINs, recovery codes, and deterministic versus cryptographic modes.
- Words by part of speech, weighted word frequency, lorem words/sentences/paragraphs/lines, exact and bounded lengths, punctuation, and locale-native filler text.

### 3. Checksums, identifiers, and standards-shaped values

- Reusable Luhn, Verhoeff, Damm, weighted modulo-10, modulo-11, ISO 7064 MOD 97-10, ISBN-10, ISBN-13/GTIN, and VIN checksum engines with validate/calculate/append APIs.
- GUID/UUID v4 and v7, nil/max UUID, canonical strings, and destination-span formatting per RFC 9562; intentionally invalid version/variant cases.
- ULID random and monotonic forms, fixed-time generation, overflow behavior, canonical Crockford Base32, parsing/validation, and timestamp extraction.
- Nano ID-style configurable alphabet IDs, MongoDB ObjectId-shaped IDs, sortable timestamp IDs, short IDs, decimal sequences, and opaque tokens.
- ISBN-10/13, ISSN, DOI-shaped strings, EAN-8, UPC-A, GTIN-12/13/14, GS1 check digits, SKU, serial, lot, and order numbers.
- IMEI/IMEISV, VIN, license plate, passport, driving-license, tax, social/national identity, and business-registration shapes where a locale pack supplies authoritative rules.
- Explicit metadata distinguishing structurally valid, checksum valid, assigned/known (never claimed without a registry), fictional-reserved, and deliberately invalid output.

### 4. Dates, times, durations, and calendars

- `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, and `TimeSpan` generation at arbitrary precision and within inclusive/exclusive ranges.
- Past, future, recent, soon, anytime, between, multiple sorted dates, business-day, weekend, month, weekday, quarter, fiscal period, and leap-day generation.
- Birth dates by age range, exact age at reference date, anniversaries, expiration dates, and correlated start/end intervals.
- UTC, local-offset, and IANA/Windows time-zone ID values from bundled/runtime registries; DST gap/overlap, minimum/maximum supported dates, leap year, end-of-month, and offset-boundary edge cases.
- RFC 3339/ISO-like output, Unix seconds/milliseconds, locale-formatted date/time text, relative phrases, month/weekday names, and deterministic reference-clock behavior.

### 5. Locale, language, script, and culture infrastructure

- Normalize and validate BCP 47 tags; language/script/region/variant parsing; aliases; parent fallback; configured fallback; invariant fallback; and clear unsupported-locale errors in strict mode.
- Locale metadata: English/native names, script, writing direction, territories, default currency, telephone code, time zones, date order, decimal separators, address format, and available provider fields.
- Locale pack version, source/provenance, license, fallback origin, and completeness/coverage reporting.
- Language names/codes, territory names/codes, script names/codes, currency display names, time-zone names, and localized direction/month/weekday terms.
- Unicode normalization and grapheme-safe handling; right-to-left and bidirectional text cases; combining marks; non-BMP characters; zero-width characters; mixed scripts; locale-sensitive casing.
- Runtime registration of user locale resource streams/data dictionaries without inheritance or third-party serializers; duplicate and malformed pack diagnostics.

### 6. People, identity, demographics, and work

- Given, middle, family, full, display, formal, informal, maiden, patronymic/matronymic, prefix, suffix, initials, transliterated, and native-script names driven by locale-specific ordering/templates.
- Sex category used only where data selection requires it, independent gender identity text, pronouns, title/honorific, age, birth date, zodiac, marital status, and household relationship.
- Username, safe example email, phone, avatar data URI, biography, signature, favorite color, locale/language, and time zone.
- Occupation title, descriptor, area, type, seniority, department, employment status, salary band, education level, school/university, and professional profile.
- Correlated `PersonProfile` so name, username, email, phone, birth date/age, locale, address, employer, and identifiers agree with one another.
- Name edge cases: mononyms, multi-part surnames, particles, apostrophes, hyphens, diacritics, combining forms, right-to-left names, very long names, and missing optional components represented by empty/reference-null policy.

### 7. Addresses, places, and geospatial values

- Building number, street name/type, secondary unit, PO box, neighborhood/district, city/locality, county, state/province/subdivision, postal code, country, and formatted postal address.
- Locale-correct field order, line breaks, abbreviations, native/localized forms, rural routes, dependent localities, missing postal codes, alphanumeric codes, and region-specific validation masks.
- Country ISO-like alpha-2/alpha-3/numeric shapes, subdivision codes, continent, language, currency, time zone, and calling-code correlation.
- Latitude, longitude, altitude, depth, coordinate pair, geohash-shaped text, bounding-box point, nearby point within radius, cardinal/ordinal direction, and compass bearing.
- Reserved-example address mode versus plausible synthetic mode; never claim that a street/postcode combination exists or is deliverable.
- Correlated `AddressRecord` and `GeoRecord` with country, subdivision, postal format, time zone, and coordinates that do not contradict configured broad bounds.

### 8. Internet, networking, web, and security-shaped data

- Reserved-safe and realistic domain labels/names, TLDs, IDNs/Punycode, subdomains, hostnames, URLs/URIs, paths, query strings, fragments, protocols, and ports.
- ASCII and SMTPUTF8 email, display mailbox, example-domain email, username, password, passphrase, and deterministic Gravatar-compatible hash-shaped value without external lookup.
- IPv4/IPv6 text and .NET address objects; public/private/link-local/loopback/multicast/documentation categories; CIDR, masks, network ranges, endpoints, and canonical IPv6 formatting.
- MAC addresses with local/unicast flags, network interfaces, service names, registered/dynamic ports, HTTP methods/status codes/headers, user agents, MIME types, and data URIs.
- JWT-shaped unsigned/synthetic tokens with claims, algorithms labels, and expiry correlation; never present fake cryptographic material as authenticated.
- Browser, operating system, device, app/version, bot/crawler, URL short code, emoji, social handle, hashtag, and safe image placeholder/SVG data URI.
- Injection-oriented edge values for URL, HTML, JSON, CSV, SQL, shell, log, and header contexts, available only through explicit edge/adversarial APIs and clearly labeled as unsafe strings.

### 9. Telephony and devices

- E.164, national, international, extension, mobile, landline, toll-free, premium, and reserved-fiction phone formats driven by locale metadata.
- Default safe ranges where authorities publish them (for example NANPA 555-0100–0199); explicit plausible mode elsewhere with non-deliverability disclaimer.
- Country calling code, area code, dial prefix, extension, phone words where applicable, and formatting variants.
- IMEI/IMEISV validation and generation, device manufacturer/model/type, operating system/version, screen size, hardware identifiers, Android-style ID, push-token-shaped values, and SIM/ICCID/IMSI shapes with safeguards.
- Phone-number edge cases from `libphonenumber-falsehoods.md`: variable length, shared calling codes, non-geographic services, leading zeros, extensions, formatting ambiguity, and numbers not uniquely mapping to people.

### 10. Finance, banking, payments, and cryptocurrency-shaped data

- Decimal amount, price, money record, currency code/numeric code/name/symbol/minor units, exchange-rate-shaped value, account type/name, transaction type/description/reference, and ledger interval.
- Credit/debit card PAN by issuer pattern, Luhn validity, masked PAN, CVV shape, PIN, expiry, cardholder, and synthetic test-mode marker; do not imply authorization or active accounts.
- IBAN per-country length/BBAN pattern/MOD 97, print/electronic forms, BIC shape, ABA routing checksum, domestic account shapes, bank/branch names, and bank profile.
- Check, invoice, purchase order, tax amount/rate, discount, fee, interest rate, quantity, balance, and correlated transaction/payment records.
- Bitcoin/Ethereum/Litecoin and generic wallet/address/hash-shaped values with explicit shape-only semantics; never generate or expose a private key by default.
- Locale-appropriate currency and formatting while keeping numeric values separate from display strings.

### 11. Company, commerce, products, and logistics

- Company name/suffix/legal form, department, industry, slogan, catch phrase, buzz phrase, business ID/tax ID, email/domain/phone/address, and employer profile.
- Product name/adjective/material/category/description, brand, SKU, GTIN, color, dimensions, weight, price/currency, inventory, and product profile.
- Order/cart/line items, quantity, subtotal/tax/discount/shipping/total consistency, status, timestamps, shipping/billing addresses, tracking number, and invoice profile.
- Retail department, promotion/coupon, subscription plan/status/interval, marketplace seller/review/rating, and return/refund reason.
- Logistics carrier/method, warehouse/bin, package dimensions, container/tracking shapes, origin/destination, and estimated delivery interval.

### 12. Files, software, databases, and developer data

- File name/base/extension/type/MIME, directory/path for Unix and Windows, rooted/relative forms, URI, size, checksum-shaped value, and line-ending variants.
- Semantic version, framework/app version, cron-shaped expression, environment variable name, command argument, exception/message/stack-trace-shaped text, and log entry.
- Database name/schema/table/column/index/constraint, SQL type/collation/engine, MongoDB ObjectId, SQL identifier, connection-string-shaped value, and safe fake SQL statement text.
- Git branch/tag/commit SHA/message/author/date/entry, repository/issue/pull-request shapes, package name/version, programming language, and license identifier.
- JSON scalar/object/array text, CSV row/document, XML element/document-shaped text, YAML-shaped text, and SQL insert generation with correct escaping and configurable invalid cases.
- No formatter performs I/O or executes generated commands/SQL; output is returned as data.

### 13. General-purpose thematic providers

- Color: named/localized colors and RGB, HSL, HWB, CMYK, Lab, LCH, hex, CSS function, and palette records.
- Vehicle/transport: make, model, type, fuel, bicycle, registration/plate, VIN, route, transit mode, and vehicle profile.
- Airline: airline/airport/aircraft codes and names, flight number, seat, record locator, cabin, route, and itinerary profile.
- Books/media: title, author, publisher, series, genre, format, ISBN, publication date, movie/TV/podcast/news-shaped metadata without copied copyrighted text.
- Food: dish, cuisine, ingredient, fruit, vegetable, meat, spice, beverage, dietary/allergen tag, menu item, and nutrition-shaped record.
- Animals/nature: species/common names by broad category, pet names, breed-shaped values, habitat, plant, tree, flower, mountain, and weather-shaped values.
- Science/measurement: chemical element, scientific field, unit, SI prefix, measurement record, coordinate, planet/space terms, and experiment-shaped metadata.
- Health: blood type, body part/system, symptom, procedure/diagnosis-code-shaped value, medicine-form/dosage-shaped text, provider/facility record, with conspicuous non-clinical-use warnings and no claim of coding completeness.
- Education: institution, course, subject, degree, academic term, grade, student ID shape, and education profile.
- Culture/text vocabularies: music artist/album/song/genre, art, hobby, sport/team/position, gaming, marketing, fashion, construction, appliance, weather, military/NATO alphabet, hacker, and programming vocabulary.
- Thematic data must be generic, licensed, non-defamatory, and free of live secrets, personal datasets, and substantial copyrighted passages.

### 14. Composition, records, schemas, templates, and exports

- Concrete records: `PersonProfile`, `AddressRecord`, `GeoRecord`, `CompanyProfile`, `ProductProfile`, `OrderProfile`, `PaymentProfile`, `VehicleProfile`, `FlightProfile`, `DeviceProfile`, and metadata/value-result classes, all using public fields.
- Rule set for populating public fields/properties on an existing `object`, constructor/factory callback support via built-in delegates, ordered rules, skip/constant/generated rules, nested paths, and strict missing-member diagnostics.
- Schema tree for scalars, arrays, objects, constants, references, derived fields, conditions, weighted variants, sequences, and cross-field correlation without authored generic types.
- Named template placeholders, provider-method catalog, escaping, nested evaluation, format arguments, missing-token diagnostics, recursion limit, and deterministic evaluation order.
- Generate one, many, lazy-like callback delivery without iterator syntax, fill existing arrays/lists, deterministic paging, and cancellation checks where ordinary APIs permit.
- Dataset-level uniqueness by field/composite key, retry budget, deterministic exhaustion, resettable scopes, and sequence counters.
- Export records/schemas to JSON, CSV, XML, vCard, and SQL insert text using built-in .NET serializers or dedicated escapers; configurable headers, null/empty handling, newline, indentation, and culture-invariant machine formats.
- Validation result reports whether a generated value met structural, checksum, locale, safe-reserved, correlation, and requested edge-case expectations.

### 15. Edge cases, failure semantics, and safety

- Generation modes: typical, boundary, valid-unusual, invalid, mixed, and adversarial; safety modes: reserved, synthetic-plausible, and unrestricted-shape (still never claimed allocated).
- Empty string, reference `null`, whitespace-only, min/max length, overlong, repeated, all-equal, zero/negative, min/max numeric, NaN/infinity where explicitly requested, and precision/rounding cases.
- Unicode cases: RTL, bidi controls, combining marks, decomposed text, emoji sequences, non-BMP, zero-width joiners/non-joiners, confusables, mixed scripts, invalid UTF-16 only where a `char[]` API can represent it, and normalization changes.
- Temporal cases: leap day, DST gap/overlap, offset extremes, year/month boundary, Unix epoch, min/max supported runtime values, reversed/empty range rejection.
- Identifier cases: wrong checksum, bad length, invalid character, forbidden prefix, bad UUID variant/version, ULID overflow, invalid IBAN country/length, malformed IDN, and ambiguous phone formatting.
- Collection/composition cases: zero count, huge requested count rejected by configured limits, uniqueness exhaustion, cyclic schema/template references, missing locale field, fallback disabled, and contradictory correlation constraints.
- Injection strings are opt-in and returned as data only. Default APIs produce escaped/safe-reserved values suitable for fixtures and documentation.
- Every invalid argument throws a documented library exception derived directly from `System.Exception` or an appropriate built-in exception; no silent clamping unless the API name says so.

## Locale rollout and data policy

- Base/invariant pack contains standards-shaped, non-cultural data and safe example values.
- Wave 1 regional packs: `en-US`, `en-GB`, `en-CA`, `en-AU`, `de-DE`, `de-AT`, `de-CH`, `fr-FR`, `fr-CA`, `es-ES`, `es-MX`, `pt-PT`, `pt-BR`, `it-IT`, `nl-NL`, `pl-PL`, `cs-CZ`, `sv-SE`, `nb-NO`, `da-DK`, and `fi-FI`.
- Wave 2 script/ordering coverage: `ar-EG`, `ar-SA`, `fa-IR`, `he-IL`, `hi-IN`, `th-TH`, `tr-TR`, `ru-RU`, `uk-UA`, `el-GR`, `ja-JP`, `ko-KR`, `zh-CN`, `zh-TW`, `zh-HK`, `id-ID`, and `vi-VN`.
- Later packs are accepted whenever data has documented provenance, compatible terms, native-speaker/reviewer sign-off where practical, and conformance tests. Language-only packs may provide text/name defaults; region packs override address, phone, identifier, currency, and date conventions.
- Data resources must include provenance and license metadata. Do not scrape personal records, breach data, live credentials, copyrighted prose, or undocumented lists. Prefer standards registries, public-domain/compatible word lists, and clearly synthetic components.
- Fallback is field-level: exact locale → language/script parent → configured fallback → invariant. `FallbackTrace` exposes the actual source; strict mode can reject any fallback.

## Public API completion audit

Reviewed against the feature catalog and the public types under `src/Api` on 2026-08-24. The existing layer already covers the root generator, deterministic and cryptographic randomness, primitives and text, checksums and common identifiers, dates, locale fallback, the principal domain providers, profiles, rules, schemas, edge modes, and structured exports. The open tasks below are limited to catalog features that have no complete public entry point or result model, plus the functional tests needed to close coverage.

## Public API completion tasks

- [DONE] T001: Add public low-level repeat and destination-fill selection operations driven by built-in callbacks, plus `Type`/object-based random enum selection with bounded counts, deterministic behavior, and explicit invalid-type and exhaustion failures.
- [DONE] T002: Add functional tests for the new repeat, fill, and enum-selection operations, covering zero and maximum counts, callback failures, every enum value, seeded reproducibility, and invalid arguments.
- [DONE] T003: Expand `LocaleInfo` and `ILocaleProvider` with English and native locale names, writing direction, script and territory catalogs, localized language/script/territory/currency/time-zone names and codes, default currency and calling code, time zones, date order, decimal separators, address format, and provider availability with observable provenance and fallback.
- [DONE] T004: Add cross-locale functional tests for every locale metadata and catalog operation across invariant, Wave 1, and Wave 2 packs, including exact values, aliases, parent/configured/invariant fallback, strict rejection, RTL locales, returned-array isolation, and malformed registrations.
- [DONE] T005: Add locale-backed public generation, validation, mutation, and metadata APIs for passport, driving-licence, national/social identity, personal tax, business-registration, and vehicle-registration identifiers, returning unsupported-locale failures when authoritative rules are unavailable and never claiming assignment.
- [DONE] T006: Add conformance and safety tests for every supported civil identifier using authoritative locale vectors, valid and invalid lengths/characters/checksums, fictional-reservation and allocation metadata, fallback behavior, and deterministic mutation kinds.
- [DONE] T007: Complete the date/time public surface with convenience time and duration generation, requested-precision generation, quarter generation, locale-formatted date/time strings, localized relative phrases, and explicit DST gap/overlap and offset-boundary values.
- [DONE] T008: Add functional date/time tests for every new operation over all temporal precisions and boundaries, leap days, end-of-month and fiscal transitions, DST gaps/overlaps, offset extremes, locale formats and phrases, reversed or empty ranges, and deterministic reference clocks.
- [DONE] T009: Complete person APIs with maiden, patronymic, and matronymic components, plural initials, native and transliterated component access, birth-date/age and work/education bridges, and expand `PersonProfile` to expose correlated optional name components, pronouns, identifiers, avatar, favorite color, language, and time zone.
- [DONE] T010: Add person and profile tests for all new components and correlations across differing name orders and scripts, including missing optional parts, mononyms, long names, category independence, exact age, reserved contact data, fallback traces, and seeded reproducibility.
- [DONE] T011: Complete internet/network APIs with generic `IPAddress` selection, IPv6 masks and network ranges, generic endpoints, Gravatar-compatible hash-shaped values without lookup, URL short codes, and explicit HTTP header name/value records.
- [DONE] T012: Add internet/network tests using RFC 3492, RFC 3986, RFC 4291, RFC 4632, RFC 5737, RFC 5952, and UTS 39 cases, including Punycode round trips, CIDR boundaries, IPv6 canonicalization, address categories, header injection exclusion, reserved-safe defaults, and malformed variants.
- [DONE] T013: Complete phone/device APIs with explicit reserved-fiction generation, phone-word formatting where locale rules support it, and ICCID/IMSI result records that expose parsed components, structural/checksum validity, synthetic markers, and unknown allocation status.
- [DONE] T014: Add ITU-T E.118 and E.212 conformance tests for ICCID and IMSI lengths, fields, check digits, two- and three-digit MNC handling, private/test shapes, mutation failures, parsing metadata, and unsupported locale or category behavior.
- [DONE] T015: Complete finance APIs with exchange-rate-shaped values, ledger intervals, purchase-order records, configurable quantities, and correlated account-ledger records while keeping amounts numeric and display formatting separate.
- [DONE] T016: Add finance tests for rate and amount precision, rounding modes, interval ordering, debit/credit balance arithmetic, quantity limits, purchase-order correlation, currencies with unusual or absent minor units, and invalid or cross-currency inputs.
- [DONE] T017: Complete company, commerce, and logistics APIs with company slogans, retail departments, marketplace sellers, container identifiers, purchase-order linkage, and correlated seller/order/shipment fields.
- [DONE] T018: Add company, commerce, and logistics tests for the new values and records, covering locale fallback, fictional organization guarantees, duplicate and empty inventories, arithmetic consistency, route and delivery ordering, return/refund transitions, and deterministic correlation.
- [DONE] T019: Add a public media provider and data records for synthetic movie, television, podcast, and news-shaped metadata, with compositional titles and summaries that do not reproduce copyrighted text or claim real publication.
- [DONE] T020: Add media-provider functional tests for every scalar and record operation, category membership, date and episode correlation, nonempty bounded text, synthetic markers, locale fallback, determinism, and non-reproduction fixtures.
- [DONE] T021: Complete system/developer APIs with framework versions, application versions, package metadata records, and explicit line-ending variants, then add tests for valid and invalid shapes, platform path styles, size limits, and deterministic output.
- [DONE] T022: Add a public template contract and implementation reachable from `Fake` that can register resolvers, resolve named tokens, render values, validate templates, and enumerate tokens while preserving escaping, deterministic evaluation, and recursion/cycle limits.
- [DONE] T023: Add template functional tests for registration precedence, provider and caller resolvers, nested values, escaping, format arguments, missing tokens, duplicate registration, validation diagnostics, cycles, recursion limits, and deterministic evaluation order.
- [DONE] T024: Complete schema composition with generate-many, callback-delivery, fill-existing-array/list, deterministic paging, and cancellation-aware public operations that share sequence, uniqueness, and correlation state correctly.
- [DONE] T025: Add schema composition tests for zero and maximum counts, callback order, paging stability, cancellation, partial-delivery behavior, references and derived fields across records, sequence continuity, uniqueness exhaustion, cycles, and output-size limits.
- [DONE] T026: Add span-destination writers where output can be bounded for JSON strings/scalars, CSV fields/rows, XML elements, vCard lines, and SQL identifiers/literals/inserts, with exact written-length results and no partial successful writes.
- [DONE] T027: Add export tests using RFC 4180, RFC 8259, XML 1.0 Fifth Edition, YAML 1.2.2, and RFC 6350 cases, covering exact-fit and undersized spans, escaping, invalid Unicode, null/empty policy, formula guards, deterministic member ordering, dialect quoting, and independent parser round trips.
- [DONE] T028: Generalize public generation metadata and validation results beyond identifiers so callers can observe structural and checksum validity, locale source, safe-reserved status, allocation knowledge, cross-field correlation, requested edge case, and the deliberately violated rule for any generated result that exposes metadata.
- [DONE] T029: Add metadata and validation tests across typical, boundary, valid-unusual, invalid, mixed, and adversarial generation; reserved, plausible, and unrestricted safety; fallback and strict locale modes; and all applicable validity dimensions.
- [DONE] T030: Complete functional coverage of the entire existing and newly added `src/Api` surface with direct behavior tests for every public constructor and method, every enum mode, and every data-result invariant, including success, argument failure, exhaustion, deterministic reset/snapshot/fork behavior, locale fallback, safety guarantees, correlation, and span boundary behavior; use test-only external validators where useful without introducing any dependency under `src/`.
