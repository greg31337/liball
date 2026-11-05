using System.Collections.Generic;

namespace pptx;

internal static class PptxPackageNormalizer
{
    internal static PptxResult Normalize(PptxPackage package)
    {
        PptxResult result = new PptxResult();
        if (package == null)
        {
            result.AddError("PKGN001", "Package is required.", "", "", "");
            return result;
        }

        int overrideCount = package.ContentTypes.Overrides.Count;
        package.EnsurePartContentTypes();
        bool changed = package.ContentTypes.Overrides.Count != overrideCount;
        changed = PptxPackageCleaner.SortParts(package.Parts) || changed;
        changed = NormalizeRelationships(package) || changed;
        changed = NormalizeContentTypes(package) || changed;
        changed = NormalizeRawEntries(package) || changed;

        if (changed)
        {
            package.MarkDirty();
            result.AddInfo("PKGN010", "Normalized deterministic package ordering and retained entries.", "", "", "");
        }
        else
        {
            result.AddInfo("PKGN011", "Package ordering and retained entries were already normalized.", "", "", "");
        }

        return result;
    }

    internal static bool NormalizeRelationships(PptxPackage package)
    {
        bool changed = PptxPackageCleaner.SortRelationshipCollection(package.RootRelationships);
        for (int index = 0; index < package.Parts.Count; index++)
        {
            PptxPart part = package.Parts[index];
            if (PptxPackageCleaner.SortRelationshipCollection(part.Relationships))
            {
                part.MarkDirty();
                changed = true;
            }
        }

        return changed;
    }

    internal static bool NormalizeContentTypes(PptxPackage package)
    {
        bool defaultsChanged = ContentTypeDefaultsNeedSorting(package.ContentTypes.Defaults);
        bool overridesChanged = ContentTypeOverridesNeedSorting(package.ContentTypes.Overrides);
        if (defaultsChanged)
        {
            PptxContentTypesWriter.SortDefaults(package.ContentTypes.Defaults);
        }

        if (overridesChanged)
        {
            PptxContentTypesWriter.SortOverrides(package.ContentTypes.Overrides);
        }

        return defaultsChanged || overridesChanged;
    }

    internal static bool ContentTypeDefaultsNeedSorting(List<PptxContentTypeDefault> values)
    {
        for (int index = 1; index < values.Count; index++)
        {
            if (PptxContentTypesWriter.CompareText(values[index - 1].Extension, values[index].Extension) > 0)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool ContentTypeOverridesNeedSorting(List<PptxContentTypeOverride> values)
    {
        for (int index = 1; index < values.Count; index++)
        {
            if (PptxContentTypesWriter.CompareText(values[index - 1].PartName, values[index].PartName) > 0)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool NormalizeRawEntries(PptxPackage package)
    {
        List<PptxZipEntry> entries = PptxZipWriter.CreateOrderedEntries(package.BuildZipEntries());
        if (PptxPackageCleaner.RawEntriesEqual(package.RawEntries, entries))
        {
            return false;
        }

        package.RawEntries = entries;
        return true;
    }
}
