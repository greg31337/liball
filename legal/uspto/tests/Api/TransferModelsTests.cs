using uspto;

namespace uspto.Tests;

public class TransferModelsTests
{
    [Test]
    public void DownloadRequestDefaultsAreSafeAndUnboundedOnlyWhenExplicit()
    {
        DownloadRequest request = new DownloadRequest();

        Assert.Multiple(() =>
        {
            Assert.That(request.CatalogFileName, Is.Null);
            Assert.That(request.SourceUri, Is.Null);
            Assert.That(request.Output, Is.Null);
            Assert.That(request.DestinationPath, Is.Null);
            Assert.That(request.OverwriteExisting, Is.False);
            Assert.That(request.ResumePartial, Is.True);
            Assert.That(request.ExpectedSize, Is.Zero);
            Assert.That(request.HasExpectedSize, Is.False);
            Assert.That(request.ExpectedChecksums, Is.Empty);
            Assert.That(request.Progress, Is.Null);
            Assert.That(request.Context, Is.Not.Null);
            Assert.That(new DownloadProgress().HasTimestamp, Is.False);
            Assert.That(new DownloadResumeState().HasUpdatedAt, Is.False);
        });
    }

    [Test]
    public void DownloadModelDefaultsAreIndependent()
    {
        DownloadResult first = new DownloadResult();
        DownloadResult second = new DownloadResult();

        first.Request.ExpectedChecksums = new ChecksumExpectation[]
        {
            new ChecksumExpectation
            {
                Algorithm = ChecksumAlgorithm.Sha256,
                Value = "AABB"
            }
        };
        first.Progress.BytesTransferred = 100L;
        first.Content.FileName = "first.zip";
        first.ResumeState.CompletedBytes = 50L;
        first.Checksums = new ChecksumResult[] { new ChecksumResult() };

        Assert.Multiple(() =>
        {
            Assert.That(first.Request, Is.Not.SameAs(second.Request));
            Assert.That(first.Progress, Is.Not.SameAs(second.Progress));
            Assert.That(first.Content, Is.Not.SameAs(second.Content));
            Assert.That(first.ResumeState, Is.Not.SameAs(second.ResumeState));
            Assert.That(second.Request.ExpectedChecksums, Is.Empty);
            Assert.That(second.Progress.BytesTransferred, Is.Zero);
            Assert.That(second.Content.FileName, Is.Null);
            Assert.That(second.ResumeState.CompletedBytes, Is.Zero);
            Assert.That(second.Checksums, Is.Empty);
        });
    }

    [Test]
    public void ProgressAndResumeStateDistinguishKnownTotals()
    {
        DateTimeOffset modified = new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);
        DownloadProgress progress = new DownloadProgress
        {
            State = DownloadState.Downloading,
            BytesTransferred = 750L,
            ResumedBytes = 500L,
            TotalBytes = 1000L,
            HasTotalBytes = true,
            FileName = "dataset.zip"
        };
        DownloadResumeState resume = new DownloadResumeState
        {
            SourceUri = new Uri("https://bulkdata.uspto.gov/dataset.zip"),
            PartialPath = "/mirror/dataset.zip.partial",
            CompletedBytes = 750L,
            ExpectedTotalBytes = 1000L,
            HasExpectedTotalBytes = true,
            ETag = "\"validator\"",
            LastModified = modified,
            HasLastModified = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(progress.State, Is.EqualTo(DownloadState.Downloading));
            Assert.That(progress.BytesTransferred, Is.EqualTo(750L));
            Assert.That(progress.ResumedBytes, Is.EqualTo(500L));
            Assert.That(progress.TotalBytes, Is.EqualTo(1000L));
            Assert.That(progress.HasTotalBytes, Is.True);
            Assert.That(resume.CompletedBytes, Is.EqualTo(750L));
            Assert.That(resume.ExpectedTotalBytes, Is.EqualTo(1000L));
            Assert.That(resume.HasExpectedTotalBytes, Is.True);
            Assert.That(resume.ETag, Is.EqualTo("\"validator\""));
            Assert.That(resume.LastModified, Is.EqualTo(modified));
            Assert.That(resume.HasLastModified, Is.True);
        });
    }

    [Test]
    public void ContentAndChecksumResultsRepresentVerificationOutcome()
    {
        ChecksumExpectation expectation = new ChecksumExpectation
        {
            Algorithm = ChecksumAlgorithm.Sha256,
            Value = "AABB"
        };
        ChecksumResult checksum = new ChecksumResult
        {
            Expectation = expectation,
            ActualValue = "AABB",
            IsAvailable = true,
            IsComplete = true,
            IsMatch = true
        };
        DownloadContentMetadata content = new DownloadContentMetadata
        {
            FinalUri = new Uri("https://bulkdata.uspto.gov/dataset.zip"),
            FileName = "dataset.zip",
            MediaType = UsptoMediaTypes.Zip,
            Format = DocumentFormat.Zip,
            ContentLength = 4096L,
            HasContentLength = true,
            AcceptsByteRanges = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(checksum.Expectation, Is.SameAs(expectation));
            Assert.That(checksum.Expectation.Algorithm, Is.EqualTo(ChecksumAlgorithm.Sha256));
            Assert.That(checksum.IsAvailable, Is.True);
            Assert.That(checksum.IsComplete, Is.True);
            Assert.That(checksum.IsMatch, Is.True);
            Assert.That(content.FileName, Is.EqualTo("dataset.zip"));
            Assert.That(content.MediaType, Is.EqualTo(UsptoMediaTypes.Zip));
            Assert.That(content.Format, Is.EqualTo(DocumentFormat.Zip));
            Assert.That(content.ContentLength, Is.EqualTo(4096L));
            Assert.That(content.HasContentLength, Is.True);
            Assert.That(content.AcceptsByteRanges, Is.True);
            Assert.That(content.HasLastModified, Is.False);
        });
    }

    [Test]
    public void ArchiveListingPreservesDetectionAndOptionalSizes()
    {
        ArchiveEntry entry = new ArchiveEntry
        {
            Name = "records/data.xml",
            CompressedSize = 100L,
            HasCompressedSize = true,
            UncompressedSize = 300L,
            HasUncompressedSize = true
        };
        ArchiveListingResult listing = new ArchiveListingResult
        {
            SourceName = "dataset.zip",
            Detection = new CompressionDetectionResult
            {
                Format = CompressionFormat.Zip,
                DetectedBySignature = true,
                ExtensionMatches = true,
                IsSupported = true
            },
            Entries = new ArchiveEntry[] { entry },
            IsComplete = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(listing.Detection.Format, Is.EqualTo(CompressionFormat.Zip));
            Assert.That(listing.Detection.DetectedBySignature, Is.True);
            Assert.That(listing.Detection.ExtensionMatches, Is.True);
            Assert.That(listing.Detection.IsSupported, Is.True);
            Assert.That(listing.Entries, Has.Length.EqualTo(1));
            Assert.That(listing.Entries[0], Is.SameAs(entry));
            Assert.That(entry.CompressedSize, Is.EqualTo(100L));
            Assert.That(entry.HasCompressedSize, Is.True);
            Assert.That(entry.UncompressedSize, Is.EqualTo(300L));
            Assert.That(entry.HasUncompressedSize, Is.True);
            Assert.That(entry.HasLastModified, Is.False);
        });
    }

    [Test]
    public void ExtractionDefaultsAreNonDestructiveAndIndependent()
    {
        ExtractionResult first = new ExtractionResult();
        ExtractionResult second = new ExtractionResult();

        first.Options.SelectedEntries = new string[] { "records/data.xml" };
        first.Options.Limits.MaximumEntries = 1;
        first.Detection.Format = CompressionFormat.Zip;
        first.Entries = new ExtractionEntryResult[] { new ExtractionEntryResult() };

        Assert.Multiple(() =>
        {
            Assert.That(first.Options, Is.Not.SameAs(second.Options));
            Assert.That(first.Options.Limits, Is.Not.SameAs(second.Options.Limits));
            Assert.That(first.Detection, Is.Not.SameAs(second.Detection));
            Assert.That(second.Options.SelectedEntries, Is.Empty);
            Assert.That(second.Options.ExtractAll, Is.True);
            Assert.That(second.Options.OverwriteExisting, Is.False);
            Assert.That(second.Options.Limits.MaximumEntries, Is.EqualTo(UsptoDefaults.ArchiveEntryCount));
            Assert.That(second.Detection.Format, Is.EqualTo(CompressionFormat.Unknown));
            Assert.That(second.Entries, Is.Empty);
            Assert.That(second.IsComplete, Is.False);
            Assert.That(second.IsCancelled, Is.False);
        });
    }

    [Test]
    public void ExtractionResultRetainsLimitAndEntryOutcomes()
    {
        ArchiveEntry entry = new ArchiveEntry
        {
            Name = "records/data.xml",
            UncompressedSize = 300L,
            HasUncompressedSize = true
        };
        ArchiveSafetyLimitResult safety = new ArchiveSafetyLimitResult
        {
            IsAccepted = true,
            LimitName = "MaximumEntryBytes",
            ObservedValue = 300L,
            LimitValue = 1024L,
            Entry = entry
        };
        ExtractionEntryResult extracted = new ExtractionEntryResult
        {
            Entry = entry,
            DestinationPath = "/extract/records/data.xml",
            IsExtracted = true
        };
        ExtractionResult result = new ExtractionResult
        {
            Detection = new CompressionDetectionResult
            {
                Format = CompressionFormat.Zip,
                IsSupported = true
            },
            SafetyLimits = new ArchiveSafetyLimitResult[] { safety },
            Entries = new ExtractionEntryResult[] { extracted },
            BytesWritten = 300L,
            IsComplete = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.SafetyLimits[0], Is.SameAs(safety));
            Assert.That(result.SafetyLimits[0].Entry, Is.SameAs(entry));
            Assert.That(result.SafetyLimits[0].ObservedValue, Is.EqualTo(300L));
            Assert.That(result.SafetyLimits[0].LimitValue, Is.EqualTo(1024L));
            Assert.That(result.Entries[0], Is.SameAs(extracted));
            Assert.That(result.Entries[0].IsExtracted, Is.True);
            Assert.That(result.BytesWritten, Is.EqualTo(300L));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.FailureCode, Is.Null);
        });
    }
}
