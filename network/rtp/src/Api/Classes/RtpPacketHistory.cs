using System.Collections.Generic;

namespace rtp;

/// <summary>Retains a bounded rolling history of sent RTP packets.</summary>
public class RtpPacketHistory : IRtpPacketHistory
{
    internal object syncRoot = new object();
    internal List<RtpPacketHistoryEntry> entries =
        new List<RtpPacketHistoryEntry>();
    internal int maximumPacketCount;
    internal int maximumPacketBytes;
    internal long maximumPacketAgeTicks;
    internal int maximumRtpPacketSize;
    internal bool strictValidation;
    internal int retainedPacketBytes;
    internal bool hasCurrentTime;
    internal long currentTimeTicks;

    /// <summary>Initializes packet history from an isolated session configuration.</summary>
    /// <param name="configuration">The packet validation and history limits.</param>
    public RtpPacketHistory(RtpSessionConfiguration configuration)
    {
        RtpConfigurationValidator.ValidateRtpPacketHistoryConfiguration(
            configuration);
        maximumPacketCount = configuration.MaximumPacketHistoryCount;
        maximumPacketBytes = configuration.MaximumPacketHistoryBytes;
        maximumPacketAgeTicks = configuration.MaximumPacketHistoryAgeTicks;
        maximumRtpPacketSize = configuration.MaximumRtpPacketSize;
        strictValidation = configuration.StrictValidation;
    }

    /// <summary>Stores an independent copy of one sent RTP packet.</summary>
    /// <param name="packet">The packet to retain.</param>
    /// <param name="context">The packet send-time context.</param>
    public void StoreRtpHistoryPacket(
        RtpPacket packet,
        RtpPacketContext context)
    {
        StoreHistoryPacket(packet, context, true);
    }

    /// <summary>Attempts to store a packet without throwing for capacity failure.</summary>
    /// <param name="packet">The packet to retain.</param>
    /// <param name="context">The packet send-time context.</param>
    /// <returns><c>true</c> when the packet was retained.</returns>
    public bool TryStoreRtpHistoryPacket(
        RtpPacket packet,
        RtpPacketContext context)
    {
        return StoreHistoryPacket(packet, context, false);
    }

    /// <summary>Finds an exact sent packet by SSRC and sequence number.</summary>
    /// <param name="sourceIdentifier">The packet synchronization source.</param>
    /// <param name="sequenceNumber">The packet sequence number.</param>
    /// <returns>An owned packet copy, or <c>null</c> when absent.</returns>
    public RtpPacket? FindRtpHistoryPacket(
        uint sourceIdentifier,
        ushort sequenceNumber)
    {
        lock (syncRoot)
        {
            int entryIndex = entries.Count - 1;
            while (entryIndex >= 0)
            {
                RtpPacket candidate = entries[entryIndex].Packet;
                if (candidate.SynchronizationSource == sourceIdentifier &&
                    candidate.SequenceNumber == sequenceNumber)
                {
                    return candidate.Copy();
                }

                entryIndex = entryIndex - 1;
            }

            return null;
        }
    }

    /// <summary>Removes packets older than the configured history age.</summary>
    /// <param name="currentTimeTicks">The current monotonic time.</param>
    /// <returns>The number of packets removed.</returns>
    public int RemoveExpiredRtpHistoryPackets(long currentTimeTicks)
    {
        ValidateHistoryTime(currentTimeTicks);
        lock (syncRoot)
        {
            ValidateHistoryChronology(currentTimeTicks);
            int removedCount = RemoveExpiredHistoryEntries(
                currentTimeTicks);
            RecordHistoryTime(currentTimeTicks);
            return removedCount;
        }
    }

    /// <summary>Clears every retained history packet.</summary>
    public void ClearRtpPacketHistory()
    {
        lock (syncRoot)
        {
            entries.Clear();
            retainedPacketBytes = 0;
            hasCurrentTime = false;
            currentTimeTicks = 0L;
        }
    }

    internal bool StoreHistoryPacket(
        RtpPacket packet,
        RtpPacketContext context,
        bool throwOnCapacity)
    {
        int encodedSize = ValidateHistoryPacket(packet, context);
        lock (syncRoot)
        {
            ValidateHistoryChronology(context.ArrivalTimeTicks);
            bool packetFits = maximumPacketCount > 0 &&
                encodedSize <= maximumPacketBytes;
            if (!packetFits)
            {
                if (throwOnCapacity)
                {
                    throw new RtpBufferCapacityException(
                        "The RTP packet cannot fit within the history limits.");
                }

                return false;
            }

            RtpPacketHistoryEntry entry = CreateHistoryEntry(
                packet,
                context,
                encodedSize);
            RemoveExpiredHistoryEntries(context.ArrivalTimeTicks);
            RemoveMatchingHistoryEntries(
                packet.SynchronizationSource,
                packet.SequenceNumber);
            while (entries.Count >= maximumPacketCount ||
                retainedPacketBytes > maximumPacketBytes - encodedSize)
            {
                RemoveHistoryEntry(0);
            }

            entries.Add(entry);
            retainedPacketBytes = retainedPacketBytes + encodedSize;
            RecordHistoryTime(context.ArrivalTimeTicks);
            return true;
        }
    }

    internal int ValidateHistoryPacket(
        RtpPacket packet,
        RtpPacketContext context)
    {
        if (context == null)
        {
            throw new RtpConfigurationException(
                "The RTP history packet context is required.");
        }

        ValidateHistoryTime(context.ArrivalTimeTicks);
        return RtpPacketEncoder.GetEncodedSize(
            packet,
            strictValidation,
            maximumRtpPacketSize);
    }

    internal RtpPacketHistoryEntry CreateHistoryEntry(
        RtpPacket packet,
        RtpPacketContext context,
        int encodedSize)
    {
        RtpPacketHistoryEntry entry = new RtpPacketHistoryEntry();
        entry.Packet = packet.Copy();
        entry.Context = context.Copy();
        entry.EncodedSize = encodedSize;
        return entry;
    }

    internal int RemoveExpiredHistoryEntries(long operationTimeTicks)
    {
        if (maximumPacketAgeTicks == 0)
        {
            return 0;
        }

        int removedCount = 0;
        while (entries.Count > 0 &&
            operationTimeTicks > entries[0].Context.ArrivalTimeTicks &&
            operationTimeTicks - entries[0].Context.ArrivalTimeTicks >
            maximumPacketAgeTicks)
        {
            RemoveHistoryEntry(0);
            removedCount = removedCount + 1;
        }

        return removedCount;
    }

    internal void RemoveMatchingHistoryEntries(
        uint sourceIdentifier,
        ushort sequenceNumber)
    {
        int entryIndex = 0;
        while (entryIndex < entries.Count)
        {
            RtpPacket candidate = entries[entryIndex].Packet;
            if (candidate.SynchronizationSource == sourceIdentifier &&
                candidate.SequenceNumber == sequenceNumber)
            {
                RemoveHistoryEntry(entryIndex);
            }
            else
            {
                entryIndex = entryIndex + 1;
            }
        }
    }

    internal void RemoveHistoryEntry(int entryIndex)
    {
        retainedPacketBytes = retainedPacketBytes -
            entries[entryIndex].EncodedSize;
        entries.RemoveAt(entryIndex);
    }

    internal void ValidateHistoryTime(long operationTimeTicks)
    {
        if (operationTimeTicks < 0)
        {
            throw new RtpConfigurationException(
                "RTP packet history time cannot be negative.");
        }
    }

    internal void ValidateHistoryChronology(long operationTimeTicks)
    {
        if (hasCurrentTime && operationTimeTicks < currentTimeTicks)
        {
            throw new RtpSessionException(
                "RTP packet history time cannot move backward.");
        }
    }

    internal void RecordHistoryTime(long operationTimeTicks)
    {
        hasCurrentTime = true;
        currentTimeTicks = operationTimeTicks;
    }
}
