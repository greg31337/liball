using asn1;

namespace asn1.Tests;

public class EcnSyntaxParserTests
{
    [Test]
    public void EncodingDefinitionModuleParsesEveryAssignmentAndParameterForm()
    {
        EcnParseFixture fixture = ParseEcn(
            "Example { iso(1) 2 } ENCODING-DEFINITIONS ::= BEGIN\n"
                + "EXPORTS Encodings;\n"
                + "IMPORTS #Imported FROM Abstract { iso(1) 3 };\n"
                + "RENAMES #Imported AS #Local IN ALL;\n"
                + "#Small {< INTEGER : limit, REFERENCE : path >} ::= #INT (0..limit)\n"
                + "valueEncoding {< REFERENCE : determinant >} #Small ::= {\n"
                + "  ENCODING-SPACE SIZE determinant\n"
                + "  DETERMINED BY field-to-be-set USING Root.length\n"
                + "  MAPPING DISTRIBUTION { 0..7 TO small, REMAINDER TO large }\n"
                + "  TRANSFORMS {{INT-TO-INT divide:2}, {INT-TO-INT decrement:1}}\n"
                + "  IF determinant }\n"
                + "Encodings #ENCODINGS ::= { valueEncoding | fallbackEncoding }\n"
                + "  COMPLETED BY PER-BASIC-UNALIGNED\n"
                + "END");

        Assert.That(fixture.Result.Success, Is.True,
            fixture.Result.ErrorMessage);
        EcnModuleSyntax module = fixture.Result.Module
            ?? throw new AssertionException("No ECN module was parsed.");
        EcnAssignmentSyntax encodingClass = module.Assignments[0];
        EcnAssignmentSyntax encodingObject = module.Assignments[1];
        EcnAssignmentSyntax encodingSet = module.Assignments[2];

        Assert.Multiple(() =>
        {
            Assert.That(module.Name, Is.EqualTo("Example"));
            Assert.That(module.LinkDefinitions, Is.False);
            Assert.That(module.Exports, Is.EqualTo(new[] { "Encodings" }));
            Assert.That(module.Imports, Does.Contain("#Imported"));
            Assert.That(module.RenameTokens.Select(token => token.Text),
                Is.EqualTo(new[] { "#Imported", "AS", "#Local", "IN", "ALL" }));
            Assert.That(module.Assignments, Has.Length.EqualTo(3));
            Assert.That(encodingClass.Kind,
                Is.EqualTo(EcnAssignmentKind.EncodingClass));
            Assert.That(encodingClass.Reference, Is.EqualTo("#Small"));
            Assert.That(encodingClass.Parameters.Select(item => item.Governor),
                Is.EqualTo(new[] { "INTEGER", "REFERENCE" }));
            Assert.That(encodingClass.Parameters.Select(item => item.Reference),
                Is.EqualTo(new[] { "limit", "path" }));
            Assert.That(encodingObject.Kind,
                Is.EqualTo(EcnAssignmentKind.EncodingObject));
            Assert.That(encodingObject.Governor, Is.EqualTo("#Small"));
            Assert.That(encodingObject.Parameters.Single().Reference,
                Is.EqualTo("determinant"));
            Assert.That(encodingSet.Kind,
                Is.EqualTo(EcnAssignmentKind.EncodingObjectSet));
            Assert.That(encodingSet.Governor, Is.EqualTo("#ENCODINGS"));
            Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void ObjectBodyRetainsPropertiesLinksPathsMappingsConditionsAndOrdering()
    {
        EcnParseFixture fixture = ParseEcn(
            "Clauses ENCODING-DEFINITIONS ::= BEGIN\n"
                + "encoding #Payload ::= {\n"
                + "  ENCODING-SPACE SIZE 8\n"
                + "  DETERMINED BY field-to-be-set USING Container.length\n"
                + "  MAPPING VALUES { 0 TO false, 1 TO true }\n"
                + "  TRANSFORMS {{first #TRANSFORM}, {second #TRANSFORM}}\n"
                + "  STRUCTURE #INT\n"
                + "  IF Container.enabled }\n"
                + "END");
        EcnAssignmentSyntax assignment = RequireSingleAssignment(fixture);

        Assert.Multiple(() =>
        {
            Assert.That(assignment.Clauses.Select(item => item.Kind),
                Does.Contain(EcnClauseKind.Property));
            Assert.That(assignment.Clauses.Select(item => item.Kind),
                Does.Contain(EcnClauseKind.Link));
            Assert.That(assignment.Clauses.Select(item => item.Kind),
                Does.Contain(EcnClauseKind.Path));
            Assert.That(assignment.Clauses.Select(item => item.Kind),
                Does.Contain(EcnClauseKind.Mapping));
            Assert.That(assignment.Clauses.Select(item => item.Kind),
                Does.Contain(EcnClauseKind.Condition));
            Assert.That(assignment.Clauses.Select(item => item.Kind),
                Does.Contain(EcnClauseKind.OrderedEncodingObjectList));
            Assert.That(assignment.Clauses.Select(item => item.Kind),
                Does.Contain(EcnClauseKind.EncodingStructure));
            Assert.That(assignment.Clauses.Select(item => item.Kind)
                    .Distinct(),
                Is.EquivalentTo(System.Enum.GetValues<EcnClauseKind>()
                    .Where(item => item != EcnClauseKind.NonEcn)));
            Assert.That(assignment.Clauses.Single(item => item.Kind
                == EcnClauseKind.Mapping).MappingEntries, Has.Length.EqualTo(2));
            Assert.That(assignment.Clauses.Single(item => item.Kind
                == EcnClauseKind.OrderedEncodingObjectList).Items,
                Has.Length.EqualTo(2));
            Assert.That(assignment.Clauses.Where(item => item.Kind
                    == EcnClauseKind.Path)
                .SelectMany(item => item.Paths),
                Does.Contain("Container.length"));
        });
    }

    [Test]
    public void LinkModuleAndNonEcnDefinitionAreRetained()
    {
        string raw = "\nextern int decode(void) { return 7; }\n";
        EcnParseFixture fixture = ParseEcn(
            "Links LINK-DEFINITIONS ::= BEGIN\n"
                + "external #Payload ::= NON-ECN-BEGIN"
                + raw
                + "NON-ECN-END\n"
                + "END");
        EcnAssignmentSyntax assignment = RequireSingleAssignment(fixture);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Result.Module?.LinkDefinitions, Is.True);
            Assert.That(assignment.BodyTokens.Single(token => token.Kind
                == AsnTokenKind.NonEcnText).Text, Is.EqualTo(raw));
            Assert.That(assignment.Clauses.Single().Kind,
                Is.EqualTo(EcnClauseKind.NonEcn));
            Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void AsnModuleEncodingControlSectionParsesEcnAssignments()
    {
        string source = "Controls DEFINITIONS ::= BEGIN\n"
            + "Value ::= INTEGER\n"
            + "ENCODING-CONTROL ECN\n"
            + "#Small ::= #INT (0..255)\n"
            + "valueEncoding #Small ::= { SIZE 8 }\n"
            + "END";
        IncrementalLexSession session = Lex(source);
        ModuleParseResult parsed = new ModuleParser(
            new TokenCursor(session, 4)).ParseModule();
        ModuleSyntax module = parsed.Module
            ?? throw new AssertionException("No ASN.1 module was parsed.");

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(ModuleParseStatus.Parsed));
            Assert.That(module.EncodingControlSections.Single()
                .EncodingReference, Is.EqualTo("ECN"));
            Assert.That(module.EncodingControlSections.Single()
                .EcnAssignments.Select(item => item.Kind), Is.EqualTo(new[]
                {
                    EcnAssignmentKind.EncodingClass,
                    EcnAssignmentKind.EncodingObject
                }));
            Assert.That(session.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void UnterminatedAssignmentGroupReturnsAParseFailure()
    {
        EcnParseFixture fixture = ParseEcn(
            "Broken ENCODING-DEFINITIONS ::= BEGIN\n"
                + "encoding #Payload ::= { SIZE 8\n"
                + "END");

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Result.Success, Is.False);
            Assert.That(fixture.Result.ErrorMessage,
                Does.Contain("unterminated delimiter"));
        });
    }

    [TestCase("")]
    [TestCase("Broken DEFINITIONS ::= BEGIN END")]
    [TestCase("Broken ENCODING-DEFINITIONS BEGIN END")]
    [TestCase("Broken ENCODING-DEFINITIONS ::= END")]
    [TestCase("Broken ENCODING-DEFINITIONS ::= BEGIN")]
    [TestCase("Broken ENCODING-DEFINITIONS ::= BEGIN END trailing")]
    [TestCase("Broken ENCODING-DEFINITIONS ::= BEGIN value #INT END")]
    [TestCase("Broken ENCODING-DEFINITIONS ::= BEGIN ::= #INT END")]
    [TestCase("Broken ENCODING-DEFINITIONS ::= BEGIN value ::= #INT END")]
    [TestCase("Broken ENCODING-DEFINITIONS ::= BEGIN #Class #INT ::= #INT END")]
    [TestCase("Broken ENCODING-DEFINITIONS ::= BEGIN value #INT ::=")]
    [TestCase("Broken LINK-DEFINITIONS ::= BEGIN value #INT ::= NON-ECN-BEGIN raw END")]
    [TestCase("Broken ENCODING-DEFINITIONS ::= BEGIN value #INT ::= { ) } END")]
    [TestCase("Broken ENCODING-DEFINITIONS ::= BEGIN Set #ENCODINGS ::= { value } COMPLETED value END")]
    public void EveryMalformedModuleAndAssignmentProductionFailsParsing(
        string source)
    {
        EcnParseFixture fixture = ParseEcn(source);

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Result.Success, Is.False, source);
            Assert.That(fixture.Result.ErrorMessage, Is.Not.Empty, source);
        });
    }

    [Test]
    public void ModuleClausesAndAssignmentBodyAlternativesRetainTheirForms()
    {
        EcnParseFixture fixture = ParseEcn(
            "Forms ENCODING-DEFINITIONS ::= BEGIN\n"
                + "EXPORTS ALL;\n"
                + "IMPORTS #First, object FROM One { iso(1) 2 }, "
                + "#Second FROM Two;\n"
                + "RENAMES #First AS #Local IN encoding;\n"
                + "#Alias ::= [ #TAG (1) ] #INT\n"
                + "object #Alias ::= [APPLICATION 3] { SIZE 8 }\n"
                + "Set #ENCODINGS ::= { object } COMPLETED BY PER-BASIC-ALIGNED\n"
                + "END");
        EcnModuleSyntax module = fixture.Result.Module
            ?? throw new AssertionException("No ECN module was parsed.");

        Assert.Multiple(() =>
        {
            Assert.That(fixture.Result.Success,
                Is.True,
                fixture.Result.ErrorMessage);
            Assert.That(module.ExportsSpecified, Is.True);
            Assert.That(module.ExportsAll, Is.True);
            Assert.That(module.ImportedModules, Has.Length.EqualTo(2));
            Assert.That(module.ImportedModules[0].Symbols,
                Is.EqualTo(new[] { "#First", "object" }));
            Assert.That(module.ImportedModules[1].Symbols,
                Is.EqualTo(new[] { "#Second" }));
            Assert.That(module.RenameTokens.Select(item => item.Text),
                Is.EqualTo(new[] { "#First", "AS", "#Local", "IN", "encoding" }));
            Assert.That(module.Assignments.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    EcnAssignmentKind.EncodingClass,
                    EcnAssignmentKind.EncodingObject,
                    EcnAssignmentKind.EncodingObjectSet
                }));
            Assert.That(module.Assignments[1].BodyTokens[0].Kind,
                Is.EqualTo(AsnTokenKind.LeftBracket));
            Assert.That(module.Assignments[2].BodyTokens.Select(
                    item => item.Text),
                Does.Contain("PER-BASIC-ALIGNED"));
            Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
        });
    }

    private static EcnAssignmentSyntax RequireSingleAssignment(
        EcnParseFixture fixture)
    {
        Assert.That(fixture.Result.Success, Is.True,
            fixture.Result.ErrorMessage);
        Assert.That(fixture.Result.Module, Is.Not.Null);
        return fixture.Result.Module?.Assignments.Single()
            ?? throw new AssertionException("No ECN assignment was parsed.");
    }

    private static EcnParseFixture ParseEcn(string source)
    {
        IncrementalLexSession session = Lex(source);
        AsnToken[] tokens = Drain(session);
        return new EcnParseFixture
        {
            Session = session,
            Result = new EcnSyntaxParser(tokens).ParseModuleSyntax()
        };
    }

    private static IncrementalLexSession Lex(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "syntax.edm",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        return session;
    }

    private static AsnToken[] Drain(IncrementalLexSession session)
    {
        System.Collections.Generic.List<AsnToken> tokens =
            new System.Collections.Generic.List<AsnToken>();
        while (true)
        {
            IncrementalLexResult result = session.ScanNext();
            if (result.Status == IncrementalLexStatus.EndOfInput)
            {
                return tokens.ToArray();
            }

            if (result.Status == IncrementalLexStatus.NeedMoreData)
            {
                throw new AssertionException("Final ECN input requested more data.");
            }

            if (result.Token != null)
            {
                tokens.Add(result.Token);
            }
        }
    }

    private class EcnParseFixture
    {
        internal IncrementalLexSession Session = null!;
        internal EcnParseResult Result = null!;
    }
}
