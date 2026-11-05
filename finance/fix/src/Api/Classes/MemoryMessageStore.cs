using System;
using System.Collections.Generic;

namespace fix;

/// <summary>Stores replayable messages and sequence state in process memory.</summary>
public class MemoryMessageStore : IMessageStore
{
    internal object StoreLock = new object();
    internal SortedDictionary<int, StoredMessage> StoredMessages = new SortedDictionary<int, StoredMessage>();
    internal int MaximumMessages;
    internal int NextSenderSeqNum = FixDefaults.InitialSequenceNumber;
    internal int NextTargetSeqNum = FixDefaults.InitialSequenceNumber;

    /// <summary>Creates an in-memory store with count-based message retention.</summary>
    /// <param name="maximumMessages">The maximum retained message count, or zero for no count limit.</param>
    /// <exception cref="FixStoreException">Thrown when <c>maximumMessages</c> is negative.</exception>
    public MemoryMessageStore(int maximumMessages)
    {
        if (maximumMessages < 0)
        {
            throw new FixStoreException("The maximum retained message count cannot be negative.");
        }
        MaximumMessages = maximumMessages;
    }

    /// <summary>Stores a detached copy of one outbound message record.</summary>
    /// <param name="message">The record to store.</param>
    /// <returns>The explicit store outcome.</returns>
    public StoreRangeResult StoreMessage(StoredMessage message)
    {
        lock (StoreLock)
        {
            string validationCode = StoredMessageValidation.ValidateStoredMessage(message);
            if (validationCode.Length != 0)
            {
                return CreateFailureResult(validationCode, "The stored message is invalid.");
            }

            StoredMessages[message.SequenceNumber] = CloneStoredMessage(message);
            ApplyMessageRetention();
            StoreRangeResult result = CreateSuccessResult();
            result.BeginSeqNo = message.SequenceNumber;
            result.EndSeqNo = message.SequenceNumber;
            return result;
        }
    }

    /// <summary>Reads detached records in ascending sequence order for an inclusive range.</summary>
    /// <param name="beginSeqNo">The first sequence number.</param>
    /// <param name="endSeqNo">The final sequence number, or zero for the latest retained record.</param>
    /// <returns>The available records, current sequence state, and gap status.</returns>
    public StoreRangeResult ReadMessageRange(int beginSeqNo, int endSeqNo)
    {
        lock (StoreLock)
        {
            if (beginSeqNo < FixDefaults.InitialSequenceNumber || endSeqNo < 0)
            {
                return CreateFailureResult("invalid-message-range", "Message range sequence numbers are invalid.");
            }
            if (endSeqNo != 0 && endSeqNo < beginSeqNo)
            {
                return CreateFailureResult("inverted-message-range", "The message range ends before it begins.");
            }

            StoreRangeResult result = CreateSuccessResult();
            result.BeginSeqNo = beginSeqNo;
            int resolvedEnd = endSeqNo;
            if (resolvedEnd == 0)
            {
                resolvedEnd = FindLatestSequenceNumber();
            }
            result.EndSeqNo = resolvedEnd;
            if (resolvedEnd < beginSeqNo)
            {
                result.HasGap = true;
                return result;
            }

            foreach (KeyValuePair<int, StoredMessage> pair in StoredMessages)
            {
                if (pair.Key < beginSeqNo)
                {
                    continue;
                }
                if (pair.Key > resolvedEnd)
                {
                    break;
                }
                result.Messages.Add(CloneStoredMessage(pair.Value));
            }
            long requestedCount = (long)resolvedEnd - beginSeqNo + 1L;
            result.HasGap = result.Messages.Count != requestedCount;
            return result;
        }
    }

    /// <summary>Atomically updates both next sequence numbers.</summary>
    /// <param name="nextSenderSeqNum">The next sender sequence number.</param>
    /// <param name="nextTargetSeqNum">The next target sequence number.</param>
    /// <returns>The explicit store outcome and resulting sequence state.</returns>
    public StoreRangeResult SaveSequenceState(int nextSenderSeqNum, int nextTargetSeqNum)
    {
        lock (StoreLock)
        {
            if (nextSenderSeqNum < FixDefaults.InitialSequenceNumber || nextTargetSeqNum < FixDefaults.InitialSequenceNumber)
            {
                return CreateFailureResult("invalid-sequence-state", "Next sender and target sequence numbers must be positive.");
            }
            NextSenderSeqNum = nextSenderSeqNum;
            NextTargetSeqNum = nextTargetSeqNum;
            return CreateSuccessResult();
        }
    }

    /// <summary>Returns the current in-memory sequence state without changing it.</summary>
    /// <returns>The explicit store outcome and current sequence state.</returns>
    public StoreRangeResult RefreshStore()
    {
        lock (StoreLock)
        {
            return CreateSuccessResult();
        }
    }

    /// <summary>Clears all retained records and restores both sequence numbers to one.</summary>
    /// <returns>The explicit store outcome and reset sequence state.</returns>
    public StoreRangeResult ResetStore()
    {
        lock (StoreLock)
        {
            StoredMessages.Clear();
            NextSenderSeqNum = FixDefaults.InitialSequenceNumber;
            NextTargetSeqNum = FixDefaults.InitialSequenceNumber;
            return CreateSuccessResult();
        }
    }

    internal void ApplyMessageRetention()
    {
        while (MaximumMessages > 0 && StoredMessages.Count > MaximumMessages)
        {
            int oldestSequence = FindEarliestSequenceNumber();
            StoredMessages.Remove(oldestSequence);
        }
    }

    internal int FindEarliestSequenceNumber()
    {
        foreach (KeyValuePair<int, StoredMessage> pair in StoredMessages)
        {
            return pair.Key;
        }
        return 0;
    }

    internal int FindLatestSequenceNumber()
    {
        int latest = 0;
        foreach (KeyValuePair<int, StoredMessage> pair in StoredMessages)
        {
            latest = pair.Key;
        }
        return latest;
    }

    internal StoreRangeResult CreateSuccessResult()
    {
        StoreRangeResult result = new StoreRangeResult();
        result.Success = true;
        CopySequenceState(result);
        return result;
    }

    internal StoreRangeResult CreateFailureResult(string code, string text)
    {
        StoreRangeResult result = new StoreRangeResult();
        result.Code = code;
        result.Text = text;
        CopySequenceState(result);
        return result;
    }

    internal void CopySequenceState(StoreRangeResult result)
    {
        result.HasSequenceState = true;
        result.NextSenderSeqNum = NextSenderSeqNum;
        result.NextTargetSeqNum = NextTargetSeqNum;
    }

    internal static StoredMessage CloneStoredMessage(StoredMessage source)
    {
        StoredMessage copy = new StoredMessage();
        copy.SequenceNumber = source.SequenceNumber;
        copy.SendingTimeUtc = source.SendingTimeUtc;
        copy.Bytes = new byte[source.Bytes.Length];
        Array.Copy(source.Bytes, copy.Bytes, source.Bytes.Length);
        copy.MsgType = source.MsgType;
        copy.IsAdministrative = source.IsAdministrative;
        return copy;
    }
}
