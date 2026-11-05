using uspto;

namespace uspto.Tests;

public class IncrementalMirrorSelectorTests
{
    [Test]
    public void DeltaSelectionUsesStrictHighWaterMarkButRetainsOlderCorrections()
    {
        DateTimeOffset watermark = Time(8);
        DatasetManifest manifest = Manifest(DatasetFamily.PatentFileWrapperDelta, new[]
        {
            ManifestFile("known.zip", 10L),
            ManifestFile("corrected.zip", 10L)
        }, Time(1), false);
        BulkProduct product = Product(new[]
        {
            CatalogFile("known.zip", 10L, Time(2), true),
            CatalogFile("corrected.zip", 11L, Time(1), true),
            CatalogFile("old.zip", 3L, Time(7), true),
            CatalogFile("boundary.zip", 4L, watermark, true),
            CatalogFile("new.zip", 5L, Time(9), true),
            CatalogFile("undated.zip", 6L, default(DateTimeOffset), false)
        });
        IncrementalMirrorSelectionRequest request = Request(DatasetFamily.PatentFileWrapperDelta, product, manifest);
        request.HighWaterMark = watermark;
        request.HasHighWaterMark = true;

        IncrementalMirrorSelectionResult result = IncrementalMirrorSelector.SelectIncrementalMirror(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Entries.Select(entry => entry.IsSelected), Is.EqualTo(new[] { false, true, false, false, true, true }));
            Assert.That(result.SelectedEntries.Select(entry => entry.FileName), Is.EqualTo(new[] { "corrected.zip", "new.zip", "undated.zip" }));
            Assert.That(result.Entries[1].Reason, Is.EqualTo("DELTA_CORRECTED"));
            Assert.That(result.Entries[3].Reason, Is.EqualTo("DELTA_AT_OR_BEFORE_HIGH_WATER_MARK"));
            Assert.That(result.Entries[5].Reason, Is.EqualTo("DELTA_DATE_UNAVAILABLE"));
            Assert.That(result.Entries.All(entry => entry.Mode == MirrorSelectionMode.Delta), Is.True);
            Assert.That(result.NextHighWaterMark, Is.EqualTo(Time(9)));
            Assert.That(result.HasNextHighWaterMark, Is.True);
            Assert.That(result.SnapshotChanged, Is.False);
        });
    }

    [Test]
    public void WeeklyCatalogCadenceAppliesIncrementalSelectionToOtherFamilies()
    {
        BulkProduct product = Product(new[] { CatalogFile("assignment.zip", 20L, Time(10), true) });
        product.Frequency = "Weekly release";
        IncrementalMirrorSelectionRequest request = Request(DatasetFamily.PatentAssignment, product, EmptyManifest());
        request.HighWaterMark = Time(9);
        request.HasHighWaterMark = true;

        IncrementalMirrorSelectionResult result = IncrementalMirrorSelector.SelectIncrementalMirror(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.SelectedEntries.Single().Mode, Is.EqualTo(MirrorSelectionMode.Delta));
            Assert.That(result.SelectedEntries.Single().Reason, Is.EqualTo("DELTA_AFTER_HIGH_WATER_MARK"));
            Assert.That(result.NextHighWaterMark, Is.EqualTo(Time(10)));
        });
    }

    [Test]
    public void PatentsViewSnapshotDetectsNewChangedAndRemovedFiles()
    {
        DatasetManifest manifest = Manifest(DatasetFamily.PatentsViewGrant, new[]
        {
            ManifestFile("stable.tsv", 5L),
            ManifestFile("changed.tsv", 6L),
            ManifestFile("removed.tsv", 7L)
        }, Time(2), true);
        BulkProduct product = Product(new[]
        {
            CatalogFile("stable.tsv", 5L, default(DateTimeOffset), false),
            CatalogFile("changed.tsv", 8L, default(DateTimeOffset), false),
            CatalogFile("new.tsv", 9L, default(DateTimeOffset), false)
        });
        product.LastModified = Time(2);
        product.HasLastModified = true;

        IncrementalMirrorSelectionResult result = IncrementalMirrorSelector.SelectIncrementalMirror(Request(DatasetFamily.PatentsViewGrant, product, manifest));

        Assert.Multiple(() =>
        {
            Assert.That(result.SnapshotChanged, Is.True);
            Assert.That(result.Entries.Select(entry => entry.Reason), Is.EqualTo(new[]
            {
                "SNAPSHOT_UNCHANGED",
                "SNAPSHOT_FILE_CHANGED",
                "SNAPSHOT_FILE_NEW",
                "SNAPSHOT_FILE_REMOVED"
            }));
            Assert.That(result.SelectedEntries.Select(entry => entry.FileName), Is.EqualTo(new[] { "changed.tsv", "new.tsv" }));
            Assert.That(result.Entries[3].CatalogFile, Is.Null);
            Assert.That(result.Entries[3].ManifestFile, Is.Not.Null);
            Assert.That(result.Entries.All(entry => entry.Mode == MirrorSelectionMode.Snapshot), Is.True);
        });
    }

    [Test]
    public void CompleteSnapshotProductValidatorChangeSelectsEveryCurrentFile()
    {
        DatasetManifest manifest = Manifest(DatasetFamily.PatentFileWrapperComplete, new[]
        {
            ManifestFile("part-1.zip", 10L),
            ManifestFile("part-2.zip", 20L)
        }, Time(1), true);
        BulkProduct product = Product(new[]
        {
            CatalogFile("part-1.zip", 10L, default(DateTimeOffset), false),
            CatalogFile("part-2.zip", 20L, default(DateTimeOffset), false)
        });
        product.LastModified = Time(2);
        product.HasLastModified = true;

        IncrementalMirrorSelectionResult result = IncrementalMirrorSelector.SelectIncrementalMirror(Request(DatasetFamily.PatentFileWrapperComplete, product, manifest));

        Assert.Multiple(() =>
        {
            Assert.That(result.SnapshotChanged, Is.True);
            Assert.That(result.SelectedEntries.Length, Is.EqualTo(2));
            Assert.That(result.SelectedEntries.Select(entry => entry.Reason), Has.All.EqualTo("SNAPSHOT_PRODUCT_CHANGED"));
            Assert.That(result.HasNextHighWaterMark, Is.False);
        });
    }

    [Test]
    public void UnchangedSnapshotDoesNotProduceTransfersOrAWatermark()
    {
        DatasetManifest manifest = Manifest(DatasetFamily.PatentsViewSorted, new[] { ManifestFile("sorted.tsv", 30L) }, Time(3), true);
        BulkProduct product = Product(new[] { CatalogFile("sorted.tsv", 30L, default(DateTimeOffset), false) });
        product.LastModified = Time(3);
        product.HasLastModified = true;

        IncrementalMirrorSelectionResult result = IncrementalMirrorSelector.SelectIncrementalMirror(Request(DatasetFamily.PatentsViewSorted, product, manifest));

        Assert.Multiple(() =>
        {
            Assert.That(result.SnapshotChanged, Is.False);
            Assert.That(result.SelectedEntries, Is.Empty);
            Assert.That(result.Entries.Single().Reason, Is.EqualTo("SNAPSHOT_UNCHANGED"));
            Assert.That(result.HasNextHighWaterMark, Is.False);
        });
    }

    [Test]
    public void CancellationStopsSelectionBeforeCatalogEvaluation()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        IncrementalMirrorSelectionRequest request = Request(DatasetFamily.OfficeAction, Product(Array.Empty<BulkProductFile>()), EmptyManifest());
        request.CancellationToken = cancellation.Token;

        Assert.Throws<OperationCanceledException>(() => IncrementalMirrorSelector.SelectIncrementalMirror(request));
    }

    private static IncrementalMirrorSelectionRequest Request(DatasetFamily family, BulkProduct product, DatasetManifest manifest)
    {
        return new IncrementalMirrorSelectionRequest
        {
            Family = family,
            CatalogProducts = new[] { product },
            Manifest = manifest
        };
    }

    private static BulkProduct Product(BulkProductFile[] files)
    {
        return new BulkProduct { ProductIdentifier = "PRODUCT", Files = files };
    }

    private static BulkProductFile CatalogFile(string fileName, long size, DateTimeOffset releaseTime, bool hasReleaseTime)
    {
        return new BulkProductFile
        {
            ProductIdentifier = "PRODUCT",
            FileName = fileName,
            Size = size,
            HasSize = true,
            ReleaseDate = releaseTime,
            HasReleaseDate = hasReleaseTime
        };
    }

    private static DatasetManifest Manifest(DatasetFamily family, DatasetManifestFile[] files, DateTimeOffset modified, bool hasModified)
    {
        PatentDataProvenance provenance = Provenance();
        return new DatasetManifest
        {
            Provenance = provenance,
            Products = new[]
            {
                new DatasetManifestProduct
                {
                    ProductIdentifier = "PRODUCT",
                    Family = family,
                    Files = files,
                    LastModified = modified,
                    HasLastModified = hasModified,
                    Provenance = provenance
                }
            }
        };
    }

    private static DatasetManifest EmptyManifest()
    {
        return new DatasetManifest { Provenance = Provenance() };
    }

    private static DatasetManifestFile ManifestFile(string fileName, long size)
    {
        return new DatasetManifestFile
        {
            ProductIdentifier = "PRODUCT",
            FileName = fileName,
            Size = size,
            HasSize = true,
            Provenance = Provenance()
        };
    }

    private static PatentDataProvenance Provenance()
    {
        return new PatentDataProvenance { Source = DataProvenance.LocalMirror, IsAuthoritative = false };
    }

    private static DateTimeOffset Time(int day)
    {
        return new DateTimeOffset(2026, 9, day, 0, 0, 0, TimeSpan.Zero);
    }
}
