namespace languagecodes;

internal static class LanguageRelationshipEngine
{
    internal static LanguageCodeEntry? GetMacrolanguageForMember(
        string? memberCode,
        bool includeRetiredRelationships)
    {
        return GetMacrolanguageForMemberEntries(
            memberCode,
            includeRetiredRelationships,
            RelationshipIndex.Data,
            LanguageCatalogEngine.Data);
    }

    internal static LanguageCodeEntry[] GetMacrolanguageMembers(
        string? macrolanguageCode,
        bool includeRetiredRelationships)
    {
        return GetMacrolanguageMemberEntries(
            macrolanguageCode,
            includeRetiredRelationships,
            RelationshipIndex.Data,
            LanguageCatalogEngine.Data);
    }

    internal static LanguageCodeEntry[] GetGroupParents(
        string? groupCode,
        bool transitive)
    {
        return GetGroupParentEntries(
            groupCode,
            transitive,
            RelationshipIndex.Data,
            LanguageCatalogEngine.Data);
    }

    internal static LanguageCodeEntry[] GetGroupChildren(
        string? groupCode,
        bool transitive)
    {
        return GetGroupChildEntries(
            groupCode,
            transitive,
            RelationshipIndex.Data,
            LanguageCatalogEngine.Data);
    }

    internal static LanguageCodeEntry? GetMacrolanguageForMemberEntries(
        string? memberCode,
        bool includeRetiredRelationships,
        RelationshipIndexData relationships,
        LanguageCatalogData catalog)
    {
        if (memberCode == null ||
            !relationships.MacrolanguagesByMember.ContainsKey(memberCode))
        {
            return null;
        }

        RelationshipIndexEntry[] parents =
            RelationshipIndexBuilder.CopyRelationshipEntriesByStatus(
                relationships.MacrolanguagesByMember[memberCode],
                includeRetiredRelationships);
        LanguageCodeEntry? active = FindMacrolanguageParentByStatus(
            parents,
            ReconciledRelationshipStatus.Active,
            catalog);
        if (active != null)
        {
            return PublicResultFactory.CopyLanguageCodeEntry(active);
        }

        if (includeRetiredRelationships)
        {
            LanguageCodeEntry? retired = FindMacrolanguageParentByStatus(
                parents,
                ReconciledRelationshipStatus.Retired,
                catalog);
            if (retired != null)
            {
                return PublicResultFactory.CopyLanguageCodeEntry(retired);
            }
        }

        return null;
    }

    internal static LanguageCodeEntry[] GetMacrolanguageMemberEntries(
        string? macrolanguageCode,
        bool includeRetiredRelationships,
        RelationshipIndexData relationships,
        LanguageCatalogData catalog)
    {
        if (macrolanguageCode == null ||
            !relationships.MacrolanguageMembersByParent.ContainsKey(macrolanguageCode))
        {
            return new LanguageCodeEntry[0];
        }

        RelationshipIndexEntry[] members =
            RelationshipIndexBuilder.CopyRelationshipEntriesByStatus(
                relationships.MacrolanguageMembersByParent[macrolanguageCode],
                includeRetiredRelationships);
        global::System.Collections.Generic.HashSet<string> addedCodes =
            new global::System.Collections.Generic.HashSet<string>(
                global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.List<LanguageCodeEntry> entries =
            new global::System.Collections.Generic.List<LanguageCodeEntry>();
        int index;
        for (index = 0; index < members.Length; index++)
        {
            RelationshipIndexEntry relationship = members[index];
            bool retiredRelationship =
                relationship.Status == ReconciledRelationshipStatus.Retired;
            LanguageCodeEntry? entry = FindDirectSet3Entry(
                relationship.ChildCode,
                retiredRelationship,
                catalog);
            if (entry != null)
            {
                string stableCode = LanguageCatalogBuilder.FindStablePublicCode(entry);
                if (!addedCodes.Contains(stableCode))
                {
                    addedCodes.Add(stableCode);
                    entries.Add(entry);
                }
            }
        }

        entries.Sort(CompareRelationshipEntries);
        return PublicResultFactory.CopyLanguageCodeEntries(entries.ToArray());
    }

    internal static LanguageCodeEntry[] GetGroupParentEntries(
        string? groupCode,
        bool transitive,
        RelationshipIndexData relationships,
        LanguageCatalogData catalog)
    {
        return TraverseGroupEntries(
            groupCode,
            transitive,
            true,
            relationships.GroupParentsByChild,
            catalog);
    }

    internal static LanguageCodeEntry[] GetGroupChildEntries(
        string? groupCode,
        bool transitive,
        RelationshipIndexData relationships,
        LanguageCatalogData catalog)
    {
        return TraverseGroupEntries(
            groupCode,
            transitive,
            false,
            relationships.GroupChildrenByParent,
            catalog);
    }

    internal static LanguageCodeEntry[] TraverseGroupEntries(
        string? groupCode,
        bool transitive,
        bool traverseParents,
        global::System.Collections.Generic.Dictionary<string, RelationshipIndexEntry[]>
            relationshipsByCode,
        LanguageCatalogData catalog)
    {
        if (groupCode == null || groupCode.Length == 0)
        {
            return new LanguageCodeEntry[0];
        }

        global::System.Collections.Generic.HashSet<string> visited =
            new global::System.Collections.Generic.HashSet<string>(
                global::System.StringComparer.Ordinal);
        global::System.Collections.Generic.Queue<string> pending =
            new global::System.Collections.Generic.Queue<string>();
        global::System.Collections.Generic.List<LanguageCodeEntry> entries =
            new global::System.Collections.Generic.List<LanguageCodeEntry>();
        visited.Add(groupCode);
        pending.Enqueue(groupCode);
        while (pending.Count > 0)
        {
            string currentCode = pending.Dequeue();
            if (relationshipsByCode.ContainsKey(currentCode))
            {
                RelationshipIndexEntry[] direct = relationshipsByCode[currentCode];
                int index;
                for (index = 0; index < direct.Length; index++)
                {
                    string relatedCode = traverseParents
                        ? direct[index].ParentCode
                        : direct[index].ChildCode;
                    if (!visited.Contains(relatedCode))
                    {
                        visited.Add(relatedCode);
                        LanguageCodeEntry? entry = FindDirectSet5Entry(relatedCode, catalog);
                        if (entry != null)
                        {
                            entries.Add(entry);
                        }

                        if (transitive)
                        {
                            pending.Enqueue(relatedCode);
                        }
                    }
                }
            }
        }

        entries.Sort(CompareRelationshipEntries);
        return PublicResultFactory.CopyLanguageCodeEntries(entries.ToArray());
    }

    internal static LanguageCodeEntry? FindMacrolanguageParentByStatus(
        RelationshipIndexEntry[] relationships,
        ReconciledRelationshipStatus status,
        LanguageCatalogData catalog)
    {
        LanguageCodeEntry? selected = null;
        int index;
        for (index = 0; index < relationships.Length; index++)
        {
            RelationshipIndexEntry relationship = relationships[index];
            if (relationship.Status == status)
            {
                LanguageCodeEntry? candidate = FindDirectSet3Entry(
                    relationship.ParentCode,
                    false,
                    catalog);
                if (candidate != null &&
                    (selected == null || CompareRelationshipEntries(candidate, selected) < 0))
                {
                    selected = candidate;
                }
            }
        }

        return selected;
    }

    internal static LanguageCodeEntry? FindDirectSet3Entry(
        string code,
        bool preferRetired,
        LanguageCatalogData catalog)
    {
        if (preferRetired && catalog.RetiredEntriesByCode.ContainsKey(code))
        {
            return PublicResultFactory.CopyLanguageCodeEntry(
                catalog.RetiredEntriesByCode[code]);
        }

        int index;
        for (index = 0; index < catalog.EntriesByEntityIndex.Length; index++)
        {
            LanguageCodeEntry entry = catalog.EntriesByEntityIndex[index];
            if (entry.Set3Code == code)
            {
                return PublicResultFactory.CopyLanguageCodeEntry(entry);
            }
        }

        if (catalog.RetiredEntriesByCode.ContainsKey(code))
        {
            return PublicResultFactory.CopyLanguageCodeEntry(
                catalog.RetiredEntriesByCode[code]);
        }

        return null;
    }

    internal static LanguageCodeEntry? FindDirectSet5Entry(
        string code,
        LanguageCatalogData catalog)
    {
        int index;
        for (index = 0; index < catalog.EntriesByEntityIndex.Length; index++)
        {
            LanguageCodeEntry entry = catalog.EntriesByEntityIndex[index];
            if (entry.Set5Code == code)
            {
                return PublicResultFactory.CopyLanguageCodeEntry(entry);
            }
        }

        return null;
    }

    internal static int CompareRelationshipEntries(
        LanguageCodeEntry left,
        LanguageCodeEntry right)
    {
        int comparison = string.CompareOrdinal(
            LanguageCatalogBuilder.FindStablePublicCode(left),
            LanguageCatalogBuilder.FindStablePublicCode(right));
        if (comparison != 0)
        {
            return comparison;
        }

        return string.CompareOrdinal(left.ReferenceName, right.ReferenceName);
    }
}
