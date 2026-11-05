using System.Collections.Generic;

namespace pptx;

internal static class PptxPackageCleaner
{
    internal static PptxPackageCleanupResult Cleanup(PptxPackage package)
    {
        PptxPackageCleanupResult result = new PptxPackageCleanupResult();
        if (package == null)
        {
            result.Diagnostics.AddError("PKGC001", "Package is required.", "", "", "");
            return result;
        }

        PptxPackageRepair.RepairRelationshipCollections(package, result.Diagnostics);
        PptxPackageRepair.RepairRelationshipIdentifiers(package, result.Diagnostics);
        RemoveUnusedXmlRelationships(package, result);
        RemoveOrphanRelationships(package, result);
        DeduplicateMediaParts(package, result);
        RemoveUnusedParts(package, result);
        RemoveDanglingContentTypeOverrides(package, result);
        SortPackageContent(package, result);
        CompactRawEntries(package, result);
        return result;
    }

    internal static void RemoveUnusedXmlRelationships(PptxPackage package, PptxPackageCleanupResult result)
    {
        int before = CountRelationships(package);
        PptxPackageRepair.RemoveUnusedRelationships(package, result.Diagnostics);
        int after = CountRelationships(package);
        int removed = before - after;
        if (removed > 0)
        {
            result.RelationshipsRemoved = result.RelationshipsRemoved + removed;
        }
    }

    internal static void RemoveOrphanRelationships(PptxPackage package, PptxPackageCleanupResult result)
    {
        RemoveOrphanRelationshipsFromCollection(package, package.RootRelationships, result);
        for (int index = 0; index < package.Parts.Count; index++)
        {
            RemoveOrphanRelationshipsFromCollection(package, package.Parts[index].Relationships, result);
        }
    }

    internal static void RemoveOrphanRelationshipsFromCollection(PptxPackage package, PptxRelationshipCollection relationships, PptxPackageCleanupResult result)
    {
        for (int index = relationships.Relationships.Count - 1; index >= 0; index--)
        {
            PptxRelationship relationship = relationships.Relationships[index];
            if (relationship.TargetMode == PptxRelationshipTargetMode.External)
            {
                continue;
            }

            if (package.HasPart(relationship.TargetPartName))
            {
                continue;
            }

            PptxPackageRepair.RemoveRelationshipIdReferences(package, relationships.SourcePartName, relationship.Id);
            relationships.Relationships.RemoveAt(index);
            package.MarkRelationshipsDirty(relationships.SourcePartName);
            result.RelationshipsRemoved = result.RelationshipsRemoved + 1;
            result.Diagnostics.AddInfo("PKGC010", "Removed orphan relationship.", relationships.SourcePartName, relationship.Id, "");
        }
    }

    internal static void DeduplicateMediaParts(PptxPackage package, PptxPackageCleanupResult result)
    {
        List<string> reachable = CollectReachableParts(package);
        for (int index = 0; index < package.Parts.Count; index++)
        {
            PptxPart part = package.Parts[index];
            if (!IsMediaPart(part))
            {
                continue;
            }

            PptxPart? canonical = FindCanonicalMediaPart(package, part, reachable);
            if (canonical == null)
            {
                continue;
            }

            if (canonical.PartName == part.PartName)
            {
                continue;
            }

            int retargeted = RetargetInternalRelationships(package, part.PartName, canonical.PartName);
            if (retargeted > 0)
            {
                result.MediaRelationshipsRetargeted = result.MediaRelationshipsRetargeted + retargeted;
            }

            result.DuplicateMediaPartsRemoved = result.DuplicateMediaPartsRemoved + 1;
            result.Diagnostics.AddInfo("PKGC020", "Retargeted duplicate media part.", part.PartName, "", canonical.PartName);
        }
    }

    internal static PptxPart? FindCanonicalMediaPart(PptxPackage package, PptxPart duplicate, List<string> reachable)
    {
        PptxPart? canonical = null;
        for (int index = 0; index < package.Parts.Count; index++)
        {
            PptxPart candidate = package.Parts[index];
            if (!IsDuplicateMediaPart(candidate, duplicate))
            {
                continue;
            }

            if (canonical == null)
            {
                canonical = candidate;
                continue;
            }

            bool canonicalReachable = ContainsPartName(reachable, canonical.PartName);
            bool candidateReachable = ContainsPartName(reachable, candidate.PartName);

            if (!canonicalReachable && candidateReachable)
            {
                canonical = candidate;
                continue;
            }

            if (canonicalReachable == candidateReachable && string.Compare(candidate.PartName, canonical.PartName, System.StringComparison.Ordinal) < 0)
            {
                canonical = candidate;
            }
        }

        return canonical;
    }

    internal static bool IsDuplicateMediaPart(PptxPart left, PptxPart right)
    {
        if (!IsMediaPart(left) || !IsMediaPart(right))
        {
            return false;
        }

        if (left.ContentType != right.ContentType)
        {
            return false;
        }

        return BytesEqual(left.GetBytes(), right.GetBytes());
    }

    internal static int RetargetInternalRelationships(PptxPackage package, string oldPartName, string newPartName)
    {
        int count = 0;
        count = count + RetargetInternalRelationshipsInCollection(package, package.RootRelationships, oldPartName, newPartName);
        for (int index = 0; index < package.Parts.Count; index++)
        {
            count = count + RetargetInternalRelationshipsInCollection(package, package.Parts[index].Relationships, oldPartName, newPartName);
        }

        return count;
    }

    internal static int RetargetInternalRelationshipsInCollection(PptxPackage package, PptxRelationshipCollection relationships, string oldPartName, string newPartName)
    {
        int count = 0;
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = relationships.Relationships[index];
            if (relationship.TargetMode != PptxRelationshipTargetMode.Internal)
            {
                continue;
            }

            if (relationship.TargetPartName != oldPartName)
            {
                continue;
            }

            relationship.Target = "";
            relationship.TargetPartName = newPartName;
            package.MarkRelationshipsDirty(relationships.SourcePartName);
            count = count + 1;
        }

        return count;
    }

    internal static void RemoveUnusedParts(PptxPackage package, PptxPackageCleanupResult result)
    {
        List<string> reachable = CollectReachableParts(package);
        for (int index = package.Parts.Count - 1; index >= 0; index--)
        {
            PptxPart part = package.Parts[index];
            if (ContainsPartName(reachable, part.PartName))
            {
                continue;
            }

            string partName = part.PartName;
            package.RemovePart(partName);
            result.UnusedPartsRemoved = result.UnusedPartsRemoved + 1;
            result.Diagnostics.AddInfo("PKGC030", "Removed unused package part.", partName, "", "");
        }
    }

    internal static List<string> CollectReachableParts(PptxPackage package)
    {
        List<string> reachable = new List<string>();
        AddReachableRelationships(package, package.RootRelationships, reachable);

        int position = 0;
        while (position < reachable.Count)
        {
            string partName = reachable[position];
            if (package.HasPart(partName))
            {
                AddReachableRelationships(package, package.GetPart(partName).Relationships, reachable);
            }

            position = position + 1;
        }

        return reachable;
    }

    internal static void AddReachableRelationships(PptxPackage package, PptxRelationshipCollection relationships, List<string> reachable)
    {
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = relationships.Relationships[index];
            if (relationship.TargetMode == PptxRelationshipTargetMode.External)
            {
                continue;
            }

            if (!package.HasPart(relationship.TargetPartName))
            {
                continue;
            }

            AddPartName(reachable, relationship.TargetPartName);
        }
    }

    internal static void RemoveDanglingContentTypeOverrides(PptxPackage package, PptxPackageCleanupResult result)
    {
        for (int index = package.ContentTypes.Overrides.Count - 1; index >= 0; index--)
        {
            PptxContentTypeOverride item = package.ContentTypes.Overrides[index];
            bool remove = !package.HasPart(item.PartName);
            string diagnosticCode = "PKGC040";
            string message = "Removed dangling content type override.";

            if (!remove && HasEarlierContentTypeOverride(package, index, item.PartName))
            {
                remove = true;
                diagnosticCode = "PKGC041";
                message = "Removed duplicate content type override.";
            }

            if (!remove)
            {
                string extension = PptxContentTypesParser.GetPartNameExtension(item.PartName);
                string defaultContentType = package.ContentTypes.GetDefaultContentType(extension);
                if (defaultContentType.Length > 0 && defaultContentType == item.ContentType)
                {
                    remove = true;
                    diagnosticCode = "PKGC042";
                    message = "Removed redundant content type override.";
                }
            }

            if (!remove)
            {
                continue;
            }

            package.ContentTypes.Overrides.RemoveAt(index);
            package.MarkDirty();
            result.ContentTypeOverridesRemoved = result.ContentTypeOverridesRemoved + 1;
            result.Diagnostics.AddInfo(diagnosticCode, message, item.PartName, "", "");
        }
    }

    internal static bool HasEarlierContentTypeOverride(PptxPackage package, int currentIndex, string partName)
    {
        for (int index = 0; index < currentIndex; index++)
        {
            if (package.ContentTypes.Overrides[index].PartName == partName)
            {
                return true;
            }
        }

        return false;
    }

    internal static void SortPackageContent(PptxPackage package, PptxPackageCleanupResult result)
    {
        bool changed = SortParts(package.Parts);
        changed = SortRelationshipCollection(package.RootRelationships) || changed;

        for (int index = 0; index < package.Parts.Count; index++)
        {
            if (SortRelationshipCollection(package.Parts[index].Relationships))
            {
                package.Parts[index].MarkDirty();
                changed = true;
            }
        }

        if (changed)
        {
            package.MarkDirty();
            result.Diagnostics.AddInfo("PKGC060", "Normalized deterministic package ordering.", "", "", "");
        }
    }

    internal static bool SortParts(List<PptxPart> parts)
    {
        bool changed = false;

        for (int index = 1; index < parts.Count; index++)
        {
            PptxPart current = parts[index];
            int position = index - 1;

            while (position >= 0 && string.Compare(parts[position].PartName, current.PartName, System.StringComparison.Ordinal) > 0)
            {
                parts[position + 1] = parts[position];
                position = position - 1;
                changed = true;
            }

            parts[position + 1] = current;
        }

        return changed;
    }

    internal static bool SortRelationshipCollection(PptxRelationshipCollection relationships)
    {
        bool changed = false;
        for (int index = 1; index < relationships.Relationships.Count; index++)
        {
            if (PptxRelationshipsWriter.CompareRelationshipIds(relationships.Relationships[index - 1].Id, relationships.Relationships[index].Id) > 0)
            {
                changed = true;
                break;
            }
        }

        if (changed)
        {
            PptxRelationshipsWriter.SortRelationships(relationships.Relationships);
        }

        return changed;
    }

    internal static void CompactRawEntries(PptxPackage package, PptxPackageCleanupResult result)
    {
        List<PptxZipEntry> entries = PptxZipWriter.CreateOrderedEntries(package.BuildZipEntries());
        if (!RawEntriesEqual(package.RawEntries, entries))
        {
            package.RawEntries = new List<PptxZipEntry>();
            for (int index = 0; index < entries.Count; index++)
            {
                package.RawEntries.Add(entries[index]);
            }

            package.MarkDirty();
            result.RawEntriesCompacted = true;
            result.Diagnostics.AddInfo("PKGC050", "Compacted package entries deterministically.", "", "", "");
        }
    }

    internal static int CountRelationships(PptxPackage package)
    {
        int count = package.RootRelationships.Count();
        for (int index = 0; index < package.Parts.Count; index++)
        {
            count = count + package.Parts[index].Relationships.Count();
        }

        return count;
    }

    internal static bool RawEntriesEqual(List<PptxZipEntry> left, List<PptxZipEntry> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (int index = 0; index < left.Count; index++)
        {
            if (left[index].Name != right[index].Name)
            {
                return false;
            }

            if (!BytesEqual(left[index].Bytes, right[index].Bytes))
            {
                return false;
            }
        }

        return true;
    }

    internal static void AddPartName(List<string> values, string partName)
    {
        if (!ContainsPartName(values, partName))
        {
            values.Add(partName);
        }
    }

    internal static bool ContainsPartName(List<string> values, string partName)
    {
        string normalized = PptxUriCore.NormalizePartName(partName);
        for (int index = 0; index < values.Count; index++)
        {
            if (values[index] == normalized)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsMediaPart(PptxPart part)
    {
        if (!part.PartName.StartsWith("/ppt/media/", System.StringComparison.Ordinal))
        {
            return false;
        }

        if (part.ContentType.StartsWith("image/", System.StringComparison.Ordinal))
        {
            return true;
        }

        if (part.ContentType.StartsWith("audio/", System.StringComparison.Ordinal))
        {
            return true;
        }

        if (part.ContentType.StartsWith("video/", System.StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    internal static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        if (left.Length != right.Length)
        {
            return false;
        }

        for (int index = 0; index < left.Length; index++)
        {
            if (left[index] != right[index])
            {
                return false;
            }
        }

        return true;
    }
}
