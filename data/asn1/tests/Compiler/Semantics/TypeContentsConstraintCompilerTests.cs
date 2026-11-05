using asn1;

namespace asn1.Tests;

public class TypeContentsConstraintCompilerTests
{
    [Test]
    public void CompilerResolvesOpenTypeConstraintsAndNestedConstraintMetadata()
    {
        ResolutionFixture fixture = Resolve(
            "Contents DEFINITIONS ::= BEGIN "
                + "Payload ::= UTF8String (SIZE (1..20)); "
                + "OpenPayload ::= ANY (Payload) END",
            true);
        ResolvedTypeSyntax open = RequireType(fixture, "OpenPayload");
        NormalizedTypeConstraint constraint = open.TypeConstraints.Single();
        ResolvedTypeSyntax payload = constraint.ConstrainedType
            ?? throw new InvalidOperationException();

        Assert.That(open.EffectiveKind, Is.EqualTo(AsnTypeKind.OpenType));
        Assert.That(constraint.GoverningType, Is.SameAs(open));
        Assert.That(payload.EffectiveKind, Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(open.ConstraintTypes.Single(), Is.SameAs(payload));
        Assert.That(payload.SizeConstraints?.RootLowerBound.ToString(), Is.EqualTo("1"));
        Assert.That(payload.SizeConstraints?.RootUpperBound.ToString(), Is.EqualTo("20"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CompilerBuildsContainingAndEncodedByCodecMetadata()
    {
        ResolutionFixture fixture = Resolve(
            "Contents DEFINITIONS ::= BEGIN "
                + "encodingRules OBJECT IDENTIFIER ::= { 1 2 840 113549 }; "
                + "Document ::= SEQUENCE { id INTEGER, text UTF8String }; "
                + "Octets ::= OCTET STRING "
                + "(CONTAINING Document ENCODED BY encodingRules); "
                + "Bits ::= BIT STRING (ENCODED BY { 2 1 1 }); "
                + "TextBits ::= BIT STRING "
                + "(CONTAINING UTF8String (SIZE (2..8))) END",
            false);
        InnerCodecMetadata octets = RequireType(fixture, "Octets")
            .EffectiveInnerCodec ?? throw new InvalidOperationException();
        InnerCodecMetadata bits = RequireType(fixture, "Bits")
            .EffectiveInnerCodec ?? throw new InvalidOperationException();
        InnerCodecMetadata textBits = RequireType(fixture, "TextBits")
            .EffectiveInnerCodec ?? throw new InvalidOperationException();

        Assert.That(octets.ContainedType?.EffectiveKind, Is.EqualTo(AsnTypeKind.Sequence));
        Assert.That(octets.EncodingIdentifier?.Arcs.Select(arc => arc.ToString()),
            Is.EqualTo(new[] { "1", "2", "840", "113549" }));
        Assert.That(octets.EncodingValue?.ReferenceTarget, Is.Not.Null);
        Assert.That(octets.HasEncodingIdentifier, Is.True);
        Assert.That(octets.UsesContainerEncodingRules, Is.False);
        Assert.That(octets.RequiresOctetAlignment, Is.True);
        Assert.That(bits.ContainedType, Is.Null);
        Assert.That(bits.EncodingIdentifier?.Arcs.Select(arc => arc.ToString()),
            Is.EqualTo(new[] { "2", "1", "1" }));
        Assert.That(bits.RequiresOctetAlignment, Is.False);
        Assert.That(textBits.UsesContainerEncodingRules, Is.True);
        Assert.That(textBits.HasEncodingIdentifier, Is.False);
        Assert.That(textBits.ContainedType?.SizeConstraints?.RootLowerBound.ToString(),
            Is.EqualTo("2"));
        Assert.That(textBits.ContainedType?.SizeConstraints?.RootUpperBound.ToString(),
            Is.EqualTo("8"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CompilerRetainsEffectiveContentsMetadataThroughAliases()
    {
        ResolutionFixture fixture = Resolve(
            "Contents DEFINITIONS ::= BEGIN "
                + "Base ::= OCTET STRING (CONTAINING INTEGER); "
                + "Alias ::= Base END",
            false);
        ResolvedTypeSyntax baseType = RequireType(fixture, "Base");
        ResolvedTypeSyntax alias = RequireType(fixture, "Alias");

        Assert.That(baseType.ContentsConstraints, Has.Length.EqualTo(1));
        Assert.That(alias.ContentsConstraints, Has.Length.EqualTo(1));
        Assert.That(alias.EffectiveInnerCodec?.ContainedType?.EffectiveKind,
            Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(alias.EffectiveInnerCodec?.ContainerType, Is.SameAs(alias));
        Assert.That(alias.EffectiveInnerCodec?.RequiresOctetAlignment, Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CompilerKeepsNestedContentsMetadataOnConstrainedComponent()
    {
        ResolutionFixture fixture = Resolve(
            "Contents DEFINITIONS ::= BEGIN "
                + "Envelope ::= SEQUENCE { payload OCTET STRING } "
                + "(WITH COMPONENTS { payload (CONTAINING INTEGER) }) END",
            false);
        ResolvedTypeSyntax envelope = RequireType(fixture, "Envelope");
        NormalizedComponentConstraint component = envelope.ComponentConstraintSets
            .Single().Components.Single();
        NormalizedConstraintNode node = component.ValueConstraint?.RootPartition?
            .Expression ?? throw new InvalidOperationException();
        InnerCodecMetadata metadata = node.CompiledContentsConstraint
            ?? throw new InvalidOperationException();

        Assert.That(envelope.ContentsConstraints, Is.Empty);
        Assert.That(metadata.ContainerType, Is.SameAs(component.ComponentType));
        Assert.That(metadata.ContainedType?.EffectiveKind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(envelope.ConstraintTypes, Does.Contain(metadata.ContainedType));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void CompilerRejectsFurtherConstraintsAndNamedBitContainers()
    {
        ResolutionFixture fixture = Resolve(
            "Contents DEFINITIONS ::= BEGIN "
                + "Base ::= OCTET STRING (CONTAINING INTEGER); "
                + "Further ::= Base (SIZE (1..8)); "
                + "Direct ::= OCTET STRING (SIZE (1..8)) (CONTAINING BOOLEAN); "
                + "Named ::= BIT STRING { active(0) } (CONTAINING INTEGER) END",
            false);
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Count(diagnostic => diagnostic.Code == "ASNC0014"),
            Is.EqualTo(2));
        Assert.That(diagnostics.Count(diagnostic => diagnostic.Code == "ASNC0015"),
            Is.EqualTo(1));
        Assert.That(diagnostics.Where(diagnostic => diagnostic.Code == "ASNC0014"
                || diagnostic.Code == "ASNC0015")
            .All(diagnostic => diagnostic.Span != null), Is.True);
    }

    [Test]
    public void CompilerRejectsInvalidContainersAndEncodingIdentifiers()
    {
        ResolutionFixture fixture = Resolve(
            "Contents DEFINITIONS ::= BEGIN "
                + "notAnOid INTEGER ::= 7; "
                + "Scalar ::= INTEGER (CONTAINING BOOLEAN); "
                + "BadEncoding ::= OCTET STRING (ENCODED BY notAnOid) END",
            false);
        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.That(fixture.Values.Success, Is.False);
        Assert.That(diagnostics.Count(diagnostic => diagnostic.Code == "ASNC0012"),
            Is.EqualTo(1));
        Assert.That(diagnostics.Count(diagnostic => diagnostic.Code == "ASNC0013"),
            Is.EqualTo(1));
        Assert.That(diagnostics.Where(diagnostic => diagnostic.Code == "ASNC0012"
                || diagnostic.Code == "ASNC0013")
            .All(diagnostic => diagnostic.Span != null), Is.True);
    }

    private static ResolvedTypeSyntax RequireType(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0].FindSymbol(name, AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();
    }

    private static ResolutionFixture Resolve(string source, bool enableLegacyAny)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        diagnostics.ApplyCompilerOptions(new AsnCompilerOptions
        {
            EnableLegacyAny = enableLegacyAny
        });
        IncrementalLexSession session = new IncrementalLexSession(
            "contents-constraints.asn",
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
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Tables = tables,
            Values = values
        };
    }

    private class ResolutionFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
        internal ValueResolutionResult Values = null!;
    }
}
