using System;
using System.Collections.Generic;
using System.IO;

namespace avif;

internal class RangeCacheEntry
{
    internal long Offset;
    internal byte[] Data;
    internal int Length;
    internal long LastUse;

    internal RangeCacheEntry(long offset, byte[] data, int length, long lastUse)
    {
        Offset = offset;
        Data = data;
        Length = length;
        LastUse = lastUse;
    }
}

internal class RangeCache : IInputSource
{
    internal Stream Source;
    internal bool LeaveOpen;
    internal int BlockSize;
    internal long MaxCachedBytes;
    internal long CachedBytes;
    internal long SourceLength;
    internal long UseCounter;
    internal List<RangeCacheEntry> Entries;
    internal bool Closed;
    internal AvifResultCode Code;

    internal RangeCache(Stream source, bool leaveOpen, int blockSize, long maxCachedBytes)
    {
        Source = source;
        LeaveOpen = leaveOpen;
        BlockSize = blockSize;
        MaxCachedBytes = maxCachedBytes;
        CachedBytes = 0;
        SourceLength = 0;
        UseCounter = 0;
        Entries = new List<RangeCacheEntry>();
        Closed = false;
        if (source == null
            || !source.CanRead
            || !source.CanSeek
            || blockSize <= 0
            || maxCachedBytes < blockSize)
        {
            Code = AvifResultCode.InvalidArgument;
        }
        else
        {
            SourceLength = source.Length;
            Code = SourceLength < 0 ? AvifResultCode.InvalidData : AvifResultCode.Success;
        }
    }

    /// <summary>Reads up to the requested byte count through the bounded range cache.</summary>
    /// <param name="offset">The absolute source offset.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>The read outcome and transferred byte count.</returns>
    public InputReadResult Read(long offset, Span<byte> destination)
    {
        InputReadResult? unavailable = CheckAvailable(offset);
        if (unavailable != null)
        {
            return unavailable;
        }

        if (offset > SourceLength)
        {
            return new InputReadResult(AvifResultCode.EndOfData, 0, offset, destination.Length);
        }

        if (destination.Length == 0)
        {
            return new InputReadResult(AvifResultCode.Success, 0, 0, 0);
        }

        if (offset == SourceLength)
        {
            return new InputReadResult(AvifResultCode.EndOfData, 0, offset, destination.Length);
        }

        long available = SourceLength - offset;
        int target = destination.Length;
        if (available < target)
        {
            target = (int)available;
        }

        int total = 0;
        while (total < target)
        {
            long currentOffset = offset + total;
            long blockOffset = currentOffset / BlockSize * BlockSize;
            RangeCacheEntry? entry = GetBlock(blockOffset);
            if (entry == null)
            {
                return new InputReadResult(Code, total, currentOffset, target - total);
            }

            int withinBlock = (int)(currentOffset - blockOffset);
            if (withinBlock >= entry.Length)
            {
                Code = AvifResultCode.TruncatedData;
                return new InputReadResult(Code, total, currentOffset, target - total);
            }

            int count = entry.Length - withinBlock;
            if (count > target - total)
            {
                count = target - total;
            }

            entry.Data.AsSpan(withinBlock, count).CopyTo(destination.Slice(total, count));
            total += count;
        }

        return new InputReadResult(AvifResultCode.Success, total, 0, 0);
    }

    /// <summary>Reads an entire cached range or reports the missing suffix.</summary>
    /// <param name="offset">The absolute source offset.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>The read outcome and any missing range.</returns>
    public InputReadResult ReadExact(long offset, Span<byte> destination)
    {
        InputReadResult? unavailable = CheckAvailable(offset);
        if (unavailable != null)
        {
            return unavailable;
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(offset, 0, destination.Length, SourceLength);
        if (!range.Success)
        {
            CheckedIntegerResult requestedEndResult = CheckedMath.AddInt64(offset, destination.Length);
            if (!requestedEndResult.Success)
            {
                return new InputReadResult(requestedEndResult.Code, 0, 0, 0);
            }

            long requestedEnd = requestedEndResult.Value;
            long missingOffset = SourceLength;
            long missingLength = requestedEnd - SourceLength;
            return new InputReadResult(range.Code, 0, missingOffset, missingLength);
        }

        InputReadResult result = Read(offset, destination);
        if (result.Code == AvifResultCode.Success && result.BytesRead != destination.Length)
        {
            Code = AvifResultCode.TruncatedData;
            return new InputReadResult(
                Code,
                result.BytesRead,
                offset + result.BytesRead,
                destination.Length - result.BytesRead);
        }

        return result;
    }

    /// <summary>Clears cached ranges and honors the configured stream ownership.</summary>
    /// <returns>Success; repeated calls are harmless.</returns>
    public AvifResultCode Close()
    {
        if (!Closed)
        {
            Closed = true;
            Entries.Clear();
            CachedBytes = 0;
            if (Source != null && !LeaveOpen)
            {
                Source.Close();
            }
        }

        return AvifResultCode.Success;
    }

    internal InputReadResult? CheckAvailable(long offset)
    {
        if (Closed)
        {
            return new InputReadResult(AvifResultCode.Closed, 0, 0, 0);
        }

        if (Code != AvifResultCode.Success)
        {
            return new InputReadResult(Code, 0, 0, 0);
        }

        if (offset < 0)
        {
            return new InputReadResult(AvifResultCode.InvalidArgument, 0, 0, 0);
        }

        return null;
    }

    internal RangeCacheEntry? GetBlock(long blockOffset)
    {
        int index = 0;
        while (index < Entries.Count)
        {
            RangeCacheEntry existing = Entries[index];
            if (existing.Offset == blockOffset)
            {
                existing.LastUse = NextUse();
                return existing;
            }

            index++;
        }

        long remaining = SourceLength - blockOffset;
        int length = remaining < BlockSize ? (int)remaining : BlockSize;
        if (length <= 0)
        {
            Code = AvifResultCode.TruncatedData;
            return null;
        }

        EvictFor(length);
        if (Code != AvifResultCode.Success)
        {
            return null;
        }

        byte[] data = new byte[length];
        Source.Position = blockOffset;
        int total = 0;
        while (total < length)
        {
            int count = Source.Read(data.AsSpan(total));
            if (count == 0)
            {
                break;
            }

            total += count;
        }

        if (total != length)
        {
            Code = AvifResultCode.TruncatedData;
            return null;
        }

        long lastUse = NextUse();
        RangeCacheEntry entry = new RangeCacheEntry(blockOffset, data, length, lastUse);
        Entries.Add(entry);
        CachedBytes += length;
        return entry;
    }

    internal void EvictFor(int requiredBytes)
    {
        while (requiredBytes > MaxCachedBytes - CachedBytes && Entries.Count > 0)
        {
            int oldestIndex = 0;
            long oldestUse = Entries[0].LastUse;
            int index = 1;
            while (index < Entries.Count)
            {
                if (Entries[index].LastUse < oldestUse)
                {
                    oldestUse = Entries[index].LastUse;
                    oldestIndex = index;
                }

                index++;
            }

            CachedBytes -= Entries[oldestIndex].Length;
            Entries.RemoveAt(oldestIndex);
        }

        if (requiredBytes > MaxCachedBytes - CachedBytes)
        {
            Code = AvifResultCode.LimitExceeded;
        }
    }

    internal long NextUse()
    {
        if (UseCounter == long.MaxValue)
        {
            if (Entries.Count == 0)
            {
                UseCounter = 0;
            }
            else
            {
                long oldestUse = Entries[0].LastUse;
                int findIndex = 1;
                while (findIndex < Entries.Count)
                {
                    if (Entries[findIndex].LastUse < oldestUse)
                    {
                        oldestUse = Entries[findIndex].LastUse;
                    }

                    findIndex++;
                }

                long reduction = oldestUse > 0 ? oldestUse : 1;
                int updateIndex = 0;
                while (updateIndex < Entries.Count)
                {
                    Entries[updateIndex].LastUse -= reduction;
                    updateIndex++;
                }

                UseCounter -= reduction;
            }
        }

        UseCounter++;
        return UseCounter;
    }
}
