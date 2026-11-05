using NUnit.Framework;

namespace time.Tests;

[TestFixture]
public class IntervalTests
{
    [Test]
    public void Create_StoresStartEndDurationAndEmptyState()
    {
        Instant start = Instant.Parse("2024-01-01T00:00:00Z");
        Instant end = Instant.Parse("2024-01-01T01:00:00Z");
        Interval interval = Interval.Create(start, end);
        Interval empty = Interval.Create(start, start);

        Assert.That(interval.GetStart(), Is.SameAs(start));
        Assert.That(interval.GetEnd(), Is.SameAs(end));
        Assert.That(interval.GetDuration().EqualsValue(Duration.FromHours(1L)), Is.True);
        Assert.That(interval.IsEmpty(), Is.False);
        Assert.That(empty.IsEmpty(), Is.True);
        Assert.That(empty.GetDuration().EqualsValue(Duration.Zero()), Is.True);
    }

    [Test]
    public void Create_RequiresStartAndEnd()
    {
        Instant start = Instant.Parse("2024-01-01T00:00:00Z");
        Instant end = Instant.Parse("2024-01-01T01:00:00Z");

        ArgumentNullException startException = Assert.Throws<ArgumentNullException>(() => Interval.Create(null!, end))!;
        ArgumentNullException endException = Assert.Throws<ArgumentNullException>(() => Interval.Create(start, null!))!;

        Assert.That(startException.ParamName, Is.EqualTo("start"));
        Assert.That(endException.ParamName, Is.EqualTo("end"));
    }

    [Test]
    public void Create_RejectsEndBeforeStart()
    {
        Instant start = Instant.Parse("2024-01-01T01:00:00Z");
        Instant end = Instant.Parse("2024-01-01T00:00:00Z");

        ArgumentException exception = Assert.Throws<ArgumentException>(() => Interval.Create(start, end))!;

        Assert.That(exception.ParamName, Is.EqualTo("end"));
    }

    [Test]
    public void FromStartAndDuration_CreatesEndFromDuration()
    {
        Instant start = Instant.Parse("2024-01-01T00:00:00Z");
        Interval interval = Interval.FromStartAndDuration(start, Duration.FromMinutes(90L));

        Assert.That(interval.GetStart(), Is.SameAs(start));
        Assert.That(interval.GetEnd().ToIsoString(), Is.EqualTo("2024-01-01T01:30:00Z"));
        Assert.That(interval.GetDuration().EqualsValue(Duration.FromMinutes(90L)), Is.True);
    }

    [Test]
    public void FromStartAndDuration_RequiresStartAndDuration()
    {
        Instant start = Instant.Parse("2024-01-01T00:00:00Z");
        Duration duration = Duration.FromHours(1L);

        ArgumentNullException startException = Assert.Throws<ArgumentNullException>(() => Interval.FromStartAndDuration(null!, duration))!;
        ArgumentNullException durationException = Assert.Throws<ArgumentNullException>(() => Interval.FromStartAndDuration(start, null!))!;

        Assert.That(startException.ParamName, Is.EqualTo("start"));
        Assert.That(durationException.ParamName, Is.EqualTo("duration"));
    }

    [Test]
    public void FromStartAndDuration_RejectsNegativeDuration()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Interval.FromStartAndDuration(Instant.UnixEpoch(), Duration.FromTicks(-1L)))!;

        Assert.That(exception.ParamName, Is.EqualTo("duration"));
    }

    [Test]
    public void ContainsInstant_UsesHalfOpenSemantics()
    {
        Interval interval = CreateHourInterval(0L, 10L);

        Assert.That(interval.ContainsInstant(Instant.FromUnixSeconds(-1L)), Is.False);
        Assert.That(interval.ContainsInstant(Instant.FromUnixSeconds(0L)), Is.True);
        Assert.That(interval.ContainsInstant(Instant.FromUnixSeconds(9L)), Is.True);
        Assert.That(interval.ContainsInstant(Instant.FromUnixSeconds(10L)), Is.False);
        Assert.That(Interval.Create(Instant.UnixEpoch(), Instant.UnixEpoch()).ContainsInstant(Instant.UnixEpoch()), Is.False);
    }

    [Test]
    public void ContainsInstant_RequiresInstant()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => CreateHourInterval(0L, 1L).ContainsInstant(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("instant"));
    }

    [Test]
    public void ContainsInterval_UsesHalfOpenRangeContainment()
    {
        Interval interval = CreateHourInterval(0L, 10L);

        Assert.That(interval.ContainsInterval(CreateHourInterval(0L, 10L)), Is.True);
        Assert.That(interval.ContainsInterval(CreateHourInterval(2L, 8L)), Is.True);
        Assert.That(interval.ContainsInterval(CreateHourInterval(10L, 10L)), Is.True);
        Assert.That(interval.ContainsInterval(CreateHourInterval(-1L, 1L)), Is.False);
        Assert.That(interval.ContainsInterval(CreateHourInterval(9L, 11L)), Is.False);
    }

    [Test]
    public void OverlapsAndAbuts_DistinguishConnectedIntervals()
    {
        Interval first = CreateHourInterval(0L, 10L);
        Interval overlapping = CreateHourInterval(5L, 15L);
        Interval abutting = CreateHourInterval(10L, 20L);
        Interval disconnected = CreateHourInterval(11L, 20L);
        Interval emptyAtFive = CreateHourInterval(5L, 5L);

        Assert.That(first.Overlaps(overlapping), Is.True);
        Assert.That(first.Abuts(overlapping), Is.False);
        Assert.That(first.Overlaps(abutting), Is.False);
        Assert.That(first.Abuts(abutting), Is.True);
        Assert.That(first.Overlaps(disconnected), Is.False);
        Assert.That(first.Abuts(disconnected), Is.False);
        Assert.That(first.Overlaps(emptyAtFive), Is.False);
    }

    [Test]
    public void Intersection_ReturnsOverlapEmptyIntersectionOrNull()
    {
        Interval first = CreateHourInterval(0L, 10L);
        Interval overlap = first.Intersection(CreateHourInterval(5L, 15L))!;
        Interval empty = first.Intersection(CreateHourInterval(10L, 20L))!;
        Interval? disconnected = first.Intersection(CreateHourInterval(11L, 20L));

        Assert.That(overlap.ToIsoString(), Is.EqualTo("1970-01-01T00:00:05Z/1970-01-01T00:00:10Z"));
        Assert.That(empty.IsEmpty(), Is.True);
        Assert.That(empty.GetStart().ToIsoString(), Is.EqualTo("1970-01-01T00:00:10Z"));
        Assert.That(disconnected, Is.Null);
    }

    [Test]
    public void UnionIfConnected_ReturnsUnionForOverlappingOrAbuttingIntervals()
    {
        Interval first = CreateHourInterval(0L, 10L);
        Interval overlap = first.UnionIfConnected(CreateHourInterval(5L, 15L))!;
        Interval abutting = first.UnionIfConnected(CreateHourInterval(10L, 20L))!;
        Interval? disconnected = first.UnionIfConnected(CreateHourInterval(11L, 20L));

        Assert.That(overlap.ToIsoString(), Is.EqualTo("1970-01-01T00:00:00Z/1970-01-01T00:00:15Z"));
        Assert.That(abutting.ToIsoString(), Is.EqualTo("1970-01-01T00:00:00Z/1970-01-01T00:00:20Z"));
        Assert.That(disconnected, Is.Null);
    }

    [Test]
    public void Operations_RequireInterval()
    {
        Interval interval = CreateHourInterval(0L, 10L);

        Assert.That(Assert.Throws<ArgumentNullException>(() => interval.ContainsInterval(null!))!.ParamName, Is.EqualTo("interval"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => interval.Overlaps(null!))!.ParamName, Is.EqualTo("interval"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => interval.Abuts(null!))!.ParamName, Is.EqualTo("interval"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => interval.Intersection(null!))!.ParamName, Is.EqualTo("interval"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => interval.UnionIfConnected(null!))!.ParamName, Is.EqualTo("interval"));
    }

    [Test]
    public void ToIsoString_FormatsStartAndEnd()
    {
        Interval interval = Interval.Create(Instant.Parse("2024-01-01T00:00:00Z"), Instant.Parse("2024-01-01T01:30:00Z"));

        Assert.That(interval.ToIsoString(), Is.EqualTo("2024-01-01T00:00:00Z/2024-01-01T01:30:00Z"));
    }

    [Test]
    public void Parse_ReadsStartEndAndStartDurationForms()
    {
        Interval startEnd = Interval.Parse("2024-01-01T00:00:00Z/2024-01-01T01:30:00Z");
        Interval startDuration = Interval.Parse("2024-01-01T00:00:00Z/PT90M");

        Assert.That(startEnd.GetStart().ToIsoString(), Is.EqualTo("2024-01-01T00:00:00Z"));
        Assert.That(startEnd.GetEnd().ToIsoString(), Is.EqualTo("2024-01-01T01:30:00Z"));
        Assert.That(startDuration.GetStart().ToIsoString(), Is.EqualTo("2024-01-01T00:00:00Z"));
        Assert.That(startDuration.GetEnd().ToIsoString(), Is.EqualTo("2024-01-01T01:30:00Z"));
    }

    [Test]
    public void Parse_RequiresText()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Interval.Parse(null!))!;

        Assert.That(exception.ParamName, Is.EqualTo("text"));
    }

    [TestCase("")]
    [TestCase("2024-01-01T00:00:00Z")]
    [TestCase("/2024-01-01T00:00:00Z")]
    [TestCase("2024-01-01T00:00:00Z/")]
    [TestCase("2024-01-01T00:00:00Z/2024-01-01T01:00:00Z/PT1H")]
    public void Parse_RejectsMalformedText(string text)
    {
        Assert.Throws<FormatException>(() => Interval.Parse(text));
    }

    [Test]
    public void Parse_RejectsNegativeDurationAndEndBeforeStart()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Interval.Parse("2024-01-01T00:00:00Z/-PT1S"));
        Assert.Throws<ArgumentException>(() => Interval.Parse("2024-01-01T01:00:00Z/2024-01-01T00:00:00Z"));
    }

    [Test]
    public void HalfOpenBoundary_AllowsAdjacentIntervalsWithoutOverlap()
    {
        Interval first = CreateHourInterval(0L, 10L);
        Interval second = CreateHourInterval(10L, 20L);
        Instant boundary = Instant.FromUnixSeconds(10L);

        Assert.That(first.ContainsInstant(boundary), Is.False);
        Assert.That(second.ContainsInstant(boundary), Is.True);
        Assert.That(first.Overlaps(second), Is.False);
        Assert.That(first.Abuts(second), Is.True);
        Assert.That(second.Abuts(first), Is.True);
    }

    [Test]
    public void EmptyIntervals_AreContainedAndCanConnectAtTheirPoint()
    {
        Interval interval = CreateHourInterval(0L, 10L);
        Interval emptyInside = CreateHourInterval(5L, 5L);
        Interval emptyAtEnd = CreateHourInterval(10L, 10L);
        Interval emptyOutside = CreateHourInterval(11L, 11L);
        Interval insideIntersection = interval.Intersection(emptyInside)!;
        Interval insideUnion = interval.UnionIfConnected(emptyInside)!;
        Interval endIntersection = interval.Intersection(emptyAtEnd)!;
        Interval endUnion = interval.UnionIfConnected(emptyAtEnd)!;

        Assert.That(interval.ContainsInterval(emptyInside), Is.True);
        Assert.That(interval.ContainsInterval(emptyAtEnd), Is.True);
        Assert.That(interval.ContainsInterval(emptyOutside), Is.False);
        Assert.That(insideIntersection.IsEmpty(), Is.True);
        Assert.That(insideIntersection.GetStart().ToIsoString(), Is.EqualTo("1970-01-01T00:00:05Z"));
        Assert.That(insideUnion.ToIsoString(), Is.EqualTo(interval.ToIsoString()));
        Assert.That(endIntersection.IsEmpty(), Is.True);
        Assert.That(endIntersection.GetStart().ToIsoString(), Is.EqualTo("1970-01-01T00:00:10Z"));
        Assert.That(endUnion.ToIsoString(), Is.EqualTo(interval.ToIsoString()));
        Assert.That(interval.Intersection(emptyOutside), Is.Null);
        Assert.That(interval.UnionIfConnected(emptyOutside), Is.Null);
    }

    [Test]
    public void ConnectedOperations_WorkWhenOtherIntervalStartsBeforeThisInterval()
    {
        Interval later = CreateHourInterval(5L, 15L);
        Interval earlier = CreateHourInterval(0L, 5L);
        Interval overlapping = CreateHourInterval(0L, 10L);
        Interval abuttingIntersection = later.Intersection(earlier)!;
        Interval abuttingUnion = later.UnionIfConnected(earlier)!;
        Interval overlappingIntersection = later.Intersection(overlapping)!;
        Interval overlappingUnion = later.UnionIfConnected(overlapping)!;

        Assert.That(abuttingIntersection.IsEmpty(), Is.True);
        Assert.That(abuttingIntersection.GetStart().ToIsoString(), Is.EqualTo("1970-01-01T00:00:05Z"));
        Assert.That(abuttingUnion.ToIsoString(), Is.EqualTo("1970-01-01T00:00:00Z/1970-01-01T00:00:15Z"));
        Assert.That(overlappingIntersection.ToIsoString(), Is.EqualTo("1970-01-01T00:00:05Z/1970-01-01T00:00:10Z"));
        Assert.That(overlappingUnion.ToIsoString(), Is.EqualTo("1970-01-01T00:00:00Z/1970-01-01T00:00:15Z"));
    }

    [Test]
    public void DisconnectedOperations_ReturnNullInBothOrders()
    {
        Interval first = CreateHourInterval(0L, 10L);
        Interval second = CreateHourInterval(20L, 30L);

        Assert.That(first.Intersection(second), Is.Null);
        Assert.That(second.Intersection(first), Is.Null);
        Assert.That(first.UnionIfConnected(second), Is.Null);
        Assert.That(second.UnionIfConnected(first), Is.Null);
        Assert.That(first.Overlaps(second), Is.False);
        Assert.That(second.Overlaps(first), Is.False);
    }

    [Test]
    public void ParseAndFormat_RoundTripStartEndEmptyAndDurationForms()
    {
        Interval startEnd = Interval.Parse("2024-01-01T00:00:00Z/2024-01-01T00:00:10Z");
        Interval empty = Interval.Parse("2024-01-01T00:00:00Z/2024-01-01T00:00:00Z");
        Interval duration = Interval.Parse("2024-01-01T00:00:00Z/PT10S");
        Interval zeroDuration = Interval.Parse("2024-01-01T00:00:00Z/PT0S");

        Assert.That(Interval.Parse(startEnd.ToIsoString()).ToIsoString(), Is.EqualTo(startEnd.ToIsoString()));
        Assert.That(empty.IsEmpty(), Is.True);
        Assert.That(empty.ToIsoString(), Is.EqualTo("2024-01-01T00:00:00Z/2024-01-01T00:00:00Z"));
        Assert.That(duration.ToIsoString(), Is.EqualTo("2024-01-01T00:00:00Z/2024-01-01T00:00:10Z"));
        Assert.That(zeroDuration.IsEmpty(), Is.True);
    }

    static Interval CreateHourInterval(long startSeconds, long endSeconds)
    {
        return Interval.Create(Instant.FromUnixSeconds(startSeconds), Instant.FromUnixSeconds(endSeconds));
    }
}
