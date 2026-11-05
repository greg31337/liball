using System;

namespace ftp;

internal static class TransferVerifier
{
    internal static FtpTransferResult Verify(FtpTransferVerificationRequest? request)
    {
        ValidateRequest(request);
        if (request == null)
        {
            throw new FtpConfigurationException("A transfer verification request is required.", null);
        }

        FtpTransferResult result = new FtpTransferResult();
        result.VerificationMode = request.Mode;
        result.VerificationAttempted = request.Mode != FtpVerificationMode.None;
        result.HasActualSize = request.HasActualSize;
        result.ActualSize = request.ActualSize;

        if (request.Mode == FtpVerificationMode.None)
        {
            return result;
        }

        if (request.Mode == FtpVerificationMode.Size)
        {
            result.Verified = HasMatchingSize(request);
            return result;
        }

        if (request.Mode == FtpVerificationMode.ModifiedTime)
        {
            result.Verified = HasMatchingModifiedTime(request);
            return result;
        }

        if (request.Mode == FtpVerificationMode.SizeAndModifiedTime)
        {
            result.Verified = HasMatchingSize(request) && HasMatchingModifiedTime(request);
            return result;
        }

        ApplyChecksumVerification(request, result);
        return result;
    }

    internal static bool HasMatchingSize(FtpTransferVerificationRequest request)
    {
        return request.HasActualSize && request.ActualSize == request.ExpectedSize;
    }

    internal static bool HasMatchingModifiedTime(FtpTransferVerificationRequest request)
    {
        if (request.ExpectedModifiedTime == null || request.ActualModifiedTime == null ||
            !request.ActualModifiedTime.HasValue)
        {
            return false;
        }

        FtpTimestamp expected = request.ExpectedModifiedTime;
        FtpTimestamp actual = request.ActualModifiedTime;
        ValidateTimestamp(expected, true);
        ValidateTimestamp(actual, false);
        return expected.ValueUtc == actual.ValueUtc &&
            expected.LeapSecond == actual.LeapSecond &&
            NormalizeFraction(expected.FractionalSeconds) ==
                NormalizeFraction(actual.FractionalSeconds);
    }

    internal static void ApplyChecksumVerification(
        FtpTransferVerificationRequest request,
        FtpTransferResult result)
    {
        string expectedAlgorithm = TransferHash.NormalizeAlgorithm(request.ExpectedChecksumAlgorithm);
        string? expectedChecksum = TransferHash.NormalizeChecksum(
            request.ExpectedChecksum, expectedAlgorithm);
        if (expectedChecksum == null)
        {
            throw new FtpConfigurationException(
                "The expected checksum is not valid hexadecimal text for its algorithm.", null);
        }

        result.ChecksumAlgorithm = expectedAlgorithm;
        result.ExpectedChecksum = expectedChecksum;
        string? actualAlgorithm = TransferHash.TryNormalizeAlgorithm(request.ActualChecksumAlgorithm);
        if (actualAlgorithm == null || actualAlgorithm != expectedAlgorithm)
        {
            result.ActualChecksum = request.ActualChecksum;
            return;
        }

        string? actualChecksum = TransferHash.NormalizeChecksum(request.ActualChecksum, actualAlgorithm);
        result.ActualChecksum = actualChecksum == null ? request.ActualChecksum : actualChecksum;
        result.Verified = actualChecksum != null && actualChecksum == expectedChecksum;
    }

    internal static string NormalizeFraction(string fraction)
    {
        int length = fraction.Length;
        while (length > 0 && fraction[length - 1] == '0')
        {
            length--;
        }

        return fraction.Substring(0, length);
    }

    internal static void ValidateRequest(FtpTransferVerificationRequest? request)
    {
        if (request == null)
        {
            throw new FtpConfigurationException("A transfer verification request is required.", null);
        }

        if (request.Mode != FtpVerificationMode.None &&
            request.Mode != FtpVerificationMode.Size &&
            request.Mode != FtpVerificationMode.ModifiedTime &&
            request.Mode != FtpVerificationMode.SizeAndModifiedTime &&
            request.Mode != FtpVerificationMode.Checksum)
        {
            throw new FtpConfigurationException("The transfer verification mode is invalid.", null);
        }

        if (request.HasExpectedSize)
        {
            if (request.ExpectedSize < 0)
            {
                throw new FtpConfigurationException("An expected verification size cannot be negative.", null);
            }
        }
        else if (request.ExpectedSize != 0)
        {
            throw new FtpConfigurationException(
                "An expected verification size requires its availability flag.", null);
        }

        if (request.HasActualSize)
        {
            if (request.ActualSize < 0)
            {
                throw new FtpTransferException("An observed verification size cannot be negative.", null);
            }
        }
        else if (request.ActualSize != 0)
        {
            throw new FtpTransferException(
                "An observed verification size requires its availability flag.", null);
        }

        bool needsSize = request.Mode == FtpVerificationMode.Size ||
            request.Mode == FtpVerificationMode.SizeAndModifiedTime;
        if (needsSize && !request.HasExpectedSize)
        {
            throw new FtpConfigurationException("Size verification requires an expected size.", null);
        }

        bool needsModifiedTime = request.Mode == FtpVerificationMode.ModifiedTime ||
            request.Mode == FtpVerificationMode.SizeAndModifiedTime;
        if (needsModifiedTime)
        {
            if (request.ExpectedModifiedTime == null || !request.ExpectedModifiedTime.HasValue)
            {
                throw new FtpConfigurationException(
                    "Modification-time verification requires an expected UTC timestamp.", null);
            }

            ValidateTimestamp(request.ExpectedModifiedTime, true);
        }

        if (request.ActualModifiedTime != null && request.ActualModifiedTime.HasValue)
        {
            ValidateTimestamp(request.ActualModifiedTime, false);
        }

        if (request.Mode == FtpVerificationMode.Checksum)
        {
            string algorithm = TransferHash.NormalizeAlgorithm(request.ExpectedChecksumAlgorithm);
            if (TransferHash.NormalizeChecksum(request.ExpectedChecksum, algorithm) == null)
            {
                throw new FtpConfigurationException(
                    "Checksum verification requires valid expected hexadecimal text.", null);
            }
        }
    }

    internal static void ValidateTimestamp(FtpTimestamp timestamp, bool expected)
    {
        string? fraction = timestamp.FractionalSeconds;
        bool valid = timestamp.HasValue && timestamp.ValueUtc.Kind == DateTimeKind.Utc &&
            fraction != null;
        if (valid && fraction != null)
        {
            int index = 0;
            while (index < fraction.Length)
            {
                char character = fraction[index];
                if (character < '0' || character > '9')
                {
                    valid = false;
                }

                index++;
            }
        }

        if (valid && fraction != null)
        {
            long actualTicks = timestamp.ValueUtc.Ticks % TimeSpan.TicksPerSecond;
            long representedTicks = FtpTimestampCodec.ParseFractionalTicks(fraction);
            valid = actualTicks == representedTicks &&
                (!timestamp.LeapSecond || timestamp.ValueUtc.Second == 59);
        }

        if (!valid && expected)
        {
            throw new FtpConfigurationException(
                "The expected modification timestamp is not a consistent UTC value.", null);
        }

        if (!valid)
        {
            throw new FtpTransferException(
                "The observed modification timestamp is not a consistent UTC value.", null);
        }
    }
}
