using System.IO;
using System.IO.Compression;

namespace ftp;

internal static class ModeZCodec
{
    internal static long EncodeStream(
        FtpTransferCodecRequest? request,
        bool extensionEnabled)
    {
        return TransformStream(request, extensionEnabled, true);
    }

    internal static long DecodeStream(
        FtpTransferCodecRequest? request,
        bool extensionEnabled)
    {
        return TransformStream(request, extensionEnabled, false);
    }

    internal static long TransformStream(
        FtpTransferCodecRequest? request,
        bool extensionEnabled,
        bool encode)
    {
        if (!extensionEnabled)
        {
            throw new FtpConfigurationException("MODE Z is disabled unless the compatibility extension is explicitly enabled.", null);
        }

        if (request == null)
        {
            throw new FtpConfigurationException("A MODE Z conversion request is required.", null);
        }

        Stream? sourceValue = request.Source;
        Stream? destinationValue = request.Destination;
        FtpTransferOptions? optionsValue = request.Options;
        if (sourceValue == null || destinationValue == null)
        {
            throw new FtpConfigurationException("MODE Z requires source and destination streams.", null);
        }

        if (object.ReferenceEquals(sourceValue, destinationValue))
        {
            throw new FtpConfigurationException("MODE Z requires distinct source and destination streams.", null);
        }

        FtpTransferValidator.ValidateTransferOptions(optionsValue);
        if (optionsValue == null)
        {
            throw new FtpConfigurationException("Transfer options are required.", null);
        }

        if (optionsValue.Mode != FtpTransferMode.Deflate)
        {
            throw new FtpConfigurationException("MODE Z Deflate wrapping is distinct from RFC 959 MODE C.", null);
        }

        if (!sourceValue.CanRead)
        {
            throw new FtpConfigurationException("The MODE Z source stream must be readable.", null);
        }

        if (!destinationValue.CanWrite)
        {
            throw new FtpConfigurationException("The MODE Z destination stream must be writable.", null);
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

        Stream? deflateValue = null;
        FtpFaultIsolationResult creationResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            if (encode)
            {
                deflateValue = CreateEncodingStream(destinationValue);
            }
            else
            {
                deflateValue = CreateDecodingStream(sourceValue);
            }

            return null;
        });

        FtpResourceLifetime deflateLifetime = new FtpResourceLifetime(deflateValue);
        long bytesProcessed = 0;
        FtpFaultIsolationResult conversionResult = new FtpFaultIsolationResult();
        if (creationResult.Succeeded && deflateValue != null)
        {
            Stream transformValue = deflateValue;
            conversionResult = FtpFaultIsolationRunner.RunOperation(() =>
            {
                byte[] buffer = new byte[optionsValue.BufferBytes];
                if (encode)
                {
                    int bytesRead = sourceValue.Read(buffer, 0, buffer.Length);
                    while (bytesRead != 0)
                    {
                        AddProcessedBytes(bytesProcessed, bytesRead);
                        transformValue.Write(buffer, 0, bytesRead);
                        bytesProcessed += bytesRead;
                        bytesRead = sourceValue.Read(buffer, 0, buffer.Length);
                    }

                    transformValue.Flush();
                }
                else
                {
                    int bytesRead = transformValue.Read(buffer, 0, buffer.Length);
                    while (bytesRead != 0)
                    {
                        AddProcessedBytes(bytesProcessed, bytesRead);
                        destinationValue.Write(buffer, 0, bytesRead);
                        bytesProcessed += bytesRead;
                        bytesRead = transformValue.Read(buffer, 0, buffer.Length);
                    }
                }

                return null;
            });
        }

        FtpFaultIsolationResult deflateCleanup = deflateLifetime.DisposeResource();
        FtpFaultIsolationResult destinationCleanup = destinationLifetime.DisposeResource();
        FtpFaultIsolationResult sourceCleanup = sourceLifetime.DisposeResource();

        if (!creationResult.Succeeded)
        {
            throw new FtpTransferException("The MODE Z compression stream could not be created.", creationResult.Failure);
        }

        if (!conversionResult.Succeeded)
        {
            throw new FtpTransferException("The MODE Z stream conversion failed.", conversionResult.Failure);
        }

        if (!deflateCleanup.Succeeded)
        {
            throw new FtpTransferException("The MODE Z compression stream could not be finalized.", deflateCleanup.Failure);
        }

        if (!destinationCleanup.Succeeded)
        {
            throw new FtpTransferException("The MODE Z destination stream could not be closed.", destinationCleanup.Failure);
        }

        if (!sourceCleanup.Succeeded)
        {
            throw new FtpTransferException("The MODE Z source stream could not be closed.", sourceCleanup.Failure);
        }

        return bytesProcessed;
    }

    internal static Stream CreateEncodingStream(Stream destination)
    {
        return new ZLibStream(
            destination, CompressionLevel.Optimal, true);
    }

    internal static Stream CreateDecodingStream(Stream source)
    {
        return new ZLibStream(
            source, CompressionMode.Decompress, true);
    }

    internal static void AddProcessedBytes(long currentCount, int additionalCount)
    {
        if (currentCount > long.MaxValue - additionalCount)
        {
            throw new FtpTransferException("The MODE Z logical byte count exceeded Int64 capacity.", null);
        }
    }
}
