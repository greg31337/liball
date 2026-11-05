using System.IO;

namespace ftp;

internal static class AsciiTransferCodec
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
            throw new FtpConfigurationException("An ASCII conversion request is required.", null);
        }

        Stream? sourceValue = request.Source;
        Stream? destinationValue = request.Destination;
        FtpTransferOptions? optionsValue = request.Options;
        if (sourceValue == null || destinationValue == null)
        {
            throw new FtpConfigurationException("ASCII conversion requires source and destination streams.", null);
        }

        if (object.ReferenceEquals(sourceValue, destinationValue))
        {
            throw new FtpConfigurationException("ASCII conversion requires distinct source and destination streams.", null);
        }

        FtpTransferValidator.ValidateTransferOptions(optionsValue);
        if (optionsValue == null)
        {
            throw new FtpConfigurationException("Transfer options are required.", null);
        }

        if (optionsValue.Type != FtpTransferType.Ascii)
        {
            throw new FtpConfigurationException("ASCII conversion requires TYPE A.", null);
        }

        if (!sourceValue.CanRead)
        {
            throw new FtpConfigurationException("The ASCII conversion source stream must be readable.", null);
        }

        if (!destinationValue.CanWrite)
        {
            throw new FtpConfigurationException("The ASCII conversion destination stream must be writable.", null);
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
                if (optionsValue.TextFormat == FtpTextFormat.CarriageControl)
                {
                    EncodeCarriageControlInput(sourceValue, inputBuffer, output);
                }
                else
                {
                    EncodeInput(sourceValue, inputBuffer, output);
                }
            }
            else
            {
                if (optionsValue.TextFormat == FtpTextFormat.CarriageControl)
                {
                    DecodeCarriageControlInput(sourceValue, inputBuffer, output);
                }
                else
                {
                    DecodeInput(sourceValue, inputBuffer, output);
                }
            }

            output.FlushPendingBytes();
            bytesWritten = output.BytesWritten;
            return null;
        });

        FtpFaultIsolationResult destinationCleanup = destinationLifetime.DisposeResource();
        FtpFaultIsolationResult sourceCleanup = sourceLifetime.DisposeResource();

        if (!conversionResult.Succeeded)
        {
            throw new FtpTransferException("The ASCII stream conversion failed.", conversionResult.Failure);
        }

        if (!destinationCleanup.Succeeded)
        {
            throw new FtpTransferException("The ASCII conversion destination stream could not be closed.", destinationCleanup.Failure);
        }

        if (!sourceCleanup.Succeeded)
        {
            throw new FtpTransferException("The ASCII conversion source stream could not be closed.", sourceCleanup.Failure);
        }

        return bytesWritten;
    }

    internal static void EncodeInput(Stream source, byte[] inputBuffer, FtpTransferOutputBuffer output)
    {
        bool pendingCarriageReturn = false;
        int bytesRead = source.Read(inputBuffer, 0, inputBuffer.Length);
        while (bytesRead != 0)
        {
            int index = 0;
            while (index < bytesRead)
            {
                byte value = inputBuffer[index];
                ValidateAsciiByte(value);
                if (pendingCarriageReturn)
                {
                    output.WriteByte(13);
                    if (value == 10)
                    {
                        output.WriteByte(10);
                        pendingCarriageReturn = false;
                        index++;
                        continue;
                    }

                    output.WriteByte(0);
                    pendingCarriageReturn = false;
                }

                if (value == 13)
                {
                    pendingCarriageReturn = true;
                }
                else if (value == 10)
                {
                    output.WriteByte(13);
                    output.WriteByte(10);
                }
                else
                {
                    output.WriteByte(value);
                }

                index++;
            }

            bytesRead = source.Read(inputBuffer, 0, inputBuffer.Length);
        }

        if (pendingCarriageReturn)
        {
            output.WriteByte(13);
            output.WriteByte(0);
        }
    }

    internal static void DecodeInput(Stream source, byte[] inputBuffer, FtpTransferOutputBuffer output)
    {
        bool pendingCarriageReturn = false;
        int bytesRead = source.Read(inputBuffer, 0, inputBuffer.Length);
        while (bytesRead != 0)
        {
            int index = 0;
            while (index < bytesRead)
            {
                byte value = inputBuffer[index];
                ValidateAsciiByte(value);
                if (pendingCarriageReturn)
                {
                    if (value == 10)
                    {
                        WriteLocalLineEnding(output);
                    }
                    else if (value == 0)
                    {
                        output.WriteByte(13);
                    }
                    else
                    {
                        throw new InvalidDataException("A network-ASCII carriage return must be followed by LF or NUL.");
                    }

                    pendingCarriageReturn = false;
                }
                else if (value == 13)
                {
                    pendingCarriageReturn = true;
                }
                else
                {
                    output.WriteByte(value);
                }

                index++;
            }

            bytesRead = source.Read(inputBuffer, 0, inputBuffer.Length);
        }

        if (pendingCarriageReturn)
        {
            throw new InvalidDataException("A network-ASCII stream ended after a carriage return.");
        }
    }

    internal static void EncodeCarriageControlInput(Stream source, byte[] inputBuffer, FtpTransferOutputBuffer output)
    {
        bool pendingCarriageReturn = false;
        bool atRecordStart = true;
        int bytesRead = source.Read(inputBuffer, 0, inputBuffer.Length);
        while (bytesRead != 0)
        {
            int index = 0;
            while (index < bytesRead)
            {
                byte value = inputBuffer[index];
                ValidateAsciiByte(value);
                if (atRecordStart)
                {
                    ValidateCarriageControl(value);
                    atRecordStart = false;
                }

                if (pendingCarriageReturn)
                {
                    output.WriteByte(13);
                    if (value == 10)
                    {
                        output.WriteByte(10);
                        pendingCarriageReturn = false;
                        atRecordStart = true;
                        index++;
                        continue;
                    }

                    output.WriteByte(0);
                    pendingCarriageReturn = false;
                }

                if (value == 13)
                {
                    pendingCarriageReturn = true;
                }
                else if (value == 10)
                {
                    output.WriteByte(13);
                    output.WriteByte(10);
                    atRecordStart = true;
                }
                else
                {
                    output.WriteByte(value);
                }

                index++;
            }

            bytesRead = source.Read(inputBuffer, 0, inputBuffer.Length);
        }

        if (pendingCarriageReturn)
        {
            output.WriteByte(13);
            output.WriteByte(0);
        }
    }

    internal static void DecodeCarriageControlInput(Stream source, byte[] inputBuffer, FtpTransferOutputBuffer output)
    {
        bool pendingCarriageReturn = false;
        bool atRecordStart = true;
        int bytesRead = source.Read(inputBuffer, 0, inputBuffer.Length);
        while (bytesRead != 0)
        {
            int index = 0;
            while (index < bytesRead)
            {
                byte value = inputBuffer[index];
                ValidateAsciiByte(value);
                if (atRecordStart)
                {
                    ValidateCarriageControl(value);
                    atRecordStart = false;
                }

                if (pendingCarriageReturn)
                {
                    if (value == 10)
                    {
                        WriteLocalLineEnding(output);
                        atRecordStart = true;
                    }
                    else if (value == 0)
                    {
                        output.WriteByte(13);
                    }
                    else
                    {
                        throw new InvalidDataException("A network-ASCII carriage return must be followed by LF or NUL.");
                    }

                    pendingCarriageReturn = false;
                }
                else if (value == 13)
                {
                    pendingCarriageReturn = true;
                }
                else
                {
                    output.WriteByte(value);
                }

                index++;
            }

            bytesRead = source.Read(inputBuffer, 0, inputBuffer.Length);
        }

        if (pendingCarriageReturn)
        {
            throw new InvalidDataException("A network-ASCII stream ended after a carriage return.");
        }
    }

    internal static void ValidateCarriageControl(byte value)
    {
        if (value != 32 && value != 48 && value != 49 && value != 43)
        {
            throw new InvalidDataException("An ASA record must start with space, 0, 1, or + carriage control.");
        }
    }

    internal static void ValidateAsciiByte(byte value)
    {
        if (value > 127)
        {
            throw new InvalidDataException("TYPE A data must use the 7-bit NVT-ASCII character set.");
        }
    }

    internal static void WriteLocalLineEnding(FtpTransferOutputBuffer output)
    {
        if (System.Environment.NewLine.Length == 2)
        {
            output.WriteByte(13);
        }

        output.WriteByte(10);
    }
}
