using System.IO;

namespace ftp;

internal static class EbcdicTransferCodec
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
            throw new FtpConfigurationException("An EBCDIC conversion request is required.", null);
        }

        Stream? sourceValue = request.Source;
        Stream? destinationValue = request.Destination;
        FtpTransferOptions? optionsValue = request.Options;
        if (sourceValue == null || destinationValue == null)
        {
            throw new FtpConfigurationException("EBCDIC conversion requires source and destination streams.", null);
        }

        if (object.ReferenceEquals(sourceValue, destinationValue))
        {
            throw new FtpConfigurationException("EBCDIC conversion requires distinct source and destination streams.", null);
        }

        FtpTransferValidator.ValidateTransferOptions(optionsValue);
        if (optionsValue == null)
        {
            throw new FtpConfigurationException("Transfer options are required.", null);
        }

        if (optionsValue.Type != FtpTransferType.Ebcdic)
        {
            throw new FtpConfigurationException("EBCDIC conversion requires TYPE E.", null);
        }

        if (!sourceValue.CanRead)
        {
            throw new FtpConfigurationException("The EBCDIC conversion source stream must be readable.", null);
        }

        if (!destinationValue.CanWrite)
        {
            throw new FtpConfigurationException("The EBCDIC conversion destination stream must be writable.", null);
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
                EncodeInput(sourceValue, inputBuffer, output, optionsValue.TextFormat);
            }
            else
            {
                DecodeInput(sourceValue, inputBuffer, output, optionsValue.TextFormat);
            }

            output.FlushPendingBytes();
            bytesWritten = output.BytesWritten;
            return null;
        });

        FtpFaultIsolationResult destinationCleanup = destinationLifetime.DisposeResource();
        FtpFaultIsolationResult sourceCleanup = sourceLifetime.DisposeResource();

        if (!conversionResult.Succeeded)
        {
            throw new FtpTransferException("The EBCDIC stream conversion failed.", conversionResult.Failure);
        }

        if (!destinationCleanup.Succeeded)
        {
            throw new FtpTransferException("The EBCDIC conversion destination stream could not be closed.", destinationCleanup.Failure);
        }

        if (!sourceCleanup.Succeeded)
        {
            throw new FtpTransferException("The EBCDIC conversion source stream could not be closed.", sourceCleanup.Failure);
        }

        return bytesWritten;
    }

    internal static void EncodeInput(
        Stream source,
        byte[] inputBuffer,
        FtpTransferOutputBuffer output,
        FtpTextFormat format)
    {
        FtpEbcdicEncodingState state = new FtpEbcdicEncodingState(format);
        int bytesRead = source.Read(inputBuffer, 0, inputBuffer.Length);
        while (bytesRead != 0)
        {
            int index = 0;
            while (index < bytesRead)
            {
                ConsumeUtf8Byte(inputBuffer[index], state, output);
                index++;
            }

            bytesRead = source.Read(inputBuffer, 0, inputBuffer.Length);
        }

        CompleteEncoding(state, output);
    }

    internal static void DecodeInput(
        Stream source,
        byte[] inputBuffer,
        FtpTransferOutputBuffer output,
        FtpTextFormat format)
    {
        bool atRecordStart = true;
        int bytesRead = source.Read(inputBuffer, 0, inputBuffer.Length);
        while (bytesRead != 0)
        {
            int index = 0;
            while (index < bytesRead)
            {
                byte value = inputBuffer[index];
                int character = EbcdicCodePage037.DecodeByte(value);
                if (format == FtpTextFormat.CarriageControl && atRecordStart)
                {
                    ValidateCarriageControl(character);
                    atRecordStart = false;
                }

                if (format == FtpTextFormat.Telnet)
                {
                    WriteUtf8Character(character, output);
                }
                else if (value == 0x15)
                {
                    WriteLocalLineEnding(output);
                    atRecordStart = true;
                }
                else
                {
                    WriteUtf8Character(character, output);
                }

                index++;
            }

            bytesRead = source.Read(inputBuffer, 0, inputBuffer.Length);
        }
    }

    internal static void ConsumeUtf8Byte(
        byte value,
        FtpEbcdicEncodingState state,
        FtpTransferOutputBuffer output)
    {
        if (state.RemainingUtf8Bytes == 0)
        {
            if (value <= 0x7F)
            {
                EncodeTextCharacter(value, state, output);
                return;
            }

            if (value >= 0xC2 && value <= 0xDF)
            {
                state.CodePoint = value & 0x1F;
                state.RemainingUtf8Bytes = 1;
                state.MinimumCodePoint = 0x80;
                return;
            }

            if (value >= 0xE0 && value <= 0xEF)
            {
                state.CodePoint = value & 0x0F;
                state.RemainingUtf8Bytes = 2;
                state.MinimumCodePoint = 0x800;
                return;
            }

            if (value >= 0xF0 && value <= 0xF4)
            {
                state.CodePoint = value & 0x07;
                state.RemainingUtf8Bytes = 3;
                state.MinimumCodePoint = 0x10000;
                return;
            }

            throw new InvalidDataException("The local EBCDIC source contains invalid UTF-8.");
        }

        if (value < 0x80 || value > 0xBF)
        {
            throw new InvalidDataException("The local EBCDIC source contains invalid UTF-8.");
        }

        state.CodePoint = (state.CodePoint << 6) | (value & 0x3F);
        state.RemainingUtf8Bytes--;
        if (state.RemainingUtf8Bytes != 0)
        {
            return;
        }

        int character = state.CodePoint;
        if (character < state.MinimumCodePoint ||
            character > 0x10FFFF ||
            (character >= 0xD800 && character <= 0xDFFF))
        {
            throw new InvalidDataException("The local EBCDIC source contains invalid UTF-8.");
        }

        EncodeTextCharacter(character, state, output);
        state.CodePoint = 0;
        state.MinimumCodePoint = 0;
    }

    internal static void EncodeTextCharacter(
        int character,
        FtpEbcdicEncodingState state,
        FtpTransferOutputBuffer output)
    {
        if (state.Format == FtpTextFormat.Telnet)
        {
            output.WriteByte(EbcdicCodePage037.EncodeCharacter(character));
            return;
        }

        if (state.Format == FtpTextFormat.CarriageControl && state.AtRecordStart)
        {
            ValidateCarriageControl(character);
            state.AtRecordStart = false;
        }

        if (state.PendingCarriageReturn)
        {
            if (character == 10)
            {
                output.WriteByte(0x15);
                state.PendingCarriageReturn = false;
                state.AtRecordStart = true;
                return;
            }

            output.WriteByte(EbcdicCodePage037.EncodeCharacter(13));
            state.PendingCarriageReturn = false;
        }

        if (character == 13)
        {
            state.PendingCarriageReturn = true;
        }
        else if (character == 10 || character == 0x85)
        {
            output.WriteByte(0x15);
            state.AtRecordStart = true;
        }
        else
        {
            output.WriteByte(EbcdicCodePage037.EncodeCharacter(character));
        }
    }

    internal static void CompleteEncoding(FtpEbcdicEncodingState state, FtpTransferOutputBuffer output)
    {
        if (state.RemainingUtf8Bytes != 0)
        {
            throw new InvalidDataException("The local EBCDIC source ended within a UTF-8 character.");
        }

        if (state.PendingCarriageReturn)
        {
            output.WriteByte(EbcdicCodePage037.EncodeCharacter(13));
            state.PendingCarriageReturn = false;
        }
    }

    internal static void WriteUtf8Character(int character, FtpTransferOutputBuffer output)
    {
        if (character <= 0x7F)
        {
            output.WriteByte((byte)character);
            return;
        }

        output.WriteByte((byte)(0xC0 | (character >> 6)));
        output.WriteByte((byte)(0x80 | (character & 0x3F)));
    }

    internal static void WriteLocalLineEnding(FtpTransferOutputBuffer output)
    {
        if (System.Environment.NewLine.Length == 2)
        {
            output.WriteByte(13);
        }

        output.WriteByte(10);
    }

    internal static void ValidateCarriageControl(int character)
    {
        if (character != 32 && character != 48 && character != 49 && character != 43)
        {
            throw new InvalidDataException("An ASA record must start with space, 0, 1, or + carriage control.");
        }
    }
}
