using System.Text;
using rtp;

namespace rtp.Tests.Session;

public class RtpSourceTableTests
{
    [Test]
    public void SequentialRemoteSourceActivatesAndDiscoversContributors()
    {
        RtpSourceTable table = new RtpSourceTable(
            CreateConfiguration(10, 2),
            new FixedRandomSource());
        RtpPacket firstPacket = CreatePacket(100U, 10);
        RtpPacketContext firstContext = CreateContext(5L, "path-a", 90000);

        RtpSequenceUpdate probation = table.RecordRemoteRtpPacket(
            firstPacket,
            firstContext);
        RtpSourceSnapshot? probationSnapshot =
            table.FindRemoteSourceSnapshot(100U);

        RtpPacket secondPacket = CreatePacket(100U, 11);
        secondPacket.ContributingSources = new uint[] { 201U, 200U, 201U };
        RtpSequenceUpdate accepted = table.RecordRemoteRtpPacket(
            secondPacket,
            CreateContext(6L, "path-b", 90000));
        RtpSourceSnapshot[] snapshots = table.GetRemoteSourceSnapshots();

        Assert.Multiple(() =>
        {
            Assert.That(probation.Status,
                Is.EqualTo(RtpSequenceStatus.Probation));
            Assert.That(probationSnapshot?.Status,
                Is.EqualTo(RtpSourceStatus.Probationary));
            Assert.That(probationSnapshot?.IsSender, Is.False);
            Assert.That(probationSnapshot?.LastRtpActivityTicks,
                Is.EqualTo(5L));
            Assert.That(accepted.Status,
                Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(snapshots.Select(value => value.SourceIdentifier),
                Is.EqualTo(new uint[] { 100U, 200U, 201U }));
            Assert.That(snapshots[0].Status,
                Is.EqualTo(RtpSourceStatus.ActiveSender));
            Assert.That(snapshots[0].IsSender, Is.True);
            Assert.That(snapshots[0].TransportIdentity, Is.EqualTo("path-b"));
            Assert.That(snapshots[0].PayloadType, Is.EqualTo(96));
            Assert.That(snapshots[0].ClockRate, Is.EqualTo(90000));
            Assert.That(snapshots[1].Status,
                Is.EqualTo(RtpSourceStatus.ActiveMember));
            Assert.That(snapshots[2].Status,
                Is.EqualTo(RtpSourceStatus.ActiveMember));
            Assert.That(snapshots[1].LastRtpActivityTicks, Is.EqualTo(6L));
            Assert.That(table.GetRemoteMemberCount(), Is.EqualTo(3));
            Assert.That(table.GetRemoteSenderCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void RtcpActivityAndExplicitSenderChangesPreserveMembership()
    {
        RtpSourceTable table = new RtpSourceTable(
            CreateConfiguration(4, 2),
            new FixedRandomSource());

        RtpSourceSnapshot member = table.RecordRemoteRtcpActivity(
            300U,
            CreateContext(10L, "control-a", 0));
        RtpSourceSnapshot sender = table.UpdateRemoteSenderState(300U, true);
        RtpSourceSnapshot demoted = table.UpdateRemoteSenderState(300U, false);
        RtpSourceSnapshot refreshed = table.RecordRemoteRtcpActivity(
            300U,
            CreateContext(12L, "control-b", 0));

        Assert.Multiple(() =>
        {
            Assert.That(member.Status,
                Is.EqualTo(RtpSourceStatus.ActiveMember));
            Assert.That(member.LastRtcpActivityTicks, Is.EqualTo(10L));
            Assert.That(sender.Status,
                Is.EqualTo(RtpSourceStatus.ActiveSender));
            Assert.That(sender.IsSender, Is.True);
            Assert.That(demoted.Status,
                Is.EqualTo(RtpSourceStatus.ActiveMember));
            Assert.That(demoted.IsSender, Is.False);
            Assert.That(refreshed.TransportIdentity, Is.EqualTo("control-b"));
            Assert.That(refreshed.LastRtcpActivityTicks, Is.EqualTo(12L));
            Assert.That(table.GetRemoteMemberCount(), Is.EqualTo(1));
            Assert.That(table.GetRemoteSenderCount(), Is.Zero);
        });

        Assert.Throws<RtpSessionException>(() =>
            table.RecordRemoteRtcpActivity(
                300U,
                CreateContext(11L, "old", 0)));
        Assert.Throws<RtpSessionException>(() =>
            table.UpdateRemoteSenderState(999U, true));
    }

    [Test]
    public void MemberObservedByRtcpRemainsActiveDuringRtpProbation()
    {
        RtpSourceTable table = new RtpSourceTable(
            CreateConfiguration(4, 2),
            new FixedRandomSource());
        table.RecordRemoteRtcpActivity(
            400U,
            CreateContext(1L, "path", 0));

        RtpSequenceUpdate probation = table.RecordRemoteRtpPacket(
            CreatePacket(400U, 20),
            CreateContext(2L, "path", 8000));
        RtpSourceSnapshot? member = table.FindRemoteSourceSnapshot(400U);
        RtpSequenceUpdate accepted = table.RecordRemoteRtpPacket(
            CreatePacket(400U, 21),
            CreateContext(3L, "path", 8000));
        RtpSourceSnapshot? sender = table.FindRemoteSourceSnapshot(400U);

        Assert.Multiple(() =>
        {
            Assert.That(probation.Status,
                Is.EqualTo(RtpSequenceStatus.Probation));
            Assert.That(member?.Status,
                Is.EqualTo(RtpSourceStatus.ActiveMember));
            Assert.That(member?.IsSender, Is.False);
            Assert.That(accepted.Status,
                Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(sender?.Status,
                Is.EqualTo(RtpSourceStatus.ActiveSender));
            Assert.That(sender?.IsSender, Is.True);
        });
    }

    [Test]
    public void CapacityChecksAreAtomicAndDeduplicateContributors()
    {
        RtpSourceTable table = new RtpSourceTable(
            CreateConfiguration(3, 1),
            new FixedRandomSource());
        RtpPacket initial = CreatePacket(100U, 10);
        initial.ContributingSources = new uint[] { 200U, 200U };
        table.RecordRemoteRtpPacket(
            initial,
            CreateContext(1L, "path", 8000));
        table.RecordRemoteRtcpActivity(
            300U,
            CreateContext(2L, "control", 0));
        RtpSourceSnapshot[] beforeFailure = table.GetRemoteSourceSnapshots();
        RtpPacket overflow = CreatePacket(100U, 11);
        overflow.ContributingSources = new uint[] { 400U };

        Assert.Throws<RtpBufferCapacityException>(() =>
            table.RecordRemoteRtpPacket(
                overflow,
                CreateContext(3L, "path", 8000)));
        RtpSourceSnapshot[] afterFailure = table.GetRemoteSourceSnapshots();

        Assert.Multiple(() =>
        {
            Assert.That(beforeFailure.Select(value => value.SourceIdentifier),
                Is.EqualTo(new uint[] { 100U, 200U, 300U }));
            Assert.That(afterFailure.Select(value => value.SourceIdentifier),
                Is.EqualTo(new uint[] { 100U, 200U, 300U }));
            Assert.That(afterFailure[0].SequenceUpdate?.SequenceNumber,
                Is.EqualTo(10));
            Assert.That(table.FindRemoteSourceSnapshot(400U), Is.Null);
        });
    }

    [Test]
    public void SnapshotsAndConfigurationAreIsolatedAndResettable()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(3, 1);
        RtpSourceTable table = new RtpSourceTable(
            configuration,
            new FixedRandomSource());
        configuration.MaximumSourceCount = 1;
        configuration.MinimumSequential = 5;
        table.RecordRemoteRtpPacket(
            CreatePacket(500U, 30),
            CreateContext(1L, "path", 48000));
        RtpSourceSnapshot snapshot = table.GetRemoteSourceSnapshots()[0];
        snapshot.TransportIdentity = "changed";
        snapshot.SequenceUpdate!.SequenceNumber = 99;

        RtpSourceSnapshot? fresh = table.FindRemoteSourceSnapshot(500U);
        table.RecordRemoteRtcpActivity(
            600U,
            CreateContext(2L, "control", 0));
        table.ClearRemoteSources();

        Assert.Multiple(() =>
        {
            Assert.That(fresh?.TransportIdentity, Is.EqualTo("path"));
            Assert.That(fresh?.SequenceUpdate?.SequenceNumber, Is.EqualTo(30));
            Assert.That(fresh?.Status,
                Is.EqualTo(RtpSourceStatus.ActiveSender));
            Assert.That(table.GetRemoteSourceSnapshots(), Is.Empty);
            Assert.That(table.GetRemoteMemberCount(), Is.Zero);
            Assert.That(table.GetRemoteSenderCount(), Is.Zero);
        });
    }

    [Test]
    public void InvalidConfigurationAndActivityAreRejectedBeforeMutation()
    {
        RtpSessionConfiguration invalidConfiguration =
            CreateConfiguration(0, 2);
        Assert.Throws<RtpConfigurationException>(() =>
            new RtpSourceTable(invalidConfiguration, new FixedRandomSource()));
        Assert.Throws<RtpConfigurationException>(() =>
            new RtpSourceTable(CreateConfiguration(2, 2), null!));

        RtpSourceTable table = new RtpSourceTable(
            CreateConfiguration(2, 2),
            new FixedRandomSource());
        RtpPacket invalidPacket = CreatePacket(100U, 1);
        invalidPacket.Version = 1;
        RtpPacketContext invalidContext = CreateContext(-1L, "path", 8000);

        Assert.Throws<RtpPacketFormatException>(() =>
            table.RecordRemoteRtpPacket(
                invalidPacket,
                CreateContext(0L, "path", 8000)));
        Assert.Throws<RtpConfigurationException>(() =>
            table.RecordRemoteRtpPacket(
                CreatePacket(100U, 1),
                invalidContext));
        Assert.Throws<RtpConfigurationException>(() =>
            table.RecordRemoteRtpPacket(
                CreatePacket(100U, 1),
                CreateContext(0L, "path", 0)));

        Assert.That(table.GetRemoteSourceSnapshots(), Is.Empty);
    }

    [Test]
    public void SourceDescriptionsAssociateCanonicalNamesAndReplaceMetadata()
    {
        RtpSourceTable table = new RtpSourceTable(
            CreateConfiguration(4, 1),
            new FixedRandomSource());
        RtcpSourceDescription description = new RtcpSourceDescription();
        description.Chunks = new RtcpSourceDescriptionChunk[]
        {
            CreateSourceDescriptionChunk(200U, "alice", "Alice"),
            CreateSourceDescriptionChunk(100U, "alice", "")
        };

        RtpSourceSnapshot[] updates = table.RecordRemoteSourceDescription(
            description,
            CreateContext(5L, "control-a", 0));
        description.Chunks[0].Items[0].Value[0] = (byte)'X';
        RtpSourceSnapshot[] associated =
            table.FindRemoteSourcesByCanonicalName("alice");

        RtcpSourceDescription metadataUpdate = new RtcpSourceDescription();
        metadataUpdate.Chunks = new RtcpSourceDescriptionChunk[]
        {
            CreateSourceDescriptionChunk(200U, "", "Alice Updated")
        };
        table.RecordRemoteSourceDescription(
            metadataUpdate,
            CreateContext(6L, "control-b", 0));
        RtpSourceSnapshot? preserved = table.FindRemoteSourceSnapshot(200U);
        RtpSourceState? preservedState = table.FindSourceState(200U);

        RtcpSourceDescription reassociation = new RtcpSourceDescription();
        reassociation.Chunks = new RtcpSourceDescriptionChunk[]
        {
            CreateSourceDescriptionChunk(100U, "bob", "")
        };
        table.RecordRemoteSourceDescription(
            reassociation,
            CreateContext(7L, "control-c", 0));

        Assert.Multiple(() =>
        {
            Assert.That(updates.Select(value => value.SourceIdentifier),
                Is.EqualTo(new uint[] { 200U, 100U }));
            Assert.That(associated.Select(value => value.SourceIdentifier),
                Is.EqualTo(new uint[] { 100U, 200U }));
            Assert.That(associated.Select(value => value.CanonicalName),
                Is.EqualTo(new string[] { "alice", "alice" }));
            Assert.That(preserved?.CanonicalName, Is.EqualTo("alice"));
            Assert.That(preserved?.TransportIdentity,
                Is.EqualTo("control-b"));
            Assert.That(preserved?.LastRtcpActivityTicks, Is.EqualTo(6L));
            Assert.That(preservedState?.sourceDescriptionItems.Length,
                Is.EqualTo(1));
            Assert.That(Encoding.UTF8.GetString(
                preservedState!.sourceDescriptionItems[0].Value),
                Is.EqualTo("Alice Updated"));
            Assert.That(table.FindRemoteSourcesByCanonicalName("alice")
                .Select(value => value.SourceIdentifier),
                Is.EqualTo(new uint[] { 200U }));
            Assert.That(table.FindRemoteSourcesByCanonicalName("bob")
                .Select(value => value.SourceIdentifier),
                Is.EqualTo(new uint[] { 100U }));
        });
    }

    [Test]
    public void InvalidSourceDescriptionsAreRejectedAtomically()
    {
        RtpSourceTable table = new RtpSourceTable(
            CreateConfiguration(3, 1),
            new FixedRandomSource());
        table.RecordRemoteRtcpActivity(
            100U,
            CreateContext(3L, "control", 0));
        RtcpSourceDescription invalid = new RtcpSourceDescription();
        invalid.Chunks = new RtcpSourceDescriptionChunk[]
        {
            CreateSourceDescriptionChunk(200U, "alice", ""),
            CreateSourceDescriptionChunk(200U, "bob", "")
        };

        Assert.Throws<RtcpPacketFormatException>(() =>
            table.RecordRemoteSourceDescription(
                invalid,
                CreateContext(4L, "control", 0)));

        RtcpSourceDescription invalidUtf8 = new RtcpSourceDescription();
        RtcpSourceDescriptionChunk invalidChunk =
            CreateSourceDescriptionChunk(300U, "", "");
        RtcpSourceDescriptionItem invalidName =
            new RtcpSourceDescriptionItem();
        invalidName.Type = RtcpSdesItemType.CanonicalName;
        invalidName.Value = new byte[] { 0xc3, 0x28 };
        invalidChunk.Items = new RtcpSourceDescriptionItem[] { invalidName };
        invalidUtf8.Chunks =
            new RtcpSourceDescriptionChunk[] { invalidChunk };

        Assert.Throws<RtcpPacketFormatException>(() =>
            table.RecordRemoteSourceDescription(
                invalidUtf8,
                CreateContext(4L, "control", 0)));
        Assert.Multiple(() =>
        {
            Assert.That(table.GetRemoteSourceSnapshots()
                .Select(value => value.SourceIdentifier),
                Is.EqualTo(new uint[] { 100U }));
            Assert.Throws<RtpConfigurationException>(() =>
                table.FindRemoteSourcesByCanonicalName(""));
        });
    }

    [Test]
    public void GoodbyeMarksKnownSourcesUntilExplicitRemoval()
    {
        RtpSourceTable table = new RtpSourceTable(
            CreateConfiguration(4, 1),
            new FixedRandomSource());
        table.RecordRemoteRtpPacket(
            CreatePacket(100U, 1),
            CreateContext(1L, "media", 8000));
        table.RecordRemoteRtcpActivity(
            200U,
            CreateContext(1L, "control", 0));
        RtcpGoodbye goodbye = new RtcpGoodbye();
        goodbye.Sources = new uint[] { 100U, 999U, 100U };
        goodbye.HasReason = true;
        goodbye.Reason = Encoding.UTF8.GetBytes("leaving");

        RtpSourceSnapshot[] marked = table.RecordRemoteGoodbye(
            goodbye,
            CreateContext(2L, "control-b", 0));
        goodbye.Reason[0] = (byte)'X';
        RtpSourceSnapshot? goodbyeSnapshot =
            table.FindRemoteSourceSnapshot(100U);
        RtpSourceState? goodbyeState = table.FindSourceState(100U);

        Assert.Multiple(() =>
        {
            Assert.That(marked.Length, Is.EqualTo(1));
            Assert.That(marked[0].SourceIdentifier, Is.EqualTo(100U));
            Assert.That(marked[0].Status,
                Is.EqualTo(RtpSourceStatus.Goodbye));
            Assert.That(marked[0].IsSender, Is.False);
            Assert.That(goodbyeSnapshot?.Status,
                Is.EqualTo(RtpSourceStatus.Goodbye));
            Assert.That(goodbyeSnapshot?.TransportIdentity,
                Is.EqualTo("control-b"));
            Assert.That(Encoding.UTF8.GetString(goodbyeState!.goodbyeReason),
                Is.EqualTo("leaving"));
            Assert.That(table.GetRemoteMemberCount(), Is.EqualTo(1));
            Assert.That(table.GetRemoteSenderCount(), Is.Zero);
        });

        Assert.That(table.RemoveGoodbyeSources(), Is.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(table.FindRemoteSourceSnapshot(100U), Is.Null);
            Assert.That(table.FindRemoteSourceSnapshot(200U), Is.Not.Null);
            Assert.That(table.RemoveGoodbyeSources(), Is.Zero);
        });
    }

    [Test]
    public void SourceTimeoutsDemoteSendersAndRemoveStaleMembersAtBoundaries()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(4, 1);
        configuration.RtcpScheduler.SenderTimeoutTicks = 5L;
        configuration.RtcpScheduler.MemberTimeoutTicks = 10L;
        RtpSourceTable table = new RtpSourceTable(
            configuration,
            new FixedRandomSource());
        table.RecordRemoteRtpPacket(
            CreatePacket(100U, 1),
            CreateContext(0L, "media", 8000));
        table.RecordRemoteRtcpActivity(
            100U,
            CreateContext(3L, "control", 0));
        table.RecordRemoteRtcpActivity(
            200U,
            CreateContext(1L, "control", 0));

        RtpSourceSnapshot[] early = table.ExpireRemoteSourceActivity(4L);
        RtpSourceSnapshot[] demoted = table.ExpireRemoteSourceActivity(5L);
        RtpSourceSnapshot[] retained = table.ExpireRemoteSourceActivity(10L);
        RtpSourceSnapshot[] firstExpired =
            table.ExpireRemoteSourceActivity(11L);
        RtpSourceSnapshot[] secondExpired =
            table.ExpireRemoteSourceActivity(13L);

        Assert.Multiple(() =>
        {
            Assert.That(early, Is.Empty);
            Assert.That(demoted.Length, Is.EqualTo(1));
            Assert.That(demoted[0].SourceIdentifier, Is.EqualTo(100U));
            Assert.That(demoted[0].Status,
                Is.EqualTo(RtpSourceStatus.ActiveMember));
            Assert.That(demoted[0].IsSender, Is.False);
            Assert.That(retained, Is.Empty);
            Assert.That(firstExpired.Length, Is.EqualTo(1));
            Assert.That(firstExpired[0].SourceIdentifier, Is.EqualTo(200U));
            Assert.That(firstExpired[0].Status,
                Is.EqualTo(RtpSourceStatus.Inactive));
            Assert.That(secondExpired.Length, Is.EqualTo(1));
            Assert.That(secondExpired[0].SourceIdentifier, Is.EqualTo(100U));
            Assert.That(table.GetRemoteSourceSnapshots(), Is.Empty);
            Assert.Throws<RtpSessionException>(() =>
                table.ExpireRemoteSourceActivity(12L));
            Assert.Throws<RtpSessionException>(() =>
                table.RecordRemoteRtcpActivity(
                    300U,
                    CreateContext(12L, "old", 0)));
        });
    }

    [Test]
    public void TimeoutConfigurationIsCopiedAndZeroDisablesExpiry()
    {
        RtpSessionConfiguration copiedConfiguration =
            CreateConfiguration(2, 1);
        copiedConfiguration.RtcpScheduler.SenderTimeoutTicks = 2L;
        copiedConfiguration.RtcpScheduler.MemberTimeoutTicks = 4L;
        RtpSourceTable copiedTable = new RtpSourceTable(
            copiedConfiguration,
            new FixedRandomSource());
        copiedConfiguration.RtcpScheduler.SenderTimeoutTicks = 20L;
        copiedConfiguration.RtcpScheduler.MemberTimeoutTicks = 40L;
        copiedTable.RecordRemoteRtpPacket(
            CreatePacket(100U, 1),
            CreateContext(0L, "media", 8000));

        RtpSessionConfiguration disabledConfiguration =
            CreateConfiguration(2, 1);
        RtpSourceTable disabledTable = new RtpSourceTable(
            disabledConfiguration,
            new FixedRandomSource());
        disabledTable.RecordRemoteRtpPacket(
            CreatePacket(200U, 1),
            CreateContext(0L, "media", 8000));

        Assert.Multiple(() =>
        {
            Assert.That(copiedTable.ExpireRemoteSourceActivity(2L).Single()
                .Status, Is.EqualTo(RtpSourceStatus.ActiveMember));
            Assert.That(copiedTable.ExpireRemoteSourceActivity(4L).Single()
                .Status, Is.EqualTo(RtpSourceStatus.Inactive));
            Assert.That(disabledTable.ExpireRemoteSourceActivity(1000L),
                Is.Empty);
            Assert.That(disabledTable.FindRemoteSourceSnapshot(200U)?.Status,
                Is.EqualTo(RtpSourceStatus.ActiveSender));
        });
    }

    [Test]
    public void MixedMediaSourceLifecycleComposesWithoutLeakingSnapshots()
    {
        RtpSessionConfiguration configuration = CreateConfiguration(8, 2);
        configuration.RtcpScheduler.SenderTimeoutTicks = 5L;
        configuration.RtcpScheduler.MemberTimeoutTicks = 10L;
        RtpPayloadTypeRegistry registry = new RtpPayloadTypeRegistry(
            RtpSessionTransportMode.Separate);
        registry.SetPayloadTypeDefinition(
            CreatePayloadDefinition(96, 90000, "video"));
        registry.SetPayloadTypeDefinition(
            CreatePayloadDefinition(0, 8000, "audio"));
        RtpSourceTable table = new RtpSourceTable(
            configuration,
            new FixedRandomSource());
        table.ConfigureRemotePayloadTypeRegistry(registry);

        RtpSourceSnapshot discoveredByControl =
            table.RecordRemoteRtcpActivity(
                300U,
                CreateContext(0L, "control", 0));
        RtcpSourceDescription firstDescription =
            new RtcpSourceDescription();
        firstDescription.Chunks = new RtcpSourceDescriptionChunk[]
        {
            CreateSourceDescriptionChunk(300U, "alice", "Alice")
        };
        table.RecordRemoteSourceDescription(
            firstDescription,
            CreateContext(0L, "control", 0));

        RtpPacket firstVideo = CreatePacket(300U, 10);
        firstVideo.PayloadType = 96;
        firstVideo.Timestamp = 1000U;
        firstVideo.ContributingSources = new uint[] { 400U };
        RtpSequenceUpdate probation = table.RecordMappedRemoteRtpPacket(
            firstVideo,
            CreateContext(1L, "video", 1));
        RtpSourceSnapshot? duringProbation =
            table.FindRemoteSourceSnapshot(300U);
        RtpSourceSnapshot? contributorDuringProbation =
            table.FindRemoteSourceSnapshot(400U);

        RtpPacket secondVideo = CreatePacket(300U, 11);
        secondVideo.PayloadType = 96;
        secondVideo.Timestamp = 2000U;
        secondVideo.ContributingSources = new uint[] { 400U };
        RtpSequenceUpdate acceptedVideo = table.RecordMappedRemoteRtpPacket(
            secondVideo,
            CreateContext(2L, "video", 1));
        RtpPacket firstAudio = CreatePacket(200U, 20);
        firstAudio.PayloadType = 0;
        firstAudio.Timestamp = 100U;
        table.RecordMappedRemoteRtpPacket(
            firstAudio,
            CreateContext(2L, "audio", 1));
        RtpPacket secondAudio = CreatePacket(200U, 21);
        secondAudio.PayloadType = 0;
        secondAudio.Timestamp = 200U;
        table.RecordMappedRemoteRtpPacket(
            secondAudio,
            CreateContext(3L, "audio", 1));

        RtcpSourceDescription changedDescription =
            new RtcpSourceDescription();
        changedDescription.Chunks = new RtcpSourceDescriptionChunk[]
        {
            CreateSourceDescriptionChunk(300U, "bob", "Bob")
        };
        table.RecordRemoteSourceDescription(
            changedDescription,
            CreateContext(3L, "control-new", 0));
        RtpSourceSnapshot[] isolatedSnapshots =
            table.GetRemoteSourceSnapshots();
        RtpSourceSnapshot isolatedVideo = isolatedSnapshots.Single(
            value => value.SourceIdentifier == 300U);
        isolatedVideo.CanonicalName = "changed";
        isolatedVideo.SequenceUpdate!.SequenceNumber = 99;

        RtcpGoodbye goodbye = new RtcpGoodbye();
        goodbye.Sources = new uint[] { 400U };
        table.RecordRemoteGoodbye(
            goodbye,
            CreateContext(4L, "control-bye", 0));
        int removedGoodbyeCount = table.RemoveGoodbyeSources();
        RtpSourceSnapshot? freshVideo =
            table.FindRemoteSourceSnapshot(300U);
        RtpSourceSnapshot? audio =
            table.FindRemoteSourceSnapshot(200U);
        RtpSourceSnapshot[] firstDemotion =
            table.ExpireRemoteSourceActivity(7L);
        RtpSourceSnapshot[] secondDemotion =
            table.ExpireRemoteSourceActivity(8L);
        RtpSourceSnapshot[] expired =
            table.ExpireRemoteSourceActivity(13L);

        Assert.Multiple(() =>
        {
            Assert.That(discoveredByControl.Status,
                Is.EqualTo(RtpSourceStatus.ActiveMember));
            Assert.That(probation.Status,
                Is.EqualTo(RtpSequenceStatus.Probation));
            Assert.That(duringProbation?.Status,
                Is.EqualTo(RtpSourceStatus.ActiveMember));
            Assert.That(duringProbation?.CanonicalName,
                Is.EqualTo("alice"));
            Assert.That(contributorDuringProbation, Is.Null);
            Assert.That(acceptedVideo.Status,
                Is.EqualTo(RtpSequenceStatus.Accepted));
            Assert.That(freshVideo?.Status,
                Is.EqualTo(RtpSourceStatus.ActiveSender));
            Assert.That(freshVideo?.CanonicalName, Is.EqualTo("bob"));
            Assert.That(freshVideo?.SequenceUpdate?.SequenceNumber,
                Is.EqualTo(11));
            Assert.That(freshVideo?.PayloadType, Is.EqualTo(96));
            Assert.That(freshVideo?.ClockRate, Is.EqualTo(90000));
            Assert.That(freshVideo?.MediaIdentity, Is.EqualTo("video"));
            Assert.That(audio?.Status,
                Is.EqualTo(RtpSourceStatus.ActiveSender));
            Assert.That(audio?.PayloadType, Is.Zero);
            Assert.That(audio?.ClockRate, Is.EqualTo(8000));
            Assert.That(audio?.MediaIdentity, Is.EqualTo("audio"));
            Assert.That(removedGoodbyeCount, Is.EqualTo(1));
            Assert.That(table.FindRemoteSourceSnapshot(400U), Is.Null);
            Assert.That(firstDemotion.Select(
                value => value.SourceIdentifier),
                Is.EqualTo(new uint[] { 300U }));
            Assert.That(firstDemotion[0].Status,
                Is.EqualTo(RtpSourceStatus.ActiveMember));
            Assert.That(secondDemotion.Select(
                value => value.SourceIdentifier),
                Is.EqualTo(new uint[] { 200U }));
            Assert.That(expired.Select(value => value.SourceIdentifier),
                Is.EqualTo(new uint[] { 200U, 300U }));
            Assert.That(expired,
                Has.All.Matches<RtpSourceSnapshot>(
                    value => value.Status == RtpSourceStatus.Inactive));
            Assert.That(table.GetRemoteSourceSnapshots(), Is.Empty);
        });
    }

    static RtpSessionConfiguration CreateConfiguration(
        int maximumSourceCount,
        int minimumSequential)
    {
        RtpSessionConfiguration configuration = new RtpSessionConfiguration();
        configuration.MaximumSourceCount = maximumSourceCount;
        configuration.MinimumSequential = minimumSequential;
        return configuration;
    }

    static RtpPacket CreatePacket(uint sourceIdentifier, ushort sequenceNumber)
    {
        RtpPacket packet = new RtpPacket();
        packet.SynchronizationSource = sourceIdentifier;
        packet.SequenceNumber = sequenceNumber;
        packet.PayloadType = 96;
        packet.Payload = new byte[] { 1 };
        return packet;
    }

    static RtpPayloadTypeDefinition CreatePayloadDefinition(
        byte payloadType,
        int clockRate,
        string mediaIdentity)
    {
        RtpPayloadTypeDefinition definition =
            new RtpPayloadTypeDefinition();
        definition.PayloadType = payloadType;
        definition.ClockRate = clockRate;
        definition.MediaIdentity = mediaIdentity;
        definition.ChannelCount = 1;
        return definition;
    }

    static RtcpSourceDescriptionChunk CreateSourceDescriptionChunk(
        uint sourceIdentifier,
        string canonicalName,
        string displayName)
    {
        RtcpSourceDescriptionChunk chunk =
            new RtcpSourceDescriptionChunk();
        chunk.SourceIdentifier = sourceIdentifier;
        List<RtcpSourceDescriptionItem> items =
            new List<RtcpSourceDescriptionItem>();
        if (canonicalName.Length > 0)
        {
            RtcpSourceDescriptionItem canonicalNameItem =
                new RtcpSourceDescriptionItem();
            canonicalNameItem.Type = RtcpSdesItemType.CanonicalName;
            canonicalNameItem.Value = Encoding.UTF8.GetBytes(canonicalName);
            items.Add(canonicalNameItem);
        }

        if (displayName.Length > 0)
        {
            RtcpSourceDescriptionItem displayNameItem =
                new RtcpSourceDescriptionItem();
            displayNameItem.Type = RtcpSdesItemType.Name;
            displayNameItem.Value = Encoding.UTF8.GetBytes(displayName);
            items.Add(displayNameItem);
        }

        chunk.Items = items.ToArray();
        return chunk;
    }

    static RtpPacketContext CreateContext(
        long arrivalTimeTicks,
        string transportIdentity,
        int clockRate)
    {
        RtpPacketContext context = new RtpPacketContext();
        context.ArrivalTimeTicks = arrivalTimeTicks;
        context.TransportIdentity = transportIdentity;
        context.ClockRate = clockRate;
        return context;
    }

    private class FixedRandomSource : IRtpRandomSource
    {
        public uint CreateSynchronizationSource()
        {
            return 1U;
        }

        public ushort CreateSequenceNumber()
        {
            return 1;
        }

        public uint CreateTimestamp()
        {
            return 1U;
        }

        public double CreateUnitIntervalValue()
        {
            return 0.5;
        }
    }
}
