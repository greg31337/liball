using System.IO;

namespace ftp;

internal static class FtpZeroConversionCodec
{
    internal static long CopyStream(FtpTransferCodecRequest? request)
    {
        if (request == null)
        {
            throw new FtpConfigurationException("A zero-conversion request is required.", null);
        }

        Stream? sourceValue = request.Source;
        Stream? destinationValue = request.Destination;
        FtpTransferOptions? optionsValue = request.Options;
        if (sourceValue == null || destinationValue == null)
        {
            throw new FtpConfigurationException("Zero-conversion requires source and destination streams.", null);
        }

        if (object.ReferenceEquals(sourceValue, destinationValue))
        {
            throw new FtpConfigurationException("Zero-conversion requires distinct source and destination streams.", null);
        }

        FtpTransferValidator.ValidateTransferOptions(optionsValue);
        if (optionsValue == null)
        {
            throw new FtpConfigurationException("Transfer options are required.", null);
        }

        if (optionsValue.Type != FtpTransferType.Image &&
            (optionsValue.Type != FtpTransferType.LocalByte || optionsValue.LocalByteSize != 8))
        {
            throw new FtpConfigurationException("Zero-conversion supports only TYPE I and TYPE L 8.", null);
        }

        if (!sourceValue.CanRead)
        {
            throw new FtpConfigurationException("The zero-conversion source stream must be readable.", null);
        }

        if (!destinationValue.CanWrite)
        {
            throw new FtpConfigurationException("The zero-conversion destination stream must be writable.", null);
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

        long bytesCopied = 0;
        FtpFaultIsolationResult copyResult = FtpFaultIsolationRunner.RunOperation(() =>
        {
            byte[] buffer = new byte[optionsValue.BufferBytes];
            int bytesRead = sourceValue.Read(buffer, 0, buffer.Length);
            while (bytesRead != 0)
            {
                if (bytesCopied > long.MaxValue - bytesRead)
                {
                    throw new FtpTransferException("The zero-conversion byte count exceeded Int64 capacity.", null);
                }

                destinationValue.Write(buffer, 0, bytesRead);
                bytesCopied += bytesRead;
                bytesRead = sourceValue.Read(buffer, 0, buffer.Length);
            }

            return null;
        });

        FtpFaultIsolationResult destinationCleanup = destinationLifetime.DisposeResource();
        FtpFaultIsolationResult sourceCleanup = sourceLifetime.DisposeResource();

        if (!copyResult.Succeeded)
        {
            throw new FtpTransferException("The zero-conversion stream copy failed.", copyResult.Failure);
        }

        if (!destinationCleanup.Succeeded)
        {
            throw new FtpTransferException("The zero-conversion destination stream could not be closed.", destinationCleanup.Failure);
        }

        if (!sourceCleanup.Succeeded)
        {
            throw new FtpTransferException("The zero-conversion source stream could not be closed.", sourceCleanup.Failure);
        }

        return bytesCopied;
    }
}
