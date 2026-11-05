using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxPackageInspectionTests
{
    [Test]
    public void ClonePackage_CreatesDetachedEquivalentPackage()
    {
        PptxPackageOperations operations = new PptxPackageOperations();
        PptxPackage source = PptxDocument.CreatePresentation().GetPackage();

        PptxPackage clone = operations.ClonePackage(source);
        PptxPackageManifestComparison initialComparison = operations.CompareManifests(source, clone);

        Assert.That(clone, Is.Not.SameAs(source));
        Assert.That(clone.Parts, Is.Not.SameAs(source.Parts));
        Assert.That(clone.Parts[0], Is.Not.SameAs(source.Parts[0]));
        Assert.That(clone.Parts[0].Bytes, Is.Not.SameAs(source.Parts[0].Bytes));
        Assert.That(clone.Parts[0].Relationships, Is.Not.SameAs(source.Parts[0].Relationships));
        Assert.That(initialComparison.Equal, Is.True);

        byte sourceByte = source.Parts[0].Bytes[0];
        clone.Parts[0].Bytes[0] = (byte)(clone.Parts[0].Bytes[0] + 1);

        Assert.That(source.Parts[0].Bytes[0], Is.EqualTo(sourceByte));
        Assert.That(operations.CompareManifests(source, clone).Equal, Is.False);
    }

    [Test]
    public void InspectManifest_ReportsSortedEffectiveEntriesAndFingerprints()
    {
        PptxPackageOperations operations = new PptxPackageOperations();
        PptxPackage package = PptxDocument.CreatePresentation().GetPackage();

        PptxPackageManifest manifest = operations.InspectManifest(package);

        Assert.That(manifest.Entries.Count, Is.GreaterThan(package.Parts.Count));
        Assert.That(FindEntry(manifest, PptxPartNames.ContentTypesEntry), Is.Not.Null);
        Assert.That(FindEntry(manifest, "_rels/.rels"), Is.Not.Null);
        PptxPackageManifestEntry? presentationEntry = FindEntry(manifest, "ppt/presentation.xml");
        Assert.That(presentationEntry, Is.Not.Null);
        Assert.That(presentationEntry!.PartName, Is.EqualTo(PptxPartNames.Presentation));
        Assert.That(presentationEntry.ContentType, Is.EqualTo(PptxContentTypes.Presentation));
        Assert.That(presentationEntry.ByteCount, Is.GreaterThan(0));
        Assert.That(presentationEntry.Sha256, Has.Length.EqualTo(64));
        Assert.That(manifest.TotalBytes, Is.EqualTo(SumBytes(manifest)));
        Assert.That(IsSorted(manifest), Is.True);
    }

    [Test]
    public void CompareManifests_ClassifiesAddedAndChangedEntries()
    {
        PptxPackageOperations operations = new PptxPackageOperations();
        PptxPackage left = PptxDocument.CreatePresentation().GetPackage();
        PptxPackage right = operations.ClonePackage(left);

        operations.AddPart(right, "/custom/data.bin", "application/octet-stream", new byte[] { 1, 2, 3 });
        right.Parts[operations.IndexOfPart(right, PptxPartNames.FirstSlide)].Bytes[0] = 1;

        PptxPackageManifestComparison comparison = operations.CompareManifests(left, right);

        Assert.That(comparison.Equal, Is.False);
        Assert.That(comparison.AddedEntryNames, Does.Contain("custom/data.bin"));
        Assert.That(comparison.ChangedEntryNames, Does.Contain("ppt/slides/slide1.xml"));
        Assert.That(comparison.ChangedEntryNames, Does.Contain(PptxPartNames.ContentTypesEntry));
        Assert.That(comparison.RemovedEntryNames, Is.Empty);
    }

    [Test]
    public void CompareEditableModels_ReportsAggregateModelChanges()
    {
        PptxPackageOperations packageOperations = new PptxPackageOperations();
        PptxPresentationOperations presentationOperations = new PptxPresentationOperations();
        PptxSlideOperations slideOperations = new PptxSlideOperations();
        PptxDocument leftDocument = PptxDocument.CreatePresentation();
        PptxDocument rightDocument = leftDocument.CloneDocument();
        PptxPresentation rightPresentation = rightDocument.LoadPresentation();
        PptxSlide addedSlide = presentationOperations.AddSlide(rightPresentation);
        slideOperations.AddTextBox(addedSlide, "Compared text", new PptxBounds(0L, 0L, 100L, 100L));

        PptxEditableModelSummary leftSummary = packageOperations.SummarizeEditableModel(leftDocument.GetPackage());
        PptxEditableModelSummary rightSummary = packageOperations.SummarizeEditableModel(rightDocument.GetPackage());
        PptxEditableModelComparison comparison = packageOperations.CompareEditableModels(leftDocument.GetPackage(), rightDocument.GetPackage());

        Assert.That(leftSummary.SlideCount, Is.EqualTo(1));
        Assert.That(rightSummary.SlideCount, Is.EqualTo(2));
        Assert.That(rightSummary.ShapeCount, Is.EqualTo(leftSummary.ShapeCount + 1));
        Assert.That(comparison.Equal, Is.False);
        Assert.That(FindDifference(comparison, "SlideCount"), Is.Not.Null);
        Assert.That(FindDifference(comparison, "ShapeCount"), Is.Not.Null);
        Assert.That(FindDifference(comparison, "SlideCount")!.LeftValue, Is.EqualTo("1"));
        Assert.That(FindDifference(comparison, "SlideCount")!.RightValue, Is.EqualTo("2"));

        PptxEditableModelComparison same = packageOperations.CompareEditableModels(leftDocument.GetPackage(), packageOperations.ClonePackage(leftDocument.GetPackage()));
        Assert.That(same.Equal, Is.True);
        Assert.That(same.Differences, Is.Empty);
    }

    static PptxPackageManifestEntry? FindEntry(PptxPackageManifest manifest, string entryName)
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

    static PptxEditableModelDifference? FindDifference(PptxEditableModelComparison comparison, string fieldName)
    {
        for (int index = 0; index < comparison.Differences.Count; index++)
        {
            if (comparison.Differences[index].FieldName == fieldName)
            {
                return comparison.Differences[index];
            }
        }

        return null;
    }

    static long SumBytes(PptxPackageManifest manifest)
    {
        long total = 0L;
        for (int index = 0; index < manifest.Entries.Count; index++)
        {
            total = total + manifest.Entries[index].ByteCount;
        }

        return total;
    }

    static bool IsSorted(PptxPackageManifest manifest)
    {
        for (int index = 1; index < manifest.Entries.Count; index++)
        {
            if (string.CompareOrdinal(manifest.Entries[index - 1].EntryName, manifest.Entries[index].EntryName) > 0)
            {
                return false;
            }
        }

        return true;
    }
}
