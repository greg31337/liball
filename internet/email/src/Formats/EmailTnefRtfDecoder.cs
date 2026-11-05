using System;
using System.Text;

namespace email;

internal class EmailTnefRtfDecodeResult
{
    byte[] content;
    bool complete;

    internal EmailTnefRtfDecodeResult(byte[] content, bool complete)
    {
        this.content = content;
        this.complete = complete;
    }

    internal byte[] GetContent() { return content; }
    internal bool IsComplete() { return complete; }
}
internal class EmailTnefRtfDecoder
{
    const uint CompressedMagic = 0x75465A4C;
    const uint UncompressedMagic = 0x414C454D;

    internal static EmailTnefRtfDecodeResult Decode(byte[] input, long sourceOffset, EmailTnefReaderOptions options, EmailIssueList issues)
    {
        if (input.Length < 16)
        {
            AddIssue(issues, "The compressed-RTF value is shorter than its 16-byte header.", sourceOffset);
            return new EmailTnefRtfDecodeResult(new byte[0], false);
        }

        long compressedSize = ReadUInt32(input, 0);
        long rawSize = ReadUInt32(input, 4);
        uint magic = ReadUInt32(input, 8);
        uint storedCrc = ReadUInt32(input, 12);
        if (rawSize > options.GetMaximumDecodedRtfBytes()) throw new EmailLimitException("MaxTnefDecodedRtfBytes", options.GetMaximumDecodedRtfBytes(), rawSize);
        if (rawSize > int.MaxValue) throw new EmailLimitException("MaxTnefDecodedRtfBytes", int.MaxValue, rawSize);
        if (compressedSize < 12 || compressedSize - 12 > input.Length - 16)
        {
            AddIssue(issues, "The compressed-RTF header declares content beyond the available property value.", sourceOffset);
            return new EmailTnefRtfDecodeResult(new byte[0], false);
        }

        int contentLength = (int)compressedSize - 12;
        if (compressedSize + 4 != input.Length) AddIssue(issues, "The compressed-RTF size does not account for every property byte.", sourceOffset);
        if (magic == UncompressedMagic)
        {
            if (storedCrc != 0) AddIssue(issues, "An uncompressed RTF value has a nonzero CRC field.", sourceOffset + 12);
            if (rawSize > contentLength)
            {
                AddIssue(issues, "The uncompressed RTF payload is shorter than its declared raw size.", sourceOffset + 16);
                return new EmailTnefRtfDecodeResult(Copy(input, 16, contentLength), false);
            }

            return new EmailTnefRtfDecodeResult(Copy(input, 16, (int)rawSize), storedCrc == 0 && compressedSize + 4 == input.Length);
        }

        if (magic != CompressedMagic)
        {
            AddIssue(issues, "The RTF compression type is neither LZFu nor uncompressed MELA.", sourceOffset + 8);
            return new EmailTnefRtfDecodeResult(new byte[0], false);
        }

        uint calculatedCrc = CalculateCrc(input, 16, contentLength);
        bool validCrc = calculatedCrc == storedCrc;
        if (!validCrc) AddIssue(issues, "The compressed-RTF CRC does not match its compressed contents.", sourceOffset + 12);
        byte[] dictionary = new byte[4096];
        byte[] initial = Encoding.ASCII.GetBytes("{\\rtf1\\ansi\\mac\\deff0\\deftab720{\\fonttbl;}{\\f0\\fnil \\froman \\fswiss \\fmodern \\fscript \\fdecor MS Sans SerifSymbolArialTimes New RomanCourier{\\colortbl\\red0\\green0\\blue0\r\n\\par \\pard\\plain\\f0\\fs20\\b\\i\\u\\tab\\tx");
        initial.CopyTo(dictionary, 0);
        int writeOffset = 207;
        byte[] output = new byte[(int)rawSize];
        int outputOffset = 0;
        int inputOffset = 16;
        int inputEnd = 16 + contentLength;
        bool terminated = false;
        bool malformed = false;
        while (inputOffset < inputEnd && !terminated && !malformed)
        {
            int control = input[inputOffset];
            inputOffset++;
            int bit = 1;
            int token = 0;
            while (token < 8 && !terminated && !malformed)
            {
                if ((control & bit) == 0)
                {
                    if (inputOffset >= inputEnd || outputOffset >= output.Length) malformed = true;
                    else
                    {
                        byte literal = input[inputOffset];
                        inputOffset++;
                        output[outputOffset] = literal;
                        outputOffset++;
                        dictionary[writeOffset] = literal;
                        writeOffset = (writeOffset + 1) & 4095;
                    }
                }
                else
                {
                    if (inputEnd - inputOffset < 2) malformed = true;
                    else
                    {
                        int first = input[inputOffset];
                        int second = input[inputOffset + 1];
                        inputOffset += 2;
                        int readOffset = first << 4 | second >> 4;
                        int length = (second & 15) + 2;
                        if (readOffset == writeOffset) terminated = true;
                        else
                        {
                            int copyIndex = 0;
                            while (copyIndex < length && !malformed)
                            {
                                if (outputOffset >= output.Length) malformed = true;
                                else
                                {
                                    byte value = dictionary[readOffset];
                                    readOffset = (readOffset + 1) & 4095;
                                    output[outputOffset] = value;
                                    outputOffset++;
                                    dictionary[writeOffset] = value;
                                    writeOffset = (writeOffset + 1) & 4095;
                                }

                                copyIndex++;
                            }
                        }
                    }
                }

                bit <<= 1;
                token++;
            }
        }

        if (!terminated)
        {
            AddIssue(issues, malformed ? "The compressed-RTF token stream is malformed or exceeds its declared raw size." : "The compressed-RTF token stream has no end reference.", sourceOffset + inputOffset);
        }

        if (outputOffset != output.Length)
        {
            AddIssue(issues, "The decompressed RTF byte count does not match the declared raw size.", sourceOffset + 4);
            return new EmailTnefRtfDecodeResult(Copy(output, 0, outputOffset), false);
        }

        return new EmailTnefRtfDecodeResult(output, terminated && !malformed && validCrc && compressedSize + 4 == input.Length);
    }

    static uint CalculateCrc(byte[] value, int offset, int count)
    {
        uint crc = 0;
        int index = 0;
        while (index < count)
        {
            crc ^= value[offset + index];
            int bit = 0;
            while (bit < 8)
            {
                crc = (crc & 1) == 0 ? crc >> 1 : crc >> 1 ^ 0xEDB88320;
                bit++;
            }

            index++;
        }

        return crc;
    }

    static uint ReadUInt32(byte[] value, int offset)
    {
        return (uint)value[offset] | (uint)value[offset + 1] << 8 | (uint)value[offset + 2] << 16 | (uint)value[offset + 3] << 24;
    }

    static byte[] Copy(byte[] value, int offset, int count)
    {
        byte[] result = new byte[count];
        if (count > 0) Array.Copy(value, offset, result, 0, count);
        return result;
    }

    static void AddIssue(EmailIssueList issues, string description, long offset)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.TnefRtfMalformed, description, new EmailSourcePosition(offset, 1, offset + 1), null, null, "The original compressed-RTF property value was retained."));
    }
}
