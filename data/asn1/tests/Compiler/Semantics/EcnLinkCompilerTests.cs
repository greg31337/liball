using asn1;

namespace asn1.Tests;

public class EcnLinkCompilerTests
{
    [Test]
    public void ResolvesFieldLinksAndInstantiatesReferenceDeterminants()
    {
        EcnFixture fixture = Compile(
            "Links ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Packet ::= #CONCATENATION { a #INT, c #BOOL, d #BOOL }\n"
                + "is-present {< REFERENCE : determinant >} #OPTIONAL ::= {\n"
                + "  PRESENCE DETERMINED BY field-to-be-used USING determinant\n"
                + "  MAPPING VALUES { 0 TO absent, 1 TO present } }\n"
                + "packet #Packet ::= { ENCODE STRUCTURE {\n"
                + "  c USE is-present{< a >},\n"
                + "  d USE is-present{< a >} } }\n"
                + "derived #Packet ::= packet\n"
                + "END");

        EcnEncodingObjectDefinition packet = ObjectDefinition(
            fixture,
            "packet");
        EcnEncodingObjectDefinition derived = ObjectDefinition(
            fixture,
            "derived");

        Assert.Multiple(() =>
        {
            Assert.That(packet.Links.Select(link => link.TargetPath?.Text),
                Is.EqualTo(new[] { "c", "d" }));
            Assert.That(packet.Links.All(link => link.Kind
                == EcnEncodingLinkKind.EncodingObject), Is.True);
            Assert.That(packet.Links.All(link => link.ReferenceSymbol?.Name
                == "is-present"), Is.True);
            Assert.That(packet.Determinants.Select(relation => relation
                    .DeterminantPath?.NormalizedText),
                Is.EqualTo(new[] { "a", "a" }));
            Assert.That(packet.Determinants.Select(relation => relation
                    .ControlledPath?.NormalizedText),
                Is.EqualTo(new[] { "c", "d" }));
            Assert.That(packet.Determinants.All(relation => relation.Direction
                == EcnLinkDirection.DeterminantToContent), Is.True);
            Assert.That(packet.Determinants.All(relation => relation.Valid),
                Is.True);
            Assert.That(packet.Determinants.All(relation => relation.Mapping
                ?.Kind == EcnMappingKind.Presence), Is.True);
            Assert.That(derived.Links, Is.Not.SameAs(packet.Links));
            Assert.That(derived.Determinants,
                Is.Not.SameAs(packet.Determinants));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void ResolvesAbsoluteAndNestedStructurePaths()
    {
        EcnFixture fixture = Compile(
            "Paths ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Inner ::= #CONCATENATION { value #INT }\n"
                + "#Outer ::= #CONCATENATION { head #INT, inner #Inner }\n"
                + "sized {< REFERENCE : determinant >} #INT ::= {\n"
                + "  ENCODING-SPACE DETERMINED BY field-to-be-used USING determinant }\n"
                + "outer #Outer ::= { ENCODE STRUCTURE {\n"
                + "  Root.inner.value USE sized{< head >} } }\n"
                + "END");

        EcnEncodingObjectDefinition outer = ObjectDefinition(
            fixture,
            "outer");
        EcnFieldPath target = outer.Links.Single().TargetPath
            ?? throw new AssertionException("The target path is missing.");
        EcnFieldPath determinant = outer.Determinants.Single()
            .DeterminantPath
            ?? throw new AssertionException("The determinant path is missing.");
        EcnFieldPath parentRelative = new EcnLinkCompiler(
            fixture.Diagnostics).ResolveFieldPath("../head", outer, null);

        Assert.Multiple(() =>
        {
            Assert.That(target.Root,
                Is.EqualTo(EcnFieldPathRoot.Absolute));
            Assert.That(target.Segments,
                Is.EqualTo(new[] { "Root", "inner", "value" }));
            Assert.That(target.ResolvedSegments.Select(segment => segment.Name),
                Is.EqualTo(new[] { "inner", "value" }));
            Assert.That(target.TopLevelFieldOrder, Is.EqualTo(1));
            Assert.That(determinant.Root,
                Is.EqualTo(EcnFieldPathRoot.Relative));
            Assert.That(determinant.TopLevelFieldOrder, Is.EqualTo(0));
            Assert.That(target.NormalizedText, Is.EqualTo("inner.value"));
            Assert.That(parentRelative.ParentDepth, Is.EqualTo(1));
            Assert.That(parentRelative.Deferred, Is.True);
            Assert.That(outer.Valid, Is.True);
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void CompilesPresenceChoiceLengthAndValueMappings()
    {
        EcnFixture fixture = Compile(
            "Mappings ENCODING-DEFINITIONS ::= BEGIN\n"
                + "lengthMap {< REFERENCE : determinant >} #INT ::= {\n"
                + "  ENCODING-SPACE DETERMINED BY field-to-be-used USING determinant\n"
                + "  MAPPING VALUES { 0 TO zero, 1 TO one } }\n"
                + "choiceMap {< REFERENCE : determinant >} #ALTERNATIVES ::= {\n"
                + "  ALTERNATIVE DETERMINED BY field-to-be-used USING determinant\n"
                + "  MAPPING DISTRIBUTION { 0 TO small, REMAINDER TO large } }\n"
                + "presenceMap {< REFERENCE : determinant >} #OPTIONAL ::= {\n"
                + "  PRESENCE DETERMINED BY field-to-be-used USING determinant\n"
                + "  MAPPING VALUES { 0 TO absent, 1 TO present } }\n"
                + "repeatMap {< REFERENCE : determinant >} #REPETITION ::= {\n"
                + "  REPETITION-SPACE DETERMINED BY field-to-be-used USING determinant\n"
                + "  MAPPING VALUES { 0 TO empty, 1 TO one } }\n"
                + "valueMap #INT ::= { USE #INT MAPPING ORDERED VALUES }\n"
                + "END");

        EcnValueMapping length = ObjectDefinition(fixture, "lengthMap")
            .Mappings.Single();
        EcnValueMapping choice = ObjectDefinition(fixture, "choiceMap")
            .Mappings.Single();
        EcnValueMapping presence = ObjectDefinition(fixture, "presenceMap")
            .Mappings.Single();
        EcnValueMapping repetition = ObjectDefinition(fixture, "repeatMap")
            .Mappings.Single();
        EcnEncodingObjectDefinition value = ObjectDefinition(
            fixture,
            "valueMap");

        Assert.Multiple(() =>
        {
            Assert.That(length.Kind, Is.EqualTo(EcnMappingKind.Length));
            Assert.That(length.Entries.Select(entry => entry.Source),
                Is.EqualTo(new[] { "0", "1" }));
            Assert.That(length.Entries.Select(entry => entry.Target),
                Is.EqualTo(new[] { "zero", "one" }));
            Assert.That(choice.Kind, Is.EqualTo(EcnMappingKind.Choice));
            Assert.That(choice.Entries[1].Remainder, Is.True);
            Assert.That(presence.Kind, Is.EqualTo(EcnMappingKind.Presence));
            Assert.That(repetition.Kind, Is.EqualTo(EcnMappingKind.Length));
            Assert.That(value.Mappings.Single().Kind,
                Is.EqualTo(EcnMappingKind.Value));
            Assert.That(value.Mappings.Single().Automatic, Is.True);
            Assert.That(value.Links.Single().Kind,
                Is.EqualTo(EcnEncodingLinkKind.EncodingClass));
            Assert.That(value.Links.Single().BuiltIn, Is.True);
            Assert.That(value.Links.Single().Mapping,
                Is.SameAs(value.Mappings.Single()));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void ResolvesParameterizedEncodingLinkTargets()
    {
        EcnFixture fixture = Compile(
            "Parameters ENCODING-DEFINITIONS ::= BEGIN\n"
                + "classParam {< #ENCODING >} #INT ::= { USE #ENCODING }\n"
                + "objectParam {< #INT : encoder >} #INT ::= { USE encoder }\n"
                + "setParam {< #ENCODINGS : choices >} #INT ::= { USE-SET choices }\n"
                + "END");

        EcnEncodingLink classLink = ObjectDefinition(fixture, "classParam")
            .Links.Single();
        EcnEncodingLink objectLink = ObjectDefinition(fixture, "objectParam")
            .Links.Single();
        EcnEncodingLink setLink = ObjectDefinition(fixture, "setParam")
            .Links.Single();

        Assert.Multiple(() =>
        {
            Assert.That(classLink.Kind,
                Is.EqualTo(EcnEncodingLinkKind.EncodingClass));
            Assert.That(classLink.ReferenceSymbol?.ParameterKind,
                Is.EqualTo(EcnParameterKind.EncodingClass));
            Assert.That(objectLink.Kind,
                Is.EqualTo(EcnEncodingLinkKind.EncodingObject));
            Assert.That(objectLink.ReferenceSymbol?.ParameterKind,
                Is.EqualTo(EcnParameterKind.EncodingObject));
            Assert.That(setLink.Kind,
                Is.EqualTo(EcnEncodingLinkKind.EncodingObjectSet));
            Assert.That(setLink.ReferenceSymbol?.ParameterKind,
                Is.EqualTo(EcnParameterKind.EncodingObjectSet));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void SubstitutesReferenceActualsInInheritedDeterminants()
    {
        EcnFixture fixture = Compile(
            "Actuals ENCODING-DEFINITIONS ::= BEGIN\n"
                + "base {< REFERENCE : determinant >} #OPTIONAL ::= {\n"
                + "  PRESENCE DETERMINED BY field-to-be-used USING determinant }\n"
                + "derived {< REFERENCE : actual >} #OPTIONAL ::= base{< actual >}\n"
                + "END");

        EcnEncodingObjectDefinition derived = ObjectDefinition(
            fixture,
            "derived");
        EcnFieldPath path = derived.Determinants.Single().DeterminantPath
            ?? throw new AssertionException("The determinant path is missing.");

        Assert.Multiple(() =>
        {
            Assert.That(path.Text, Is.EqualTo("actual"));
            Assert.That(path.Root, Is.EqualTo(EcnFieldPathRoot.Parameter));
            Assert.That(path.ParameterSymbol?.Name, Is.EqualTo("actual"));
            Assert.That(derived.Valid, Is.True);
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void DiagnosesUnknownPathsTargetsDirectionsCyclesAndConflicts()
    {
        EcnFixture fixture = Compile(
            "InvalidLinks ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Bad ::= #CONCATENATION { payload #INT, length #INT, other #INT }\n"
                + "#Cycle ::= #CONCATENATION { a #INT, b #INT }\n"
                + "#Conflict ::= #CONCATENATION { determinant #INT, first #INT, second #INT }\n"
                + "reader {< REFERENCE : determinant >} #OPTIONAL ::= {\n"
                + "  PRESENCE DETERMINED BY field-to-be-used USING determinant }\n"
                + "writer {< REFERENCE : determinant >} #OPTIONAL ::= {\n"
                + "  PRESENCE DETERMINED BY field-to-be-set USING determinant }\n"
                + "bad #Bad ::= { ENCODE STRUCTURE {\n"
                + "  payload USE reader{< length >},\n"
                + "  missing USE reader{< length >},\n"
                + "  other USE unknownEncoding } }\n"
                + "cyclic #Cycle ::= { ENCODE STRUCTURE {\n"
                + "  a USE reader{< b >},\n"
                + "  b USE reader{< a >} } }\n"
                + "conflicting #Conflict ::= { ENCODE STRUCTURE {\n"
                + "  first USE writer{< determinant >},\n"
                + "  second USE writer{< determinant >} } }\n"
                + "badMapping {< REFERENCE : determinant >} #INT ::= {\n"
                + "  ENCODING-SPACE DETERMINED BY field-to-be-used USING determinant\n"
                + "  MAPPING VALUES { 0..3 TO one, 2..5 TO two, REMAINDER TO rest, 6 TO rest } }\n"
                + "END");

        string[] codes = fixture.Diagnostics.GetDiagnostics()
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(codes, Does.Contain("ASNECN0035"));
            Assert.That(codes, Does.Contain("ASNECN0036"));
            Assert.That(codes, Does.Contain("ASNECN0037"));
            Assert.That(codes, Does.Contain("ASNECN0038"));
            Assert.That(codes, Does.Contain("ASNECN0039"));
            Assert.That(codes, Does.Contain("ASNECN0040"));
            Assert.That(codes, Does.Contain("ASNECN0041"));
            Assert.That(ObjectDefinition(fixture, "bad").Valid, Is.False);
            Assert.That(ObjectDefinition(fixture, "cyclic").Valid, Is.False);
            Assert.That(ObjectDefinition(fixture, "conflicting").Valid,
                Is.False);
            Assert.That(ObjectDefinition(fixture, "badMapping").Valid,
                Is.False);
        });
    }

    private static EcnEncodingObjectDefinition ObjectDefinition(
        EcnFixture fixture,
        string name)
    {
        return fixture.Table.FindSymbol(name, EcnSymbolKind.EncodingObject)
                ?.ObjectDefinition
            ?? throw new AssertionException(
                "The encoding object definition '" + name + "' is missing.");
    }

    private static EcnFixture Compile(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "links.edm",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        System.Collections.Generic.List<AsnToken> tokens =
            new System.Collections.Generic.List<AsnToken>();
        while (true)
        {
            IncrementalLexResult result = session.ScanNext();
            if (result.Status == IncrementalLexStatus.EndOfInput)
            {
                break;
            }

            if (result.Status == IncrementalLexStatus.NeedMoreData)
            {
                throw new AssertionException(
                    "Final ECN input requested more data.");
            }

            if (result.Token != null)
            {
                tokens.Add(result.Token);
            }
        }

        EcnParseResult parsed = new EcnSyntaxParser(tokens.ToArray())
            .ParseModuleSyntax();
        Assert.That(parsed.Success, Is.True, parsed.ErrorMessage);
        Assert.That(session.GetDiagnostics(), Is.Empty);
        EcnModuleSyntax module = parsed.Module
            ?? throw new AssertionException("No ECN module was parsed.");
        EcnSymbolTable table = new EcnSymbolTableBuilder(session.Diagnostics)
            .BuildTable(module, []);
        return new EcnFixture
        {
            Diagnostics = session.Diagnostics,
            Table = table
        };
    }

    private class EcnFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal EcnSymbolTable Table = null!;
    }
}
