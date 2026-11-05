using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace iso20022;

/// <summary>Stores immutable verified artifacts beneath caller-selected filesystem roots.</summary>
public class FileArtifactStore : IArtifactStore
{
    /// <summary>Initializes a filesystem artifact store.</summary>
    public FileArtifactStore()
    {
    }

    /// <summary>Atomically stores one artifact stream and its manifest.</summary>
    /// <param name="input">The content stream to store.</param><param name="request">The explicit root, manifest, and operation policy.</param><returns>The artifact-store result.</returns>
    public ArtifactStoreResult StoreArtifactStream(Stream? input, ArtifactStoreRequest? request)
    {
        if (input == null)
        {
            return CreateFailure(
                "artifact.store.input_missing",
                "An artifact content stream is required.",
                string.Empty);
        }

        if (request == null)
        {
            return CreateFailure(
                "artifact.store.request_missing",
                "An artifact-store request is required.",
                string.Empty);
        }

        ArtifactStoreResult? validation = ValidateStoreRequest(request);
        if (validation != null)
        {
            return validation;
        }

        request.CancellationToken.ThrowIfCancellationRequested();
        return StoreValidatedArtifact(input, request);
    }

    /// <summary>Opens one verified artifact by coordinate.</summary>
    /// <param name="request">The explicit root, coordinate, and operation policy.</param><returns>The artifact-store result.</returns>
    public ArtifactStoreResult OpenArtifact(ArtifactStoreRequest? request)
    {
        if (request == null)
        {
            return CreateFailure(
                "artifact.store.request_missing",
                "An artifact-store request is required.",
                string.Empty);
        }

        ArtifactStoreResult? validation = ValidateOpenRequest(request);
        if (validation != null)
        {
            return validation;
        }

        request.CancellationToken.ThrowIfCancellationRequested();
        return OpenValidatedArtifact(request);
    }

    internal static ArtifactStoreResult StoreValidatedArtifact(
        Stream input,
        ArtifactStoreRequest request)
    {
        string artifactDirectory = ResolveArtifactDirectory(request.RootPath, request.Coordinate);
        string parentDirectory = Path.GetDirectoryName(artifactDirectory) ?? string.Empty;
        Directory.CreateDirectory(parentDirectory);
        string stagingDirectory = Path.Combine(
            parentDirectory,
            ".staging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingDirectory);
        string stagingContentPath = Path.Combine(stagingDirectory, "artifact.bin");
        ArtifactHashOperationResult copied = CopyAndHashArtifact(
            input,
            stagingContentPath,
            request.MaxArtifactBytes,
            request);
        if (copied.Cancelled)
        {
            DeleteStagingDirectory(stagingDirectory);
            request.CancellationToken.ThrowIfCancellationRequested();
        }

        if (!copied.Success)
        {
            DeleteStagingDirectory(stagingDirectory);
            return CreateFailure(copied.ErrorCode, copied.ErrorMessage, stagingContentPath);
        }

        if (request.Artifact.Hash.Value.Length > 0 &&
            !HashesEqual(request.Artifact.Hash.Value, copied.Hash))
        {
            DeleteStagingDirectory(stagingDirectory);
            return CreateFailure(
                "artifact.store.hash_mismatch",
                "The artifact content does not match the supplied SHA-256 hash.",
                stagingContentPath);
        }

        ArtifactManifest storedManifest = CloneManifestWithHash(request.Artifact, copied.Hash);
        ArtifactManifestEncodeResult encoded = ArtifactManifestCodec.EncodeManifest(storedManifest);
        if (!encoded.Success)
        {
            DeleteStagingDirectory(stagingDirectory);
            return CreateFailure(encoded.ErrorCode, encoded.ErrorMessage, stagingContentPath);
        }

        string stagingManifestPath = Path.Combine(stagingDirectory, "manifest.dat");
        WriteManifestFile(stagingManifestPath, encoded.Lines);
        if (request.CancellationToken.IsCancellationRequested)
        {
            DeleteStagingDirectory(stagingDirectory);
            request.CancellationToken.ThrowIfCancellationRequested();
        }

        if (Directory.Exists(artifactDirectory))
        {
            ArtifactStoreResult collision = ResolveExistingCoordinate(
                artifactDirectory,
                stagingDirectory,
                copied,
                request);
            return collision;
        }

        Directory.Move(stagingDirectory, artifactDirectory);
        ArtifactStoreResult result = new ArtifactStoreResult();
        result.Success = true;
        result.Found = true;
        result.Artifact = storedManifest;
        result.StoragePath = Path.Combine(artifactDirectory, "artifact.bin");
        result.ByteCount = copied.ByteCount;
        return result;
    }

    internal static ArtifactStoreResult OpenValidatedArtifact(ArtifactStoreRequest request)
    {
        string artifactDirectory = ResolveArtifactDirectory(request.RootPath, request.Coordinate);
        string contentPath = Path.Combine(artifactDirectory, "artifact.bin");
        string manifestPath = Path.Combine(artifactDirectory, "manifest.dat");
        if (!Directory.Exists(artifactDirectory))
        {
            ArtifactStoreResult missing = new ArtifactStoreResult();
            missing.Success = true;
            missing.StoragePath = contentPath;
            return missing;
        }

        if (!File.Exists(contentPath) || !File.Exists(manifestPath))
        {
            return CreateFailure(
                "artifact.store.incomplete",
                "The stored coordinate does not contain both artifact content and a manifest.",
                contentPath);
        }

        FileInfo manifestInfo = new FileInfo(manifestPath);
        if (manifestInfo.Length > 1048576)
        {
            return CreateFailure(
                "artifact.store.manifest_too_large",
                "The stored artifact manifest exceeds the supported size.",
                contentPath);
        }

        ArtifactManifestDecodeResult decoded = ArtifactManifestCodec.DecodeManifest(
            File.ReadAllLines(manifestPath, Encoding.UTF8));
        if (!decoded.Success)
        {
            return CreateFailure(decoded.ErrorCode, decoded.ErrorMessage, contentPath);
        }

        if (!CoordinatesEqual(decoded.Manifest.Coordinate, request.Coordinate))
        {
            return CreateFailure(
                "artifact.store.coordinate_mismatch",
                "The stored manifest coordinate does not match its storage coordinate.",
                contentPath);
        }

        FileStream content = new FileStream(
            contentPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        if (content.Length > request.MaxArtifactBytes)
        {
            content.Dispose();
            return CreateFailure(
                "artifact.store.size_limit",
                "The stored artifact exceeds the configured byte limit.",
                contentPath);
        }

        if (request.VerifyHash)
        {
            if (!IsSha256(decoded.Manifest.HashAlgorithm) || decoded.Manifest.Hash.Value.Length != 32)
            {
                content.Dispose();
                return CreateFailure(
                    "artifact.store.hash_algorithm_unsupported",
                    "Stored artifact verification requires a SHA-256 manifest hash.",
                    contentPath);
            }

            ArtifactHashOperationResult hashed = HashOpenContent(content, request);
            if (hashed.Cancelled)
            {
                content.Dispose();
                request.CancellationToken.ThrowIfCancellationRequested();
            }

            if (!HashesEqual(decoded.Manifest.Hash.Value, hashed.Hash))
            {
                content.Dispose();
                return CreateFailure(
                    "artifact.store.content_hash_mismatch",
                    "The stored artifact content does not match its manifest hash.",
                    contentPath);
            }

            content.Position = 0;
        }

        ArtifactStoreResult result = new ArtifactStoreResult();
        result.Success = true;
        result.Found = true;
        result.Content = content;
        result.Artifact = decoded.Manifest;
        result.StoragePath = contentPath;
        result.ByteCount = content.Length;
        return result;
    }

    internal static ArtifactStoreResult ResolveExistingCoordinate(
        string artifactDirectory,
        string stagingDirectory,
        ArtifactHashOperationResult copied,
        ArtifactStoreRequest request)
    {
        ArtifactStoreResult existing = OpenExistingForCollision(request);
        Stream? existingContent = existing.Content;
        if (existingContent != null)
        {
            existingContent.Dispose();
            existing.Content = null;
        }

        if (!existing.Success || !existing.Found)
        {
            DeleteStagingDirectory(stagingDirectory);
            return existing;
        }

        bool sameContent = HashesEqual(existing.Artifact.Hash.Value, copied.Hash);
        if (sameContent && request.AllowIdenticalArtifactReuse)
        {
            DeleteStagingDirectory(stagingDirectory);
            existing.ReusedExistingArtifact = true;
            return existing;
        }

        DeleteStagingDirectory(stagingDirectory);
        return CreateFailure(
            "artifact.store.coordinate_collision",
            "The artifact coordinate is already occupied and cannot be overwritten.",
            Path.Combine(artifactDirectory, "artifact.bin"));
    }

    internal static ArtifactHashOperationResult CopyAndHashArtifact(
        Stream input,
        string path,
        int maximumBytes,
        ArtifactStoreRequest request)
    {
        ArtifactHashOperationResult result = new ArtifactHashOperationResult();
        byte[] buffer = new byte[81920];
        using (IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        using (FileStream output = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None))
        {
            int read = input.Read(buffer, 0, buffer.Length);
            while (read > 0)
            {
                if (request.CancellationToken.IsCancellationRequested)
                {
                    result.Cancelled = true;
                    return result;
                }

                if (result.ByteCount > maximumBytes - read)
                {
                    result.ErrorCode = "artifact.store.size_limit";
                    result.ErrorMessage = "The artifact exceeds the configured byte limit.";
                    return result;
                }

                output.Write(buffer, 0, read);
                hash.AppendData(buffer, 0, read);
                result.ByteCount = result.ByteCount + read;
                read = input.Read(buffer, 0, buffer.Length);
            }

            output.Flush(true);
            result.Hash = hash.GetHashAndReset();
        }

        result.Success = true;
        return result;
    }

    internal static ArtifactStoreResult OpenExistingForCollision(ArtifactStoreRequest request)
    {
        ArtifactStoreRequest verifiedRequest = new ArtifactStoreRequest();
        verifiedRequest.RootPath = request.RootPath;
        verifiedRequest.Coordinate = request.Coordinate;
        verifiedRequest.VerifyHash = true;
        verifiedRequest.MaxArtifactBytes = request.MaxArtifactBytes;
        verifiedRequest.CancellationToken = request.CancellationToken;
        return OpenValidatedArtifact(verifiedRequest);
    }

    internal static ArtifactHashOperationResult HashOpenContent(
        FileStream content,
        ArtifactStoreRequest request)
    {
        ArtifactHashOperationResult result = new ArtifactHashOperationResult();
        byte[] buffer = new byte[81920];
        using (IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            int read = content.Read(buffer, 0, buffer.Length);
            while (read > 0)
            {
                if (request.CancellationToken.IsCancellationRequested)
                {
                    result.Cancelled = true;
                    return result;
                }

                hash.AppendData(buffer, 0, read);
                result.ByteCount = result.ByteCount + read;
                read = content.Read(buffer, 0, buffer.Length);
            }

            result.Hash = hash.GetHashAndReset();
        }

        result.Success = true;
        return result;
    }

    internal static void WriteManifestFile(string path, string[] lines)
    {
        using (FileStream file = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None))
        using (StreamWriter writer = new StreamWriter(file, new UTF8Encoding(false), 1024, true))
        {
            foreach (string line in lines)
            {
                writer.WriteLine(line);
            }

            writer.Flush();
            file.Flush(true);
        }
    }

    internal static ArtifactStoreResult? ValidateStoreRequest(ArtifactStoreRequest request)
    {
        ArtifactStoreResult? common = ValidateCommonRequest(request);
        if (common != null)
        {
            return common;
        }

        if (request.Artifact == null || request.Artifact.Coordinate == null ||
            request.Artifact.Hash == null || request.Artifact.Hash.Value == null)
        {
            return CreateFailure(
                "artifact.store.manifest_invalid",
                "A complete artifact manifest is required when storing content.",
                string.Empty);
        }

        if (!CoordinatesEqual(request.Artifact.Coordinate, request.Coordinate))
        {
            return CreateFailure(
                "artifact.store.coordinate_mismatch",
                "The request coordinate and manifest coordinate must be identical.",
                string.Empty);
        }

        ArtifactManifestEncodeResult encoded = ArtifactManifestCodec.EncodeManifest(request.Artifact);
        if (!encoded.Success)
        {
            return CreateFailure(encoded.ErrorCode, encoded.ErrorMessage, string.Empty);
        }

        if (request.Artifact.HashAlgorithm.Length > 0 &&
            !IsSha256(request.Artifact.HashAlgorithm))
        {
            return CreateFailure(
                "artifact.store.hash_algorithm_unsupported",
                "Artifact storage supports SHA-256 manifest hashes only.",
                string.Empty);
        }

        return null;
    }

    internal static ArtifactStoreResult? ValidateOpenRequest(ArtifactStoreRequest request)
    {
        return ValidateCommonRequest(request);
    }

    internal static ArtifactStoreResult? ValidateCommonRequest(ArtifactStoreRequest request)
    {
        if (request.RootPath == null || request.RootPath.Length == 0)
        {
            return CreateFailure(
                "artifact.store.root_missing",
                "A caller-selected artifact-store root is required.",
                string.Empty);
        }

        if (!Path.IsPathFullyQualified(request.RootPath))
        {
            return CreateFailure(
                "artifact.store.root_not_absolute",
                "The artifact-store root must be an absolute caller-selected path.",
                string.Empty);
        }

        if (request.MaxArtifactBytes <= 0)
        {
            return CreateFailure(
                "artifact.store.limit_invalid",
                "The maximum artifact byte count must be positive.",
                string.Empty);
        }

        if (request.Coordinate == null || !CoordinateIsValid(request.Coordinate))
        {
            return CreateFailure(
                "artifact.store.coordinate_invalid",
                "The artifact coordinate kind, edition, identifier, and version are required and bounded.",
                string.Empty);
        }

        return null;
    }

    internal static bool CoordinateIsValid(ArtifactCoordinate coordinate)
    {
        return coordinate.Kind != ArtifactKind.Unknown &&
            StringIsCoordinateComponentValid(coordinate.Edition) &&
            StringIsCoordinateComponentValid(coordinate.Identifier) &&
            StringIsCoordinateComponentValid(coordinate.Version);
    }

    internal static bool StringIsCoordinateComponentValid(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        if (!StringHasValidUtf16(value))
        {
            return false;
        }

        return Encoding.UTF8.GetByteCount(value) <= 120;
    }

    internal static bool StringHasValidUtf16(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character >= 55296 && character <= 56319)
            {
                if (index + 1 >= value.Length ||
                    value[index + 1] < 56320 || value[index + 1] > 57343)
                {
                    return false;
                }

                index = index + 2;
                continue;
            }

            if (character >= 56320 && character <= 57343)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool CoordinatesEqual(ArtifactCoordinate first, ArtifactCoordinate second)
    {
        return first.Kind == second.Kind &&
            first.Edition == second.Edition &&
            first.Identifier == second.Identifier &&
            first.Version == second.Version;
    }

    internal static string ResolveArtifactDirectory(string rootPath, ArtifactCoordinate coordinate)
    {
        string root = Path.GetFullPath(rootPath);
        string kind = "k-" + IntegerLexical.FormatInt32((int)coordinate.Kind);
        return Path.Combine(
            root,
            "artifacts",
            kind,
            "e-" + EncodeCoordinateComponent(coordinate.Edition),
            "i-" + EncodeCoordinateComponent(coordinate.Identifier),
            "v-" + EncodeCoordinateComponent(coordinate.Version));
    }

    internal static string EncodeCoordinateComponent(string value)
    {
        return BinaryLexical.FormatHexadecimal(Encoding.UTF8.GetBytes(value));
    }

    internal static bool HashesEqual(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(first, second);
    }

    internal static bool IsSha256(string value)
    {
        return string.Equals(value, "SHA-256", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "SHA256", StringComparison.OrdinalIgnoreCase);
    }

    internal static ArtifactManifest CloneManifestWithHash(ArtifactManifest source, byte[] hash)
    {
        ArtifactManifest result = new ArtifactManifest();
        result.Coordinate = new ArtifactCoordinate();
        result.Coordinate.Kind = source.Coordinate.Kind;
        result.Coordinate.Edition = source.Coordinate.Edition;
        result.Coordinate.Identifier = source.Coordinate.Identifier;
        result.Coordinate.Version = source.Coordinate.Version;
        result.OriginUri = source.OriginUri;
        result.RetrievedAt = source.RetrievedAt;
        result.PublicationDate = source.PublicationDate;
        result.Hash = new IsoBinaryValue();
        result.Hash.Value = hash;
        result.Hash.Encoding = BinaryEncodingKind.Hexadecimal;
        result.Hash.LexicalValue = BinaryLexical.FormatHexadecimal(hash);
        result.HashAlgorithm = "SHA-256";
        result.MediaType = source.MediaType;
        result.LicenseNotice = source.LicenseNotice;
        result.IsApproved = source.IsApproved;
        return result;
    }

    internal static void DeleteStagingDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
    }

    internal static ArtifactStoreResult CreateFailure(
        string errorCode,
        string errorMessage,
        string storagePath)
    {
        ArtifactStoreResult result = new ArtifactStoreResult();
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        result.StoragePath = storagePath;
        return result;
    }
}
