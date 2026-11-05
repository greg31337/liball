create development plan for universal chemical molecules handling library, and put it in todo.md
create a list of necessary features, from low level to high
the scope of the library should include:
- Molecular structure model and canonicalization: Atoms, bonds, aromaticity, stereochemistry, valence, canonical identifiers and deterministic serialization.
- Chemical formats conversion: SMILES, SMARTS, SDF, MOL, MOL2, PDB, InChI-related workflows and vendor formats.
- Structure standardization and deduplication: Salt removal, charge normalization, tautomer policies, stereochemistry cleanup and duplicate detection.
research existing libraries to figure out a set of complete features
list all features in todo.md
document the current public API groups in todo.md, without fine details
list planned source files in todo.md, arranging them by groups in folders under src
list tasks that would implement the features in todo.md, in line format: "- [OPEN] T001: Task description."
take into account simplified C# syntax to be used for implementation described in syntax.md
do not use any external libraries, only built-in .NET ones

Current source policy:
- Treat `public` declarations as the supported application API and use `internal` for implementation details.
- Add XML documentation to public types, constructors, methods, fields, enums, and enum members only, using the elements allowed by `syntax.md`.
