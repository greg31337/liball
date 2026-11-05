using fix;
using NUnit.Framework;

namespace fix.Tests;

public class SequenceNumberTests
{
    [Test]
    public void InitialAndConsumedSequenceStatePersistsAcrossProcessors()
    {
        IMessageStore store = new MemoryMessageStore(0);
        SessionSequenceProcessor first = new SessionSequenceProcessor(store);

        SessionSequenceActionResult initial = first.RefreshSequences();
        SessionSequenceActionResult outbound = first.ProcessOutboundSequence(1, true);
        SessionSequenceActionResult inbound = first.ProcessInboundSequence(Message("D"), 1, false, false);
        SessionSequenceProcessor restarted = new SessionSequenceProcessor(store);
        SessionSequenceActionResult refreshed = restarted.RefreshSequences();

        Assert.Multiple(() =>
        {
            Assert.That(initial.Code, Is.EqualTo("sequence-state-refreshed"));
            Assert.That(initial.NextSenderSeqNum, Is.EqualTo(1));
            Assert.That(initial.NextTargetSeqNum, Is.EqualTo(1));
            Assert.That(outbound.Consumed, Is.True);
            Assert.That(outbound.EventKind, Is.EqualTo(SessionEventKind.MessageSent));
            Assert.That(inbound.Consumed, Is.True);
            Assert.That(inbound.EventKind, Is.EqualTo(SessionEventKind.MessageReceived));
            Assert.That(refreshed.NextSenderSeqNum, Is.EqualTo(2));
            Assert.That(refreshed.NextTargetSeqNum, Is.EqualTo(2));
            Assert.That(restarted.NextSenderSeqNum, Is.EqualTo(2));
            Assert.That(restarted.NextTargetSeqNum, Is.EqualTo(2));
        });
    }

    [Test]
    public void SenderSequenceIsConsumedOnlyForConfirmedExactSend()
    {
        SessionSequenceProcessor processor = Processor(4, 7);

        SessionSequenceActionResult notSent = processor.ProcessOutboundSequence(4, false);
        SessionSequenceActionResult low = processor.ProcessOutboundSequence(3, true);
        SessionSequenceActionResult high = processor.ProcessOutboundSequence(5, true);
        SessionSequenceActionResult sent = processor.ProcessOutboundSequence(4, true);

        Assert.Multiple(() =>
        {
            Assert.That(notSent.Consumed, Is.False);
            Assert.That(notSent.Code, Is.EqualTo("outbound-message-not-sent"));
            Assert.That(low.Code, Is.EqualTo("sender-sequence-too-low"));
            Assert.That(high.Code, Is.EqualTo("sender-sequence-too-high"));
            Assert.That(sent.Accepted, Is.True);
            Assert.That(sent.Consumed, Is.True);
            Assert.That(sent.SequenceNumber, Is.EqualTo(4));
            Assert.That(sent.ExpectedSequenceNumber, Is.EqualTo(4));
            Assert.That(sent.NextSenderSeqNum, Is.EqualTo(5));
            Assert.That(sent.NextTargetSeqNum, Is.EqualTo(7));
            Assert.That(processor.NextSenderSeqNum, Is.EqualTo(5));
        });
    }

    [Test]
    public void RejectedExpectedInboundMessageStillConsumesTargetSequence()
    {
        SessionSequenceProcessor processor = Processor(3, 8);

        SessionSequenceActionResult rejected = processor.ProcessInboundSequence(Message("D"), 8, false, true);
        SessionSequenceActionResult next = processor.ProcessInboundSequence(Message("F"), 9, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Consumed, Is.True);
            Assert.That(rejected.RejectMessage, Is.True);
            Assert.That(rejected.Disposition, Is.EqualTo(MessageDisposition.Reject));
            Assert.That(rejected.Code, Is.EqualTo("rejected-target-sequence-consumed"));
            Assert.That(rejected.NextTargetSeqNum, Is.EqualTo(9));
            Assert.That(next.Accepted, Is.True);
            Assert.That(next.Consumed, Is.True);
            Assert.That(next.NextTargetSeqNum, Is.EqualTo(10));
        });
    }

    [Test]
    public void LowPossibleDuplicateIsIgnoredWithoutConsumption()
    {
        SessionSequenceProcessor processor = Processor(1, 5);

        SessionSequenceActionResult duplicate = processor.ProcessInboundSequence(Message("D"), 4, true, false);
        SessionSequenceActionResult current = processor.ProcessInboundSequence(Message("D"), 5, true, false);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Accepted, Is.True);
            Assert.That(duplicate.Duplicate, Is.True);
            Assert.That(duplicate.Consumed, Is.False);
            Assert.That(duplicate.Disposition, Is.EqualTo(MessageDisposition.Drop));
            Assert.That(duplicate.Code, Is.EqualTo("possible-duplicate-ignored"));
            Assert.That(duplicate.NextTargetSeqNum, Is.EqualTo(5));
            Assert.That(current.Duplicate, Is.False);
            Assert.That(current.Consumed, Is.True);
            Assert.That(current.NextTargetSeqNum, Is.EqualTo(6));
        });
    }

    [Test]
    public void LowUnflaggedInboundMessageRequiresDisconnectWithoutConsumption()
    {
        SessionSequenceProcessor processor = Processor(1, 5);

        SessionSequenceActionResult result = processor.ProcessInboundSequence(Message("D"), 4, false, false);
        StoreRangeResult stored = processor.Store.RefreshStore();

        Assert.Multiple(() =>
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Consumed, Is.False);
            Assert.That(result.RejectMessage, Is.True);
            Assert.That(result.Disconnect, Is.True);
            Assert.That(result.Disposition, Is.EqualTo(MessageDisposition.Disconnect));
            Assert.That(result.Code, Is.EqualTo("target-sequence-too-low"));
            Assert.That(processor.NextTargetSeqNum, Is.EqualTo(5));
            Assert.That(stored.NextTargetSeqNum, Is.EqualTo(5));
        });
    }

    [Test]
    public void HighInboundMessagesAreQueuedInOrderWithoutSequenceConsumption()
    {
        SessionSequenceProcessor processor = Processor(1, 2);
        FixMessage fifth = Message("F");
        FixMessage fourth = Message("G");
        FixMessage repeatedFourth = Message("D");

        SessionSequenceActionResult highFive = processor.ProcessInboundSequence(fifth, 5, false, false);
        SessionSequenceActionResult highFour = processor.ProcessInboundSequence(fourth, 4, true, true);
        SessionSequenceActionResult repeated = processor.ProcessInboundSequence(repeatedFourth, 4, false, false);
        StoreRangeResult stored = processor.Store.RefreshStore();

        Assert.Multiple(() =>
        {
            Assert.That(highFive.Queued, Is.True);
            Assert.That(highFive.EventKind, Is.EqualTo(SessionEventKind.SequenceGap));
            Assert.That(highFive.ExpectedSequenceNumber, Is.EqualTo(2));
            Assert.That(highFour.Queued, Is.True);
            Assert.That(highFour.QueueCount, Is.EqualTo(2));
            Assert.That(repeated.Queued, Is.True);
            Assert.That(repeated.Duplicate, Is.True);
            Assert.That(repeated.Code, Is.EqualTo("sequence-already-queued"));
            Assert.That(repeated.QueueCount, Is.EqualTo(2));
            Assert.That(processor.QueuedMessages.Keys, Is.EqualTo(new[] { 4, 5 }));
            Assert.That(processor.QueuedMessages[4].Message, Is.SameAs(fourth));
            Assert.That(processor.QueuedMessages[4].SequenceNumber, Is.EqualTo(4));
            Assert.That(processor.QueuedMessages[4].PossibleDuplicate, Is.True);
            Assert.That(processor.QueuedMessages[4].Rejected, Is.True);
            Assert.That(stored.NextTargetSeqNum, Is.EqualTo(2));
        });
    }

    [Test]
    public void PersistenceFailureIsTerminalAndDoesNotMutateLocalSequenceState()
    {
        ControllableMessageStore outboundStore = new ControllableMessageStore(2, 3);
        SessionSequenceProcessor outboundProcessor = new SessionSequenceProcessor(outboundStore);
        outboundProcessor.RefreshSequences();
        outboundStore.FailSave = true;
        SessionSequenceActionResult outbound = outboundProcessor.ProcessOutboundSequence(2, true);

        ControllableMessageStore inboundStore = new ControllableMessageStore(4, 5);
        SessionSequenceProcessor inboundProcessor = new SessionSequenceProcessor(inboundStore);
        inboundProcessor.RefreshSequences();
        inboundStore.ReturnMismatchedSaveState = true;
        SessionSequenceActionResult inbound = inboundProcessor.ProcessInboundSequence(Message("D"), 5, false, false);

        Assert.Multiple(() =>
        {
            AssertStoreFailure(outbound, 2, 2, 3, "forced-save-failure");
            Assert.That(outboundProcessor.NextSenderSeqNum, Is.EqualTo(2));
            AssertStoreFailure(inbound, 5, 4, 5, "sequence-state-save-failed");
            Assert.That(inboundProcessor.NextTargetSeqNum, Is.EqualTo(5));
        });
    }

    [Test]
    public void InvalidRefreshAndUseBeforeRefreshHaveExplicitTerminalOutcomes()
    {
        ControllableMessageStore store = new ControllableMessageStore(1, 1);
        SessionSequenceProcessor processor = new SessionSequenceProcessor(store);

        SessionSequenceActionResult beforeRefresh = processor.ProcessOutboundSequence(1, true);
        store.FailRefresh = true;
        SessionSequenceActionResult failedRefresh = processor.RefreshSequences();

        Assert.Multiple(() =>
        {
            Assert.Throws<FixSessionException>(() => new SessionSequenceProcessor(null!));
            Assert.That(beforeRefresh.Code, Is.EqualTo("sequence-state-not-initialized"));
            Assert.That(beforeRefresh.Disconnect, Is.True);
            Assert.That(failedRefresh.PersistenceFailed, Is.True);
            Assert.That(failedRefresh.Disconnect, Is.True);
            Assert.That(failedRefresh.Code, Is.EqualTo("forced-refresh-failure"));
            Assert.That(processor.Initialized, Is.False);
        });
    }

    [Test]
    public void InvalidAndExhaustedSequenceNumbersDoNotOverflowOrPersist()
    {
        SessionSequenceProcessor outbound = Processor(int.MaxValue, 1);
        SessionSequenceProcessor inbound = Processor(1, int.MaxValue);

        SessionSequenceActionResult invalid = inbound.ProcessInboundSequence(Message("D"), 0, false, false);
        SessionSequenceActionResult senderExhausted = outbound.ProcessOutboundSequence(int.MaxValue, true);
        SessionSequenceActionResult targetExhausted = inbound.ProcessInboundSequence(Message("D"), int.MaxValue, false, false);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Code, Is.EqualTo("invalid-target-sequence-number"));
            Assert.That(invalid.Disconnect, Is.True);
            Assert.That(senderExhausted.Code, Is.EqualTo("sender-sequence-number-exhausted"));
            Assert.That(senderExhausted.Disconnect, Is.True);
            Assert.That(targetExhausted.Code, Is.EqualTo("target-sequence-number-exhausted"));
            Assert.That(targetExhausted.Disconnect, Is.True);
            Assert.That(outbound.NextSenderSeqNum, Is.EqualTo(int.MaxValue));
            Assert.That(inbound.NextTargetSeqNum, Is.EqualTo(int.MaxValue));
        });
    }

    private static SessionSequenceProcessor Processor(int sender, int target)
    {
        MemoryMessageStore store = new MemoryMessageStore(0);
        StoreRangeResult saved = store.SaveSequenceState(sender, target);
        Assert.That(saved.Success, Is.True);
        SessionSequenceProcessor processor = new SessionSequenceProcessor(store);
        SessionSequenceActionResult refreshed = processor.RefreshSequences();
        Assert.That(refreshed.Accepted, Is.True);
        return processor;
    }

    private static FixMessage Message(string messageType)
    {
        return new FixMessage { BeginString = FixBeginStrings.Fix44, MsgType = messageType };
    }

    private static void AssertStoreFailure(SessionSequenceActionResult result, int sequenceNumber, int sender, int target, string code)
    {
        Assert.That(result.Accepted, Is.False);
        Assert.That(result.Consumed, Is.False);
        Assert.That(result.PersistenceFailed, Is.True);
        Assert.That(result.Disconnect, Is.True);
        Assert.That(result.Disposition, Is.EqualTo(MessageDisposition.Disconnect));
        Assert.That(result.SequenceNumber, Is.EqualTo(sequenceNumber));
        Assert.That(result.NextSenderSeqNum, Is.EqualTo(sender));
        Assert.That(result.NextTargetSeqNum, Is.EqualTo(target));
        Assert.That(result.Code, Is.EqualTo(code));
    }

    private class ControllableMessageStore : IMessageStore
    {
        internal int Sender;
        internal int Target;
        internal bool FailRefresh;
        internal bool FailSave;
        internal bool ReturnMismatchedSaveState;

        internal ControllableMessageStore(int sender, int target)
        {
            Sender = sender;
            Target = target;
        }

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
            if (ReturnMismatchedSaveState)
            {
                return new StoreRangeResult { Success = true, HasSequenceState = true, NextSenderSeqNum = Sender, NextTargetSeqNum = Target };
            }
            Sender = nextSenderSeqNum;
            Target = nextTargetSeqNum;
            return Success();
        }

        public StoreRangeResult RefreshStore()
        {
            if (FailRefresh)
            {
                return new StoreRangeResult { Code = "forced-refresh-failure", Text = "The test store refused the refresh." };
            }
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
