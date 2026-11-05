using System.Collections.Generic;

namespace cbor;

internal class CddlValidationMemoEntry
{
    internal const int Pending = 0;
    internal const int Matched = 1;
    internal const int Rejected = 2;
    internal CddlRule Rule = new CddlRule();
    internal CborValue Value = new CborValue();
    internal int InstancePosition;
    internal int Context;
    internal int Status;
    internal bool IsActive;
    internal CddlValidationIssue[] Issues = [];
    internal int[] Positions = [];
    internal bool[] UsedEntries = [];
    internal CddlControlResult[] ControlResults = [];
    internal List<CddlMapMatchState> MapStates =
        new List<CddlMapMatchState>();
    internal bool CutMatched;

    internal CddlValidationMemoEntry()
    {
    }

    internal void CompleteMatch()
    {
        Status = Matched;
        Issues = [];
    }

    internal void CompleteRejection(CddlValidationIssue[] issues)
    {
        Status = Rejected;
        Issues = (CddlValidationIssue[])issues.Clone();
    }

    internal void CompleteGroupMatch(CddlGroupMatchResult result)
    {
        Status = result.Positions.Count > 0 ? Matched : Rejected;
        Positions = result.Positions.ToArray();
        Issues = result.Issues.ToArray();
    }

    internal void CompleteMapMatch(CddlMapMatchResult result)
    {
        Status = result.States.Count > 0 ? Matched : Rejected;
        MapStates.Clear();
        int stateIndex = 0;
        while (stateIndex < result.States.Count)
        {
            CddlMapMatchState source = result.States[stateIndex];
            CddlMapMatchState clone = new CddlMapMatchState(
                source.UsedEntries.Length
            );
            int entryIndex = 0;
            while (entryIndex < source.UsedEntries.Length)
            {
                clone.UsedEntries[entryIndex] =
                    source.UsedEntries[entryIndex];
                entryIndex = entryIndex + 1;
            }
            clone.ControlResults = (CddlControlResult[])
                source.ControlResults.Clone();
            MapStates.Add(clone);
            stateIndex = stateIndex + 1;
        }
        Issues = result.Issues.ToArray();
        CutMatched = result.CutMatched;
    }
}

internal class CddlValidationMemoTable
{
    internal List<CddlValidationMemoEntry> Entries =
        new List<CddlValidationMemoEntry>();

    internal CddlValidationMemoTable()
    {
    }

    internal CddlValidationMemoEntry? FindEntry(
        CddlRule rule,
        CborValue value,
        int instancePosition,
        int context)
    {
        int index = 0;
        while (index < Entries.Count)
        {
            CddlValidationMemoEntry entry = Entries[index];
            if (entry.Rule == rule
                && entry.Value == value
                && entry.InstancePosition == instancePosition
                && entry.Context == context)
            {
                return entry;
            }
            index = index + 1;
        }
        return null;
    }

    internal CddlValidationMemoEntry? FindMapEntry(
        CddlRule rule,
        CborValue value,
        bool[] usedEntries,
        CddlControlResult[] controlResults,
        int context)
    {
        int index = 0;
        while (index < Entries.Count)
        {
            CddlValidationMemoEntry entry = Entries[index];
            if (entry.Rule == rule
                && entry.Value == value
                && entry.Context == context
                && BoolArraysEqual(entry.UsedEntries, usedEntries)
                && ControlResultArraysEqual(
                    entry.ControlResults,
                    controlResults))
            {
                return entry;
            }
            index = index + 1;
        }
        return null;
    }

    internal CddlValidationMemoEntry CreateEntry(
        CddlRule rule,
        CborValue value,
        int instancePosition,
        int context)
    {
        CddlValidationMemoEntry entry = new CddlValidationMemoEntry();
        entry.Rule = rule;
        entry.Value = value;
        entry.InstancePosition = instancePosition;
        entry.Context = context;
        Entries.Add(entry);
        return entry;
    }

    internal CddlValidationMemoEntry CreateMapEntry(
        CddlRule rule,
        CborValue value,
        bool[] usedEntries,
        CddlControlResult[] controlResults,
        int context)
    {
        CddlValidationMemoEntry entry = new CddlValidationMemoEntry();
        entry.Rule = rule;
        entry.Value = value;
        entry.Context = context;
        entry.UsedEntries = (bool[])usedEntries.Clone();
        entry.ControlResults = (CddlControlResult[])controlResults.Clone();
        Entries.Add(entry);
        return entry;
    }

    internal bool ControlResultArraysEqual(
        CddlControlResult[] left,
        CddlControlResult[] right)
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

    internal bool BoolArraysEqual(bool[] left, bool[] right)
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
}
