# molecule

`molecule` is a small C# chemistry toolkit implemented with the .NET standard library. It provides molecule graph storage, format readers and writers, simple standardization, canonical identity hashes, SMARTS matching, fingerprints, descriptors, and 2D depiction helpers.

## Supported Format Features

| Format | Read | Write | Supported surface |
| --- | --- | --- | --- |
| SMILES | yes | yes | Atoms, organic subset atoms, bracket atoms, isotopes, hydrogens, formal charges, atom maps, branches, ring closures, disconnected components, aromatic atoms/bonds, directional bond markers, strict diagnostics, and recovery mode. The default writer is canonical; traversal-order output is available through `SmilesWriter.WriteNoncanonical`. |
| SMARTS | yes | no | Query atoms and bonds, any atom/bond, atomic-number primitives, isotopes, hydrogens, charges, aromatic/aliphatic primitives, atom lists, negated atomic numbers, degree/ring/valence constraints, branches, rings, components, and directional bond primitives for matching. |
| MOL V2000 | yes | yes | Header blocks, counts, atoms, bonds, coordinates, atom lists, aliases, atom-block properties, `M  CHG`, `M  ISO`, `M  RAD`, basic bond stereo, feature policies, count limits, and structured diagnostics. |
| MOL V3000 | yes | yes | CTAB counts, atoms, bonds, coordinates, atom lists, aliases, common atom properties, atom and bond stereo, enhanced stereo collections, feature policies, and structures beyond V2000 count limits. |
| SDF | yes | yes | Mixed V2000/V3000 multi-record files, data fields, aggregate and per-record results, configurable failed-record recovery and retention, optional separators, and streaming reads/writes. |
| MOL2 | yes | yes | Tripos molecule, atom, bond, and substructure sections; molecule and charge types, comments, coordinates, atom and bond types, partial charges, typed residues, substructure types, roots, chains, membership, and structured writer diagnostics. |
| PDB | yes | yes | `HEADER`, multi-model `MODEL`/`ENDMDL`, `ATOM`, `HETATM`, `CONECT`, and `END` records; model-aware serials and connectivity; residues, chains, alternate locations, insertion codes, coordinates, occupancy, B-factor, element, charge, structured writer diagnostics, and configurable non-water ligand extraction. |
| InChI | yes | yes | Parses and stores existing InChI layers as metadata, including formula, connectivity, hydrogen, tautomer, charge, proton, stereo, isotope, and fixed-H layers. Export returns stored metadata text. |
| InChIKey | yes | yes | Parses and stores existing InChIKey blocks and version/protonation metadata. Export returns stored metadata text. |
| Vendor delimited CSV/TSV | yes | yes | Reads and writes caller-selected columns and delimiters with optional headers, quoted delimiters/quotes/multiline fields, lossless vendor metadata, and embedded SMILES, MOL V2000/V3000, SDF, MOL2, PDB, or deterministic molecule text. |
| Deterministic text | yes | yes | Library-specific deterministic serialization for exact molecule identity, regression tests, and stable round trips. |

Format detection accepts caller hints, common filename extensions, and content markers. `MoleculeFormats` exposes a facade for string, stream, and format-specific read/write paths. `IMoleculeReader` and `IMoleculeWriter` provide a uniform contract for single-molecule text codecs, while `IMoleculeFormatService` is the injectable facade boundary used by streaming, batch conversion, and text-based duplicate ingestion.

## Public API Boundary

All supported application-facing types are declared under `src/Api` while retaining the `molecule` namespace. Implementation machinery—including parser state, graph caches, built-in rule records, sorting/formatting helpers, and intermediate algorithm work objects—lives outside that tree and is `internal`. Public symbols include XML documentation describing behavior, inputs, results, and important sentinel values.

Prefer `MoleculeFormats` for conversion, `Standardizer` for cleanup, `Canonicalizer` or `DuplicateDetector` for identity, `SubgraphMatcher` for graph queries, and `FingerprintGenerator` plus `Similarity` for similarity workflows. Concrete readers, writers, perception services, graph operations, coordinate helpers, and result models are also public when direct control is useful. Applications can depend on the format interfaces and inject alternate implementations without coupling their own services to the concrete facade.

`MoleculeFormats.CanReadMolecule` and `CanWriteMolecule` report facade support for molecular graphs. This intentionally differs from general library format support: SMARTS is read through `SmartsParser` into `QueryMolecule`, so it is not an `IMoleculeReader` codec. Multi-record SDF and vendor readers expose their richer APIs directly; their `IMoleculeReader` implementation returns the first molecular graph.

Reader and writer registrations are scoped to a `MoleculeFormats` instance and can replace a built-in codec for a known `FormatKind`. Single-record streams opened from that facade, writers, batch converters constructed with it, and duplicate-detector text ingestion use the same registrations. SDF stream input continues to use the record-aware `SdfReader` so record diagnostics and boundaries are preserved.

## Public API Examples

### Parsing

```csharp
using molecule;

MoleculeFormats formats = new MoleculeFormats();
FormatOptions options = new FormatOptions();
options.KindHint = FormatKind.Smiles;

Molecule acetamide = formats.ReadString("CC(=O)N", options);
int atomCount = acetamide.Atoms.Count;
```

### Writing

```csharp
using molecule;

MoleculeFormats formats = new MoleculeFormats();
Molecule molecule = formats.ReadStringAs("CCO", FormatKind.Smiles);

string smiles = formats.WriteStringAs(molecule, FormatKind.Smiles);
string sdf = formats.WriteStringAs(molecule, FormatKind.Sdf);
```

### Standardizing

```csharp
using molecule;

MoleculeFormats formats = new MoleculeFormats();
Molecule molecule = formats.ReadStringAs("C(O)=C.[Na+].[Cl-]", FormatKind.Smiles);

StandardizationOptions options = new StandardizationOptions();
options.EnableSaltRemoval = true;
options.EnableMetalDisconnection = true;
options.EnableChargeNormalization = true;
options.EnableReionization = true;
options.EnableIsotopeCleanup = true;
options.EnableRadicalCleanup = true;
options.EnableTautomerEnumeration = true;
options.EnableTautomerCanonicalization = true;
options.EnableStereoCleanup = true;
options.IsotopeCleanup.RemoveAllIsotopes = true;
options.IsotopeCleanup.PreserveHydrogenIsotopes = false;
options.RadicalCleanup.RemoveAllRadicals = true;

Standardizer standardizer = new Standardizer();
StandardizationResult result = standardizer.Standardize(molecule, options);
Molecule parent = result.Molecule;
```

### Deduplicating

```csharp
using molecule;

MoleculeFormats formats = new MoleculeFormats();
IdentityOptions options = new IdentityOptions();
options.Policy = IdentityPolicy.Exact;

DuplicateDetector detector = new DuplicateDetector();
detector.Configure(options);
DuplicateResult first = detector.Add(formats.ReadStringAs("CCO", FormatKind.Smiles));
DuplicateResult second = detector.Add(formats.ReadStringAs("OCC", FormatKind.Smiles));

bool duplicate = second.IsDuplicate;
int firstIndex = second.FirstIndex;
```

### Querying

```csharp
using molecule;

MoleculeFormats formats = new MoleculeFormats();
Molecule target = formats.ReadStringAs("CC(=O)N", FormatKind.Smiles);
QueryMolecule query = new SmartsParser().Parse("[#6]=O");

SubgraphMatcher matcher = new SubgraphMatcher();
SubgraphMatch match = matcher.FindFirst(target, query);
bool found = match.Success;
```

### Fingerprinting

```csharp
using molecule;

MoleculeFormats formats = new MoleculeFormats();
Molecule molecule = formats.ReadStringAs("CC(=O)N", FormatKind.Smiles);

FingerprintOptions options = new FingerprintOptions();
options.BitCount = 1024;
options.CircularRadius = 2;

FingerprintGenerator generator = new FingerprintGenerator();
BitFingerprint fingerprint = generator.CreateCircularFingerprint(molecule, options);
int onBits = 0;
foreach (bool bit in fingerprint.Bits)
{
    if (bit)
    {
        onBits = onBits + 1;
    }
}
```

## Unsupported Edge Cases

The implementation is intentionally scoped to the feature set covered by the local model and tests. Important limitations:

- SMILES and SMARTS are not complete language implementations. Reaction SMILES/SMARTS, CXSMILES/CXSMARTS extensions, recursive SMARTS beyond the local query model, full stereochemical grammar, atom-class semantics beyond stored maps, and every vendor extension are out of scope.
- MOL and SDF support common CTAB atom/bond/property data, but not complete MDL query feature semantics, reactions/RXN, full Sgroup behavior, polymer/repeating-unit semantics, advanced atom lists, or every vendor property block.
- MOL2 support covers practical molecule, atom, bond, charge, and residue data. It does not implement full Tripos force-field typing, scoring semantics, or all optional sections.
- PDB support is for coordinate/connectivity extraction and emission. It does not infer missing bonds from templates, perform protein/nucleic-acid chemistry perception, interpret alternate-location chemistry, or implement mmCIF.
- InChI and InChIKey support is metadata parsing, validation, comparison-layer extraction, and export of stored identifiers. The library does not generate a chemically authoritative InChI or InChIKey from an arbitrary connection table.
- Format readers prefer deterministic diagnostics and partial metadata preservation over exhaustive repair of invalid input.

## No External Chemistry Toolkit

This package does not call RDKit, Open Babel, the IUPAC InChI engine, or other external chemistry libraries. All parsing, writing, graph algorithms, standardization rules, fingerprints, descriptors, and depiction output are implemented in this repository with built-in .NET APIs.

That keeps the library easy to embed and test, but it also means behavior is limited to the simplified chemistry model here. Treat format output and identifiers as library-local unless a test or documented feature explicitly states compatibility with an external standard.
