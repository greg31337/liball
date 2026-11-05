using System.Collections.Generic;

namespace fix;

internal class ReplayProcessor
{
    internal IMessageStore Store;
    internal SessionSequenceProcessor Sequences;
    internal ReplayBuilder Builder;
    internal ISessionApplication Application;

    internal ReplayProcessor(IMessageStore store, SessionSequenceProcessor sequences, ReplayBuilder builder, ISessionApplication application)
    {
        if (store == null || sequences == null || builder == null || application == null)
        {
            throw new FixSessionException("Replay processing dependencies are missing.");
        }
        Store = store;
        Sequences = sequences;
        Builder = builder;
        Application = application;
    }

    internal ReplayProcessingResult ProcessReplayRange(int beginSeqNo, int endSeqNo)
    {
        lock (this)
        {
            if (!Sequences.Initialized)
            {
                return CreateFailure(beginSeqNo, endSeqNo, "sequence-state-not-initialized", "The sender sequence state has not been loaded.", true, false);
            }
            if (beginSeqNo < FixDefaults.InitialSequenceNumber || endSeqNo < 0 || endSeqNo != 0 && endSeqNo < beginSeqNo)
            {
                return CreateFailure(beginSeqNo, endSeqNo, "invalid-replay-range", "The requested replay range is invalid.", false, false);
            }
            int senderSnapshot = Sequences.NextSenderSeqNum;
            int lastSentSeqNo = senderSnapshot - 1;
            if (beginSeqNo > lastSentSeqNo)
            {
                ReplayProcessingResult empty = CreateSuccess(beginSeqNo, lastSentSeqNo, senderSnapshot, "replay-range-empty", "The requested replay range contains no sent sequence numbers.");
                return empty;
            }
            int resolvedEndSeqNo = endSeqNo == 0 || endSeqNo > lastSentSeqNo ? lastSentSeqNo : endSeqNo;
            StoreRangeResult range = Store.ReadMessageRange(beginSeqNo, resolvedEndSeqNo);
            if (range == null || !range.Success)
            {
                string code = range == null || range.Code == null || range.Code.Length == 0 ? "replay-store-read-failed" : range.Code;
                string text = range == null || range.Text == null || range.Text.Length == 0 ? "The replay range could not be read from the message store." : range.Text;
                return CreateFailure(beginSeqNo, resolvedEndSeqNo, code, text, true, false);
            }
            if (range.IsCorrupt || !ValidateStoredRange(range.Messages, beginSeqNo, resolvedEndSeqNo))
            {
                return CreateFailure(beginSeqNo, resolvedEndSeqNo, "corrupt-replay-store-range", "The replay store range is corrupt or unordered.", true, true);
            }

            ReplayProcessingResult result = CreateSuccess(beginSeqNo, resolvedEndSeqNo, senderSnapshot, "replay-range-ready", "The replay range was processed.");
            int recordIndex = 0;
            int gapBeginSeqNo = 0;
            for (int sequenceNumber = beginSeqNo; sequenceNumber <= resolvedEndSeqNo; sequenceNumber++)
            {
                StoredMessage? stored = null;
                if (recordIndex < range.Messages.Count && range.Messages[recordIndex].SequenceNumber == sequenceNumber)
                {
                    stored = range.Messages[recordIndex];
                    recordIndex++;
                }
                if (stored == null)
                {
                    if (gapBeginSeqNo == 0) gapBeginSeqNo = sequenceNumber;
                    result.UnavailableSequenceCount++;
                    result.UsedUnavailableGapFill = true;
                    continue;
                }
                if (stored.IsAdministrative)
                {
                    if (gapBeginSeqNo == 0) gapBeginSeqNo = sequenceNumber;
                    result.AdministrativeSequenceCount++;
                    continue;
                }

                ReplayMessageResult prepared = Builder.PrepareApplicationReplay(stored);
                if (!prepared.Success || prepared.Message == null)
                {
                    return BuilderFailure(result, prepared, senderSnapshot, true);
                }
                ApplicationMessageContext context = CreateReplayContext(prepared.Message, sequenceNumber);
                ApplicationDecision decision = Application.OnReplayMessage(context);
                if (decision == null)
                {
                    return CreateFailure(beginSeqNo, resolvedEndSeqNo, "missing-replay-decision", "The application did not return a replay decision.", true, false);
                }
                prepared.ApplicationDecision = decision;
                if (decision.Disposition == MessageDisposition.Disconnect)
                {
                    string code = decision.Code == null || decision.Code.Length == 0 ? "application-replay-disconnect" : decision.Code;
                    string text = decision.Text == null || decision.Text.Length == 0 ? "The application requested disconnection during replay." : decision.Text;
                    return CreateFailure(beginSeqNo, resolvedEndSeqNo, code, text, true, false);
                }
                if (decision.Disposition == MessageDisposition.Drop || decision.Disposition == MessageDisposition.Reject)
                {
                    if (gapBeginSeqNo == 0) gapBeginSeqNo = sequenceNumber;
                    result.SkippedApplicationCount++;
                    continue;
                }
                if (decision.Disposition != MessageDisposition.Accept)
                {
                    return CreateFailure(beginSeqNo, resolvedEndSeqNo, "invalid-replay-disposition", "The application returned an invalid replay disposition.", true, false);
                }
                if (gapBeginSeqNo != 0)
                {
                    if (!AppendGapFill(result, gapBeginSeqNo, sequenceNumber - 1, senderSnapshot)) return result;
                    gapBeginSeqNo = 0;
                }
                ReplayMessageResult encoded = Builder.EncodeApplicationReplay(prepared);
                if (!encoded.Success)
                {
                    return BuilderFailure(result, encoded, senderSnapshot, false);
                }
                result.Messages.Add(encoded);
                result.ReplayedApplicationCount++;
            }
            if (gapBeginSeqNo != 0)
            {
                if (!AppendGapFill(result, gapBeginSeqNo, resolvedEndSeqNo, senderSnapshot)) return result;
            }
            if (Sequences.NextSenderSeqNum != senderSnapshot)
            {
                return CreateFailure(beginSeqNo, resolvedEndSeqNo, "replay-consumed-sender-sequence", "Replay unexpectedly changed the live sender sequence number.", true, false);
            }
            result.NextSenderSeqNum = senderSnapshot;
            return result;
        }
    }

    internal bool AppendGapFill(ReplayProcessingResult result, int beginSeqNo, int endSeqNo, int senderSnapshot)
    {
        ReplayMessageResult gapFill = Builder.CreateGapFillReplay(beginSeqNo, endSeqNo);
        if (!gapFill.Success)
        {
            ReplayProcessingResult failure = BuilderFailure(result, gapFill, senderSnapshot, false);
            result.Accepted = failure.Accepted;
            result.Disconnect = failure.Disconnect;
            result.StoreCorrupt = failure.StoreCorrupt;
            result.Code = failure.Code;
            result.Text = failure.Text;
            result.Messages.Clear();
            return false;
        }
        result.Messages.Add(gapFill);
        result.GapFillCount++;
        return true;
    }

    internal ReplayProcessingResult BuilderFailure(ReplayProcessingResult current, ReplayMessageResult failed, int senderSnapshot, bool corrupt)
    {
        current.Accepted = false;
        current.Disconnect = true;
        current.StoreCorrupt = corrupt;
        current.NextSenderSeqNum = senderSnapshot;
        current.Code = failed.Code.Length == 0 ? "replay-build-failed" : failed.Code;
        current.Text = failed.Text.Length == 0 ? "A replay message could not be built." : failed.Text;
        current.Messages.Clear();
        return current;
    }

    internal ApplicationMessageContext CreateReplayContext(FixMessage message, int sequenceNumber)
    {
        ApplicationMessageContext context = new ApplicationMessageContext();
        context.Id = LogonProcessor.CopySessionId(Builder.Id);
        context.Message = message;
        context.SequenceNumber = sequenceNumber;
        context.IsReplay = true;
        context.IsPossibleDuplicate = true;
        return context;
    }

    internal static bool ValidateStoredRange(List<StoredMessage> messages, int beginSeqNo, int endSeqNo)
    {
        int previous = beginSeqNo - 1;
        for (int index = 0; index < messages.Count; index++)
        {
            StoredMessage message = messages[index];
            if (message == null || message.SequenceNumber < beginSeqNo || message.SequenceNumber > endSeqNo || message.SequenceNumber <= previous)
            {
                return false;
            }
            previous = message.SequenceNumber;
        }
        return true;
    }

    internal ReplayProcessingResult CreateSuccess(int beginSeqNo, int endSeqNo, int nextSenderSeqNum, string code, string text)
    {
        ReplayProcessingResult result = new ReplayProcessingResult();
        result.Accepted = true;
        result.BeginSeqNo = beginSeqNo;
        result.EndSeqNo = endSeqNo;
        result.NextSenderSeqNum = nextSenderSeqNum;
        result.Code = code;
        result.Text = text;
        return result;
    }

    internal ReplayProcessingResult CreateFailure(int beginSeqNo, int endSeqNo, string code, string text, bool disconnect, bool corrupt)
    {
        ReplayProcessingResult result = new ReplayProcessingResult();
        result.Disconnect = disconnect;
        result.StoreCorrupt = corrupt;
        result.BeginSeqNo = beginSeqNo;
        result.EndSeqNo = endSeqNo;
        result.NextSenderSeqNum = Sequences.NextSenderSeqNum;
        result.Code = code;
        result.Text = text;
        return result;
    }
}
