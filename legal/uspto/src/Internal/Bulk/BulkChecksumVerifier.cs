namespace uspto;

internal class NormalizedChecksumValue
{
    internal string? Value = null;
    internal bool IsValid = false;
}

internal class BulkChecksumWorkItem
{
    internal ChecksumResult Result = new ChecksumResult();
    internal global::System.Security.Cryptography.IncrementalHash? Hash = null;
}

internal class BulkChecksumSession : global::System.IDisposable
{
    internal BulkChecksumWorkItem[] WorkItems;
    internal bool IsFinalized = false;

    internal BulkChecksumSession(ChecksumExpectation[] expectations)
    {
        ChecksumExpectation[] selected = BulkChecksumVerifier.SelectExpectations(expectations);
        WorkItems = new BulkChecksumWorkItem[selected.Length];
        int index;
        for (index = 0; index < selected.Length; index += 1)
        {
            ChecksumExpectation expectation = selected[index];
            BulkChecksumWorkItem item = new BulkChecksumWorkItem();
            item.Result.Expectation = new ChecksumExpectation();
            item.Result.Expectation.Algorithm = expectation.Algorithm;
            NormalizedChecksumValue normalized = BulkChecksumVerifier.NormalizeExpected(expectation);
            item.Result.Expectation.Value = normalized.Value;
            item.Result.IsAvailable = normalized.IsValid;
            if (BulkChecksumVerifier.IsSupportedAlgorithm(expectation.Algorithm))
            {
                item.Hash = global::System.Security.Cryptography.IncrementalHash.CreateHash(BulkChecksumVerifier.GetHashAlgorithmName(expectation.Algorithm));
            }

            WorkItems[index] = item;
        }
    }

    internal void Append(byte[] buffer, int offset, int count)
    {
        if (IsFinalized)
        {
            throw new ValidationException("The checksum session is already finalized.", "session", "CHECKSUM_SESSION_FINALIZED", null);
        }

        int index;
        for (index = 0; index < WorkItems.Length; index += 1)
        {
            global::System.Security.Cryptography.IncrementalHash? hash = WorkItems[index].Hash;
            if (hash != null)
            {
                hash.AppendData(buffer, offset, count);
            }
        }
    }

    internal ChecksumResult[] Complete()
    {
        if (IsFinalized)
        {
            throw new ValidationException("The checksum session is already finalized.", "session", "CHECKSUM_SESSION_FINALIZED", null);
        }

        IsFinalized = true;
        int index;
        for (index = 0; index < WorkItems.Length; index += 1)
        {
            BulkChecksumWorkItem item = WorkItems[index];
            global::System.Security.Cryptography.IncrementalHash? hash = item.Hash;
            if (hash != null)
            {
                byte[] digest = hash.GetHashAndReset();
                item.Result.ActualValue = global::System.Convert.ToHexString(digest).ToLowerInvariant();
                item.Result.IsComplete = true;
                item.Result.IsMatch = item.Result.IsAvailable && global::System.String.Equals(item.Result.Expectation.Value, item.Result.ActualValue, global::System.StringComparison.OrdinalIgnoreCase);
                hash.Dispose();
                item.Hash = null;
            }
        }

        return GetResults();
    }

    internal ChecksumResult[] MarkIncomplete()
    {
        if (!IsFinalized)
        {
            IsFinalized = true;
            int index;
            for (index = 0; index < WorkItems.Length; index += 1)
            {
                global::System.Security.Cryptography.IncrementalHash? hash = WorkItems[index].Hash;
                if (hash != null)
                {
                    hash.Dispose();
                    WorkItems[index].Hash = null;
                }

                WorkItems[index].Result.ActualValue = null;
                WorkItems[index].Result.IsComplete = false;
                WorkItems[index].Result.IsMatch = false;
            }
        }

        return GetResults();
    }

    internal ChecksumResult[] GetResults()
    {
        ChecksumResult[] results = new ChecksumResult[WorkItems.Length];
        int index;
        for (index = 0; index < WorkItems.Length; index += 1)
        {
            results[index] = WorkItems[index].Result;
        }

        return results;
    }

    public void Dispose()
    {
        if (!IsFinalized)
        {
            MarkIncomplete();
        }
    }
}

internal static class BulkChecksumVerifier
{
    internal static ChecksumResult[] Calculate(global::System.IO.Stream input, ChecksumExpectation[] expectations, int bufferSize, global::System.Threading.CancellationToken cancellationToken)
    {
        if (input == null || !input.CanRead)
        {
            throw new ValidationException("A readable checksum input stream is required.", "input", "STREAM_NOT_READABLE", null);
        }

        if (bufferSize <= 0)
        {
            throw new ValidationException("The checksum buffer size must be positive.", "bufferSize", "INVALID_LIMIT", null);
        }

        using (BulkChecksumSession session = new BulkChecksumSession(expectations))
        {
            byte[] buffer = new byte[bufferSize];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int read = input.Read(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    break;
                }

                session.Append(buffer, 0, read);
            }

            return session.Complete();
        }
    }

    internal static ChecksumResult[] VerifyFile(string filePath, ChecksumExpectation[] expectations, int bufferSize, global::System.Threading.CancellationToken cancellationToken)
    {
        if (global::System.String.IsNullOrWhiteSpace(filePath) || !global::System.IO.File.Exists(filePath))
        {
            throw new ValidationException("A checksum file is required.", "filePath", "FILE_NOT_FOUND", null);
        }

        using (global::System.IO.FileStream input = new global::System.IO.FileStream(filePath, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Read, global::System.IO.FileShare.Read, bufferSize, global::System.IO.FileOptions.SequentialScan))
        {
            return Calculate(input, expectations, bufferSize, cancellationToken);
        }
    }

    internal static ChecksumExpectation[] SelectExpectations(ChecksumExpectation[] expectations)
    {
        if (expectations == null)
        {
            throw new ValidationException("Checksum expectations are required.", "expectations", "REQUIRED", null);
        }

        bool hasSha256 = false;
        int index;
        for (index = 0; index < expectations.Length; index += 1)
        {
            if (expectations[index] != null && expectations[index].Algorithm == ChecksumAlgorithm.Sha256)
            {
                hasSha256 = true;
            }
        }

        int count = expectations.Length + (hasSha256 ? 0 : 1);
        ChecksumExpectation[] selected = new ChecksumExpectation[count];
        for (index = 0; index < expectations.Length; index += 1)
        {
            if (expectations[index] == null)
            {
                throw new ValidationException("Checksum expectations cannot contain null entries.", "expectations", "REQUIRED", null);
            }

            selected[index] = expectations[index];
        }

        if (!hasSha256)
        {
            selected[selected.Length - 1] = new ChecksumExpectation();
            selected[selected.Length - 1].Algorithm = ChecksumAlgorithm.Sha256;
        }

        return selected;
    }

    internal static NormalizedChecksumValue NormalizeExpected(ChecksumExpectation expectation)
    {
        NormalizedChecksumValue result = new NormalizedChecksumValue();
        if (expectation == null || global::System.String.IsNullOrWhiteSpace(expectation.Value))
        {
            return result;
        }

        int digestBytes = GetDigestBytes(expectation.Algorithm);
        if (digestBytes == 0)
        {
            return result;
        }

        string value = expectation.Value.Trim();
        if (value.Length == digestBytes * 2 && IsHex(value))
        {
            result.Value = value.ToLowerInvariant();
            result.IsValid = true;
            return result;
        }

        if (!IsValidBase64(value, digestBytes))
        {
            return result;
        }

        byte[] decoded = global::System.Convert.FromBase64String(value);
        result.Value = global::System.Convert.ToHexString(decoded).ToLowerInvariant();
        result.IsValid = true;
        return result;
    }

    internal static bool IsHex(string value)
    {
        int index;
        for (index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            bool digit = character >= '0' && character <= '9';
            bool lower = character >= 'a' && character <= 'f';
            bool upper = character >= 'A' && character <= 'F';
            if (!digit && !lower && !upper)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsValidBase64(string value, int expectedBytes)
    {
        if (value.Length == 0 || value.Length % 4 != 0)
        {
            return false;
        }

        int padding = 0;
        if (value[value.Length - 1] == '=')
        {
            padding += 1;
        }

        if (value.Length >= 2 && value[value.Length - 2] == '=')
        {
            padding += 1;
        }

        int decodedBytes = value.Length / 4 * 3 - padding;
        if (decodedBytes != expectedBytes)
        {
            return false;
        }

        int index;
        for (index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            bool letter = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z';
            bool digit = character >= '0' && character <= '9';
            bool symbol = character == '+' || character == '/';
            bool isPadding = character == '=' && index >= value.Length - padding;
            if (!letter && !digit && !symbol && !isPadding)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsSupportedAlgorithm(ChecksumAlgorithm algorithm)
    {
        return GetDigestBytes(algorithm) != 0;
    }

    internal static int GetDigestBytes(ChecksumAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case ChecksumAlgorithm.Md5:
                return 16;
            case ChecksumAlgorithm.Sha1:
                return 20;
            case ChecksumAlgorithm.Sha256:
                return 32;
            case ChecksumAlgorithm.Sha384:
                return 48;
            case ChecksumAlgorithm.Sha512:
                return 64;
            default:
                return 0;
        }
    }

    internal static global::System.Security.Cryptography.HashAlgorithmName GetHashAlgorithmName(ChecksumAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case ChecksumAlgorithm.Md5:
                return global::System.Security.Cryptography.HashAlgorithmName.MD5;
            case ChecksumAlgorithm.Sha1:
                return global::System.Security.Cryptography.HashAlgorithmName.SHA1;
            case ChecksumAlgorithm.Sha256:
                return global::System.Security.Cryptography.HashAlgorithmName.SHA256;
            case ChecksumAlgorithm.Sha384:
                return global::System.Security.Cryptography.HashAlgorithmName.SHA384;
            case ChecksumAlgorithm.Sha512:
                return global::System.Security.Cryptography.HashAlgorithmName.SHA512;
            default:
                throw new ValidationException("The checksum algorithm is not supported.", "Algorithm", "CHECKSUM_ALGORITHM_UNAVAILABLE", null);
        }
    }

    internal static bool HasMismatch(ChecksumResult[] results)
    {
        int index;
        for (index = 0; index < results.Length; index += 1)
        {
            if (results[index].IsAvailable && results[index].IsComplete && !results[index].IsMatch)
            {
                return true;
            }
        }

        return false;
    }
}
