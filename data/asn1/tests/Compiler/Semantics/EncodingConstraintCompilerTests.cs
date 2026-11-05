using asn1;

namespace asn1.Tests;

public class EncodingConstraintCompilerTests
{
    [Test]
    public void CompilerSeparatesBerPerOerAndXerIntegerBounds()
    {
        ResolutionFixture fixture = Resolve(
            "Encoding DEFINITIONS ::= BEGIN "
                + "Bounded ::= INTEGER ((0..255) EXCEPT 1); "
                + "Extended ::= INTEGER (0..255, ...) END");
        ResolvedTypeSyntax bounded = RequireType(fixture, "Bounded");
        EncodingEffectiveConstraints views = bounded.EncodingConstraints;

        Assert.That(bounded.IntegerConstraints?.KnownValues?.Contains(
            System.Numerics.BigInteger.One), Is.False);
        Assert.That(views.Per.IntegerConstraints?.KnownValues?.Contains(
            System.Numerics.BigInteger.One), Is.True);
        Assert.That(views.Oer.IntegerConstraints?.KnownValues?.Contains(
            System.Numerics.BigInteger.One), Is.True);
        Assert.That(views.Ber.IntegerConstraints, Is.Null);
        Assert.That(views.Xer.IntegerConstraints, Is.Null);
        Assert.That(views.GetView(AsnEncodingRules.Der), Is.SameAs(views.Ber));
        Assert.That(views.GetView(AsnEncodingRules.Uper), Is.SameAs(views.Per));
        Assert.That(views.GetView(AsnEncodingRules.Coer), Is.SameAs(views.Oer));
        Assert.That(views.GetView(AsnEncodingRules.CanonicalXer),
            Is.SameAs(views.Xer));

        EncodingEffectiveConstraints extended = RequireType(fixture, "Extended")
            .EncodingConstraints;
        Assert.That(extended.Per.IntegerConstraints?.OpenExtension, Is.True);
        Assert.That(extended.Per.IntegerConstraints?.Root?.Contains(
            new System.Numerics.BigInteger(255)), Is.True);
        Assert.That(extended.Oer.IntegerConstraints, Is.Null);
    }

    [Test]
    public void CompilerUsesExactPerSizesAndOerRangeHulls()
    {
        ResolutionFixture fixture = Resolve(
            "Encoding DEFINITIONS ::= BEGIN "
                + "Sparse ::= OCTET STRING (SIZE (2 | 4)); "
                + "Minus ::= OCTET STRING (SIZE ((1..10) EXCEPT 5)); "
                + "Items ::= SEQUENCE (SIZE (1..3)) OF INTEGER; "
                + "Utf ::= UTF8String (SIZE (1..5)); "
                + "Fixed ::= IA5String (SIZE (1..5)) END");
        EncodingEffectiveConstraints sparse = RequireType(fixture, "Sparse")
            .EncodingConstraints;

        Assert.That(sparse.Per.SizeConstraints?.KnownSizes?.Contains(
            new System.Numerics.BigInteger(2)), Is.True);
        Assert.That(sparse.Per.SizeConstraints?.KnownSizes?.Contains(
            new System.Numerics.BigInteger(3)), Is.False);
        Assert.That(sparse.Oer.SizeConstraints?.KnownSizes?.Contains(
            new System.Numerics.BigInteger(3)), Is.True);
        Assert.That(sparse.Oer.SizeConstraints?.RootLowerBound,
            Is.EqualTo(new System.Numerics.BigInteger(2)));
        Assert.That(sparse.Oer.SizeConstraints?.RootUpperBound,
            Is.EqualTo(new System.Numerics.BigInteger(4)));

        EncodingEffectiveConstraints minus = RequireType(fixture, "Minus")
            .EncodingConstraints;
        Assert.That(minus.Per.SizeConstraints?.KnownSizes?.Contains(
            new System.Numerics.BigInteger(5)), Is.True);
        Assert.That(minus.Oer.SizeConstraints?.KnownSizes?.Contains(
            new System.Numerics.BigInteger(5)), Is.True);
        Assert.That(RequireType(fixture, "Items").EncodingConstraints.Per.SizeConstraints,
            Is.Not.Null);
        Assert.That(RequireType(fixture, "Items").EncodingConstraints.Oer.SizeConstraints,
            Is.Null);
        Assert.That(RequireType(fixture, "Utf").SizeConstraints, Is.Not.Null);
        Assert.That(RequireType(fixture, "Utf").EncodingConstraints.Per.SizeConstraints,
            Is.Null);
        Assert.That(RequireType(fixture, "Utf").EncodingConstraints.Oer.SizeConstraints,
            Is.Null);
        Assert.That(RequireType(fixture, "Fixed").EncodingConstraints.Per.SizeConstraints,
            Is.Not.Null);
        Assert.That(RequireType(fixture, "Fixed").EncodingConstraints.Oer.SizeConstraints,
            Is.Not.Null);
    }

    [Test]
    public void CompilerKeepsOnlyNonExtensiblePerAlphabets()
    {
        ResolutionFixture fixture = Resolve(
            "Encoding DEFINITIONS ::= BEGIN "
                + "Closed ::= IA5String (FROM (\"A\"..\"D\")); "
                + "Open ::= IA5String (FROM (\"A\"..\"D\", ...)); "
                + "Utf ::= UTF8String (FROM (\"A\"..\"D\")) END");
        EncodingEffectiveConstraints closed = RequireType(fixture, "Closed")
            .EncodingConstraints;
        EncodingEffectiveConstraints open = RequireType(fixture, "Open")
            .EncodingConstraints;

        Assert.That(closed.Per.PermittedAlphabet?.KnownScalars?.Contains(
            new System.Numerics.BigInteger('A')), Is.True);
        Assert.That(closed.Per.PermittedAlphabet?.KnownScalars?.Contains(
            new System.Numerics.BigInteger('Z')), Is.False);
        Assert.That(closed.Oer.PermittedAlphabet, Is.Null);
        Assert.That(RequireType(fixture, "Open").PermittedAlphabet?.OpenExtension,
            Is.True);
        Assert.That(open.Per.PermittedAlphabet, Is.Null);
        Assert.That(RequireType(fixture, "Utf").PermittedAlphabet, Is.Not.Null);
        Assert.That(RequireType(fixture, "Utf").EncodingConstraints.Per
            .PermittedAlphabet, Is.Null);
    }

    [Test]
    public void CompilerDoesNotInferBoundsAcrossEncodingInvisibleUnions()
    {
        ResolutionFixture fixture = Resolve(
            "Encoding DEFINITIONS ::= BEGIN "
                + "Both ::= IA5String (SIZE (1..4) ^ FROM (\"A\"..\"D\", ...)); "
                + "Either ::= IA5String (SIZE (1..4) | FROM (\"A\"..\"D\")) END");
        EncodingEffectiveConstraints both = RequireType(fixture, "Both")
            .EncodingConstraints;
        EncodingEffectiveConstraints either = RequireType(fixture, "Either")
            .EncodingConstraints;

        Assert.That(both.Per.SizeConstraints?.RootLowerBound,
            Is.EqualTo(System.Numerics.BigInteger.One));
        Assert.That(both.Per.SizeConstraints?.RootUpperBound,
            Is.EqualTo(new System.Numerics.BigInteger(4)));
        Assert.That(both.Per.PermittedAlphabet, Is.Null);
        Assert.That(either.Per.SizeConstraints, Is.Null);
        Assert.That(either.Oer.SizeConstraints, Is.Null);
    }

    [Test]
    public void CompilerSeparatesTimeContentsAndTypeConstraintChannels()
    {
        ResolutionFixture fixture = Resolve(
            "Encoding DEFINITIONS ::= BEGIN "
                + "Clock ::= TIME (SETTINGS \"Basic=Time Time=HMS\"); "
                + "Payload ::= IA5String (SIZE (1..8)); "
                + "OpenPayload ::= ANY (Payload); "
                + "EncodedPayload ::= OCTET STRING (CONTAINING Payload) END");
        EncodingEffectiveConstraints clock = RequireType(fixture, "Clock")
            .EncodingConstraints;
        EncodingEffectiveConstraints open = RequireType(fixture, "OpenPayload")
            .EncodingConstraints;
        EncodingEffectiveConstraints encoded = RequireType(fixture, "EncodedPayload")
            .EncodingConstraints;

        Assert.That(clock.Per.TimeSettings, Has.Length.EqualTo(1));
        Assert.That(clock.Oer.TimeSettings, Has.Length.EqualTo(1));
        Assert.That(clock.Ber.TimeSettings, Is.Empty);
        Assert.That(clock.Xer.TimeSettings, Is.Empty);
        Assert.That(open.TypeConstraints, Has.Length.EqualTo(1));
        Assert.That(open.ContentsConstraints, Is.Empty);
        Assert.That(encoded.ContentsConstraints, Has.Length.EqualTo(1));
        Assert.That(encoded.TypeConstraints, Is.Empty);
        Assert.That(encoded.Ber.SizeConstraints, Is.Null);
        Assert.That(encoded.Xer.SizeConstraints, Is.Null);
    }

    [Test]
    public void NonVisibleSerialApplicationRemovesEarlierPerExtensibility()
    {
        ResolutionFixture fixture = Resolve(
            "Encoding DEFINITIONS ::= BEGIN "
                + "Base ::= IA5String (SIZE (1..4, ...)); "
                + "Closed ::= Base (FROM (\"A\"..\"D\", ...)); "
                + "Narrow ::= INTEGER (0..100, ...); "
                + "Narrower ::= Narrow (20..80) END");
        EncodingConstraintView closed = RequireType(fixture, "Closed")
            .EncodingConstraints.Per;
        EncodingConstraintView narrower = RequireType(fixture, "Narrower")
            .EncodingConstraints.Per;

        Assert.Multiple(() =>
        {
            Assert.That(closed.SizeConstraints?.RootLowerBound,
                Is.EqualTo(System.Numerics.BigInteger.One));
            Assert.That(closed.SizeConstraints?.RootUpperBound,
                Is.EqualTo(new System.Numerics.BigInteger(4)));
            Assert.That(closed.SizeConstraints?.OpenExtension, Is.False);
            Assert.That(closed.SizeConstraints?.Extension?.IsEmpty, Is.True);
            Assert.That(closed.PermittedAlphabet, Is.Null);
            Assert.That(closed.HasEncodingEffect, Is.True);
            Assert.That(closed.HasValidationOnlyConstraints, Is.True);
            Assert.That(narrower.IntegerConstraints?.Root?.Contains(20),
                Is.True);
            Assert.That(narrower.IntegerConstraints?.Root?.Contains(80),
                Is.True);
            Assert.That(narrower.IntegerConstraints?.Root?.Contains(81),
                Is.False);
            Assert.That(narrower.IntegerConstraints?.OpenExtension, Is.False);
        });
    }

    [Test]
    public void CompilerMarksConstraintsThatValidateWithoutChangingPerBits()
    {
        ResolutionFixture fixture = Resolve(
            "Encoding DEFINITIONS ::= BEGIN "
                + "Flag ::= BOOLEAN (TRUE); "
                + "Utf ::= UTF8String (SIZE (1..5)); "
                + "Visible ::= INTEGER (1..9) END");
        EncodingConstraintView flag = RequireType(fixture, "Flag")
            .EncodingConstraints.Per;
        EncodingConstraintView utf = RequireType(fixture, "Utf")
            .EncodingConstraints.Per;
        EncodingConstraintView visible = RequireType(fixture, "Visible")
            .EncodingConstraints.Per;

        Assert.Multiple(() =>
        {
            Assert.That(flag.HasEncodingEffect, Is.False);
            Assert.That(flag.HasValidationOnlyConstraints, Is.True);
            Assert.That(utf.HasEncodingEffect, Is.False);
            Assert.That(utf.HasValidationOnlyConstraints, Is.True);
            Assert.That(visible.HasEncodingEffect, Is.True);
            Assert.That(visible.HasValidationOnlyConstraints, Is.False);
        });
    }

    [Test]
    public void ComponentConstraintsRemainAvailableWithoutChangingWireShape()
    {
        ResolutionFixture fixture = Resolve(
            "Encoding DEFINITIONS ::= BEGIN "
                + "Record ::= SEQUENCE { count INTEGER OPTIONAL, "
                + "label IA5String OPTIONAL } "
                + "(WITH COMPONENTS { ..., count (1..5) PRESENT, "
                + "label ABSENT }); "
                + "Numbers ::= SEQUENCE (WITH COMPONENT (1..9)) OF INTEGER END");
        ResolvedTypeSyntax recordType = RequireType(fixture, "Record");
        EncodingConstraintView record = recordType.EncodingConstraints.Per;
        EncodingConstraintView numbers = RequireType(fixture, "Numbers")
            .EncodingConstraints.Per;

        Assert.Multiple(() =>
        {
            Assert.That(record.ComponentConstraintSets,
                Has.Length.EqualTo(1));
            Assert.That(record.ComponentConstraintSets,
                Is.SameAs(recordType.ComponentConstraintSets));
            Assert.That(record.ComponentConstraintSets[0].Components[0].Presence,
                Is.EqualTo(ComponentPresenceConstraintKind.Present));
            Assert.That(record.ComponentConstraintSets[0].Components[1].Presence,
                Is.EqualTo(ComponentPresenceConstraintKind.Absent));
            Assert.That(record.HasEncodingEffect, Is.False);
            Assert.That(record.HasValidationOnlyConstraints, Is.True);
            Assert.That(numbers.ComponentConstraintSets,
                Has.Length.EqualTo(1));
            Assert.That(numbers.ComponentConstraintSets[0].CollectionElement,
                Is.True);
            Assert.That(numbers.HasEncodingEffect, Is.False);
            Assert.That(numbers.HasValidationOnlyConstraints, Is.True);
        });
    }

    [Test]
    public void PerViewRetainsValueSizeAlphabetRootsAndKnownExtensions()
    {
        ResolutionFixture fixture = Resolve(
            "Encoding DEFINITIONS ::= BEGIN "
                + "Number ::= INTEGER (0..10, ..., 20..30); "
                + "Payload ::= OCTET STRING (SIZE (1..4, ..., 8..9)); "
                + "Letters ::= IA5String (FROM (\"A\"..\"D\")) END");
        EncodingConstraintView number = RequireType(fixture, "Number")
            .EncodingConstraints.Per;
        EncodingConstraintView payload = RequireType(fixture, "Payload")
            .EncodingConstraints.Per;
        EncodingConstraintView letters = RequireType(fixture, "Letters")
            .EncodingConstraints.Per;

        Assert.Multiple(() =>
        {
            Assert.That(number.IntegerConstraints?.Root?.Contains(10), Is.True);
            Assert.That(number.IntegerConstraints?.Root?.Contains(20), Is.False);
            Assert.That(number.IntegerConstraints?.Extension?.Contains(20),
                Is.True);
            Assert.That(number.IntegerConstraints?.OpenExtension, Is.True);
            Assert.That(payload.SizeConstraints?.Root?.Contains(4), Is.True);
            Assert.That(payload.SizeConstraints?.Extension?.Contains(8), Is.True);
            Assert.That(payload.SizeConstraints?.OpenExtension, Is.True);
            Assert.That(letters.PermittedAlphabet?.RootAlphabetSize,
                Is.EqualTo(4));
            Assert.That(letters.PermittedAlphabet?.GetRootIndex('C'),
                Is.EqualTo(2));
            Assert.That(number.HasEncodingEffect, Is.True);
            Assert.That(payload.HasEncodingEffect, Is.True);
            Assert.That(letters.HasEncodingEffect, Is.True);
        });
    }

    [Test]
    public void CrossDimensionUnionsProduceSafeEffectivePerConstraints()
    {
        ResolutionFixture fixture = Resolve(
            "Encoding DEFINITIONS ::= BEGIN "
                + "Mixed ::= IA5String ((SIZE (20) ^ FROM (\"A\"..\"F\")) | "
                + "(SIZE (3) ^ FROM (\"F\"..\"K\"))); "
                + "Sparse ::= IA5String (SIZE (3 | 20)) END");
        EncodingConstraintView mixed = RequireType(fixture, "Mixed")
            .EncodingConstraints.Per;
        EncodingConstraintView sparse = RequireType(fixture, "Sparse")
            .EncodingConstraints.Per;

        Assert.Multiple(() =>
        {
            Assert.That(mixed.SizeConstraints?.RootLowerBound,
                Is.EqualTo(new System.Numerics.BigInteger(3)));
            Assert.That(mixed.SizeConstraints?.RootUpperBound,
                Is.EqualTo(new System.Numerics.BigInteger(20)));
            Assert.That(mixed.SizeConstraints?.Root?.Contains(10), Is.True);
            Assert.That(mixed.PermittedAlphabet?.KnownScalars?.Contains('A'),
                Is.True);
            Assert.That(mixed.PermittedAlphabet?.KnownScalars?.Contains('K'),
                Is.True);
            Assert.That(mixed.PermittedAlphabet?.KnownScalars?.Contains('G'),
                Is.True);
            Assert.That(sparse.SizeConstraints?.KnownSizes?.Contains(3), Is.True);
            Assert.That(sparse.SizeConstraints?.KnownSizes?.Contains(20), Is.True);
            Assert.That(sparse.SizeConstraints?.KnownSizes?.Contains(10), Is.False);
        });
    }

    private static ResolvedTypeSyntax RequireType(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0].FindSymbol(name, AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();
    }

    private static ResolutionFixture Resolve(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        diagnostics.ApplyCompilerOptions(new AsnCompilerOptions { EnableLegacyAny = true });
        IncrementalLexSession session = new IncrementalLexSession(
            "encoding-constraints.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult parsed = new ModuleParser(new TokenCursor(session, 4)).ParseModule();
        Assert.That(parsed.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables(
            [parsed.Module ?? throw new InvalidOperationException()]);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        AssignmentClassificationResult classification = new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(classification.Success, Is.True);
        Assert.That(types.Success, Is.True);
        ValueResolutionResult values = new ValueResolver(diagnostics).ResolveValues(tables);
        Assert.That(values.Success, Is.True, string.Join(" | ", diagnostics.GetDiagnostics()
            .Select(item => item.Code + ":" + item.Message)));
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Tables = tables
        };
    }

    private class ResolutionFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
    }
}
