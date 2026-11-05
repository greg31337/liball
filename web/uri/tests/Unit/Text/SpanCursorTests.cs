namespace uri.Tests;

public class SpanCursorTests
{
    [Test]
    public void PeekReportsCurrentCharacterWithoutAdvancing()
    {
        SpanCursor cursor = new SpanCursor(1);

        SpanCursorResult result = cursor.PeekCharacter("abc".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Character, Is.EqualTo('b'));
            Assert.That(result.StartOffset, Is.EqualTo(1));
            Assert.That(result.NextOffset, Is.EqualTo(1));
            Assert.That(cursor.Offset, Is.EqualTo(1));
            Assert.That(cursor.HasRemaining("abc".AsSpan()), Is.True);
            Assert.That(cursor.GetRemainingLength("abc".AsSpan()), Is.EqualTo(2));
        });
    }

    [Test]
    public void ReadAdvancesAndReportsEndWithoutMovingPastIt()
    {
        SpanCursor cursor = new SpanCursor(0);

        SpanCursorResult first = cursor.ReadCharacter("ab".AsSpan());
        SpanCursorResult second = cursor.ReadCharacter("ab".AsSpan());
        SpanCursorResult end = cursor.ReadCharacter("ab".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(first.Character, Is.EqualTo('a'));
            Assert.That(first.StartOffset, Is.Zero);
            Assert.That(first.NextOffset, Is.EqualTo(1));
            Assert.That(second.Character, Is.EqualTo('b'));
            Assert.That(second.StartOffset, Is.EqualTo(1));
            Assert.That(second.NextOffset, Is.EqualTo(2));
            Assert.That(end.Success, Is.False);
            Assert.That(end.ErrorOffset, Is.EqualTo(2));
            Assert.That(end.ErrorLength, Is.Zero);
            Assert.That(cursor.Offset, Is.EqualTo(2));
            Assert.That(cursor.HasRemaining("ab".AsSpan()), Is.False);
            Assert.That(cursor.GetRemainingLength("ab".AsSpan()), Is.Zero);
        });
    }

    [Test]
    public void AdvanceAcceptsExactBoundaryAndRejectsInvalidCountsAtomically()
    {
        SpanCursor exact = new SpanCursor(1);
        SpanCursorResult advanced = exact.Advance("abcd".AsSpan(), 3);
        SpanCursorResult beyond = exact.Advance("abcd".AsSpan(), 1);

        SpanCursor negative = new SpanCursor(2);
        SpanCursorResult negativeCount = negative.Advance("abcd".AsSpan(), -1);

        Assert.Multiple(() =>
        {
            Assert.That(advanced.Success, Is.True);
            Assert.That(advanced.StartOffset, Is.EqualTo(1));
            Assert.That(advanced.NextOffset, Is.EqualTo(4));
            Assert.That(exact.Offset, Is.EqualTo(4));
            Assert.That(beyond.Success, Is.False);
            Assert.That(beyond.ErrorOffset, Is.EqualTo(4));
            Assert.That(negativeCount.Success, Is.False);
            Assert.That(negativeCount.ErrorOffset, Is.EqualTo(2));
            Assert.That(negative.Offset, Is.EqualTo(2));
        });
    }

    [Test]
    public void ConsumeCharacterAdvancesOnlyOnMatch()
    {
        SpanCursor cursor = new SpanCursor(1);

        SpanCursorResult mismatch = cursor.ConsumeCharacter("abc".AsSpan(), 'x');
        SpanCursorResult match = cursor.ConsumeCharacter("abc".AsSpan(), 'b');

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.Success, Is.False);
            Assert.That(mismatch.ErrorOffset, Is.EqualTo(1));
            Assert.That(mismatch.ErrorLength, Is.EqualTo(1));
            Assert.That(match.Success, Is.True);
            Assert.That(match.Character, Is.EqualTo('b'));
            Assert.That(match.NextOffset, Is.EqualTo(2));
            Assert.That(cursor.Offset, Is.EqualTo(2));
        });
    }

    [Test]
    public void ConsumeTextReportsExactMismatchOrTruncationOffset()
    {
        SpanCursor mismatchCursor = new SpanCursor(1);
        SpanCursorResult mismatch = mismatchCursor.ConsumeText("abcdef".AsSpan(), "bxd".AsSpan());

        SpanCursor truncatedCursor = new SpanCursor(4);
        SpanCursorResult truncated = truncatedCursor.ConsumeText("abcdef".AsSpan(), "efg".AsSpan());

        SpanCursor matchingCursor = new SpanCursor(1);
        SpanCursorResult matching = matchingCursor.ConsumeText("abcdef".AsSpan(), "bcd".AsSpan());
        SpanCursorResult empty = matchingCursor.ConsumeText("abcdef".AsSpan(), "".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.Success, Is.False);
            Assert.That(mismatch.ErrorOffset, Is.EqualTo(2));
            Assert.That(mismatch.ErrorLength, Is.EqualTo(1));
            Assert.That(mismatchCursor.Offset, Is.EqualTo(1));
            Assert.That(truncated.Success, Is.False);
            Assert.That(truncated.ErrorOffset, Is.EqualTo(6));
            Assert.That(truncated.ErrorLength, Is.Zero);
            Assert.That(truncatedCursor.Offset, Is.EqualTo(4));
            Assert.That(matching.Success, Is.True);
            Assert.That(matching.StartOffset, Is.EqualTo(1));
            Assert.That(matching.NextOffset, Is.EqualTo(4));
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.StartOffset, Is.EqualTo(4));
            Assert.That(empty.NextOffset, Is.EqualTo(4));
            Assert.That(matchingCursor.Offset, Is.EqualTo(4));
        });
    }

    [Test]
    public void InvalidInitialOffsetsReportNearestInputBoundary()
    {
        SpanCursor negative = new SpanCursor(-2);
        SpanCursor beyond = new SpanCursor(10);

        SpanCursorResult negativeResult = negative.PeekCharacter("abc".AsSpan());
        SpanCursorResult beyondResult = beyond.PeekCharacter("abc".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(negativeResult.Success, Is.False);
            Assert.That(negativeResult.ErrorOffset, Is.Zero);
            Assert.That(negativeResult.ErrorLength, Is.Zero);
            Assert.That(negative.GetRemainingLength("abc".AsSpan()), Is.Zero);
            Assert.That(beyondResult.Success, Is.False);
            Assert.That(beyondResult.ErrorOffset, Is.EqualTo(3));
            Assert.That(beyondResult.ErrorLength, Is.Zero);
            Assert.That(beyond.GetRemainingLength("abc".AsSpan()), Is.Zero);
        });
    }
}
