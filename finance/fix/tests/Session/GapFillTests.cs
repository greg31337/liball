using fix;
using NUnit.Framework;

namespace fix.Tests;

public class GapFillTests
{
    [Test]
    public void GapFillAdvancesDurableTargetAndReleasesContiguousQueuedMessages()
    {
        MemoryMessageStore store = Store(1, 2);
        SessionSequenceProcessor sequences = Sequences(store);
        GapTracker gaps = new GapTracker(new SessionSettings());
        SequenceResetProcessor processor = new SequenceResetProcessor(sequences, gaps);
        FixMessage fifth = Message("D");
        FixMessage sixth = Message("F");
        sequences.ProcessInboundSequence(fifth, 5, false, false);
        gaps.ObserveGap(2, 5);
        sequences.ProcessInboundSequence(sixth, 6, true, true);
        gaps.ObserveGap(2, 6);

        SequenceResetProcessingResult result = processor.ProcessSequenceReset(Message(FixAdminMessageTypes.SequenceReset), 2, 5, true, false);
        StoreRangeResult persisted = store.RefreshStore();

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.SequenceAdvanced, Is.True);
            Assert.That(result.NewSequenceNumber, Is.EqualTo(5));
            Assert.That(result.ReleasedMessages, Has.Count.EqualTo(2));
            Assert.That(result.ReleasedMessages[0].Message, Is.SameAs(fifth));
            Assert.That(result.ReleasedMessages[1].Message, Is.SameAs(sixth));
            Assert.That(result.ReleasedMessages[1].PossibleDuplicate, Is.True);
            Assert.That(result.ReleasedMessages[1].Rejected, Is.True);
            Assert.That(result.RecoveryCompleted, Is.True);
            Assert.That(result.Code, Is.EqualTo("sequence-recovery-complete"));
            Assert.That(result.NextTargetSeqNum, Is.EqualTo(7));
            Assert.That(result.QueueCount, Is.Zero);
            Assert.That(persisted.NextTargetSeqNum, Is.EqualTo(7));
            Assert.That(gaps.GapActive, Is.False);
        });
    }

    [Test]
    public void GapFillDiscardsQueuedMessagesCoveredBySkippedRange()
    {
        SessionSequenceProcessor sequences = Sequences(Store(1, 2));
        GapTracker gaps = new GapTracker(new SessionSettings { ClosedResendInterval = true });
        SequenceResetProcessor processor = new SequenceResetProcessor(sequences, gaps);
        FixMessage third = Message("D");
        FixMessage fourth = Message("F");
        FixMessage fifth = Message("G");
        QueueGap(sequences, gaps, third, 3);
        QueueGap(sequences, gaps, fourth, 4);
        QueueGap(sequences, gaps, fifth, 5);

        SequenceResetProcessingResult result = processor.ProcessSequenceReset(Message(FixAdminMessageTypes.SequenceReset), 2, 5, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.DroppedQueueCount, Is.EqualTo(2));
            Assert.That(result.ReleasedMessages, Has.Count.EqualTo(1));
            Assert.That(result.ReleasedMessages[0].Message, Is.SameAs(fifth));
            Assert.That(result.NextTargetSeqNum, Is.EqualTo(6));
            Assert.That(result.QueueCount, Is.Zero);
            Assert.That(result.RecoveryCompleted, Is.True);
        });
    }

    [Test]
    public void ChunkedRecoveryRequestsNextRangeWhenGapRemains()
    {
        SessionSequenceProcessor sequences = Sequences(Store(1, 2));
        GapTracker gaps = new GapTracker(new SessionSettings { ResendRequestChunkSize = 3 });
        SequenceResetProcessor processor = new SequenceResetProcessor(sequences, gaps);
        QueueGap(sequences, gaps, Message("D"), 10);

        SequenceResetProcessingResult result = processor.ProcessSequenceReset(Message(FixAdminMessageTypes.SequenceReset), 2, 5, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.SequenceAdvanced, Is.True);
            Assert.That(result.RecoveryCompleted, Is.False);
            Assert.That(result.NextTargetSeqNum, Is.EqualTo(5));
            Assert.That(result.QueueCount, Is.EqualTo(1));
            Assert.That(result.ResendAction.GenerateResendRequest, Is.True);
            Assert.That(result.ResendAction.BeginSeqNo, Is.EqualTo(5));
            Assert.That(result.ResendAction.EndSeqNo, Is.EqualTo(7));
            Assert.That(result.Code, Is.EqualTo("sequence-gap-continues"));
        });
    }

    [Test]
    public void StaleGapFillAtOrBelowExpectedSequenceIsIgnored()
    {
        MemoryMessageStore store = Store(4, 5);
        SessionSequenceProcessor sequences = Sequences(store);
        GapTracker gaps = new GapTracker(new SessionSettings());
        SequenceResetProcessor processor = new SequenceResetProcessor(sequences, gaps);

        SequenceResetProcessingResult result = processor.ProcessSequenceReset(Message(FixAdminMessageTypes.SequenceReset), 2, 5, true, true);
        StoreRangeResult persisted = store.RefreshStore();

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Ignored, Is.True);
            Assert.That(result.SequenceAdvanced, Is.False);
            Assert.That(result.Code, Is.EqualTo("stale-gap-fill-ignored"));
            Assert.That(result.NextTargetSeqNum, Is.EqualTo(5));
            Assert.That(persisted.NextTargetSeqNum, Is.EqualTo(5));
        });
    }

    [Test]
    public void PossibleDuplicateGapFillCanAdvanceBeyondCurrentExpectedSequence()
    {
        SessionSequenceProcessor sequences = Sequences(Store(1, 5));
        GapTracker gaps = new GapTracker(new SessionSettings());
        SequenceResetProcessor processor = new SequenceResetProcessor(sequences, gaps);
        QueueGap(sequences, gaps, Message("D"), 7);

        SequenceResetProcessingResult result = processor.ProcessSequenceReset(Message(FixAdminMessageTypes.SequenceReset), 2, 7, true, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.SequenceAdvanced, Is.True);
            Assert.That(result.ReleasedMessages, Has.Count.EqualTo(1));
            Assert.That(result.NextTargetSeqNum, Is.EqualTo(8));
            Assert.That(result.RecoveryCompleted, Is.True);
        });
    }

    [Test]
    public void LowUnflaggedSequenceResetDisconnectsWithoutApplyingNewSequence()
    {
        SessionSequenceProcessor sequences = Sequences(Store(1, 5));
        SequenceResetProcessor processor = new SequenceResetProcessor(sequences, new GapTracker(new SessionSettings()));

        SequenceResetProcessingResult result = processor.ProcessSequenceReset(Message(FixAdminMessageTypes.SequenceReset), 2, 7, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Disconnect, Is.True);
            Assert.That(result.SequenceAdvanced, Is.False);
            Assert.That(result.Code, Is.EqualTo("target-sequence-too-low"));
            Assert.That(result.NextTargetSeqNum, Is.EqualTo(5));
        });
    }

    [Test]
    public void ResetAtExpectedPositionIsAcceptedWithoutChangeAndHigherInvalidGapFillIsQueued()
    {
        SessionSequenceProcessor sequences = Sequences(Store(1, 5));
        SequenceResetProcessor processor = new SequenceResetProcessor(sequences, new GapTracker(new SessionSettings()));

        SequenceResetProcessingResult reset = processor.ProcessSequenceReset(Message(FixAdminMessageTypes.SequenceReset), 5, 5, false, false);
        SequenceResetProcessingResult gapFill = processor.ProcessSequenceReset(Message(FixAdminMessageTypes.SequenceReset), 6, 6, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(reset.Accepted, Is.True);
            Assert.That(reset.Rejected, Is.False);
            Assert.That(reset.Code, Is.EqualTo("sequence-reset-no-change"));
            Assert.That(reset.NextTargetSeqNum, Is.EqualTo(5));
            Assert.That(gapFill.Accepted, Is.True);
            Assert.That(gapFill.Rejected, Is.False);
            Assert.That(gapFill.Queued, Is.True);
            Assert.That(gapFill.Code, Is.EqualTo("invalid-sequence-reset-queued"));
            Assert.That(gapFill.NextTargetSeqNum, Is.EqualTo(5));
        });
    }

    [Test]
    public void ResetMatchingExpectedSequenceDoesNotConsumeOrReleaseQueuedMessages()
    {
        SessionSequenceProcessor sequences = Sequences(Store(1, 5));
        GapTracker gaps = new GapTracker(new SessionSettings());
        SequenceResetProcessor processor = new SequenceResetProcessor(sequences, gaps);
        FixMessage sixth = Message("D");
        QueueGap(sequences, gaps, sixth, 6);

        SequenceResetProcessingResult result = processor.ProcessSequenceReset(Message(FixAdminMessageTypes.SequenceReset), 5, 5, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Rejected, Is.False);
            Assert.That(result.Code, Is.EqualTo("sequence-reset-no-change"));
            Assert.That(result.ReleasedMessages, Is.Empty);
            Assert.That(result.NextTargetSeqNum, Is.EqualTo(5));
            Assert.That(result.QueueCount, Is.EqualTo(1));
            Assert.That(result.RecoveryCompleted, Is.False);
            Assert.That(gaps.GapActive, Is.True);
        });
    }

    [Test]
    public void ResetModeSkipsCoveredQueueAndReleasesFromNewSequence()
    {
        SessionSequenceProcessor sequences = Sequences(Store(3, 5));
        GapTracker gaps = new GapTracker(new SessionSettings { ClosedResendInterval = true });
        SequenceResetProcessor processor = new SequenceResetProcessor(sequences, gaps);
        FixMessage sixth = Message("D");
        FixMessage tenth = Message("F");
        FixMessage eleventh = Message("G");
        QueueGap(sequences, gaps, sixth, 6);
        QueueGap(sequences, gaps, tenth, 10);
        QueueGap(sequences, gaps, eleventh, 11);

        SequenceResetProcessingResult result = processor.ProcessSequenceReset(Message(FixAdminMessageTypes.SequenceReset), 5, 10, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.SequenceAdvanced, Is.True);
            Assert.That(result.DroppedQueueCount, Is.EqualTo(1));
            Assert.That(result.ReleasedMessages, Has.Count.EqualTo(2));
            Assert.That(result.ReleasedMessages[0].Message, Is.SameAs(tenth));
            Assert.That(result.ReleasedMessages[1].Message, Is.SameAs(eleventh));
            Assert.That(result.NextTargetSeqNum, Is.EqualTo(12));
            Assert.That(result.QueueCount, Is.Zero);
            Assert.That(result.RecoveryCompleted, Is.True);
        });
    }

    [Test]
    public void HigherSequenceResetIsQueuedAndStartsRecoveryWithoutConsumption()
    {
        SessionSequenceProcessor sequences = Sequences(Store(1, 2));
        GapTracker gaps = new GapTracker(new SessionSettings());
        SequenceResetProcessor processor = new SequenceResetProcessor(sequences, gaps);
        FixMessage message = Message(FixAdminMessageTypes.SequenceReset);

        SequenceResetProcessingResult result = processor.ProcessSequenceReset(message, 5, 7, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Queued, Is.True);
            Assert.That(result.SequenceAdvanced, Is.False);
            Assert.That(result.NextTargetSeqNum, Is.EqualTo(2));
            Assert.That(result.QueueCount, Is.EqualTo(1));
            Assert.That(sequences.QueuedMessages[5].Message, Is.SameAs(message));
            Assert.That(sequences.QueuedMessages[5].Rejected, Is.False);
            Assert.That(result.ResendAction.GenerateResendRequest, Is.True);
            Assert.That(result.ResendAction.BeginSeqNo, Is.EqualTo(2));
            Assert.That(result.ResendAction.EndSeqNo, Is.Zero);
            Assert.That(gaps.GapActive, Is.True);
        });
    }

    [Test]
    public void QueuedSequenceResetWaitsForSpecializedProcessingBeforeRecoveryCompletes()
    {
        SessionSequenceProcessor sequences = Sequences(Store(1, 2));
        GapTracker gaps = new GapTracker(new SessionSettings());
        SequenceResetProcessor processor = new SequenceResetProcessor(sequences, gaps);
        FixMessage queuedReset = Message(FixAdminMessageTypes.SequenceReset);
        processor.ProcessSequenceReset(queuedReset, 5, 7, true, false);

        SequenceResetProcessingResult firstFill = processor.ProcessSequenceReset(Message(FixAdminMessageTypes.SequenceReset), 2, 5, true, false);
        bool gapActiveAfterFirst = gaps.GapActive;
        bool queuedAfterFirst = sequences.QueuedMessages.ContainsKey(5);
        QueuedSessionMessage? pendingAfterFirst = firstFill.PendingSequenceReset;
        SequenceResetProcessingResult queuedResult = processor.ProcessSequenceReset(queuedReset, 5, 7, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(firstFill.Accepted, Is.True);
            Assert.That(firstFill.SequenceResetPending, Is.True);
            Assert.That(pendingAfterFirst, Is.Not.Null);
            Assert.That(pendingAfterFirst!.Message, Is.SameAs(queuedReset));
            Assert.That(firstFill.ReleasedMessages, Is.Empty);
            Assert.That(firstFill.NextTargetSeqNum, Is.EqualTo(5));
            Assert.That(firstFill.RecoveryCompleted, Is.False);
            Assert.That(gapActiveAfterFirst, Is.True);
            Assert.That(queuedAfterFirst, Is.True);
            Assert.That(queuedResult.Accepted, Is.True);
            Assert.That(queuedResult.SequenceAdvanced, Is.True);
            Assert.That(queuedResult.NextTargetSeqNum, Is.EqualTo(7));
            Assert.That(queuedResult.QueueCount, Is.Zero);
            Assert.That(queuedResult.RecoveryCompleted, Is.True);
            Assert.That(gaps.GapActive, Is.False);
        });
    }

    [Test]
    public void RejectedQueuedSequenceResetIsRemovedAfterOrderedConsumption()
    {
        SessionSequenceProcessor sequences = Sequences(Store(1, 2));
        GapTracker gaps = new GapTracker(new SessionSettings());
        SequenceResetProcessor processor = new SequenceResetProcessor(sequences, gaps);
        FixMessage queuedReset = Message(FixAdminMessageTypes.SequenceReset);
        SequenceResetProcessingResult queued = processor.ProcessSequenceReset(queuedReset, 5, 5, true, false);
        SequenceResetProcessingResult firstFill = processor.ProcessSequenceReset(Message(FixAdminMessageTypes.SequenceReset), 2, 5, true, false);

        SequenceResetProcessingResult rejected = processor.ProcessSequenceReset(queuedReset, 5, 5, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(queued.Queued, Is.True);
            Assert.That(sequences.QueuedMessages.ContainsKey(5), Is.False);
            Assert.That(firstFill.SequenceResetPending, Is.True);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Rejected, Is.True);
            Assert.That(rejected.Code, Is.EqualTo("invalid-new-sequence-number"));
            Assert.That(rejected.NextTargetSeqNum, Is.EqualTo(6));
            Assert.That(rejected.QueueCount, Is.Zero);
            Assert.That(rejected.RecoveryCompleted, Is.True);
        });
    }

    [Test]
    public void PersistenceFailureLeavesTargetAndQueueUntouchedAndRequiresDisconnect()
    {
        FailingSequenceStore store = new FailingSequenceStore();
        SessionSequenceProcessor sequences = new SessionSequenceProcessor(store);
        sequences.RefreshSequences();
        FixMessage fifth = Message("D");
        sequences.ProcessInboundSequence(fifth, 5, false, false);
        store.FailSave = true;
        SequenceResetProcessor processor = new SequenceResetProcessor(sequences, new GapTracker(new SessionSettings()));

        SequenceResetProcessingResult result = processor.ProcessSequenceReset(Message(FixAdminMessageTypes.SequenceReset), 2, 5, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.PersistenceFailed, Is.True);
            Assert.That(result.Disconnect, Is.True);
            Assert.That(result.SequenceAdvanced, Is.False);
            Assert.That(result.Code, Is.EqualTo("forced-save-failure"));
            Assert.That(result.NextTargetSeqNum, Is.EqualTo(2));
            Assert.That(result.QueueCount, Is.EqualTo(1));
            Assert.That(sequences.QueuedMessages[5].Message, Is.SameAs(fifth));
        });
    }

    [Test]
    public void MissingDependenciesMessagesAndInitializationAreRejected()
    {
        MemoryMessageStore store = Store(1, 1);
        SessionSequenceProcessor initialized = Sequences(store);
        GapTracker gaps = new GapTracker(new SessionSettings());
        SessionSequenceProcessor uninitialized = new SessionSequenceProcessor(store);

        SequenceResetProcessingResult missing = new SequenceResetProcessor(initialized, gaps).ProcessSequenceReset(null!, 1, 2, true, false);
        SequenceResetProcessingResult notReady = new SequenceResetProcessor(uninitialized, gaps).ProcessSequenceReset(Message(FixAdminMessageTypes.SequenceReset), 1, 2, true, false);

        Assert.Multiple(() =>
        {
            Assert.Throws<FixSessionException>(() => new SequenceResetProcessor(null!, gaps));
            Assert.Throws<FixSessionException>(() => new SequenceResetProcessor(initialized, null!));
            Assert.That(missing.Accepted, Is.False);
            Assert.That(missing.Code, Is.EqualTo("missing-sequence-reset-message"));
            Assert.That(notReady.Accepted, Is.False);
            Assert.That(notReady.Disconnect, Is.True);
            Assert.That(notReady.Code, Is.EqualTo("sequence-state-not-initialized"));
        });
    }

    private static void QueueGap(SessionSequenceProcessor sequences, GapTracker gaps, FixMessage message, int sequenceNumber)
    {
        SessionSequenceActionResult queued = sequences.ProcessInboundSequence(message, sequenceNumber, false, false);
        Assert.That(queued.Queued, Is.True);
        gaps.ObserveGap(sequences.NextTargetSeqNum, sequenceNumber);
    }

    private static MemoryMessageStore Store(int sender, int target)
    {
        MemoryMessageStore store = new MemoryMessageStore(0);
        StoreRangeResult saved = store.SaveSequenceState(sender, target);
        Assert.That(saved.Success, Is.True);
        return store;
    }

    private static SessionSequenceProcessor Sequences(IMessageStore store)
    {
        SessionSequenceProcessor sequences = new SessionSequenceProcessor(store);
        SessionSequenceActionResult refreshed = sequences.RefreshSequences();
        Assert.That(refreshed.Accepted, Is.True);
        return sequences;
    }

    private static FixMessage Message(string messageType)
    {
        return new FixMessage { BeginString = FixBeginStrings.Fix44, MsgType = messageType };
    }

    private class FailingSequenceStore : IMessageStore
    {
        internal int Sender = 1;
        internal int Target = 2;
        internal bool FailSave;

        public StoreRangeResult StoreMessage(StoredMessage message)
        {
            return Success();
        }

        public StoreRangeResult ReadMessageRange(int beginSeqNo, int endSeqNo)
        {
            return Success();
        }

        public StoreRangeResult SaveSequenceState(int nextSenderSeqNum, int nextTargetSeqNum)
        {
            if (FailSave)
            {
                return new StoreRangeResult { Code = "forced-save-failure", Text = "The test store refused the save.", HasSequenceState = true, NextSenderSeqNum = Sender, NextTargetSeqNum = Target };
            }
            Sender = nextSenderSeqNum;
            Target = nextTargetSeqNum;
            return Success();
        }

        public StoreRangeResult RefreshStore()
        {
            return Success();
        }

        public StoreRangeResult ResetStore()
        {
            Sender = 1;
            Target = 1;
            return Success();
        }

        private StoreRangeResult Success()
        {
            return new StoreRangeResult { Success = true, HasSequenceState = true, NextSenderSeqNum = Sender, NextTargetSeqNum = Target };
        }
    }
}
