using asn1;

namespace asn1.Tests;

public class ValueResolverTests
{
    [Test]
    public void ResolverAcceptsRestrictedAlphabetsUnicodeAndCharacterTuples()
    {
        ModuleSyntax module = ParseModule(
            "valid-strings.asn",
            "ValidStrings DEFINITIONS ::= BEGIN "
                + "numeric NumericString ::= \"12 345\"; "
                + "printable PrintableString ::= \"AZaz09 '()+,-./:=?\"; "
                + "ascii IA5String ::= \"line\tend\"; "
                + "visible VisibleString ::= \"visible ~ text\"; "
                + "utf8 UTF8String ::= \"hello 😀\"; "
                + "bmp BMPString ::= { 0, 65 }; "
                + "universal UniversalString ::= { 0, 1, 246, 0 } END");
        ResolutionFixture fixture = Resolve([module]);

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics(),
            Is.Empty,
            FormatDiagnostics(fixture.Diagnostics));
    }

    [Test]
    public void ResolverRejectsCharactersOutsideRestrictedAlphabetsAndInvalidTuples()
    {
        ModuleSyntax module = ParseModule(
            "invalid-strings.asn",
            "InvalidStrings DEFINITIONS ::= BEGIN "
                + "numeric NumericString ::= \"12A\"; "
                + "printable PrintableString ::= \"bad@value\"; "
                + "ascii IA5String ::= \"café\"; "
                + "bmp BMPString ::= \"😀\"; "
                + "tuple UniversalString ::= { 0, 17, 0, 0 } END");
        ResolutionFixture fixture = Resolve([module]);
        string[] codes = fixture.Diagnostics.GetDiagnostics()
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(codes.Count(code => code == "ASNS0032"), Is.EqualTo(4));
        Assert.That(codes, Does.Contain("ASNS0033"));
        Assert.That(
            fixture.Diagnostics.GetDiagnostics()
                .Where(diagnostic => diagnostic.Code == "ASNS0032"
                    || diagnostic.Code == "ASNS0033")
                .All(diagnostic => diagnostic.RelatedLocations.Length == 1),
            Is.True);
    }

    [Test]
    public void ValidatorRejectsInvalidUnicodeSequencesAndUnusedBitPositions()
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SemanticValueValidator validator = new SemanticValueValidator(diagnostics);
        ResolvedValueSyntax text = new ResolvedValueSyntax
        {
            GoverningType = new ResolvedTypeSyntax { EffectiveKind = AsnTypeKind.Utf8String },
            EffectiveValue = new AsnValue
            {
                Kind = AsnValueKind.CharacterString,
                TextValue = "\ud800"
            }
        };
        ResolvedValueSyntax bits = new ResolvedValueSyntax
        {
            GoverningType = new ResolvedTypeSyntax { EffectiveKind = AsnTypeKind.BitString },
            EffectiveValue = new AsnValue { Kind = AsnValueKind.BitString },
            BitStringValue = new AsnBitStringValue
            {
                Bytes = [0x81],
                BitLength = 1
            }
        };

        Assert.That(validator.ValidateResolvedValue(text), Is.False);
        Assert.That(validator.ValidateResolvedValue(bits), Is.False);
        Assert.That(
            diagnostics.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Is.EqualTo(new[] { "ASNS0031", "ASNS0034" }));
    }

    [Test]
    public void ResolverValidatesObjectIdentifiersAndIriLexicalForms()
    {
        ModuleSyntax validModule = ParseModule(
            "valid-identifiers.asn",
            "ValidIdentifiers DEFINITIONS ::= BEGIN "
                + "bits BIT STRING ::= 'A'H; "
                + "oid OBJECT IDENTIFIER ::= { 2 999999999999999999999999999999 }; "
                + "relative RELATIVE-OID ::= { 0 999 }; "
                + "iri OID-IRI ::= \"/org/example-é/%20\"; "
                + "relativeIri RELATIVE-OID-IRI ::= \"org/example\" END");
        ResolutionFixture valid = Resolve([validModule]);

        Assert.That(valid.Result.Success, Is.True);
        Assert.That(GetValue(valid.Tables[0], "bits").BitStringValue?.BitLength,
            Is.EqualTo(4));
        Assert.That(
            valid.Diagnostics.GetDiagnostics(),
            Is.Empty,
            FormatDiagnostics(valid.Diagnostics));

        ModuleSyntax invalidModule = ParseModule(
            "invalid-identifiers.asn",
            "InvalidIdentifiers DEFINITIONS ::= BEGIN "
                + "negative INTEGER ::= -1; "
                + "badRoot OBJECT IDENTIFIER ::= { 3 1 }; "
                + "badSecond OBJECT IDENTIFIER ::= { 1 40 }; "
                + "badRelative RELATIVE-OID ::= { negative }; "
                + "missingRoot OID-IRI ::= \"org/example\"; "
                + "leadingRoot RELATIVE-OID-IRI ::= \"/org/example\"; "
                + "emptyArc OID-IRI ::= \"/org//example\"; "
                + "badEscape OID-IRI ::= \"/org/%ZZ\" END");
        ResolutionFixture invalid = Resolve([invalidModule]);
        string[] codes = invalid.Diagnostics.GetDiagnostics()
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        Assert.That(invalid.Result.Success, Is.False);
        Assert.That(codes.Count(code => code == "ASNS0036"), Is.EqualTo(3));
        Assert.That(codes.Count(code => code == "ASNS0037"), Is.EqualTo(4));
    }

    [Test]
    public void ResolverValidatesUsefulAndModernTimeCalendars()
    {
        ModuleSyntax validModule = ParseModule(
            "valid-time.asn",
            "ValidTime DEFINITIONS ::= BEGIN "
                + "utc UTCTime ::= \"000229235960Z\"; "
                + "generalized GeneralizedTime ::= \"20240229235959.125+1400\"; "
                + "date DATE ::= \"2024-02-29\"; "
                + "clock TIME-OF-DAY ::= \"24:00:00\"; "
                + "combined DATE-TIME ::= \"2024-02-29T23:59:60-07:00\"; "
                + "duration DURATION ::= \"P2Y3M4DT5H6M7.5S\" END");
        ResolutionFixture valid = Resolve([validModule]);

        Assert.That(valid.Result.Success, Is.True);
        Assert.That(
            valid.Diagnostics.GetDiagnostics(),
            Is.Empty,
            FormatDiagnostics(valid.Diagnostics));

        ModuleSyntax invalidModule = ParseModule(
            "invalid-time.asn",
            "InvalidTime DEFINITIONS ::= BEGIN "
                + "utc UTCTime ::= \"2302291200Z\"; "
                + "generalized GeneralizedTime ::= \"20230229120000Z\"; "
                + "date DATE ::= \"2023-04-31\"; "
                + "clock TIME-OF-DAY ::= \"24:00:01\"; "
                + "zone DATE-TIME ::= \"2024-01-01T12:00:00+14:01\"; "
                + "duration DURATION ::= \"P1.5Y2M\" END");
        ResolutionFixture invalid = Resolve([invalidModule]);

        Assert.That(invalid.Result.Success, Is.False);
        Assert.That(
            invalid.Diagnostics.GetDiagnostics()
                .Count(diagnostic => diagnostic.Code == "ASNS0038"),
            Is.EqualTo(6));
    }

    [Test]
    public void ResolverNormalizesDecimalRealValuesWithoutLosingExactness()
    {
        ModuleSyntax module = ParseModule(
            "decimal-real.asn",
            "DecimalReal DEFINITIONS ::= BEGIN "
                + "decimal REAL ::= 1.2500E+3; "
                + "huge REAL ::= -9.90E+999999999999999999999999999999; "
                + "zero REAL ::= 0; "
                + "minusZero REAL ::= -0E+100; "
                + "copy REAL ::= decimal; "
                + "Holder ::= SEQUENCE { rate REAL DEFAULT 10.00E-1 } END");
        ResolutionFixture fixture = Resolve([module]);

        ResolvedValueSyntax decimalValue = GetValue(fixture.Tables[0], "decimal");
        AsnRealValue decimalReal = decimalValue.EffectiveValue?.RealValue
            ?? throw new InvalidOperationException();
        AsnRealValue hugeReal = GetValue(fixture.Tables[0], "huge")
            .EffectiveValue?.RealValue ?? throw new InvalidOperationException();
        ResolvedValueSyntax copy = GetValue(fixture.Tables[0], "copy");
        ResolvedTypeSyntax holder = fixture.Tables[0]
            .FindSymbol("Holder", AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(decimalValue.SourceValue?.RealValue?.Normalized, Is.False);
        Assert.That(decimalValue.SourceValue?.RealValue?.DecimalText, Is.EqualTo("1.2500E+3"));
        Assert.That(decimalReal.Normalized, Is.True);
        Assert.That(decimalReal.IsValid(), Is.True);
        Assert.That(decimalReal.Kind, Is.EqualTo(AsnRealKind.FiniteDecimal));
        Assert.That(decimalReal.DecimalCoefficient.ToString(), Is.EqualTo("125"));
        Assert.That(decimalReal.DecimalExponent.ToString(), Is.EqualTo("1"));
        Assert.That(decimalReal.DecimalText, Is.EqualTo("1.25E+3"));
        Assert.That(hugeReal.DecimalCoefficient.ToString(), Is.EqualTo("-99"));
        Assert.That(
            hugeReal.DecimalExponent.ToString(),
            Is.EqualTo("999999999999999999999999999998"));
        Assert.That(GetValue(fixture.Tables[0], "zero").EffectiveValue?.RealValue?.Kind,
            Is.EqualTo(AsnRealKind.Zero));
        Assert.That(GetValue(fixture.Tables[0], "minusZero").EffectiveValue?.RealValue?.Kind,
            Is.EqualTo(AsnRealKind.MinusZero));
        Assert.That(copy.ReferenceTarget, Is.SameAs(decimalValue));
        Assert.That(copy.EffectiveValue?.RealValue?.DecimalCoefficient,
            Is.EqualTo(decimalReal.DecimalCoefficient));
        Assert.That(copy.EffectiveValue?.RealValue, Is.Not.SameAs(decimalReal));
        Assert.That(
            holder.Components[0].DefaultValue?.EffectiveValue?.RealValue?.DecimalCoefficient,
            Is.EqualTo(new System.Numerics.BigInteger(1)));
        Assert.That(holder.Components[0].DefaultValue?.EffectiveValue?.RealValue?.DecimalExponent,
            Is.EqualTo(System.Numerics.BigInteger.Zero));
        Assert.That(
            fixture.Diagnostics.GetDiagnostics(),
            Is.Empty,
            FormatDiagnostics(fixture.Diagnostics));
    }

    [Test]
    public void ResolverReducesBinaryRealTuplesToCanonicalBaseTwoComponents()
    {
        ModuleSyntax module = ParseModule(
            "binary-real.asn",
            "BinaryReal DEFINITIONS ::= BEGIN "
                + "binary REAL ::= { mantissa -160, base 16, exponent -3 }; "
                + "octal REAL ::= { mantissa 72, base 8, "
                + "exponent 999999999999999999999999999999 }; "
                + "tupleZero REAL ::= { mantissa 0, base 2, exponent -500 }; "
                + "positive REAL ::= PLUS-INFINITY; "
                + "negative REAL ::= MINUS-INFINITY; "
                + "nan REAL ::= NOT-A-NUMBER END");
        ResolutionFixture fixture = Resolve([module]);

        ResolvedValueSyntax binaryValue = GetValue(fixture.Tables[0], "binary");
        AsnRealValue source = binaryValue.SourceValue?.RealValue
            ?? throw new InvalidOperationException();
        AsnRealValue normalized = binaryValue.EffectiveValue?.RealValue
            ?? throw new InvalidOperationException();
        AsnRealValue octal = GetValue(fixture.Tables[0], "octal")
            .EffectiveValue?.RealValue ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(source.Radix, Is.EqualTo(16));
        Assert.That(source.Mantissa.ToString(), Is.EqualTo("-160"));
        Assert.That(normalized.Normalized, Is.True);
        Assert.That(normalized.IsValid(), Is.True);
        Assert.That(normalized.Radix, Is.EqualTo(2));
        Assert.That(normalized.Mantissa.ToString(), Is.EqualTo("-5"));
        Assert.That(normalized.Exponent.ToString(), Is.EqualTo("-7"));
        Assert.That(octal.Radix, Is.EqualTo(2));
        Assert.That(octal.Mantissa.ToString(), Is.EqualTo("9"));
        Assert.That(
            octal.Exponent.ToString(),
            Is.EqualTo("3000000000000000000000000000000"));
        Assert.That(GetValue(fixture.Tables[0], "tupleZero").EffectiveValue?.RealValue?.Kind,
            Is.EqualTo(AsnRealKind.Zero));
        Assert.That(GetValue(fixture.Tables[0], "positive").EffectiveValue?.RealValue?.Kind,
            Is.EqualTo(AsnRealKind.PositiveInfinity));
        Assert.That(GetValue(fixture.Tables[0], "negative").EffectiveValue?.RealValue?.Kind,
            Is.EqualTo(AsnRealKind.NegativeInfinity));
        Assert.That(GetValue(fixture.Tables[0], "nan").EffectiveValue?.RealValue?.Kind,
            Is.EqualTo(AsnRealKind.NotANumber));
        Assert.That(
            fixture.Diagnostics.GetDiagnostics(),
            Is.Empty,
            FormatDiagnostics(fixture.Diagnostics));
    }

    [Test]
    public void ResolverEvaluatesExactArithmeticNamedNumbersAndNamedBits()
    {
        ModuleSyntax module = ParseModule(
            "values.asn",
            "Values DEFINITIONS ::= BEGIN "
                + "base INTEGER ::= 123456789012345678901234567890; "
                + "calculated INTEGER ::= (base + 10) * 3 - 4 / 2; "
                + "bitPosition INTEGER ::= 10; "
                + "NamedInteger ::= INTEGER { calculated-number(calculated) }; "
                + "selected NamedInteger ::= calculated-number; "
                + "Flags ::= BIT STRING { first(0), distant(bitPosition) }; "
                + "mask Flags ::= { first, distant } END");
        ResolutionFixture fixture = Resolve([module]);

        ResolvedValueSyntax calculated = GetValue(fixture.Tables[0], "calculated");
        ResolvedValueSyntax selected = GetValue(fixture.Tables[0], "selected");
        ResolvedValueSyntax mask = GetValue(fixture.Tables[0], "mask");

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(
            calculated.EffectiveValue?.IntegerValue.ToString(),
            Is.EqualTo("370370367037037036703703703698"));
        Assert.That(
            selected.EffectiveValue?.IntegerValue,
            Is.EqualTo(calculated.EffectiveValue?.IntegerValue));
        Assert.That(mask.BitStringValue?.BitLength, Is.EqualTo(11));
        Assert.That(mask.BitStringValue?.Bytes, Is.EqualTo(new byte[] { 0x80, 0x20 }));
        Assert.That(
            fixture.Diagnostics.GetDiagnostics(),
            Is.Empty,
            FormatDiagnostics(fixture.Diagnostics));
    }

    [Test]
    public void ResolverEvaluatesNestedStructuredValuesAndDefaults()
    {
        ModuleSyntax module = ParseModule(
            "structured.asn",
            "Structured DEFINITIONS ::= BEGIN "
                + "limit INTEGER ::= 40; "
                + "Count ::= INTEGER { standard(limit) }; "
                + "Flags ::= BIT STRING { urgent(1), archived(9) }; "
                + "Settings ::= SEQUENCE { "
                + "count Count DEFAULT standard, "
                + "offset INTEGER DEFAULT limit + 2, "
                + "flags Flags DEFAULT { archived } }; "
                + "settings Settings ::= { count limit + 2, flags { urgent, archived } } END");
        ResolutionFixture fixture = Resolve([module]);

        ResolvedValueSyntax settings = GetValue(fixture.Tables[0], "settings");
        ResolvedTypeSyntax settingsType = fixture.Tables[0]
            .FindSymbol("Settings", AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(settings.Items[0].EffectiveValue?.IntegerValue.ToString(), Is.EqualTo("42"));
        Assert.That(settings.Items[1].BitStringValue?.BitLength, Is.EqualTo(10));
        Assert.That(settings.Items[1].BitStringValue?.Bytes,
            Is.EqualTo(new byte[] { 0x40, 0x40 }));
        Assert.That(
            settingsType.Components[0].DefaultValue?.EffectiveValue?.IntegerValue.ToString(),
            Is.EqualTo("40"));
        Assert.That(
            settingsType.Components[1].DefaultValue?.EffectiveValue?.IntegerValue.ToString(),
            Is.EqualTo("42"));
        Assert.That(settingsType.Components[2].DefaultValue?.BitStringValue?.BitLength,
            Is.EqualTo(10));
        Assert.That(settingsType.Components[2].DefaultValue?.BitStringValue?.Bytes,
            Is.EqualTo(new byte[] { 0x00, 0x40 }));
        Assert.That(
            fixture.Diagnostics.GetDiagnostics(),
            Is.Empty,
            FormatDiagnostics(fixture.Diagnostics));
    }

    [Test]
    public void ResolverNormalizesStructuredDefaultsAndPreservesPresence()
    {
        ModuleSyntax module = ParseModule(
            "normalized-defaults.asn",
            "NormalizedDefaults DEFINITIONS ::= BEGIN "
                + "Config ::= SEQUENCE { id INTEGER, enabled BOOLEAN DEFAULT TRUE, "
                + "note UTF8String OPTIONAL, ..., revision INTEGER OPTIONAL }; "
                + "minimal Config ::= { id 1 }; "
                + "explicit Config ::= { id 2, enabled TRUE }; "
                + "different Config ::= { id 3, enabled FALSE } END");
        ResolutionFixture fixture = Resolve([module]);

        ResolvedValueSyntax minimal = GetValue(fixture.Tables[0], "minimal");
        ResolvedValueSyntax explicitValue = GetValue(fixture.Tables[0], "explicit");
        ResolvedValueSyntax different = GetValue(fixture.Tables[0], "different");

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(minimal.ComponentValues.Select(item => item.Name),
            Is.EqualTo(new[] { "id", "enabled", "note", "revision" }));
        Assert.That(minimal.ComponentValues[0].Present, Is.True);
        Assert.That(minimal.ComponentValues[1].Present, Is.False);
        Assert.That(minimal.ComponentValues[1].Defaulted, Is.True);
        Assert.That(minimal.ComponentValues[2].Value, Is.Null);
        Assert.That(minimal.ComponentValues[3].Value, Is.Null);
        Assert.That(minimal.EffectiveValue?.NamedValues.Select(item => item.Name),
            Is.EqualTo(new[] { "id", "enabled" }));
        Assert.That(explicitValue.ComponentValues[1].Present, Is.True);
        Assert.That(explicitValue.ComponentValues[1].Defaulted, Is.False);
        Assert.That(explicitValue.ComponentValues[1].PresentWithDefaultValue, Is.True);
        Assert.That(different.ComponentValues[1].PresentWithDefaultValue, Is.False);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics(),
            Is.Empty,
            FormatDiagnostics(fixture.Diagnostics));
    }

    [Test]
    public void ResolverNormalizesSetsAndRetainsPermittedExtensionValues()
    {
        ModuleSyntax module = ParseModule(
            "set-extensions.asn",
            "SetExtensions DEFINITIONS ::= BEGIN "
                + "ExtensibleSet ::= SET { first INTEGER, second BOOLEAN DEFAULT FALSE, "
                + "..., known UTF8String OPTIONAL }; "
                + "value ExtensibleSet ::= { unknown 99, known \"ok\", first 1 } END");
        ResolutionFixture fixture = Resolve([module]);
        ResolvedValueSyntax value = GetValue(fixture.Tables[0], "value");

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(value.ComponentValues.Select(item => item.Name),
            Is.EqualTo(new[] { "first", "second", "known", "unknown" }));
        Assert.That(value.ComponentValues[1].Defaulted, Is.True);
        Assert.That(value.ComponentValues[2].Extension, Is.True);
        Assert.That(value.ComponentValues[3].Present, Is.True);
        Assert.That(value.ComponentValues[3].Extension, Is.True);
        Assert.That(value.ComponentValues[3].UnknownExtension, Is.True);
        Assert.That(value.ComponentValues[3].Value?.EffectiveValue?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(99)));
        Assert.That(value.EffectiveValue?.NamedValues.Select(item => item.Name),
            Is.EqualTo(new[] { "first", "second", "known", "unknown" }));
        Assert.That(
            fixture.Diagnostics.GetDiagnostics(),
            Is.Empty,
            FormatDiagnostics(fixture.Diagnostics));
    }

    [Test]
    public void ResolverRejectsInvalidStructuredRootComponents()
    {
        ModuleSyntax module = ParseModule(
            "invalid-structured.asn",
            "InvalidStructured DEFINITIONS ::= BEGIN "
                + "Pair ::= SEQUENCE { first INTEGER, second INTEGER }; "
                + "duplicate Pair ::= { first 1, first 2, second 3 }; "
                + "unknown Pair ::= { first 1, extra 2, second 3 }; "
                + "missing Pair ::= { first 1 }; "
                + "reordered Pair ::= { second 2, first 1 } END");
        ResolutionFixture fixture = Resolve([module]);
        string[] codes = fixture.Diagnostics.GetDiagnostics()
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(codes, Does.Contain("ASNS0039"));
        Assert.That(codes, Does.Contain("ASNS0040"));
        Assert.That(codes, Does.Contain("ASNS0041"));
        Assert.That(codes, Does.Contain("ASNS0042"));
        Assert.That(
            fixture.Diagnostics.GetDiagnostics()
                .Where(diagnostic => diagnostic.Code == "ASNS0039"
                    || diagnostic.Code == "ASNS0040"
                    || diagnostic.Code == "ASNS0041"
                    || diagnostic.Code == "ASNS0042")
                .All(diagnostic => diagnostic.RelatedLocations.Length == 1),
            Is.True);
        AsnDiagnostic duplicate = fixture.Diagnostics.GetDiagnostics()
            .Single(diagnostic => diagnostic.Code == "ASNS0039");
        Assert.That(duplicate.Span, Is.Not.Null);
        Assert.That(
            duplicate.RelatedLocations[0].Span.Start.Offset,
            Is.LessThan(duplicate.Span == null
                ? int.MaxValue
                : duplicate.Span.Start.Offset));
    }

    [Test]
    public void ResolverNormalizesChoiceAndNamedCollectionValues()
    {
        ModuleSyntax module = ParseModule(
            "choice-collections.asn",
            "ChoiceCollections DEFINITIONS ::= BEGIN "
                + "OpenChoice ::= CHOICE { number INTEGER, ..., text UTF8String }; "
                + "known OpenChoice ::= text: \"ok\"; "
                + "future OpenChoice ::= other: 7; "
                + "Numbers ::= SEQUENCE OF item INTEGER; "
                + "named Numbers ::= { first 1, second 2 } END");
        ResolutionFixture fixture = Resolve([module]);
        ResolvedValueSyntax known = GetValue(fixture.Tables[0], "known");
        ResolvedValueSyntax future = GetValue(fixture.Tables[0], "future");
        ResolvedValueSyntax named = GetValue(fixture.Tables[0], "named");

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(known.ComponentValues.Single().Name, Is.EqualTo("text"));
        Assert.That(known.ComponentValues.Single().UnknownExtension, Is.False);
        Assert.That(future.ComponentValues.Single().Name, Is.EqualTo("other"));
        Assert.That(future.ComponentValues.Single().UnknownExtension, Is.True);
        Assert.That(future.ComponentValues.Single().Extension, Is.True);
        Assert.That(named.NamedCollection, Is.True);
        Assert.That(named.ItemNames, Is.EqualTo(new[] { "first", "second" }));
        Assert.That(named.EffectiveValue?.NamedValues.Select(item => item.Name),
            Is.EqualTo(new[] { "first", "second" }));
        Assert.That(
            fixture.Diagnostics.GetDiagnostics(),
            Is.Empty,
            FormatDiagnostics(fixture.Diagnostics));
    }

    [Test]
    public void ResolverRejectsUnknownClosedChoiceAndDuplicateCollectionNames()
    {
        ModuleSyntax module = ParseModule(
            "invalid-choice-collections.asn",
            "InvalidChoiceCollections DEFINITIONS ::= BEGIN "
                + "ClosedChoice ::= CHOICE { number INTEGER }; "
                + "badChoice ClosedChoice ::= other: 7; "
                + "Numbers ::= SEQUENCE OF item INTEGER; "
                + "badNames Numbers ::= { repeated 1, repeated 2 } END");
        ResolutionFixture fixture = Resolve([module]);
        string[] codes = fixture.Diagnostics.GetDiagnostics()
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(codes, Does.Contain("ASNS0043"));
        Assert.That(codes, Does.Contain("ASNS0044"));
        Assert.That(
            fixture.Diagnostics.GetDiagnostics()
                .Where(diagnostic => diagnostic.Code == "ASNS0043"
                    || diagnostic.Code == "ASNS0044")
                .All(diagnostic => diagnostic.RelatedLocations.Length == 1),
            Is.True);
    }

    [Test]
    public void ResolverEvaluatesOidRelativeOidAndIriConstantsAcrossModules()
    {
        ModuleSyntax provider = ParseModule(
            "provider.asn",
            "Provider DEFINITIONS ::= BEGIN "
                + "arc INTEGER ::= 840; "
                + "base OBJECT IDENTIFIER ::= { iso(1) member-body(2) arc }; "
                + "branch RELATIVE-OID ::= { arc 7 }; "
                + "identifier OID-IRI ::= \"/iso/member-body/us\" END");
        ModuleSyntax consumer = ParseModule(
            "consumer.asn",
            "Consumer DEFINITIONS ::= BEGIN "
                + "IMPORTS arc, base, branch, identifier FROM Provider; "
                + "combined OBJECT IDENTIFIER ::= { Provider.base next(Provider.arc) }; "
                + "relative RELATIVE-OID ::= { branch 9 }; "
                + "copied OID-IRI ::= identifier END");
        ResolutionFixture fixture = Resolve([provider, consumer]);

        ResolvedValueSyntax combined = GetValue(fixture.Tables[1], "combined");
        ResolvedValueSyntax relative = GetValue(fixture.Tables[1], "relative");
        ResolvedValueSyntax copied = GetValue(fixture.Tables[1], "copied");

        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(
            combined.ObjectIdentifierValue?.Arcs.Select(arc => arc.ToString()),
            Is.EqualTo(new[] { "1", "2", "840", "840" }));
        Assert.That(
            relative.ObjectIdentifierValue?.Arcs.Select(arc => arc.ToString()),
            Is.EqualTo(new[] { "840", "7", "9" }));
        Assert.That(copied.EffectiveValue?.Kind,
            Is.EqualTo(AsnValueKind.ObjectIdentifierIri));
        Assert.That(copied.EffectiveValue?.TextValue, Is.EqualTo("/iso/member-body/us"));
        Assert.That(copied.ReferenceTarget, Is.SameAs(GetValue(fixture.Tables[0], "identifier")));
        Assert.That(
            fixture.Diagnostics.GetDiagnostics(),
            Is.Empty,
            FormatDiagnostics(fixture.Diagnostics));
    }

    [Test]
    public void ResolverDiagnosesInvalidArithmeticAndValueCycles()
    {
        ModuleSyntax module = ParseModule(
            "invalid.asn",
            "Invalid DEFINITIONS ::= BEGIN "
                + "zero INTEGER ::= 0; "
                + "bad INTEGER ::= 12 / zero; "
                + "first INTEGER ::= second; "
                + "second INTEGER ::= first END");
        ResolutionFixture fixture = Resolve([module]);

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Does.Contain("ASNS0029"));
        Assert.That(
            fixture.Diagnostics.GetDiagnostics().Select(diagnostic => diagnostic.Code),
            Does.Contain("ASNB0016"));
    }

    private static ResolutionFixture Resolve(ModuleSyntax[] modules)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(modules);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        new AssignmentClassifier(diagnostics).ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(types.Success, Is.True);
        ValueResolutionResult result = new ValueResolver(diagnostics).ResolveValues(tables);
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Result = result,
            Tables = tables
        };
    }

    private static ResolvedValueSyntax GetValue(SymbolTable table, string name)
    {
        return table.FindSymbol(name, AsnAssignmentKind.Value)?.ResolvedValue
            ?? throw new InvalidOperationException();
    }

    private static string FormatDiagnostics(DiagnosticBag diagnostics)
    {
        return string.Join(
            System.Environment.NewLine,
            diagnostics.GetDiagnostics().Select(
                diagnostic => diagnostic.Code + ": " + diagnostic.Message));
    }

    private static ModuleSyntax ParseModule(string sourceName, string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            sourceName,
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult result = new ModuleParser(new TokenCursor(session, 4)).ParseModule();
        Assert.That(result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        return result.Module ?? throw new InvalidOperationException();
    }

    private class ResolutionFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal ValueResolutionResult Result = null!;
        internal SymbolTable[] Tables = [];
    }
}
