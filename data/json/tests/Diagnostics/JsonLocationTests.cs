namespace json.Tests;

public class JsonLocationTests
{
    [Test]
    public void InitialPositionUsesZeroBasedOffsetAndOneBasedEnabledCoordinates()
    {
        JsonSourcePosition full = new JsonLocationTracker(true, true).GetPosition();
        JsonSourcePosition lineOnly = new JsonLocationTracker(true, false).GetPosition();
        JsonSourcePosition offsetOnly = new JsonLocationTracker(false, false).GetPosition();

        Assert.Multiple(() =>
        {
            AssertPosition(full, 0, 1, 1, 1);
            AssertPosition(lineOnly, 0, 1, 1, 0);
            AssertPosition(offsetOnly, 0, 0, 0, 0);
        });
    }

    [Test]
    public void AsciiAndMultibyteScalarsAdvanceByteAndScalarColumnsDifferently()
    {
        JsonLocationTracker tracker = new JsonLocationTracker(true, true);

        tracker.AdvanceScalar(0x41, 1);
        JsonSourcePosition afterAscii = tracker.GetPosition();
        tracker.AdvanceScalar(0x20AC, 3);
        JsonSourcePosition afterEuro = tracker.GetPosition();
        tracker.AdvanceScalar(0x1F600, 4);
        JsonSourcePosition afterEmoji = tracker.GetPosition();
        tracker.AdvanceScalar(0x2028, 3);
        JsonSourcePosition afterLineSeparator = tracker.GetPosition();

        Assert.Multiple(() =>
        {
            AssertPosition(afterAscii, 1, 1, 2, 2);
            AssertPosition(afterEuro, 4, 1, 5, 3);
            AssertPosition(afterEmoji, 8, 1, 9, 4);
            AssertPosition(afterLineSeparator, 11, 1, 12, 5);
        });
    }

    [Test]
    public void LfAndCrEachAdvanceExactlyOneLine()
    {
        JsonLocationTracker lfTracker = new JsonLocationTracker(true, true);
        lfTracker.AdvanceScalar(0x41, 1);
        lfTracker.AdvanceScalar(0x0A, 1);
        JsonLocationTracker crTracker = new JsonLocationTracker(true, true);
        crTracker.AdvanceScalar(0x41, 1);
        crTracker.AdvanceScalar(0x0D, 1);

        Assert.Multiple(() =>
        {
            AssertPosition(lfTracker.GetPosition(), 2, 2, 1, 1);
            Assert.That(lfTracker.HasPendingCarriageReturn(), Is.False);
            AssertPosition(crTracker.GetPosition(), 2, 2, 1, 1);
            Assert.That(crTracker.HasPendingCarriageReturn(), Is.True);
        });
    }

    [Test]
    public void CrLfCountsAsOneLineBreakAcrossChunkBoundaries()
    {
        JsonLocationTracker tracker = new JsonLocationTracker(true, true);
        tracker.AdvanceScalar(0x41, 1);
        tracker.AdvanceScalar(0x0D, 1);
        JsonSourcePosition afterCarriageReturn = tracker.GetPosition();

        tracker.AdvanceScalar(0x0A, 1);
        JsonSourcePosition afterLineFeed = tracker.GetPosition();
        tracker.AdvanceScalar(0x42, 1);
        JsonSourcePosition afterNextScalar = tracker.GetPosition();

        Assert.Multiple(() =>
        {
            AssertPosition(afterCarriageReturn, 2, 2, 1, 1);
            AssertPosition(afterLineFeed, 3, 2, 1, 1);
            Assert.That(tracker.HasPendingCarriageReturn(), Is.False);
            AssertPosition(afterNextScalar, 4, 2, 2, 2);
        });
    }

    [Test]
    public void MixedCrLfSequencesPreserveLineState()
    {
        JsonLocationTracker tracker = new JsonLocationTracker(true, true);
        tracker.AdvanceScalar(0x0D, 1);
        tracker.AdvanceScalar(0x0D, 1);
        tracker.AdvanceScalar(0x0A, 1);
        tracker.AdvanceScalar(0x0A, 1);

        AssertPosition(tracker.GetPosition(), 4, 4, 1, 1);
    }

    [Test]
    public void OffsetOnlyModeStillTracksEveryUtf8Byte()
    {
        JsonLocationTracker tracker = new JsonLocationTracker(false, false);
        tracker.AdvanceScalar(0x41, 1);
        tracker.AdvanceScalar(0x20AC, 3);
        tracker.AdvanceScalar(0x0D, 1);
        tracker.AdvanceScalar(0x0A, 1);

        AssertPosition(tracker.GetPosition(), 6, 0, 0, 0);
    }

    [Test]
    public void RangeEndpointsAreIndependentPositionSnapshots()
    {
        JsonLocationTracker tracker = new JsonLocationTracker(true, true);
        JsonSourcePosition start = tracker.GetPosition();
        tracker.AdvanceScalar(0x20AC, 3);
        JsonSourceRange range = tracker.CreateRange(start);
        tracker.AdvanceScalar(0x41, 1);

        Assert.Multiple(() =>
        {
            Assert.That(range.Start, Is.SameAs(start));
            AssertPosition(range.Start ?? throw new InvalidOperationException(), 0, 1, 1, 1);
            AssertPosition(range.End ?? throw new InvalidOperationException(), 3, 1, 4, 2);
            AssertPosition(tracker.GetPosition(), 4, 1, 5, 3);
        });
    }

    [Test]
    public void CursorByteLengthsCanDriveLocationsAcrossSplitInput()
    {
        JsonByteQueue queue = new JsonByteQueue(16);
        Utf8Cursor cursor = new Utf8Cursor(queue);
        JsonLocationTracker tracker = new JsonLocationTracker(true, true);
        queue.EnqueueBytes(new byte[] { 0x41, 0xE2 });

        int ascii = cursor.ReadScalar(false);
        tracker.AdvanceScalar(ascii, cursor.GetLastScalarByteCount());
        Assert.That(cursor.ReadScalar(false), Is.EqualTo(Utf8Decoder.NeedMoreData));

        queue.EnqueueBytes(new byte[] { 0x82, 0xAC, 0x0D });
        int euro = cursor.ReadScalar(false);
        tracker.AdvanceScalar(euro, cursor.GetLastScalarByteCount());
        int carriageReturn = cursor.ReadScalar(false);
        tracker.AdvanceScalar(carriageReturn, cursor.GetLastScalarByteCount());

        queue.EnqueueBytes(new byte[] { 0x0A, 0x42 });
        int lineFeed = cursor.ReadScalar(false);
        tracker.AdvanceScalar(lineFeed, cursor.GetLastScalarByteCount());
        int trailingAscii = cursor.ReadScalar(true);
        tracker.AdvanceScalar(trailingAscii, cursor.GetLastScalarByteCount());

        AssertPosition(tracker.GetPosition(), 7, 2, 2, 2);
    }

    [Test]
    public void InvalidTrackingAndScalarArgumentsAreRejected()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => new JsonLocationTracker(false, true), Throws.TypeOf<JsonParseException>());
            Assert.That(() => JsonLocationTracker.GetRequiredByteCount(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => JsonLocationTracker.GetRequiredByteCount(0xD800), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => JsonLocationTracker.GetRequiredByteCount(0x110000), Throws.TypeOf<ArgumentOutOfRangeException>());
        });

        JsonLocationTracker tracker = new JsonLocationTracker(true, true);
        Assert.Multiple(() =>
        {
            Assert.That(() => tracker.AdvanceScalar(0x7F, 2), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => tracker.AdvanceScalar(0x80, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    [Test]
    public void ResetRestoresInitialCoordinatesAndCrLfState()
    {
        JsonLocationTracker tracker = new JsonLocationTracker(true, true);
        tracker.AdvanceScalar(0x41, 1);
        tracker.AdvanceScalar(0x0D, 1);

        tracker.ResetTracker();

        Assert.Multiple(() =>
        {
            AssertPosition(tracker.GetPosition(), 0, 1, 1, 1);
            Assert.That(tracker.HasPendingCarriageReturn(), Is.False);
        });
    }

    private static void AssertPosition(
        JsonSourcePosition position,
        long byteOffset,
        int line,
        int byteColumn,
        int scalarColumn)
    {
        Assert.Multiple(() =>
        {
            Assert.That(position.ByteOffset, Is.EqualTo(byteOffset));
            Assert.That(position.Line, Is.EqualTo(line));
            Assert.That(position.ByteColumn, Is.EqualTo(byteColumn));
            Assert.That(position.ScalarColumn, Is.EqualTo(scalarColumn));
        });
    }
}
