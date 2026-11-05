namespace fix;

/// <summary>Persists session sequence state and replayable outbound messages.</summary>
public interface IMessageStore
{
    /// <summary>Stores one outbound message record.</summary>
    /// <param name="message">The record to store.</param>
    /// <returns>The explicit store outcome.</returns>
    StoreRangeResult StoreMessage(StoredMessage message);
    /// <summary>Reads an inclusive ordered sequence range.</summary>
    /// <param name="beginSeqNo">The first sequence number.</param>
    /// <param name="endSeqNo">The final sequence number, or zero for the latest.</param>
    /// <returns>The available records and gap diagnostics.</returns>
    StoreRangeResult ReadMessageRange(int beginSeqNo, int endSeqNo);
    /// <summary>Updates the durable next sender and target sequence numbers.</summary>
    /// <param name="nextSenderSeqNum">The next sender sequence number.</param>
    /// <param name="nextTargetSeqNum">The next target sequence number.</param>
    /// <returns>The explicit store outcome.</returns>
    StoreRangeResult SaveSequenceState(int nextSenderSeqNum, int nextTargetSeqNum);
    /// <summary>Refreshes and returns the current durable sequence state.</summary>
    /// <returns>The explicit store outcome and current sequence state.</returns>
    StoreRangeResult RefreshStore();
    /// <summary>Clears durable messages and restores initial sequence state.</summary>
    /// <returns>The explicit store outcome.</returns>
    StoreRangeResult ResetStore();
}
