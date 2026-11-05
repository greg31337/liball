using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace pptx;

internal static class PptxPackageInspectionCore
{
    internal static PptxPackageManifest InspectManifest(PptxPackage package)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        PptxPackage detached = PptxDocument.ClonePackage(package);
        List<PptxZipEntry> zipEntries = detached.BuildZipEntries();
        PptxPackageManifest manifest = new PptxPackageManifest();

        for (int index = 0; index < zipEntries.Count; index++)
        {
            PptxZipEntry zipEntry = zipEntries[index];
            PptxPackageManifestEntry entry = new PptxPackageManifestEntry();
            entry.EntryName = zipEntry.Name;
            entry.PartName = zipEntry.PartName;
            entry.ContentType = ResolveContentType(detached, zipEntry);
            entry.ByteCount = zipEntry.Bytes.Length;
            entry.Sha256 = ComputeSha256(zipEntry.Bytes);
            manifest.Entries.Add(entry);
            manifest.TotalBytes = manifest.TotalBytes + entry.ByteCount;
        }

        SortEntries(manifest.Entries);
        return manifest;
    }

    internal static PptxPackageManifestComparison CompareManifests(PptxPackage left, PptxPackage right)
    {
        if (left == null)
        {
            throw new System.ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new System.ArgumentNullException("right");
        }

        PptxPackageManifestComparison comparison = new PptxPackageManifestComparison();
        comparison.Left = InspectManifest(left);
        comparison.Right = InspectManifest(right);

        for (int index = 0; index < comparison.Left.Entries.Count; index++)
        {
            PptxPackageManifestEntry leftEntry = comparison.Left.Entries[index];
            PptxPackageManifestEntry? rightEntry = FindEntry(comparison.Right, leftEntry.EntryName);
            if (rightEntry == null)
            {
                comparison.RemovedEntryNames.Add(leftEntry.EntryName);
            }
            else if (!EntriesEqual(leftEntry, rightEntry))
            {
                comparison.ChangedEntryNames.Add(leftEntry.EntryName);
            }
        }

        for (int index = 0; index < comparison.Right.Entries.Count; index++)
        {
            PptxPackageManifestEntry rightEntry = comparison.Right.Entries[index];
            if (FindEntry(comparison.Left, rightEntry.EntryName) == null)
            {
                comparison.AddedEntryNames.Add(rightEntry.EntryName);
            }
        }

        comparison.Equal = comparison.AddedEntryNames.Count == 0 && comparison.RemovedEntryNames.Count == 0 && comparison.ChangedEntryNames.Count == 0;
        return comparison;
    }

    internal static PptxEditableModelSummary SummarizeEditableModel(PptxPackage package)
    {
        if (package == null)
        {
            throw new System.ArgumentNullException("package");
        }

        PptxPresentation presentation = PptxPresentation.Load(PptxDocument.ClonePackage(package));
        PptxEditableModelSummary summary = new PptxEditableModelSummary();
        summary.SlideCount = presentation.SlideCount();
        summary.SlideWidth = presentation.SlideWidth;
        summary.SlideHeight = presentation.SlideHeight;
        summary.SlideSizeType = presentation.SlideSizeType;
        summary.SlideMasterCount = presentation.SlideMasterCount();
        summary.SlideLayoutCount = presentation.SlideLayoutCount();
        summary.ThemeCount = presentation.ThemeCount();
        summary.SectionCount = presentation.Sections.Count;
        summary.CustomShowCount = presentation.CustomShows.Count;
        summary.MediaCount = presentation.GetMedia().Count;
        summary.EmbeddedObjectCount = presentation.GetEmbeddedObjects().Count;
        summary.AnimationCount = presentation.GetAnimations().Count;
        summary.TransitionCount = presentation.GetTransitions().Count;
        summary.HasNotesMaster = presentation.NotesMaster != null;
        summary.HasHandoutMaster = presentation.HandoutMaster != null;
        summary.HasMacros = presentation.HasMacros();

        for (int index = 0; index < presentation.SlideCount(); index++)
        {
            PptxSlide slide = presentation.GetSlide(index);
            summary.ShapeCount = summary.ShapeCount + slide.GetShapeTree().GetAllShapes().Count;
            summary.PlaceholderCount = summary.PlaceholderCount + slide.PlaceholderCount();
            summary.ImageCount = summary.ImageCount + slide.ExtractImages().Count;
            summary.TableCount = summary.TableCount + slide.GetTables().Count;
            summary.ChartCount = summary.ChartCount + slide.GetCharts().Count;
            summary.DiagramCount = summary.DiagramCount + slide.GetDiagrams().Count;
            summary.CommentCount = summary.CommentCount + slide.GetComments().Count;
            summary.ModernCommentCount = summary.ModernCommentCount + slide.GetModernComments().Count;
            if (slide.GetNotes() != null)
            {
                summary.NotesSlideCount = summary.NotesSlideCount + 1;
            }
        }

        return summary;
    }

    internal static PptxEditableModelComparison CompareEditableModels(PptxPackage left, PptxPackage right)
    {
        if (left == null)
        {
            throw new System.ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new System.ArgumentNullException("right");
        }

        PptxEditableModelComparison comparison = new PptxEditableModelComparison();
        comparison.Left = SummarizeEditableModel(left);
        comparison.Right = SummarizeEditableModel(right);
        CompareSummaryFields(comparison);
        comparison.Equal = comparison.Differences.Count == 0;
        return comparison;
    }

    internal static string ResolveContentType(PptxPackage package, PptxZipEntry entry)
    {
        if (entry.Name == PptxPartNames.ContentTypesEntry)
        {
            return PptxContentTypes.Xml;
        }

        if (entry.Name.EndsWith(".rels", System.StringComparison.Ordinal))
        {
            return PptxContentTypes.Relationships;
        }

        if (entry.PartName.Length > 0 && package.HasPart(entry.PartName))
        {
            return package.GetPart(entry.PartName).ContentType;
        }

        return package.ContentTypes.GetContentTypeForPartName(entry.PartName);
    }

    internal static string ComputeSha256(byte[] bytes)
    {
        byte[] hash;
        using (SHA256 algorithm = SHA256.Create())
        {
            hash = algorithm.ComputeHash(bytes);
        }

        StringBuilder builder = new StringBuilder(hash.Length * 2);
        for (int index = 0; index < hash.Length; index++)
        {
            builder.Append(hash[index].ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    internal static void SortEntries(List<PptxPackageManifestEntry> entries)
    {
        for (int index = 1; index < entries.Count; index++)
        {
            PptxPackageManifestEntry entry = entries[index];
            int targetIndex = index;
            while (targetIndex > 0 && string.CompareOrdinal(entries[targetIndex - 1].EntryName, entry.EntryName) > 0)
            {
                entries[targetIndex] = entries[targetIndex - 1];
                targetIndex = targetIndex - 1;
            }

            entries[targetIndex] = entry;
        }
    }

    internal static PptxPackageManifestEntry? FindEntry(PptxPackageManifest manifest, string entryName)
    {
        for (int index = 0; index < manifest.Entries.Count; index++)
        {
            if (manifest.Entries[index].EntryName == entryName)
            {
                return manifest.Entries[index];
            }
        }

        return null;
    }

    internal static bool EntriesEqual(PptxPackageManifestEntry left, PptxPackageManifestEntry right)
    {
        return left.PartName == right.PartName && left.ContentType == right.ContentType && left.ByteCount == right.ByteCount && left.Sha256 == right.Sha256;
    }

    internal static void CompareSummaryFields(PptxEditableModelComparison comparison)
    {
        PptxEditableModelSummary left = comparison.Left;
        PptxEditableModelSummary right = comparison.Right;
        CompareSummaryValue(comparison, "SlideCount", left.SlideCount.ToString(CultureInfo.InvariantCulture), right.SlideCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "SlideWidth", left.SlideWidth.ToString(CultureInfo.InvariantCulture), right.SlideWidth.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "SlideHeight", left.SlideHeight.ToString(CultureInfo.InvariantCulture), right.SlideHeight.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "SlideSizeType", left.SlideSizeType, right.SlideSizeType);
        CompareSummaryValue(comparison, "SlideMasterCount", left.SlideMasterCount.ToString(CultureInfo.InvariantCulture), right.SlideMasterCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "SlideLayoutCount", left.SlideLayoutCount.ToString(CultureInfo.InvariantCulture), right.SlideLayoutCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "ThemeCount", left.ThemeCount.ToString(CultureInfo.InvariantCulture), right.ThemeCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "SectionCount", left.SectionCount.ToString(CultureInfo.InvariantCulture), right.SectionCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "CustomShowCount", left.CustomShowCount.ToString(CultureInfo.InvariantCulture), right.CustomShowCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "ShapeCount", left.ShapeCount.ToString(CultureInfo.InvariantCulture), right.ShapeCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "PlaceholderCount", left.PlaceholderCount.ToString(CultureInfo.InvariantCulture), right.PlaceholderCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "ImageCount", left.ImageCount.ToString(CultureInfo.InvariantCulture), right.ImageCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "TableCount", left.TableCount.ToString(CultureInfo.InvariantCulture), right.TableCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "ChartCount", left.ChartCount.ToString(CultureInfo.InvariantCulture), right.ChartCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "DiagramCount", left.DiagramCount.ToString(CultureInfo.InvariantCulture), right.DiagramCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "NotesSlideCount", left.NotesSlideCount.ToString(CultureInfo.InvariantCulture), right.NotesSlideCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "CommentCount", left.CommentCount.ToString(CultureInfo.InvariantCulture), right.CommentCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "ModernCommentCount", left.ModernCommentCount.ToString(CultureInfo.InvariantCulture), right.ModernCommentCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "MediaCount", left.MediaCount.ToString(CultureInfo.InvariantCulture), right.MediaCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "EmbeddedObjectCount", left.EmbeddedObjectCount.ToString(CultureInfo.InvariantCulture), right.EmbeddedObjectCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "AnimationCount", left.AnimationCount.ToString(CultureInfo.InvariantCulture), right.AnimationCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "TransitionCount", left.TransitionCount.ToString(CultureInfo.InvariantCulture), right.TransitionCount.ToString(CultureInfo.InvariantCulture));
        CompareSummaryValue(comparison, "HasNotesMaster", left.HasNotesMaster.ToString(), right.HasNotesMaster.ToString());
        CompareSummaryValue(comparison, "HasHandoutMaster", left.HasHandoutMaster.ToString(), right.HasHandoutMaster.ToString());
        CompareSummaryValue(comparison, "HasMacros", left.HasMacros.ToString(), right.HasMacros.ToString());
    }

    internal static void CompareSummaryValue(PptxEditableModelComparison comparison, string fieldName, string leftValue, string rightValue)
    {
        if (leftValue != rightValue)
        {
            PptxEditableModelDifference difference = new PptxEditableModelDifference();
            difference.FieldName = fieldName;
            difference.LeftValue = leftValue;
            difference.RightValue = rightValue;
            comparison.Differences.Add(difference);
        }
    }
}
