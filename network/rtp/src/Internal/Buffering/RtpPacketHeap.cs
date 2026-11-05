using System.Collections.Generic;

namespace rtp;

internal class RtpPacketHeap
{
    internal uint SourceIdentifier;
    internal List<RtpJitterBufferEntry> Entries =
        new List<RtpJitterBufferEntry>();
    internal List<RtpMissingPacketState> MissingPackets =
        new List<RtpMissingPacketState>();
    internal int BufferedOctetCount;
    internal bool HasReleaseSequence;
    internal bool ReleaseSequenceExhausted;
    internal uint NextReleaseExtendedSequenceNumber;
    internal bool HasOpenGap;
    internal uint GapExtendedSequenceNumber;
    internal long GapStartTimeTicks;

    internal int FindExtendedSequenceIndex(uint extendedSequenceNumber)
    {
        int entryIndex = 0;
        while (entryIndex < Entries.Count)
        {
            uint candidate =
                Entries[entryIndex].SequenceUpdate.ExtendedSequenceNumber;
            if (candidate == extendedSequenceNumber)
            {
                return entryIndex;
            }

            if (candidate > extendedSequenceNumber)
            {
                return -1;
            }

            entryIndex = entryIndex + 1;
        }

        return -1;
    }

    internal int FindPacketInsertionIndex(uint extendedSequenceNumber)
    {
        int entryIndex = 0;
        while (entryIndex < Entries.Count &&
            Entries[entryIndex].SequenceUpdate.ExtendedSequenceNumber <
            extendedSequenceNumber)
        {
            entryIndex = entryIndex + 1;
        }

        return entryIndex;
    }

    internal int FindMissingPacketIndex(uint extendedSequenceNumber)
    {
        int missingIndex = 0;
        while (missingIndex < MissingPackets.Count)
        {
            uint candidate =
                MissingPackets[missingIndex].ExtendedSequenceNumber;
            if (candidate == extendedSequenceNumber)
            {
                return missingIndex;
            }

            if (candidate > extendedSequenceNumber)
            {
                return -1;
            }

            missingIndex = missingIndex + 1;
        }

        return -1;
    }

    internal void InsertMissingPacketState(RtpMissingPacketState state)
    {
        int missingIndex = 0;
        while (missingIndex < MissingPackets.Count &&
            MissingPackets[missingIndex].ExtendedSequenceNumber <
            state.ExtendedSequenceNumber)
        {
            missingIndex = missingIndex + 1;
        }

        MissingPackets.Insert(missingIndex, state);
    }

    internal void RemoveMissingPacketState(uint extendedSequenceNumber)
    {
        int missingIndex = FindMissingPacketIndex(extendedSequenceNumber);
        if (missingIndex >= 0)
        {
            MissingPackets.RemoveAt(missingIndex);
        }
    }

    internal void InsertBufferedEntry(RtpJitterBufferEntry entry)
    {
        int insertionIndex = FindPacketInsertionIndex(
            entry.SequenceUpdate.ExtendedSequenceNumber);
        Entries.Insert(insertionIndex, entry);
        BufferedOctetCount = BufferedOctetCount + entry.Packet.Payload.Length;
    }

    internal void RemoveBufferedEntry(int entryIndex)
    {
        RtpJitterBufferEntry entry = Entries[entryIndex];
        BufferedOctetCount = BufferedOctetCount - entry.Packet.Payload.Length;
        Entries.RemoveAt(entryIndex);
    }

    internal void ClearBufferedEntries()
    {
        Entries.Clear();
        MissingPackets.Clear();
        BufferedOctetCount = 0;
        HasReleaseSequence = false;
        ReleaseSequenceExhausted = false;
        NextReleaseExtendedSequenceNumber = 0U;
        HasOpenGap = false;
        GapExtendedSequenceNumber = 0U;
        GapStartTimeTicks = 0L;
    }
}
