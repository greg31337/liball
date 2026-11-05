using System.Collections.Generic;

namespace cbor;

internal class CborDuplicateKeyProcessor
{
    internal string? ErrorCode;
    internal string? ErrorMessage;
    internal long ErrorOffset = -1;
    internal string ErrorPath = "$";

    internal CborDuplicateKeyProcessor()
    {
    }

    internal bool Process(CborValue value, CborDuplicateKeyPolicy policy)
    {
        int selectedPolicy = (int)policy;
        if (selectedPolicy < (int)CborDuplicateKeyPolicy.Preserve
            || selectedPolicy > (int)CborDuplicateKeyPolicy.KeepLast)
        {
            return Fail(
                "CBOR_DUPLICATE_KEY_POLICY_INVALID",
                "The duplicate-key policy is not defined.",
                -1,
                "$"
            );
        }
        if (policy == CborDuplicateKeyPolicy.Preserve)
        {
            return true;
        }

        List<CborValidationNode> pending = new List<CborValidationNode>();
        pending.Add(new CborValidationNode(value, "$"));
        while (pending.Count > 0)
        {
            int last = pending.Count - 1;
            CborValidationNode node = pending[last];
            pending.RemoveAt(last);
            if (node.Value.Kind == CborValueKind.Map
                && !ProcessMap(node.Value, policy, node.Path))
            {
                return false;
            }
            AddChildren(node.Value, node.Path, pending);
        }
        return true;
    }

    internal bool ProcessMap(
        CborValue map,
        CborDuplicateKeyPolicy policy,
        string path)
    {
        if (map.MapEntries == null)
        {
            return true;
        }
        List<CborMapEntry> retained = new List<CborMapEntry>();
        int sourceIndex = 0;
        while (sourceIndex < map.MapEntries.Count)
        {
            CborMapEntry entry = map.MapEntries[sourceIndex];
            int duplicate = FindEquivalentKey(retained, entry);
            if (duplicate < 0)
            {
                retained.Add(entry);
            }
            else if (policy == CborDuplicateKeyPolicy.Reject)
            {
                return Fail(
                    "CBOR_DUPLICATE_MAP_KEY",
                    "The map contains equivalent keys.",
                    GetOffset(entry.Key),
                    $"{path}[{sourceIndex}].key"
                );
            }
            else if (policy == CborDuplicateKeyPolicy.KeepLast)
            {
                retained.RemoveAt(duplicate);
                retained.Add(entry);
            }
            sourceIndex = sourceIndex + 1;
        }
        map.MapEntries.Clear();
        map.MapEntries.AddRange(retained);
        return true;
    }

    internal static int FindEquivalentKey(
        List<CborMapEntry> entries,
        CborMapEntry candidate)
    {
        if (candidate == null || candidate.Key == null)
        {
            return -1;
        }
        int index = 0;
        while (index < entries.Count)
        {
            CborMapEntry entry = entries[index];
            if (entry != null
                && entry.Key != null
                && CborValueComparer.AreEqual(entry.Key, candidate.Key))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static void AddChildren(
        CborValue value,
        string path,
        List<CborValidationNode> pending)
    {
        if (value.Kind == CborValueKind.Array && value.ArrayItems != null)
        {
            int index = value.ArrayItems.Count - 1;
            while (index >= 0)
            {
                CborValue item = value.ArrayItems[index];
                if (item != null)
                {
                    pending.Add(new CborValidationNode(item, $"{path}[{index}]"));
                }
                index = index - 1;
            }
        }
        if (value.Kind == CborValueKind.Map && value.MapEntries != null)
        {
            int index = value.MapEntries.Count - 1;
            while (index >= 0)
            {
                CborMapEntry entry = value.MapEntries[index];
                if (entry != null && entry.Value != null)
                {
                    pending.Add(new CborValidationNode(
                        entry.Value,
                        $"{path}[{index}].value"
                    ));
                }
                if (entry != null && entry.Key != null)
                {
                    pending.Add(new CborValidationNode(
                        entry.Key,
                        $"{path}[{index}].key"
                    ));
                }
                index = index - 1;
            }
        }
        if (value.Kind == CborValueKind.Tag && value.TaggedValue != null)
        {
            pending.Add(new CborValidationNode(
                value.TaggedValue,
                $"{path}<tag:{value.TagNumber}>"
            ));
        }
    }

    internal static long GetOffset(CborValue? value)
    {
        if (value == null
            || value.Representation == null
            || !value.Representation.HasSourceRange)
        {
            return -1;
        }
        return value.Representation.SourceOffset;
    }

    internal bool Fail(
        string errorCode,
        string errorMessage,
        long errorOffset,
        string errorPath)
    {
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ErrorOffset = errorOffset;
        ErrorPath = errorPath;
        return false;
    }
}
