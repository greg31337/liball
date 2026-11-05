using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace email;
/// <summary>
/// Generates rfc 2231 using explicit policy settings.
/// </summary>
public class EmailRfc2231Generator : IEmailRfc2231GeneratorContract
{
    internal static EmailRfc2231EncodeResult EncodeImplementation(string name, ReadOnlySpan<byte> value, EmailRfc2231GenerationOptions options)
    {
        ValidateArguments(name, options);
        List<byte[]> units = CreateByteUnits(value, options.GetCharset());
        return EncodeUnits(name, units, options);
    }

    /// <summary>
    /// Encodes the supplied value.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailRfc2231EncodeResult Encode(string name, ReadOnlySpan<byte> value, EmailRfc2231GenerationOptions options)
    {
        return EmailRfc2231Generator.EncodeImplementation(name, value, options);
    }

    internal static EmailRfc2231EncodeResult EncodeTextImplementation(string name, string value, Encoding characterEncoding, EmailRfc2231GenerationOptions options)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (characterEncoding == null)
        {
            throw new ArgumentNullException("characterEncoding");
        }

        ValidateArguments(name, options);
        List<byte[]> units = new List<byte[]>();
        int index = 0;
        while (index < value.Length)
        {
            int characterLength = 1;
            if (char.IsHighSurrogate(value[index]) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
            {
                characterLength = 2;
            }

            byte[] encoded = characterEncoding.GetBytes(value.Substring(index, characterLength));
            if (encoded.Length > 0)
            {
                units.Add(encoded);
            }

            index += characterLength;
        }

        return EncodeUnits(name, units, options);
    }

    /// <summary>
    /// Encodes the text.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "characterEncoding">The character encoding used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailRfc2231EncodeResult EncodeText(string name, string value, Encoding characterEncoding, EmailRfc2231GenerationOptions options)
    {
        return EmailRfc2231Generator.EncodeTextImplementation(name, value, characterEncoding, options);
    }

    static EmailRfc2231EncodeResult EncodeUnits(string name, List<byte[]> units, EmailRfc2231GenerationOptions options)
    {
        List<byte[]> encodedUnits = new List<byte[]>();
        List<byte> allEncoded = new List<byte>();
        int index = 0;
        while (index < units.Count)
        {
            byte[] encoded = new EmailPercentEncoding().Encode(units[index]);
            encodedUnits.Add(encoded);
            Append(allEncoded, encoded);
            index++;
        }

        byte[] metadata = CreateMetadata(options);
        int assignmentLimit = options.GetMaximumLineLength() - 2;
        byte[] singleValue = Combine(metadata, allEncoded.ToArray());
        int singleAssignmentLength = name.Length + 2 + singleValue.Length;
        List<EmailRfc2231Parameter> parameters;
        if (singleAssignmentLength <= assignmentLimit)
        {
            parameters = new List<EmailRfc2231Parameter>();
            parameters.Add(new EmailRfc2231ParameterOperations().CreateWithNameAndValueBytesAndQuoted(name + "*", singleValue, false));
        }
        else
        {
            parameters = CreateContinuations(name, encodedUnits, metadata, assignmentLimit, options.GetMaximumSegments());
        }

        return CreateHeaderFragment(parameters, options);
    }

    static void ValidateArguments(string name, EmailRfc2231GenerationOptions options)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (!IsAttribute(name))
        {
            throw new ArgumentException("RFC 2231 parameter names must use attribute characters only.", "name");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }
    }

    static List<EmailRfc2231Parameter> CreateContinuations(string name, List<byte[]> encodedUnits, byte[] metadata, int assignmentLimit, int maximumSegments)
    {
        List<EmailRfc2231Parameter> parameters = new List<EmailRfc2231Parameter>();
        int unitIndex = 0;
        int section = 0;
        while (unitIndex < encodedUnits.Count)
        {
            if (parameters.Count >= maximumSegments)
            {
                throw new EmailLimitException("RFC 2231 segments", maximumSegments, parameters.Count + 1);
            }

            string parameterName = CreateSectionName(name, section);
            int capacity = assignmentLimit - parameterName.Length - 1;
            List<byte> value = new List<byte>();
            if (section == 0)
            {
                Append(value, metadata);
                capacity -= metadata.Length;
                if (capacity < 0)
                {
                    throw new ArgumentException("The configured line length cannot contain the RFC 2231 charset and language metadata.", "options");
                }
            }

            int added = 0;
            while (unitIndex < encodedUnits.Count && encodedUnits[unitIndex].Length <= capacity)
            {
                Append(value, encodedUnits[unitIndex]);
                capacity -= encodedUnits[unitIndex].Length;
                unitIndex++;
                added++;
            }

            if (added == 0 && section > 0)
            {
                throw new ArgumentException("The configured line length cannot contain one complete encoded character and its parameter name.", "options");
            }

            parameters.Add(new EmailRfc2231ParameterOperations().CreateWithNameAndValueBytesAndQuoted(parameterName, value.ToArray(), false));
            if (section == int.MaxValue)
            {
                throw new EmailLimitException("RFC 2231 section number", int.MaxValue, (long)int.MaxValue + 1);
            }

            section++;
        }

        if (encodedUnits.Count == 0)
        {
            throw new ArgumentException("The configured line length cannot contain the RFC 2231 charset and language metadata.", "options");
        }

        return parameters;
    }

    static EmailRfc2231EncodeResult CreateHeaderFragment(List<EmailRfc2231Parameter> parameters, EmailRfc2231GenerationOptions options)
    {
        List<byte> output = new List<byte>();
        int column = options.GetFirstLineColumn();
        int folds = 0;
        int index = 0;
        while (index < parameters.Count)
        {
            EmailRfc2231Parameter parameter = parameters[index];
            byte[] assignment = CreateAssignment(parameter);
            if (column + 2 + assignment.Length <= options.GetMaximumLineLength())
            {
                output.Add(59);
                output.Add(32);
                column += 2;
            }
            else if (column < options.GetMaximumLineLength())
            {
                output.Add(59);
                AddFold(output);
                column = 1;
                folds++;
            }
            else
            {
                AddFold(output);
                output.Add(59);
                column = 2;
                folds++;
            }

            Append(output, assignment);
            column += assignment.Length;
            index++;
        }

        return new EmailRfc2231EncodeResult(output.ToArray(), parameters, folds, column);
    }

    static byte[] CreateAssignment(EmailRfc2231Parameter parameter)
    {
        List<byte> output = new List<byte>();
        AddAscii(parameter.GetName(), output);
        output.Add(61);
        Append(output, parameter.GetValueBytes());
        return output.ToArray();
    }

    static byte[] CreateMetadata(EmailRfc2231GenerationOptions options)
    {
        List<byte> output = new List<byte>();
        AddAscii(options.GetCharset(), output);
        output.Add(39);
        AddAscii(options.GetLanguage(), output);
        output.Add(39);
        return output.ToArray();
    }

    static string CreateSectionName(string name, int section)
    {
        return name + "*" + section.ToString(CultureInfo.InvariantCulture) + "*";
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

    static bool IsAttribute(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character > 127 || !IsAttributeCharacter((byte)character))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsAttributeCharacter(byte value)
    {
        if (value < 33 || value > 126 || value == 42 || value == 39 || value == 37)
        {
            return false;
        }

        return value != 40 && value != 41 && value != 60 && value != 62 && value != 64 && value != 44 && value != 59 && value != 58 && value != 92 && value != 34 && value != 47 && value != 91 && value != 93 && value != 63 && value != 61;
    }

    static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] combined = new byte[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }

    static void Append(List<byte> output, byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            output.Add(value[index]);
            index++;
        }
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
