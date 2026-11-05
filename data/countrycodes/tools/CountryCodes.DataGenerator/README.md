# CountryCodes.DataGenerator

This project is an offline development tool and is intentionally not referenced by `src/countrycodes.csproj` or `countrycodes.slnx`. It uses only assemblies included in the .NET runtime.

From the repository root, verify the pinned acquisition inputs with:

```sh
dotnet run --project tools/CountryCodes.DataGenerator -- verify-acquisition
```

The command validates the manifest's fail-closed settings, revisions, retrieval dates, license identifiers, unique source IDs and local paths, HTTPS acquisition locations, repository path containment, and every local SHA-256 digest. It performs no network access and does not parse any source dataset.

After verification, parse all pinned generator inputs with:

```sh
dotnet run --project tools/CountryCodes.DataGenerator -- parse-sources
```

The parsers accept streams or text readers so focused fixtures can exercise them without filesystem or network coupling. `parse-sources` always completes acquisition verification before opening a dataset parser.

Normalize parsed records into provenance-preserving staging rows with:

```sh
dotnet run --project tools/CountryCodes.DataGenerator -- stage-sources
```

Every staged field retains its source field and record origin, including the source revision, upstream key, locale, and effective date. Identifiers are normalized with culture-independent ASCII rules, while display text and numeric strings remain byte-for-byte unchanged. `stage-sources` validates these guarantees before it succeeds.

Run strict cross-source reconciliation with:

```sh
dotnet run --project tools/CountryCodes.DataGenerator -- reconcile-sources
```

Reconciliation rejects duplicate or malformed identifiers, conflicting country triples, M49 disagreements, unknown subdivision prefixes and parents, hierarchy cycles, invalid dates or locales, and invalid CLDR transitions. Expected source gaps are recorded explicitly and become failures when they disappear or change, so a source update cannot silently add a new omission.

Validate and apply reviewed supplements from `data/overrides.json` with:

```sh
dotnet run --project tools/CountryCodes.DataGenerator -- process-overrides
```

Each entry requires evidence-backed provenance, a reviewer, a reason, an effective date, and an expiry condition pinned to a manifest source revision. The processor rejects unknown schema fields, expired or stale entries, conflicting supplements, authoritative identifier changes, and any supplement that would displace an upstream value. Accepted entries are ordered by target kind, key, field, and override ID using ordinal comparison.

Generate the checked-in country and subdivision runtime rows with:

```sh
dotnet run --project tools/CountryCodes.DataGenerator -- emit-country-rows
```

The emitter writes country, name, subdivision, historical-country, country-change, and geographic-region tables under `src/Generated` only after acquisition, staging, reconciliation, and override validation succeed. Current and historical rows use ordinal canonical-code order. Reserved/former rows use code-kind then ordinal code order, and the user-assigned range metadata follows the ISO-defined alpha-2, alpha-3, and numeric range sequence. Name rows retain their source revision, field, locale, effective date, and alternate qualifier. Display names are emitted in NFC, with precomputed culture-independent keys that fold case, compatibility characters, diacritics, punctuation, and whitespace. Subdivision aliases and their replacement edges are stored separately so one-to-many transitions and unresolved historical replacements remain explicit. Country-change predecessor and successor edges are also stored separately, preserving splits and multi-step historical transitions with the provenance that established each edge. M49 aggregates, localized names, and country mappings remain separate generated row types so a world, region, subregion, or intermediate-region code cannot be treated as an ISO country assignment.

The same command emits exact identifier indexes and normalized name-search indexes as sorted key-to-row mappings. Generated metadata records the dataset schema and data versions, effective date, generator version, every pinned source revision, and SHA-256 hashes for all generated content files. The aggregate checksum hashes the ordinal file-name and per-file-hash manifest, excluding the metadata file itself to avoid a circular digest.

Verify the checked-in generated snapshot with:

```sh
dotnet run --project tools/CountryCodes.DataGenerator -- verify-generated
```

Verification regenerates twice under invariant, English, Turkish, Arabic, and Japanese cultures in isolated temporary directories. Every run must contain the same generated file names and bytes, and the baseline must match `src/Generated` exactly. The command exits unsuccessfully for missing, unexpected, or changed generated files and never modifies the checked-in snapshot.
