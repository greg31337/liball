# Specification Sources

The repository previously contained no `specs/` directory, so the available
local specification set was not sufficient for the library goals. The files
below were retrieved on 2026-08-25 from authoritative publishers.

| Local file | Source | Purpose |
| --- | --- | --- |
| `bipm/SI-Brochure-9-EN-v4.01-2026.pdf` | https://www.bipm.org/documents/20126/41483022/SI-Brochure-9-EN.pdf | Current English SI definition, base and derived units, prefixes, non-SI units, and writing rules. |
| `ucum/ucum-v2.2.html` | https://ucum.org/ucum | UCUM 2.2 grammar, semantics, prefixes, unit atoms, and non-ratio conversion rules. |
| `ucum/ucum-essence-v2.2.xml` | https://raw.githubusercontent.com/ucum-org/ucum/v2.2/ucum-essence.xml | Machine-readable UCUM 2.2 prefixes and unit definitions. |
| `ucum/LICENSE-v2.2.md` | https://raw.githubusercontent.com/ucum-org/ucum/v2.2/LICENSE.md | License accompanying the downloaded UCUM material. |
| `nist/NIST-SP-811-2008.pdf` | https://doi.org/10.6028/NIST.SP.811e2008 | SI usage, formatting conventions, and conversion-factor reference; use the newer SI Brochure for post-2019 SI definitions. |
| `nist/NIST-HB-44-2026-Appendix-B.pdf` | https://www.nist.gov/document/2026-nist-handbook-44-appendix-b | Current descriptions of SI, British Imperial, and US customary measurement systems. |
| `nist/NIST-HB-44-2026-Appendix-C.pdf` | https://www.nist.gov/document/2026-nist-handbook-44-appendix-c | Current US customary and metric unit tables and conversion factors. |
| `nist/CODATA-2022-constants.txt` | https://physics.nist.gov/cuu/Constants/Table/allascii.txt | Latest published CODATA recommended physical-constant values available at retrieval time. |
| `qudt/QUDT-schema-v3.1.11-2026-02.html` | https://qudt.org/doc/2026/02/DOC_SCHEMA-QUDT.html | Quantity, unit, dimension-vector, system-of-units, and physical-constant metadata model. |
| `research/Pint-documentation.html` | https://pint.readthedocs.io/en/latest/index.html | Registry, quantity arithmetic, conversion, context, parsing, and formatting feature reference. |
| `research/UnitsNet-package-description.html` | https://www.nuget.org/packages/UnitsNet | Typed .NET quantity, conversion, parsing, formatting, and extensibility feature reference. |
| `research/JSR-385-Unit-API.html` | https://unitsofmeasurement.github.io/unit-api/site/apidocs/javax/measure/Unit.html | Standardized unit operations, compatibility, converters, prefixes, powers, roots, and alternate-system behavior. |
| `research/Boost.Units-quantities.html` | https://www.boost.org/doc/libs/latest/doc/html/boost_units/Quantities.html | Compile-time dimensional-analysis and explicit quantity-conversion design reference. |
| `research/Astropy-units.html` | https://docs.astropy.org/en/stable/units/index.html | Unit composition/decomposition, equivalencies, systems, custom units, and formatting feature reference. |
| `research/unyt-usage.html` | https://unyt.readthedocs.io/en/stable/usage.html | Quantity arithmetic, unit systems, constants, custom units, and automatic conversion feature reference. |
| `research/FSharp-units-of-measure.html` | https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/units-of-measure | .NET compile-time units-of-measure comparison point for the optional typed facade. |

The SI Brochure is the primary authority where its current definitions or
style rules conflict with the older NIST SP 811. UCUM governs only behavior
that the library explicitly identifies as UCUM-compatible; the feature plan
does not claim complete UCUM conformance.

No IETF RFC directly defines the unit algebra or measurement metadata in scope,
so no RFC was added merely for completeness.
