using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace diameter.Tests;

public class DuplicateRequestStoreTests
{
    [Test]
    public void FirstAndRepeatedObservationOwnInputsAndExposeMetadata()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        DuplicateRequestStore store = new DuplicateRequestStore(
            clock,
            4,
            5000);
        DiameterMessage firstRequest = CreateDuplicateStoreRequest(
            "Origin.Example",
            10,
            100,
            false);
        DiameterIdentity suppliedOrigin = CreateDuplicateStoreIdentity(
            "ORIGIN.EXAMPLE");

        DiameterDuplicateResult first =
            store.ObserveDiameterDuplicateRequest(
                firstRequest,
                suppliedOrigin,
                clock.UtcTime);
        firstRequest.CommandCode = 999;
        firstRequest.Avps[1].Data[0] = 9;
        suppliedOrigin.Value = "changed.example";
        clock.AdvanceDiscoveryTime(1);
        DiameterMessage repeatedRequest = CreateDuplicateStoreRequest(
            "origin.example",
            11,
            100,
            true);
        DiameterDuplicateResult repeated =
            store.ObserveDiameterDuplicateRequest(
                repeatedRequest,
                CreateDuplicateStoreIdentity("origin.example"),
                clock.UtcTime);

        Assert.Multiple(() =>
        {
            Assert.That(first.Duplicate, Is.False);
            Assert.That(first.Retransmission, Is.False);
            Assert.That(first.ObservationCount, Is.EqualTo(1));
            Assert.That(first.Code,
                Is.EqualTo("duplicate-request-first-observation"));
            Assert.That(first.OriginHost.Value, Is.EqualTo("origin.example"));
            Assert.That(first.Request!.CommandCode, Is.EqualTo(300));
            Assert.That(first.Request.Avps[1].Data, Is.EqualTo(new byte[] { 1 }));
            Assert.That(first.FirstSeenAt,
                Is.EqualTo(clock.UtcTime.AddSeconds(-1)));
            Assert.That(first.ExpiresAt,
                Is.EqualTo(clock.UtcTime.AddSeconds(4)));
            Assert.That(repeated.Duplicate, Is.True);
            Assert.That(repeated.Retransmission, Is.True);
            Assert.That(repeated.Code,
                Is.EqualTo("duplicate-request-detected"));
            Assert.That(repeated.ObservationCount, Is.EqualTo(2));
            Assert.That(repeated.FirstSeenAt,
                Is.EqualTo(clock.UtcTime.AddSeconds(-1)));
            Assert.That(repeated.LastSeenAt, Is.EqualTo(clock.UtcTime));
            Assert.That(repeated.ExpiresAt,
                Is.EqualTo(clock.UtcTime.AddSeconds(5)));
            Assert.That(repeated.CachedAnswerAvailable, Is.False);
            Assert.That(repeated.ReplaySafe, Is.False);
            Assert.That(store.ReadDuplicateRequestCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ReplaySafeAnswerIsOwnedAndCorrelatedToCurrentHopIdentifier()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        DuplicateRequestStore concrete = new DuplicateRequestStore(
            clock,
            4,
            5000);
        IDuplicateRequestStore store = concrete;
        DiameterMessage request = CreateDuplicateStoreRequest(
            "origin.example",
            20,
            200,
            false);
        DiameterIdentity origin = CreateDuplicateStoreIdentity(
            "origin.example");
        DiameterDuplicateResult observed =
            store.ObserveDiameterDuplicateRequest(
                request,
                origin,
                clock.UtcTime);
        observed.CachedAnswer = CreateDuplicateStoreAnswer(request, 7);
        observed.ReplaySafe = true;

        store.CacheDiameterDuplicateAnswer(observed);
        observed.CachedAnswer.Avps[0].Data[3] = 9;
        DiameterMessage retry = CreateDuplicateStoreRequest(
            "origin.example",
            21,
            200,
            true);
        DiameterDuplicateResult replay =
            store.ObserveDiameterDuplicateRequest(
                retry,
                origin,
                clock.UtcTime);
        replay.CachedAnswer!.Avps[0].Data[3] = 8;
        replay.ReplaySafe = false;
        store.CacheDiameterDuplicateAnswer(replay);
        DiameterMessage secondRetry = CreateDuplicateStoreRequest(
            "origin.example",
            22,
            200,
            true);
        DiameterDuplicateResult secondReplay =
            store.ObserveDiameterDuplicateRequest(
                secondRetry,
                origin,
                clock.UtcTime);

        Assert.Multiple(() =>
        {
            Assert.That(observed.CachedAnswerAvailable, Is.True);
            Assert.That(replay.Duplicate, Is.True);
            Assert.That(replay.ReplaySafe, Is.False);
            Assert.That(replay.CachedAnswerAvailable, Is.False);
            Assert.That(replay.Code,
                Is.EqualTo("duplicate-answer-replay-available"));
            Assert.That(replay.CachedAnswer.HopByHopIdentifier,
                Is.EqualTo(21));
            Assert.That(replay.CachedAnswer.EndToEndIdentifier,
                Is.EqualTo(200));
            Assert.That(secondReplay.CachedAnswer!.HopByHopIdentifier,
                Is.EqualTo(22));
            Assert.That(secondReplay.ReplaySafe, Is.True);
            Assert.That(secondReplay.CachedAnswerAvailable, Is.True);
            Assert.That(NetworkByteOrder.ReadUInt32(
                secondReplay.CachedAnswer.Avps[0].Data),
                Is.EqualTo(DiameterResultCodes.Success));
            Assert.That(concrete.Entries.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void UnsafeAnswerPolicyDoesNotExposeCachedAnswer()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        DuplicateRequestStore store = new DuplicateRequestStore(
            clock,
            4,
            5000);
        DiameterMessage request = CreateDuplicateStoreRequest(
            "origin.example",
            30,
            300,
            false);
        DiameterIdentity origin = CreateDuplicateStoreIdentity(
            "origin.example");
        DiameterDuplicateResult observed =
            store.ObserveDiameterDuplicateRequest(
                request,
                origin,
                clock.UtcTime);
        observed.CachedAnswer = CreateDuplicateStoreAnswer(request, 1);
        observed.ReplaySafe = false;

        store.CacheDiameterDuplicateAnswer(observed);
        DiameterDuplicateResult repeated =
            store.ObserveDiameterDuplicateRequest(
                CreateDuplicateStoreRequest(
                    "origin.example",
                    31,
                    300,
                    false),
                origin,
                clock.UtcTime);

        Assert.Multiple(() =>
        {
            Assert.That(observed.CachedAnswerAvailable, Is.False);
            Assert.That(repeated.Duplicate, Is.True);
            Assert.That(repeated.Retransmission, Is.False);
            Assert.That(repeated.CachedAnswer, Is.Null);
            Assert.That(repeated.CachedAnswerAvailable, Is.False);
            Assert.That(repeated.ReplaySafe, Is.False);
            Assert.That(repeated.Code,
                Is.EqualTo("duplicate-request-detected"));
        });
    }

    [Test]
    public void ReusedKeyWithChangedRequestNeverReplaysCachedState()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        DuplicateRequestStore store = new DuplicateRequestStore(
            clock,
            4,
            5000);
        DiameterIdentity origin = CreateDuplicateStoreIdentity(
            "origin.example");
        DiameterMessage original = CreateDuplicateStoreRequest(
            "origin.example",
            40,
            400,
            false);
        DiameterDuplicateResult observed =
            store.ObserveDiameterDuplicateRequest(
                original,
                origin,
                clock.UtcTime);
        observed.CachedAnswer = CreateDuplicateStoreAnswer(original, 1);
        observed.ReplaySafe = true;
        store.CacheDiameterDuplicateAnswer(observed);
        DiameterMessage changedPayload = CreateDuplicateStoreRequest(
            "origin.example",
            41,
            400,
            true);
        changedPayload.Avps[1].Data[0] = 2;
        DiameterDuplicateResult payloadCollision =
            store.ObserveDiameterDuplicateRequest(
                changedPayload,
                origin,
                clock.UtcTime);
        DiameterMessage changedCommand = CreateDuplicateStoreRequest(
            "origin.example",
            42,
            400,
            true);
        changedCommand.CommandCode = 301;
        DiameterDuplicateResult commandCollision =
            store.ObserveDiameterDuplicateRequest(
                changedCommand,
                origin,
                clock.UtcTime);
        DiameterDuplicateResult originalAgain =
            store.ObserveDiameterDuplicateRequest(
                CreateDuplicateStoreRequest(
                    "origin.example",
                    43,
                    400,
                    true),
                origin,
                clock.UtcTime);

        Assert.Multiple(() =>
        {
            Assert.That(payloadCollision.Duplicate, Is.True);
            Assert.That(payloadCollision.Code,
                Is.EqualTo("duplicate-request-key-collision"));
            Assert.That(payloadCollision.CachedAnswer, Is.Null);
            Assert.That(payloadCollision.ReplaySafe, Is.False);
            Assert.That(commandCollision.Code,
                Is.EqualTo("duplicate-request-key-collision"));
            Assert.That(commandCollision.CommandCode, Is.EqualTo(301));
            Assert.That(originalAgain.Code,
                Is.EqualTo("duplicate-answer-replay-available"));
            Assert.That(originalAgain.ObservationCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void StoreValidatesRequestOriginAndCachedAnswerCorrelation()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        DuplicateRequestStore store = new DuplicateRequestStore(
            clock,
            4,
            5000);
        DiameterIdentity origin = CreateDuplicateStoreIdentity(
            "origin.example");
        DiameterMessage missingOrigin = CreateDuplicateStoreRequest(
            "origin.example",
            50,
            500,
            false);
        missingOrigin.Avps.RemoveAt(0);
        DiameterMessage answer = new DiameterMessage();
        DiameterMessage request = CreateDuplicateStoreRequest(
            "origin.example",
            51,
            501,
            false);
        DiameterDuplicateResult observed =
            store.ObserveDiameterDuplicateRequest(
                request,
                origin,
                clock.UtcTime);
        observed.CachedAnswer = answer;
        observed.ReplaySafe = true;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                store.ObserveDiameterDuplicateRequest(
                    null,
                    origin,
                    clock.UtcTime));
            Assert.Throws<ArgumentException>(() =>
                store.ObserveDiameterDuplicateRequest(
                    missingOrigin,
                    origin,
                    clock.UtcTime));
            Assert.Throws<ArgumentException>(() =>
                store.ObserveDiameterDuplicateRequest(
                    CreateDuplicateStoreRequest(
                        "origin.example",
                        52,
                        502,
                        false),
                    CreateDuplicateStoreIdentity("other.example"),
                    clock.UtcTime));
            Assert.Throws<ArgumentException>(() =>
                store.CacheDiameterDuplicateAnswer(observed));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DuplicateRequestStore(clock, 0, 5000));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DuplicateRequestStore(clock, 1, 0));
            Assert.That(store.ReadDuplicateRequestCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void CapacityUsesRecentObservationAndExpiryRemovesEntries()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        DuplicateRequestStore store = new DuplicateRequestStore(
            clock,
            2,
            2000);
        DiameterIdentity origin = CreateDuplicateStoreIdentity(
            "origin.example");
        store.ObserveDiameterDuplicateRequest(
            CreateDuplicateStoreRequest("origin.example", 60, 600, false),
            origin,
            clock.UtcTime);
        store.ObserveDiameterDuplicateRequest(
            CreateDuplicateStoreRequest("origin.example", 61, 601, false),
            origin,
            clock.UtcTime);
        store.ObserveDiameterDuplicateRequest(
            CreateDuplicateStoreRequest("origin.example", 62, 600, true),
            origin,
            clock.UtcTime);
        store.ObserveDiameterDuplicateRequest(
            CreateDuplicateStoreRequest("origin.example", 63, 602, false),
            origin,
            clock.UtcTime);
        string firstKey = store.CreateDuplicateRequestKey(
            "origin.example",
            600);
        string evictedKey = store.CreateDuplicateRequestKey(
            "origin.example",
            601);
        string latestKey = store.CreateDuplicateRequestKey(
            "origin.example",
            602);

        Assert.Multiple(() =>
        {
            Assert.That(store.ReadDuplicateRequestCount(), Is.EqualTo(2));
            Assert.That(store.Entries.ContainsKey(firstKey), Is.True);
            Assert.That(store.Entries.ContainsKey(evictedKey), Is.False);
            Assert.That(store.Entries.ContainsKey(latestKey), Is.True);
        });

        clock.AdvanceDiscoveryTime(2);

        Assert.Multiple(() =>
        {
            Assert.That(store.ReadDuplicateRequestCount(), Is.Zero);
            Assert.That(store.Entries, Is.Empty);
            Assert.That(store.EntryOrder, Is.Empty);
        });
    }

    [Test]
    public void ConcurrentObservationHasOneFirstSeenWinnerAndExactCount()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        DuplicateRequestStore store = new DuplicateRequestStore(
            clock,
            4,
            5000);
        DiameterMessage request = CreateDuplicateStoreRequest(
            "origin.example",
            70,
            700,
            true);
        DiameterIdentity origin = CreateDuplicateStoreIdentity(
            "origin.example");
        Barrier barrier = new Barrier(17);
        List<Task<DiameterDuplicateResult>> tasks =
            new List<Task<DiameterDuplicateResult>>();
        int index = 0;
        while (index < 16)
        {
            tasks.Add(Task.Run(() =>
            {
                barrier.SignalAndWait();
                return store.ObserveDiameterDuplicateRequest(
                    request,
                    origin,
                    clock.UtcTime);
            }));
            index = index + 1;
        }

        barrier.SignalAndWait();
        Task.WaitAll(tasks.ToArray());
        int first = 0;
        int duplicate = 0;
        int maximumCount = 0;
        index = 0;
        while (index < tasks.Count)
        {
            DiameterDuplicateResult result = tasks[index].Result;
            if (result.Duplicate)
            {
                duplicate = duplicate + 1;
            }
            else
            {
                first = first + 1;
            }

            if (result.ObservationCount > maximumCount)
            {
                maximumCount = result.ObservationCount;
            }

            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(1));
            Assert.That(duplicate, Is.EqualTo(15));
            Assert.That(maximumCount, Is.EqualTo(16));
            Assert.That(store.ReadDuplicateRequestCount(), Is.EqualTo(1));
            Assert.That(store.Entries[store.CreateDuplicateRequestKey(
                "origin.example",
                700)].ObservationCount, Is.EqualTo(16));
        });
    }

    [Test]
    public void ExpiredObservationCannotBeUsedToCacheAnAnswer()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        DuplicateRequestStore store = new DuplicateRequestStore(
            clock,
            1,
            1000);
        DiameterMessage request = CreateDuplicateStoreRequest(
            "origin.example",
            80,
            800,
            false);
        DiameterDuplicateResult observed =
            store.ObserveDiameterDuplicateRequest(
                request,
                CreateDuplicateStoreIdentity("origin.example"),
                clock.UtcTime);
        observed.CachedAnswer = CreateDuplicateStoreAnswer(request, 1);
        observed.ReplaySafe = true;
        clock.AdvanceDiscoveryTime(1);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                store.CacheDiameterDuplicateAnswer(observed));
            Assert.That(store.ReadDuplicateRequestCount(), Is.Zero);
        });
    }

    [Test]
    public void CanonicalOriginFormsShareKeyEquivalenceAndReplayState()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        DuplicateRequestStore store = new DuplicateRequestStore(
            clock,
            4,
            5000);
        DiameterMessage request = CreateDuplicateStoreRequest(
            "origin.example",
            90,
            900,
            false);
        DiameterDuplicateResult observed =
            store.ObserveDiameterDuplicateRequest(
                request,
                CreateDuplicateStoreIdentity("origin.example"),
                clock.UtcTime);
        observed.CachedAnswer = CreateDuplicateStoreAnswer(request, 1);
        observed.ReplaySafe = true;
        store.CacheDiameterDuplicateAnswer(observed);
        DiameterMessage retransmission = CreateDuplicateStoreRequest(
            "origin.example",
            91,
            900,
            true);
        retransmission.Avps[0].Data = Encoding.ASCII.GetBytes(
            "ORIGIN.EXAMPLE.");

        DiameterDuplicateResult replay =
            store.ObserveDiameterDuplicateRequest(
                retransmission,
                CreateDuplicateStoreIdentity("ORIGIN.EXAMPLE."),
                clock.UtcTime);

        Assert.Multiple(() =>
        {
            Assert.That(replay.Duplicate, Is.True);
            Assert.That(replay.Retransmission, Is.True);
            Assert.That(replay.Code,
                Is.EqualTo("duplicate-answer-replay-available"));
            Assert.That(replay.OriginHost.Value,
                Is.EqualTo("origin.example"));
            Assert.That(replay.Request!.Avps[0].Data,
                Is.EqualTo(Encoding.ASCII.GetBytes("ORIGIN.EXAMPLE.")));
            Assert.That(replay.CachedAnswerAvailable, Is.True);
            Assert.That(replay.CachedAnswer!.HopByHopIdentifier,
                Is.EqualTo(91U));
            Assert.That(store.ReadDuplicateRequestCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void OriginAndEndToEndDimensionsIsolateDuplicateKeys()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        DuplicateRequestStore store = new DuplicateRequestStore(
            clock,
            4,
            5000);
        DiameterDuplicateResult first =
            store.ObserveDiameterDuplicateRequest(
                CreateDuplicateStoreRequest(
                    "first.example", 100, 1000, false),
                CreateDuplicateStoreIdentity("first.example"),
                clock.UtcTime);
        DiameterDuplicateResult differentOrigin =
            store.ObserveDiameterDuplicateRequest(
                CreateDuplicateStoreRequest(
                    "second.example", 101, 1000, false),
                CreateDuplicateStoreIdentity("second.example"),
                clock.UtcTime);
        DiameterDuplicateResult differentEndToEnd =
            store.ObserveDiameterDuplicateRequest(
                CreateDuplicateStoreRequest(
                    "first.example", 102, 1001, false),
                CreateDuplicateStoreIdentity("first.example"),
                clock.UtcTime);
        DiameterDuplicateResult repeated =
            store.ObserveDiameterDuplicateRequest(
                CreateDuplicateStoreRequest(
                    "first.example", 103, 1000, true),
                CreateDuplicateStoreIdentity("first.example"),
                clock.UtcTime);

        Assert.Multiple(() =>
        {
            Assert.That(first.Duplicate, Is.False);
            Assert.That(differentOrigin.Duplicate, Is.False);
            Assert.That(differentEndToEnd.Duplicate, Is.False);
            Assert.That(repeated.Duplicate, Is.True);
            Assert.That(repeated.ObservationCount, Is.EqualTo(2));
            Assert.That(store.ReadDuplicateRequestCount(), Is.EqualTo(3));
        });
    }

    [Test]
    public void RetransmissionMetadataIsIndependentFromDuplicateStatus()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        DuplicateRequestStore store = new DuplicateRequestStore(
            clock,
            2,
            5000);
        DiameterIdentity origin = CreateDuplicateStoreIdentity(
            "origin.example");

        DiameterDuplicateResult first =
            store.ObserveDiameterDuplicateRequest(
                CreateDuplicateStoreRequest(
                    "origin.example", 110, 1100, true),
                origin,
                clock.UtcTime);
        DiameterDuplicateResult repeated =
            store.ObserveDiameterDuplicateRequest(
                CreateDuplicateStoreRequest(
                    "origin.example", 111, 1100, false),
                origin,
                clock.UtcTime);

        Assert.Multiple(() =>
        {
            Assert.That(first.Duplicate, Is.False);
            Assert.That(first.Retransmission, Is.True);
            Assert.That(repeated.Duplicate, Is.True);
            Assert.That(repeated.Retransmission, Is.False);
            Assert.That(repeated.Request!.Flags.HasFlag(
                DiameterMessageFlags.PotentiallyRetransmitted), Is.False);
        });
    }

    [Test]
    public void CapacityEvictionInvalidatesMetadataAndPreservesTouchedReplay()
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        DuplicateRequestStore store = new DuplicateRequestStore(
            clock,
            2,
            5000);
        DiameterIdentity origin = CreateDuplicateStoreIdentity(
            "origin.example");
        DiameterMessage firstRequest = CreateDuplicateStoreRequest(
            "origin.example", 120, 1200, false);
        DiameterDuplicateResult first =
            store.ObserveDiameterDuplicateRequest(
                firstRequest, origin, clock.UtcTime);
        first.CachedAnswer = CreateDuplicateStoreAnswer(firstRequest, 1);
        first.ReplaySafe = true;
        store.CacheDiameterDuplicateAnswer(first);
        DiameterMessage secondRequest = CreateDuplicateStoreRequest(
            "origin.example", 121, 1201, false);
        DiameterDuplicateResult second =
            store.ObserveDiameterDuplicateRequest(
                secondRequest, origin, clock.UtcTime);
        store.ObserveDiameterDuplicateRequest(
            CreateDuplicateStoreRequest(
                "origin.example", 122, 1200, true),
            origin,
            clock.UtcTime);
        store.ObserveDiameterDuplicateRequest(
            CreateDuplicateStoreRequest(
                "origin.example", 123, 1202, false),
            origin,
            clock.UtcTime);
        second.CachedAnswer = CreateDuplicateStoreAnswer(secondRequest, 2);
        second.ReplaySafe = true;

        DiameterDuplicateResult replay =
            store.ObserveDiameterDuplicateRequest(
                CreateDuplicateStoreRequest(
                    "origin.example", 124, 1200, true),
                origin,
                clock.UtcTime);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                store.CacheDiameterDuplicateAnswer(second));
            Assert.That(replay.CachedAnswerAvailable, Is.True);
            Assert.That(replay.CachedAnswer!.HopByHopIdentifier,
                Is.EqualTo(124U));
            Assert.That(store.Entries.ContainsKey(
                store.CreateDuplicateRequestKey(
                    "origin.example", 1201)), Is.False);
            Assert.That(store.ReadDuplicateRequestCount(), Is.EqualTo(2));
        });
    }

    [TestCase("version")]
    [TestCase("command")]
    [TestCase("application")]
    [TestCase("hop-by-hop")]
    [TestCase("end-to-end")]
    [TestCase("proxyable")]
    [TestCase("request-bit")]
    [TestCase("retransmitted-bit")]
    [TestCase("reserved-bit")]
    [TestCase("avp-storage")]
    public void ReplaySafetyRejectsEveryHeaderAndCorrelationMutation(
        string mutation)
    {
        FakeDiscoveryClock clock = new FakeDiscoveryClock();
        DuplicateRequestStore store = new DuplicateRequestStore(
            clock,
            2,
            5000);
        DiameterMessage request = CreateDuplicateStoreRequest(
            "origin.example",
            130,
            1300,
            false);
        DiameterDuplicateResult observed =
            store.ObserveDiameterDuplicateRequest(
                request,
                CreateDuplicateStoreIdentity("origin.example"),
                clock.UtcTime);
        DiameterMessage answer = CreateDuplicateStoreAnswer(request, 1);
        ApplyDuplicateStoreAnswerMutation(answer, mutation);
        observed.CachedAnswer = answer;
        observed.ReplaySafe = true;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                store.CacheDiameterDuplicateAnswer(observed));
            Assert.That(observed.CachedAnswerAvailable, Is.False);
            Assert.That(store.Entries[store.CreateDuplicateRequestKey(
                "origin.example", 1300)].CachedAnswer, Is.Null);
            Assert.That(store.Entries[store.CreateDuplicateRequestKey(
                "origin.example", 1300)].ReplaySafe, Is.False);
        });
    }

    private static void ApplyDuplicateStoreAnswerMutation(
        DiameterMessage answer,
        string mutation)
    {
        if (mutation == "version")
        {
            answer.Version++;
        }
        else if (mutation == "command")
        {
            answer.CommandCode++;
        }
        else if (mutation == "application")
        {
            answer.ApplicationId++;
        }
        else if (mutation == "hop-by-hop")
        {
            answer.HopByHopIdentifier++;
        }
        else if (mutation == "end-to-end")
        {
            answer.EndToEndIdentifier++;
        }
        else if (mutation == "proxyable")
        {
            answer.Flags = answer.Flags & ~DiameterMessageFlags.Proxyable;
        }
        else if (mutation == "request-bit")
        {
            answer.Flags = answer.Flags | DiameterMessageFlags.Request;
        }
        else if (mutation == "retransmitted-bit")
        {
            answer.Flags = answer.Flags |
                DiameterMessageFlags.PotentiallyRetransmitted;
        }
        else if (mutation == "reserved-bit")
        {
            answer.Flags = answer.Flags | (DiameterMessageFlags)1;
        }
        else
        {
            answer.Avps = null!;
        }
    }

    private static DiameterMessage CreateDuplicateStoreRequest(
        string originHost,
        uint hopByHopIdentifier,
        uint endToEndIdentifier,
        bool retransmission)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = DiameterMessageFlags.Request |
            DiameterMessageFlags.Proxyable;
        if (retransmission)
        {
            result.Flags = result.Flags |
                DiameterMessageFlags.PotentiallyRetransmitted;
        }

        result.CommandCode = 300;
        result.ApplicationId = 4;
        result.HopByHopIdentifier = hopByHopIdentifier;
        result.EndToEndIdentifier = endToEndIdentifier;
        result.Avps.Add(
            new DiameterBaseAvpFactory().CreateOriginHostAvp(
                CreateDuplicateStoreIdentity(originHost)));
        DiameterAvp state = new DiameterAvp();
        state.Code = 900;
        state.Data = new byte[] { 1 };
        result.Avps.Add(state);
        return result;
    }

    private static DiameterMessage CreateDuplicateStoreAnswer(
        DiameterMessage request,
        byte marker)
    {
        DiameterMessage result = new DiameterMessage();
        result.Flags = request.Flags & DiameterMessageFlags.Proxyable;
        result.CommandCode = request.CommandCode;
        result.ApplicationId = request.ApplicationId;
        result.HopByHopIdentifier = request.HopByHopIdentifier;
        result.EndToEndIdentifier = request.EndToEndIdentifier;
        DiameterAvp resultCode = new DiameterAvp();
        resultCode.Code = DiameterAvpCodes.ResultCode;
        resultCode.Flags = DiameterAvpFlags.Mandatory;
        resultCode.Data = new byte[4];
        NetworkByteOrder.WriteUInt32(
            resultCode.Data,
            DiameterResultCodes.Success);
        result.Avps.Add(resultCode);
        DiameterAvp markerAvp = new DiameterAvp();
        markerAvp.Code = 901;
        markerAvp.Data = new byte[] { marker };
        result.Avps.Add(markerAvp);
        return result;
    }

    private static DiameterIdentity CreateDuplicateStoreIdentity(string value)
    {
        DiameterIdentity result = new DiameterIdentity();
        result.Value = value;
        return result;
    }
}
