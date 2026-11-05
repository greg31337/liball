using System.Collections.Generic;

namespace fix;

internal class SequenceResetProcessor
{
    internal SessionSequenceProcessor Sequences;
    internal GapTracker Gaps;

    internal SequenceResetProcessor(SessionSequenceProcessor sequences, GapTracker gaps)
    {
        if (sequences == null || gaps == null)
        {
            throw new FixSessionException("SequenceReset processing dependencies are missing.");
        }
        Sequences = sequences;
        Gaps = gaps;
    }

    internal SequenceResetProcessingResult ProcessSequenceReset(FixMessage message, int messageSequenceNumber, int newSequenceNumber, bool gapFill, bool possibleDuplicate)
    {
        lock (this)
        {
            if (message == null)
            {
                return CreateFailure(messageSequenceNumber, newSequenceNumber, "missing-sequence-reset-message", "The SequenceReset message is missing.", false);
            }
            if (!Sequences.Initialized)
            {
                return CreateFailure(messageSequenceNumber, newSequenceNumber, "sequence-state-not-initialized", "The durable sequence state has not been loaded.", true);
            }
            if (messageSequenceNumber < FixDefaults.InitialSequenceNumber || newSequenceNumber < FixDefaults.InitialSequenceNumber)
            {
                return RejectInvalidReset(message, messageSequenceNumber, newSequenceNumber, possibleDuplicate, "invalid-sequence-reset-number", "SequenceReset sequence numbers must be positive.");
            }

            int expected = Sequences.NextTargetSeqNum;
            if (!gapFill)
            {
                return ProcessResetMode(messageSequenceNumber, newSequenceNumber, expected);
            }
            bool invalidAdvance = newSequenceNumber <= expected;
            bool invalidGapFillRange = gapFill && newSequenceNumber <= messageSequenceNumber;
            if (messageSequenceNumber < expected)
            {
                if (gapFill && newSequenceNumber <= expected)
                {
                    SequenceResetProcessingResult stale = CreateSuccess(messageSequenceNumber, newSequenceNumber, "stale-gap-fill-ignored", "The stale GapFill does not advance the target sequence number.");
                    stale.Ignored = true;
                    return CompleteResult(stale);
                }
                if (!possibleDuplicate)
                {
                    SessionSequenceActionResult low = Sequences.ProcessInboundSequence(message, messageSequenceNumber, false, false);
                    return FromSequenceAction(low, messageSequenceNumber, newSequenceNumber);
                }
                if (invalidAdvance || invalidGapFillRange)
                {
                    SequenceResetProcessingResult invalidLow = CreateFailure(messageSequenceNumber, newSequenceNumber, "invalid-new-sequence-number", "NewSeqNo must advance both the current target and the GapFill message sequence number.", false);
                    invalidLow.Rejected = true;
                    return CompleteResult(invalidLow);
                }
                return ApplySequenceAdvance(messageSequenceNumber, newSequenceNumber);
            }

            bool rejected = invalidAdvance || invalidGapFillRange;
            if (messageSequenceNumber > expected)
            {
                SessionSequenceActionResult queued = Sequences.ProcessInboundSequence(message, messageSequenceNumber, possibleDuplicate, rejected);
                SequenceResetProcessingResult queuedResult = FromSequenceAction(queued, messageSequenceNumber, newSequenceNumber);
                queuedResult.ResendAction = Gaps.ObserveGap(expected, messageSequenceNumber);
                queuedResult.Queued = queued.Queued;
                queuedResult.Code = rejected ? "invalid-sequence-reset-queued" : "sequence-reset-queued";
                queuedResult.Text = rejected ? "The higher invalid SequenceReset was queued for ordered rejection." : "The higher SequenceReset was queued until the preceding gap is recovered.";
                return CompleteResult(queuedResult);
            }

            if (rejected)
            {
                return RejectInvalidReset(message, messageSequenceNumber, newSequenceNumber, possibleDuplicate, "invalid-new-sequence-number", "NewSeqNo must advance both the current target and the GapFill message sequence number.");
            }
            return ApplySequenceAdvance(messageSequenceNumber, newSequenceNumber);
        }
    }

    internal SequenceResetProcessingResult ProcessResetMode(int messageSequenceNumber, int newSequenceNumber, int expected)
    {
        if (newSequenceNumber > expected)
        {
            return ApplySequenceAdvance(messageSequenceNumber, newSequenceNumber);
        }
        if (newSequenceNumber == expected)
        {
            SequenceResetProcessingResult unchanged = CreateSuccess(messageSequenceNumber, newSequenceNumber, "sequence-reset-no-change", "SequenceReset matched the current target sequence number.");
            unchanged.NextTargetSeqNum = expected;
            return CompleteResult(unchanged);
        }
        SequenceResetProcessingResult rejected = CreateFailure(messageSequenceNumber, newSequenceNumber, "invalid-new-sequence-number", "NewSeqNo must not lower the current target sequence number.", false);
        rejected.Rejected = true;
        rejected.NextTargetSeqNum = expected;
        return CompleteResult(rejected);
    }

    internal SequenceResetProcessingResult ApplySequenceAdvance(int messageSequenceNumber, int newSequenceNumber)
    {
        bool wasRecovering = Gaps.GapActive;
        SessionSequenceActionResult advanced = Sequences.AdvanceTargetSequence(newSequenceNumber);
        if (!advanced.Accepted)
        {
            return FromSequenceAction(advanced, messageSequenceNumber, newSequenceNumber);
        }

        SequenceResetProcessingResult result = CreateSuccess(messageSequenceNumber, newSequenceNumber, "sequence-reset-applied", "SequenceReset advanced the target sequence number.");
        result.SequenceAdvanced = true;
        RemoveQueuedSequenceReset(messageSequenceNumber);
        result.DroppedQueueCount = DiscardSkippedMessages(newSequenceNumber);
        ReleaseContiguousMessages(result);
        if (result.PersistenceFailed || result.Disconnect)
        {
            return CompleteResult(result);
        }
        ProgressRecovery(result, wasRecovering);
        result.NextTargetSeqNum = Sequences.NextTargetSeqNum;
        result.QueueCount = Sequences.QueuedMessages.Count;
        if (result.RecoveryCompleted)
        {
            result.Code = "sequence-recovery-complete";
            result.Text = "SequenceReset and queued-message release completed sequence recovery.";
        }
        else if (result.ResendAction.GenerateResendRequest)
        {
            result.Code = "sequence-gap-continues";
            result.Text = "SequenceReset advanced recovery and another ResendRequest range is required.";
        }
        return CompleteResult(result);
    }

    internal SequenceResetProcessingResult RejectInvalidReset(FixMessage message, int messageSequenceNumber, int newSequenceNumber, bool possibleDuplicate, string code, string text)
    {
        bool wasRecovering = Gaps.GapActive;
        SessionSequenceActionResult rejected = Sequences.ProcessInboundSequence(message, messageSequenceNumber, possibleDuplicate, true);
        SequenceResetProcessingResult result = FromSequenceAction(rejected, messageSequenceNumber, newSequenceNumber);
        result.Rejected = true;
        if (rejected.Consumed)
        {
            result.Code = code;
            result.Text = text;
            RemoveQueuedSequenceReset(messageSequenceNumber);
            ReleaseContiguousMessages(result);
            if (!result.PersistenceFailed && !result.Disconnect)
            {
                ProgressRecovery(result, wasRecovering);
            }
        }
        return CompleteResult(result);
    }

    internal void RemoveQueuedSequenceReset(int messageSequenceNumber)
    {
        if (Sequences.QueuedMessages.ContainsKey(messageSequenceNumber))
        {
            QueuedSessionMessage queued = Sequences.QueuedMessages[messageSequenceNumber];
            if (queued.Message.MsgType == FixAdminMessageTypes.SequenceReset)
            {
                Sequences.QueuedMessages.Remove(messageSequenceNumber);
            }
        }
    }

    internal void ProgressRecovery(SequenceResetProcessingResult result, bool wasRecovering)
    {
        if (Gaps.GapActive && !result.SequenceResetPending)
        {
            ResendRequestActionResult progress = Gaps.AdvanceGap(Sequences.NextTargetSeqNum);
            result.ResendAction = progress;
            result.RecoveryCompleted = wasRecovering && !progress.GapActive;
        }
    }

    internal int DiscardSkippedMessages(int newSequenceNumber)
    {
        List<int> skipped = [];
        foreach (KeyValuePair<int, QueuedSessionMessage> pair in Sequences.QueuedMessages)
        {
            if (pair.Key < newSequenceNumber)
            {
                skipped.Add(pair.Key);
            }
        }
        for (int index = 0; index < skipped.Count; index++)
        {
            Sequences.QueuedMessages.Remove(skipped[index]);
        }
        return skipped.Count;
    }

    internal void ReleaseContiguousMessages(SequenceResetProcessingResult result)
    {
        bool continueRelease = true;
        while (continueRelease)
        {
            int sequenceNumber = Sequences.NextTargetSeqNum;
            if (!Sequences.QueuedMessages.ContainsKey(sequenceNumber))
            {
                continueRelease = false;
                continue;
            }
            QueuedSessionMessage queued = Sequences.QueuedMessages[sequenceNumber];
            if (queued.Message.MsgType == FixAdminMessageTypes.SequenceReset)
            {
                result.SequenceResetPending = true;
                result.PendingSequenceReset = queued;
                continueRelease = false;
                continue;
            }
            SessionSequenceActionResult released = Sequences.ProcessInboundSequence(queued.Message, queued.SequenceNumber, queued.PossibleDuplicate, queued.Rejected);
            if (!released.Consumed)
            {
                result.Accepted = false;
                result.Disconnect = released.Disconnect;
                result.PersistenceFailed = released.PersistenceFailed;
                result.Code = released.Code;
                result.Text = released.Text;
                continueRelease = false;
                continue;
            }
            Sequences.QueuedMessages.Remove(sequenceNumber);
            result.ReleasedMessages.Add(queued);
        }
    }

    internal SequenceResetProcessingResult FromSequenceAction(SessionSequenceActionResult action, int messageSequenceNumber, int newSequenceNumber)
    {
        SequenceResetProcessingResult result = new SequenceResetProcessingResult();
        result.Accepted = action.Accepted;
        result.Disconnect = action.Disconnect;
        result.Queued = action.Queued;
        result.PersistenceFailed = action.PersistenceFailed;
        result.MessageSequenceNumber = messageSequenceNumber;
        result.NewSequenceNumber = newSequenceNumber;
        result.NextTargetSeqNum = Sequences.NextTargetSeqNum;
        result.QueueCount = Sequences.QueuedMessages.Count;
        result.Code = action.Code;
        result.Text = action.Text;
        return result;
    }

    internal SequenceResetProcessingResult CreateSuccess(int messageSequenceNumber, int newSequenceNumber, string code, string text)
    {
        SequenceResetProcessingResult result = new SequenceResetProcessingResult();
        result.Accepted = true;
        result.MessageSequenceNumber = messageSequenceNumber;
        result.NewSequenceNumber = newSequenceNumber;
        result.Code = code;
        result.Text = text;
        return result;
    }

    internal SequenceResetProcessingResult CreateFailure(int messageSequenceNumber, int newSequenceNumber, string code, string text, bool disconnect)
    {
        SequenceResetProcessingResult result = new SequenceResetProcessingResult();
        result.Disconnect = disconnect;
        result.MessageSequenceNumber = messageSequenceNumber;
        result.NewSequenceNumber = newSequenceNumber;
        result.Code = code;
        result.Text = text;
        return CompleteResult(result);
    }

    internal SequenceResetProcessingResult CompleteResult(SequenceResetProcessingResult result)
    {
        result.NextTargetSeqNum = Sequences.NextTargetSeqNum;
        result.QueueCount = Sequences.QueuedMessages.Count;
        return result;
    }
}
