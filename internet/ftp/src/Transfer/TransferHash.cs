using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace ftp;

internal static class TransferHash
{
    internal static FtpTransferHashResult Calculate(FtpTransferHashRequest? request)
    {
        ValidateRequest(request);
        if (request == null || request.Source == null)
        {
            throw new FtpConfigurationException("A complete local hash request is required.", null);
        }

        string algorithm = NormalizeAlgorithm(request.Algorithm);
        Stream source = request.Source;
        FtpDeadline deadline = FtpTimeout.StartDeadline(request.DeadlineMilliseconds);
        FtpTransferHashResult result = new FtpTransferHashResult();
        result.Algorithm = algorithm;
        FtpResourceLifetime sourceLifetime = new FtpResourceLifetime(source);
        if (request.LeaveOpen)
        {
            sourceLifetime.ReleaseResource();
        }

        FtpFaultIsolationResult calculation = FtpFaultIsolationRunner.RunOperation(() =>
        {
            if (algorithm == "CRC32")
            {
                result.Checksum = CalculateCrc32(
                    source, request.BufferBytes, request.CancellationToken, deadline, result);
            }
            else
            {
                result.Checksum = CalculateCryptographic(
                    source, algorithm, request.BufferBytes,
                    request.CancellationToken, deadline, result);
            }

            return null;
        });
        FtpFaultIsolationResult cleanup = sourceLifetime.DisposeResource();
        ThrowFailure(calculation, cleanup, request.CancellationToken, deadline);
        return result;
    }

    internal static string CalculateCryptographic(
        Stream source,
        string algorithm,
        int bufferBytes,
        CancellationToken cancellationToken,
        FtpDeadline deadline,
        FtpTransferHashResult result)
    {
        using (IncrementalHash hash = CreateIncrementalHash(algorithm))
        {
            byte[] buffer = new byte[bufferBytes];
            TransferPump.CheckOperationState(cancellationToken, deadline);
            int count = source.Read(buffer, 0, buffer.Length);
            while (count != 0)
            {
                TransferPump.ValidateReadCount(count, buffer.Length);
                hash.AppendData(buffer, 0, count);
                AddBytesRead(result, count);
                TransferPump.CheckOperationState(cancellationToken, deadline);
                count = source.Read(buffer, 0, buffer.Length);
            }

            TransferPump.ValidateReadCount(count, buffer.Length);
            TransferPump.CheckOperationState(cancellationToken, deadline);
            return Convert.ToHexString(hash.GetHashAndReset());
        }
    }

    internal static string CalculateCrc32(
        Stream source,
        int bufferBytes,
        CancellationToken cancellationToken,
        FtpDeadline deadline,
        FtpTransferHashResult result)
    {
        uint crc = 0xFFFFFFFFU;
        byte[] buffer = new byte[bufferBytes];
        TransferPump.CheckOperationState(cancellationToken, deadline);
        int count = source.Read(buffer, 0, buffer.Length);
        while (count != 0)
        {
            TransferPump.ValidateReadCount(count, buffer.Length);
            int index = 0;
            while (index < count)
            {
                crc ^= buffer[index];
                int bit = 0;
                while (bit < 8)
                {
                    uint polynomial = (crc & 1U) == 1U ? 0xEDB88320U : 0U;
                    crc = (crc >> 1) ^ polynomial;
                    bit++;
                }

                index++;
            }

            AddBytesRead(result, count);
            TransferPump.CheckOperationState(cancellationToken, deadline);
            count = source.Read(buffer, 0, buffer.Length);
        }

        TransferPump.ValidateReadCount(count, buffer.Length);
        TransferPump.CheckOperationState(cancellationToken, deadline);
        crc = ~crc;
        return crc.ToString("X8", CultureInfo.InvariantCulture);
    }

    internal static IncrementalHash CreateIncrementalHash(string algorithm)
    {
        if (algorithm == "MD5")
        {
            return IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        }

        if (algorithm == "SHA-1")
        {
            return IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        }

        if (algorithm == "SHA-256")
        {
            return IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        }

        return IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
    }

    internal static string NormalizeAlgorithm(string? algorithm)
    {
        string? normalized = TryNormalizeAlgorithm(algorithm);
        if (normalized == null)
        {
            throw new FtpConfigurationException(
                "A supported checksum algorithm is required: MD5, SHA-1, SHA-256, SHA-512, or CRC32.", null);
        }

        return normalized;
    }

    internal static string? TryNormalizeAlgorithm(string? algorithm)
    {
        if (string.IsNullOrWhiteSpace(algorithm))
        {
            return null;
        }

        string value = algorithm.Trim().ToUpperInvariant();
        if (value == "MD5")
        {
            return "MD5";
        }

        if (value == "SHA1" || value == "SHA-1")
        {
            return "SHA-1";
        }

        if (value == "SHA256" || value == "SHA-256")
        {
            return "SHA-256";
        }

        if (value == "SHA512" || value == "SHA-512")
        {
            return "SHA-512";
        }

        if (value == "CRC32" || value == "CRC-32")
        {
            return "CRC32";
        }

        return null;
    }

    internal static string? NormalizeChecksum(string? checksum, string algorithm)
    {
        if (string.IsNullOrWhiteSpace(checksum))
        {
            return null;
        }

        string value = checksum.Trim();
        if (value.Length != GetChecksumLength(algorithm))
        {
            return null;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool decimalDigit = character >= '0' && character <= '9';
            bool lowerHex = character >= 'a' && character <= 'f';
            bool upperHex = character >= 'A' && character <= 'F';
            if (!decimalDigit && !lowerHex && !upperHex)
            {
                return null;
            }

            index++;
        }

        return value.ToUpperInvariant();
    }

    internal static int GetChecksumLength(string algorithm)
    {
        if (algorithm == "MD5")
        {
            return 32;
        }

        if (algorithm == "SHA-1")
        {
            return 40;
        }

        if (algorithm == "SHA-256")
        {
            return 64;
        }

        if (algorithm == "SHA-512")
        {
            return 128;
        }

        return 8;
    }

    internal static void AddBytesRead(FtpTransferHashResult result, int count)
    {
        TransferPump.AddByteCount(result.BytesRead, count, "hash source");
        result.BytesRead += count;
    }

    internal static void ValidateRequest(FtpTransferHashRequest? request)
    {
        if (request == null || request.Source == null)
        {
            throw new FtpConfigurationException("A complete local hash request is required.", null);
        }

        NormalizeAlgorithm(request.Algorithm);
        if (!request.Source.CanRead)
        {
            throw new FtpConfigurationException("The local hash source must be readable.", null);
        }

        if (request.BufferBytes <= 0)
        {
            throw new FtpConfigurationException("The local hash buffer size must be positive.", null);
        }

        if (request.DeadlineMilliseconds < Timeout.Infinite)
        {
            throw new FtpConfigurationException(
                "A local hash deadline must be non-negative or Timeout.Infinite.", null);
        }
    }

    internal static void ThrowFailure(
        FtpFaultIsolationResult calculation,
        FtpFaultIsolationResult cleanup,
        CancellationToken cancellationToken,
        FtpDeadline deadline)
    {
        if (FtpCancellation.IsCancellationRequested(cancellationToken))
        {
            throw new OperationCanceledException(
                "The local checksum calculation was canceled.", calculation.Failure, cancellationToken);
        }

        if (FtpTimeout.HasDeadlineExpired(deadline))
        {
            throw new TimeoutException("The local checksum calculation timed out.", calculation.Failure);
        }

        if (!calculation.Succeeded)
        {
            throw new FtpTransferException("The local checksum calculation failed.", calculation.Failure);
        }

        if (!cleanup.Succeeded)
        {
            throw new FtpTransferException("The local checksum source could not be closed.", cleanup.Failure);
        }
    }
}
