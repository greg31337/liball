using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using uspto;

namespace uspto.Tests;

public class MirrorVerifierTests
{
    [Test]
    public void ValidMirrorPassesEveryApplicableCheckWithoutMutation()
    {
        string directory = CreateDirectory();
        try
        {
            string archivePath = Path.Combine(directory, "files/data.zip");
            string extractedPath = Path.Combine(directory, "extracted/data.txt");
            CreateZip(archivePath, "data.txt", "payload");
            Directory.CreateDirectory(Path.GetDirectoryName(extractedPath)!);
            File.WriteAllText(extractedPath, "payload");
            DatasetManifest manifest = Manifest(new[]
            {
                ManifestFile("data.zip", "files/data.zip", new[] { "extracted/data.txt" }, new[]
                {
                    Stage("download", MirrorStageState.Complete),
                    Stage("checksum", MirrorStageState.Complete),
                    Stage("extract", MirrorStageState.Complete),
                    Stage("parse", MirrorStageState.Complete)
                })
            });
            SetFileEvidence(manifest.Products[0].Files[0], archivePath, Digest(archivePath));
            string before = File.ReadAllText(archivePath);

            MirrorVerificationResult result = MirrorVerifier.Verify(Request(directory, manifest));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.True);
                Assert.That(result.ValidFileCount, Is.EqualTo(1));
                Assert.That(result.MissingFileCount, Is.Zero);
                Assert.That(result.CorruptFileCount, Is.Zero);
                Assert.That(result.ManifestChecks, Has.All.Matches<MirrorVerificationCheckResult>(check => check.IsValid));
                Assert.That(result.Files[0].Action, Is.EqualTo(MirrorAction.Unchanged));
                Assert.That(result.Files[0].Checks.Where(check => check.WasEvaluated), Has.All.Matches<MirrorVerificationCheckResult>(check => check.IsValid));
                Assert.That(Check(result.Files[0], MirrorVerificationCheck.ArchiveReadability).Code, Is.EqualTo("MIRROR_ARCHIVE_READABLE"));
                Assert.That(Check(result.Files[0], MirrorVerificationCheck.ExtractedArtifacts).Code, Is.EqualTo("MIRROR_EXTRACTED_ARTIFACTS_PRESENT"));
                Assert.That(Check(result.Files[0], MirrorVerificationCheck.ParserState).Code, Is.EqualTo("MIRROR_PARSE_COMPLETE"));
                Assert.That(File.ReadAllText(archivePath), Is.EqualTo(before));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void MissingLocalFileIsClassifiedWithoutAttemptingDependentChecks()
    {
        string directory = CreateDirectory();
        try
        {
            DatasetManifest manifest = Manifest(new[] { ManifestFile("missing.bin", "files/missing.bin", Array.Empty<string>(), Array.Empty<DatasetManifestStage>()) });

            MirrorVerificationResult result = MirrorVerifier.Verify(Request(directory, manifest));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(result.MissingFileCount, Is.EqualTo(1));
                Assert.That(result.Files[0].Action, Is.EqualTo(MirrorAction.Missing));
                Assert.That(Check(result.Files[0], MirrorVerificationCheck.FileExistence).Code, Is.EqualTo("MIRROR_FILE_MISSING"));
                Assert.That(Check(result.Files[0], MirrorVerificationCheck.Checksum).WasEvaluated, Is.False);
                Assert.That(Check(result.Files[0], MirrorVerificationCheck.ArchiveReadability).WasEvaluated, Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void SizeAndDigestMismatchesAreReportedIndependently()
    {
        string directory = CreateDirectory();
        try
        {
            string path = Path.Combine(directory, "files/data.bin");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "actual");
            DatasetManifest manifest = Manifest(new[] { ManifestFile("data.bin", "files/data.bin", Array.Empty<string>(), Array.Empty<DatasetManifestStage>()) });
            DatasetManifestFile file = manifest.Products[0].Files[0];
            file.Size = 100L;
            file.HasSize = true;
            file.DigestAlgorithm = ChecksumAlgorithm.Sha256;
            file.Digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("expected"))).ToLowerInvariant();
            file.DigestVerified = true;

            MirrorFileVerificationResult result = MirrorVerifier.Verify(Request(directory, manifest)).Files[0];

            Assert.Multiple(() =>
            {
                Assert.That(result.Action, Is.EqualTo(MirrorAction.Corrupt));
                Assert.That(Check(result, MirrorVerificationCheck.FileSize).Code, Is.EqualTo("MIRROR_SIZE_MISMATCH"));
                Assert.That(Check(result, MirrorVerificationCheck.Checksum).Code, Is.EqualTo("MIRROR_CHECKSUM_MISMATCH"));
                Assert.That(Check(result, MirrorVerificationCheck.ArchiveReadability).WasEvaluated, Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void MalformedArchiveBecomesAFileFailureInsteadOfAbortingVerification()
    {
        string directory = CreateDirectory();
        try
        {
            string path = Path.Combine(directory, "files/broken.zip");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "not a zip archive");
            DatasetManifest manifest = Manifest(new[] { ManifestFile("broken.zip", "files/broken.zip", Array.Empty<string>(), Array.Empty<DatasetManifestStage>()) });
            SetFileEvidence(manifest.Products[0].Files[0], path, Digest(path));

            MirrorVerificationResult verification = null!;
            Assert.DoesNotThrow(() => verification = MirrorVerifier.Verify(Request(directory, manifest)));

            Assert.Multiple(() =>
            {
                Assert.That(verification.Files[0].Action, Is.EqualTo(MirrorAction.Corrupt));
                Assert.That(Check(verification.Files[0], MirrorVerificationCheck.ArchiveReadability).Code, Is.EqualTo("MIRROR_ARCHIVE_UNREADABLE"));
                Assert.That(Check(verification.Files[0], MirrorVerificationCheck.Checksum).IsValid, Is.True);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void MissingExtractedArtifactAndFailedParserStateAreBothReported()
    {
        string directory = CreateDirectory();
        try
        {
            string path = Path.Combine(directory, "files/data.bin");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "content");
            DatasetManifest manifest = Manifest(new[]
            {
                ManifestFile("data.bin", "files/data.bin", new[] { "extracted/missing.txt" }, new[]
                {
                    Stage("download", MirrorStageState.Complete),
                    Stage("checksum", MirrorStageState.Complete),
                    Stage("extract", MirrorStageState.Complete),
                    Stage("parse", MirrorStageState.Failed)
                })
            });
            SetFileEvidence(manifest.Products[0].Files[0], path, Digest(path));

            MirrorFileVerificationResult result = MirrorVerifier.Verify(Request(directory, manifest)).Files[0];

            Assert.Multiple(() =>
            {
                Assert.That(result.Action, Is.EqualTo(MirrorAction.Corrupt));
                Assert.That(Check(result, MirrorVerificationCheck.ExtractedArtifacts).Code, Is.EqualTo("MIRROR_EXTRACTED_ARTIFACT_MISSING"));
                Assert.That(Check(result, MirrorVerificationCheck.ParserState).Code, Is.EqualTo("MIRROR_PARSE_INCOMPLETE"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TrackedIncompletePipelineStagesCannotVerifyAsHealthy()
    {
        string directory = CreateDirectory();
        try
        {
            string path = Path.Combine(directory, "files/data.bin");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "content");
            DatasetManifest manifest = Manifest(new[]
            {
                ManifestFile("data.bin", "files/data.bin", Array.Empty<string>(), new[]
                {
                    Stage("download", MirrorStageState.Failed),
                    Stage("checksum", MirrorStageState.Failed),
                    Stage("extract", MirrorStageState.NotStarted),
                    Stage("parse", MirrorStageState.NotStarted)
                })
            });

            MirrorFileVerificationResult result = MirrorVerifier.Verify(Request(directory, manifest)).Files[0];

            Assert.Multiple(() =>
            {
                Assert.That(result.Action, Is.EqualTo(MirrorAction.Corrupt));
                Assert.That(Check(result, MirrorVerificationCheck.FileExistence).Code, Is.EqualTo("MIRROR_DOWNLOAD_INCOMPLETE"));
                Assert.That(Check(result, MirrorVerificationCheck.Checksum).Code, Is.EqualTo("MIRROR_CHECKSUM_INCOMPLETE"));
                Assert.That(Check(result, MirrorVerificationCheck.ExtractedArtifacts).Code, Is.EqualTo("MIRROR_EXTRACTION_INCOMPLETE"));
                Assert.That(Check(result, MirrorVerificationCheck.ParserState).Code, Is.EqualTo("MIRROR_PARSE_INCOMPLETE"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ManifestConsistencyReportsIdentityPathAndStageDependencyDefects()
    {
        string directory = CreateDirectory();
        try
        {
            DatasetManifestFile first = ManifestFile("first.bin", "files/shared.bin", Array.Empty<string>(), new[]
            {
                Stage("download", MirrorStageState.Failed),
                Stage("checksum", MirrorStageState.Complete)
            });
            first.ProductIdentifier = "OTHER";
            DatasetManifestFile second = ManifestFile("second.bin", "files/shared.bin", Array.Empty<string>(), Array.Empty<DatasetManifestStage>());
            DatasetManifest manifest = Manifest(new[] { first, second });

            MirrorVerificationResult result = MirrorVerifier.Verify(Request(directory, manifest));
            string[] codes = result.ManifestChecks.Select(check => check.Code!).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(result.IsValid, Is.False);
                Assert.That(codes, Does.Contain("MANIFEST_PRODUCT_MISMATCH"));
                Assert.That(codes, Does.Contain("MANIFEST_PATH_DUPLICATE"));
                Assert.That(codes, Does.Contain("MANIFEST_STAGE_DEPENDENCY_INVALID"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CancellationStopsVerificationBeforeFilesystemReads()
    {
        string directory = CreateDirectory();
        try
        {
            DatasetManifest manifest = Manifest(new[] { ManifestFile("data.bin", "files/data.bin", Array.Empty<string>(), Array.Empty<DatasetManifestStage>()) });
            MirrorVerificationRequest request = Request(directory, manifest);
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            request.Context.CancellationToken = cancellation.Token;

            Assert.Throws<OperationCanceledException>(() => MirrorVerifier.Verify(request));
            Assert.That(Directory.GetFiles(directory, "*", SearchOption.AllDirectories), Is.Empty);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static MirrorVerificationRequest Request(string directory, DatasetManifest manifest)
    {
        return new MirrorVerificationRequest
        {
            MirrorDirectory = directory,
            Manifest = manifest,
            VerifyChecksums = true,
            VerifyArchives = true,
            ChecksumBufferSize = 4,
            ArchiveLimits = new ExtractionLimits(),
            Context = new RequestContext()
        };
    }

    private static DatasetManifest Manifest(DatasetManifestFile[] files)
    {
        PatentDataProvenance provenance = new PatentDataProvenance { Source = DataProvenance.LocalMirror, IsAuthoritative = false };
        return new DatasetManifest
        {
            ManifestIdentifier = "verify",
            Provenance = provenance,
            Products = new[]
            {
                new DatasetManifestProduct
                {
                    ProductIdentifier = "PRODUCT",
                    Family = DatasetFamily.OfficeAction,
                    Files = files,
                    Provenance = provenance
                }
            }
        };
    }

    private static DatasetManifestFile ManifestFile(string fileName, string localPath, string[] extractedPaths, DatasetManifestStage[] stages)
    {
        return new DatasetManifestFile
        {
            ProductIdentifier = "PRODUCT",
            FileName = fileName,
            LocalPath = localPath,
            ExtractedPaths = extractedPaths,
            Stages = stages,
            Provenance = new PatentDataProvenance { Source = DataProvenance.LocalMirror, IsAuthoritative = false }
        };
    }

    private static DatasetManifestStage Stage(string name, MirrorStageState state)
    {
        return new DatasetManifestStage { Name = name, State = state };
    }

    private static void SetFileEvidence(DatasetManifestFile file, string path, string digest)
    {
        file.Size = new FileInfo(path).Length;
        file.HasSize = true;
        file.DigestAlgorithm = ChecksumAlgorithm.Sha256;
        file.Digest = digest;
        file.DigestVerified = true;
    }

    private static MirrorVerificationCheckResult Check(MirrorFileVerificationResult file, MirrorVerificationCheck check)
    {
        return file.Checks.Single(result => result.Check == check);
    }

    private static string Digest(string path)
    {
        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    }

    private static void CreateZip(string path, string entryName, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using FileStream output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, false);
        ZipArchiveEntry entry = archive.CreateEntry(entryName);
        using Stream stream = entry.Open();
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string CreateDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "uspto-mirror-verify-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
