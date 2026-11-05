namespace http.Tests;

public class Http1LimitTrackerTests
{
    [Test]
    public void ContentLimitAccumulatesAcrossDeliveriesAndResets()
    {
        HttpLimits limits = new HttpLimits();
        limits.MaximumContentSize = 5;
        Http1LimitTracker tracker = new Http1LimitTracker();
        tracker.Reset(limits);

        bool first = tracker.AddContentSize(2);
        bool boundary = tracker.AddContentSize(3);
        bool exceeded = tracker.AddContentSize(1);

        tracker.Reset(limits);
        bool resetBoundary = tracker.AddContentSize(5);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(boundary, Is.True);
            Assert.That(exceeded, Is.False);
            Assert.That(resetBoundary, Is.True);
        });

        tracker.AddContentSize(1);
        Assert.That(tracker.ErrorCode, Is.EqualTo("http1-content-limit-exceeded"));
    }

    [Test]
    public void ChunkLimitAcceptsBoundaryAndRejectsLargerOrInvalidSizes()
    {
        HttpLimits limits = new HttpLimits();
        limits.MaximumChunkSize = 5;
        Http1LimitTracker tracker = new Http1LimitTracker();
        tracker.Reset(limits);

        bool boundary = tracker.CheckChunkSize(5);
        bool exceeded = tracker.CheckChunkSize(6);
        string? exceededCode = tracker.ErrorCode;
        bool negative = tracker.CheckChunkSize(-1);

        Assert.Multiple(() =>
        {
            Assert.That(boundary, Is.True);
            Assert.That(exceeded, Is.False);
            Assert.That(exceededCode, Is.EqualTo("http1-chunk-limit-exceeded"));
            Assert.That(negative, Is.False);
            Assert.That(tracker.ErrorCode, Is.EqualTo("invalid-http1-limit-size"));
        });
    }

    [Test]
    public void TrailerLimitsIncludeSharedLineAndValueBounds()
    {
        HttpLimits limits = new HttpLimits();
        limits.MaximumFieldLineSize = 5;
        limits.MaximumFieldValueSize = 2;
        limits.MaximumTrailerSectionSize = 7;
        limits.MaximumTrailerCount = 1;
        Http1LimitTracker tracker = new Http1LimitTracker();
        tracker.Reset(limits);

        bool field = tracker.TrackTrailerLine(5, 2, true);
        bool continuation = tracker.TrackTrailerLine(2, 2, false);
        bool count = tracker.TrackTrailerLine(0, 0, true);
        string? countCode = tracker.ErrorCode;

        tracker.Reset(limits);
        bool line = tracker.TrackTrailerLine(6, 0, true);
        string? lineCode = tracker.ErrorCode;
        bool value = tracker.TrackTrailerLine(5, 3, true);
        string? valueCode = tracker.ErrorCode;

        tracker.Reset(limits);
        tracker.TrackTrailerLine(5, 2, true);
        bool section = tracker.TrackTrailerLine(3, 2, false);

        Assert.Multiple(() =>
        {
            Assert.That(field, Is.True);
            Assert.That(continuation, Is.True);
            Assert.That(count, Is.False);
            Assert.That(countCode, Is.EqualTo("http1-trailer-count-limit-exceeded"));
            Assert.That(line, Is.False);
            Assert.That(lineCode, Is.EqualTo("http1-field-line-limit-exceeded"));
            Assert.That(value, Is.False);
            Assert.That(valueCode, Is.EqualTo("http1-field-value-limit-exceeded"));
            Assert.That(section, Is.False);
            Assert.That(tracker.ErrorCode,
                Is.EqualTo("http1-trailer-section-limit-exceeded"));
        });
    }

    [Test]
    public void BufferedInputAndEventsShareOneRetainedByteBudget()
    {
        HttpLimits limits = new HttpLimits();
        limits.MaximumBufferedSize = 5;
        Http1LimitTracker tracker = new Http1LimitTracker();
        tracker.Reset(limits);

        bool input = tracker.TrackBufferedInput(2);
        bool eventBoundary = tracker.AddBufferedEventSize(3);
        bool eventExceeded = tracker.AddBufferedEventSize(1);
        string? eventCode = tracker.ErrorCode;
        bool removed = tracker.RemoveBufferedEventSize(3);
        bool inputExceeded = tracker.TrackBufferedInput(6);

        Assert.Multiple(() =>
        {
            Assert.That(input, Is.True);
            Assert.That(eventBoundary, Is.True);
            Assert.That(eventExceeded, Is.False);
            Assert.That(eventCode, Is.EqualTo("http1-buffer-limit-exceeded"));
            Assert.That(removed, Is.True);
            Assert.That(inputExceeded, Is.False);
            Assert.That(tracker.ErrorCode, Is.EqualTo("http1-buffer-limit-exceeded"));
        });
    }
}
