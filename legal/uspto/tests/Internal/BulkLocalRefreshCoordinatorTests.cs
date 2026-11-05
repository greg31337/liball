using System.Security.Cryptography;
using System.Text;
using uspto;

namespace uspto.Tests;

public class BulkLocalRefreshCoordinatorTests
{
    [Test]
    public void RefreshDiscoversPlansSynchronizesProcessesAndVerifiesEverySelectedFamily()
    {
        string directory = CreateDirectory();
        try
        {
            BulkProduct officeActions = Product("OFFICE", "office.bin", Encoding.UTF8.GetBytes("office"));
            BulkProduct assignments = Product("ASSIGN", "assign.bin", Encoding.UTF8.GetBytes("assignment"));
            officeActions.Title = "Office Actions";
            officeActions.LastModified = Time(3);
            officeActions.HasLastModified = true;
            RecordingBulkDatasetPresetSource discovery = Discovery(new[]
            {
                Preset(DatasetFamily.PatentAssignment, assignments, true),
                Preset(DatasetFamily.OfficeAction, officeActions, true)
            });
            RecordingMirrorTransfer transfer = Transfer(officeActions, assignments);
            RecordingMirrorStageExecutor stages = new RecordingMirrorStageExecutor();
            BulkLocalRefreshRequest request = Request(directory, new[] { DatasetFamily.OfficeAction, DatasetFamily.PatentAssignment });

            BulkLocalRefreshResult result = Coordinator(discovery, transfer, stages).Refresh(request);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.AvailableFamilyCount, Is.EqualTo(2));
                Assert.That(result.ProcessedFileCount, Is.EqualTo(2));
                Assert.That(result.FailedFileCount, Is.Zero);
                Assert.That(result.Families.Select(item => item.Family), Is.EqualTo(request.DatasetFamilies));
                Assert.That(result.Families.Select(item => item.Plan.NewCount), Is.EqualTo(new[] { 1, 1 }));
                Assert.That(result.Families.Select(item => item.Synchronization.TransferredCount), Is.EqualTo(new[] { 1, 1 }));
                Assert.That(result.Files.Select(item => item.Pipeline!.Stages.Select(stage => stage.Stage)), Has.All.EqualTo(new[] { MirrorPipelineStage.Download, MirrorPipelineStage.Checksum, MirrorPipelineStage.Extraction, MirrorPipelineStage.Parse }));
                Assert.That(result.Files.Select(item => item.Pipeline!.Stages[0].WasReused), Has.All.True);
                Assert.That(result.Files.Select(item => item.Verification!.IsValid), Has.All.True);
                Assert.That(transfer.CallCount, Is.EqualTo(2));
                Assert.That(stages.Calls, Is.EqualTo(new[] { MirrorPipelineStage.Checksum, MirrorPipelineStage.Extraction, MirrorPipelineStage.Parse, MirrorPipelineStage.Checksum, MirrorPipelineStage.Extraction, MirrorPipelineStage.Parse }));
                Assert.That(discovery.RequestedFamilies.Single(), Is.EqualTo(request.DatasetFamilies));
                Assert.That(result.Manifest.Products.Single(product => product.ProductIdentifier == "OFFICE").Title, Is.EqualTo("Office Actions"));
                Assert.That(result.Manifest.Products.Single(product => product.ProductIdentifier == "OFFICE").LastModified, Is.EqualTo(Time(3)));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void DeltaRefreshHonorsHighWaterMarkWhileReportingSkippedCatalogFiles()
    {
        string directory = CreateDirectory();
        try
        {
            BulkProduct product = Product("OFFICE", "old.bin", Encoding.UTF8.GetBytes("old"));
            BulkProductFile recent = CatalogFile("OFFICE", "recent.bin", Encoding.UTF8.GetBytes("recent"));
            product.Files = new[] { product.Files[0], recent };
            product.Files[0].ReleaseDate = Time(4);
            product.Files[0].HasReleaseDate = true;
            recent.ReleaseDate = Time(6);
            recent.HasReleaseDate = true;
            RecordingBulkDatasetPresetSource discovery = Discovery(new[] { Preset(DatasetFamily.OfficeAction, product, true) });
            RecordingMirrorTransfer transfer = Transfer(product);
            RecordingMirrorStageExecutor stages = new RecordingMirrorStageExecutor();
            BulkLocalRefreshRequest request = Request(directory, new[] { DatasetFamily.OfficeAction });
            request.HighWaterMark = Time(5);
            request.HasHighWaterMark = true;

            BulkLocalRefreshResult result = Coordinator(discovery, transfer, stages).Refresh(request);

            BulkLocalRefreshFileResult old = result.Files.Single(item => item.FileName == "old.bin");
            BulkLocalRefreshFileResult recentResult = result.Files.Single(item => item.FileName == "recent.bin");
            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.SkippedFileCount, Is.EqualTo(1));
                Assert.That(old.Selection!.Reason, Is.EqualTo("DELTA_AT_OR_BEFORE_HIGH_WATER_MARK"));
                Assert.That(old.WasSkipped, Is.True);
                Assert.That(old.Synchronization, Is.Null);
                Assert.That(old.IsComplete, Is.True);
                Assert.That(recentResult.Selection!.Reason, Is.EqualTo("DELTA_AFTER_HIGH_WATER_MARK"));
                Assert.That(recentResult.Synchronization!.Download.State, Is.EqualTo(DownloadState.Completed));
                Assert.That(transfer.CallCount, Is.EqualTo(1));
                Assert.That(result.Manifest.Products.Single().Files.Select(file => file.FileName), Is.EqualTo(new[] { "recent.bin" }));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void SnapshotMetadataIsPersistedSoAnUnchangedSecondRefreshIsNotSelectedAgain()
    {
        string directory = CreateDirectory();
        try
        {
            byte[] content = Encoding.UTF8.GetBytes("stable");
            Write(directory, "files/PV/data.bin", content);
            DatasetManifest manifest = ExistingManifest(DatasetFamily.PatentsViewGrant, "PV", "data.bin", content, Time(1));
            DatasetManifestStore.Save(directory, manifest, Array.Empty<string>());
            BulkProduct product = Product("PV", "data.bin", content);
            product.Title = "Current snapshot";
            product.LastModified = Time(2);
            product.HasLastModified = true;
            RecordingBulkDatasetPresetSource discovery = Discovery(new[] { Preset(DatasetFamily.PatentsViewGrant, product, true) });
            RecordingMirrorTransfer transfer = Transfer(product);
            RecordingMirrorStageExecutor stages = new RecordingMirrorStageExecutor();
            BulkLocalRefreshCoordinator coordinator = Coordinator(discovery, transfer, stages);
            BulkLocalRefreshRequest request = Request(directory, new[] { DatasetFamily.PatentsViewGrant });

            BulkLocalRefreshResult first = coordinator.Refresh(request);
            BulkLocalRefreshResult second = coordinator.Refresh(request);

            Assert.Multiple(() =>
            {
                Assert.That(first.Families[0].Selection.SnapshotChanged, Is.True);
                Assert.That(first.Files[0].WasIncrementallySelected, Is.True);
                Assert.That(first.Files[0].Synchronization!.Action, Is.EqualTo(MirrorAction.Changed));
                Assert.That(first.Files[0].Synchronization!.SkipReason, Is.EqualTo("ALREADY_SYNCHRONIZED"));
                Assert.That(second.Families[0].Selection.SnapshotChanged, Is.False);
                Assert.That(second.Files[0].WasIncrementallySelected, Is.False);
                Assert.That(second.Files[0].Synchronization, Is.Null);
                Assert.That(second.Files[0].Pipeline!.Stages.Select(stage => stage.WasReused), Has.All.True);
                Assert.That(second.IsComplete, Is.True);
                Assert.That(transfer.CallCount, Is.Zero);
                Assert.That(DatasetManifestStore.Load(directory).Products.Single().LastModified, Is.EqualTo(Time(2)));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void RemovedSnapshotFileBecomesAnOrphanWithoutDeletingManifestOrArtifactState()
    {
        string directory = CreateDirectory();
        try
        {
            byte[] content = Encoding.UTF8.GetBytes("preserve retired snapshot");
            Write(directory, "files/PV/retired.bin", content);
            DatasetManifest manifest = ExistingManifest(DatasetFamily.PatentsViewGrant, "PV", "retired.bin", content, Time(1));
            DatasetManifestStore.Save(directory, manifest, Array.Empty<string>());
            BulkProduct product = new BulkProduct
            {
                ProductIdentifier = "PV",
                Title = "Replacement snapshot",
                LastModified = Time(2),
                HasLastModified = true,
                Files = Array.Empty<BulkProductFile>()
            };
            RecordingBulkDatasetPresetSource discovery = Discovery(new[] { Preset(DatasetFamily.PatentsViewGrant, product, true) });
            RecordingMirrorTransfer transfer = new RecordingMirrorTransfer(0);
            RecordingMirrorStageExecutor stages = new RecordingMirrorStageExecutor();

            BulkLocalRefreshResult result = Coordinator(discovery, transfer, stages).Refresh(Request(directory, new[] { DatasetFamily.PatentsViewGrant }));
            DatasetManifest persisted = DatasetManifestStore.Load(directory);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.Families[0].Selection.SnapshotChanged, Is.True);
                Assert.That(result.Families[0].Selection.Entries.Single().Reason, Is.EqualTo("SNAPSHOT_FILE_REMOVED"));
                Assert.That(result.Families[0].Plan.OrphanedCount, Is.EqualTo(1));
                Assert.That(result.Files[0].Plan.Action, Is.EqualTo(MirrorAction.Orphaned));
                Assert.That(result.Files[0].WasSkipped, Is.True);
                Assert.That(result.Files[0].IsComplete, Is.True);
                Assert.That(transfer.CallCount, Is.Zero);
                Assert.That(stages.Calls, Is.Empty);
                Assert.That(File.ReadAllBytes(Path.Combine(directory, "files/PV/retired.bin")), Is.EqualTo(content));
                Assert.That(persisted.Products.Single().Files.Single().FileName, Is.EqualTo("retired.bin"));
                Assert.That(Directory.Exists(Path.Combine(directory, ".superseded")), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void FailedProcessingStageRetainsTransferAndStageEvidenceInTheResult()
    {
        string directory = CreateDirectory();
        try
        {
            BulkProduct product = Product("OFFICE", "data.bin", Encoding.UTF8.GetBytes("content"));
            RecordingBulkDatasetPresetSource discovery = Discovery(new[] { Preset(DatasetFamily.OfficeAction, product, true) });
            RecordingMirrorTransfer transfer = Transfer(product);
            RecordingMirrorStageExecutor stages = new RecordingMirrorStageExecutor();
            stages.ConfigureFailure(MirrorPipelineStage.Parse, 1, "parser stopped");

            BulkLocalRefreshResult result = Coordinator(discovery, transfer, stages).Refresh(Request(directory, new[] { DatasetFamily.OfficeAction }));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.FailedFileCount, Is.EqualTo(1));
                Assert.That(result.Files[0].Synchronization!.Download.State, Is.EqualTo(DownloadState.Completed));
                Assert.That(result.Files[0].Pipeline!.Stages.Last().State, Is.EqualTo(MirrorStageState.Failed));
                Assert.That(result.Files[0].FailureCode, Is.EqualTo("PARSE_FAILED"));
                Assert.That(result.Files[0].Verification!.IsValid, Is.False);
                Assert.That(result.Families[0].IsComplete, Is.False);
                Assert.That(File.Exists(Path.Combine(directory, "files/OFFICE/data.bin")), Is.True);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void UnavailableSelectedFamilyIsExplicitAndMakesTheRefreshIncomplete()
    {
        string directory = CreateDirectory();
        try
        {
            RecordingBulkDatasetPresetSource discovery = Discovery(new[] { Preset(DatasetFamily.Ttab, new BulkProduct(), false) });
            RecordingMirrorTransfer transfer = new RecordingMirrorTransfer(0);
            RecordingMirrorStageExecutor stages = new RecordingMirrorStageExecutor();

            BulkLocalRefreshResult result = Coordinator(discovery, transfer, stages).Refresh(Request(directory, new[] { DatasetFamily.Ttab }));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsComplete, Is.False);
                Assert.That(result.AvailableFamilyCount, Is.Zero);
                Assert.That(result.UnavailableFamilyCount, Is.EqualTo(1));
                Assert.That(result.Families[0].IsAvailable, Is.False);
                Assert.That(result.Files, Is.Empty);
                Assert.That(result.Verification.IsValid, Is.True);
                Assert.That(transfer.CallCount, Is.Zero);
                Assert.That(File.Exists(DatasetManifestStore.GetPath(directory)), Is.True);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CancellationBeforeDiscoveryDoesNotCallDependenciesOrCreateMirrorState()
    {
        string directory = CreateDirectory();
        try
        {
            RecordingBulkDatasetPresetSource discovery = Discovery(new[] { Preset(DatasetFamily.OfficeAction, new BulkProduct(), false) });
            RecordingMirrorTransfer transfer = new RecordingMirrorTransfer(0);
            RecordingMirrorStageExecutor stages = new RecordingMirrorStageExecutor();
            BulkLocalRefreshRequest request = Request(directory, new[] { DatasetFamily.OfficeAction });
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            request.Context.CancellationToken = cancellation.Token;

            Assert.Throws<OperationCanceledException>(() => Coordinator(discovery, transfer, stages).Refresh(request));

            Assert.Multiple(() =>
            {
                Assert.That(discovery.CallCount, Is.Zero);
                Assert.That(transfer.CallCount, Is.Zero);
                Assert.That(stages.Calls, Is.Empty);
                Assert.That(File.Exists(DatasetManifestStore.GetPath(directory)), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static BulkLocalRefreshCoordinator Coordinator(RecordingBulkDatasetPresetSource discovery, RecordingMirrorTransfer transfer, RecordingMirrorStageExecutor stages)
    {
        return new BulkLocalRefreshCoordinator(discovery, new MirrorSynchronizer(transfer), new MirrorStagePipeline(stages));
    }

    private static BulkLocalRefreshRequest Request(string directory, DatasetFamily[] families)
    {
        return new BulkLocalRefreshRequest
        {
            MirrorDirectory = directory,
            DatasetFamilies = families,
            MaximumParallelTransfers = 2,
            Now = Time(10),
            Context = new RequestContext(),
            Secrets = Array.Empty<string>()
        };
    }

    private static RecordingBulkDatasetPresetSource Discovery(BulkDatasetPreset[] presets)
    {
        return new RecordingBulkDatasetPresetSource { Presets = presets };
    }

    private static BulkDatasetPreset Preset(DatasetFamily family, BulkProduct product, bool available)
    {
        return new BulkDatasetPreset
        {
            Family = family,
            Products = available ? new[] { product } : Array.Empty<BulkProduct>(),
            ProductIdentifiers = available ? new[] { product.ProductIdentifier! } : Array.Empty<string>(),
            IsAvailable = available
        };
    }

    private static BulkProduct Product(string identifier, string fileName, byte[] content)
    {
        return new BulkProduct
        {
            ProductIdentifier = identifier,
            Files = new[] { CatalogFile(identifier, fileName, content) }
        };
    }

    private static BulkProductFile CatalogFile(string identifier, string fileName, byte[] content)
    {
        return new BulkProductFile
        {
            ProductIdentifier = identifier,
            FileName = fileName,
            DownloadUri = new Uri("https://api.uspto.gov/files/" + fileName),
            Size = content.LongLength,
            HasSize = true
        };
    }

    private static RecordingMirrorTransfer Transfer(params BulkProduct[] products)
    {
        RecordingMirrorTransfer result = new RecordingMirrorTransfer(0);
        int productIndex;
        for (productIndex = 0; productIndex < products.Length; productIndex += 1)
        {
            int fileIndex;
            for (fileIndex = 0; fileIndex < products[productIndex].Files.Length; fileIndex += 1)
            {
                BulkProductFile file = products[productIndex].Files[fileIndex];
                result.Bodies[file.FileName!] = Encoding.UTF8.GetBytes(file.FileName == "assign.bin" ? "assignment" : file.FileName == "office.bin" ? "office" : file.FileName == "recent.bin" ? "recent" : file.FileName == "data.bin" ? "content" : "old");
            }
        }

        return result;
    }

    private static DatasetManifest ExistingManifest(DatasetFamily family, string productIdentifier, string fileName, byte[] content, DateTimeOffset modified)
    {
        PatentDataProvenance provenance = new PatentDataProvenance { Source = DataProvenance.LocalMirror, IsAuthoritative = false };
        return new DatasetManifest
        {
            Provenance = provenance,
            Products = new[]
            {
                new DatasetManifestProduct
                {
                    ProductIdentifier = productIdentifier,
                    Family = family,
                    LastModified = modified,
                    HasLastModified = true,
                    Provenance = provenance,
                    Files = new[]
                    {
                        new DatasetManifestFile
                        {
                            ProductIdentifier = productIdentifier,
                            FileName = fileName,
                            LocalPath = "files/" + productIdentifier + "/" + fileName,
                            PartialPath = "files/" + productIdentifier + "/" + fileName + ".partial",
                            Size = content.LongLength,
                            HasSize = true,
                            DigestAlgorithm = ChecksumAlgorithm.Sha256,
                            Digest = Digest(content),
                            DigestVerified = true,
                            ExtractedPaths = Array.Empty<string>(),
                            Stages = CompleteStages(),
                            Provenance = provenance
                        }
                    }
                }
            }
        };
    }

    private static DatasetManifestStage[] CompleteStages()
    {
        return new[]
        {
            new DatasetManifestStage { Name = "download", State = MirrorStageState.Complete },
            new DatasetManifestStage { Name = "checksum", State = MirrorStageState.Complete },
            new DatasetManifestStage { Name = "extract", State = MirrorStageState.Complete },
            new DatasetManifestStage { Name = "parse", State = MirrorStageState.Complete }
        };
    }

    private static string Digest(byte[] content)
    {
        return Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    }

    private static void Write(string directory, string relativePath, byte[] content)
    {
        string path = Path.Combine(directory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
    }

    private static DateTimeOffset Time(int day)
    {
        return new DateTimeOffset(2026, 9, day, 0, 0, 0, TimeSpan.Zero);
    }

    private static string CreateDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "uspto-bulk-local-refresh-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}

internal class RecordingBulkDatasetPresetSource : IBulkDatasetPresetSource
{
    internal BulkDatasetPreset[] Presets = Array.Empty<BulkDatasetPreset>();
    internal List<DatasetFamily[]> RequestedFamilies = new List<DatasetFamily[]>();
    internal int CallCount = 0;

    public BulkDatasetPreset[] GetBulkDatasetPresets(DatasetFamily[] families, RequestContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        CallCount += 1;
        RequestedFamilies.Add(families.ToArray());
        return Presets;
    }
}
