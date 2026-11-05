using System.Collections.Generic;

namespace fix;

internal class SessionSequenceProcessor
{
    internal IMessageStore Store;
    internal bool Initialized;
    internal int NextSenderSeqNum = FixDefaults.InitialSequenceNumber;
    internal int NextTargetSeqNum = FixDefaults.InitialSequenceNumber;
    internal SortedDictionary<int, QueuedSessionMessage> QueuedMessages = new SortedDictionary<int, QueuedSessionMessage>();

    internal SessionSequenceProcessor(IMessageStore store)
    {
        if (store == null)
        {
            throw new FixSessionException("The session message store is missing.");
        }
        Store = store;
    }

    internal SessionSequenceActionResult RefreshSequences()
    {
        lock (this)
        {
            StoreRangeResult refreshed = Store.RefreshStore();
            if (!IsValidStoreState(refreshed))
            {
                Initialized = false;
                return StoreFailure(refreshed, "sequence-state-refresh-failed", "The durable sequence state could not be loaded.");
            }
            NextSenderSeqNum = refreshed.NextSenderSeqNum;
            NextTargetSeqNum = refreshed.NextTargetSeqNum;
            Initialized = true;
            QueuedMessages.Clear();
            return CreateResult(true, MessageDisposition.Accept, SessionEventKind.None, "sequence-state-refreshed", "The durable sequence state was loaded.");
        }
    }

    internal SessionSequenceActionResult ProcessOutboundSequence(int sequenceNumber, bool sent)
    {
        lock (this)
        {
            if (!Initialized)
            {
                return NotInitialized(sequenceNumber);
            }
            if (sequenceNumber < FixDefaults.InitialSequenceNumber)
            {
                return RejectSequence(sequenceNumber, NextSenderSeqNum, "invalid-sender-sequence-number", "The outbound sequence number must be positive.");
            }
            if (!sent)
            {
                return CreateSequencedResult(false, MessageDisposition.Drop, SessionEventKind.None, sequenceNumber, NextSenderSeqNum, "outbound-message-not-sent", "The outbound message did not consume a sequence number because it was not sent.");
            }
            if (sequenceNumber < NextSenderSeqNum)
            {
                return RejectSequence(sequenceNumber, NextSenderSeqNum, "sender-sequence-too-low", "The outbound sequence number is lower than the next sender sequence number.");
            }
            if (sequenceNumber > NextSenderSeqNum)
            {
                return RejectSequence(sequenceNumber, NextSenderSeqNum, "sender-sequence-too-high", "The outbound sequence number is higher than the next sender sequence number.");
            }
            if (NextSenderSeqNum == int.MaxValue)
            {
                return ExhaustedSequence(sequenceNumber, NextSenderSeqNum, "sender-sequence-number-exhausted", "The sender sequence number cannot be incremented.");
            }

            int nextSender = NextSenderSeqNum + 1;
            StoreRangeResult saved = Store.SaveSequenceState(nextSender, NextTargetSeqNum);
            if (!IsSavedState(saved, nextSender, NextTargetSeqNum))
            {
                SessionSequenceActionResult failure = StoreFailure(saved, "sequence-state-save-failed", "The consumed sender sequence number could not be persisted.");
                failure.SequenceNumber = sequenceNumber;
                failure.ExpectedSequenceNumber = NextSenderSeqNum;
                return failure;
            }
            NextSenderSeqNum = nextSender;
            SessionSequenceActionResult result = CreateSequencedResult(true, MessageDisposition.Accept, SessionEventKind.MessageSent, sequenceNumber, sequenceNumber, "sender-sequence-consumed", "The sent message consumed the expected sender sequence number.");
            result.Consumed = true;
            return result;
        }
    }

    internal SessionSequenceActionResult ProcessInboundSequence(FixMessage message, int sequenceNumber, bool possibleDuplicate, bool rejected)
    {
        lock (this)
        {
            if (!Initialized)
            {
                return NotInitialized(sequenceNumber);
            }
            if (message == null)
            {
                return RejectSequence(sequenceNumber, NextTargetSeqNum, "missing-inbound-message", "The inbound message is missing.");
            }
            if (sequenceNumber < FixDefaults.InitialSequenceNumber)
            {
                SessionSequenceActionResult invalid = RejectSequence(sequenceNumber, NextTargetSeqNum, "invalid-target-sequence-number", "The inbound sequence number must be positive.");
                invalid.Disconnect = true;
                invalid.Disposition = MessageDisposition.Disconnect;
                return invalid;
            }
            if (sequenceNumber < NextTargetSeqNum)
            {
                if (possibleDuplicate)
                {
                    SessionSequenceActionResult duplicate = CreateSequencedResult(true, MessageDisposition.Drop, SessionEventKind.MessageReceived, sequenceNumber, NextTargetSeqNum, "possible-duplicate-ignored", "The lower sequence number was already consumed and PossDupFlag permits it to be ignored.");
                    duplicate.Duplicate = true;
                    return duplicate;
                }
                SessionSequenceActionResult low = CreateSequencedResult(false, MessageDisposition.Disconnect, SessionEventKind.Error, sequenceNumber, NextTargetSeqNum, "target-sequence-too-low", "The inbound sequence number is lower than expected without PossDupFlag.");
                low.RejectMessage = true;
                low.Disconnect = true;
                return low;
            }
            if (sequenceNumber > NextTargetSeqNum)
            {
                return QueueHighSequence(message, sequenceNumber, possibleDuplicate, rejected);
            }
            if (NextTargetSeqNum == int.MaxValue)
            {
                return ExhaustedSequence(sequenceNumber, NextTargetSeqNum, "target-sequence-number-exhausted", "The target sequence number cannot be incremented.");
            }

            int nextTarget = NextTargetSeqNum + 1;
            StoreRangeResult saved = Store.SaveSequenceState(NextSenderSeqNum, nextTarget);
            if (!IsSavedState(saved, NextSenderSeqNum, nextTarget))
            {
                SessionSequenceActionResult failure = StoreFailure(saved, "sequence-state-save-failed", "The consumed target sequence number could not be persisted.");
                failure.SequenceNumber = sequenceNumber;
                failure.ExpectedSequenceNumber = NextTargetSeqNum;
                return failure;
            }
            NextTargetSeqNum = nextTarget;
            MessageDisposition disposition = rejected ? MessageDisposition.Reject : MessageDisposition.Accept;
            string code = rejected ? "rejected-target-sequence-consumed" : "target-sequence-consumed";
            string text = rejected ? "The rejected inbound message consumed the expected target sequence number." : "The inbound message consumed the expected target sequence number.";
            SessionSequenceActionResult result = CreateSequencedResult(!rejected, disposition, SessionEventKind.MessageReceived, sequenceNumber, sequenceNumber, code, text);
            result.Consumed = true;
            result.RejectMessage = rejected;
            return result;
        }
    }

    internal SessionSequenceActionResult AdvanceTargetSequence(int newTargetSeqNum)
    {
        lock (this)
        {
            if (!Initialized)
            {
                return NotInitialized(newTargetSeqNum);
            }
            if (newTargetSeqNum <= NextTargetSeqNum)
            {
                return RejectSequence(newTargetSeqNum, NextTargetSeqNum, "invalid-target-sequence-advance", "The new target sequence number must be higher than the current target sequence number.");
            }
            StoreRangeResult saved = Store.SaveSequenceState(NextSenderSeqNum, newTargetSeqNum);
            if (!IsSavedState(saved, NextSenderSeqNum, newTargetSeqNum))
            {
                SessionSequenceActionResult failure = StoreFailure(saved, "sequence-state-save-failed", "The advanced target sequence number could not be persisted.");
                failure.SequenceNumber = newTargetSeqNum;
                failure.ExpectedSequenceNumber = NextTargetSeqNum;
                return failure;
            }
            int previousTarget = NextTargetSeqNum;
            NextTargetSeqNum = newTargetSeqNum;
            SessionSequenceActionResult result = CreateSequencedResult(true, MessageDisposition.Accept, SessionEventKind.MessageReceived, newTargetSeqNum, previousTarget, "target-sequence-advanced", "The target sequence number advanced to NewSeqNo.");
            result.Consumed = true;
            return result;
        }
    }

    internal SessionSequenceActionResult QueueHighSequence(FixMessage message, int sequenceNumber, bool possibleDuplicate, bool rejected)
    {
        if (QueuedMessages.ContainsKey(sequenceNumber))
        {
            SessionSequenceActionResult repeated = CreateSequencedResult(true, MessageDisposition.Drop, SessionEventKind.SequenceGap, sequenceNumber, NextTargetSeqNum, "sequence-already-queued", "The higher sequence number is already queued.");
            repeated.Queued = true;
            repeated.Duplicate = true;
            return repeated;
        }
        QueuedSessionMessage queuedMessage = new QueuedSessionMessage();
        queuedMessage.Message = message;
        queuedMessage.SequenceNumber = sequenceNumber;
        queuedMessage.PossibleDuplicate = possibleDuplicate;
        queuedMessage.Rejected = rejected;
        QueuedMessages.Add(sequenceNumber, queuedMessage);
        SessionSequenceActionResult queued = CreateSequencedResult(true, MessageDisposition.Drop, SessionEventKind.SequenceGap, sequenceNumber, NextTargetSeqNum, "sequence-gap-queued", "The higher sequence number was queued until the missing range is recovered.");
        queued.Queued = true;
        return queued;
    }

    internal SessionSequenceActionResult NotInitialized(int sequenceNumber)
    {
        SessionSequenceActionResult result = CreateSequencedResult(false, MessageDisposition.Disconnect, SessionEventKind.Error, sequenceNumber, 0, "sequence-state-not-initialized", "The durable sequence state has not been loaded.");
        result.Disconnect = true;
        return result;
    }

    internal SessionSequenceActionResult RejectSequence(int sequenceNumber, int expectedSequenceNumber, string code, string text)
    {
        SessionSequenceActionResult result = CreateSequencedResult(false, MessageDisposition.Reject, SessionEventKind.Error, sequenceNumber, expectedSequenceNumber, code, text);
        result.RejectMessage = true;
        return result;
    }

    internal SessionSequenceActionResult ExhaustedSequence(int sequenceNumber, int expectedSequenceNumber, string code, string text)
    {
        SessionSequenceActionResult result = CreateSequencedResult(false, MessageDisposition.Disconnect, SessionEventKind.Error, sequenceNumber, expectedSequenceNumber, code, text);
        result.Disconnect = true;
        return result;
    }

    internal SessionSequenceActionResult StoreFailure(StoreRangeResult? storeResult, string fallbackCode, string fallbackText)
    {
        string code = storeResult == null || storeResult.Code == null || storeResult.Code.Length == 0 ? fallbackCode : storeResult.Code;
        string text = storeResult == null || storeResult.Text == null || storeResult.Text.Length == 0 ? fallbackText : storeResult.Text;
        SessionSequenceActionResult result = CreateResult(false, MessageDisposition.Disconnect, SessionEventKind.Error, code, text);
        result.Disconnect = true;
        result.PersistenceFailed = true;
        return result;
    }

    internal bool IsValidStoreState(StoreRangeResult? result)
    {
        return result != null && result.Success && result.HasSequenceState && result.NextSenderSeqNum >= FixDefaults.InitialSequenceNumber && result.NextTargetSeqNum >= FixDefaults.InitialSequenceNumber;
    }

    internal bool IsSavedState(StoreRangeResult? result, int expectedSender, int expectedTarget)
    {
        if (result == null)
        {
            return false;
        }
        return IsValidStoreState(result) && result.NextSenderSeqNum == expectedSender && result.NextTargetSeqNum == expectedTarget;
    }

    internal SessionSequenceActionResult CreateSequencedResult(bool accepted, MessageDisposition disposition, SessionEventKind eventKind, int sequenceNumber, int expectedSequenceNumber, string code, string text)
    {
        SessionSequenceActionResult result = CreateResult(accepted, disposition, eventKind, code, text);
        result.SequenceNumber = sequenceNumber;
        result.ExpectedSequenceNumber = expectedSequenceNumber;
        return result;
    }

    internal SessionSequenceActionResult CreateResult(bool accepted, MessageDisposition disposition, SessionEventKind eventKind, string code, string text)
    {
        SessionSequenceActionResult result = new SessionSequenceActionResult();
        result.Accepted = accepted;
        result.Disposition = disposition;
        result.EventKind = eventKind;
        result.NextSenderSeqNum = NextSenderSeqNum;
        result.NextTargetSeqNum = NextTargetSeqNum;
        result.QueueCount = QueuedMessages.Count;
        result.Code = code;
        result.Text = text;
        return result;
    }
}
