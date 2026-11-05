using asn1;

namespace asn1.Tests;

public class ParameterizedParserTests
{
    [Test]
    public void FormalParametersClassifyEveryCategoryAndDummyGovernors()
    {
        LexFixture lexical = Lex(
            "{ TypeParam, OPERATION, INTEGER : valueParam, INTEGER : ValueSetParam, "
            + "ERROR : objectParam, ERROR : ObjectSetParam, "
            + "TypeParam : otherValue, OPERATION : otherObject }");
        ParameterListParseResult result = new ParameterizedParser(
            lexical.Tokens,
            lexical.Diagnostics,
            true).ParseFormalParameterListSyntax();

        Assert.That(result.Success, Is.True);
        Assert.That(result.ConsumedTokenCount, Is.EqualTo(lexical.Tokens.Length));
        Assert.That(result.Parameters.Select(item => item.Parameter.Kind),
            Is.EqualTo(new[]
            {
                AsnParameterKind.Type,
                AsnParameterKind.ObjectClass,
                AsnParameterKind.Value,
                AsnParameterKind.ValueSet,
                AsnParameterKind.Object,
                AsnParameterKind.ObjectSet,
                AsnParameterKind.Value,
                AsnParameterKind.Object
            }));
        Assert.That(result.Parameters[2].GovernorTypeSyntax?.Type.Kind,
            Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(result.Parameters[4].GovernorObjectClassSyntax?.ReferenceName,
            Is.EqualTo("ERROR"));
        Assert.That(result.Parameters[6].DummyGovernorName, Is.EqualTo("TypeParam"));
        Assert.That(result.Parameters[7].DummyGovernorName, Is.EqualTo("OPERATION"));
        Assert.That(lexical.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ActualParametersParseEveryCategoryUsingFormalGovernors()
    {
        ParsedParameterSyntax[] formals = ParseFormals(
            "{ TypeParam, OPERATION, INTEGER : valueParam, INTEGER : ValueSetParam, "
            + "ERROR : objectParam, ERROR : ObjectSetParam }");
        LexFixture lexical = Lex(
            "{ UTF8String, ERROR, 7, { 1 | 2 }, operationRef, "
            + "{ operationRef | otherRef } }");
        ActualParameterListParseResult result = new ParameterizedParser(
            lexical.Tokens,
            lexical.Diagnostics,
            true).ParseActualParameterListSyntax(formals);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ConsumedTokenCount, Is.EqualTo(lexical.Tokens.Length));
        Assert.That(result.Parameters.Select(item => item.Parameter.Kind),
            Is.EqualTo(new[]
            {
                AsnParameterKind.Type,
                AsnParameterKind.ObjectClass,
                AsnParameterKind.Value,
                AsnParameterKind.ValueSet,
                AsnParameterKind.Object,
                AsnParameterKind.ObjectSet
            }));
        Assert.That(result.Parameters[0].Parameter.Type?.Kind, Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(result.Parameters[1].Parameter.ObjectClass?.Name, Is.EqualTo("ERROR"));
        Assert.That(result.Parameters[2].Parameter.Value?.IntegerValue.ToString(), Is.EqualTo("7"));
        Assert.That(result.Parameters[3].Parameter.ValueSetType?.Constraints.Single().Kind,
            Is.EqualTo(AsnConstraintKind.Union));
        Assert.That(result.Parameters[4].Parameter.Object?.Name, Is.EqualTo("operationRef"));
        Assert.That(result.Parameters[5].Parameter.ObjectSet?.Objects.Select(item => item.Name),
            Is.EqualTo(new[] { "operationRef", "otherRef" }));
        Assert.That(lexical.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [TestCase(
        AsnAssignmentKind.Type,
        "Box { TypeParam } ::= SEQUENCE { value TypeParam }",
        "Box")]
    [TestCase(
        AsnAssignmentKind.Value,
        "limit { INTEGER : maximum } INTEGER ::= maximum",
        "limit")]
    [TestCase(
        AsnAssignmentKind.ValueSet,
        "Range { INTEGER : maximum } INTEGER ::= { 0..maximum }",
        "Range")]
    [TestCase(
        AsnAssignmentKind.ObjectClass,
        "WRAPPER { TypeParam } ::= CLASS { &Type DEFAULT TypeParam }",
        "WRAPPER")]
    [TestCase(
        AsnAssignmentKind.Object,
        "operation { INTEGER : code } OPERATION ::= { CODE code }",
        "operation")]
    [TestCase(
        AsnAssignmentKind.ObjectSet,
        "OPERATIONS { OPERATION : op } OPERATION ::= { op | base }",
        "OPERATIONS")]
    public void ParameterizedAssignmentsCoverEveryAssignmentCategory(
        AsnAssignmentKind kind,
        string source,
        string expectedName)
    {
        AssignmentFixture fixture = ParseAssignment(source, kind);

        ParsedParameterizedAssignmentSyntax syntax = RequireAssignment(fixture);
        Assert.That(syntax.Assignment.Kind, Is.EqualTo(kind));
        Assert.That(syntax.Assignment.Name, Is.EqualTo(expectedName));
        Assert.That(syntax.Assignment.Parameters, Is.Not.Empty);
    }

    [Test]
    public void ParameterizedValueSetTypeRetainsGovernorAndElementSet()
    {
        AssignmentFixture fixture = ParseAssignment(
            "Range { INTEGER : lower, INTEGER : upper } INTEGER ::= { lower..upper }",
            AsnAssignmentKind.ValueSet);

        ParsedParameterizedAssignmentSyntax syntax = RequireAssignment(fixture);
        Assert.That(syntax.GovernorTypeSyntax?.Type.Kind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(syntax.ValueSetSyntax?.RootElement?.Kind, Is.EqualTo(AsnConstraintKind.ValueRange));
        Assert.That(syntax.Assignment.Type?.Constraints.Single().Kind,
            Is.EqualTo(AsnConstraintKind.ValueRange));
        Assert.That(syntax.ValueSetTokens.Select(token => token.Text),
            Is.EqualTo(new[] { "{", "lower", "..", "upper", "}" }));
    }

    [TestCase(AsnParameterKind.Type, "Template")]
    [TestCase(AsnParameterKind.Value, "templateValue")]
    [TestCase(AsnParameterKind.ValueSet, "TemplateSet")]
    [TestCase(AsnParameterKind.ObjectClass, "TEMPLATE-CLASS")]
    [TestCase(AsnParameterKind.Object, "templateObject")]
    [TestCase(AsnParameterKind.ObjectSet, "TemplateObjects")]
    public void ParameterizedReferencesCoverEveryReferenceCategory(
        AsnParameterKind kind,
        string reference)
    {
        ParsedParameterSyntax[] formals = ParseFormals(
            "{ TypeParam, OPERATION, INTEGER : value, INTEGER : Values, "
            + "ERROR : object, ERROR : Objects }");
        LexFixture lexical = Lex(
            reference
            + " { UTF8String, ERROR, 7, { 1 | 2 }, operationRef, { operationRef } }");
        ParameterizedReferenceParseResult result = new ParameterizedParser(
            lexical.Tokens,
            lexical.Diagnostics,
            true).ParseParameterizedReferenceSyntax(kind, formals);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ConsumedTokenCount, Is.EqualTo(lexical.Tokens.Length));
        Assert.That(result.Syntax?.Kind, Is.EqualTo(kind));
        Assert.That(result.Syntax?.ReferenceName, Is.EqualTo(reference));
        Assert.That(result.Syntax?.ActualParameters.Length, Is.EqualTo(6));
        Assert.That(lexical.Diagnostics.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void ParameterizedReferencesRetainExternalQualification()
    {
        ParsedParameterSyntax[] formals = ParseFormals("{ TypeParam }");
        LexFixture lexical = Lex("Remote.Template { INTEGER }");
        ParameterizedReferenceParseResult result = new ParameterizedParser(
            lexical.Tokens,
            lexical.Diagnostics,
            true).ParseParameterizedReferenceSyntax(AsnParameterKind.Type, formals);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Syntax?.ReferenceModule, Is.EqualTo("Remote"));
        Assert.That(result.Syntax?.ReferenceName, Is.EqualTo("Template"));
        Assert.That(result.Syntax?.ActualParameters.Single().Type?.Kind,
            Is.EqualTo(AsnTypeKind.Integer));
    }

    [Test]
    public void ParameterizedReferencesAreIntegratedIntoEveryExistingParserPath()
    {
        LexFixture typeLexical = Lex("Template { INTEGER }");
        TypeParseResult type = new TypeParser(
            typeLexical.Tokens,
            typeLexical.Diagnostics,
            true).ParseTypeSyntax();
        LexFixture valueLexical = Lex("templateValue { INTEGER }");
        ValueParseResult value = new ValueParser(
            valueLexical.Tokens,
            valueLexical.Diagnostics,
            true).ParseValueSyntax(null);
        LexFixture classLexical = Lex("TEMPLATE-CLASS { INTEGER }");
        ObjectClassParseResult objectClass = new ObjectClassParser(
            classLexical.Tokens,
            classLexical.Diagnostics,
            true).ParseObjectClassSyntax();
        LexFixture objectLexical = Lex("templateObject { INTEGER }");
        InformationObjectParseResult informationObject = new InformationObjectParser(
            objectLexical.Tokens,
            objectLexical.Diagnostics,
            true).ParseObjectSyntax(null);
        LexFixture setLexical = Lex("TemplateObjects { INTEGER }");
        InformationObjectSetParseResult objectSet = new InformationObjectParser(
            setLexical.Tokens,
            setLexical.Diagnostics,
            true).ParseObjectSetSyntax(null);

        Assert.That(type.Syntax?.ParameterizedReference?.ReferenceName, Is.EqualTo("Template"));
        Assert.That(value.Syntax?.ParameterizedReference?.ReferenceName,
            Is.EqualTo("templateValue"));
        Assert.That(objectClass.Syntax?.ParameterizedReference?.ReferenceName,
            Is.EqualTo("TEMPLATE-CLASS"));
        Assert.That(informationObject.Syntax?.ParameterizedReference?.ReferenceName,
            Is.EqualTo("templateObject"));
        Assert.That(objectSet.Syntax?.ParameterizedReference?.ReferenceName,
            Is.EqualTo("TemplateObjects"));
        Assert.That(type.ConsumedTokenCount, Is.EqualTo(typeLexical.Tokens.Length));
        Assert.That(value.ConsumedTokenCount, Is.EqualTo(valueLexical.Tokens.Length));
        Assert.That(objectClass.ConsumedTokenCount, Is.EqualTo(classLexical.Tokens.Length));
        Assert.That(informationObject.ConsumedTokenCount, Is.EqualTo(objectLexical.Tokens.Length));
        Assert.That(objectSet.ConsumedTokenCount, Is.EqualTo(setLexical.Tokens.Length));
    }

    [Test]
    public void ParameterizedValueSetTypesAreAcceptedAsFieldSettings()
    {
        LexFixture classLexical = Lex("CLASS { &Values INTEGER }");
        ObjectClassParseResult objectClass = new ObjectClassParser(
            classLexical.Tokens,
            classLexical.Diagnostics,
            true).ParseObjectClassSyntax();
        LexFixture objectLexical = Lex("{ &Values TemplateValues { INTEGER } }");
        InformationObjectParseResult informationObject = new InformationObjectParser(
            objectLexical.Tokens,
            objectLexical.Diagnostics,
            true).ParseObjectSyntax(objectClass.Syntax);

        Assert.That(informationObject.Success, Is.True);
        Assert.That(informationObject.ConsumedTokenCount, Is.EqualTo(objectLexical.Tokens.Length));
        Assert.That(
            informationObject.Syntax?.FieldSettingSyntax.Single().ParameterizedReference?.Kind,
            Is.EqualTo(AsnParameterKind.ValueSet));
        Assert.That(
            informationObject.Syntax?.FieldSettingSyntax.Single().ParameterizedReference?.ReferenceName,
            Is.EqualTo("TemplateValues"));
    }

    [TestCase("{}", "ASNP0201")]
    [TestCase("{ INTEGER : }", "ASNP0205")]
    [TestCase("{ T, }", "ASNP0203")]
    public void MalformedFormalListsProduceSpecificDiagnostics(
        string source,
        string expectedCode)
    {
        LexFixture lexical = Lex(source);
        ParameterListParseResult result = new ParameterizedParser(
            lexical.Tokens,
            lexical.Diagnostics,
            true).ParseFormalParameterListSyntax();

        Assert.That(result.Success, Is.False);
        Assert.That(lexical.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain(expectedCode));
    }

    [Test]
    public void ActualParameterArityMismatchIsDiagnosed()
    {
        ParsedParameterSyntax[] formals = ParseFormals("{ TypeParam, OtherType }");
        LexFixture lexical = Lex("{ INTEGER }");
        ActualParameterListParseResult result = new ParameterizedParser(
            lexical.Tokens,
            lexical.Diagnostics,
            true).ParseActualParameterListSyntax(formals);

        Assert.That(result.Success, Is.False);
        Assert.That(lexical.Diagnostics.GetDiagnostics().Select(item => item.Code),
            Does.Contain("ASNP0211"));
    }

    private static ParsedParameterSyntax[] ParseFormals(string source)
    {
        LexFixture lexical = Lex(source);
        ParameterListParseResult result = new ParameterizedParser(
            lexical.Tokens,
            lexical.Diagnostics,
            true).ParseFormalParameterListSyntax();
        Assert.That(result.Success, Is.True);
        Assert.That(result.ConsumedTokenCount, Is.EqualTo(lexical.Tokens.Length));
        Assert.That(lexical.Diagnostics.GetDiagnostics(), Is.Empty);
        return result.Parameters;
    }

    private static AssignmentFixture ParseAssignment(
        string source,
        AsnAssignmentKind kind)
    {
        LexFixture lexical = Lex(source);
        return new AssignmentFixture
        {
            Tokens = lexical.Tokens,
            Diagnostics = lexical.Diagnostics,
            Result = new ParameterizedParser(
                lexical.Tokens,
                lexical.Diagnostics,
                true).ParseParameterizedAssignmentSyntax(kind)
        };
    }

    private static ParsedParameterizedAssignmentSyntax RequireAssignment(
        AssignmentFixture fixture)
    {
        Assert.That(fixture.Result.Success, Is.True);
        Assert.That(fixture.Result.ConsumedTokenCount, Is.EqualTo(fixture.Tokens.Length));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        return fixture.Result.Syntax ?? throw new InvalidOperationException();
    }

    private static LexFixture Lex(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "parameterized.asn",
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

    private class AssignmentFixture
    {
        public AsnToken[] Tokens = [];
        public DiagnosticBag Diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        public ParameterizedAssignmentParseResult Result = new ParameterizedAssignmentParseResult();
    }
}
