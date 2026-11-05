using asn1;

namespace asn1.Tests;

public class EcnEncodingClassCompilerTests
{
    [Test]
    public void CompilesAliasesAndCompositeEncodingStructures()
    {
        EcnFixture fixture = Compile(
            "Structures ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#Length ::= #INT\n"
                + "#Choice ::= #ALTERNATIVES { small #INT, large #OCTETS (SIZE (1..8)) }\n"
                + "#Items ::= #REPETITION { item #Choice } (SIZE (1..4))\n"
                + "#Packet ::= #CONCATENATION {\n"
                + "  length #Length,\n"
                + "  payload #OCTETS OPTIONAL-ENCODING #OPTIONAL,\n"
                + "  items #Items\n"
                + "}\n"
                + "END");

        EcnEncodingClassDefinition length = Definition(fixture, "#Length");
        EcnEncodingStructure choice = Structure(fixture, "#Choice");
        EcnEncodingStructure items = Structure(fixture, "#Items");
        EcnEncodingStructure packet = Structure(fixture, "#Packet");

        Assert.Multiple(() =>
        {
            Assert.That(length.Kind,
                Is.EqualTo(EcnEncodingClassDefinitionKind.Alias));
            Assert.That(length.AliasClassReference, Is.EqualTo("#INT"));
            Assert.That(length.AliasBuiltIn, Is.True);
            Assert.That(length.Valid, Is.True);

            Assert.That(choice.Kind,
                Is.EqualTo(EcnEncodingStructureKind.Alternatives));
            Assert.That(choice.Fields.Select(field => field.Name),
                Is.EqualTo(new[] { "small", "large" }));
            Assert.That(choice.Fields[1].Structure?.ConstraintTokens,
                Is.Not.Empty);

            Assert.That(items.Kind,
                Is.EqualTo(EcnEncodingStructureKind.Repetition));
            Assert.That(items.ElementName, Is.EqualTo("item"));
            Assert.That(items.Element?.ClassReference, Is.EqualTo("#Choice"));
            Assert.That(items.ConstraintTokens, Is.Not.Empty);

            Assert.That(packet.Kind,
                Is.EqualTo(EcnEncodingStructureKind.Concatenation));
            Assert.That(packet.Fields.Select(field => field.Name),
                Is.EqualTo(new[] { "length", "payload", "items" }));
            Assert.That(packet.Fields[1].Optional, Is.True);
            Assert.That(packet.Fields[1].OptionalClassReference,
                Is.EqualTo("#OPTIONAL"));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void CompilesTaggedStructuresAndDerivedConstructorClasses()
    {
        EcnFixture fixture = Compile(
            "Tagged ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#MyConcatenation ::= #CONCATENATION\n"
                + "#MyTag ::= #TAG\n"
                + "#TaggedInteger ::= [ #MyTag (3) ] #INT\n"
                + "#Packet ::= #MyConcatenation { value #TaggedInteger }\n"
                + "END");

        EcnEncodingStructure tagged = Structure(fixture, "#TaggedInteger");
        EcnEncodingStructure packet = Structure(fixture, "#Packet");

        Assert.Multiple(() =>
        {
            Assert.That(tagged.Kind,
                Is.EqualTo(EcnEncodingStructureKind.Tagged));
            Assert.That(tagged.ClassReference, Is.EqualTo("#MyTag"));
            Assert.That(tagged.ConstraintTokens, Is.Not.Empty);
            Assert.That(tagged.InnerStructure?.Kind,
                Is.EqualTo(EcnEncodingStructureKind.Field));
            Assert.That(tagged.InnerStructure?.PrimitiveClassName,
                Is.EqualTo("#INT"));
            Assert.That(packet.Kind,
                Is.EqualTo(EcnEncodingStructureKind.Concatenation));
            Assert.That(packet.ClassReference, Is.EqualTo("#MyConcatenation"));
            Assert.That(packet.Fields[0].Structure?.ClassReference,
                Is.EqualTo("#TaggedInteger"));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void DiagnosesInvalidCategoriesAndDuplicateFields()
    {
        EcnFixture fixture = Compile(
            "Invalid ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#BadConstructor ::= #INT { first #INT, second #BOOL }\n"
                + "#BadField ::= #CONCATENATION { option #OPTIONAL }\n"
                + "#BadOptional ::= #CONCATENATION { value #INT OPTIONAL-ENCODING #BOOL }\n"
                + "#BadTag ::= [ #INT (1) ] #BOOL\n"
                + "#Duplicate ::= #CONCATENATION { same #INT, same #BOOL }\n"
                + "#ProcedureAlias ::= #TRANSFORM\n"
                + "END");

        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.Select(item => item.Code),
                Does.Contain("ASNECN0012"));
            Assert.That(diagnostics.Select(item => item.Code),
                Does.Contain("ASNECN0013"));
            Assert.That(diagnostics.Select(item => item.Code),
                Does.Contain("ASNECN0015"));
            Assert.That(diagnostics.Select(item => item.Code),
                Does.Contain("ASNECN0016"));
            Assert.That(diagnostics.Single(item => item.Code == "ASNECN0013")
                .RelatedLocations, Has.Length.EqualTo(1));
            Assert.That(Definition(fixture, "#ProcedureAlias").Valid, Is.False);
        });
    }

    [Test]
    public void DiagnosesMalformedCompositeStructuresAndUnknownClasses()
    {
        EcnFixture fixture = Compile(
            "Malformed ENCODING-DEFINITIONS ::= BEGIN\n"
                + "#OneChoice ::= #ALTERNATIVES { only #INT }\n"
                + "#EmptyRecord ::= #CONCATENATION { }\n"
                + "#UnknownField ::= #CONCATENATION { value #Missing }\n"
                + "END");

        AsnDiagnostic[] diagnostics = fixture.Diagnostics.GetDiagnostics();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics.Count(item => item.Code == "ASNECN0010"),
                Is.EqualTo(2));
            Assert.That(diagnostics.Count(item => item.Code == "ASNECN0011"),
                Is.EqualTo(1));
            Assert.That(Definition(fixture, "#OneChoice").Valid, Is.False);
            Assert.That(Definition(fixture, "#EmptyRecord").Valid, Is.False);
            Assert.That(Definition(fixture, "#UnknownField").Valid, Is.False);
        });
    }

    private static EcnEncodingClassDefinition Definition(
        EcnFixture fixture,
        string name)
    {
        return fixture.Table.FindClassSymbol(name)?.ClassDefinition
            ?? throw new AssertionException(
                "The encoding class definition '" + name + "' is missing.");
    }

    private static EcnEncodingStructure Structure(
        EcnFixture fixture,
        string name)
    {
        return Definition(fixture, name).Structure
            ?? throw new AssertionException(
                "The encoding structure '" + name + "' is missing.");
    }

    private static EcnFixture Compile(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "encoding-classes.edm",
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
