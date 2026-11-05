using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace diameter.Tests;

public class ClockAndIdentifierTests
{
    [Test]
    public void ClockContractProvidesInjectableUtcAndMonotonicTime()
    {
        DateTimeOffset expectedUtc = DateTimeOffset.FromUnixTimeSeconds(1704067200L);
        TestDiameterClock clock = new TestDiameterClock(expectedUtc, 2468L);
        IDiameterClock clockContract = clock;

        Assert.Multiple(() =>
        {
            Assert.That(clockContract.ReadUtcTime(), Is.EqualTo(expectedUtc));
            Assert.That(clockContract.ReadMonotonicMilliseconds(), Is.EqualTo(2468L));
        });
    }

    [Test]
    public void SystemClockProvidesCurrentUtcAndNondecreasingMonotonicTime()
    {
        DiameterSystemClock clock = new DiameterSystemClock();
        DateTimeOffset before = DateTimeOffset.UtcNow;
        long firstMonotonic = clock.ReadMonotonicMilliseconds();
        DateTimeOffset actualUtc = clock.ReadUtcTime();
        long secondMonotonic = clock.ReadMonotonicMilliseconds();
        DateTimeOffset after = DateTimeOffset.UtcNow;

        Assert.Multiple(() =>
        {
            Assert.That(actualUtc, Is.InRange(before, after));
            Assert.That(actualUtc.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(secondMonotonic, Is.GreaterThanOrEqualTo(firstMonotonic));
        });
    }

    [Test]
    public void IdentifierSourceUsesClockAndIndependentSequences()
    {
        DateTimeOffset startup = DateTimeOffset.FromUnixTimeSeconds(1704067200L);
        TestDiameterClock clock = new TestDiameterClock(startup, 99L);
        IDiameterIdentifierSource source = new DiameterIdentifierSource(clock);
        DiameterIdentity originHost = new DiameterIdentity();
        originHost.Value = "node.example";

        uint firstHopByHop = source.CreateHopByHopIdentifier();
        uint secondHopByHop = source.CreateHopByHopIdentifier();
        uint originState = source.CreateOriginStateIdentifier();
        string correlatedSession = source.CreateSessionIdentifier(originHost, "request-1");
        string plainSession = source.CreateSessionIdentifier(originHost, null);

        Assert.Multiple(() =>
        {
            Assert.That(secondHopByHop, Is.EqualTo(firstHopByHop + 1U));
            Assert.That(originState, Is.EqualTo(1704067200U));
            Assert.That(source.CreateOriginStateIdentifier(), Is.EqualTo(originState));
            Assert.That(correlatedSession, Is.EqualTo("node.example;3913056000;100;request-1"));
            Assert.That(plainSession, Is.EqualTo("node.example;3913056000;101"));
        });
    }

    [Test]
    public void IdentifierStartupSeedsCombineEntropyTimeAndMonotonicState()
    {
        uint hopByHopSeed = DiameterIdentifierSource.CreateHopByHopSeed(
            0x10203040U,
            0x55667788U,
            0x01020304U);
        uint endToEndSeed = DiameterIdentifierSource.CreateEndToEndSeed(
            0x10203040U,
            0x12345678U,
            0x01020304U);

        Assert.Multiple(() =>
        {
            Assert.That(hopByHopSeed, Is.EqualTo(0x444444CCU));
            Assert.That(endToEndSeed, Is.EqualTo(0x67823344U));
        });
    }

    [Test]
    public void EndToEndIdentifierUsesStartupTimeAndAdvancesAcrossLowBitBoundary()
    {
        TestDiameterClock clock = new TestDiameterClock(
            DateTimeOffset.FromUnixTimeSeconds(4097L),
            0L);
        DiameterIdentifierSource source = new DiameterIdentifierSource(clock);

        Assert.That(source.EndToEndSequence >> 20, Is.EqualTo(1U));

        source.EndToEndSequence = 0x001ffffeU;
        clock.UtcTime = DateTimeOffset.FromUnixTimeSeconds(8192L);

        uint lastLowSequenceValue = source.CreateEndToEndIdentifier();
        uint advancedTimeValue = source.CreateEndToEndIdentifier();

        Assert.Multiple(() =>
        {
            Assert.That(lastLowSequenceValue, Is.EqualTo(0x001fffffU));
            Assert.That(advancedTimeValue, Is.EqualTo(0x00200000U));
        });
    }

    [Test]
    public void ProtocolIdentifierSequencesWrapAcrossEntireUnsignedRange()
    {
        TestDiameterClock clock = new TestDiameterClock(DateTimeOffset.UnixEpoch, 0L);
        DiameterIdentifierSource source = new DiameterIdentifierSource(clock);
        source.HopByHopSequence = uint.MaxValue - 1U;
        source.EndToEndSequence = uint.MaxValue - 1U;

        uint lastHopByHop = source.CreateHopByHopIdentifier();
        uint wrappedHopByHop = source.CreateHopByHopIdentifier();
        uint lastEndToEnd = source.CreateEndToEndIdentifier();
        uint wrappedEndToEnd = source.CreateEndToEndIdentifier();

        Assert.Multiple(() =>
        {
            Assert.That(lastHopByHop, Is.EqualTo(uint.MaxValue));
            Assert.That(wrappedHopByHop, Is.Zero);
            Assert.That(lastEndToEnd, Is.EqualTo(uint.MaxValue));
            Assert.That(wrappedEndToEnd, Is.Zero);
        });
    }

    [Test]
    public void ConcurrentIdentifierCreationDoesNotCollide()
    {
        const int identifierCount = 20000;
        TestDiameterClock clock = new TestDiameterClock(DateTimeOffset.UnixEpoch, 0L);
        IDiameterIdentifierSource source = new DiameterIdentifierSource(clock);
        ConcurrentDictionary<uint, byte> hopByHopIdentifiers = new ConcurrentDictionary<uint, byte>();
        ConcurrentDictionary<uint, byte> endToEndIdentifiers = new ConcurrentDictionary<uint, byte>();
        ConcurrentDictionary<string, byte> sessionIdentifiers = new ConcurrentDictionary<string, byte>();
        DiameterIdentity originHost = new DiameterIdentity();
        originHost.Value = "node.example";

        Parallel.For(0, identifierCount, index =>
        {
            hopByHopIdentifiers.TryAdd(source.CreateHopByHopIdentifier(), 0);
            endToEndIdentifiers.TryAdd(source.CreateEndToEndIdentifier(), 0);
            sessionIdentifiers.TryAdd(source.CreateSessionIdentifier(originHost, null), 0);
        });

        Assert.Multiple(() =>
        {
            Assert.That(hopByHopIdentifiers.Count, Is.EqualTo(identifierCount));
            Assert.That(endToEndIdentifiers.Count, Is.EqualTo(identifierCount));
            Assert.That(sessionIdentifiers.Count, Is.EqualTo(identifierCount));
        });
    }

    [Test]
    public void SessionIdentifierCanonicalizesIdentityAndValidatesInputs()
    {
        TestDiameterClock clock = new TestDiameterClock(DateTimeOffset.UnixEpoch, 0L);
        DiameterIdentifierSource source = new DiameterIdentifierSource(clock);
        DiameterIdentity missingIdentity = new DiameterIdentity();
        DiameterIdentity invalidIdentity = new DiameterIdentity();
        invalidIdentity.Value = "bad_name.example";
        DiameterIdentity canonicalIdentity = new DiameterIdentity();
        canonicalIdentity.Value = "NODE.Example.COM.";

        string identifier = source.CreateSessionIdentifier(canonicalIdentity, "subscriber-α");

        Assert.Multiple(() =>
        {
            Assert.That(identifier, Is.EqualTo("node.example.com;2208988800;1;subscriber-α"));
            Assert.Throws<ArgumentException>(() => source.CreateSessionIdentifier(missingIdentity, null));
            Assert.Throws<ArgumentException>(() => source.CreateSessionIdentifier(invalidIdentity, null));
            Assert.Throws<ArgumentException>(() => source.CreateSessionIdentifier(null!, null));
            Assert.Throws<ArgumentException>(() =>
                source.CreateSessionIdentifier(canonicalIdentity, "invalid\0text"));
            Assert.Throws<ArgumentException>(() =>
                source.CreateSessionIdentifier(canonicalIdentity, "invalid\ud800text"));
        });
    }

    [Test]
    public void IdentifierSourceRejectsMissingClockAndAdvancesSessionHighSequenceOnWrap()
    {
        Assert.Throws<ArgumentException>(() => new DiameterIdentifierSource(null!));

        TestDiameterClock clock = new TestDiameterClock(DateTimeOffset.UnixEpoch, 0L);
        DiameterIdentifierSource source = new DiameterIdentifierSource(clock);
        source.SessionHighSequence = 42U;
        source.SessionSequence = uint.MaxValue;
        DiameterIdentity originHost = new DiameterIdentity();
        originHost.Value = "node.example";

        string wrappedSession = source.CreateSessionIdentifier(originHost, "");

        Assert.That(wrappedSession, Is.EqualTo("node.example;43;0"));
        Assert.That(
            DiameterIdentifierSource.FoldUnsigned(-1L),
            Is.EqualTo(uint.MaxValue));
    }

    [Test]
    public void SourceLifetimeKeepsOriginStateStableAndRestartAdvancesStartupValues()
    {
        TestDiameterClock firstClock = new TestDiameterClock(
            DateTimeOffset.FromUnixTimeSeconds(1704067200L),
            0L);
        IDiameterIdentifierSource firstSource = new DiameterIdentifierSource(firstClock);
        DiameterIdentity originHost = new DiameterIdentity();
        originHost.Value = "node.example";

        firstClock.UtcTime = DateTimeOffset.FromUnixTimeSeconds(1704067300L);
        uint stableOriginState = firstSource.CreateOriginStateIdentifier();
        string stableStartupSession = firstSource.CreateSessionIdentifier(originHost, null);

        TestDiameterClock restartedClock = new TestDiameterClock(
            DateTimeOffset.FromUnixTimeSeconds(1704067201L),
            0L);
        IDiameterIdentifierSource restartedSource = new DiameterIdentifierSource(restartedClock);

        Assert.Multiple(() =>
        {
            Assert.That(stableOriginState, Is.EqualTo(1704067200U));
            Assert.That(firstSource.CreateOriginStateIdentifier(), Is.EqualTo(stableOriginState));
            Assert.That(stableStartupSession, Is.EqualTo("node.example;3913056000;1"));
            Assert.That(restartedSource.CreateOriginStateIdentifier(), Is.EqualTo(1704067201U));
            Assert.That(
                restartedSource.CreateSessionIdentifier(originHost, null),
                Is.EqualTo("node.example;3913056001;1"));
        });
    }

    [Test]
    public void IdentifierSourceNormalizesPreEpochTimeAndFoldsTimeEras()
    {
        TestDiameterClock preEpochClock = new TestDiameterClock(
            DateTimeOffset.FromUnixTimeSeconds(-1L),
            0L);
        IDiameterIdentifierSource preEpochSource = new DiameterIdentifierSource(preEpochClock);
        DiameterIdentity originHost = new DiameterIdentity();
        originHost.Value = "node.example";

        TestDiameterClock nextEraClock = new TestDiameterClock(
            DateTimeOffset.FromUnixTimeSeconds(4294967297L),
            0L);
        IDiameterIdentifierSource nextEraSource = new DiameterIdentifierSource(nextEraClock);

        Assert.Multiple(() =>
        {
            Assert.That(preEpochSource.CreateOriginStateIdentifier(), Is.Zero);
            Assert.That(
                preEpochSource.CreateSessionIdentifier(originHost, null),
                Is.EqualTo("node.example;2208988800;1"));
            Assert.That(nextEraSource.CreateOriginStateIdentifier(), Is.EqualTo(1U));
            Assert.That(
                nextEraSource.CreateSessionIdentifier(originHost, null),
                Is.EqualTo("node.example;2208988801;1"));
        });
    }
}

public class TestDiameterClock : IDiameterClock
{
    public DateTimeOffset UtcTime;
    public long MonotonicMilliseconds;

    public TestDiameterClock(DateTimeOffset utcTime, long monotonicMilliseconds)
    {
        UtcTime = utcTime;
        MonotonicMilliseconds = monotonicMilliseconds;
    }

    public DateTimeOffset ReadUtcTime()
    {
        return UtcTime;
    }

    public long ReadMonotonicMilliseconds()
    {
        return MonotonicMilliseconds;
    }
}
