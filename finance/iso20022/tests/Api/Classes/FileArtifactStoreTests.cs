using System.Security.Cryptography;
using iso20022;

namespace iso20022.Tests;

public class FileArtifactStoreTests
{
    [Test]
    public void StoreAndOpenRoundTripContentManifestAndEncodedCoordinate()
    {
        string root = CreateTemporaryRoot();
        try
        {
            byte[] bytes = "official external code catalogue"u8.ToArray();
            ArtifactStoreRequest request = CreateRequest(root, "../2026/Q1", "codes\\payments", ":v1");
            request.Artifact.HashAlgorithm = "SHA-256";
            request.Artifact.Hash.Value = SHA256.HashData(bytes);
            IArtifactStore store = new FileArtifactStore();

            ArtifactStoreResult stored;
            using (MemoryStream input = new MemoryStream(bytes))
            {
                stored = store.StoreArtifactStream(input, request);
            }

            ArtifactStoreResult opened = store.OpenArtifact(request);
            ArtifactStoreRequest distinctRequest = CreateRequest(root, "../2026\\Q1", "codes/payments", ":v1");
            ArtifactStoreResult distinct;
            using (MemoryStream input = new MemoryStream(bytes))
            {
                distinct = store.StoreArtifactStream(input, distinctRequest);
            }

            using Stream openedContent = opened.Content ?? throw new AssertionException("Expected opened content.");
            using MemoryStream copy = new MemoryStream();
            openedContent.CopyTo(copy);

            Assert.Multiple(() =>
            {
                Assert.That(stored.Success, Is.True);
                Assert.That(stored.Found, Is.True);
                Assert.That(stored.ByteCount, Is.EqualTo(bytes.Length));
                Assert.That(Path.GetFullPath(stored.StoragePath), Does.StartWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar));
                Assert.That(stored.StoragePath, Does.Not.Contain("../2026/Q1"));
                Assert.That(distinct.Success, Is.True);
                Assert.That(distinct.StoragePath, Is.Not.EqualTo(stored.StoragePath));
                Assert.That(stored.Artifact.HashAlgorithm, Is.EqualTo("SHA-256"));
                Assert.That(stored.Artifact.Hash.Value, Is.EqualTo(SHA256.HashData(bytes)));
                Assert.That(opened.Success, Is.True);
                Assert.That(opened.Found, Is.True);
                Assert.That(copy.ToArray(), Is.EqualTo(bytes));
                Assert.That(opened.Artifact.Coordinate.Edition, Is.EqualTo("../2026/Q1"));
                Assert.That(opened.Artifact.Coordinate.Identifier, Is.EqualTo("codes\\payments"));
                Assert.That(opened.Artifact.OriginUri, Is.EqualTo("https://example.test/catalogue.zip"));
                Assert.That(opened.Artifact.RetrievedAt?.LexicalValue, Is.EqualTo("2026-05-27T13:28:00Z"));
                Assert.That(opened.Artifact.RetrievedAt?.Date.Year, Is.EqualTo(2026));
                Assert.That(opened.Artifact.RetrievedAt?.Time.Hour, Is.EqualTo(13));
                Assert.That(opened.Artifact.PublicationDate?.Kind, Is.EqualTo(PartialDateKind.YearMonth));
                Assert.That(opened.Artifact.PublicationDate?.Month, Is.EqualTo(5));
                Assert.That(opened.Artifact.MediaType, Is.EqualTo(Iso20022MediaTypes.Zip));
                Assert.That(opened.Artifact.LicenseNotice, Is.EqualTo("ISO publication terms"));
                Assert.That(opened.Artifact.IsApproved, Is.True);
                Assert.That(FindStagingDirectories(root), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void ExistingCoordinateReusesIdenticalContentAndRejectsDifferentContent()
    {
        string root = CreateTemporaryRoot();
        try
        {
            byte[] firstBytes = "first artifact"u8.ToArray();
            byte[] secondBytes = "different artifact"u8.ToArray();
            ArtifactStoreRequest request = CreateRequest(root, "2026-Q1", "external-codes", "1");
            IArtifactStore store = new FileArtifactStore();

            ArtifactStoreResult first;
            ArtifactStoreResult repeated;
            ArtifactStoreResult reuseDisabled;
            ArtifactStoreResult collision;
            using (MemoryStream input = new MemoryStream(firstBytes))
            {
                first = store.StoreArtifactStream(input, request);
            }

            using (MemoryStream input = new MemoryStream(firstBytes))
            {
                repeated = store.StoreArtifactStream(input, request);
            }

            request.AllowIdenticalArtifactReuse = false;
            using (MemoryStream input = new MemoryStream(firstBytes))
            {
                reuseDisabled = store.StoreArtifactStream(input, request);
            }

            request.AllowIdenticalArtifactReuse = true;
            using (MemoryStream input = new MemoryStream(secondBytes))
            {
                collision = store.StoreArtifactStream(input, request);
            }

            ArtifactStoreResult opened = store.OpenArtifact(request);
            using Stream openedContent = opened.Content ?? throw new AssertionException("Expected original content.");
            using MemoryStream copy = new MemoryStream();
            openedContent.CopyTo(copy);

            Assert.Multiple(() =>
            {
                Assert.That(first.Success, Is.True);
                Assert.That(repeated.Success, Is.True);
                Assert.That(repeated.ReusedExistingArtifact, Is.True);
                Assert.That(reuseDisabled.Success, Is.False);
                Assert.That(reuseDisabled.ErrorCode, Is.EqualTo("artifact.store.coordinate_collision"));
                Assert.That(collision.Success, Is.False);
                Assert.That(collision.ErrorCode, Is.EqualTo("artifact.store.coordinate_collision"));
                Assert.That(copy.ToArray(), Is.EqualTo(firstBytes));
                Assert.That(FindStagingDirectories(root), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void HashAndSizeFailuresDoNotPublishPartialCoordinates()
    {
        string root = CreateTemporaryRoot();
        try
        {
            byte[] bytes = "bounded artifact"u8.ToArray();
            ArtifactStoreRequest hashRequest = CreateRequest(root, "2026-Q1", "bad-hash", "1");
            hashRequest.Artifact.HashAlgorithm = "SHA-256";
            hashRequest.Artifact.Hash.Value = new byte[32];
            ArtifactStoreRequest sizeRequest = CreateRequest(root, "2026-Q1", "too-large", "1");
            sizeRequest.MaxArtifactBytes = bytes.Length - 1;
            IArtifactStore store = new FileArtifactStore();

            ArtifactStoreResult badHash;
            ArtifactStoreResult tooLarge;
            using (MemoryStream input = new MemoryStream(bytes))
            {
                badHash = store.StoreArtifactStream(input, hashRequest);
            }

            using (MemoryStream input = new MemoryStream(bytes))
            {
                tooLarge = store.StoreArtifactStream(input, sizeRequest);
            }

            ArtifactStoreResult missingHashCoordinate = store.OpenArtifact(hashRequest);
            ArtifactStoreResult missingSizeCoordinate = store.OpenArtifact(sizeRequest);

            Assert.Multiple(() =>
            {
                Assert.That(badHash.Success, Is.False);
                Assert.That(badHash.ErrorCode, Is.EqualTo("artifact.store.hash_mismatch"));
                Assert.That(tooLarge.Success, Is.False);
                Assert.That(tooLarge.ErrorCode, Is.EqualTo("artifact.store.size_limit"));
                Assert.That(missingHashCoordinate.Success, Is.True);
                Assert.That(missingHashCoordinate.Found, Is.False);
                Assert.That(missingSizeCoordinate.Success, Is.True);
                Assert.That(missingSizeCoordinate.Found, Is.False);
                Assert.That(FindStagingDirectories(root), Is.Empty);
            });
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void OpenDetectsContentTamperingUnlessVerificationIsExplicitlyDisabled()
    {
        string root = CreateTemporaryRoot();
        try
        {
            ArtifactStoreRequest request = CreateRequest(root, "2026-Q1", "tamper-test", "1");
            IArtifactStore store = new FileArtifactStore();
            ArtifactStoreResult stored;
            using (MemoryStream input = new MemoryStream("original"u8.ToArray()))
            {
                stored = store.StoreArtifactStream(input, request);
            }

            File.WriteAllBytes(stored.StoragePath, "tampered"u8.ToArray());
            ArtifactStoreResult verified = store.OpenArtifact(request);
            request.VerifyHash = false;
            ArtifactStoreResult unverified = store.OpenArtifact(request);

            Assert.Multiple(() =>
            {
                Assert.That(verified.Success, Is.False);
                Assert.That(verified.ErrorCode, Is.EqualTo("artifact.store.content_hash_mismatch"));
                Assert.That(verified.Content, Is.Null);
                Assert.That(unverified.Success, Is.True);
                Assert.That(unverified.Found, Is.True);
                Assert.That(unverified.ByteCount, Is.EqualTo(8));
            });

            unverified.Content?.Dispose();
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void StoreRequiresAnExplicitAbsoluteRootAndReturnsMissingWithoutCreatingIt()
    {
        IArtifactStore store = new FileArtifactStore();
        ArtifactStoreRequest relativeRequest = CreateRequest("relative-cache", "2026-Q1", "codes", "1");
        string missingRoot = Path.Combine(
            Path.GetTempPath(),
            "iso20022-artifact-store-missing-" + Guid.NewGuid().ToString("N"));
        ArtifactStoreRequest missingRequest = CreateRequest(missingRoot, "2026-Q1", "codes", "1");

        ArtifactStoreResult relative;
        using (MemoryStream input = new MemoryStream(new byte[] { 1 }))
        {
            relative = store.StoreArtifactStream(input, relativeRequest);
        }

        ArtifactStoreResult missing = store.OpenArtifact(missingRequest);

        Assert.Multiple(() =>
        {
            Assert.That(relative.Success, Is.False);
            Assert.That(relative.ErrorCode, Is.EqualTo("artifact.store.root_not_absolute"));
            Assert.That(missing.Success, Is.True);
            Assert.That(missing.Found, Is.False);
            Assert.That(Directory.Exists(missingRoot), Is.False);
        });
    }

    [Test]
    public void ValidationAndCorruptStorageStatesReturnSpecificFailures()
    {
        string root = CreateTemporaryRoot();
        try
        {
            FileArtifactStore store = new FileArtifactStore();
            ArtifactStoreRequest request = CreateRequest(root, "e", "i", "v");
            ArtifactStoreRequest zeroLimit = CreateRequest(root, "e", "i", "v");
            zeroLimit.MaxArtifactBytes = 0;
            ArtifactStoreRequest missingCoordinate = CreateRequest(root, "e", "i", "v");
            missingCoordinate.Coordinate = null!;
            ArtifactStoreRequest missingManifest = CreateRequest(root, "e", "i", "v");
            missingManifest.Artifact = null!;
            ArtifactStoreRequest mismatch = CreateRequest(root, "e", "i", "v");
            mismatch.Artifact.Coordinate = new ArtifactCoordinate
                { Kind = ArtifactKind.ExternalCodeSet, Edition = "other",
                    Identifier = "i", Version = "v" };
            ArtifactStoreRequest badAlgorithm = CreateRequest(root, "e", "i", "v");
            badAlgorithm.Artifact.HashAlgorithm = "MD5";

            Assert.Multiple(() =>
            {
                Assert.That(store.OpenArtifact(new ArtifactStoreRequest()).ErrorCode,
                    Is.EqualTo("artifact.store.root_missing"));
                Assert.That(store.OpenArtifact(zeroLimit).ErrorCode,
                    Is.EqualTo("artifact.store.limit_invalid"));
                Assert.That(store.OpenArtifact(missingCoordinate).ErrorCode,
                    Is.EqualTo("artifact.store.coordinate_invalid"));
                Assert.That(FileArtifactStore.ValidateStoreRequest(missingManifest)?.ErrorCode,
                    Is.EqualTo("artifact.store.manifest_invalid"));
                Assert.That(FileArtifactStore.ValidateStoreRequest(mismatch)?.ErrorCode,
                    Is.EqualTo("artifact.store.coordinate_mismatch"));
                Assert.That(FileArtifactStore.ValidateStoreRequest(badAlgorithm)?.ErrorCode,
                    Is.EqualTo("artifact.store.hash_algorithm_unsupported"));
                Assert.That(FileArtifactStore.StringIsCoordinateComponentValid(null), Is.False);
                Assert.That(FileArtifactStore.StringIsCoordinateComponentValid(""), Is.False);
                Assert.That(FileArtifactStore.StringIsCoordinateComponentValid(
                    new string('x', 121)), Is.False);
                Assert.That(FileArtifactStore.StringHasValidUtf16("\ud800"), Is.False);
                Assert.That(FileArtifactStore.StringHasValidUtf16("\udc00"), Is.False);
                Assert.That(FileArtifactStore.StringHasValidUtf16("\ud800x"), Is.False);
                Assert.That(FileArtifactStore.StringHasValidUtf16("\ud800\udc00"), Is.True);
                Assert.That(FileArtifactStore.HashesEqual([1], [1, 2]), Is.False);
                Assert.That(FileArtifactStore.HashesEqual([1], [2]), Is.False);
            });

            string directory = FileArtifactStore.ResolveArtifactDirectory(
                request.RootPath, request.Coordinate);
            Directory.CreateDirectory(directory);
            Assert.That(store.OpenArtifact(request).ErrorCode,
                Is.EqualTo("artifact.store.incomplete"));

            File.WriteAllBytes(Path.Combine(directory, "artifact.bin"), [1, 2]);
            File.WriteAllText(Path.Combine(directory, "manifest.dat"), "bad\n");
            Assert.That(store.OpenArtifact(request).Success, Is.False);

            File.WriteAllBytes(Path.Combine(directory, "manifest.dat"),
                new byte[1048577]);
            Assert.That(store.OpenArtifact(request).ErrorCode,
                Is.EqualTo("artifact.store.manifest_too_large"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static ArtifactStoreRequest CreateRequest(
        string root,
        string edition,
        string identifier,
        string version)
    {
        ArtifactCoordinate coordinate = new ArtifactCoordinate
        {
            Kind = ArtifactKind.ExternalCodeSet,
            Edition = edition,
            Identifier = identifier,
            Version = version
        };
        return new ArtifactStoreRequest
        {
            RootPath = root,
            Coordinate = coordinate,
            Artifact = new ArtifactManifest
            {
                Coordinate = coordinate,
                OriginUri = "https://example.test/catalogue.zip",
                RetrievedAt = new IsoDateTimeValue
                {
                    Date = new IsoDateValue
                    {
                        Year = 2026,
                        Month = 5,
                        Day = 27,
                        HasTimezone = true,
                        IsUtc = true,
                        LexicalValue = "2026-05-27Z"
                    },
                    Time = new IsoTimeValue
                    {
                        Hour = 13,
                        Minute = 28,
                        HasTimezone = true,
                        IsUtc = true,
                        LexicalValue = "13:28:00Z"
                    },
                    LexicalValue = "2026-05-27T13:28:00Z"
                },
                PublicationDate = new IsoPartialDateValue
                {
                    Kind = PartialDateKind.YearMonth,
                    Year = 2026,
                    Month = 5,
                    LexicalValue = "2026-05"
                },
                MediaType = Iso20022MediaTypes.Zip,
                LicenseNotice = "ISO publication terms",
                IsApproved = true
            },
            MaxArtifactBytes = 4096
        };
    }

    private static string CreateTemporaryRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "iso20022-artifact-store-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static string[] FindStagingDirectories(string root)
    {
        return Directory.GetDirectories(root, ".staging-*", SearchOption.AllDirectories);
    }
}
