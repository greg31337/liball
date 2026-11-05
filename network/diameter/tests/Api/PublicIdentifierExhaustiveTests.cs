using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class PublicIdentifierExhaustiveTests
{
    [Test]
    public void SameSecondSourceLifetimesUseMonotonicSessionSeparation()
    {
        DateTimeOffset startup = DateTimeOffset.FromUnixTimeSeconds(1704067200L);
        IDiameterIdentifierSource first = new DiameterIdentifierSource(
            new TestDiameterClock(startup, 100L));
        IDiameterIdentifierSource restarted = new DiameterIdentifierSource(
            new TestDiameterClock(startup, 200L));
        DiameterIdentity originHost = CreateIdentity("node.example");

        string firstSession = first.CreateSessionIdentifier(originHost, null);
        string restartedSession = restarted.CreateSessionIdentifier(originHost, null);

        Assert.Multiple(() =>
        {
            Assert.That(firstSession, Is.EqualTo("node.example;3913056000;101"));
            Assert.That(restartedSession, Is.EqualTo("node.example;3913056000;201"));
            Assert.That(restartedSession, Is.Not.EqualTo(firstSession));
        });
    }

    [Test]
    public void EveryPublicIdentifierRemainsUniqueAcrossASequentialBatch()
    {
        const int count = 65536;
        IDiameterIdentifierSource source = new DiameterIdentifierSource(
            new TestDiameterClock(DateTimeOffset.FromUnixTimeSeconds(1704067200L), 0L));
        DiameterIdentity originHost = CreateIdentity("node.example");
        HashSet<uint> hopByHop = new HashSet<uint>();
        HashSet<uint> endToEnd = new HashSet<uint>();
        HashSet<string> sessions = new HashSet<string>();
        uint originState = source.CreateOriginStateIdentifier();
        int index = 0;
        while (index < count)
        {
            hopByHop.Add(source.CreateHopByHopIdentifier());
            endToEnd.Add(source.CreateEndToEndIdentifier());
            sessions.Add(source.CreateSessionIdentifier(originHost, null));
            Assert.That(source.CreateOriginStateIdentifier(), Is.EqualTo(originState));
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(hopByHop, Has.Count.EqualTo(count));
            Assert.That(endToEnd, Has.Count.EqualTo(count));
            Assert.That(sessions, Has.Count.EqualTo(count));
        });
    }

    [Test]
    public void SessionIdentifierFormattingIsExactAtBothUnsignedBoundaries()
    {
        DiameterIdentifierSource source = new DiameterIdentifierSource(
            new TestDiameterClock(DateTimeOffset.UnixEpoch, 0L));
        source.SessionHighSequence = uint.MaxValue;
        source.SessionSequence = uint.MaxValue - 1U;
        DiameterIdentity originHost = CreateIdentity("NODE.Example.COM.");

        string maximum = source.CreateSessionIdentifier(originHost, "device;alpha");
        string wrapped = source.CreateSessionIdentifier(originHost, "");

        Assert.Multiple(() =>
        {
            Assert.That(maximum,
                Is.EqualTo("node.example.com;4294967295;4294967295;device;alpha"));
            Assert.That(wrapped, Is.EqualTo("node.example.com;0;0"));
        });
    }

    [Test]
    public void InvalidSessionInputsDoNotConsumeTheMonotonicSequence()
    {
        DiameterIdentifierSource source = new DiameterIdentifierSource(
            new TestDiameterClock(DateTimeOffset.UnixEpoch, 41L));
        DiameterIdentity valid = CreateIdentity("node.example");
        DiameterIdentity invalid = CreateIdentity("bad_name.example");

        Assert.Throws<ArgumentException>(() => source.CreateSessionIdentifier(invalid, null));
        Assert.Throws<ArgumentException>(() =>
            source.CreateSessionIdentifier(valid, "invalid\0text"));
        Assert.Throws<ArgumentException>(() =>
            source.CreateSessionIdentifier(valid, "invalid\ud800text"));
        string firstValid = source.CreateSessionIdentifier(valid, "subscriber-🚀");

        Assert.That(firstValid,
            Is.EqualTo("node.example;2208988800;42;subscriber-🚀"));
    }

    [Test]
    public void RestartAtALaterStartupSecondChangesSessionAndOriginStateValues()
    {
        DiameterIdentity originHost = CreateIdentity("node.example");
        IDiameterIdentifierSource first = new DiameterIdentifierSource(
            new TestDiameterClock(DateTimeOffset.FromUnixTimeSeconds(1704067200L), 0L));
        IDiameterIdentifierSource restarted = new DiameterIdentifierSource(
            new TestDiameterClock(DateTimeOffset.FromUnixTimeSeconds(1704067201L), 0L));

        string firstSession = first.CreateSessionIdentifier(originHost, null);
        string restartedSession = restarted.CreateSessionIdentifier(originHost, null);
        uint firstOriginState = first.CreateOriginStateIdentifier();
        uint restartedOriginState = restarted.CreateOriginStateIdentifier();
        uint firstEndToEnd = first.CreateEndToEndIdentifier();
        uint restartedEndToEnd = restarted.CreateEndToEndIdentifier();

        Assert.Multiple(() =>
        {
            Assert.That(firstSession, Is.EqualTo("node.example;3913056000;1"));
            Assert.That(restartedSession, Is.EqualTo("node.example;3913056001;1"));
            Assert.That(restartedSession, Is.Not.EqualTo(firstSession));
            Assert.That(firstOriginState, Is.EqualTo(1704067200U));
            Assert.That(restartedOriginState, Is.EqualTo(1704067201U));
            Assert.That(restartedOriginState, Is.GreaterThan(firstOriginState));
            Assert.That(restartedEndToEnd, Is.Not.EqualTo(firstEndToEnd));
        });
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = value;
        return identity;
    }
}
