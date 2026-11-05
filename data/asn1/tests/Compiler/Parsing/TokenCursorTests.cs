using asn1;

namespace asn1.Tests;

public class TokenCursorTests
{
    [Test]
    public void CursorProvidesBoundedLookaheadAndRetainsTriviaSeparately()
    {
        TokenCursor cursor = CreateCursor("A -- note --\n ::= INTEGER", 4);

        TokenCursorResult first = cursor.PeekNext(0);
        TokenCursorResult assignment = cursor.PeekNext(1);
        TokenCursorResult integer = cursor.PeekNext(2);

        Assert.That(first.Status, Is.EqualTo(TokenCursorStatus.Token));
        Assert.That(first.Token?.Kind, Is.EqualTo(AsnTokenKind.ObjectClassReference));
        Assert.That(assignment.Token?.Kind, Is.EqualTo(AsnTokenKind.Assignment));
        Assert.That(integer.Token?.Text, Is.EqualTo("INTEGER"));
        Assert.That(cursor.GetPosition(), Is.Zero);
        Assert.That(cursor.GetTriviaTokens().Select(token => token.Kind), Does.Contain(AsnTokenKind.Comment));
        Assert.That(cursor.GetTriviaTokens().Select(token => token.Kind), Does.Contain(AsnTokenKind.Whitespace));
        Assert.That(cursor.PeekNext(4).Status, Is.EqualTo(TokenCursorStatus.LookaheadLimitExceeded));
        Assert.That(cursor.PeekNext(-1).Status, Is.EqualTo(TokenCursorStatus.LookaheadLimitExceeded));

        Assert.That(cursor.ReadNext().Token?.Text, Is.EqualTo("A"));
        Assert.That(cursor.GetPosition(), Is.EqualTo(1));
    }

    [Test]
    public void IncrementalLookaheadNeedsMoreDataWithoutMovingAndThenResumes()
    {
        IncrementalLexSession session = CreateSession();
        TokenCursor cursor = new TokenCursor(session, 3);
        session.SupplyTextChunk("Na", false);

        Assert.That(cursor.PeekNext(0).Status, Is.EqualTo(TokenCursorStatus.NeedMoreData));
        Assert.That(cursor.GetPosition(), Is.Zero);

        session.SupplyTextChunk("me ::= INTEGER", true);

        Assert.That(cursor.PeekNext(0).Token?.Text, Is.EqualTo("Name"));
        Assert.That(cursor.PeekNext(1).Token?.Kind, Is.EqualTo(AsnTokenKind.Assignment));
        Assert.That(cursor.GetPosition(), Is.Zero);
    }

    [Test]
    public void NestedCheckpointsRequireLifoCompletionAndRewindWithoutExceptions()
    {
        TokenCursor cursor = CreateCursor("A B C", 2);
        int outer = cursor.BeginCheckpoint();
        Assert.That(cursor.ReadNext().Token?.Text, Is.EqualTo("A"));
        int inner = cursor.BeginCheckpoint();
        Assert.That(cursor.ReadNext().Token?.Text, Is.EqualTo("B"));

        Assert.That(cursor.CommitCheckpoint(outer), Is.False);
        Assert.That(cursor.RewindCheckpoint(inner), Is.True);
        Assert.That(cursor.ReadNext().Token?.Text, Is.EqualTo("B"));
        Assert.That(cursor.CommitCheckpoint(outer), Is.True);
        Assert.That(cursor.RewindCheckpoint(outer), Is.False);
        Assert.That(cursor.ReadNext().Token?.Text, Is.EqualTo("C"));
    }

    [Test]
    public void ExpectedTokenMismatchAddsLocatedDiagnosticWithoutConsuming()
    {
        IncrementalLexSession session = CreateSession();
        session.SupplyTextChunk("INTEGER }", true);
        TokenCursor cursor = new TokenCursor(session, 2);

        TokenCursorResult mismatch = cursor.ExpectToken(AsnTokenKind.Assignment);

        Assert.That(mismatch.Status, Is.EqualTo(TokenCursorStatus.Mismatch));
        Assert.That(mismatch.Token?.Text, Is.EqualTo("INTEGER"));
        Assert.That(cursor.GetPosition(), Is.Zero);
        AsnDiagnostic diagnostic = session.GetDiagnostics().Single();
        Assert.That(diagnostic.Code, Is.EqualTo("ASNP0001"));
        Assert.That(diagnostic.Category, Is.EqualTo(AsnDiagnosticCategory.Syntax));
        Assert.That(diagnostic.Message, Does.Contain("Expected Assignment"));
        Assert.That(diagnostic.Message, Does.Contain("INTEGER"));
        Assert.That(diagnostic.Span?.Start.Offset, Is.Zero);
        Assert.That(diagnostic.Span?.End.Offset, Is.EqualTo(7));

        TokenCursorResult expected = cursor.ExpectTokenText(AsnTokenKind.ReservedWord, "INTEGER");
        Assert.That(expected.Status, Is.EqualTo(TokenCursorStatus.Token));
        Assert.That(cursor.GetPosition(), Is.EqualTo(1));
        Assert.That(
            cursor.ExpectOneOf([AsnTokenKind.Comma, AsnTokenKind.RightBrace]).Status,
            Is.EqualTo(TokenCursorStatus.Token));
    }

    [Test]
    public void RecoveryStopsBeforeTheProductionBoundary()
    {
        TokenCursor cursor = CreateCursor("A ( 1 , 2 } END", 2);
        RecoverySetCatalog sets = new RecoverySetCatalog();

        TokenCursorResult component = cursor.RecoverTo(sets.ComponentBoundary);

        Assert.That(component.Status, Is.EqualTo(TokenCursorStatus.Token));
        Assert.That(component.Token?.Kind, Is.EqualTo(AsnTokenKind.Comma));
        Assert.That(component.SkippedTokenCount, Is.EqualTo(3));
        Assert.That(cursor.ReadNext().Token?.Kind, Is.EqualTo(AsnTokenKind.Comma));

        TokenCursorResult module = cursor.RecoverTo(sets.ModuleBoundary);
        Assert.That(module.Token?.Text, Is.EqualTo("END"));
        Assert.That(module.SkippedTokenCount, Is.EqualTo(2));
        Assert.That(cursor.ReadNext().Token?.Text, Is.EqualTo("END"));
    }

    [Test]
    public void RecoveryCanPauseForMoreInputAndContinueFromItsProgress()
    {
        IncrementalLexSession session = CreateSession();
        TokenCursor cursor = new TokenCursor(session, 2);
        RecoverySet components = new RecoverySetCatalog().ComponentBoundary;
        session.SupplyTextChunk("A (", false);

        TokenCursorResult paused = cursor.RecoverTo(components);

        Assert.That(paused.Status, Is.EqualTo(TokenCursorStatus.NeedMoreData));
        Assert.That(paused.SkippedTokenCount, Is.EqualTo(2));
        Assert.That(cursor.GetPosition(), Is.EqualTo(2));

        session.SupplyTextChunk(" 1, B", true);
        TokenCursorResult resumed = cursor.RecoverTo(components);
        Assert.That(resumed.Token?.Kind, Is.EqualTo(AsnTokenKind.Comma));
        Assert.That(resumed.SkippedTokenCount, Is.EqualTo(1));
        Assert.That(cursor.ReadNext().Token?.Kind, Is.EqualTo(AsnTokenKind.Comma));
    }

    [Test]
    public void RecoveryCatalogDefinesDistinctParserBoundaries()
    {
        RecoverySetCatalog sets = new RecoverySetCatalog();
        AsnToken end = CreateToken(AsnTokenKind.ReservedWord, "END");
        AsnToken begin = CreateToken(AsnTokenKind.ReservedWord, "BEGIN");

        Assert.That(sets.ModuleBoundary.Contains(end), Is.True);
        Assert.That(sets.ModuleBoundary.Contains(begin), Is.False);
        Assert.That(sets.AssignmentBoundary.Contains(CreateToken(AsnTokenKind.ValueReference, "v")), Is.True);
        Assert.That(sets.ComponentBoundary.Contains(CreateToken(AsnTokenKind.Comma, ",")), Is.True);
        Assert.That(sets.ConstraintBoundary.Contains(CreateToken(AsnTokenKind.RightParenthesis, ")")), Is.True);
        Assert.That(sets.ObjectSyntaxBoundary.Contains(CreateToken(AsnTokenKind.RightBracket, "]")), Is.True);
        Assert.That(sets.ParameterListBoundary.Contains(CreateToken(AsnTokenKind.Assignment, "::=")), Is.True);
        Assert.That(
            sets.EncodingControlBoundary.Contains(
                CreateToken(AsnTokenKind.ReservedWord, "ENCODING-CONTROL")),
            Is.True);
    }

    private static TokenCursor CreateCursor(string source, int maximumLookahead)
    {
        IncrementalLexSession session = CreateSession();
        session.SupplyTextChunk(source, true);
        return new TokenCursor(session, maximumLookahead);
    }

    private static IncrementalLexSession CreateSession()
    {
        return new IncrementalLexSession("cursor.asn", false, new AsnResourceLimits());
    }

    private static AsnToken CreateToken(AsnTokenKind kind, string text)
    {
        return new AsnToken
        {
            Kind = kind,
            Text = text
        };
    }
}
