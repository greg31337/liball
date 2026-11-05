using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Generates encoded word using explicit policy settings.
/// </summary>
public class EmailEncodedWordGenerator : IEmailEncodedWordGeneratorContract
{
    internal static EmailEncodedWordEncodeResult EncodeImplementation(ReadOnlySpan<byte> input, EmailEncodedWordGenerationOptions options)
    {
        ValidateOptions(options);
        List<byte[]> units = CreateByteUnits(input, options.GetCharset());
        return EncodeUnits(units, options);
    }

    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailEncodedWordEncodeResult Encode(ReadOnlySpan<byte> input, EmailEncodedWordGenerationOptions options)
    {
        return EmailEncodedWordGenerator.EncodeImplementation(input, options);
    }

    internal static EmailEncodedWordEncodeResult EncodeTextImplementation(string input, Encoding characterEncoding, EmailEncodedWordGenerationOptions options)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }

        if (characterEncoding == null)
        {
            throw new ArgumentNullException("characterEncoding");
        }

        ValidateOptions(options);
        List<byte[]> units = new List<byte[]>();
        int index = 0;
        while (index < input.Length)
        {
            int characterLength = 1;
            if (char.IsHighSurrogate(input[index]) && index + 1 < input.Length && char.IsLowSurrogate(input[index + 1]))
            {
                characterLength = 2;
            }

            byte[] encoded = characterEncoding.GetBytes(input.Substring(index, characterLength));
            if (encoded.Length > 0)
            {
                units.Add(encoded);
            }

            index += characterLength;
        }

        return EncodeUnits(units, options);
    }

    /// <summary>
    /// Encodes the text.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "characterEncoding">The character encoding used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailEncodedWordEncodeResult EncodeText(string input, Encoding characterEncoding, EmailEncodedWordGenerationOptions options)
    {
        return EmailEncodedWordGenerator.EncodeTextImplementation(input, characterEncoding, options);
    }

    static EmailEncodedWordEncodeResult EncodeUnits(List<byte[]> units, EmailEncodedWordGenerationOptions options)
    {
        EmailEncodedWordEncoding encoding = SelectEncoding(units, options);
        if (units.Count == 0)
        {
            return new EmailEncodedWordEncodeResult(new byte[0], encoding, 0, 0, options.GetFirstLineColumn());
        }

        int wordLimit = options.GetMaximumLineLength() - 1;
        if (wordLimit > EmailEncodedWordConstants.MaximumWordBytes)
        {
            wordLimit = EmailEncodedWordConstants.MaximumWordBytes;
        }

        int prefixLength = GetPrefixLength(options);
        int payloadCapacity = wordLimit - prefixLength - 2;
        if (payloadCapacity < 1 || encoding == EmailEncodedWordEncoding.Base64 && payloadCapacity < 4)
        {
            throw new ArgumentException("The configured line length and charset leave no room for encoded text.", "options");
        }

        List<byte[]> segments = PackSegments(units, encoding, options.GetContext(), payloadCapacity, options.GetMaximumEncodedWords());
        List<byte[]> words = new List<byte[]>();
        int index = 0;
        while (index < segments.Count)
        {
            words.Add(CreateWord(segments[index], encoding, options));
            index++;
        }

        return FoldWords(words, encoding, options);
    }

    static void ValidateOptions(EmailEncodedWordGenerationOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (options.GetContext() == EmailEncodedWordContext.Forbidden)
        {
            throw new ArgumentException("Encoded words cannot be generated in the selected header context.", "options");
        }
    }

    static List<byte[]> CreateByteUnits(ReadOnlySpan<byte> input, string charset)
    {
        List<byte[]> units = new List<byte[]>();
        bool utf8 = string.Equals(charset, "utf-8", StringComparison.OrdinalIgnoreCase) || string.Equals(charset, "utf8", StringComparison.OrdinalIgnoreCase);
        int index = 0;
        while (index < input.Length)
        {
            int length = 1;
            if (utf8)
            {
                length = GetUtf8UnitLength(input, index);
            }

            units.Add(input.Slice(index, length).ToArray());
            index += length;
        }

        return units;
    }

    static int GetUtf8UnitLength(ReadOnlySpan<byte> input, int index)
    {
        byte first = input[index];
        if (first < 128)
        {
            return 1;
        }

        if (first >= 194 && first <= 223 && HasContinuation(input, index + 1))
        {
            return 2;
        }

        if (first == 224 && index + 2 < input.Length && input[index + 1] >= 160 && input[index + 1] <= 191 && IsContinuation(input[index + 2]))
        {
            return 3;
        }

        if (first >= 225 && first <= 236 && HasTwoContinuations(input, index))
        {
            return 3;
        }

        if (first == 237 && index + 2 < input.Length && input[index + 1] >= 128 && input[index + 1] <= 159 && IsContinuation(input[index + 2]))
        {
            return 3;
        }

        if (first >= 238 && first <= 239 && HasTwoContinuations(input, index))
        {
            return 3;
        }

        if (first == 240 && index + 3 < input.Length && input[index + 1] >= 144 && input[index + 1] <= 191 && IsContinuation(input[index + 2]) && IsContinuation(input[index + 3]))
        {
            return 4;
        }

        if (first >= 241 && first <= 243 && HasThreeContinuations(input, index))
        {
            return 4;
        }

        if (first == 244 && index + 3 < input.Length && input[index + 1] >= 128 && input[index + 1] <= 143 && IsContinuation(input[index + 2]) && IsContinuation(input[index + 3]))
        {
            return 4;
        }

        return 1;
    }

    static bool HasContinuation(ReadOnlySpan<byte> input, int index)
    {
        return index < input.Length && IsContinuation(input[index]);
    }

    static bool HasTwoContinuations(ReadOnlySpan<byte> input, int index)
    {
        return index + 2 < input.Length && IsContinuation(input[index + 1]) && IsContinuation(input[index + 2]);
    }

    static bool HasThreeContinuations(ReadOnlySpan<byte> input, int index)
    {
        return index + 3 < input.Length && IsContinuation(input[index + 1]) && IsContinuation(input[index + 2]) && IsContinuation(input[index + 3]);
    }

    static bool IsContinuation(byte value)
    {
        return value >= 128 && value <= 191;
    }

    static EmailEncodedWordEncoding SelectEncoding(List<byte[]> units, EmailEncodedWordGenerationOptions options)
    {
        if (options.GetEncodingChoice() == EmailEncodedWordEncodingChoice.Base64)
        {
            return EmailEncodedWordEncoding.Base64;
        }

        if (options.GetEncodingChoice() == EmailEncodedWordEncodingChoice.QuotedPrintable)
        {
            return EmailEncodedWordEncoding.QuotedPrintable;
        }

        long byteCount = 0;
        long quotedPrintableSize = 0;
        int index = 0;
        while (index < units.Count)
        {
            byte[] unit = units[index];
            byteCount = new EmailSafeArithmetic().AddNonNegative(byteCount, unit.Length);
            int byteIndex = 0;
            while (byteIndex < unit.Length)
            {
                quotedPrintableSize = new EmailSafeArithmetic().AddNonNegative(quotedPrintableSize, GetQByteSize(unit[byteIndex], options.GetContext()));
                byteIndex++;
            }

            index++;
        }

        long quantumCount = byteCount / 3;
        if (byteCount % 3 != 0)
        {
            quantumCount++;
        }

        long base64Size = new EmailSafeArithmetic().MultiplyNonNegative(quantumCount, 4);
        if (quotedPrintableSize <= base64Size)
        {
            return EmailEncodedWordEncoding.QuotedPrintable;
        }

        return EmailEncodedWordEncoding.Base64;
    }

    static List<byte[]> PackSegments(List<byte[]> units, EmailEncodedWordEncoding encoding, EmailEncodedWordContext context, int payloadCapacity, int maximumEncodedWords)
    {
        List<byte[]> segments = new List<byte[]>();
        List<byte> current = new List<byte>();
        int currentQSize = 0;
        int index = 0;
        while (index < units.Count)
        {
            byte[] unit = units[index];
            int unitQSize = GetQSize(unit, context);
            bool fits;
            if (encoding == EmailEncodedWordEncoding.Base64)
            {
                fits = GetBase64Size(current.Count + unit.Length) <= payloadCapacity;
            }
            else
            {
                fits = currentQSize + unitQSize <= payloadCapacity;
            }

            if (!fits && current.Count > 0)
            {
                AddSegment(segments, current, maximumEncodedWords);
                current.Clear();
                currentQSize = 0;
                continue;
            }

            if (!fits)
            {
                throw new ArgumentException("The configured line length cannot contain one complete input character.", "options");
            }

            int byteIndex = 0;
            while (byteIndex < unit.Length)
            {
                current.Add(unit[byteIndex]);
                byteIndex++;
            }

            currentQSize += unitQSize;
            index++;
        }

        if (current.Count > 0)
        {
            AddSegment(segments, current, maximumEncodedWords);
        }

        return segments;
    }

    static void AddSegment(List<byte[]> segments, List<byte> current, int maximumEncodedWords)
    {
        if (segments.Count >= maximumEncodedWords)
        {
            throw new EmailLimitException("encoded words", maximumEncodedWords, segments.Count + 1);
        }

        segments.Add(current.ToArray());
    }

    static byte[] CreateWord(byte[] segment, EmailEncodedWordEncoding encoding, EmailEncodedWordGenerationOptions options)
    {
        List<byte> output = new List<byte>();
        output.Add(61);
        output.Add(63);
        AddAscii(options.GetCharset(), output);
        if (options.HasLanguage())
        {
            output.Add(42);
            AddAscii(options.GetLanguage(), output);
        }

        output.Add(63);
        if (encoding == EmailEncodedWordEncoding.Base64)
        {
            output.Add(66);
        }
        else
        {
            output.Add(81);
        }

        output.Add(63);
        if (encoding == EmailEncodedWordEncoding.Base64)
        {
            byte[] encoded = new EmailBase64().EncodeWithInput(segment);
            int index = 0;
            while (index < encoded.Length)
            {
                output.Add(encoded[index]);
                index++;
            }
        }
        else
        {
            AddQEncoded(segment, options.GetContext(), output);
        }

        output.Add(63);
        output.Add(61);
        return output.ToArray();
    }

    static EmailEncodedWordEncodeResult FoldWords(List<byte[]> words, EmailEncodedWordEncoding encoding, EmailEncodedWordGenerationOptions options)
    {
        List<byte> output = new List<byte>();
        int column = options.GetFirstLineColumn();
        int folds = 0;
        int index = 0;
        while (index < words.Count)
        {
            byte[] word = words[index];
            if (index == 0)
            {
                if (column + word.Length > options.GetMaximumLineLength())
                {
                    AddFold(output);
                    column = 1;
                    folds++;
                }
            }
            else if (column + 1 + word.Length <= options.GetMaximumLineLength())
            {
                output.Add(32);
                column++;
            }
            else
            {
                AddFold(output);
                column = 1;
                folds++;
            }

            int byteIndex = 0;
            while (byteIndex < word.Length)
            {
                output.Add(word[byteIndex]);
                byteIndex++;
            }

            column += word.Length;
            index++;
        }

        return new EmailEncodedWordEncodeResult(output.ToArray(), encoding, words.Count, folds, column);
    }

    static int GetPrefixLength(EmailEncodedWordGenerationOptions options)
    {
        int length = options.GetCharset().Length + 5;
        if (options.HasLanguage())
        {
            length += (options.GetLanguage() ?? throw new InvalidOperationException()).Length + 1;
        }

        return length;
    }

    static int GetBase64Size(int byteCount)
    {
        int quantumCount = byteCount / 3;
        if (byteCount % 3 != 0)
        {
            quantumCount++;
        }

        return quantumCount * 4;
    }

    static int GetQSize(byte[] input, EmailEncodedWordContext context)
    {
        int size = 0;
        int index = 0;
        while (index < input.Length)
        {
            size += GetQByteSize(input[index], context);
            index++;
        }

        return size;
    }

    static int GetQByteSize(byte value, EmailEncodedWordContext context)
    {
        if (value == 32 || IsQLiteral(value, context))
        {
            return 1;
        }

        return 3;
    }

    static void AddQEncoded(byte[] input, EmailEncodedWordContext context, List<byte> output)
    {
        int index = 0;
        while (index < input.Length)
        {
            byte value = input[index];
            if (value == 32)
            {
                output.Add(95);
            }
            else if (IsQLiteral(value, context))
            {
                output.Add(value);
            }
            else
            {
                output.Add(61);
                output.Add(new EmailByteScanner().ToUpperHex(value >> 4));
                output.Add(new EmailByteScanner().ToUpperHex(value & 15));
            }

            index++;
        }
    }

    static bool IsQLiteral(byte value, EmailEncodedWordContext context)
    {
        if (context == EmailEncodedWordContext.Phrase)
        {
            if (value >= 65 && value <= 90 || value >= 97 && value <= 122 || value >= 48 && value <= 57)
            {
                return true;
            }

            return value == 33 || value == 42 || value == 43 || value == 45 || value == 47;
        }

        if (value < 33 || value > 126 || value == 61 || value == 63 || value == 95)
        {
            return false;
        }

        if (context == EmailEncodedWordContext.Comment && (value == 40 || value == 41 || value == 92))
        {
            return false;
        }

        return true;
    }

    static void AddAscii(string? value, List<byte> output)
    {
        int index = 0;
        while (index < (value ?? throw new InvalidOperationException()).Length)
        {
            output.Add((byte)value[index]);
            index++;
        }
    }

    static void AddFold(List<byte> output)
    {
        output.Add(13);
        output.Add(10);
        output.Add(32);
    }
}
