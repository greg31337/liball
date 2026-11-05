# Universal Units And Physical Quantities Development Plan

## Goals

Build a universal units and physical quantities library for .NET using only built-in .NET APIs. The library should support reliable dimensional analysis, conversion, parsing, formatting, metadata lookup, and higher-level quantity workflows while staying inside the simplified C# subset described in `syntax.md`.

The implementation must avoid external runtime libraries. It must also avoid source syntax that `syntax.md` disallows: structs, records, properties, operator overloads, extension methods, generic type or method declarations, default parameters, `out`/`ref`, LINQ, attributes, `try`/`catch`, and other unsupported forms.

## Research Baseline

- Pint: registry-centered quantities, editable unit definitions, arithmetic, conversion, prefixes, formatting, temperature handling, contexts, systems, serialization, measurements, and dimensional checks. Source: https://pint.readthedocs.io/en/latest/index.html
- UnitsNet: strongly typed quantity classes, static factories, unit conversion, parsing, abbreviations, culture-aware formatting, runtime quantity/unit metadata, custom units, dynamic conversion helpers, serialization guidance, and explicit precision caveats. Source: https://www.nuget.org/packages/UnitsNet
- JSR 385 Unit API: separation of quantities, units, converters, dimensions, unit systems, formatting/parsing, compatibility checks, equivalence checks, immutable unit instances, system units, offset units, prefixes, powers, roots, and service-oriented catalogs. Source: https://unitsofmeasurement.github.io/unit-api/site/apidocs/javax/measure/Unit.html
- Boost.Units: dimensional analysis as a safety primitive, quantity value plus unit pairing, explicit construction and conversion, conversion factors and affine offsets, heterogeneous unit operations, and emphasis on catching unit-system mismatches. Source: https://boost.org.cpp.al/doc/libs/1_59_0/doc/html/boost_units/Quantities.html
- Astropy units: quantity arithmetic, decomposition to base units, composition into named derived units, conversion across systems, dimensionless units, logarithmic units, equivalencies such as wavelength/frequency, custom units, and rich string formatting. Source: https://docs.astropy.org/en/stable/units/index.html
- unyt: scalar and array quantities, predefined physical constants, automatic conversion during arithmetic, unit systems, custom units, and arbitrary numeric value types. Source: https://unyt.readthedocs.io/en/stable/usage.html
- UCUM: ASCII unit grammar, prefixes, unit atoms, infinite compound expressions, unambiguous machine communication, canonical forms, commensurability, equality, special non-ratio conversions, and healthcare/business interoperability. Source: https://ucum.org/ucum
- QUDT: semantic model for quantity kinds, units, dimensions, systems, conversion factors, and machine-processable metadata. Source: https://www.qudt.org/doc/2026/02/DOC_SCHEMA-QUDT.html
- F# Units of Measure: compile-time unit checking with zero runtime representation cost, useful as inspiration for typed facades, but this C# subset cannot reproduce generic compile-time measure syntax. Source: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/units-of-measure

## Critical Must-Have Feature Set

- Dimension vectors over canonical base dimensions.
- Exact scale factors represented without avoidable floating-point drift.
- Unit definitions that map names, symbols, dimensions, scale, offset, and metadata.
- SI base units and common SI derived units.
- Metric prefixes with exact decimal scale factors.
- Dimension compatibility checks before conversion and arithmetic.
- Multiplicative conversions through canonical system units.
- Affine conversions for temperature and other shifted scales.
- Special conversion pipeline for non-simple conversions, initially limited and explicit.
- Quantity values as value plus unit.
- Addition and subtraction for compatible quantities.
- Multiplication and division that derive compound dimensions and units.
- Integer powers and compatible integer roots.
- Dimensionless quantities and units.
- Unit expression parsing for symbols such as `m/s`, `kg*m/s^2`, `m2`, `m^2`, and `(N*m)`.
- Quantity parsing from strings such as `9.81 m/s^2`.
- Unit formatting in compact, long-name, and canonical styles.
- Quantity formatting with culture-aware numeric text.
- Unit and quantity metadata lookup at runtime.
- Unit systems such as SI, CGS, Imperial, and US customary as named catalogs.
- Preferred units per quantity kind and unit system.
- Common physical constants with units.
- Custom unit registration without changing source code.
- Registry snapshots or freezing to make read use predictable.
- Ambiguity handling for duplicate symbols.
- Error results for parse and convert operations that do not require `out`, exceptions, or `try`/`catch`.
- Serialization-friendly DTO methods based on built-in strings, arrays, and dictionaries.
- A focused typed facade layer for common quantities.
- High test coverage for dimensions, conversions, parsing, formatting, and public behavior.

## Feature Plan From Low Level To High Level

### F00 Numeric Foundation

- Represent rational scale factors with normalized arbitrary-range numerators and denominators.
- Preserve exact decimal definitions and convert to `double` only at numeric boundaries.
- Provide comparison and arithmetic helpers without operator overloads.

### F01 Dimensions

- Represent fixed exponent slots for length, mass, time, electric current, thermodynamic temperature, amount of substance, luminous intensity, plane angle, solid angle, information, currency, and count.
- Support multiply, divide, integer power, compatible integer root, equality, dimensionless checks, and stable keys.

### F02 Unit Core

- Represent immutable-by-convention unit identity, names, symbols, aliases, dimensions, exact scale and offset, prefixability, and conversion kind.
- Support base, scaled, affine, alternate, special, and compound units.
- Support unit multiply, divide, inverse, integer power, integer root, compatibility, and equivalence.
- Keep quantity kind distinct from dimension.

### F03 Prefixes

- Define exact SI prefixes from quetta through quecto.
- Apply prefixes only to prefixable scale units and prefer a full registered symbol over a prefixed interpretation.
- Recognize prefix symbols and names while preserving ambiguity information.

### F04 Registry And Catalogs

- Let a registry own typed units, aliases, prefixes, quantity kinds, physical constants, equivalencies, systems, and parser context.
- Support custom registration before freeze, typed lookup and enumeration, duplicate detection, validation, and deterministic frozen reads.

### F05 Conversion

- Convert compatible scale and affine units through system units.
- Support a limited explicit special-conversion pipeline.
- Return cacheable conversion plans and clear incompatible, ambiguous, or unsupported failures.

### F06 Quantity Core

- Construct quantities from a value plus a unit instance, id, symbol, or expression.
- Expose conversion, compatibility, and dimensionless checks.
- Support add, subtract, multiply, divide, scale, negate, absolute value, integer power, and compatible integer root.
- Preserve the left unit for add and subtract unless a target is requested, and reject misleading affine arithmetic.

### F07 Unit Expression Parser

- Parse symbols, multiplication, division, parentheses, integer powers, and registered prefixes.
- Offer a documented UCUM-inspired canonical subset without claiming full UCUM conformance.
- Return result objects with useful errors and ambiguity candidates, while retaining direct throwing entry points for programmer-controlled input.

### F08 Quantity Parser

- Parse culture-aware numeric values followed by unit expressions in strict and relaxed modes.
- Narrow ambiguous units by quantity kind or unit system and return result objects.

### F09 Formatting

- Format symbols, names, canonical expressions, and decomposed base expressions deterministically.
- Format quantity numbers with a selected culture and pluralize names from metadata.
- Keep stable machine interchange separate from human display.

### F10 Unit Systems

- Define SI, CGS, Imperial, and US customary catalogs with resolvable base and preferred units.
- Convert to preferred units, decompose into system base units, and compose equivalent base expressions into known named derived units.

### F11 Quantity Kinds

- Model quantity-kind identity, aliases, dimension, default unit, and accepted units.
- Include length, mass, time, area, volume, speed, acceleration, force, pressure, energy, power, frequency, angle, angular speed, temperature, temperature interval, electric charge, current, voltage, resistance, capacitance, luminous intensity, amount, density, flow rate, data size, data rate, ratio, count, and currency.
- Support multiple kinds with equal dimensions and lookup by identity or applicable unit.

### F12 Typed Facades

- Provide typed facades for length, mass, duration, area, volume, speed, acceleration, force, pressure, energy, power, frequency, angle, temperature, temperature interval, data size, and data rate.
- Provide construction, named conversions, meaningful typed arithmetic, and bridges in both directions between facades and dynamic quantities.

### F13 Special And Contextual Conversions

- Keep temperature points and intervals separate and support Kelvin, Celsius, Fahrenheit, and Rankine correctly.
- Support opt-in wavelength/frequency, mass/energy, and molar-mass contexts.
- Reserve logarithmic-unit support until the core is stable.

### F14 Constants

- Include exact defining constants and useful measured physical constants with quantity, symbol, source, adjustment/version, uncertainty or exactness, and aliases.

### F15 Serialization And Interchange

- Serialize value, canonical unit expression, and optional quantity kind through built-in strings and DTOs.
- Provide non-throwing deserialization results and deterministic round trips for standard, custom, prefixed, and compound units.

### F16 Validation, Errors, And Diagnostics

- Use typed result classes for expected parsing, lookup, conversion, and deserialization failures.
- Reserve exceptions for invalid direct-method use and expose stable error codes, messages, candidates, and registry validation reports.

### F17 Performance And Reliability

- Use built-in arrays, dictionaries, spans where appropriate, and deterministic immutable-by-convention definitions.
- Avoid LINQ and hidden mutable global state.
- Permit internal caching without changing observable results.

### F18 Documentation And Examples

- Describe supported syntax, conversions, custom registration, ambiguity, precision, unsupported behavior, and common workflows.

## Implementation Tasks

- [DONE] T001: Complete `StandardUnits` and `IStandardUnits` with every SI, temperature-point, temperature-interval, dimensionless, CGS, Imperial, US customary, data, and compound unit referenced by the standard quantity kinds, typed facades, and unit systems, using exact factors from `specs/` and distinct Imperial and US volume definitions.
- [DONE] T002: Make `StandardRegistry.Create()` resolve every default, accepted, base, and preferred unit id and return a frozen registry whose validation report is valid.
- [DONE] T003: Extend the public exact-number construction path with built-in `System.Numerics.BigInteger` or invariant decimal-text input so callers can register arbitrary-range exact scales and offsets, including quetta and quecto factors, without an early `double` conversion.
- [DONE] T004: Replace the `object` parameters, arrays, and values in `IUnitRegistry`, `UnitRegistry`, `ILookupResultFactory`, and quantity-kind lookup results with `QuantityKindDefinition`, `UnitSystemDefinition`, and `PhysicalConstantDefinition` types, and reject duplicate ids and invalid metadata relationships in every catalog.
- [DONE] T005: Add typed public lookup and enumeration operations for unit systems and physical constants, plus quantity-kind lookup by applicable unit, while preserving explicit not-found and ambiguous outcomes.
- [DONE] T006: Promote unit and quantity parse-result contracts and classes into `src/Api`, add non-throwing parse methods to both parser interfaces and implementations, and carry stable error codes, messages, source positions, and ambiguity candidates without removing the direct throwing methods.
- [DONE] T007: Extend parse options and parser behavior so quantity-kind and unit-system context can narrow ambiguous unit text, and ensure the compact grammar consistently handles symbols, aliases, `*`, `/`, parentheses, signed integer powers, suffix powers, prefixes, whitespace policy, and the declared UCUM-inspired canonical subset.
- [DONE] T008: Add a public quantity-construction service that resolves a value plus unit id, symbol, or expression through `IUnitCatalog`, optionally validates a quantity kind, and provides both result-returning and direct throwing workflows.
- [DONE] T009: Implement the public special-unit conversion path represented by `UnitConversionKind.Special`, with explicit built-in strategy metadata and conversion-plan/result integration that remains separate from opt-in cross-dimension equivalencies.
- [DONE] T010: Extend quantity arithmetic with add and subtract operations that accept an explicit target unit and return a typed failure for incompatible dimensions or invalid affine point/interval combinations.
- [DONE] T011: Extend `IUnitSystemConverter` and `UnitSystemConverter` to convert a `QuantityValue` to a preferred unit, decompose it into the selected system's resolvable base-unit expression, and compose an equivalent expression into a registered named derived unit.
- [DONE] T012: Add reverse bridges from `QuantityValue` to every typed facade and meaningful named cross-facade operations such as length divided by duration, speed multiplied by duration, mass multiplied by acceleration, and length-derived area or volume, with dimension and affine validation.
- [DONE] T013: Add the planned opt-in molar-mass contextual conversion model and converter workflow so callers supply the required molar-mass context explicitly rather than enabling an implicit global conversion.
- [DONE] T014: Extend physical-constant metadata with source label, adjustment or edition, and uncertainty data, then align the standard exact constants with the current SI Brochure and the measured constants with the downloaded CODATA 2022 table.
- [DONE] T015: Define one stable canonical interchange grammar for unit expressions, make formatter output parseable by that grammar, and add non-throwing canonical text and DTO deserialization results without introducing a runtime dependency outside built-in .NET APIs.
- [DONE] T016: Expand registry validation to cover missing accepted units, unresolved system base and preferred units, incompatible preferred-unit mappings, duplicate metadata ids and aliases, invalid equivalencies, special-conversion metadata, and physical constants with incompatible or incomplete metadata.
- [DONE] T017: Add specification-driven tests for every standard prefix and unit definition, exact scale and offset, affine temperature point and interval, Imperial-versus-US distinction, standard quantity-kind mapping, and resolvability of every standard registry reference.
- [DONE] T018: Add complete success, boundary, invalid-input, defensive-copy, and failure-path tests for `RationalNumber`, dimension vectors and roots, unit definitions and roots, prefix definitions, conversion plans, and all related result types.
- [DONE] T019: Add complete public-behavior tests for typed registry registration, freeze behavior, lookup and ambiguity, catalog enumeration, metadata validation, custom units, custom prefixes, systems, constants, equivalencies, and independent standard-registry instances.
- [DONE] T020: Add complete conversion tests for scale, affine, explicit special, unsupported, and incompatible paths; conversion-plan reuse semantics; preferred-system conversion; base decomposition; named-unit composition; and every contextual equivalency in both directions.
- [DONE] T021: Add complete unit and quantity parser tests for every grammar form, culture, strict and relaxed modes, quantity-kind and unit-system narrowing, full-symbol precedence over prefixes, unknown input, malformed input, source positions, candidate arrays, and parity between result-returning and throwing entry points.
- [DONE] T022: Add complete dynamic quantity tests for construction by instance and text, conversion, compatibility, dimensionless behavior, all arithmetic operations, explicit target units, integer powers and roots, division by zero behavior, and invalid affine or dimension combinations.
- [DONE] T023: Add complete unit-system and quantity-kind tests for all standard definitions, same-dimension distinct kinds, lookup by applicable unit, preferred-unit selection, unresolved mappings, and custom systems and kinds.
- [DONE] T024: Add complete formatting and serialization tests for every style and option, pluralization, cultures, deterministic compound ordering, canonical round trips for standard and custom units, DTO quantity-kind validation, and all non-throwing failure results.
- [DONE] T025: Add complete tests for every typed facade constructor, named conversion, same-type arithmetic operation, dynamic bridge in both directions, cross-facade operation, invalid dimension, null input, and temperature point-versus-interval rule.
- [DONE] T026: Audit all public constructors and interface methods under `src/Api` and add behavioral tests for any remaining success, failure, null, empty, invalid-enum, and returned-array isolation paths so every exposed contract has complete functional coverage.
