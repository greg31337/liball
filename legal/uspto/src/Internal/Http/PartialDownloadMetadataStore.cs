namespace uspto;

internal static class PartialDownloadMetadataStore
{
    internal const int FormatVersion = 1;
    internal const int DigestBytes = 32;
    internal const int MaximumMetadataBytes = 65536;
    internal static readonly byte[] Magic = new byte[] { 85, 83, 80, 84, 79, 80, 65, 82, 84 };

    internal static string GetMetadataPath(string partialPath)
    {
        if (global::System.String.IsNullOrWhiteSpace(partialPath))
        {
            throw new ValidationException("A partial download path is required.", "partialPath", "REQUIRED", null);
        }

        return global::System.IO.Path.GetFullPath(partialPath) + ".metadata";
    }

    internal static DownloadResumeState Save(DownloadResumeState state, global::System.DateTimeOffset now, string[]? secrets)
    {
        ValidateForSave(state);
        string partialPath = global::System.IO.Path.GetFullPath(state.PartialPath ?? string.Empty);
        global::System.Uri sourceUri = state.SourceUri ?? new global::System.Uri("https://invalid.example/");
        DownloadResumeState persisted = new DownloadResumeState();
        persisted.SourceUri = CreateCredentialFreeUri(sourceUri, secrets);
        persisted.PartialPath = partialPath;
        persisted.CompletedBytes = state.CompletedBytes;
        persisted.ExpectedTotalBytes = state.HasExpectedTotalBytes ? state.ExpectedTotalBytes : 0L;
        persisted.HasExpectedTotalBytes = state.HasExpectedTotalBytes;
        persisted.ETag = SecretRedactor.RedactText(state.ETag, secrets);
        persisted.LastModified = state.HasLastModified ? state.LastModified : default(global::System.DateTimeOffset);
        persisted.HasLastModified = state.HasLastModified;
        persisted.UpdatedAt = now.ToUniversalTime();
        persisted.HasUpdatedAt = true;
        byte[] bytes = Serialize(persisted);
        if (bytes.Length > MaximumMetadataBytes)
        {
            throw new ValidationException("The partial download metadata exceeds its size limit.", "state", "PARTIAL_METADATA_TOO_LARGE", null);
        }

        string metadataPath = GetMetadataPath(partialPath);
        string temporaryPath = metadataPath + ".tmp";
        global::System.IO.File.WriteAllBytes(temporaryPath, bytes);
        global::System.IO.File.Move(temporaryPath, metadataPath, true);
        return persisted;
    }

    internal static DownloadResumeState? Load(string partialPath)
    {
        string metadataPath = GetMetadataPath(partialPath);
        string fullPartialPath = global::System.IO.Path.GetFullPath(partialPath);
        if (!global::System.IO.File.Exists(fullPartialPath) || !global::System.IO.File.Exists(metadataPath))
        {
            return null;
        }

        global::System.IO.FileInfo information = new global::System.IO.FileInfo(metadataPath);
        if (information.Length < Magic.Length + DigestBytes || information.Length > MaximumMetadataBytes || information.Length > global::System.Int32.MaxValue)
        {
            return null;
        }

        byte[] bytes = global::System.IO.File.ReadAllBytes(metadataPath);
        if (!HasMatchingDigest(bytes))
        {
            return null;
        }

        int payloadOffset = Magic.Length + DigestBytes;
        byte[] payload = new byte[bytes.Length - payloadOffset];
        global::System.Array.Copy(bytes, payloadOffset, payload, 0, payload.Length);
        DownloadResumeState? state = Deserialize(payload, fullPartialPath);
        if (state == null)
        {
            return null;
        }

        global::System.IO.FileInfo partialInformation = new global::System.IO.FileInfo(fullPartialPath);
        if (partialInformation.Length != state.CompletedBytes)
        {
            return null;
        }

        return state;
    }

    internal static bool Delete(string partialPath)
    {
        string metadataPath = GetMetadataPath(partialPath);
        bool existed = global::System.IO.File.Exists(metadataPath);
        global::System.IO.File.Delete(metadataPath);
        global::System.IO.File.Delete(metadataPath + ".tmp");
        return existed;
    }

    internal static global::System.Uri CreateCredentialFreeUri(global::System.Uri sourceUri, string[]? secrets)
    {
        global::System.Uri redacted = SecretRedactor.RedactUri(sourceUri, secrets);
        string encodedReplacement = global::System.Uri.EscapeDataString(SecretRedactor.Replacement);
        string safeText = redacted.OriginalString.Replace(SecretRedactor.Replacement, encodedReplacement, global::System.StringComparison.Ordinal);
        return new global::System.Uri(safeText, global::System.UriKind.Absolute);
    }

    internal static byte[] Serialize(DownloadResumeState state)
    {
        global::System.IO.MemoryStream payload = new global::System.IO.MemoryStream();
        FileResponseCache.WriteInt32(payload, FormatVersion);
        FileResponseCache.WriteString(payload, state.SourceUri == null ? null : state.SourceUri.AbsoluteUri);
        FileResponseCache.WriteInt64(payload, state.CompletedBytes);
        FileResponseCache.WriteInt64(payload, state.ExpectedTotalBytes);
        FileResponseCache.WriteBoolean(payload, state.HasExpectedTotalBytes);
        FileResponseCache.WriteString(payload, state.ETag);
        FileResponseCache.WriteInt64(payload, state.HasLastModified ? state.LastModified.UtcDateTime.Ticks : 0L);
        FileResponseCache.WriteBoolean(payload, state.HasLastModified);
        FileResponseCache.WriteInt64(payload, state.UpdatedAt.UtcDateTime.Ticks);
        byte[] payloadBytes = payload.ToArray();
        payload.Dispose();
        byte[] digest = global::System.Security.Cryptography.SHA256.HashData(payloadBytes);
        global::System.IO.MemoryStream output = new global::System.IO.MemoryStream();
        output.Write(Magic, 0, Magic.Length);
        output.Write(digest, 0, digest.Length);
        output.Write(payloadBytes, 0, payloadBytes.Length);
        byte[] result = output.ToArray();
        output.Dispose();
        return result;
    }

    internal static DownloadResumeState? Deserialize(byte[] payload, string partialPath)
    {
        FileCacheBufferReader reader = new FileCacheBufferReader(payload);
        int version = reader.ReadInt32();
        FileCacheStringValue sourceUri = reader.ReadString();
        long completedBytes = reader.ReadInt64();
        long expectedTotalBytes = reader.ReadInt64();
        bool hasExpectedTotalBytes = reader.ReadBoolean();
        FileCacheStringValue entityTag = reader.ReadString();
        long lastModifiedTicks = reader.ReadInt64();
        bool hasLastModified = reader.ReadBoolean();
        long updatedTicks = reader.ReadInt64();
        bool validTicks = lastModifiedTicks >= global::System.DateTimeOffset.MinValue.Ticks && lastModifiedTicks <= global::System.DateTimeOffset.MaxValue.Ticks && updatedTicks >= global::System.DateTimeOffset.MinValue.Ticks && updatedTicks <= global::System.DateTimeOffset.MaxValue.Ticks;
        bool validLengths = completedBytes >= 0L && expectedTotalBytes >= 0L && (!hasExpectedTotalBytes || completedBytes <= expectedTotalBytes);
        bool validSource = sourceUri.Value != null && global::System.Uri.IsWellFormedUriString(sourceUri.Value, global::System.UriKind.Absolute);
        if (!reader.IsValid || reader.Position != payload.Length || version != FormatVersion || !sourceUri.IsValid || !entityTag.IsValid || !validTicks || !validLengths || !validSource)
        {
            return null;
        }

        global::System.Uri parsedSource = new global::System.Uri(sourceUri.Value ?? "https://invalid.example/", global::System.UriKind.Absolute);
        bool isHttp = global::System.String.Equals(parsedSource.Scheme, global::System.Uri.UriSchemeHttp, global::System.StringComparison.OrdinalIgnoreCase);
        bool isHttps = global::System.String.Equals(parsedSource.Scheme, global::System.Uri.UriSchemeHttps, global::System.StringComparison.OrdinalIgnoreCase);
        if ((!isHttp && !isHttps) || parsedSource.UserInfo.Length != 0 || parsedSource.Fragment.Length != 0)
        {
            return null;
        }

        DownloadResumeState result = new DownloadResumeState();
        result.SourceUri = parsedSource;
        result.PartialPath = partialPath;
        result.CompletedBytes = completedBytes;
        result.ExpectedTotalBytes = hasExpectedTotalBytes ? expectedTotalBytes : 0L;
        result.HasExpectedTotalBytes = hasExpectedTotalBytes;
        result.ETag = entityTag.Value;
        result.LastModified = hasLastModified ? new global::System.DateTimeOffset(lastModifiedTicks, global::System.TimeSpan.Zero) : default(global::System.DateTimeOffset);
        result.HasLastModified = hasLastModified;
        result.UpdatedAt = new global::System.DateTimeOffset(updatedTicks, global::System.TimeSpan.Zero);
        result.HasUpdatedAt = true;
        return result;
    }

    internal static bool HasMatchingDigest(byte[] bytes)
    {
        int minimumLength = Magic.Length + DigestBytes;
        if (bytes.Length < minimumLength)
        {
            return false;
        }

        int index;
        for (index = 0; index < Magic.Length; index += 1)
        {
            if (bytes[index] != Magic[index])
            {
                return false;
            }
        }

        int payloadOffset = minimumLength;
        byte[] payload = new byte[bytes.Length - payloadOffset];
        global::System.Array.Copy(bytes, payloadOffset, payload, 0, payload.Length);
        byte[] digest = global::System.Security.Cryptography.SHA256.HashData(payload);
        for (index = 0; index < DigestBytes; index += 1)
        {
            if (bytes[Magic.Length + index] != digest[index])
            {
                return false;
            }
        }

        return true;
    }

    internal static void ValidateForSave(DownloadResumeState state)
    {
        if (state == null)
        {
            throw new ValidationException("Partial download metadata is required.", "state", "REQUIRED", null);
        }

        if (state.SourceUri == null || !state.SourceUri.IsAbsoluteUri)
        {
            throw new ValidationException("An absolute partial download source URI is required.", "SourceUri", "ABSOLUTE_URI_REQUIRED", null);
        }

        bool isHttp = global::System.String.Equals(state.SourceUri.Scheme, global::System.Uri.UriSchemeHttp, global::System.StringComparison.OrdinalIgnoreCase);
        bool isHttps = global::System.String.Equals(state.SourceUri.Scheme, global::System.Uri.UriSchemeHttps, global::System.StringComparison.OrdinalIgnoreCase);
        if ((!isHttp && !isHttps) || state.SourceUri.UserInfo.Length != 0 || state.SourceUri.Fragment.Length != 0)
        {
            throw new ValidationException("The partial download source URI is not safe to persist.", "SourceUri", "PARTIAL_SOURCE_URI_INVALID", null);
        }

        if (global::System.String.IsNullOrWhiteSpace(state.PartialPath))
        {
            throw new ValidationException("The partial download file does not exist.", "PartialPath", "PARTIAL_FILE_NOT_FOUND", null);
        }

        string partialPath = global::System.IO.Path.GetFullPath(state.PartialPath);
        if (!global::System.IO.File.Exists(partialPath))
        {
            throw new ValidationException("The partial download file does not exist.", "PartialPath", "PARTIAL_FILE_NOT_FOUND", null);
        }

        if (state.CompletedBytes < 0L)
        {
            throw new ValidationException("The completed partial length cannot be negative.", "CompletedBytes", "INVALID_PARTIAL_LENGTH", null);
        }

        global::System.IO.FileInfo information = new global::System.IO.FileInfo(partialPath);
        if (information.Length != state.CompletedBytes)
        {
            throw new ValidationException("The completed partial length does not match the file.", "CompletedBytes", "PARTIAL_LENGTH_MISMATCH", null);
        }

        if (state.HasExpectedTotalBytes && (state.ExpectedTotalBytes < 0L || state.CompletedBytes > state.ExpectedTotalBytes))
        {
            throw new ValidationException("The expected total length cannot be smaller than the completed partial length.", "ExpectedTotalBytes", "INVALID_EXPECTED_LENGTH", null);
        }
    }
}
