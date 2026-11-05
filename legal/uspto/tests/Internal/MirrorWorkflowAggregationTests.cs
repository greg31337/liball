using uspto;

namespace uspto.Tests;

public class MirrorWorkflowAggregationTests
{
    [Test]
    public void RefreshConversionRetainsSuccessfulFilesAndEveryIndependentFailure()
    {
        BulkLocalRefreshFileResult completed = new BulkLocalRefreshFileResult
        {
            Family = DatasetFamily.PatentsViewGrant,
            ProductIdentifier = "product-a",
            FileName = "complete.tsv.gz",
            Plan = new MirrorPlanEntry { Action = MirrorAction.Unchanged },
            WasSkipped = true,
            IsComplete = true
        };
        BulkLocalRefreshFileResult transferFailure = new BulkLocalRefreshFileResult
        {
            Family = DatasetFamily.PatentsViewGrant,
            ProductIdentifier = "product-a",
            FileName = "failed.tsv.gz",
            Plan = new MirrorPlanEntry { Action = MirrorAction.Missing },
            WasProcessed = true,
            FailureCode = "NETWORK_FAILURE",
            FailureMessage = "Transfer failed.",
            Synchronization = new MirrorSynchronizationEntryResult
            {
                Download = new DownloadResult
                {
                    State = DownloadState.Failed,
                    FailureCode = "NETWORK_FAILURE",
                    FailureMessage = "Transfer failed."
                }
            }
        };
        BulkLocalRefreshFileResult pipelineFailure = new BulkLocalRefreshFileResult
        {
            Family = DatasetFamily.PatentsViewGrant,
            ProductIdentifier = "product-a",
            FileName = "parse.tsv.gz",
            Plan = new MirrorPlanEntry { Action = MirrorAction.Changed },
            WasProcessed = true,
            FailureCode = "PARSE_FAILED",
            FailureMessage = "Parse failed.",
            Synchronization = new MirrorSynchronizationEntryResult
            {
                Download = new DownloadResult { State = DownloadState.Completed }
            }
        };
        BulkLocalRefreshResult refresh = new BulkLocalRefreshResult
        {
            Files = new[] { completed, transferFailure, pipelineFailure },
            Families = new[]
            {
                new BulkLocalRefreshFamilyResult
                {
                    Family = DatasetFamily.PatentsViewGrant,
                    IsAvailable = true,
                    MetadataPersisted = true
                },
                new BulkLocalRefreshFamilyResult
                {
                    Family = DatasetFamily.PatentsViewPreGrant,
                    IsAvailable = false
                }
            },
            Manifest = new DatasetManifest { ManifestIdentifier = "refresh-manifest" },
            UnavailableFamilyCount = 1,
            ProcessedFileCount = 2,
            SkippedFileCount = 1,
            FailedFileCount = 2,
            IsComplete = false
        };

        MirrorWorkflowResult result = ConfiguredDatasetMirror.ConvertRefreshResult(refresh, new RequestContext { CorrelationIdentifier = "refresh-request" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Files, Has.Length.EqualTo(3));
            Assert.That(result.Files[0].IsComplete, Is.True);
            Assert.That(result.Files[0].WasSkipped, Is.True);
            Assert.That(result.Files[1].Transfer, Is.SameAs(transferFailure.Synchronization?.Download));
            Assert.That(result.Files[1].Error?.ExceptionType, Is.EqualTo("DownloadException"));
            Assert.That(result.Files[1].Error?.ErrorCode, Is.EqualTo("NETWORK_FAILURE"));
            Assert.That(result.Files[2].Transfer, Is.SameAs(pipelineFailure.Synchronization?.Download));
            Assert.That(result.Files[2].Error?.ExceptionType, Is.EqualTo("MirrorException"));
            Assert.That(result.Files[2].Error?.ErrorCode, Is.EqualTo("PARSE_FAILED"));
            Assert.That(result.CompletedFileCount, Is.EqualTo(1));
            Assert.That(result.FailedFileCount, Is.EqualTo(2));
            Assert.That(result.UnavailableFamilyCount, Is.EqualTo(1));
            Assert.That(result.ProcessedFileCount, Is.EqualTo(2));
            Assert.That(result.SkippedFileCount, Is.EqualTo(1));
            Assert.That(result.Errors, Has.Length.EqualTo(3));
            Assert.That(result.Errors.Select(item => item.Section), Is.EqualTo(new[] { "product-a/failed.tsv.gz", "product-a/parse.tsv.gz", "family." + ((int)DatasetFamily.PatentsViewPreGrant).ToString() }));
            Assert.That(result.Manifest.ManifestIdentifier, Is.EqualTo("refresh-manifest"));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.HasPartialSuccess, Is.True);
            Assert.That(result.Provenance.Source, Is.EqualTo(DataProvenance.ClientDerived));
            Assert.That(result.Provenance.RequestIdentifier, Is.EqualTo("refresh-request"));
        });
    }

    [Test]
    public void IncompleteRefreshWithoutFileDetailsStillProducesStructuredFailure()
    {
        BulkLocalRefreshResult refresh = new BulkLocalRefreshResult
        {
            Families = new[]
            {
                new BulkLocalRefreshFamilyResult
                {
                    Family = DatasetFamily.PatentsViewGrant,
                    IsAvailable = true,
                    MetadataPersisted = true
                }
            },
            IsComplete = false
        };

        MirrorWorkflowResult result = ConfiguredDatasetMirror.ConvertRefreshResult(refresh, new RequestContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Errors.Single().Section, Is.EqualTo("refresh"));
            Assert.That(result.Errors.Single().ErrorCode, Is.EqualTo("MIRROR_REFRESH_INCOMPLETE"));
            Assert.That(result.IsComplete, Is.False);
            Assert.That(result.HasPartialSuccess, Is.False);
        });
    }
}
