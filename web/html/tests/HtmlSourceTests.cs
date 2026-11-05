
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSourceTests
{
    [Test]
    public void LineMapRejectsUnrepresentableTerminalPosition()
    {
        HtmlLineMap map = HtmlLineMap.Create();
        map.length = int.MaxValue - 1;

        Assert.Throws<InsufficientMemoryException>(() =>
            map.AppendWithCharacterOffsets('x', 0, 1));
    }

    [Test]
    public void PositionsExposeNormalizedOriginalAndOptionalByteOffsets()
    {
        HtmlSourcePosition position = HtmlSourcePosition.CreateMappedWithByteOffset(4, 5, 2, 3, 9);

        Assert.Multiple(() =>
        {
            Assert.That(position.IsKnown(), Is.True);
            Assert.That(position.GetNormalizedCharacterOffset(), Is.EqualTo(4));
            Assert.That(position.GetCharacterOffset(), Is.EqualTo(5));
            Assert.That(position.GetLine(), Is.EqualTo(2));
            Assert.That(position.GetColumn(), Is.EqualTo(3));
            Assert.That(position.HasByteOffset(), Is.True);
            Assert.That(position.GetByteOffset(), Is.EqualTo(9));
        });

        HtmlSourcePosition withoutBytes = HtmlSourcePosition.Create(2, 1, 3);
        Assert.That(withoutBytes.HasByteOffset(), Is.False);
        Assert.That(withoutBytes.GetByteOffset(), Is.EqualTo(-1));
    }

    [Test]
    public void UnknownPositionUsesExplicitSentinels()
    {
        HtmlSourcePosition position = HtmlSourcePosition.CreateUnknown();

        Assert.That(position.IsKnown(), Is.False);
        Assert.That(position.GetNormalizedCharacterOffset(), Is.EqualTo(-1));
        Assert.That(position.GetCharacterOffset(), Is.EqualTo(-1));
        Assert.That(position.GetLine(), Is.EqualTo(0));
        Assert.That(position.GetColumn(), Is.EqualTo(0));
        Assert.That(position.HasByteOffset(), Is.False);
        Assert.That(position.GetByteOffset(), Is.EqualTo(-1));
    }

    [Test]
    public void PositionFactoriesValidateCoordinates()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlSourcePosition.CreateMapped(-1, 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlSourcePosition.CreateMapped(0, -1, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlSourcePosition.Create(0, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlSourcePosition.Create(0, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlSourcePosition.CreateWithByteOffset(0, 1, 1, -1));
    }

    [Test]
    public void SpansAreEndExclusiveAndPreserveBothLengths()
    {
        HtmlSourcePosition start = HtmlSourcePosition.CreateMappedWithByteOffset(2, 2, 1, 3, 2);
        HtmlSourcePosition end = HtmlSourcePosition.CreateMappedWithByteOffset(3, 4, 2, 1, 4);
        HtmlSourceSpan span = HtmlSourceSpan.Create(start, end);

        Assert.That(span.IsKnown(), Is.True);
        Assert.That(span.GetStart(), Is.SameAs(start));
        Assert.That(span.GetEnd(), Is.SameAs(end));
        Assert.That(span.GetNormalizedCharacterLength(), Is.EqualTo(1));
        Assert.That(span.GetCharacterLength(), Is.EqualTo(2));
        Assert.That(span.GetByteLength(), Is.EqualTo(2));
    }

    [Test]
    public void UnknownSpanUsesExplicitLengthSentinels()
    {
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();

        Assert.That(span.IsKnown(), Is.False);
        Assert.That(span.GetNormalizedCharacterLength(), Is.EqualTo(-1));
        Assert.That(span.GetCharacterLength(), Is.EqualTo(-1));
        Assert.That(span.GetByteLength(), Is.EqualTo(-1));
    }

    [Test]
    public void SpanFactoryRejectsInconsistentPositions()
    {
        HtmlSourcePosition first = HtmlSourcePosition.Create(1, 1, 2);
        HtmlSourcePosition before = HtmlSourcePosition.Create(0, 1, 1);
        HtmlSourcePosition withBytes = HtmlSourcePosition.CreateWithByteOffset(2, 1, 3, 2);

        Assert.Throws<ArgumentException>(() => HtmlSourceSpan.Create(first, before));
        Assert.Throws<ArgumentException>(() => HtmlSourceSpan.Create(first, withBytes));
        Assert.Throws<ArgumentException>(() => HtmlSourceSpan.Create(HtmlSourcePosition.CreateUnknown(), HtmlSourcePosition.CreateUnknown()));
    }

    [Test]
    public void LineMapTracksOneBasedLinesAndColumns()
    {
        HtmlLineMap map = HtmlLineMap.Create();
        foreach (char value in "a\nbc\n")
        {
            map.Append(value);
        }

        Assert.That(map.GetLength(), Is.EqualTo(5));
        Assert.That(map.GetLineCount(), Is.EqualTo(3));
        AssertPosition(map.GetPosition(0), 0, 0, 1, 1, -1);
        AssertPosition(map.GetPosition(1), 1, 1, 1, 2, -1);
        AssertPosition(map.GetPosition(2), 2, 2, 2, 1, -1);
        AssertPosition(map.GetPosition(4), 4, 4, 2, 3, -1);
        AssertPosition(map.GetPosition(5), 5, 5, 3, 1, -1);
    }

    [Test]
    public void LineMapPreservesOriginalOffsetsAcrossCrLfNormalization()
    {
        HtmlLineMap map = HtmlLineMap.Create();
        map.AppendWithCharacterOffsets('a', 0, 1);
        map.AppendWithCharacterOffsets('\n', 1, 3);
        map.AppendWithCharacterOffsets('b', 3, 4);

        AssertPosition(map.GetPosition(1), 1, 1, 1, 2, -1);
        AssertPosition(map.GetPosition(2), 2, 3, 2, 1, -1);
        Assert.That(map.GetSpan(1, 2).GetNormalizedCharacterLength(), Is.EqualTo(1));
        Assert.That(map.GetSpan(1, 2).GetCharacterLength(), Is.EqualTo(2));
    }

    [Test]
    public void LineMapTracksOriginalByteBoundariesWhenEnabled()
    {
        HtmlLineMap map = HtmlLineMap.CreateWithByteOffsets(3, 10);
        map.AppendWithByteOffsets('a', 3, 4, 10, 11);
        map.AppendWithByteOffsets('\n', 4, 6, 11, 13);
        map.AppendWithByteOffsets('é', 6, 7, 13, 15);

        Assert.That(map.TracksByteOffsets(), Is.True);
        AssertPosition(map.GetPosition(0), 0, 3, 1, 1, 10);
        AssertPosition(map.GetPosition(2), 2, 6, 2, 1, 13);
        AssertPosition(map.GetPosition(3), 3, 7, 2, 2, 15);
        Assert.That(map.GetSpan(1, 3).GetByteLength(), Is.EqualTo(4));
    }

    [Test]
    public void LineMapGrowsAndRejectsDiscontinuousInput()
    {
        HtmlLineMap map = HtmlLineMap.Create();
        for (int index = 0; index < 40; index++)
        {
            map.Append('\n');
        }

        Assert.That(map.GetLineCount(), Is.EqualTo(41));
        Assert.Throws<ArgumentException>(() => map.Append('\r'));
        Assert.Throws<ArgumentException>(() => map.AppendWithCharacterOffsets('x', 100, 101));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.GetPosition(41));

        HtmlLineMap bytes = HtmlLineMap.CreateWithByteOffsets(0);
        Assert.Throws<ArgumentException>(() => bytes.AppendWithByteOffsets('x', 0, 1, 1, 2));
        Assert.Throws<InvalidOperationException>(() => bytes.Append('x'));
    }

    static void AssertPosition(HtmlSourcePosition position, int normalizedOffset, int characterOffset, int line, int column, long byteOffset)
    {
        Assert.That(position.GetNormalizedCharacterOffset(), Is.EqualTo(normalizedOffset));
        Assert.That(position.GetCharacterOffset(), Is.EqualTo(characterOffset));
        Assert.That(position.GetLine(), Is.EqualTo(line));
        Assert.That(position.GetColumn(), Is.EqualTo(column));
        Assert.That(position.GetByteOffset(), Is.EqualTo(byteOffset));
    }
}
