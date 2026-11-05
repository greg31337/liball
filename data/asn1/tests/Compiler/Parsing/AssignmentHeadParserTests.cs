using asn1;

namespace asn1.Tests;

public class AssignmentHeadParserTests
{
    [Test]
    public void ParserRetainsHeadsForAllSixAssignmentCategories()
    {
        string source = "Assignments DEFINITIONS ::= BEGIN\n"
            + "TypeA ::= INTEGER\n"
            + "valueA INTEGER ::= 1\n"
            + "ValuesA INTEGER ::= { 1 | 2 }\n"
            + "CLASSA ::= CLASS { &id INTEGER }\n"
            + "objectA CLASSA ::= { &id 1 }\n"
            + "ObjectsA CLASSA ::= { objectA }\n"
            + "END";

        ParseFixture fixture = Parse(source);

        ModuleSyntax module = RequireModule(fixture.Result);
        Assert.That(fixture.Result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(module.AssignmentHeads, Has.Length.EqualTo(6));
        AssertKinds(FindAlternative(module.AssignmentHeads[0], "TypeA"), AsnAssignmentKind.Type);
        AssertKinds(FindAlternative(module.AssignmentHeads[1], "valueA"), AsnAssignmentKind.Value);
        AssertKinds(FindAlternative(module.AssignmentHeads[2], "ValuesA"), AsnAssignmentKind.ValueSet);
        AssertKinds(
            FindAlternative(module.AssignmentHeads[3], "CLASSA"),
            AsnAssignmentKind.Type,
            AsnAssignmentKind.ObjectClass);
        AssertKinds(
            FindAlternative(module.AssignmentHeads[4], "objectA"),
            AsnAssignmentKind.Value,
            AsnAssignmentKind.Object);
        AssertKinds(
            FindAlternative(module.AssignmentHeads[5], "ObjectsA"),
            AsnAssignmentKind.ValueSet,
            AsnAssignmentKind.ObjectSet);
        Assert.That(module.AssignmentHeads[3].Ambiguous, Is.True);
        Assert.That(module.AssignmentHeads[4].Ambiguous, Is.True);
        Assert.That(module.AssignmentHeads[5].Ambiguous, Is.True);
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void GovernedHeadsRetainGovernorTokensAndRecognizeUsefulObjectClasses()
    {
        ParseFixture fixture = Parse(
            "Objects DEFINITIONS ::= BEGIN\n"
            + "objectA TYPE-IDENTIFIER ::= { &id 1 }\n"
            + "ObjectsA ABSTRACT-SYNTAX ::= { objectA }\n"
            + "END");

        ModuleSyntax module = RequireModule(fixture.Result);
        AssignmentHeadAlternativeSyntax objectHead = FindAlternative(module.AssignmentHeads[0], "objectA");
        AssignmentHeadAlternativeSyntax setHead = FindAlternative(module.AssignmentHeads[1], "ObjectsA");
        AssertKinds(objectHead, AsnAssignmentKind.Object);
        AssertKinds(setHead, AsnAssignmentKind.ObjectSet);
        Assert.That(objectHead.GovernorTokens.Select(token => token.Text), Is.EqualTo(new[]
        {
            "TYPE-IDENTIFIER"
        }));
        Assert.That(setHead.GovernorTokens.Select(token => token.Text), Is.EqualTo(new[]
        {
            "ABSTRACT-SYNTAX"
        }));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void QualifiedAndConstrainedGovernorsRemainAvailableForClassification()
    {
        ParseFixture fixture = Parse(
            "Governors DEFINITIONS ::= BEGIN\n"
            + "valueA Other.Type (SIZE (1)) ::= 3\n"
            + "END");

        AssignmentHeadAlternativeSyntax head = FindAlternative(
            RequireModule(fixture.Result).AssignmentHeads.Single(),
            "valueA");
        Assert.That(head.CandidateKinds, Is.EqualTo(new[]
        {
            AsnAssignmentKind.Value,
            AsnAssignmentKind.Object
        }));
        Assert.That(head.GovernorTokens.Select(token => token.Text), Is.EqualTo(new[]
        {
            "Other", ".", "Type", "(", "SIZE", "(", "1", ")", ")"
        }));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void NestedAssignmentOperatorsAreNotTreatedAsModuleAssignments()
    {
        ParseFixture fixture = Parse(
            "Nested DEFINITIONS ::= BEGIN Outer ::= { inner ::= 1 } END");

        ModuleSyntax module = RequireModule(fixture.Result);
        Assert.That(module.AssignmentHeads, Has.Length.EqualTo(1));
        Assert.That(FindAlternative(module.AssignmentHeads[0], "Outer"), Is.Not.Null);
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void SemicolonsBoundCandidateSearchWithoutBeingRequired()
    {
        ParseFixture fixture = Parse(
            "Bounds DEFINITIONS ::= BEGIN A ::= first; B ::= second END");

        ModuleSyntax module = RequireModule(fixture.Result);
        Assert.That(module.AssignmentHeads, Has.Length.EqualTo(2));
        Assert.That(module.AssignmentHeads[1].Alternatives.Select(item => item.Name), Is.EqualTo(new[] { "B" }));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void PreviousRightSideReferencesAreNotNextAssignmentAlternatives()
    {
        ParseFixture fixture = Parse(
            "Bounds DEFINITIONS ::= BEGIN\n"
            + "first INTEGER ::= previousValue\n"
            + "second INTEGER ::= 2\n"
            + "Third ::= PreviousType\n"
            + "Fourth ::= BOOLEAN\n"
            + "END");

        ModuleSyntax module = RequireModule(fixture.Result);
        Assert.That(fixture.Result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(module.AssignmentHeads, Has.Length.EqualTo(4));
        Assert.That(
            module.AssignmentHeads.SelectMany(head => head.Alternatives)
                .Select(alternative => alternative.Name),
            Is.EqualTo(new[] { "first", "second", "Third", "Fourth" }));
        Assert.That(
            module.AssignmentHeads.Select(head => head.DeclaredReference?.Name),
            Is.EqualTo(new[] { "first", "second", "Third", "Fourth" }));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void NewLineBoundsAssignmentsAfterConstrainedCollectionTypes()
    {
        ParseFixture fixture = Parse(
            "Collections DEFINITIONS ::= BEGIN\n"
            + "Items ::= SET SIZE (1..MAX) OF Item\n"
            + "Envelope ::= SEQUENCE { items Items }\n"
            + "Item ::= INTEGER\n"
            + "END");

        ModuleSyntax module = RequireModule(fixture.Result);
        Assert.That(fixture.Result.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(module.AssignmentHeads.Select(item => item.DeclaredReference?.Name),
            Is.EqualTo(new[] { "Items", "Envelope", "Item" }));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [Test]
    public void NestedSyntaxGroupsEndingWithAdjacentBracketsDoNotHideLaterAssignments()
    {
        ParseFixture fixture = Parse(
            "SyntaxGroups DEFINITIONS ::= BEGIN\n"
            + "ATTRIBUTE ::= CLASS { &id OBJECT IDENTIFIER UNIQUE,\n"
            + " &min INTEGER DEFAULT 1, &max INTEGER OPTIONAL }\n"
            + "WITH SYNTAX { [COUNTS [MIN &min] [MAX &max]] IDENTIFIED BY &id }\n"
            + "NextType ::= INTEGER\n"
            + "END");

        ModuleSyntax module = RequireModule(fixture.Result);
        Assert.That(module.AssignmentHeads.Select(item => item.DeclaredReference?.Name),
            Is.EqualTo(new[] { "ATTRIBUTE", "NextType" }));
        Assert.That(fixture.Session.GetDiagnostics(), Is.Empty);
    }

    [TestCase("Broken INTEGER 1", "ASNP0012")]
    [TestCase("::= INTEGER", "ASNP0013")]
    public void InvalidAssignmentHeadsProduceLocatedDiagnostics(string body, string code)
    {
        ParseFixture fixture = Parse(
            "Broken DEFINITIONS ::= BEGIN " + body + " END");

        Assert.That(fixture.Result.Status, Is.EqualTo(ModuleParseStatus.Invalid));
        Assert.That(fixture.Cursor.PeekNext(0).Token?.Text, Is.EqualTo("END"));
        Assert.That(fixture.Session.GetDiagnostics().Select(item => item.Code), Does.Contain(code));
    }

    private static ParseFixture Parse(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "assignments.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        TokenCursor cursor = new TokenCursor(session, 4);
        return new ParseFixture
        {
            Session = session,
            Cursor = cursor,
            Result = new ModuleParser(cursor).ParseModule()
        };
    }

    private static ModuleSyntax RequireModule(ModuleParseResult result)
    {
        Assert.That(result.Module, Is.Not.Null);
        return result.Module ?? throw new InvalidOperationException();
    }

    private static AssignmentHeadAlternativeSyntax FindAlternative(
        AssignmentHeadSyntax head,
        string name)
    {
        AssignmentHeadAlternativeSyntax? result = head.Alternatives.SingleOrDefault(item => item.Name == name);
        Assert.That(result, Is.Not.Null, "Missing assignment-head alternative " + name);
        return result ?? throw new InvalidOperationException();
    }

    private static void AssertKinds(
        AssignmentHeadAlternativeSyntax alternative,
        params AsnAssignmentKind[] expected)
    {
        Assert.That(alternative.CandidateKinds, Is.EqualTo(expected));
    }

    private class ParseFixture
    {
        internal IncrementalLexSession Session = null!;
        internal TokenCursor Cursor = null!;
        internal ModuleParseResult Result = null!;
    }
}
