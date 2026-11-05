using System;
using System.IO;
using System.Security.Cryptography;

namespace email;

internal class EmailBodyIngestor
{
    internal static EmailBodyIngestResult Ingest(EmailContent source, EmailRange range, string? transferEncodingName, EmailParseOptions options, EmailMimeParseState state)
    {
        EmailBodyIngestOptions ingestOptions = options.GetBodyIngestOptions();
        EmailBodyStorageMode storageMode = ingestOptions.GetStorageMode();
        EmailContent? lazyRaw = storageMode == EmailBodyStorageMode.Lazy ? new EmailContentOperations().CreateSlice(source, range.GetStartOffset(), range.GetLength()) : null;
        bool mustDecode = ingestOptions.GetDecodeOnIngest() || ingestOptions.GetComputeDecodedHash();
        if (storageMode == EmailBodyStorageMode.Lazy && !mustDecode && !ingestOptions.GetComputeRawHash())
        {
            EmailBodyIngestMetadata lazyMetadata = new EmailBodyIngestMetadata(storageMode, range.GetLength(), 0, false, false, null, null);
            return new EmailBodyIngestResult(lazyRaw, null, lazyMetadata, new EmailIssueList());
        }

        EmailTransferEncodingKind encodingKind = GetEncodingKind(transferEncodingName);
        bool legacyUuencode = options.GetMode() != EmailParseMode.Strict && options.GetCompatibilityOptions().GetAllowLegacyAttachmentRecovery() && IsUuencodeEncoding(transferEncodingName);
        bool knownEncoding = IsKnownEncoding(transferEncodingName) || legacyUuencode;
        EmailIssueList issues = new EmailIssueList();
        if (mustDecode && !knownEncoding)
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.TransferEncodingUnsupported, "The content-transfer-encoding is not supported for decode-on-ingest.", new EmailSourcePosition(0, 1, 1), null, "Content-Transfer-Encoding", "The encoded body was processed without producing decoded content."));
        }

        EmailStorageOptions storageOptions = options.GetStorageOptions();
        EmailTemporaryStorageLimits storageLimits = storageOptions.GetLimits();
        if (storageMode == EmailBodyStorageMode.Store && range.GetLength() > storageLimits.GetMemoryThresholdBytes() && range.GetLength() > storageLimits.GetMaxTemporaryBytes())
        {
            throw new EmailLimitException("MaxTemporaryBytes", storageLimits.GetMaxTemporaryBytes(), range.GetLength());
        }

        EmailTemporaryStore? rawStore = storageMode == EmailBodyStorageMode.Store ? new EmailTemporaryStore(storageOptions) : null;
        bool retainDecoded = ingestOptions.GetDecodeOnIngest() && storageMode != EmailBodyStorageMode.Discard && knownEncoding;
        EmailTemporaryStore? decodedStore = retainDecoded ? new EmailTemporaryStore(storageOptions) : null;
        IncrementalHash? rawHash = ingestOptions.GetComputeRawHash() ? IncrementalHash.CreateHash(GetAlgorithmName(ingestOptions.GetHashAlgorithm())) : null;
        IncrementalHash? decodedHash = ingestOptions.GetComputeDecodedHash() && knownEncoding ? IncrementalHash.CreateHash(GetAlgorithmName(ingestOptions.GetHashAlgorithm())) : null;
        long maximumDecodedBytes = GetMaximumDecodedBytes(range.GetLength(), options.GetLimits().GetDecodingLimits());
        if (retainDecoded && maximumDecodedBytes > storageLimits.GetMemoryThresholdBytes() && maximumDecodedBytes > storageLimits.GetMaxTemporaryBytes())
        {
            maximumDecodedBytes = storageLimits.GetMaxTemporaryBytes();
        }

        EmailBase64Decoder? base64 = mustDecode && knownEncoding && encodingKind == EmailTransferEncodingKind.Base64 ? new EmailBase64Decoder(GetBase64Mode(options), maximumDecodedBytes) : null;
        EmailQuotedPrintableDecoder? quotedPrintable = mustDecode && knownEncoding && encodingKind == EmailTransferEncodingKind.QuotedPrintable ? new EmailQuotedPrintableDecoder(GetQuotedPrintableMode(options), maximumDecodedBytes) : null;
        EmailUuencodeDecoder? uuencode = mustDecode && legacyUuencode ? new EmailUuencodeDecoder(maximumDecodedBytes, options.GetLimits().GetParsingLimits().GetMaxLineBytes()) : null;
        byte[] transfer = new byte[storageOptions.GetCopyBufferBytes()];
        long rawByteCount = 0;
        long decodedByteCount = 0;
        bool identityLimitReached = false;
        Stream input = source.OpenReadRange(range.GetStartOffset(), range.GetLength());
        while (rawByteCount < range.GetLength())
        {
            int requested = transfer.Length;
            long remaining = range.GetLength() - rawByteCount;
            if (remaining < requested)
            {
                requested = (int)remaining;
            }

            int read = input.Read(transfer, 0, requested);
            if (read == 0)
            {
                throw new EndOfStreamException("The body source ended before the declared MIME content range.");
            }

            ReadOnlySpan<byte> chunk = new ReadOnlySpan<byte>(transfer, 0, read);
            if (rawStore != null)
            {
                rawStore.Write(chunk);
            }

            if (rawHash != null)
            {
                rawHash.AppendData(chunk);
            }

            if (mustDecode && knownEncoding)
            {
                byte[] decoded = DecodeChunk(chunk, encodingKind, base64, quotedPrintable, uuencode, maximumDecodedBytes, decodedByteCount);
                if (IsIdentityEncoding(encodingKind) && decoded.Length < chunk.Length && !identityLimitReached)
                {
                    issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Fatal, EmailIssueCode.DecodedByteLimitExceeded, "Decoded content exceeds the configured byte or expansion limit.", new EmailSourcePosition(rawByteCount + decoded.Length, 1, rawByteCount + decoded.Length + 1), null, null, "Only the decoded prefix within the configured limit was retained."));
                    identityLimitReached = true;
                }

                AppendDecoded(decoded, decodedStore, decodedHash);
                decodedByteCount += decoded.Length;
            }

            rawByteCount += read;
        }

        if (!source.IsRangeReadStreamBorrowed())
        {
            input.Close();
        }

        if (mustDecode && knownEncoding && (uuencode != null || !IsIdentityEncoding(encodingKind)))
        {
            byte[] finalDecoded = CompleteDecoder(base64, quotedPrintable, uuencode);
            AppendDecoded(finalDecoded, decodedStore, decodedHash);
            decodedByteCount += finalDecoded.Length;
            EmailIssueList decoderIssues = base64 != null ? base64.GetIssues() : quotedPrintable != null ? quotedPrintable.GetIssues() : (uuencode ?? throw new InvalidOperationException()).GetIssues();
            AddDecoderIssues(issues, decoderIssues, options);
            if (uuencode != null && uuencode.GetDetection().IsDetected())
            {
                issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.LegacyAttachmentRecovery, "A legacy uuencode transfer encoding was decoded by an explicitly enabled compatibility rule.", new EmailSourcePosition(0, 1, 1), null, "Content-Transfer-Encoding", "The decoded attachment bytes are available while the original encoded body remains retained according to the ingest policy."));
            }
        }

        EmailContent? rawContent = rawStore == null ? lazyRaw : state.RegisterStoredContent(rawStore.Complete());
        EmailContent? decodedContent = decodedStore == null ? null : state.RegisterStoredContent(decodedStore.Complete());
        EmailHashResult? rawHashResult = CompleteHash(rawHash, ingestOptions.GetHashAlgorithm(), rawByteCount);
        EmailHashResult? decodedHashResult = CompleteHash(decodedHash, ingestOptions.GetHashAlgorithm(), decodedByteCount);
        bool decodingComplete = mustDecode && knownEncoding && !identityLimitReached && !HasDecoderFatalError(base64, quotedPrintable, uuencode);
        EmailBodyIngestMetadata metadata = new EmailBodyIngestMetadata(storageMode, rawByteCount, decodedByteCount, mustDecode, decodingComplete, rawHashResult, decodedHashResult);
        CryptographicOperations.ZeroMemory(transfer);
        return new EmailBodyIngestResult(rawContent, decodedContent, metadata, issues);
    }

    static byte[] DecodeChunk(ReadOnlySpan<byte> chunk, EmailTransferEncodingKind kind, EmailBase64Decoder? base64, EmailQuotedPrintableDecoder? quotedPrintable, EmailUuencodeDecoder? uuencode, long maximumDecodedBytes, long decodedByteCount)
    {
        if (uuencode != null)
        {
            return uuencode.Feed(chunk);
        }

        if (kind == EmailTransferEncodingKind.Base64)
        {
            return (base64 ?? throw new InvalidOperationException()).Feed(chunk);
        }

        if (kind == EmailTransferEncodingKind.QuotedPrintable)
        {
            return (quotedPrintable ?? throw new InvalidOperationException()).Feed(chunk);
        }

        long remaining = maximumDecodedBytes - decodedByteCount;
        int count = chunk.Length;
        if (remaining < count)
        {
            count = remaining <= 0 ? 0 : (int)remaining;
        }

        return chunk.Slice(0, count).ToArray();
    }

    static byte[] CompleteDecoder(EmailBase64Decoder? base64, EmailQuotedPrintableDecoder? quotedPrintable, EmailUuencodeDecoder? uuencode)
    {
        if (base64 != null)
        {
            return base64.Complete();
        }

        if (quotedPrintable != null)
        {
            return quotedPrintable.Complete();
        }

        return (uuencode ?? throw new InvalidOperationException()).Complete();
    }

    static bool HasDecoderFatalError(EmailBase64Decoder? base64, EmailQuotedPrintableDecoder? quotedPrintable, EmailUuencodeDecoder? uuencode)
    {
        if (base64 != null)
        {
            return base64.HasFatalError();
        }

        if (quotedPrintable != null)
        {
            return quotedPrintable.HasFatalError();
        }

        return uuencode != null && uuencode.HasFatalError();
    }

    static void AppendDecoded(byte[] value, EmailTemporaryStore? store, IncrementalHash? hash)
    {
        if (value.Length == 0)
        {
            return;
        }

        if (store != null)
        {
            store.Write(value);
        }

        if (hash != null)
        {
            hash.AppendData(value);
        }
    }

    static EmailHashResult? CompleteHash(IncrementalHash? hash, EmailHashAlgorithm algorithm, long byteCount)
    {
        if (hash == null)
        {
            return null;
        }

        byte[] digest = hash.GetHashAndReset();
        hash.Dispose();
        return new EmailHashResult(algorithm, digest, byteCount);
    }

    static long GetMaximumDecodedBytes(long encodedBytes, EmailDecodingLimits limits)
    {
        long maximum = limits.GetMaxDecodedBytes();
        int ratio = limits.GetMaxExpansionRatio();
        long expansionMaximum = encodedBytes > long.MaxValue / ratio ? long.MaxValue : encodedBytes * ratio;
        return expansionMaximum < maximum ? expansionMaximum : maximum;
    }

    static bool IsKnownEncoding(string? value)
    {
        return value == null || string.Equals(value, "7bit", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "8bit", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "binary", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "quoted-printable", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "base64", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsUuencodeEncoding(string? value)
    {
        return string.Equals(value, "x-uuencode", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "uuencode", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "uue", StringComparison.OrdinalIgnoreCase);
    }

    static EmailTransferEncodingKind GetEncodingKind(string? value)
    {
        if (string.Equals(value, "8bit", StringComparison.OrdinalIgnoreCase))
        {
            return EmailTransferEncodingKind.EightBit;
        }

        if (string.Equals(value, "binary", StringComparison.OrdinalIgnoreCase))
        {
            return EmailTransferEncodingKind.Binary;
        }

        if (string.Equals(value, "quoted-printable", StringComparison.OrdinalIgnoreCase))
        {
            return EmailTransferEncodingKind.QuotedPrintable;
        }

        if (string.Equals(value, "base64", StringComparison.OrdinalIgnoreCase))
        {
            return EmailTransferEncodingKind.Base64;
        }

        return EmailTransferEncodingKind.SevenBit;
    }

    static bool IsIdentityEncoding(EmailTransferEncodingKind value)
    {
        return value == EmailTransferEncodingKind.SevenBit || value == EmailTransferEncodingKind.EightBit || value == EmailTransferEncodingKind.Binary;
    }

    static EmailBase64DecodeMode GetBase64Mode(EmailParseOptions options)
    {
        return options.GetMode() == EmailParseMode.Strict ? EmailBase64DecodeMode.Strict : EmailBase64DecodeMode.Tolerant;
    }

    static EmailQuotedPrintableDecodeMode GetQuotedPrintableMode(EmailParseOptions options)
    {
        return options.GetMode() == EmailParseMode.Strict ? EmailQuotedPrintableDecodeMode.Strict : EmailQuotedPrintableDecodeMode.Tolerant;
    }

    static void AddDecoderIssues(EmailIssueList destination, EmailIssueList source, EmailParseOptions options)
    {
        int index = 0;
        while (index < source.GetCount())
        {
            EmailIssue issue = source.GetAt(index);
            bool truncated = issue.GetCode() == EmailIssueCode.Base64TruncatedQuantum || issue.GetCode() == EmailIssueCode.QuotedPrintableTruncatedEscape;
            if (truncated && !options.GetCompatibilityOptions().GetAllowTruncatedInput() && issue.GetSeverity() < EmailIssueSeverity.Error)
            {
                EmailSourcePosition? position = issue.HasSourcePosition() ? issue.GetSourcePosition() : null;
                EmailPath? path = issue.HasEntityPath() ? issue.GetEntityPath() : null;
                string? fieldName = issue.HasFieldName() ? issue.GetFieldName() : null;
                string? recovery = issue.HasRecoveryDescription() ? issue.GetRecoveryDescription() : null;
                destination.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, issue.GetCode(), issue.GetDescription(), position, path, fieldName, recovery));
            }
            else
            {
                destination.Add(issue);
            }

            index++;
        }
    }

    static HashAlgorithmName GetAlgorithmName(EmailHashAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case EmailHashAlgorithm.Sha256:
                return HashAlgorithmName.SHA256;
            case EmailHashAlgorithm.Sha384:
                return HashAlgorithmName.SHA384;
            case EmailHashAlgorithm.Sha512:
                return HashAlgorithmName.SHA512;
            default:
                throw new ArgumentOutOfRangeException("algorithm");
        }
    }
}
