using System.Text;
using uspto;

namespace uspto.Tests;

public class PartialDownloadMetadataStoreTests
{
    [Test]
    public void SaveAndLoadRoundTripResumeMetadata()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        byte[] partialBytes = Encoding.ASCII.GetBytes("partial-content");
        File.WriteAllBytes(partialPath, partialBytes);
        DateTimeOffset modified = new DateTimeOffset(2026, 8, 20, 11, 30, 0, TimeSpan.FromHours(-7));
        DateTimeOffset savedAt = new DateTimeOffset(2026, 9, 3, 9, 15, 0, TimeSpan.FromHours(-7));

        try
        {
            DownloadResumeState persisted = PartialDownloadMetadataStore.Save(new DownloadResumeState
            {
                SourceUri = new Uri("https://bulkdata.uspto.gov/files/dataset.zip"),
                PartialPath = partialPath,
                CompletedBytes = partialBytes.Length,
                ExpectedTotalBytes = 200L,
                HasExpectedTotalBytes = true,
                ETag = "\"dataset-v2\"",
                LastModified = modified,
                HasLastModified = true
            }, savedAt, null);
            DownloadResumeState? loaded = PartialDownloadMetadataStore.Load(partialPath);

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(PartialDownloadMetadataStore.GetMetadataPath(partialPath)), Is.True);
                Assert.That(persisted.PartialPath, Is.EqualTo(Path.GetFullPath(partialPath)));
                Assert.That(persisted.UpdatedAt, Is.EqualTo(savedAt.ToUniversalTime()));
                Assert.That(persisted.HasUpdatedAt, Is.True);
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded?.SourceUri?.AbsoluteUri, Is.EqualTo("https://bulkdata.uspto.gov/files/dataset.zip"));
                Assert.That(loaded?.PartialPath, Is.EqualTo(Path.GetFullPath(partialPath)));
                Assert.That(loaded?.CompletedBytes, Is.EqualTo(partialBytes.Length));
                Assert.That(loaded?.ExpectedTotalBytes, Is.EqualTo(200L));
                Assert.That(loaded?.HasExpectedTotalBytes, Is.True);
                Assert.That(loaded?.ETag, Is.EqualTo("\"dataset-v2\""));
                Assert.That(loaded?.LastModified, Is.EqualTo(modified.ToUniversalTime()));
                Assert.That(loaded?.HasLastModified, Is.True);
                Assert.That(loaded?.UpdatedAt, Is.EqualTo(savedAt.ToUniversalTime()));
                Assert.That(loaded?.HasUpdatedAt, Is.True);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void SaveRedactsCredentialValuesBeforeWritingMetadata()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        File.WriteAllBytes(partialPath, new byte[] { 1, 2, 3 });
        string signature = "signed-secret";
        string opaqueSecret = "opaque-secret";

        try
        {
            PartialDownloadMetadataStore.Save(new DownloadResumeState
            {
                SourceUri = new Uri("https://download.example.test/dataset.zip?X-Amz-Signature=" + signature + "&download=" + opaqueSecret),
                PartialPath = partialPath,
                CompletedBytes = 3L,
                ETag = "\"" + opaqueSecret + "\""
            }, DateTimeOffset.UtcNow, new string[] { opaqueSecret });
            string persistedText = Encoding.UTF8.GetString(File.ReadAllBytes(PartialDownloadMetadataStore.GetMetadataPath(partialPath)));
            DownloadResumeState? loaded = PartialDownloadMetadataStore.Load(partialPath);

            Assert.Multiple(() =>
            {
                Assert.That(persistedText, Does.Not.Contain(signature));
                Assert.That(persistedText, Does.Not.Contain(opaqueSecret));
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded?.SourceUri?.AbsoluteUri, Does.Contain("%5BREDACTED%5D"));
                Assert.That(loaded?.ETag, Is.EqualTo("\"[REDACTED]\""));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void RepeatedSaveAtomicallyReplacesTheSidecar()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        File.WriteAllBytes(partialPath, Encoding.ASCII.GetBytes("12345"));
        DateTimeOffset firstTime = new DateTimeOffset(2026, 9, 3, 10, 0, 0, TimeSpan.Zero);
        DateTimeOffset secondTime = firstTime.AddMinutes(5);

        try
        {
            DownloadResumeState state = new DownloadResumeState
            {
                SourceUri = new Uri("https://bulkdata.uspto.gov/dataset.zip"),
                PartialPath = partialPath,
                CompletedBytes = 5L,
                ETag = "\"first\""
            };
            PartialDownloadMetadataStore.Save(state, firstTime, null);
            state.ETag = "\"second\"";
            DownloadResumeState persisted = PartialDownloadMetadataStore.Save(state, secondTime, null);
            DownloadResumeState? loaded = PartialDownloadMetadataStore.Load(partialPath);
            string metadataPath = PartialDownloadMetadataStore.GetMetadataPath(partialPath);

            Assert.Multiple(() =>
            {
                Assert.That(persisted.ETag, Is.EqualTo("\"second\""));
                Assert.That(loaded?.ETag, Is.EqualTo("\"second\""));
                Assert.That(loaded?.UpdatedAt, Is.EqualTo(secondTime));
                Assert.That(File.Exists(metadataPath + ".tmp"), Is.False);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void LoadRejectsCorruptMetadata()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        File.WriteAllBytes(partialPath, new byte[] { 1, 2, 3 });

        try
        {
            PartialDownloadMetadataStore.Save(new DownloadResumeState
            {
                SourceUri = new Uri("https://bulkdata.uspto.gov/dataset.zip"),
                PartialPath = partialPath,
                CompletedBytes = 3L
            }, DateTimeOffset.UtcNow, null);
            string metadataPath = PartialDownloadMetadataStore.GetMetadataPath(partialPath);
            byte[] metadata = File.ReadAllBytes(metadataPath);
            metadata[metadata.Length - 1] ^= 255;
            File.WriteAllBytes(metadataPath, metadata);

            Assert.That(PartialDownloadMetadataStore.Load(partialPath), Is.Null);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void LoadRejectsMetadataWhenPartialLengthChangesOrFileIsMissing()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        File.WriteAllBytes(partialPath, new byte[] { 1, 2, 3 });

        try
        {
            PartialDownloadMetadataStore.Save(new DownloadResumeState
            {
                SourceUri = new Uri("https://bulkdata.uspto.gov/dataset.zip"),
                PartialPath = partialPath,
                CompletedBytes = 3L
            }, DateTimeOffset.UtcNow, null);
            File.AppendAllText(partialPath, "more");
            DownloadResumeState? changed = PartialDownloadMetadataStore.Load(partialPath);
            File.Delete(partialPath);
            DownloadResumeState? missing = PartialDownloadMetadataStore.Load(partialPath);

            Assert.Multiple(() =>
            {
                Assert.That(changed, Is.Null);
                Assert.That(missing, Is.Null);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void SaveValidatesPartialAndExpectedLengths()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        File.WriteAllBytes(partialPath, new byte[] { 1, 2, 3 });
        DownloadResumeState state = new DownloadResumeState
        {
            SourceUri = new Uri("https://bulkdata.uspto.gov/dataset.zip"),
            PartialPath = partialPath,
            CompletedBytes = 2L
        };

        try
        {
            ValidationException mismatch = Assert.Throws<ValidationException>(() => PartialDownloadMetadataStore.Save(state, DateTimeOffset.UtcNow, null));
            state.CompletedBytes = 3L;
            state.ExpectedTotalBytes = 2L;
            state.HasExpectedTotalBytes = true;
            ValidationException expected = Assert.Throws<ValidationException>(() => PartialDownloadMetadataStore.Save(state, DateTimeOffset.UtcNow, null));
            state.PartialPath = Path.Combine(directory, "missing.partial");
            ValidationException missing = Assert.Throws<ValidationException>(() => PartialDownloadMetadataStore.Save(state, DateTimeOffset.UtcNow, null));

            Assert.Multiple(() =>
            {
                Assert.That(mismatch.ErrorCode, Is.EqualTo("PARTIAL_LENGTH_MISMATCH"));
                Assert.That(expected.ErrorCode, Is.EqualTo("INVALID_EXPECTED_LENGTH"));
                Assert.That(missing.ErrorCode, Is.EqualTo("PARTIAL_FILE_NOT_FOUND"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void DeleteRemovesOnlyMetadataAndTemporarySidecar()
    {
        string directory = CreateTemporaryDirectory();
        string partialPath = Path.Combine(directory, "dataset.zip.partial");
        File.WriteAllBytes(partialPath, new byte[] { 1 });

        try
        {
            PartialDownloadMetadataStore.Save(new DownloadResumeState
            {
                SourceUri = new Uri("https://bulkdata.uspto.gov/dataset.zip"),
                PartialPath = partialPath,
                CompletedBytes = 1L
            }, DateTimeOffset.UtcNow, null);
            string metadataPath = PartialDownloadMetadataStore.GetMetadataPath(partialPath);
            File.WriteAllText(metadataPath + ".tmp", "temporary");
            bool first = PartialDownloadMetadataStore.Delete(partialPath);
            bool second = PartialDownloadMetadataStore.Delete(partialPath);

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.True);
                Assert.That(second, Is.False);
                Assert.That(File.Exists(metadataPath), Is.False);
                Assert.That(File.Exists(metadataPath + ".tmp"), Is.False);
                Assert.That(File.Exists(partialPath), Is.True);
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "uspto-partial-metadata-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
