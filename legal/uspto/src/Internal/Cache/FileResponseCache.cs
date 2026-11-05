namespace uspto;

internal class FileCacheStringValue
{
    internal string? Value = null;
    internal bool IsValid = false;
}

internal class FileCacheRecord
{
    internal string Key = string.Empty;
    internal global::System.DateTimeOffset CreatedAt = default(global::System.DateTimeOffset);
    internal global::System.DateTimeOffset ExpiresAt = default(global::System.DateTimeOffset);
    internal BufferedHttpResponse Response = new BufferedHttpResponse();
}

internal class FileCacheReadResult
{
    internal bool IsValid = false;
    internal FileCacheRecord Record = new FileCacheRecord();
}

internal class FileCacheStoredFile
{
    internal string Path = string.Empty;
    internal long Size = 0L;
    internal global::System.DateTimeOffset LastAccessedAt = default(global::System.DateTimeOffset);
}

internal class FileCacheBufferReader
{
    internal byte[] Data = new byte[0];
    internal int Position = 0;
    internal bool IsValid = true;

    internal FileCacheBufferReader(byte[] data)
    {
        Data = data;
    }

    internal int ReadInt32()
    {
        if (!IsValid || Position > Data.Length - 4)
        {
            IsValid = false;
            return 0;
        }

        int value = Data[Position] | Data[Position + 1] << 8 | Data[Position + 2] << 16 | Data[Position + 3] << 24;
        Position += 4;
        return value;
    }

    internal long ReadInt64()
    {
        if (!IsValid || Position > Data.Length - 8)
        {
            IsValid = false;
            return 0L;
        }

        long value = Data[Position] | (long)Data[Position + 1] << 8 | (long)Data[Position + 2] << 16 | (long)Data[Position + 3] << 24 | (long)Data[Position + 4] << 32 | (long)Data[Position + 5] << 40 | (long)Data[Position + 6] << 48 | (long)Data[Position + 7] << 56;
        Position += 8;
        return value;
    }

    internal bool ReadBoolean()
    {
        int value = ReadInt32();
        if (value != 0 && value != 1)
        {
            IsValid = false;
        }

        return value == 1;
    }

    internal FileCacheStringValue ReadString()
    {
        FileCacheStringValue result = new FileCacheStringValue();
        int length = ReadInt32();
        if (!IsValid || length < -1 || length > Data.Length - Position)
        {
            IsValid = false;
            return result;
        }

        if (length == -1)
        {
            result.IsValid = true;
            return result;
        }

        result.Value = global::System.Text.Encoding.UTF8.GetString(Data, Position, length);
        result.IsValid = true;
        Position += length;
        return result;
    }
}

internal class FileResponseCache
{
    internal const int FormatVersion = 1;
    internal const int DigestBytes = 32;
    internal const int MaximumMetadataBytes = 1048576;
    internal const int MaximumHeaderCount = 10000;
    internal const int MaximumHeaderValueCount = 10000;
    internal static readonly byte[] Magic = new byte[] { 85, 83, 80, 84, 79, 67, 65, 67, 72, 69 };
    internal object Sync = new object();
    internal bool Enabled = false;
    internal string RootDirectory = string.Empty;
    internal string CacheDirectory = string.Empty;
    internal int LifetimeSeconds = 0;
    internal long MaximumEntryBytes = 0L;
    internal long MaximumFileBytes = 0L;
    internal bool HasMaximumFileBytes = false;
    internal bool HonorHttpCaching = true;

    internal FileResponseCache(CachePolicy policy)
    {
        if (policy == null)
        {
            throw new ValidationException("A cache policy is required.", "policy", "REQUIRED", null);
        }

        if (policy.LifetimeSeconds < 0)
        {
            throw new ValidationException("The file-cache lifetime cannot be negative.", "LifetimeSeconds", "INVALID_CACHE_POLICY", null);
        }

        if (policy.MaximumEntryBytes < 0L)
        {
            throw new ValidationException("The file-cache response-size limit cannot be negative.", "MaximumEntryBytes", "INVALID_CACHE_POLICY", null);
        }

        if (policy.HasMaximumFileBytes && policy.MaximumFileBytes < 0L)
        {
            throw new ValidationException("The total file-cache size limit cannot be negative.", "MaximumFileBytes", "INVALID_CACHE_POLICY", null);
        }

        Enabled = policy.Mode == CacheMode.File || policy.Mode == CacheMode.MemoryAndFile;
        LifetimeSeconds = policy.LifetimeSeconds;
        MaximumEntryBytes = policy.MaximumEntryBytes;
        MaximumFileBytes = policy.MaximumFileBytes;
        HasMaximumFileBytes = policy.HasMaximumFileBytes;
        HonorHttpCaching = policy.HonorHttpCaching;
        if (!Enabled)
        {
            return;
        }

        if (global::System.String.IsNullOrWhiteSpace(policy.Directory))
        {
            throw new ValidationException("A directory is required when file caching is enabled.", "Directory", "REQUIRED", null);
        }

        RootDirectory = global::System.IO.Path.GetFullPath(policy.Directory);
        CacheDirectory = global::System.IO.Path.Combine(RootDirectory, "responses-v1");
        Sync = global::System.String.Intern(CacheDirectory);
        global::System.IO.Directory.CreateDirectory(CacheDirectory);
    }

    internal BufferedHttpResponse? Get(string key, global::System.DateTimeOffset now)
    {
        ValidateKey(key);
        if (!Enabled)
        {
            return null;
        }

        lock (Sync)
        {
            RemoveTemporaryFiles();
            string entryPath = GetEntryPath(key);
            if (!global::System.IO.File.Exists(entryPath))
            {
                return null;
            }

            FileCacheReadResult result = ReadEntry(entryPath);
            if (!result.IsValid || result.Record.Key != key || result.Record.ExpiresAt <= now)
            {
                if (!result.IsValid || result.Record.Key != key)
                {
                    global::System.IO.File.Delete(entryPath);
                }

                return null;
            }

            global::System.IO.File.SetLastWriteTimeUtc(entryPath, now.UtcDateTime);
            return MemoryResponseCache.CloneResponse(result.Record.Response);
        }
    }

    internal bool Store(string key, BufferedHttpResponse response, global::System.DateTimeOffset now, string[]? credentialValues)
    {
        ValidateKey(key);
        if (response == null)
        {
            throw new ValidationException("A buffered response is required for file caching.", "response", "REQUIRED", null);
        }

        if (!IsEligible(response, credentialValues))
        {
            return false;
        }

        FileCacheRecord record = new FileCacheRecord();
        record.Key = key;
        record.CreatedAt = now;
        record.ExpiresAt = CalculateExpiration(now, response.Metadata);
        record.Response = MemoryResponseCache.CloneResponse(response);
        byte[] bytes = SerializeRecord(record);
        long maximumStoredBytes = MaximumEntryBytes;
        if (global::System.Int64.MaxValue - maximumStoredBytes < MaximumMetadataBytes)
        {
            maximumStoredBytes = global::System.Int64.MaxValue;
        }
        else
        {
            maximumStoredBytes += MaximumMetadataBytes;
        }

        if (bytes.LongLength > maximumStoredBytes)
        {
            return false;
        }

        if (HasMaximumFileBytes && bytes.LongLength > MaximumFileBytes)
        {
            return false;
        }

        lock (Sync)
        {
            RecoverDirectory(now);
            string entryPath = GetEntryPath(key);
            string? parent = global::System.IO.Path.GetDirectoryName(entryPath);
            if (parent == null)
            {
                throw new CacheException("The file-cache entry path has no parent directory.", key, entryPath, null);
            }

            global::System.IO.Directory.CreateDirectory(parent);
            string temporaryPath = entryPath + ".tmp";
            global::System.IO.File.WriteAllBytes(temporaryPath, bytes);
            global::System.IO.File.Move(temporaryPath, entryPath, true);
            global::System.IO.File.SetLastWriteTimeUtc(entryPath, now.UtcDateTime);
            EvictToSizeLimit();
            return global::System.IO.File.Exists(entryPath);
        }
    }

    internal bool Invalidate(string key)
    {
        ValidateKey(key);
        if (!Enabled)
        {
            return false;
        }

        lock (Sync)
        {
            string entryPath = GetEntryPath(key);
            bool existed = global::System.IO.File.Exists(entryPath);
            global::System.IO.File.Delete(entryPath);
            global::System.IO.File.Delete(entryPath + ".tmp");
            return existed;
        }
    }

    internal BufferedHttpResponse? GetForRevalidation(string key)
    {
        ValidateKey(key);
        if (!Enabled)
        {
            return null;
        }

        lock (Sync)
        {
            RemoveTemporaryFiles();
            string entryPath = GetEntryPath(key);
            if (!global::System.IO.File.Exists(entryPath))
            {
                return null;
            }

            FileCacheReadResult result = ReadEntry(entryPath);
            if (!result.IsValid || result.Record.Key != key)
            {
                global::System.IO.File.Delete(entryPath);
                return null;
            }

            return MemoryResponseCache.CloneResponse(result.Record.Response);
        }
    }

    internal void Clear()
    {
        if (!Enabled)
        {
            return;
        }

        lock (Sync)
        {
            if (global::System.IO.Directory.Exists(CacheDirectory))
            {
                global::System.IO.Directory.Delete(CacheDirectory, true);
            }

            global::System.IO.Directory.CreateDirectory(CacheDirectory);
        }
    }

    internal int GetEntryCount(global::System.DateTimeOffset now)
    {
        if (!Enabled)
        {
            return 0;
        }

        lock (Sync)
        {
            RecoverDirectory(now);
            return global::System.IO.Directory.GetFiles(CacheDirectory, "*.cache", global::System.IO.SearchOption.AllDirectories).Length;
        }
    }

    internal string GetRelativePath(string key)
    {
        ValidateKey(key);
        byte[] keyBytes = global::System.Text.Encoding.UTF8.GetBytes(key);
        string digest = global::System.Convert.ToHexString(global::System.Security.Cryptography.SHA256.HashData(keyBytes)).ToLowerInvariant();
        return global::System.IO.Path.Combine(digest.Substring(0, 2), digest + ".cache");
    }

    internal string GetEntryPath(string key)
    {
        return global::System.IO.Path.Combine(CacheDirectory, GetRelativePath(key));
    }

    internal bool IsEligible(BufferedHttpResponse response, string[]? credentialValues)
    {
        if (!Enabled || LifetimeSeconds == 0 || MaximumEntryBytes == 0L)
        {
            return false;
        }

        if (response.Metadata.StatusCode < 200 || response.Metadata.StatusCode >= 300)
        {
            return false;
        }

        if (response.BodyBytes < 0L || response.BodyBytes > MaximumEntryBytes)
        {
            return false;
        }

        if (global::System.Text.Encoding.UTF8.GetByteCount(response.Text) > MaximumEntryBytes)
        {
            return false;
        }

        if (response.Metadata.Headers.Count > MaximumHeaderCount)
        {
            return false;
        }

        foreach (global::System.Collections.Generic.KeyValuePair<string, string[]> header in response.Metadata.Headers)
        {
            if (header.Value.Length > MaximumHeaderValueCount)
            {
                return false;
            }
        }

        CacheFreshness freshness = ConditionalRequest.Evaluate(response.Metadata, LifetimeSeconds, HonorHttpCaching, global::System.DateTimeOffset.MinValue, global::System.DateTimeOffset.MinValue);
        if (!freshness.CanStore)
        {
            return false;
        }

        return !MemoryResponseCache.IsSecretBearing(response, credentialValues);
    }

    internal global::System.DateTimeOffset CalculateExpiration(global::System.DateTimeOffset now, HttpResponseMetadata metadata)
    {
        return ConditionalRequest.Evaluate(metadata, LifetimeSeconds, HonorHttpCaching, now, now).FreshUntil;
    }

    internal void RecoverDirectory(global::System.DateTimeOffset now)
    {
        RemoveTemporaryFiles();
        string[] entryPaths = global::System.IO.Directory.GetFiles(CacheDirectory, "*.cache", global::System.IO.SearchOption.AllDirectories);
        foreach (string entryPath in entryPaths)
        {
            FileCacheReadResult result = ReadEntry(entryPath);
            if (!result.IsValid || result.Record.ExpiresAt <= now)
            {
                global::System.IO.File.Delete(entryPath);
            }
        }
    }

    internal void RemoveTemporaryFiles()
    {
        string[] temporaryPaths = global::System.IO.Directory.GetFiles(CacheDirectory, "*.tmp", global::System.IO.SearchOption.AllDirectories);
        foreach (string temporaryPath in temporaryPaths)
        {
            global::System.IO.File.Delete(temporaryPath);
        }
    }

    internal void EvictToSizeLimit()
    {
        if (!HasMaximumFileBytes)
        {
            return;
        }

        global::System.Collections.Generic.List<FileCacheStoredFile> files = new global::System.Collections.Generic.List<FileCacheStoredFile>();
        long totalBytes = 0L;
        string[] entryPaths = global::System.IO.Directory.GetFiles(CacheDirectory, "*.cache", global::System.IO.SearchOption.AllDirectories);
        foreach (string entryPath in entryPaths)
        {
            global::System.IO.FileInfo information = new global::System.IO.FileInfo(entryPath);
            FileCacheStoredFile stored = new FileCacheStoredFile();
            stored.Path = entryPath;
            stored.Size = information.Length;
            stored.LastAccessedAt = information.LastWriteTimeUtc;
            files.Add(stored);
            if (global::System.Int64.MaxValue - totalBytes < information.Length)
            {
                totalBytes = global::System.Int64.MaxValue;
            }
            else
            {
                totalBytes += information.Length;
            }
        }

        while (totalBytes > MaximumFileBytes && files.Count != 0)
        {
            int oldestIndex = 0;
            for (int index = 1; index < files.Count; index += 1)
            {
                if (files[index].LastAccessedAt < files[oldestIndex].LastAccessedAt || files[index].LastAccessedAt == files[oldestIndex].LastAccessedAt && global::System.String.CompareOrdinal(files[index].Path, files[oldestIndex].Path) < 0)
                {
                    oldestIndex = index;
                }
            }

            FileCacheStoredFile oldest = files[oldestIndex];
            global::System.IO.File.Delete(oldest.Path);
            totalBytes -= oldest.Size;
            files.RemoveAt(oldestIndex);
        }
    }

    internal static byte[] SerializeRecord(FileCacheRecord record)
    {
        global::System.IO.MemoryStream payload = new global::System.IO.MemoryStream();
        WriteInt32(payload, FormatVersion);
        WriteString(payload, record.Key);
        WriteInt64(payload, record.CreatedAt.UtcDateTime.Ticks);
        WriteInt64(payload, record.ExpiresAt.UtcDateTime.Ticks);
        BufferedHttpResponse response = record.Response;
        HttpResponseMetadata metadata = response.Metadata;
        WriteInt32(payload, metadata.StatusCode);
        WriteString(payload, metadata.ReasonPhrase);
        WriteString(payload, metadata.RequestUri == null ? null : metadata.RequestUri.OriginalString);
        WriteString(payload, metadata.MediaType);
        WriteString(payload, metadata.CharacterSet);
        WriteInt32(payload, (int)metadata.Format);
        WriteInt64(payload, metadata.ContentLength);
        WriteBoolean(payload, metadata.HasContentLength);
        WriteString(payload, metadata.RequestIdentifier);
        WriteInt32(payload, metadata.Headers.Count);
        foreach (global::System.Collections.Generic.KeyValuePair<string, string[]> header in metadata.Headers)
        {
            WriteString(payload, header.Key);
            WriteInt32(payload, header.Value.Length);
            foreach (string value in header.Value)
            {
                WriteString(payload, value);
            }
        }

        WriteInt32(payload, (int)response.ExpectedFormat);
        WriteString(payload, response.Text);
        WriteInt64(payload, response.BodyBytes);
        WriteString(payload, response.RawText);
        WriteInt64(payload, response.RawTextBytes);
        WriteBoolean(payload, response.HasRawText);
        WriteBoolean(payload, response.IsRawTextTruncated);
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

    internal FileCacheReadResult ReadEntry(string path)
    {
        FileCacheReadResult result = new FileCacheReadResult();
        global::System.IO.FileInfo information = new global::System.IO.FileInfo(path);
        long maximumLength = MaximumEntryBytes;
        if (global::System.Int64.MaxValue - maximumLength < MaximumMetadataBytes)
        {
            maximumLength = global::System.Int64.MaxValue;
        }
        else
        {
            maximumLength += MaximumMetadataBytes;
        }

        if (information.Length < Magic.Length + DigestBytes || information.Length > maximumLength || information.Length > global::System.Int32.MaxValue)
        {
            return result;
        }

        byte[] bytes = global::System.IO.File.ReadAllBytes(path);
        for (int index = 0; index < Magic.Length; index += 1)
        {
            if (bytes[index] != Magic[index])
            {
                return result;
            }
        }

        int payloadOffset = Magic.Length + DigestBytes;
        byte[] payload = new byte[bytes.Length - payloadOffset];
        global::System.Array.Copy(bytes, payloadOffset, payload, 0, payload.Length);
        byte[] actualDigest = global::System.Security.Cryptography.SHA256.HashData(payload);
        for (int index = 0; index < DigestBytes; index += 1)
        {
            if (bytes[Magic.Length + index] != actualDigest[index])
            {
                return result;
            }
        }

        FileCacheBufferReader reader = new FileCacheBufferReader(payload);
        int version = reader.ReadInt32();
        FileCacheStringValue key = reader.ReadString();
        long createdTicks = reader.ReadInt64();
        long expiresTicks = reader.ReadInt64();
        int statusCode = reader.ReadInt32();
        FileCacheStringValue reasonPhrase = reader.ReadString();
        FileCacheStringValue requestUri = reader.ReadString();
        FileCacheStringValue mediaType = reader.ReadString();
        FileCacheStringValue characterSet = reader.ReadString();
        int format = reader.ReadInt32();
        long contentLength = reader.ReadInt64();
        bool hasContentLength = reader.ReadBoolean();
        FileCacheStringValue requestIdentifier = reader.ReadString();
        int headerCount = reader.ReadInt32();
        if (headerCount < 0 || headerCount > MaximumHeaderCount)
        {
            reader.IsValid = false;
        }

        HttpResponseMetadata metadata = new HttpResponseMetadata();
        for (int headerIndex = 0; headerIndex < headerCount && reader.IsValid; headerIndex += 1)
        {
            FileCacheStringValue headerName = reader.ReadString();
            int valueCount = reader.ReadInt32();
            if (!headerName.IsValid || headerName.Value == null || valueCount < 0 || valueCount > MaximumHeaderValueCount || metadata.Headers.ContainsKey(headerName.Value))
            {
                reader.IsValid = false;
            }
            else
            {
                string[] values = new string[valueCount];
                for (int valueIndex = 0; valueIndex < valueCount && reader.IsValid; valueIndex += 1)
                {
                    FileCacheStringValue headerValue = reader.ReadString();
                    if (!headerValue.IsValid || headerValue.Value == null)
                    {
                        reader.IsValid = false;
                    }
                    else
                    {
                        values[valueIndex] = headerValue.Value;
                    }
                }

                if (reader.IsValid)
                {
                    metadata.Headers.Add(headerName.Value, values);
                }
            }
        }

        int expectedFormat = reader.ReadInt32();
        FileCacheStringValue text = reader.ReadString();
        long bodyBytes = reader.ReadInt64();
        FileCacheStringValue rawText = reader.ReadString();
        long rawTextBytes = reader.ReadInt64();
        bool hasRawText = reader.ReadBoolean();
        bool isRawTextTruncated = reader.ReadBoolean();
        bool validTicks = createdTicks >= global::System.DateTimeOffset.MinValue.Ticks && createdTicks <= global::System.DateTimeOffset.MaxValue.Ticks && expiresTicks >= global::System.DateTimeOffset.MinValue.Ticks && expiresTicks <= global::System.DateTimeOffset.MaxValue.Ticks;
        bool validUri = requestUri.Value == null || global::System.Uri.IsWellFormedUriString(requestUri.Value, global::System.UriKind.RelativeOrAbsolute);
        if (!reader.IsValid || reader.Position != payload.Length || version != FormatVersion || !key.IsValid || key.Value == null || !reasonPhrase.IsValid || !requestUri.IsValid || !mediaType.IsValid || !characterSet.IsValid || !requestIdentifier.IsValid || !text.IsValid || text.Value == null || !rawText.IsValid || !validTicks || !validUri || bodyBytes < 0L || rawTextBytes < 0L)
        {
            return result;
        }

        metadata.StatusCode = statusCode;
        metadata.ReasonPhrase = reasonPhrase.Value;
        if (requestUri.Value != null)
        {
            metadata.RequestUri = new global::System.Uri(requestUri.Value, global::System.UriKind.RelativeOrAbsolute);
        }

        metadata.MediaType = mediaType.Value;
        metadata.CharacterSet = characterSet.Value;
        metadata.Format = (DocumentFormat)format;
        metadata.ContentLength = contentLength;
        metadata.HasContentLength = hasContentLength;
        metadata.RequestIdentifier = requestIdentifier.Value;
        BufferedHttpResponse response = new BufferedHttpResponse();
        response.Metadata = metadata;
        response.ExpectedFormat = (DocumentFormat)expectedFormat;
        response.Text = text.Value;
        response.BodyBytes = bodyBytes;
        response.RawText = rawText.Value;
        response.RawTextBytes = rawTextBytes;
        response.HasRawText = hasRawText;
        response.IsRawTextTruncated = isRawTextTruncated;
        FileCacheRecord record = new FileCacheRecord();
        record.Key = key.Value;
        record.CreatedAt = new global::System.DateTimeOffset(createdTicks, global::System.TimeSpan.Zero);
        record.ExpiresAt = new global::System.DateTimeOffset(expiresTicks, global::System.TimeSpan.Zero);
        record.Response = response;
        result.Record = record;
        result.IsValid = true;
        return result;
    }

    internal static void WriteInt32(global::System.IO.Stream output, int value)
    {
        output.WriteByte((byte)(value & 255));
        output.WriteByte((byte)(value >> 8 & 255));
        output.WriteByte((byte)(value >> 16 & 255));
        output.WriteByte((byte)(value >> 24 & 255));
    }

    internal static void WriteInt64(global::System.IO.Stream output, long value)
    {
        output.WriteByte((byte)(value & 255L));
        output.WriteByte((byte)(value >> 8 & 255L));
        output.WriteByte((byte)(value >> 16 & 255L));
        output.WriteByte((byte)(value >> 24 & 255L));
        output.WriteByte((byte)(value >> 32 & 255L));
        output.WriteByte((byte)(value >> 40 & 255L));
        output.WriteByte((byte)(value >> 48 & 255L));
        output.WriteByte((byte)(value >> 56 & 255L));
    }

    internal static void WriteBoolean(global::System.IO.Stream output, bool value)
    {
        WriteInt32(output, value ? 1 : 0);
    }

    internal static void WriteString(global::System.IO.Stream output, string? value)
    {
        if (value == null)
        {
            WriteInt32(output, -1);
            return;
        }

        byte[] bytes = global::System.Text.Encoding.UTF8.GetBytes(value);
        WriteInt32(output, bytes.Length);
        output.Write(bytes, 0, bytes.Length);
    }

    internal static void ValidateKey(string key)
    {
        if (global::System.String.IsNullOrWhiteSpace(key))
        {
            throw new ValidationException("A file-cache key is required.", "key", "REQUIRED", null);
        }
    }
}
