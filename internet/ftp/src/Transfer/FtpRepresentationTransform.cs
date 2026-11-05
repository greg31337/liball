using System.IO;

namespace ftp;

internal class FtpRepresentationTransform
{
    internal FtpTransferOptions Options;
    internal bool Encoding;
    internal MemoryStream Buffer;
    internal FtpTransferOutputBuffer Output;
    internal FtpEbcdicEncodingState? EbcdicEncoding;
    internal FtpLocalByteEncodingState? LocalEncoding;
    internal FtpLocalByteDecodingState? LocalDecoding;
    internal bool PendingCarriageReturn;
    internal bool AtRecordStart;
    internal bool Completed;

    internal FtpRepresentationTransform(
        FtpTransferOptions options,
        bool encoding)
    {
        FtpTransferValidator.ValidateTransferOptions(options);
        Options = options;
        Encoding = encoding;
        Buffer = new MemoryStream();
        Output = new FtpTransferOutputBuffer(Buffer, options.BufferBytes);
        EbcdicEncoding = null;
        LocalEncoding = null;
        LocalDecoding = null;
        PendingCarriageReturn = false;
        AtRecordStart = true;
        Completed = false;
        if (encoding && options.Type == FtpTransferType.Ebcdic)
        {
            EbcdicEncoding = new FtpEbcdicEncodingState(
                options.TextFormat);
        }
        if (encoding && options.Type == FtpTransferType.LocalByte &&
            options.LocalByteSize != 8)
        {
            LocalEncoding = new FtpLocalByteEncodingState(
                options.LocalByteSize, Output);
        }
        if (!encoding && options.Type == FtpTransferType.LocalByte &&
            options.LocalByteSize != 8)
        {
            LocalDecoding = new FtpLocalByteDecodingState(
                options.LocalByteSize, false, 0, Output);
        }
    }

    internal byte[] ConvertChunk(
        byte[] data,
        int count,
        bool finalChunk)
    {
        if (Completed)
        {
            throw new FtpTransferException(
                "Representation data cannot follow transfer completion.", null);
        }
        if (count < 0 || count > data.Length)
        {
            throw new FtpTransferException(
                "The representation input bounds are invalid.", null);
        }

        if (Options.Type == FtpTransferType.Ascii)
        {
            ConvertAscii(data, count, finalChunk);
        }
        else if (Options.Type == FtpTransferType.Ebcdic)
        {
            ConvertEbcdic(data, count, finalChunk);
        }
        else if (Options.Type == FtpTransferType.LocalByte &&
            Options.LocalByteSize != 8)
        {
            ConvertLocalBytes(data, count, finalChunk);
        }
        else
        {
            WriteBytes(data, count);
        }

        if (finalChunk)
        {
            Completed = true;
        }
        Output.FlushPendingBytes();
        byte[] converted = Buffer.ToArray();
        Buffer = new MemoryStream();
        Output.Destination = Buffer;
        return converted;
    }

    internal void ConvertAscii(
        byte[] data,
        int count,
        bool finalChunk)
    {
        int index = 0;
        while (index < count)
        {
            byte value = data[index];
            AsciiTransferCodec.ValidateAsciiByte(value);
            if (Options.TextFormat == FtpTextFormat.CarriageControl &&
                AtRecordStart)
            {
                AsciiTransferCodec.ValidateCarriageControl(value);
                AtRecordStart = false;
            }

            if (Encoding)
            {
                EncodeAsciiByte(value);
            }
            else
            {
                DecodeAsciiByte(value);
            }
            index++;
        }

        if (finalChunk && PendingCarriageReturn)
        {
            if (Encoding)
            {
                Output.WriteByte(13);
                Output.WriteByte(0);
                PendingCarriageReturn = false;
            }
            else
            {
                throw new InvalidDataException(
                    "A network-ASCII stream ended after a carriage return.");
            }
        }
    }

    internal void EncodeAsciiByte(byte value)
    {
        if (PendingCarriageReturn)
        {
            Output.WriteByte(13);
            if (value == 10)
            {
                Output.WriteByte(10);
                PendingCarriageReturn = false;
                AtRecordStart = true;
                return;
            }
            Output.WriteByte(0);
            PendingCarriageReturn = false;
        }

        if (value == 13)
        {
            PendingCarriageReturn = true;
        }
        else if (value == 10)
        {
            Output.WriteByte(13);
            Output.WriteByte(10);
            AtRecordStart = true;
        }
        else
        {
            Output.WriteByte(value);
        }
    }

    internal void DecodeAsciiByte(byte value)
    {
        if (PendingCarriageReturn)
        {
            if (value == 10)
            {
                AsciiTransferCodec.WriteLocalLineEnding(Output);
                AtRecordStart = true;
            }
            else if (value == 0)
            {
                Output.WriteByte(13);
            }
            else
            {
                throw new InvalidDataException(
                    "A network-ASCII carriage return must be followed by LF or NUL.");
            }
            PendingCarriageReturn = false;
        }
        else if (value == 13)
        {
            PendingCarriageReturn = true;
        }
        else
        {
            Output.WriteByte(value);
        }
    }

    internal void ConvertEbcdic(
        byte[] data,
        int count,
        bool finalChunk)
    {
        int index = 0;
        if (Encoding)
        {
            FtpEbcdicEncodingState? state = EbcdicEncoding;
            if (state == null)
            {
                throw new FtpConfigurationException(
                    "An EBCDIC encoding state is required.", null);
            }
            while (index < count)
            {
                EbcdicTransferCodec.ConsumeUtf8Byte(
                    data[index], state, Output);
                index++;
            }
            if (finalChunk)
            {
                EbcdicTransferCodec.CompleteEncoding(state, Output);
            }
            return;
        }

        while (index < count)
        {
            byte value = data[index];
            int character = EbcdicCodePage037.DecodeByte(value);
            if (Options.TextFormat == FtpTextFormat.CarriageControl &&
                AtRecordStart)
            {
                EbcdicTransferCodec.ValidateCarriageControl(character);
                AtRecordStart = false;
            }
            if (Options.TextFormat == FtpTextFormat.Telnet)
            {
                EbcdicTransferCodec.WriteUtf8Character(character, Output);
            }
            else if (value == 0x15)
            {
                EbcdicTransferCodec.WriteLocalLineEnding(Output);
                AtRecordStart = true;
            }
            else
            {
                EbcdicTransferCodec.WriteUtf8Character(character, Output);
            }
            index++;
        }
    }

    internal void ConvertLocalBytes(
        byte[] data,
        int count,
        bool finalChunk)
    {
        int index = 0;
        if (Encoding)
        {
            FtpLocalByteEncodingState? state = LocalEncoding;
            if (state == null)
            {
                throw new FtpConfigurationException(
                    "A local-byte encoding state is required.", null);
            }
            while (index < count)
            {
                state.ConsumeLocalByte(data[index]);
                index++;
            }
            if (finalChunk)
            {
                state.CompleteEncoding();
            }
            return;
        }

        FtpLocalByteDecodingState? decoding = LocalDecoding;
        if (decoding == null)
        {
            throw new FtpConfigurationException(
                "A local-byte decoding state is required.", null);
        }
        while (index < count)
        {
            decoding.ConsumeWireByte(data[index]);
            index++;
        }
        if (finalChunk)
        {
            decoding.CompleteDecoding();
        }
    }

    internal void WriteBytes(byte[] data, int count)
    {
        int index = 0;
        while (index < count)
        {
            Output.WriteByte(data[index]);
            index++;
        }
    }
}
