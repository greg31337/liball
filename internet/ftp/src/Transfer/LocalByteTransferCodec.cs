using System.IO;

namespace ftp;

internal static class LocalByteTransferCodec
{
    internal static long EncodeStream(FtpTransferCodecRequest? request)
    {
        return ConvertStream(request, true);
    }

    internal static long DecodeStream(FtpTransferCodecRequest? request)
    {
        return ConvertStream(request, false);
    }

    internal static long ConvertStream(FtpTransferCodecRequest? request, bool encode)
    {
        if (request == null)
        {
            throw new FtpConfigurationException("A local-byte conversion request is required.", null);
        }

        Stream? sourceValue = request.Source;
        Stream? destinationValue = request.Destination;
        FtpTransferOptions? optionsValue = request.Options;
        if (sourceValue == null || destinationValue == null)
        {
            throw new FtpConfigurationException("Local-byte conversion requires source and destination streams.", null);
        }

        if (object.ReferenceEquals(sourceValue, destinationValue))
        {
            throw new FtpConfigurationException("Local-byte conversion requires distinct source and destination streams.", null);
        }

        FtpTransferValidator.ValidateTransferOptions(optionsValue);
        if (optionsValue == null)
        {
            throw new FtpConfigurationException("Transfer options are required.", null);
        }

        if (optionsValue.Type != FtpTransferType.LocalByte || optionsValue.LocalByteSize == 8)
        {
            throw new FtpConfigurationException("Local-byte packing requires TYPE L with a non-eight-bit size.", null);
        }

        if (!request.HasLogicalByteCount && request.LogicalByteCount != 0)
        {
            throw new FtpConfigurationException("A logical-byte count value requires its availability flag.", null);
        }

        if (request.HasLogicalByteCount && request.LogicalByteCount < 0)
        {
            throw new FtpConfigurationException("The logical-byte count cannot be negative.", null);
        }

        if (!encode && optionsValue.LocalByteSize < 8 && !request.HasLogicalByteCount)
        {
            throw new FtpConfigurationException("Decoding sub-byte TYPE L data requires an expected logical-byte count.", null);
        }

        if (!sourceValue.CanRead)
        {
            throw new FtpConfigurationException("The local-byte conversion source stream must be readable.", null);
        }

        if (!destinationValue.CanWrite)
        {
            throw new FtpConfigurationException("The local-byte conversion destination stream must be writable.", null);
        }

        FtpResourceLifetime sourceLifetime = new FtpResourceLifetime(sourceValue);
        FtpResourceLifetime destinationLifetime = new FtpResourceLifetime(destinationValue);
        if (request.LeaveSourceOpen)
        {
            sourceLifetime.ReleaseResource();
        }

        if (request.LeaveDestinationOpen)
        {
            destinationLifetime.ReleaseResource();
        }

        long bytesWritten = 0;
        FtpFaultIsolationResult conversionResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            byte[] inputBuffer = new byte[optionsValue.BufferBytes];
            FtpTransferOutputBuffer output = new FtpTransferOutputBuffer(destinationValue, optionsValue.BufferBytes);
            if (encode)
            {
                EncodeInput(sourceValue, inputBuffer, output, optionsValue.LocalByteSize,
                    request.HasLogicalByteCount, request.LogicalByteCount);
            }
            else
            {
                DecodeInput(sourceValue, inputBuffer, output, optionsValue.LocalByteSize,
                    request.HasLogicalByteCount, request.LogicalByteCount);
            }

            output.FlushPendingBytes();
            bytesWritten = output.BytesWritten;
            return null;
        });

        FtpFaultIsolationResult destinationCleanup = destinationLifetime.DisposeResource();
        FtpFaultIsolationResult sourceCleanup = sourceLifetime.DisposeResource();

        if (!conversionResult.Succeeded)
        {
            throw new FtpTransferException("The local-byte stream conversion failed.", conversionResult.Failure);
        }

        if (!destinationCleanup.Succeeded)
        {
            throw new FtpTransferException("The local-byte destination stream could not be closed.", destinationCleanup.Failure);
        }

        if (!sourceCleanup.Succeeded)
        {
            throw new FtpTransferException("The local-byte source stream could not be closed.", sourceCleanup.Failure);
        }

        return bytesWritten;
    }

    internal static void EncodeInput(
        Stream source,
        byte[] inputBuffer,
        FtpTransferOutputBuffer output,
        int localByteSize,
        bool hasLogicalByteCount,
        long logicalByteCount)
    {
        FtpLocalByteEncodingState state = new FtpLocalByteEncodingState(localByteSize, output);
        int bytesRead = source.Read(inputBuffer, 0, inputBuffer.Length);
        while (bytesRead != 0)
        {
            int index = 0;
            while (index < bytesRead)
            {
                state.ConsumeLocalByte(inputBuffer[index]);
                index++;
            }

            bytesRead = source.Read(inputBuffer, 0, inputBuffer.Length);
        }

        state.CompleteEncoding();
        if (hasLogicalByteCount && state.LogicalByteCount != logicalByteCount)
        {
            throw new InvalidDataException("The local TYPE L source does not match the expected logical-byte count.");
        }
    }

    internal static void DecodeInput(
        Stream source,
        byte[] inputBuffer,
        FtpTransferOutputBuffer output,
        int localByteSize,
        bool hasLogicalByteCount,
        long logicalByteCount)
    {
        FtpLocalByteDecodingState state = new FtpLocalByteDecodingState(
            localByteSize, hasLogicalByteCount, logicalByteCount, output);
        int bytesRead = source.Read(inputBuffer, 0, inputBuffer.Length);
        while (bytesRead != 0)
        {
            int index = 0;
            while (index < bytesRead)
            {
                state.ConsumeWireByte(inputBuffer[index]);
                index++;
            }

            bytesRead = source.Read(inputBuffer, 0, inputBuffer.Length);
        }

        state.CompleteDecoding();
    }
}
