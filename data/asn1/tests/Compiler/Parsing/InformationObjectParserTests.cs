using asn1;

namespace asn1.Tests;

public class InformationObjectParserTests
{
    [Test]
    public void DefaultSyntaxParsesEveryFieldSettingKind()
    {
        ParsedObjectClassSyntax objectClass = ParseClass(
            "CLASS { "
            + "&Type, &id INTEGER, &open &Type, &Codes INTEGER, &OpenSet &Type, "
            + "&operation OPERATION, &Errors ERROR "
            + "}");
        ObjectFixture fixture = ParseObject(
            "{ "
            + "&Type UTF8String, "
            + "&id 7, "
            + "&open payload, "
            + "&Codes { 1 | 2 }, "
            + "&OpenSet { one | two }, "
            + "&operation operationRef, "
            + "&Errors { errorOne | errorTwo } "
            + "}",
            objectClass);

        ParsedInformationObjectSyntax syntax = RequireObject(fixture);
        Assert.That(syntax.DefaultSyntax, Is.True);
        Assert.That(syntax.Object.FieldSettings.Length, Is.EqualTo(7));
        Assert.That(syntax.FieldSettingSyntax[0].TypeSyntax?.Type.Kind,
            Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(syntax.FieldSettingSyntax[1].ValueSyntax?.Value.IntegerValue.ToString(),
            Is.EqualTo("7"));
        Assert.That(syntax.FieldSettingSyntax[2].ValueSyntax?.ReferenceName,
            Is.EqualTo("payload"));
        Assert.That(syntax.FieldSettingSyntax[3].ValueSetSyntax?.RootElement?.Kind,
            Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(syntax.FieldSettingSyntax[4].ValueSetSyntax?.RootElement?.Kind,
            Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(syntax.FieldSettingSyntax[5].ObjectSyntax?.ReferenceName,
            Is.EqualTo("operationRef"));
        Assert.That(syntax.FieldSettingSyntax[6].ObjectSetSyntax?.RootElement?.Kind,
            Is.EqualTo("Union"));
        Assert.That(syntax.Object.FieldSettings[6].ObjectSet?.Objects.Select(item => item.Name),
            Is.EqualTo(new[] { "errorOne", "errorTwo" }));
    }

    [Test]
    public void DefinedSyntaxMatchesLiteralsFieldsAndOptionalGroups()
    {
        ParsedObjectClassSyntax objectClass = ParseClass(
            "CLASS { &Type, &id INTEGER UNIQUE, &Errors ERROR OPTIONAL } "
            + "WITH SYNTAX { TYPE &Type CODE &id [ ERRORS &Errors ] }");
        ObjectFixture complete = ParseObject(
            "{ TYPE UTF8String CODE 7 ERRORS { errorOne | errorTwo } }",
            objectClass);
        ObjectFixture omitted = ParseObject(
            "{ TYPE Payload CODE 8 }",
            objectClass);

        ParsedInformationObjectSyntax completeSyntax = RequireObject(complete);
        ParsedInformationObjectSyntax omittedSyntax = RequireObject(omitted);
        Assert.That(completeSyntax.DefinedSyntax, Is.True);
        Assert.That(completeSyntax.FieldSettingSyntax.Select(item => item.Setting.FieldName),
            Is.EqualTo(new[] { "&Type", "&id", "&Errors" }));
        Assert.That(completeSyntax.FieldSettingSyntax[2].ObjectSetSyntax?.ObjectSet.Objects.Length,
            Is.EqualTo(2));
        Assert.That(omittedSyntax.FieldSettingSyntax.Select(item => item.Setting.FieldName),
            Is.EqualTo(new[] { "&Type", "&id" }));
        Assert.That(completeSyntax.DefinedSyntaxTokens.Select(token => token.Text),
            Is.EqualTo(new[]
            {
                "TYPE", "UTF8String", "CODE", "7", "ERRORS",
                "{", "errorOne", "|", "errorTwo", "}"
            }));
    }

    [Test]
    public void NestedObjectSettingsRetainUnresolvedDefinedSyntax()
    {
        ParsedObjectClassSyntax objectClass = ParseClass("CLASS { &operation OPERATION }");
        ObjectFixture fixture = ParseObject(
            "{ &operation { ARGUMENT Payload CODE 7 } }",
            objectClass);

        ParsedInformationObjectSyntax syntax = RequireObject(fixture);
        ParsedInformationObjectSyntax nested = syntax.FieldSettingSyntax.Single().ObjectSyntax
            ?? throw new InvalidOperationException();
        Assert.That(nested.DefinedSyntax, Is.True);
        Assert.That(nested.DefinedSyntaxTokens.Select(token => token.Text),
            Is.EqualTo(new[] { "ARGUMENT", "Payload", "CODE", "7" }));
    }

    [Test]
    public void ObjectAndObjectSetAssignmentsRetainGovernorsAndValues()
    {
        ParsedObjectClassSyntax objectClass = ParseClass(
            "CLASS { &Type, &code INTEGER } WITH SYNTAX { ARGUMENT &Type CODE &code }");
        AssignmentFixture objectAssignment = ParseObjectAssignment(
            "operation OPERATION ::= { ARGUMENT Payload CODE 7 }",
            objectClass);
        AssignmentFixture setAssignment = ParseObjectSetAssignment(
            "Operations OPERATION ::= { operation | other, ..., extra }",
            objectClass);

        ParsedInformationObjectAssignmentSyntax objectSyntax = RequireAssignment(objectAssignment);
        ParsedInformationObjectAssignmentSyntax setSyntax = RequireAssignment(setAssignment);
        Assert.That(objectSyntax.Assignment.Kind, Is.EqualTo(AsnAssignmentKind.Object));
        Assert.That(objectSyntax.Assignment.Name, Is.EqualTo("operation"));
        Assert.That(objectSyntax.GovernorSyntax?.ReferenceName, Is.EqualTo("OPERATION"));
        Assert.That(objectSyntax.Assignment.Object?.FieldSettings[1].Value?.IntegerValue.ToString(),
            Is.EqualTo("7"));
        Assert.That(setSyntax.Assignment.Kind, Is.EqualTo(AsnAssignmentKind.ObjectSet));
        Assert.That(setSyntax.Assignment.ObjectSet?.Extensible, Is.True);
        Assert.That(setSyntax.Assignment.ObjectSet?.Objects.Select(item => item.Name),
            Is.EqualTo(new[] { "operation", "other", "extra" }));
    }

    [Test]
    public void ObjectSetArithmeticUsesIntersectionAndExclusionPrecedence()
    {
        ObjectSetFixture fixture = ParseObjectSet(
            "{ one | two ^ three EXCEPT four, ..., five }",
            null);

        ParsedInformationObjectSetSyntax syntax = RequireObjectSet(fixture);
        Assert.That(syntax.RootElement?.Kind, Is.EqualTo("Union"));
        Assert.That(syntax.RootElement?.Children[1].Kind, Is.EqualTo("Intersection"));
        Assert.That(syntax.RootElement?.Children[1].Children[1].Kind, Is.EqualTo("Except"));
        Assert.That(syntax.AdditionalElement?.ObjectSyntax?.ReferenceName, Is.EqualTo("five"));
        Assert.That(syntax.ObjectSet.Extensible, Is.True);
    }

    [Test]
    public void ObjectSetsAcceptAllExceptAndSelectionsFromObjects()
    {
        ObjectSetFixture complement = ParseObjectSet("{ ALL EXCEPT Excluded }", null);
        ObjectSetFixture selected = ParseObjectSet(
            "{ operation.&Errors | KnownErrors }",
            null);

        ParsedInformationObjectSetSyntax complementSyntax = RequireObjectSet(complement);
        ParsedInformationObjectSetSyntax selectedSyntax = RequireObjectSet(selected);
        Assert.That(complementSyntax.RootElement?.Kind, Is.EqualTo("AllExcept"));
        Assert.That(complementSyntax.RootElement?.Children.Single().ReferenceName,
            Is.EqualTo("Excluded"));
        Assert.That(selectedSyntax.RootElement?.Kind, Is.EqualTo("Union"));
        Assert.That(selectedSyntax.RootElement?.Children[0].InformationFromObjects?.FieldPath,
            Is.EqualTo(new[] { "&Errors" }));
        Assert.That(selectedSyntax.RootElement?.Children[1].ReferenceName,
            Is.EqualTo("KnownErrors"));
    }

    [TestCase("operation.&code", AsnAssignmentKind.Value, false, "operation", "&code")]
    [TestCase("Operations.&code", AsnAssignmentKind.ValueSet, true, "Operations", "&code")]
    [TestCase("operation.&Type", AsnAssignmentKind.Type, false, "operation", "&Type")]
    [TestCase("operation.&linked", AsnAssignmentKind.Object, false, "operation", "&linked")]
    [TestCase("operation.&Errors", AsnAssignmentKind.ObjectSet, false, "operation", "&Errors")]
    public void InformationFromObjectsClassifiesAllResultForms(
        string source,
        AsnAssignmentKind expectedKind,
        bool expectedSet,
        string expectedReference,
        string expectedField)
    {
        InformationFixture fixture = ParseInformation(source, expectedKind);

        ParsedInformationFromObjectsSyntax syntax = RequireInformation(fixture);
        Assert.That(syntax.CandidateKinds, Is.EqualTo(new[] { expectedKind }));
        Assert.That(syntax.ReferencedObjectSet, Is.EqualTo(expectedSet));
        Assert.That(syntax.ReferenceName, Is.EqualTo(expectedReference));
        Assert.That(syntax.FieldPath, Is.EqualTo(new[] { expectedField }));
    }

    [Test]
    public void InformationFromObjectsRetainsExternalAndLinkedPaths()
    {
        InformationFixture fixture = ParseInformation(
            "Remote.operation.&linked.&code",
            AsnAssignmentKind.Value);

        ParsedInformationFromObjectsSyntax syntax = RequireInformation(fixture);
        Assert.That(syntax.ReferenceModule, Is.EqualTo("Remote"));
        Assert.That(syntax.ReferenceName, Is.EqualTo("operation"));
        Assert.That(syntax.FieldPath, Is.EqualTo(new[] { "&linked", "&code" }));
    }

    [Test]
    public void TypeAndValueParsersAcceptInformationFromObjects()
    {
        LexFixture typeLexical = Lex("operation.&Type");
        TypeParseResult type = new TypeParser(
            typeLexical.Tokens,
            typeLexical.Diagnostics,
            true).ParseTypeSyntax();
        LexFixture valueLexical = Lex("operation.&code");
        ValueParseResult value = new ValueParser(
            valueLexical.Tokens,
            valueLexical.Diagnostics,
            true).ParseValueSyntax(new AsnType { Kind = AsnTypeKind.Integer });

        Assert.That(type.Success, Is.True);
        Assert.That(type.ConsumedTokenCount, Is.EqualTo(typeLexical.Tokens.Length));
        Assert.That(type.Syntax?.InformationFromObjects?.FieldPath,
            Is.EqualTo(new[] { "&Type" }));
        Assert.That(value.Success, Is.True);
        Assert.That(value.ConsumedTokenCount, Is.EqualTo(valueLexical.Tokens.Length));
        Assert.That(value.Syntax?.InformationFromObjects?.FieldPath,
            Is.EqualTo(new[] { "&code" }));
    }

    [TestCase("{ &Type UTF8String, }", "ASNP0179")]
    [TestCase("{ &Type }", "ASNP0186")]
    [TestCase("{ one, two }", "ASNP0193")]
    [TestCase("{ one, ... , }", "ASNP0191")]
    public void MalformedObjectNotationProducesSpecificDiagnostics(
        string source,
        string expectedCode)
    {
        ParsedObjectClassSyntax typeClass = ParseClass("CLASS { &Type }");
        if (source.StartsWith("{ &", StringComparison.Ordinal))
        {
            ObjectFixture fixture = ParseObject(source, typeClass);
            Assert.That(fixture.Result.Success, Is.False);
            Assert.That(fixture.Diagnostics.GetDiagnostics().Select(item => item.Code),
                Does.Contain(expectedCode));
            return;
        }

        ObjectSetFixture setFixture = ParseObjectSet(source, null);
        Assert.That(setFixture.Result.Success, Is.False);
        Assert.That(setFixture.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain(expectedCode));
    }

    private static ParsedObjectClassSyntax ParseClass(string source)
    {
        LexFixture lexical = Lex(source);
        ObjectClassParseResult result = new ObjectClassParser(
            lexical.Tokens,
            lexical.Diagnostics,
            true).ParseObjectClassSyntax();
        Assert.That(result.Success, Is.True);
        Assert.That(result.ConsumedTokenCount, Is.EqualTo(lexical.Tokens.Length));
        Assert.That(lexical.Diagnostics.GetDiagnostics(), Is.Empty);
        return result.Syntax ?? throw new InvalidOperationException();
    }

    private static ObjectFixture ParseObject(
        string source,
        ParsedObjectClassSyntax? objectClass)
    {
        LexFixture lexical = Lex(source);
        return new ObjectFixture
        {
            Tokens = lexical.Tokens,
            Diagnostics = lexical.Diagnostics,
            Result = new InformationObjectParser(
                lexical.Tokens,
                lexical.Diagnostics,
                true).ParseObjectSyntax(objectClass)
        };
    }

    private static ObjectSetFixture ParseObjectSet(
        string source,
        ParsedObjectClassSyntax? objectClass)
    {
        LexFixture lexical = Lex(source);
        return new ObjectSetFixture
        {
            Tokens = lexical.Tokens,
            Diagnostics = lexical.Diagnostics,
            Result = new InformationObjectParser(
                lexical.Tokens,
                lexical.Diagnostics,
                true).ParseObjectSetSyntax(objectClass)
        };
    }

    private static AssignmentFixture ParseObjectAssignment(
        string source,
        ParsedObjectClassSyntax objectClass)
    {
        LexFixture lexical = Lex(source);
        return new AssignmentFixture
        {
            Tokens = lexical.Tokens,
            Diagnostics = lexical.Diagnostics,
            Result = new InformationObjectParser(
                lexical.Tokens,
                lexical.Diagnostics,
                true).ParseObjectAssignmentSyntax(objectClass)
        };
    }

    private static AssignmentFixture ParseObjectSetAssignment(
        string source,
        ParsedObjectClassSyntax objectClass)
    {
        LexFixture lexical = Lex(source);
        return new AssignmentFixture
        {
            Tokens = lexical.Tokens,
            Diagnostics = lexical.Diagnostics,
            Result = new InformationObjectParser(
                lexical.Tokens,
                lexical.Diagnostics,
                true).ParseObjectSetAssignmentSyntax(objectClass)
        };
    }

    private static InformationFixture ParseInformation(
        string source,
        AsnAssignmentKind expectedKind)
    {
        LexFixture lexical = Lex(source);
        return new InformationFixture
        {
            Tokens = lexical.Tokens,
            Diagnostics = lexical.Diagnostics,
            Result = new InformationFromObjectsParser(
                lexical.Tokens,
                lexical.Diagnostics,
                true).ParseInformationFromObjectsSyntax(expectedKind)
        };
    }

    private static ParsedInformationObjectSyntax RequireObject(ObjectFixture fixture)
    {
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        return fixture.Result.Syntax ?? throw new InvalidOperationException();
    }

    private static ParsedInformationObjectSetSyntax RequireObjectSet(ObjectSetFixture fixture)
    {
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        return fixture.Result.Syntax ?? throw new InvalidOperationException();
    }

    private static ParsedInformationObjectAssignmentSyntax RequireAssignment(
        AssignmentFixture fixture)
    {
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        return fixture.Result.Syntax ?? throw new InvalidOperationException();
    }

    private static ParsedInformationFromObjectsSyntax RequireInformation(
        InformationFixture fixture)
    {
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        return fixture.Result.Syntax ?? throw new InvalidOperationException();
    }

    private static LexFixture Lex(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "information-object.asn",
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

        return new LexFixture
        {
            Tokens = tokens.ToArray(),
            Diagnostics = session.Diagnostics
        };
    }

    private class LexFixture
    {
        public AsnToken[] Tokens = [];
        public DiagnosticBag Diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
    }

    private class ObjectFixture
    {
        public AsnToken[] Tokens = [];
        public DiagnosticBag Diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        public InformationObjectParseResult Result = new InformationObjectParseResult();
    }

    private class ObjectSetFixture
    {
        public AsnToken[] Tokens = [];
        public DiagnosticBag Diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        public InformationObjectSetParseResult Result = new InformationObjectSetParseResult();
    }

    private class AssignmentFixture
    {
        public AsnToken[] Tokens = [];
        public DiagnosticBag Diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        public InformationObjectAssignmentParseResult Result =
            new InformationObjectAssignmentParseResult();
    }

    private class InformationFixture
    {
        public AsnToken[] Tokens = [];
        public DiagnosticBag Diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        public InformationFromObjectsParseResult Result = new InformationFromObjectsParseResult();
    }
}
