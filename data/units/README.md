# units

Initial scaffold created on 2026‑08‑01.

## Architecture

The library is centered on dynamic runtime unit definitions. A `UnitRegistry`
owns units, aliases, quantity kinds, constants, systems, and parser metadata so
applications can look up, parse, convert, and format units from data known only
at runtime. Quantities pair a numeric value with a registered unit, and
dimension checks happen through runtime dimension vectors before conversion or
arithmetic is accepted.

Read-only consumers depend on `IUnitCatalog`, the narrow lookup surface
implemented by `UnitRegistry`. Applications can therefore expose a registry
without exposing mutation, compose metadata from another source, or substitute
a small catalog in tests. Registration APIs continue to require `UnitRegistry`
because they intentionally mutate it. `UnitParser` implements `IUnitParser`,
and `QuantityParser` accepts that interface when callers need to replace the
unit-expression parser. Parse state is isolated to each call, so a parser
instance can be safely reused by independent operations.

Typed facade classes are optional convenience APIs over the dynamic engine.
Classes such as `Length`, `Mass`, and `Temperature` accept a value and unit in
their constructor and expose named conversion methods while preserving a bridge
to the underlying dynamic quantity model. They are not the core representation
and do not replace registry-driven custom units or runtime parsing.

## Public API Policy

The supported API is the set of `public` types under `src/Api`. Interfaces,
interface-backed implementation classes, data carriers, static data, and enums
are separated into category directories. Implementation details are explicitly
`internal`; this includes tokenizer types, object storage, validation helpers,
and convenience factories used inside the assembly. `LookupResultFactory`
allows custom `IUnitCatalog` implementations to create valid success, missing,
and ambiguous results without exposing invalid result constructors. The current
parser entry points use the explicit `ParseOrThrow` name and return parsed
values directly; parser-only token and intermediate result models remain
internal.

Every public type, constructor, method, field, enum, and enum member has XML
documentation. That generated reference documentation is the authoritative
member-level guide. Public array-returning metadata methods return copies unless
their XML documentation says otherwise, and registry mutation methods return an
`OperationResult` so expected registration failures do not need exception
handling.

## Nullability Policy

Nullable reference analysis is enabled for the library and its tests. A `?` is
part of the API contract: lookup methods that can miss return nullable
definitions, optional metadata accepts nullable input and is normalized to an
empty value where documented, and comparison-style predicates accept null when
their defined result is `false`. Required inputs remain non-nullable and retain
runtime argument validation for callers that bypass nullable analysis.

Failed result objects store nullable payload state internally. Their public
payload getters remain non-nullable because they either return a successful
payload or throw `InvalidOperationException`; failed results no longer allocate
placeholder units, quantities, or dimensions.

## Testing Boundaries

Tests can implement `IUnitCatalog` to exercise missing, unique, ambiguous, and
prefixed lookup behavior without building a full mutable registry. They can
also implement `IUnitParser` and inject it into `QuantityParser` to test numeric
and quantity-kind behavior independently from the unit-expression grammar.
Concrete integration tests continue to use `UnitRegistry` and `UnitParser`.

## Precision Policy

Unit scale factors are stored as `RationalNumber` values so exact definitions
such as inch, foot, pound, liter, electronvolt, data units, and SI prefixes do
not accumulate avoidable floating-point drift while unit definitions are being
composed. Conversions evaluate those exact rational scales to `double` at the
numeric conversion boundary because `QuantityValue` stores scalar values as
`double`.

Physical constants carry an explicit exactness label, source label, source
edition or adjustment, and absolute standard uncertainty in the constant's
stored unit. Constants defined by the SI or by convention are labeled `exact`
and have zero uncertainty. Measured constants, such as the Newtonian
gravitational constant, retain the representative value and uncertainty from
their identified adjustment.

The library does not claim arbitrary-precision quantity arithmetic. Exact
rational metadata is used to preserve definitions and conversion factors as far
as practical, while final computed quantity values follow normal `double`
precision rules.

## Unsupported Features

The parser and formatter use a compact UCUM-inspired syntax, but the library is
not a full UCUM implementation. It does not currently cover every UCUM unit
atom, annotation rule, case-sensitivity rule, synonym, or special semantic
conversion.

Arbitrary logarithmic units are not supported. The core conversion pipeline
supports scale and affine conversions, with explicit special/contextual
equivalencies only where they are named and opted into.

Quantities are scalar `double` values. Array quantities, vectorized operations,
and arbitrary numeric value types are outside the current runtime model.

The typed facade classes provide convenience APIs, but they do not provide
generic compile-time quantity types or F#-style units of measure. Dimension
safety is enforced by runtime dimension vectors and conversion checks.

## Canonical Interchange Grammar

Canonical unit text is whitespace-free and uses stable unit identifiers rather
than display names or symbols. The formatter emits this grammar:

```text
expression       = atom
                 | "1"
                 | "(" expression "*" expression ")"
                 | "(" expression "/" expression ")"
                 | "(" expression "^" signed-integer ")"
                 | "(" expression "^1/" positive-integer ")"
atom             = identifier | escaped-identifier
identifier       = one-or-more ASCII letters or "_"
escaped-identifier = "[" one-or-more groups of four uppercase hex digits "]"
```

Each four-digit escaped group represents one UTF-16 code unit from the original
registered identifier. This makes arbitrary custom identifiers unambiguous
inside compound expressions. Bare and escaped atoms resolve by stable id before
broader symbol, name, alias, or prefix lookup. Root expressions succeed only
when both the dimension and exact rational scale have the requested root.

Canonical quantities use the invariant round-trip `double` representation, one
ASCII space, and a canonical unit expression. `QuantityDto.Unit` uses the same
unit grammar. `DeserializeCanonicalResult` and `FromDtoResult` provide
structured failures without exceptions for invalid input, unknown or ambiguous
units, and incompatible quantity kinds.

## Examples

Common conversion:

```csharp
StandardUnits standardUnits = new StandardUnits();
UnitRegistry registry = new StandardRegistry().Create();
QuantityValue distance = new QuantityValue(1.0d, standardUnits.Mile());
double meters = distance.As(standardUnits.Meter());
```

Arithmetic:

```csharp
StandardUnits standardUnits = new StandardUnits();
QuantityValue left = new QuantityValue(3.0d, standardUnits.Foot());
QuantityValue right = new QuantityValue(12.0d, standardUnits.Inch());
QuantityValue total = new QuantityMath().Add(left, right);
```

Parsing and formatting:

```csharp
QuantityParser parser = new QuantityParser(new UnitParser());
QuantityValue acceleration = parser.ParseOrThrow("9.81 m/s^2", registry);

QuantityFormatter formatter = new QuantityFormatter();
string text = formatter.FormatWithOptions(
    acceleration,
    new QuantityFormatOptions(
        "G",
        System.Globalization.CultureInfo.InvariantCulture,
        new UnitFormatOptions(UnitFormatStyle.Canonical)));
```

Custom units:

```csharp
UnitRegistry customRegistry = new UnitRegistry();
StandardUnits standardUnits = new StandardUnits();
standardUnits.RegisterSiBaseUnits(customRegistry);

UnitDefinition smoot = new UnitDefinition(
    "smoot",
    "smoot",
    "smoots",
    "smoot",
    new string[0],
    new StandardDimensions().Length(),
    new RationalNumber(851, 500),
    new RationalNumber(0, 1),
    false,
    UnitConversionKind.Scale);

customRegistry.AddUnit(smoot);
QuantityValue bridgeLength = new QuantityValue(2.0d, smoot);
double bridgeMeters = bridgeLength.As(standardUnits.Meter());
```

Serialization:

```csharp
QuantitySerializer serializer = new QuantitySerializer();
string canonical = serializer.SerializeCanonical(distance);
QuantityValue roundTrip = serializer.DeserializeCanonical(canonical, registry);
QuantityDto dto = serializer.ToDtoWithQuantityKind(distance, "length");
QuantityParseResult textResult = serializer.DeserializeCanonicalResult(canonical, registry);
QuantityConstructionResult dtoResult = serializer.FromDtoResult(dto, registry);
```

Typed facades:

```csharp
StandardUnits standardUnits = new StandardUnits();
Length typedDistance = new Length(1.0d, standardUnits.Mile());
double kilometers = typedDistance.Kilometers();
QuantityValue dynamicDistance = typedDistance.ToQuantityValue();
```
