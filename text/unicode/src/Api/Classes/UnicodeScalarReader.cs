using System.Collections.Generic;
using System.Text;

namespace unicode;

/// <summary>
/// Decodes .NET UTF-16 strings into Unicode scalar records and reports malformed surrogate sequences.
/// </summary>
public class UnicodeScalarReader : IUnicodeScalarReader
{
    internal static readonly UnicodeScalarReader Instance = new UnicodeScalarReader();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodeScalarReader()
    {
    }
    /// <summary>
    /// Decodes UTF-16 into scalar records, replacing each malformed surrogate sequence with U+FFFD.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodeScalarRecord[] ReadAll(string text)
    {
        return ReadAllWithPolicy(text, UnicodeMalformedInputPolicy.Replace);
    }

    /// <summary>
    /// Decodes UTF-16 under an explicit malformed-input policy.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="policy">How malformed surrogate subsequences are handled.</param>
    /// <returns>A new array containing the resulting scalar records.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>policy</c> is unsupported.</exception>
    /// <exception cref="System.Text.EncoderFallbackException">Thrown for malformed UTF-16 when <c>policy</c> is strict.</exception>
    public UnicodeScalarRecord[] ReadAllWithPolicy(string text, UnicodeMalformedInputPolicy policy)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return ReadAllSpan(text, policy);
    }

    /// <summary>
    /// Decodes a UTF-16 span under an explicit malformed-input policy.
    /// </summary>
    /// <param name="text">The UTF-16 code units to process.</param>
    /// <param name="policy">How malformed surrogate subsequences are handled.</param>
    /// <returns>A new array containing the resulting scalar records.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>policy</c> is unsupported.</exception>
    /// <exception cref="System.Text.EncoderFallbackException">Thrown for malformed UTF-16 when <c>policy</c> is strict.</exception>
    public UnicodeScalarRecord[] ReadAllSpan(
        System.ReadOnlySpan<char> text,
        UnicodeMalformedInputPolicy policy)
    {
        int scalarCount = CountScalars(text, policy);
        UnicodeScalarRecord[] records = new UnicodeScalarRecord[scalarCount];
        UnicodeBufferResult result = ReadIntoBuffer(text, records, policy);
        if (!result.Success)
        {
            throw new System.InvalidOperationException("The allocated scalar destination was unexpectedly too small.");
        }

        return records;
    }

    /// <summary>
    /// Decodes UTF-16 scalar records into a caller-provided destination without partial writes.
    /// </summary>
    /// <param name="text">The UTF-16 code units to process.</param>
    /// <param name="destination">The destination for scalar records.</param>
    /// <param name="policy">How malformed surrogate subsequences are handled.</param>
    /// <returns>The write result, including the required destination length.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>policy</c> is unsupported.</exception>
    /// <exception cref="System.Text.EncoderFallbackException">Thrown for malformed UTF-16 when <c>policy</c> is strict.</exception>
    public UnicodeBufferResult ReadIntoBuffer(
        System.ReadOnlySpan<char> text,
        System.Span<UnicodeScalarRecord> destination,
        UnicodeMalformedInputPolicy policy)
    {
        int requiredLength = CountScalars(text, policy);
        if (destination.Length < requiredLength)
        {
            return new UnicodeBufferResult(false, requiredLength, 0);
        }

        int index = 0;
        int destinationIndex = 0;

        while (index < text.Length)
        {
            int codeUnit = text[index];

            if (UnicodeScalar.Instance.IsHighSurrogate(codeUnit))
            {
                if (index + 1 < text.Length)
                {
                    int lowCodeUnit = text[index + 1];
                    if (UnicodeScalar.Instance.IsLowSurrogate(lowCodeUnit))
                    {
                        int codePoint = 0x10000
                            + ((codeUnit - UnicodeScalar.HighSurrogateStart) * 0x400)
                            + (lowCodeUnit - UnicodeScalar.LowSurrogateStart);
                        destination[destinationIndex] = new UnicodeScalarRecord(codePoint, index, 2, false);
                        destinationIndex = destinationIndex + 1;
                        index = index + 2;
                        continue;
                    }
                }

                destination[destinationIndex] = new UnicodeScalarRecord(UnicodeScalar.ReplacementCharacter, index, 1, true);
                destinationIndex = destinationIndex + 1;
                index = index + 1;
                continue;
            }

            if (UnicodeScalar.Instance.IsLowSurrogate(codeUnit))
            {
                destination[destinationIndex] = new UnicodeScalarRecord(UnicodeScalar.ReplacementCharacter, index, 1, true);
                destinationIndex = destinationIndex + 1;
                index = index + 1;
                continue;
            }

            destination[destinationIndex] = new UnicodeScalarRecord(codeUnit, index, 1, false);
            destinationIndex = destinationIndex + 1;
            index = index + 1;
        }

        return new UnicodeBufferResult(true, requiredLength, destinationIndex);
    }

    /// <summary>
    /// Validates text under the selected profile and returns the prepared value and diagnostics.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>An object containing the result described in the summary.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodeValidationResult Validate(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] records = ReadAll(text);
        List<UnicodeUtf16Diagnostic> diagnostics = new List<UnicodeUtf16Diagnostic>();
        int index = 0;
        int replacementCount = 0;

        while (index < text.Length)
        {
            int codeUnit = text[index];

            if (UnicodeScalar.Instance.IsHighSurrogate(codeUnit))
            {
                if (index + 1 < text.Length)
                {
                    int lowCodeUnit = text[index + 1];
                    if (UnicodeScalar.Instance.IsLowSurrogate(lowCodeUnit))
                    {
                        index = index + 2;
                        continue;
                    }
                }

                diagnostics.Add(new UnicodeUtf16Diagnostic(
                    index,
                    1,
                    codeUnit,
                    "UnpairedHighSurrogate",
                    "High surrogate is not followed by a low surrogate."));
                replacementCount = replacementCount + 1;
                index = index + 1;
                continue;
            }

            if (UnicodeScalar.Instance.IsLowSurrogate(codeUnit))
            {
                diagnostics.Add(new UnicodeUtf16Diagnostic(
                    index,
                    1,
                    codeUnit,
                    "UnpairedLowSurrogate",
                    "Low surrogate is not preceded by a high surrogate."));
                replacementCount = replacementCount + 1;
                index = index + 1;
                continue;
            }

            index = index + 1;
        }

        return new UnicodeValidationResult(
            diagnostics.Count == 0,
            records.Length,
            replacementCount,
            diagnostics.ToArray(),
            records);
    }

    /// <summary>
    /// Returns text with malformed UTF-16 sequences replaced by U+FFFD.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string ReplaceInvalid(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] records = ReadAll(text);
        StringBuilder builder = new StringBuilder(text.Length);
        int index = 0;

        while (index < records.Length)
        {
            builder.Append(UnicodeScalar.Instance.ToUtf16String(records[index].CodePoint));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static int CountScalars(
        System.ReadOnlySpan<char> text,
        UnicodeMalformedInputPolicy policy)
    {
        ValidatePolicy(policy);

        int count = 0;
        int index = 0;
        while (index < text.Length)
        {
            int codeUnit = text[index];
            if (UnicodeScalar.Instance.IsHighSurrogate(codeUnit))
            {
                if (index + 1 < text.Length && UnicodeScalar.Instance.IsLowSurrogate(text[index + 1]))
                {
                    count = count + 1;
                    index = index + 2;
                    continue;
                }

                ThrowIfStrict(policy, index);
                count = count + 1;
                index = index + 1;
                continue;
            }

            if (UnicodeScalar.Instance.IsLowSurrogate(codeUnit))
            {
                ThrowIfStrict(policy, index);
            }

            count = count + 1;
            index = index + 1;
        }

        return count;
    }

    internal static void ValidatePolicy(UnicodeMalformedInputPolicy policy)
    {
        if (policy != UnicodeMalformedInputPolicy.Replace && policy != UnicodeMalformedInputPolicy.Strict)
        {
            throw new System.ArgumentOutOfRangeException("policy");
        }
    }

    internal static void ThrowIfStrict(UnicodeMalformedInputPolicy policy, int utf16Index)
    {
        if (policy == UnicodeMalformedInputPolicy.Strict)
        {
            throw new System.Text.EncoderFallbackException(
                "Malformed UTF-16 surrogate at code-unit index " + utf16Index.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".");
        }
    }
}
