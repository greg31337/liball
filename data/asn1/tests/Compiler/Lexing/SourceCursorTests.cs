using asn1;

namespace asn1.Tests;

public class SourceCursorTests
{
    [Test]
    public void LeadingBomIsConsumedWithoutChangingTheFirstSourceColumn()
    {
        SourceCursor cursor = new SourceCursor("bom.asn");
        cursor.SupplyChunk("\uFEFFA", true);

        SourceCursorReadResult value = cursor.ReadNext();
        SourceCursorReadResult end = cursor.ReadNext();

        Assert.That(value.Status, Is.EqualTo(SourceCursorStatus.Value));
        Assert.That(value.ScalarValue, Is.EqualTo('A'));
        AssertPosition(value.Start, "bom.asn", 1, 1, 1);
        AssertPosition(value.End, "bom.asn", 2, 1, 2);
        Assert.That(cursor.GetConsumedCharacterCount(), Is.EqualTo(2));
        Assert.That(end.Status, Is.EqualTo(SourceCursorStatus.EndOfInput));
        AssertPosition(end.Start, "bom.asn", 2, 1, 2);
    }

    [Test]
    public void NonLeadingBomIsReturnedAsAnOrdinaryUnicodeScalar()
    {
        SourceCursor cursor = new SourceCursor("middle-bom.asn");
        cursor.SupplyChunk("A\uFEFF", true);

        SourceCursorReadResult first = cursor.ReadNext();
        SourceCursorReadResult second = cursor.ReadNext();

        Assert.That(first.ScalarValue, Is.EqualTo('A'));
        Assert.That(second.Status, Is.EqualTo(SourceCursorStatus.Value));
        Assert.That(second.ScalarValue, Is.EqualTo(65279));
        AssertPosition(second.Start, "middle-bom.asn", 1, 1, 2);
        AssertPosition(second.End, "middle-bom.asn", 2, 1, 3);
    }

    [Test]
    public void SurrogatePairsUseTwoOffsetsAndOneColumn()
    {
        SourceCursor cursor = new SourceCursor("unicode.asn");
        cursor.SupplyChunk("A😀B", true);

        SourceCursorReadResult first = cursor.ReadNext();
        SourceCursorReadResult emoji = cursor.ReadNext();
        SourceCursorReadResult last = cursor.ReadNext();

        Assert.That(first.ScalarValue, Is.EqualTo('A'));
        Assert.That(emoji.Status, Is.EqualTo(SourceCursorStatus.Value));
        Assert.That(emoji.ScalarValue, Is.EqualTo(128512));
        Assert.That(emoji.Utf16Length, Is.EqualTo(2));
        AssertPosition(emoji.Start, "unicode.asn", 1, 1, 2);
        AssertPosition(emoji.End, "unicode.asn", 3, 1, 3);
        Assert.That(last.ScalarValue, Is.EqualTo('B'));
        AssertPosition(last.Start, "unicode.asn", 3, 1, 3);
        AssertPosition(last.End, "unicode.asn", 4, 1, 4);
    }

    [Test]
    public void CrLfAndLoneLineEndingsProduceNormalizedLineBreakEvents()
    {
        SourceCursor cursor = new SourceCursor("lines.asn");
        cursor.SupplyChunk("A\r\nB\rC\nD", true);

        SourceCursorReadResult a = cursor.ReadNext();
        SourceCursorReadResult crlf = cursor.ReadNext();
        SourceCursorReadResult b = cursor.ReadNext();
        SourceCursorReadResult cr = cursor.ReadNext();
        SourceCursorReadResult c = cursor.ReadNext();
        SourceCursorReadResult lf = cursor.ReadNext();
        SourceCursorReadResult d = cursor.ReadNext();

        Assert.That(a.ScalarValue, Is.EqualTo('A'));
        AssertLineBreak(crlf, "lines.asn", 2, 1, 1, 2, 3, 2, 1);
        Assert.That(b.ScalarValue, Is.EqualTo('B'));
        AssertLineBreak(cr, "lines.asn", 1, 4, 2, 2, 5, 3, 1);
        Assert.That(c.ScalarValue, Is.EqualTo('C'));
        AssertLineBreak(lf, "lines.asn", 1, 6, 3, 2, 7, 4, 1);
        Assert.That(d.ScalarValue, Is.EqualTo('D'));
        AssertPosition(d.End, "lines.asn", 8, 4, 2);
    }

    [Test]
    public void CrLfAndSurrogatePairsResumeAcrossChunkBoundaries()
    {
        SourceCursor cursor = new SourceCursor("chunks.asn");
        cursor.SupplyChunk("A\r", false);

        Assert.That(cursor.ReadNext().ScalarValue, Is.EqualTo('A'));
        Assert.That(cursor.ReadNext().Status, Is.EqualTo(SourceCursorStatus.NeedMoreData));

        cursor.SupplyChunk("\n\uD83D", false);
        SourceCursorReadResult lineBreak = cursor.ReadNext();
        AssertLineBreak(lineBreak, "chunks.asn", 2, 1, 1, 2, 3, 2, 1);
        Assert.That(cursor.ReadNext().Status, Is.EqualTo(SourceCursorStatus.NeedMoreData));

        cursor.SupplyChunk("\uDE00B", true);
        SourceCursorReadResult emoji = cursor.ReadNext();
        SourceCursorReadResult b = cursor.ReadNext();

        Assert.That(emoji.Status, Is.EqualTo(SourceCursorStatus.Value));
        Assert.That(emoji.ScalarValue, Is.EqualTo(128512));
        Assert.That(emoji.Utf16Length, Is.EqualTo(2));
        AssertPosition(emoji.Start, "chunks.asn", 3, 2, 1);
        AssertPosition(emoji.End, "chunks.asn", 5, 2, 2);
        Assert.That(b.ScalarValue, Is.EqualTo('B'));
        AssertPosition(b.End, "chunks.asn", 6, 2, 3);
        Assert.That(cursor.ReadNext().Status, Is.EqualTo(SourceCursorStatus.EndOfInput));
    }

    [Test]
    public void MalformedSurrogatesProduceRecoverableInvalidScalarEvents()
    {
        SourceCursor cursor = new SourceCursor("invalid.asn");
        cursor.SupplyChunk("\uDC00\uD83DX\uD83D", true);

        SourceCursorReadResult loneLow = cursor.ReadNext();
        SourceCursorReadResult unmatchedHigh = cursor.ReadNext();
        SourceCursorReadResult x = cursor.ReadNext();
        SourceCursorReadResult trailingHigh = cursor.ReadNext();

        AssertInvalidScalar(loneLow, 0, 1, 1, 2);
        AssertInvalidScalar(unmatchedHigh, 1, 2, 2, 3);
        Assert.That(x.Status, Is.EqualTo(SourceCursorStatus.Value));
        Assert.That(x.ScalarValue, Is.EqualTo('X'));
        AssertPosition(x.Start, "invalid.asn", 2, 1, 3);
        AssertInvalidScalar(trailingHigh, 3, 4, 4, 5);
        Assert.That(cursor.ReadNext().Status, Is.EqualTo(SourceCursorStatus.EndOfInput));
    }

    [Test]
    public void PendingCharactersDoNotConsumeAnUnmatchedNextChunkCharacter()
    {
        SourceCursor carriageReturnCursor = new SourceCursor("cr-boundary.asn");
        carriageReturnCursor.SupplyChunk("\r", false);
        Assert.That(carriageReturnCursor.ReadNext().Status, Is.EqualTo(SourceCursorStatus.NeedMoreData));
        carriageReturnCursor.SupplyChunk("A", true);

        SourceCursorReadResult carriageReturn = carriageReturnCursor.ReadNext();
        SourceCursorReadResult afterCarriageReturn = carriageReturnCursor.ReadNext();

        AssertLineBreak(carriageReturn, "cr-boundary.asn", 1, 0, 1, 1, 1, 2, 1);
        Assert.That(afterCarriageReturn.ScalarValue, Is.EqualTo('A'));
        AssertPosition(afterCarriageReturn.Start, "cr-boundary.asn", 1, 2, 1);

        SourceCursor surrogateCursor = new SourceCursor("surrogate-boundary.asn");
        surrogateCursor.SupplyChunk("\uD83D", false);
        Assert.That(surrogateCursor.ReadNext().Status, Is.EqualTo(SourceCursorStatus.NeedMoreData));
        surrogateCursor.SupplyChunk("B", true);

        SourceCursorReadResult highSurrogate = surrogateCursor.ReadNext();
        SourceCursorReadResult afterHighSurrogate = surrogateCursor.ReadNext();

        Assert.That(highSurrogate.Status, Is.EqualTo(SourceCursorStatus.Invalid));
        AssertPosition(highSurrogate.Start, "surrogate-boundary.asn", 0, 1, 1);
        AssertPosition(highSurrogate.End, "surrogate-boundary.asn", 1, 1, 2);
        Assert.That(afterHighSurrogate.ScalarValue, Is.EqualTo('B'));
        AssertPosition(afterHighSurrogate.Start, "surrogate-boundary.asn", 1, 1, 2);
    }

    [Test]
    public void CursorRequiresCurrentChunkConsumptionAndHonorsFinalChunk()
    {
        SourceCursor cursor = new SourceCursor("supply.asn");

        Assert.That(cursor.ReadNext().Status, Is.EqualTo(SourceCursorStatus.NeedMoreData));
        cursor.SupplyChunk("AB", false);
        Assert.That(() => cursor.SupplyChunk("C", false), Throws.TypeOf<InvalidOperationException>());
        Assert.That(cursor.ReadNext().ScalarValue, Is.EqualTo('A'));
        Assert.That(() => cursor.SupplyChunk("C", false), Throws.TypeOf<InvalidOperationException>());
        Assert.That(cursor.ReadNext().ScalarValue, Is.EqualTo('B'));
        Assert.That(cursor.ReadNext().Status, Is.EqualTo(SourceCursorStatus.NeedMoreData));

        cursor.SupplyChunk("", true);
        Assert.That(cursor.ReadNext().Status, Is.EqualTo(SourceCursorStatus.EndOfInput));
        Assert.That(() => cursor.SupplyChunk("C", true), Throws.TypeOf<InvalidOperationException>());
    }

    private static void AssertLineBreak(
        SourceCursorReadResult result,
        string sourceName,
        int utf16Length,
        int startOffset,
        int startLine,
        int startColumn,
        int endOffset,
        int endLine,
        int endColumn)
    {
        Assert.That(result.Status, Is.EqualTo(SourceCursorStatus.Value));
        Assert.That(result.ScalarValue, Is.EqualTo(10));
        Assert.That(result.IsLineBreak, Is.True);
        Assert.That(result.Utf16Length, Is.EqualTo(utf16Length));
        AssertPosition(result.Start, sourceName, startOffset, startLine, startColumn);
        AssertPosition(result.End, sourceName, endOffset, endLine, endColumn);
    }

    private static void AssertInvalidScalar(SourceCursorReadResult result, int startOffset, int endOffset, int startColumn, int endColumn)
    {
        Assert.That(result.Status, Is.EqualTo(SourceCursorStatus.Invalid));
        Assert.That(result.ScalarValue, Is.EqualTo(65533));
        Assert.That(result.Utf16Length, Is.EqualTo(1));
        AssertPosition(result.Start, "invalid.asn", startOffset, 1, startColumn);
        AssertPosition(result.End, "invalid.asn", endOffset, 1, endColumn);
    }

    private static void AssertPosition(AsnSourcePosition position, string sourceName, int offset, int line, int column)
    {
        Assert.That(position.SourceName, Is.EqualTo(sourceName));
        Assert.That(position.Offset, Is.EqualTo(offset));
        Assert.That(position.Line, Is.EqualTo(line));
        Assert.That(position.Column, Is.EqualTo(column));
    }
}
