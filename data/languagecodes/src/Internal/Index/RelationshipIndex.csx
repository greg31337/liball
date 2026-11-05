namespace languagecodes;

internal class RelationshipIndexEntry
{
    internal string ParentCode;
    internal string ChildCode;
    internal LanguageRelationshipKind Kind;
    internal ReconciledRelationshipStatus Status;
    internal ReconciledSource Source;

    internal RelationshipIndexEntry(
        string parentCode,
        string childCode,
        LanguageRelationshipKind kind,
        ReconciledRelationshipStatus status,
        ReconciledSource source)
    {
        ParentCode = parentCode;
        ChildCode = childCode;
        Kind = kind;
        Status = status;
        Source = source;
    }
}

internal class RelationshipIndexData
{
    internal global::System.Collections.Generic.Dictionary<string, RelationshipIndexEntry[]>
        MacrolanguageMembersByParent;
    internal global::System.Collections.Generic.Dictionary<string, RelationshipIndexEntry[]>
        MacrolanguagesByMember;
    internal global::System.Collections.Generic.Dictionary<string, RelationshipIndexEntry[]>
        GroupChildrenByParent;
    internal global::System.Collections.Generic.Dictionary<string, RelationshipIndexEntry[]>
        GroupParentsByChild;

    internal RelationshipIndexData(
        global::System.Collections.Generic.Dictionary<string, RelationshipIndexEntry[]>
            macrolanguageMembersByParent,
        global::System.Collections.Generic.Dictionary<string, RelationshipIndexEntry[]>
            macrolanguagesByMember,
        global::System.Collections.Generic.Dictionary<string, RelationshipIndexEntry[]>
            groupChildrenByParent,
        global::System.Collections.Generic.Dictionary<string, RelationshipIndexEntry[]>
            groupParentsByChild)
    {
        MacrolanguageMembersByParent = macrolanguageMembersByParent;
        MacrolanguagesByMember = macrolanguagesByMember;
        GroupChildrenByParent = groupChildrenByParent;
        GroupParentsByChild = groupParentsByChild;
    }
}

internal static class RelationshipIndex
{
    internal static readonly RelationshipIndexData Data =
        RelationshipIndexBuilder.BuildIndex(LanguageDataTables.RelationshipRows);

    internal static RelationshipIndexEntry[] FindMacrolanguageMembers(
        string? parentCode,
        bool includeRetired)
    {
        if (parentCode == null || !Data.MacrolanguageMembersByParent.ContainsKey(parentCode))
        {
            return new RelationshipIndexEntry[0];
        }

        return RelationshipIndexBuilder.CopyRelationshipEntriesByStatus(
            Data.MacrolanguageMembersByParent[parentCode],
            includeRetired);
    }

    internal static RelationshipIndexEntry[] FindMacrolanguagesForMember(
        string? memberCode,
        bool includeRetired)
    {
        if (memberCode == null || !Data.MacrolanguagesByMember.ContainsKey(memberCode))
        {
            return new RelationshipIndexEntry[0];
        }

        return RelationshipIndexBuilder.CopyRelationshipEntriesByStatus(
            Data.MacrolanguagesByMember[memberCode],
            includeRetired);
    }

    internal static RelationshipIndexEntry[] FindGroupChildren(string? parentCode)
    {
        if (parentCode == null || !Data.GroupChildrenByParent.ContainsKey(parentCode))
        {
            return new RelationshipIndexEntry[0];
        }

        return RelationshipIndexBuilder.CopyRelationshipEntries(
            Data.GroupChildrenByParent[parentCode]);
    }

    internal static RelationshipIndexEntry[] FindGroupParents(string? childCode)
    {
        if (childCode == null || !Data.GroupParentsByChild.ContainsKey(childCode))
        {
            return new RelationshipIndexEntry[0];
        }

        return RelationshipIndexBuilder.CopyRelationshipEntries(
            Data.GroupParentsByChild[childCode]);
    }
}

internal static class RelationshipIndexBuilder
{
    internal static RelationshipIndexData BuildIndex(RelationshipRow[] source)
    {
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<RelationshipIndexEntry>> macroChildren =
                CreateRelationshipLists();
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<RelationshipIndexEntry>> macroParents =
                CreateRelationshipLists();
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<RelationshipIndexEntry>> groupChildren =
                CreateRelationshipLists();
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<RelationshipIndexEntry>> groupParents =
                CreateRelationshipLists();
        int index;
        for (index = 0; index < source.Length; index++)
        {
            RelationshipRow row = source[index];
            RelationshipIndexEntry entry = new RelationshipIndexEntry(
                row.ParentCode,
                row.ChildCode,
                row.Kind,
                row.Status,
                row.Source);
            if (row.Kind == LanguageRelationshipKind.MacrolanguageMember)
            {
                AddRelationshipEntry(macroChildren, row.ParentCode, entry);
                AddRelationshipEntry(macroParents, row.ChildCode, entry);
            }
            else if (row.Kind == LanguageRelationshipKind.Set5ParentChild)
            {
                AddRelationshipEntry(groupChildren, row.ParentCode, entry);
                AddRelationshipEntry(groupParents, row.ChildCode, entry);
            }
        }

        global::System.Collections.Generic.Dictionary<string, RelationshipIndexEntry[]>
            frozenGroupChildren = FreezeRelationshipEntries(groupChildren);
        ValidateGroupCycles(frozenGroupChildren);
        return new RelationshipIndexData(
            FreezeRelationshipEntries(macroChildren),
            FreezeRelationshipEntries(macroParents),
            frozenGroupChildren,
            FreezeRelationshipEntries(groupParents));
    }

    internal static global::System.Collections.Generic.Dictionary<
        string,
        global::System.Collections.Generic.List<RelationshipIndexEntry>> CreateRelationshipLists()
    {
        return new global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<RelationshipIndexEntry>>(
                global::System.StringComparer.Ordinal);
    }

    internal static void AddRelationshipEntry(
        global::System.Collections.Generic.Dictionary<
            string,
            global::System.Collections.Generic.List<RelationshipIndexEntry>> destination,
        string key,
        RelationshipIndexEntry entry)
    {
        if (!destination.ContainsKey(key))
        {
            destination.Add(
                key,
                new global::System.Collections.Generic.List<RelationshipIndexEntry>());
        }

        destination[key].Add(entry);
    }

    internal static global::System.Collections.Generic.Dictionary<string, RelationshipIndexEntry[]>
        FreezeRelationshipEntries(
            global::System.Collections.Generic.Dictionary<
                string,
                global::System.Collections.Generic.List<RelationshipIndexEntry>> source)
    {
        global::System.Collections.Generic.Dictionary<string, RelationshipIndexEntry[]> destination =
            new global::System.Collections.Generic.Dictionary<string, RelationshipIndexEntry[]>(
                global::System.StringComparer.Ordinal);
        foreach (
            global::System.Collections.Generic.KeyValuePair<
                string,
                global::System.Collections.Generic.List<RelationshipIndexEntry>> pair in source)
        {
            pair.Value.Sort(
                (left, right) =>
                {
                    int comparison = string.CompareOrdinal(left.ParentCode, right.ParentCode);
                    if (comparison != 0)
                    {
                        return comparison;
                    }

                    comparison = string.CompareOrdinal(left.ChildCode, right.ChildCode);
                    if (comparison != 0)
                    {
                        return comparison;
                    }

                    comparison = ((int)left.Status).CompareTo((int)right.Status);
                    if (comparison != 0)
                    {
                        return comparison;
                    }

                    return ((int)left.Source).CompareTo((int)right.Source);
                });
            destination.Add(pair.Key, pair.Value.ToArray());
        }

        return destination;
    }

    internal static void ValidateGroupCycles(
        global::System.Collections.Generic.Dictionary<string, RelationshipIndexEntry[]> childrenByParent)
    {
        foreach (
            global::System.Collections.Generic.KeyValuePair<string, RelationshipIndexEntry[]> pair
                in childrenByParent)
        {
            int index;
            for (index = 0; index < pair.Value.Length; index++)
            {
                global::System.Collections.Generic.HashSet<string> visited =
                    new global::System.Collections.Generic.HashSet<string>(
                        global::System.StringComparer.Ordinal);
                if (HasGroupPath(
                    pair.Value[index].ChildCode,
                    pair.Key,
                    childrenByParent,
                    visited))
                {
                    throw new global::System.InvalidOperationException(
                        "Set 5 hierarchy contains a cycle involving: " + pair.Key);
                }
            }
        }
    }

    internal static bool HasGroupPath(
        string currentCode,
        string targetCode,
        global::System.Collections.Generic.Dictionary<string, RelationshipIndexEntry[]> childrenByParent,
        global::System.Collections.Generic.HashSet<string> visited)
    {
        if (currentCode == targetCode)
        {
            return true;
        }

        if (visited.Contains(currentCode))
        {
            return false;
        }

        visited.Add(currentCode);
        if (!childrenByParent.ContainsKey(currentCode))
        {
            return false;
        }

        RelationshipIndexEntry[] children = childrenByParent[currentCode];
        int index;
        for (index = 0; index < children.Length; index++)
        {
            if (HasGroupPath(children[index].ChildCode, targetCode, childrenByParent, visited))
            {
                return true;
            }
        }

        return false;
    }

    internal static RelationshipIndexEntry[] CopyRelationshipEntriesByStatus(
        RelationshipIndexEntry[] source,
        bool includeRetired)
    {
        global::System.Collections.Generic.List<RelationshipIndexEntry> destination =
            new global::System.Collections.Generic.List<RelationshipIndexEntry>();
        int index;
        for (index = 0; index < source.Length; index++)
        {
            if (includeRetired || source[index].Status == ReconciledRelationshipStatus.Active)
            {
                destination.Add(CopyRelationshipEntry(source[index]));
            }
        }

        return destination.ToArray();
    }

    internal static RelationshipIndexEntry[] CopyRelationshipEntries(RelationshipIndexEntry[] source)
    {
        RelationshipIndexEntry[] destination = new RelationshipIndexEntry[source.Length];
        int index;
        for (index = 0; index < source.Length; index++)
        {
            destination[index] = CopyRelationshipEntry(source[index]);
        }

        return destination;
    }

    internal static RelationshipIndexEntry CopyRelationshipEntry(RelationshipIndexEntry source)
    {
        return new RelationshipIndexEntry(
            source.ParentCode,
            source.ChildCode,
            source.Kind,
            source.Status,
            source.Source);
    }
}
