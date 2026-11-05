using asn1;

namespace asn1.Tests;

public class ObjectClassParserTests
{
    [Test]
    public void ParserClassifiesEveryObjectClassFieldSpecification()
    {
        ObjectClassFixture fixture = ParseObjectClass(
            "CLASS { "
            + "&Type DEFAULT UTF8String, "
            + "&id INTEGER UNIQUE, "
            + "&flag BOOLEAN DEFAULT TRUE, "
            + "&open &Type OPTIONAL, "
            + "&Codes INTEGER DEFAULT { 1 | 2 }, "
            + "&OpenSet &Type DEFAULT { one, two }, "
            + "&operation OPERATION OPTIONAL, "
            + "&Errors ERROR DEFAULT { errorOne } "
            + "}");

        ParsedObjectClassSyntax syntax = RequireSyntax(fixture);
        Assert.That(
            syntax.ObjectClass.Fields.Select(field => field.Kind),
            Is.EqualTo(new[]
            {
                AsnObjectFieldKind.Type,
                AsnObjectFieldKind.FixedTypeValue,
                AsnObjectFieldKind.FixedTypeValue,
                AsnObjectFieldKind.VariableTypeValue,
                AsnObjectFieldKind.FixedTypeValueSet,
                AsnObjectFieldKind.VariableTypeValueSet,
                AsnObjectFieldKind.Object,
                AsnObjectFieldKind.ObjectSet
            }));
        Assert.That(syntax.ObjectClass.Fields[0].DefaultSetting?.Type?.Kind,
            Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(syntax.ObjectClass.Fields[1].Type?.Kind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(syntax.ObjectClass.Fields[1].Unique, Is.True);
        Assert.That(syntax.ObjectClass.Fields[2].DefaultSetting?.Value?.TextValue, Is.EqualTo("TRUE"));
        Assert.That(syntax.ObjectClass.Fields[3].Optional, Is.True);
        Assert.That(syntax.ObjectClass.Fields[6].Optional, Is.True);
        Assert.That(syntax.ObjectClass.Fields[7].DefaultSetting?.ObjectSet?.Name,
            Is.EqualTo("{ errorOne }"));
    }

    [Test]
    public void ParserRetainsFixedAndVariableGovernorsAndDefaultSyntax()
    {
        ObjectClassFixture fixture = ParseObjectClass(
            "CLASS { "
            + "&value PayloadType DEFAULT payload, "
            + "&variable &Container.&Type DEFAULT 7, "
            + "&objects Remote.ERROR DEFAULT { first, second } "
            + "}");

        ParsedObjectClassSyntax syntax = RequireSyntax(fixture);
        Assert.That(syntax.FieldSyntax[0].GovernorTypeSyntax?.ReferenceName,
            Is.EqualTo("PayloadType"));
        Assert.That(syntax.FieldSyntax[0].DefaultValueSyntax?.ReferenceName,
            Is.EqualTo("payload"));
        Assert.That(syntax.FieldSyntax[1].GovernorFieldPath,
            Is.EqualTo(new[] { "&Container", "&Type" }));
        Assert.That(syntax.FieldSyntax[1].DefaultValueSyntax?.Value.IntegerValue.ToString(),
            Is.EqualTo("7"));
        Assert.That(syntax.FieldSyntax[2].GovernorObjectClassModule, Is.EqualTo("Remote"));
        Assert.That(syntax.FieldSyntax[2].GovernorObjectClassName, Is.EqualTo("ERROR"));
        Assert.That(syntax.FieldSyntax[2].DefaultTokens.Select(token => token.Text),
            Is.EqualTo(new[] { "{", "first", ",", "second", "}" }));
    }

    [Test]
    public void WithSyntaxBuildsNestedOptionalGroupTemplate()
    {
        ObjectClassFixture fixture = ParseObjectClass(
            "CLASS { &Type, &id INTEGER UNIQUE, &flag BOOLEAN OPTIONAL, &Errors ERROR OPTIONAL } "
            + "WITH SYNTAX { &Type IDENTIFIED BY &id [ HAS [ FLAG &flag ] ] [[ ERRORS &Errors ]] }");

        ParsedObjectClassSyntax syntax = RequireSyntax(fixture);
        Assert.That(syntax.SyntaxTemplate.Length, Is.EqualTo(6));
        Assert.That(syntax.SyntaxTemplate[0].FieldReference, Is.True);
        Assert.That(syntax.SyntaxTemplate[1].Text, Is.EqualTo("IDENTIFIED"));
        Assert.That(syntax.SyntaxTemplate[5].OptionalGroup, Is.True);
        Assert.That(syntax.SyntaxTemplate[5].Children.Single().OptionalGroup, Is.True);
        Assert.That(
            syntax.SyntaxTemplate[5].Children.Single().Children.Select(item => item.Text),
            Is.EqualTo(new[] { "ERRORS", "&Errors" }));
        Assert.That(syntax.ObjectClass.SyntaxTokens,
            Is.EqualTo(new[]
            {
                "&Type", "IDENTIFIED", "BY", "&id",
                "[", "HAS", "[", "FLAG", "&flag", "]", "]",
                "[", "[", "ERRORS", "&Errors", "]", "]"
            }));
    }

    [TestCase("TYPE-IDENTIFIER", "", "TYPE-IDENTIFIER")]
    [TestCase("Remote.ABSTRACT-SYNTAX", "Remote", "ABSTRACT-SYNTAX")]
    public void DefinedObjectClassReferencesRetainQualification(
        string source,
        string expectedModule,
        string expectedName)
    {
        ObjectClassFixture fixture = ParseObjectClass(source);

        ParsedObjectClassSyntax syntax = RequireSyntax(fixture);
        Assert.That(syntax.ReferenceModule, Is.EqualTo(expectedModule));
        Assert.That(syntax.ReferenceName, Is.EqualTo(expectedName));
        Assert.That(syntax.ObjectClass.Name,
            Is.EqualTo(expectedModule.Length == 0 ? expectedName : expectedModule + "." + expectedName));
    }

    [TestCase("CLASS {}", "ASNP0156")]
    [TestCase("CLASS { &id }", "ASNP0162")]
    [TestCase("CLASS { &id INTEGER DEFAULT }", "ASNP0164")]
    [TestCase("CLASS { &Type, }", "ASNP0157")]
    [TestCase("CLASS { &Type } WITH VALUE { &Type }", "ASNP0166")]
    [TestCase("CLASS { &Type } WITH SYNTAX {}", "ASNP0169")]
    [TestCase("CLASS { &Type } WITH SYNTAX { ] }", "ASNP0170")]
    [TestCase("CLASS { &Type } WITH SYNTAX { [ &Type }", "ASNP0171")]
    public void MalformedObjectClassesProduceSpecificDiagnostics(
        string source,
        string expectedCode)
    {
        ObjectClassFixture fixture = ParseObjectClass(source);

        Assert.That(fixture.Result.Success, Is.False);
        Assert.That(fixture.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain(expectedCode));
    }

    private static ParsedObjectClassSyntax RequireSyntax(ObjectClassFixture fixture)
    {
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Result.Syntax, Is.Not.Null);
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        return fixture.Result.Syntax ?? throw new InvalidOperationException();
    }

    private static ObjectClassFixture ParseObjectClass(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "object-class.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        List<AsnToken> tokens = new List<AsnToken>();
        while (true)
        {
            IncrementalLexResult lexical = session.ScanNext();
            if (lexical.Status == IncrementalLexStatus.EndOfInput)
            {
                break;
            }

            Assert.That(lexical.Status, Is.EqualTo(IncrementalLexStatus.Token));
            AsnToken token = lexical.Token ?? throw new InvalidOperationException();
            if (token.Kind != AsnTokenKind.Whitespace
                && token.Kind != AsnTokenKind.Comment
                && token.Kind != AsnTokenKind.EndOfInput)
            {
                tokens.Add(token);
            }
        }

        AsnToken[] tokenArray = tokens.ToArray();
        return new ObjectClassFixture
        {
            Tokens = tokenArray,
            Diagnostics = session.Diagnostics,
            Result = new ObjectClassParser(
                tokenArray,
                session.Diagnostics,
                true).ParseObjectClassSyntax()
        };
    }

    private class ObjectClassFixture
    {
        public AsnToken[] Tokens = [];
        public DiagnosticBag Diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        public ObjectClassParseResult Result = new ObjectClassParseResult();
    }
}
