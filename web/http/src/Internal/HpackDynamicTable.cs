using System;
using System.Collections.Generic;

namespace http;

internal class HpackDynamicTableResult
{
    internal HttpOperationStatus Status;
    internal HpackTableEntry Entry = new HpackTableEntry();
    internal List<HpackTableEntry> Entries = new List<HpackTableEntry>();
    internal int CurrentSize;
    internal int Limit;
    internal int Count;
    internal int EvictedCount;
    internal bool WasInserted;
    internal bool WasOversized;
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal class HpackDynamicTable
{
    List<HpackTableEntry> entries = new List<HpackTableEntry>();
    int currentSize;
    int limit;

    internal HpackDynamicTable(int maximumSize)
    {
        if (maximumSize < 0)
        {
            throw new ArgumentOutOfRangeException("maximumSize");
        }

        limit = maximumSize;
    }

    internal HpackDynamicTableResult Insert(
        ReadOnlySpan<byte> name,
        ReadOnlySpan<byte> value)
    {
        long measuredSize = (long)name.Length + value.Length + 32;
        HpackDynamicTableResult result = CreateStateResult();
        if (measuredSize > limit)
        {
            result.EvictedCount = entries.Count;
            entries.Clear();
            currentSize = 0;
            result.WasOversized = true;
            UpdateStateResult(result);
            return result;
        }

        int entrySize = (int)measuredSize;
        result.EvictedCount = EvictToFit(entrySize);
        HpackTableEntry stored = new HpackTableEntry();
        stored.Name = CopyOctets(name);
        stored.Value = CopyOctets(value);
        stored.Size = entrySize;
        entries.Insert(0, stored);
        currentSize = currentSize + entrySize;

        result.WasInserted = true;
        result.Entry = CreateSnapshot(0);
        UpdateStateResult(result);
        return result;
    }

    internal HpackDynamicTableResult Resize(int maximumSize)
    {
        HpackDynamicTableResult result = CreateStateResult();
        if (maximumSize < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-hpack-dynamic-table-size";
            result.ErrorMessage =
                "An HPACK dynamic-table size cannot be negative.";
            return result;
        }

        limit = maximumSize;
        result.EvictedCount = EvictToLimit();
        UpdateStateResult(result);
        return result;
    }

    internal HpackTableEntry LookupByIndex(int index)
    {
        int staticCount = GetStaticCount();
        if (index <= staticCount)
        {
            return HpackStaticTableLookup.LookupByIndex(index);
        }

        int entryIndex = index - staticCount - 1;
        if (entryIndex < 0 || entryIndex >= entries.Count)
        {
            return new HpackTableEntry();
        }

        return CreateSnapshot(entryIndex);
    }

    internal HpackTableEntry FindExact(
        ReadOnlySpan<byte> name,
        ReadOnlySpan<byte> value)
    {
        HpackTableEntry staticEntry = HpackStaticTableLookup.FindExact(name, value);
        if (staticEntry.HasIndex)
        {
            return staticEntry;
        }

        int entryIndex = 0;
        while (entryIndex < entries.Count)
        {
            HpackTableEntry entry = entries[entryIndex];
            if (EqualsOctets(name, entry.Name) &&
                EqualsOctets(value, entry.Value))
            {
                return CreateSnapshot(entryIndex);
            }

            entryIndex = entryIndex + 1;
        }

        return new HpackTableEntry();
    }

    internal HpackTableEntry FindName(ReadOnlySpan<byte> name)
    {
        HpackTableEntry staticEntry = HpackStaticTableLookup.FindName(name);
        if (staticEntry.HasIndex)
        {
            return staticEntry;
        }

        int entryIndex = 0;
        while (entryIndex < entries.Count)
        {
            if (EqualsOctets(name, entries[entryIndex].Name))
            {
                return CreateSnapshot(entryIndex);
            }

            entryIndex = entryIndex + 1;
        }

        return new HpackTableEntry();
    }

    internal HpackDynamicTableResult Inspect(int maximumEntries)
    {
        HpackDynamicTableResult result = CreateStateResult();
        if (maximumEntries < 0)
        {
            result.Status = HttpOperationStatus.Rejected;
            result.ErrorCode = "invalid-hpack-dynamic-table-inspection-limit";
            result.ErrorMessage =
                "An HPACK dynamic-table inspection limit cannot be negative.";
            return result;
        }

        int snapshotCount = entries.Count;
        if (maximumEntries < snapshotCount)
        {
            snapshotCount = maximumEntries;
        }

        int entryIndex = 0;
        while (entryIndex < snapshotCount)
        {
            result.Entries.Add(CreateSnapshot(entryIndex));
            entryIndex = entryIndex + 1;
        }

        return result;
    }

    internal int GetCurrentSize()
    {
        return currentSize;
    }

    internal int GetLimit()
    {
        return limit;
    }

    internal int GetCount()
    {
        return entries.Count;
    }

    internal HpackDynamicTable CloneTable()
    {
        HpackDynamicTable clone = new HpackDynamicTable(limit);
        int entryIndex = entries.Count - 1;
        while (entryIndex >= 0)
        {
            HpackTableEntry entry = entries[entryIndex];
            clone.Insert(entry.Name, entry.Value);
            entryIndex = entryIndex - 1;
        }

        return clone;
    }

    int EvictToFit(int entrySize)
    {
        int evictedCount = 0;
        while (entries.Count != 0 && entrySize > limit - currentSize)
        {
            RemoveOldest();
            evictedCount = evictedCount + 1;
        }

        return evictedCount;
    }

    int EvictToLimit()
    {
        int evictedCount = 0;
        while (entries.Count != 0 && currentSize > limit)
        {
            RemoveOldest();
            evictedCount = evictedCount + 1;
        }

        return evictedCount;
    }

    void RemoveOldest()
    {
        int oldestIndex = entries.Count - 1;
        currentSize = currentSize - entries[oldestIndex].Size;
        entries.RemoveAt(oldestIndex);
    }

    HpackTableEntry CreateSnapshot(int entryIndex)
    {
        HpackTableEntry stored = entries[entryIndex];
        HpackTableEntry snapshot = new HpackTableEntry();
        snapshot.Name = CopyOctets(stored.Name);
        snapshot.Value = CopyOctets(stored.Value);
        snapshot.Size = stored.Size;
        snapshot.HasIndex = true;
        snapshot.Index = GetStaticCount() + entryIndex + 1;
        return snapshot;
    }

    HpackDynamicTableResult CreateStateResult()
    {
        HpackDynamicTableResult result = new HpackDynamicTableResult();
        result.Status = HttpOperationStatus.Done;
        UpdateStateResult(result);
        return result;
    }

    void UpdateStateResult(HpackDynamicTableResult result)
    {
        result.CurrentSize = currentSize;
        result.Limit = limit;
        result.Count = entries.Count;
    }

    static int GetStaticCount()
    {
        int count = HpackStaticTable.Names.Length;
        if (HpackStaticTable.Values.Length < count)
        {
            count = HpackStaticTable.Values.Length;
        }

        return count;
    }

    static bool EqualsOctets(
        ReadOnlySpan<byte> left,
        ReadOnlySpan<byte> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static byte[] CopyOctets(ReadOnlySpan<byte> value)
    {
        byte[] result = new byte[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            result[index] = value[index];
            index = index + 1;
        }

        return result;
    }
}
