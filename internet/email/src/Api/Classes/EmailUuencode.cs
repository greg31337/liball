using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides operations for working with uuencode.
/// </summary>
public class EmailUuencode : IEmailUuencodeContract
{
    /// <summary>
    /// The default maximum line bytes used when the caller does not provide an override.
    /// </summary>
    internal const int DefaultMaximumLineBytes = 1024;
    /// <summary>
    /// The maximum file name bytes value exposed by this API.
    /// </summary>
    internal const int MaximumFileNameBytes = 255;
    internal static EmailUuencodeDetectionResult DetectImplementation(ReadOnlySpan<byte> input)
    {
        int lineStart = 0;
        long lineNumber = 1;
        int index = 0;
        while (index <= input.Length)
        {
            if (index == input.Length || input[index] == 13 || input[index] == 10)
            {
                EmailSourcePosition position = new EmailSourcePosition(lineStart, lineNumber, 1);
                EmailUuencodeDetectionResult result = ParseHeaderImplementation(input.Slice(lineStart, index - lineStart), position);
                if (result.IsDetected())
                {
                    return result;
                }

                if (index == input.Length)
                {
                    break;
                }

                if (input[index] == 13 && index + 1 < input.Length && input[index + 1] == 10)
                {
                    index++;
                }

                index++;
                lineStart = index;
                lineNumber++;
                continue;
            }

            index++;
        }

        return new EmailUuencodeDetectionResultOperations().CreateNotDetected();
    }

    /// <summary>
    /// Detects the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>The uuencode detection result produced by the operation.</returns>
    public EmailUuencodeDetectionResult Detect(ReadOnlySpan<byte> input)
    {
        return EmailUuencode.DetectImplementation(input);
    }

    internal static EmailUuencodeDecodeResult DecodeImplementation(ReadOnlySpan<byte> input, long maximumDecodedBytes)
    {
        EmailUuencodeDecoder decoder = new EmailUuencodeDecoder(maximumDecodedBytes, DefaultMaximumLineBytes);
        byte[] body = decoder.Feed(input);
        byte[] tail = decoder.Complete();
        long decodedSize = new EmailSafeArithmetic().AddNonNegative(body.Length, tail.Length);
        byte[] combined = new byte[new EmailSafeArithmetic().ToInt32NonNegative(decodedSize)];
        if (body.Length > 0)
        {
            body.CopyTo(combined, 0);
        }

        if (tail.Length > 0)
        {
            tail.CopyTo(combined, body.Length);
        }

        return new EmailUuencodeDecodeResult(combined, decoder.GetDetection(), decoder.GetIssues());
    }

    /// <summary>
    /// Decodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "maximumDecodedBytes">The source bytes or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailUuencodeDecodeResult Decode(ReadOnlySpan<byte> input, long maximumDecodedBytes)
    {
        return EmailUuencode.DecodeImplementation(input, maximumDecodedBytes);
    }

    internal static byte[] EncodeImplementation(ReadOnlySpan<byte> input, int fileMode, string fileName)
    {
        ValidateMetadataImplementation(fileMode, fileName);
        List<byte> output = new List<byte>();
        AddHeader(fileMode, fileName, output);
        int offset = 0;
        while (offset < input.Length)
        {
            int count = input.Length - offset;
            if (count > 45)
            {
                count = 45;
            }

            output.Add(EncodeCharacter(count));
            int lineOffset = 0;
            while (lineOffset < count)
            {
                byte first = input[offset + lineOffset];
                byte second = 0;
                byte third = 0;
                if (lineOffset + 1 < count)
                {
                    second = input[offset + lineOffset + 1];
                }

                if (lineOffset + 2 < count)
                {
                    third = input[offset + lineOffset + 2];
                }

                output.Add(EncodeCharacter(first >> 2));
                output.Add(EncodeCharacter((first & 3) << 4 | second >> 4));
                output.Add(EncodeCharacter((second & 15) << 2 | third >> 6));
                output.Add(EncodeCharacter(third & 63));
                lineOffset += 3;
            }

            AddCrlf(output);
            offset += count;
        }

        output.Add(96);
        AddCrlf(output);
        output.Add(101);
        output.Add(110);
        output.Add(100);
        AddCrlf(output);
        return output.ToArray();
    }

    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "fileMode">The file mode used by the operation.</param>
    /// <param name = "fileName">The file name used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public byte[] Encode(ReadOnlySpan<byte> input, int fileMode, string fileName)
    {
        return EmailUuencode.EncodeImplementation(input, fileMode, fileName);
    }

    internal static EmailUuencodeDetectionResult ParseHeaderImplementation(ReadOnlySpan<byte> line, EmailSourcePosition position)
    {
        if (position == null)
        {
            throw new ArgumentNullException("position");
        }

        if (line.Length < 11 || line.Length > MaximumFileNameBytes + 10)
        {
            return new EmailUuencodeDetectionResultOperations().CreateNotDetected();
        }

        if (line[0] != 98 || line[1] != 101 || line[2] != 103 || line[3] != 105 || line[4] != 110 || line[5] != 32)
        {
            return new EmailUuencodeDetectionResultOperations().CreateNotDetected();
        }

        if (!IsOctal(line[6]) || !IsOctal(line[7]) || !IsOctal(line[8]) || line[9] != 32)
        {
            return new EmailUuencodeDetectionResultOperations().CreateNotDetected();
        }

        bool hasNonSpace = false;
        int index = 10;
        while (index < line.Length)
        {
            if (line[index] < 32 || line[index] > 126)
            {
                return new EmailUuencodeDetectionResultOperations().CreateNotDetected();
            }

            if (line[index] != 32)
            {
                hasNonSpace = true;
            }

            index++;
        }

        if (!hasNonSpace)
        {
            return new EmailUuencodeDetectionResultOperations().CreateNotDetected();
        }

        int fileMode = (line[6] - 48) * 64 + (line[7] - 48) * 8 + line[8] - 48;
        char[] fileName = new char[line.Length - 10];
        index = 0;
        while (index < fileName.Length)
        {
            fileName[index] = (char)line[index + 10];
            index++;
        }

        return new EmailUuencodeDetectionResultOperations().CreateDetected(fileMode, new string (fileName), position);
    }

    /// <summary>
    /// Parses the supplied header data.
    /// </summary>
    /// <param name = "line">The line used by the operation.</param>
    /// <param name = "position">The position used by the operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailUuencodeDetectionResult ParseHeader(ReadOnlySpan<byte> line, EmailSourcePosition position)
    {
        return EmailUuencode.ParseHeaderImplementation(line, position);
    }

    internal static void ValidateMetadataImplementation(int fileMode, string fileName)
    {
        if (fileMode < 0 || fileMode > 511)
        {
            throw new ArgumentOutOfRangeException("fileMode");
        }

        if (fileName == null)
        {
            throw new ArgumentNullException("fileName");
        }

        if (fileName.Length == 0 || fileName.Length > MaximumFileNameBytes)
        {
            throw new ArgumentOutOfRangeException("fileName");
        }

        bool hasNonSpace = false;
        int index = 0;
        while (index < fileName.Length)
        {
            char value = fileName[index];
            if (value < 32 || value > 126)
            {
                throw new ArgumentException("The uuencode file name must contain printable ASCII only.", "fileName");
            }

            if (value != 32)
            {
                hasNonSpace = true;
            }

            index++;
        }

        if (!hasNonSpace)
        {
            throw new ArgumentException("The uuencode file name must contain a non-space character.", "fileName");
        }
    }

    /// <summary>
    /// Validates the supplied metadata against the selected policy.
    /// </summary>
    /// <param name = "fileMode">The file mode used by the operation.</param>
    /// <param name = "fileName">The file name used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void ValidateMetadata(int fileMode, string fileName)
    {
        EmailUuencode.ValidateMetadataImplementation(fileMode, fileName);
    }

    static void AddHeader(int fileMode, string fileName, List<byte> output)
    {
        output.Add(98);
        output.Add(101);
        output.Add(103);
        output.Add(105);
        output.Add(110);
        output.Add(32);
        output.Add((byte)(48 + (fileMode >> 6 & 7)));
        output.Add((byte)(48 + (fileMode >> 3 & 7)));
        output.Add((byte)(48 + (fileMode & 7)));
        output.Add(32);
        int index = 0;
        while (index < fileName.Length)
        {
            output.Add((byte)fileName[index]);
            index++;
        }

        AddCrlf(output);
    }

    static void AddCrlf(List<byte> output)
    {
        output.Add(13);
        output.Add(10);
    }

    static byte EncodeCharacter(int value)
    {
        int encoded = value & 63;
        if (encoded == 0)
        {
            return 96;
        }

        return (byte)(encoded + 32);
    }

    static bool IsOctal(byte value)
    {
        return value >= 48 && value <= 55;
    }
}
