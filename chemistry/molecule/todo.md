# Universal Molecule Library Development Plan

## Scope And Constraints

- Build a general-purpose chemical molecule handling library in C# using only built-in .NET libraries.
- Follow `syntax.md`: use the allowed classes, enums, interfaces, fields, and methods; keep interfaces method-only and use them at stable service or strategy boundaries; use `public` exclusively for the supported API and `internal` for implementation details; document every public symbol with the permitted XML elements; do not use properties, records, structs, LINQ, attributes, class inheritance, `try/catch`, `out/ref/params`, default parameters, or project-defined generics.
- Prefer deterministic, explicit APIs with block-bodied methods and simple data containers.
- Keep the core independent from file formats so canonicalization, standardization, search, and serialization all operate on the same molecule graph.
- InChI note: exact official InChI/InChIKey generation is normally provided by the official InChI implementation. With no external libraries, this project should support InChI storage, parsing, validation, layered comparison, import/export workflows, and compatibility tests, while any full native InChI generator must be treated as a large separately planned algorithmic milestone.

## Research Baseline

Established toolkits suggest the complete feature surface:

- RDKit: core molecule algorithms, SMILES/SMARTS, MOL/SDF support, 2D/3D operations, descriptors, fingerprints, substructure/similarity search, and molecule standardization APIs.
- Open Babel: broad format conversion, filtering, descriptors, fingerprints, stereochemistry perception/storage, InChI-based duplicate handling, and command-style batch conversion workflows.
- Chemistry Development Kit: valence bond representation, SMILES/SDF/InChI/Mol2/CML readers and writers, ring finding, kekulization, aromaticity, coordinate generation, canonical identifiers, SMARTS search, fingerprints, and QSAR descriptors.
- Indigo Toolkit: molecule/reaction objects, exact and substructure matching, SMARTS, fingerprints, stereochemistry, aromatization, layout/rendering, and format conversion.
- InChI Trust/IUPAC: InChI is layered around formula/connectivity/hydrogen, tautomer, isotope, stereo, and charge information; InChIKey is a fixed-length condensed identifier used for lookup and deduplication.

Reference links:

- https://www.rdkit.org/docs/source/rdkit.Chem.MolStandardize.html
- https://github.com/rdkit/rdkit
- https://openbabel.org/docs/Command-line_tools/babel.html
- https://openbabel.org/docs/Fingerprints/intro.html
- https://open-babel.readthedocs.io/en/latest/Stereochemistry/stereo.html
- https://cdk.github.io/
- https://www.inchi-trust.org/about-the-inchi-standard/

## Feature Inventory, Low Level To High Level

### Foundation

- Element table with atomic number, symbol, names, average mass, monoisotopic mass, common valences, covalent radii, isotope records, and organic subset flags.
- Core enums for bond order, bond direction, aromatic flags, radical state, hybridization, chirality, double-bond configuration, query operation, format kind, standardization policy, and error severity.
- Immutable-by-convention IDs for atoms, bonds, conformers, residues, and property records.
- Molecule metadata bag for names, comments, source format, vendor fields, SDF data fields, PDB records, and arbitrary string values.
- Error and warning model with code, message, location, atom ID, bond ID, and severity.
- Deterministic text writer utilities for invariant culture number formatting and stable ordering.
- Tokenizer and cursor utilities for character formats.
- Simple collection helpers that avoid LINQ.

### Molecular Structure Model

- Atom model: element, isotope, formal charge, implicit hydrogen count, explicit hydrogen count, radical electrons, atom map number, aromatic flag, query fields, coordinates, alias, and label.
- Bond model: begin atom, end atom, order, aromatic flag, dative/coordination flag, stereo direction, query fields, and source flags.
- Molecule graph with atom and bond lists, adjacency cache, component cache, ring cache, stereo records, conformers, residues, and metadata.
- Mutable molecule builder with add/remove atoms, add/remove bonds, replace atoms, merge molecules, split components, clone, and renumber.
- Read-only molecule view methods for atom count, bond count, neighbor lookup, bond lookup, degree, valence, ring membership, component membership, and metadata lookup.
- Explicit and implicit hydrogen handling, including add hydrogens, remove hydrogens, preserve isotopic hydrogens, and detect hydrogen-suppressed formats.
- Fragment and component model for salts, mixtures, disconnected structures, polymers, residues, and multi-model PDB structures.
- Coordinate model for 0D, 2D, and 3D conformers with occupancy, temperature factor, alternate location, residue index, and model index.

### Chemical Perception

- Periodic valence rules and allowed valence tables.
- Valence calculator with formal charge, radicals, aromatic bonds, explicit hydrogens, implicit hydrogens, dative bonds, and hypervalent exceptions.
- Atom typing for organic subset, aromatic candidates, donors/acceptors, hetero atoms, metals, halogens, and common functional groups.
- Ring finding with SSSR-like cycle basis, all small rings up to policy limits, fused ring systems, bridged systems, and ring membership flags.
- Kekulization and dekekulization for aromatic systems.
- Aromaticity perception with configurable model, defaulting to a Daylight/RDKit-like organic model.
- Conjugation and pi-electron perception.
- Hybridization assignment.
- Bond order perception from coordinates when possible.
- Cleanup of impossible valence states with diagnostics rather than silent mutation.

### Stereochemistry

- Tetrahedral atom stereo storage, perception from wedges/hash bonds, perception from 3D coordinates, inversion, removal, and unspecified stereo markers.
- Double-bond E/Z stereo storage, perception from slash/backslash SMILES bonds, 2D coordinates, and 3D coordinates.
- Extended stereo placeholders for allene/atropisomer/future non-tetrahedral support.
- Stereo group support for absolute, relative, racemic, and enhanced stereo group semantics from V3000/SDF.
- CIP-like priority ranking plan for R/S and E/Z labels using deterministic graph comparison.
- Stereo validation for duplicate ligands, missing neighbors, invalid wedge placement, impossible E/Z, and stereo lost during transformation.
- Stereo-preserving canonicalization and stereo-stripping parent generation.

### Canonicalization And Identifiers

- Deterministic atom ranking using element, isotope, charge, valence, hydrogens, degree, ring state, aromaticity, bond environments, stereo, and iterative refinement.
- Canonical atom ordering independent of input atom order.
- Canonical bond ordering.
- Canonical SMILES writer with configurable inclusion of isotope, charge, hydrogens, atom maps, aromaticity, and stereochemistry.
- Library-specific deterministic serialization for round-trip tests and stable snapshots.
- Structural hash for exact graph identity.
- Connectivity-only hash.
- Stereo-aware and stereo-insensitive hashes.
- Charge/protonation-insensitive parent hash.
- Tautomer-insensitive parent hash.
- Mixture/component sorted identifiers.
- Duplicate detection service based on selected identity policy.
- Identifier comparison utilities for exact, connectivity, stereo parent, fragment parent, tautomer parent, and charge parent equivalence.
- InChI string parser for layers, prefix/version, formula layer, connectivity layer, hydrogen/tautomer layer, charge/proton layer, stereochemistry layers, isotope layers, and fixed-H markers.
- InChIKey format validation and comparison by first block, second block, version, and protonation indicator.

### Format Conversion

- Common `IMoleculeReader` and `IMoleculeWriter` abstractions for single-molecule text codecs, with an injectable `IMoleculeFormatService` facade and concrete registry storage.
- Format detection by extension, magic/header, first-line patterns, and caller hints.
- Text cursor with line/column diagnostics.
- SMILES reader for atoms, bonds, branches, ring closures, aromatic atoms, bracket atoms, charges, isotope, hydrogens, atom maps, disconnected components, and stereo markers.
- SMILES writer for canonical, noncanonical, kekule, aromatic, isomeric, atom-map, and explicit-hydrogen modes.
- SMARTS reader for atom primitives, bond primitives, recursive SMARTS placeholder policy, logical operators, ring/degree/valence/aromatic queries, atom lists, negation, and component grouping.
- SMARTS matcher for substructure queries, atom/bond predicates, backtracking, ring constraints, recursive query staging, and match deduplication.
- MOL V2000 reader/writer with atom block, bond block, charge records, isotope records, radicals, atom lists, aliases, coordinates, and common M records.
- MOL V3000 reader/writer with collections, enhanced stereo, atom/bond properties, and large molecule support.
- SDF reader/writer with multi-record streaming, data fields, molecule titles, record separators, error recovery policy, and per-record diagnostics.
- MOL2 reader/writer with molecule, atom, bond, substructure, charge, atom type, residue, and partial-charge fields.
- PDB reader/writer with ATOM/HETATM, CONECT, MODEL/ENDMDL, residues, chains, alternate locations, occupancy, B-factor, insertion code, element/charge fields, and ligand extraction.
- InChI workflow reader/writer for storing InChI/InChIKey metadata, validating syntax, comparing layers, and connecting to internally generated parent IDs.
- Vendor format framework for simple delimited records, vendor field preservation, per-format option objects, and round-trip metadata.
- Batch conversion API with streaming input, streaming output, filter hooks expressed as concrete classes, progress counters, and conversion reports.

### Standardization And Deduplication

- Standardization pipeline with explicit ordered steps and policy object.
- Salt and solvent removal using curated built-in fragment patterns.
- Largest fragment chooser with heavy atom count, molecular weight, organic preference, and tie-breaking by canonical ID.
- Charge normalization with built-in transform table and deterministic application order.
- Reionization policy with acid/base priority table.
- Metal and organometallic disconnection policy.
- Nitro, sulfoxide, phosphate, carboxylate, azide, diazo, and common zwitterion normalizations.
- Isotope cleanup policy.
- Radical cleanup and validation policy.
- Explicit hydrogen normalization.
- Aromaticity normalization and kekule fallback.
- Tautomer transform table, tautomer enumeration limits, canonical tautomer scoring, and tautomer parent generation.
- Stereo cleanup after normalization and tautomerization.
- Fragment parent, charge parent, stereo parent, isotope parent, tautomer parent, and super parent generation.
- Duplicate detector over molecules, streams, and SDF records using selected parent policy.
- Standardization report listing mutations, removed fragments, parent identifiers, warnings, and failures.

### Search, Similarity, And Descriptors

- Exact graph isomorphism.
- Subgraph isomorphism for SMARTS.
- Maximum common substructure staging plan for later implementation.
- Fingerprints: path-based, circular/ECFP-like, pattern/substructure, MACCS-like built-in key set, count variants, folded bit vectors, and sparse integer vectors.
- Similarity metrics: Tanimoto, Dice, Cosine, Tversky.
- Screening workflow using fingerprints before exact SMARTS matching.
- Formula calculator with Hill ordering and isotope-aware formula.
- Molecular weight, exact mass, nominal mass, heavy atom count, hetero atom count, ring count, aromatic ring count, rotatable bond count, hydrogen bond donors/acceptors, formal charge, TPSA approximation, logP/MR placeholder policy, and fraction sp3.
- Descriptor registry using concrete classes and string names.

### Coordinates, Layout, And Geometry

- 2D coordinate storage and normalization.
- Basic 2D layout for acyclic graphs, rings, fused rings, and disconnected components.
- Wedge/hash assignment from stereo.
- 3D coordinate storage, distance/angle/torsion measurement, centroid, bounding box, RMSD utility, and conformer alignment staging.
- PDB residue and chain utilities.
- Optional SVG depiction writer using only `System.Text` and geometry primitives.

### Validation And Quality

- Molecule validation for valence, charge range, disconnected query issues, aromaticity consistency, duplicate bonds, self bonds, invalid stereo, impossible coordinates, empty molecule policy, unsupported element, and parser-specific warnings.
- Format round-trip tests with golden strings.
- Canonicalization invariance tests under atom renumbering.
- Standardization regression tests for curated examples.
- SMARTS conformance examples from public behavior where specs are stable enough.
- Fuzz-style parser tests using deterministic random input generation.
- Performance budgets for large SDF streaming, canonicalization, ring finding, and SMARTS matching.
- Compatibility corpus folders for SMILES, MOL/SDF, MOL2, PDB, standardization, and InChI workflows.

## Public API Completion Tasks

- [DONE] T001: Complete the built-in element, isotope, atomic-mass, covalent-radius, and common-valence tables for all 118 elements from the reference data under `specs/`, including deterministic behavior for elements without a standard atomic weight.
- [DONE] T002: Add public graph-inspection operations for atom and bond counts, atom and bond lookup, neighbors, degree, valence, ring and component membership, conformers, stereo records, and metadata without requiring callers to mutate the public storage lists.
- [DONE] T003: Extend the public molecule editor so callers can initialize it from an existing molecule and safely clone, replace, merge, split, remove, and renumber graphs while preserving coordinates, conformers, stereo records, residues, chains, metadata, and cache consistency.
- [DONE] T004: Extend the public molecular data model for format-independent polymer and multi-model structures and for MOL2/PDB atom data including atom type, partial charge, residue identity, model identity, occupancy, temperature factor, alternate location, and insertion code.
- [DONE] T005: Add a public hydrogen operation service that calculates, adds, removes, and normalizes explicit and implicit hydrogens while preserving isotopic hydrogens and reporting invalid valence changes.
- [DONE] T006: Add configurable public ring-finding options and results for a deterministic cycle basis, all small rings within caller limits, fused systems, bridged systems, and atom and bond membership.
- [DONE] T007: Add public dekekulization and configurable aromaticity-model operations, including deterministic diagnostics and fallback behavior for systems that cannot be assigned a valid aromatic or Kekulé form.
- [DONE] T008: Add public bond-order perception from 2D or 3D coordinates with conservative confidence data and diagnostics for ambiguous, hypervalent, aromatic, and metal-coordination cases.
- [DONE] T009: Add public stereo editing operations for creating, inverting, removing, and marking unspecified tetrahedral and double-bond stereo, plus typed placeholders for allene, atropisomeric, and future non-tetrahedral stereo.
- [DONE] T010: Complete canonical identity options for independently controlling isotope, charge, hydrogen, atom-map, aromaticity, and stereo participation, and expose deterministic component-sorted identifiers for mixtures.
- [DONE] T011: Add format-specific option data and uniquely named public codec operations for canonical, noncanonical, aromatic, Kekulé, isomeric, atom-map, explicit-hydrogen, recovery, and record-stream policies; use `ReadOnlySpan<char>` entry points where non-owning in-memory parsing is appropriate.
- [DONE] T012: Complete the public SMARTS query model, parser, and matcher for nested logical expressions, recursive SMARTS, negation precedence, atom and bond primitives, ring constraints, and component grouping with deterministic match deduplication.
- [DONE] T013: Complete MOL V2000, MOL V3000, and SDF public options and result models for atom lists, aliases, common M records, enhanced stereo collections, large structures, mixed-version streams, data fields, and configurable per-record error recovery.
- [DONE] T014: Replace the empty MOL2 writer marker contract with an independently useful public writer operation and complete MOL2 round trips for molecule, atom, bond, substructure, residue, atom-type, charge-type, and partial-charge fields.
- [DONE] T015: Replace the empty PDB writer marker contract with an independently useful public writer operation and complete PDB multi-model, residue, chain, alternate-location, occupancy, B-factor, insertion-code, element, charge, connectivity, and ligand-extraction workflows.
- [DONE] T016: Add a public delimited-vendor writer with explicit vendor options, quoted-field handling, selected embedded molecule formats, caller-defined columns, and lossless vendor metadata round trips.
- [DONE] T017: Add public batch-conversion filter and progress contracts using ordinary interfaces and data classes, with deterministic skipped, read, written, warning, and failure counts and no delegate dependency.
- [DONE] T018: Complete the standardization options, pipeline, and mutation report for explicit-hydrogen normalization, aromaticity normalization, Kekulé fallback, nitro, sulfoxide, phosphate, carboxylate, azide, diazo, and common zwitterion rules in an explicit deterministic order.
- [DONE] T019: Add isotope-parent generation and complete the public parent and identity-policy surface for fragment, charge, stereo, isotope, tautomer, and super parents.
- [DONE] T020: Add a bounded public maximum-common-substructure service with query options, deterministic atom and bond mappings, timeout or work-limit diagnostics, and no external library dependency.
- [DONE] T021: Expose distinct path, circular, pattern, and built-in MACCS-like fingerprint families, their bit, count, folded, and sparse forms, and a public fingerprint-screening workflow before exact substructure matching.
- [DONE] T022: Extend the public similarity service so Tanimoto, Dice, Cosine, and Tversky metrics operate consistently on bit, count, and sparse fingerprints with defined behavior for empty and differently sized inputs.
- [DONE] T023: Complete the descriptor surface with built-in logP and molar-refractivity estimates or explicit unavailable-result diagnostics, while retaining deterministic formula, mass, count, ring, donor/acceptor, TPSA, charge, and fraction-sp3 results.
- [DONE] T024: Add public 2D coordinate normalization and deterministic conformer alignment operations, including translation, rotation, scale policy, atom mapping, RMSD, and failure diagnostics.
- [DONE] T025: Complete public molecule validation for aromaticity consistency, disconnected query constraints, invalid stereo depiction, impossible or non-finite coordinates, conformer size mismatches, unsupported elements, parser-originated warnings, and configurable empty-molecule policy.
- [DONE] T026: Add complete public-behavior tests for element and isotope lookup, graph data containers, metadata and diagnostics, graph inspection and editing, conformers, residues, chains, components, hydrogen operations, and all success, null, invalid-ID, and copy-versus-mutation paths.
- [DONE] T027: Add complete public-behavior and conformance tests for valence, atom typing, rings, kekulization and dekekulization, aromaticity, conjugation, hybridization, bond-order perception, stereo perception/editing/validation, CIP ranking, canonical order, serialization, identity hashes, and mixture identifiers.
- [DONE] T028: Add complete specification-based parse, write, detection, option, diagnostic, recovery, streaming, and round-trip tests for SMILES, SMARTS, MOL V2000, MOL V3000, SDF, MOL2, PDB, InChI, InChIKey, deterministic text, registered codecs, and delimited vendor formats.
- [DONE] T029: Add complete public-behavior tests for every standardization stage and ordered combination, salt and fragment selection, charge normalization, reionization, metal disconnection, isotope and radical cleanup, tautomer enumeration and selection, stereo cleanup, every parent policy, and duplicate detection over objects and streams.
- [DONE] T030: Add complete public-behavior tests for graph and subgraph isomorphism, recursive SMARTS, maximum common substructure, every fingerprint representation and screening path, every similarity metric, formula and mass calculations, descriptor registry lookups, and unavailable descriptor results.
- [DONE] T031: Add complete public-behavior tests for coordinate normalization, layout, stereo depiction assignment, geometry, conformer alignment, SVG output, PDB residue/chain/ligand utilities, molecule validation, and all remaining public constructors and entry points, covering every documented result and error branch.
