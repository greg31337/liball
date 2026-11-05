using System;
using System.Globalization;
using System.Threading;

namespace html;

/// <summary>Decodes and encodes HTML character references with context-sensitive rules.</summary>
internal static class HtmlEntities
{
    /// <summary>Decodes the supplied input using HTML parsing rules.</summary>
    /// <param name="value">The text containing character references to decode.</param>
    /// <returns>The decoded text after applying data-state character-reference rules.</returns>
    public static string Decode(string value)
    {
        return DecodeText(value);
    }

    /// <summary>Decodes the supplied input using HTML parsing rules.</summary>
    /// <param name="value">The text containing character references to decode.</param>
    /// <param name="context">The context that selects the applicable HTML parsing or encoding rules.</param>
    /// <returns>The decoded text after applying the selected character-reference context.</returns>
    public static string Decode(string value, HtmlEntityContext context)
    {
        ValidateInput(value);
        HtmlLimits limits = CreateConvenienceLimits(value.Length);
        return Decode(value, context, limits, CancellationToken.None).GetString();
    }

    /// <summary>Decodes the supplied input using HTML parsing rules.</summary>
    /// <param name="value">The text containing character references to decode.</param>
    /// <param name="context">The context that selects the applicable HTML parsing or encoding rules.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The transformed text, completion status, and diagnostics.</returns>
    public static HtmlEntityResult Decode(string value, HtmlEntityContext context,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        return Decode(value, GetReferenceContext(context), limits, cancellationToken);
    }

    /// <summary>Encodes the supplied text for the requested HTML context.</summary>
    /// <param name="value">The text to encode for safe inclusion in HTML.</param>
    /// <param name="mode">The character-reference style used for the encoded output.</param>
    /// <returns>The encoded text using the selected reference style.</returns>
    public static string Encode(string value, HtmlEntityEncoding mode)
    {
        return EncodeText(value, mode);
    }

    /// <summary>Encodes the supplied text for the requested HTML context.</summary>
    /// <param name="value">The text to encode for safe inclusion in HTML.</param>
    /// <param name="context">The context that selects the applicable HTML parsing or encoding rules.</param>
    /// <param name="mode">The character-reference style used for the encoded output.</param>
    /// <returns>The encoded text using the selected context and reference style.</returns>
    public static string Encode(string value, HtmlEntityContext context, HtmlEntityEncoding mode)
    {
        ValidateInput(value);
        HtmlLimits limits = CreateConvenienceLimits(value.Length);
        return Encode(value, context, mode, limits, CancellationToken.None).GetString();
    }

    /// <summary>Encodes the supplied text for the requested HTML context.</summary>
    /// <param name="value">The text to encode for safe inclusion in HTML.</param>
    /// <param name="context">The context that selects the applicable HTML parsing or encoding rules.</param>
    /// <param name="mode">The character-reference style used for the encoded output.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The transformed text, completion status, and diagnostics.</returns>
    public static HtmlEntityResult Encode(string value, HtmlEntityContext context,
        HtmlEntityEncoding mode, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return Encode(value, GetReferenceContext(context), mode, limits, cancellationToken);
    }

    /// <summary>Decodes the text using HTML parsing rules.</summary>
    /// <param name="input">The input data to process.</param>
    /// <returns>The decoded text after applying HTML data-state rules.</returns>
    public static string DecodeText(string input)
    {
        ValidateInput(input);
        HtmlLimits limits = CreateConvenienceLimits(input.Length);
        return DecodeText(input, limits, CancellationToken.None).GetString();
    }

    /// <summary>Decodes the attribute using HTML parsing rules.</summary>
    /// <param name="input">The input data to process.</param>
    /// <returns>The decoded text after applying HTML attribute-value rules.</returns>
    public static string DecodeAttribute(string input)
    {
        ValidateInput(input);
        HtmlLimits limits = CreateConvenienceLimits(input.Length);
        return DecodeAttribute(input, limits, CancellationToken.None).GetString();
    }

    /// <summary>Decodes the text using HTML parsing rules.</summary>
    /// <param name="input">The input data to process.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The transformed text, completion status, and diagnostics.</returns>
    public static HtmlEntityResult DecodeText(string input, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        return Decode(input, HtmlCharacterReferenceContext.Data, limits, cancellationToken);
    }

    /// <summary>Decodes the attribute using HTML parsing rules.</summary>
    /// <param name="input">The input data to process.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The transformed text, completion status, and diagnostics.</returns>
    public static HtmlEntityResult DecodeAttribute(string input, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        return Decode(input, HtmlCharacterReferenceContext.Attribute, limits, cancellationToken);
    }

    /// <summary>Encodes the text for HTML output.</summary>
    /// <param name="input">The input data to process.</param>
    /// <param name="mode">The character-reference style used for the encoded output.</param>
    /// <returns>The text encoded for an HTML text-node context.</returns>
    public static string EncodeText(string input, HtmlEntityEncoding mode)
    {
        ValidateInput(input);
        HtmlLimits limits = CreateConvenienceLimits(input.Length);
        return Encode(input, HtmlCharacterReferenceContext.Data, mode, limits,
            CancellationToken.None).GetString();
    }

    /// <summary>Encodes the attribute for HTML output.</summary>
    /// <param name="input">The input data to process.</param>
    /// <param name="mode">The character-reference style used for the encoded output.</param>
    /// <returns>The text encoded for a quoted HTML attribute-value context.</returns>
    public static string EncodeAttribute(string input, HtmlEntityEncoding mode)
    {
        ValidateInput(input);
        HtmlLimits limits = CreateConvenienceLimits(input.Length);
        return Encode(input, HtmlCharacterReferenceContext.Attribute, mode, limits,
            CancellationToken.None).GetString();
    }

    /// <summary>Encodes the text for HTML output.</summary>
    /// <param name="input">The input data to process.</param>
    /// <param name="mode">The character-reference style used for the encoded output.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The transformed text, completion status, and diagnostics.</returns>
    public static HtmlEntityResult EncodeText(string input, HtmlEntityEncoding mode,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        return Encode(input, HtmlCharacterReferenceContext.Data, mode, limits, cancellationToken);
    }

    /// <summary>Encodes the attribute for HTML output.</summary>
    /// <param name="input">The input data to process.</param>
    /// <param name="mode">The character-reference style used for the encoded output.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The transformed text, completion status, and diagnostics.</returns>
    public static HtmlEntityResult EncodeAttribute(string input, HtmlEntityEncoding mode,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        return Encode(input, HtmlCharacterReferenceContext.Attribute, mode, limits, cancellationToken);
    }

    internal static HtmlEntityResult Decode(string input, HtmlCharacterReferenceContext context,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        ValidateInputAndLimits(input, limits);
        HtmlBudget budget = new HtmlBudget(limits, cancellationToken);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        int acceptedLength = ConsumeInput(input, budget);
        int initialCapacity = GetInitialCapacity(acceptedLength, limits.GetMaximumOutputCharacters());
        CharacterBuffer output = CharacterBuffer.CreateOutput(budget, initialCapacity);
        int position = 0;
        bool outputComplete = true;
        while (position < acceptedLength)
        {
            budget.CheckCancellation();
            char value = input[position];
            if (value != '&')
            {
                if (!output.TryAppend(value))
                {
                    AddLimitDiagnostic(diagnostics, input, position, HtmlDiagnosticCode.ResourceLimitExceeded,
                        "The entity-decoding output character limit was exceeded.");
                    outputComplete = false;
                    break;
                }

                position = position + 1;
                continue;
            }

            HtmlCharacterReferenceResult reference = HtmlCharacterReferenceDecoder.Decode(
                input.AsSpan(position + 1, acceptedLength - position - 1), true, context);
            AddReferenceDiagnostics(reference, diagnostics, input, position, acceptedLength);
            if (reference.IsMatch())
            {
                if (!output.TryAppendString(reference.GetReplacementString()))
                {
                    AddLimitDiagnostic(diagnostics, input, position, HtmlDiagnosticCode.ResourceLimitExceeded,
                        "The entity-decoding output character limit was exceeded.");
                    outputComplete = false;
                    break;
                }

                position = position + 1 + reference.GetConsumedCharacters();
            }
            else
            {
                if (!output.TryAppend('&'))
                {
                    AddLimitDiagnostic(diagnostics, input, position, HtmlDiagnosticCode.ResourceLimitExceeded,
                        "The entity-decoding output character limit was exceeded.");
                    outputComplete = false;
                    break;
                }

                position = position + 1;
            }
        }

        bool inputComplete = acceptedLength == input.Length;
        if (!inputComplete)
        {
            AddLimitDiagnostic(diagnostics, input, acceptedLength, HtmlDiagnosticCode.InputLimitExceeded,
                "The entity-decoding input character limit was exceeded.");
        }

        return HtmlEntityResult.Create(output.GetString(), diagnostics.GetDiagnostics(),
            inputComplete && outputComplete, budget.HasExceededLimit(),
            diagnostics.GetTotalCount(), diagnostics.GetDroppedCount());
    }

    internal static HtmlEntityResult Encode(string input, HtmlCharacterReferenceContext context,
        HtmlEntityEncoding mode, HtmlLimits limits, CancellationToken cancellationToken)
    {
        ValidateInputAndLimits(input, limits);
        ValidateMode(mode);
        HtmlBudget budget = new HtmlBudget(limits, cancellationToken);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        int acceptedLength = ConsumeInput(input, budget);
        int initialCapacity = GetInitialCapacity(acceptedLength, limits.GetMaximumOutputCharacters());
        CharacterBuffer output = CharacterBuffer.CreateOutput(budget, initialCapacity);
        int position = 0;
        bool outputComplete = true;
        while (position < acceptedLength)
        {
            budget.CheckCancellation();
            int scalarLength = GetScalarLength(input, position, acceptedLength);
            int scalar = GetScalar(input, position, acceptedLength);
            if (scalar == UnicodeTools.ReplacementCharacter
                && IsUnpairedSurrogate(input, position, acceptedLength))
            {
                diagnostics.AddDetails(HtmlDiagnosticCode.InputInvalidScalar, HtmlDiagnosticSeverity.Warning,
                    "An unpaired UTF-16 surrogate was replaced while encoding entities.",
                    CreateSpan(input, position, position + 1));
            }

            string pairName = "";
            int nextScalarLength = 0;
            if ((mode == HtmlEntityEncoding.Named || mode == HtmlEntityEncoding.AsciiOnly)
                && position + scalarLength < acceptedLength
                && !IsUnpairedSurrogate(input, position + scalarLength, acceptedLength))
            {
                nextScalarLength = GetScalarLength(input, position + scalarLength, acceptedLength);
                int nextScalar = GetScalar(input, position + scalarLength, acceptedLength);
                if (mode == HtmlEntityEncoding.Named
                    || RequiresEncoding(scalar, context) || RequiresEncoding(nextScalar, context)
                    || scalar > 127 || nextScalar > 127)
                {
                    pairName = HtmlNamedCharacterReferences.GetPreferredName(scalar, nextScalar);
                }
            }

            bool appended;
            if (pairName.Length > 0)
            {
                appended = output.TryAppendString("&" + pairName);
                if (appended)
                {
                    scalarLength = scalarLength + nextScalarLength;
                }
            }
            else
            {
                appended = AppendEncodedScalar(output, scalar, context, mode);
            }

            if (!appended)
            {
                AddLimitDiagnostic(diagnostics, input, position, HtmlDiagnosticCode.ResourceLimitExceeded,
                    "The entity-encoding output character limit was exceeded.");
                outputComplete = false;
                break;
            }

            position = position + scalarLength;
        }

        bool inputComplete = acceptedLength == input.Length;
        if (!inputComplete)
        {
            AddLimitDiagnostic(diagnostics, input, acceptedLength, HtmlDiagnosticCode.InputLimitExceeded,
                "The entity-encoding input character limit was exceeded.");
        }

        return HtmlEntityResult.Create(output.GetString(), diagnostics.GetDiagnostics(),
            inputComplete && outputComplete, budget.HasExceededLimit(),
            diagnostics.GetTotalCount(), diagnostics.GetDroppedCount());
    }

    internal static bool AppendEncodedScalar(CharacterBuffer output, int scalar,
        HtmlCharacterReferenceContext context, HtmlEntityEncoding mode)
    {
        bool required = RequiresEncoding(scalar, context);
        if (mode == HtmlEntityEncoding.Minimal)
        {
            string minimalName = required ? GetMinimalName(scalar) : "";
            return minimalName.Length > 0
                ? output.TryAppendString("&" + minimalName)
                : output.TryAppendScalar(scalar);
        }

        if (mode == HtmlEntityEncoding.Named)
        {
            string name = HtmlNamedCharacterReferences.GetPreferredName(scalar, -1);
            return name.Length > 0
                ? output.TryAppendString("&" + name)
                : output.TryAppendScalar(scalar);
        }

        if (mode == HtmlEntityEncoding.Numeric)
        {
            return required || scalar > 127
                ? output.TryAppendString(GetNumericReference(scalar))
                : output.TryAppendScalar(scalar);
        }

        if (required || scalar > 127)
        {
            string name = HtmlNamedCharacterReferences.GetPreferredName(scalar, -1);
            return name.Length > 0
                ? output.TryAppendString("&" + name)
                : output.TryAppendString(GetNumericReference(scalar));
        }

        return output.TryAppendScalar(scalar);
    }

    internal static bool RequiresEncoding(int scalar, HtmlCharacterReferenceContext context)
    {
        return scalar == '&' || scalar == 0xA0 || scalar == '<' || scalar == '>'
            || context == HtmlCharacterReferenceContext.Attribute && scalar == '"';
    }

    internal static string GetMinimalName(int scalar)
    {
        if (scalar == '&')
        {
            return "amp;";
        }

        if (scalar == 0xA0)
        {
            return "nbsp;";
        }

        if (scalar == '<')
        {
            return "lt;";
        }

        if (scalar == '>')
        {
            return "gt;";
        }

        if (scalar == '"')
        {
            return "quot;";
        }

        return "";
    }

    internal static string GetNumericReference(int scalar)
    {
        return "&#x" + scalar.ToString("X", CultureInfo.InvariantCulture) + ";";
    }

    internal static int ConsumeInput(string input, HtmlBudget budget)
    {
        int maximum = budget.GetLimits().GetMaximumInputCharacters();
        if (input.Length <= maximum)
        {
            budget.TryConsume(HtmlLimitKind.InputCharacters, input.Length);
            return input.Length;
        }

        budget.TryConsume(HtmlLimitKind.InputCharacters, maximum);
        budget.TryConsume(HtmlLimitKind.InputCharacters, 1);
        return maximum;
    }

    internal static int GetInitialCapacity(int inputLength, int maximumOutputCharacters)
    {
        int capacity = inputLength < 256 ? inputLength : 256;
        return capacity < maximumOutputCharacters ? capacity : maximumOutputCharacters;
    }

    internal static int GetScalarLength(string input, int position, int end)
    {
        if (UnicodeTools.IsHighSurrogate(input[position])
            && position + 1 < end && UnicodeTools.IsLowSurrogate(input[position + 1]))
        {
            return 2;
        }

        return 1;
    }

    internal static int GetScalar(string input, int position, int end)
    {
        char value = input[position];
        if (UnicodeTools.IsHighSurrogate(value)
            && position + 1 < end && UnicodeTools.IsLowSurrogate(input[position + 1]))
        {
            return UnicodeTools.CombineSurrogates(value, input[position + 1]);
        }

        if (UnicodeTools.IsHighSurrogate(value) || UnicodeTools.IsLowSurrogate(value))
        {
            return UnicodeTools.ReplacementCharacter;
        }

        return value;
    }

    internal static bool IsUnpairedSurrogate(string input, int position, int end)
    {
        char value = input[position];
        if (UnicodeTools.IsHighSurrogate(value))
        {
            return position + 1 >= end || !UnicodeTools.IsLowSurrogate(input[position + 1]);
        }

        return UnicodeTools.IsLowSurrogate(value);
    }

    internal static void AddReferenceDiagnostics(HtmlCharacterReferenceResult reference,
        HtmlDiagnosticCollector diagnostics, string input, int ampersandOffset, int acceptedLength)
    {
        int index = 0;
        while (index < reference.GetDiagnosticCount())
        {
            int end = ampersandOffset + reference.GetDiagnosticCharacterLength(index);
            if (end > acceptedLength)
            {
                end = acceptedLength;
            }

            diagnostics.AddDetails(reference.GetDiagnosticCode(index), HtmlDiagnosticSeverity.Warning,
                reference.GetDiagnosticMessage(index), CreateSpan(input, ampersandOffset, end));
            index = index + 1;
        }
    }

    internal static void AddLimitDiagnostic(HtmlDiagnosticCollector diagnostics, string input,
        int offset, HtmlDiagnosticCode code, string message)
    {
        HtmlSourceSpan span = CreateSpan(input, offset, offset);
        diagnostics.AddDetails(code, HtmlDiagnosticSeverity.Error, message, span);
    }

    internal static HtmlSourceSpan CreateSpan(string input, int start, int end)
    {
        return HtmlSourceSpan.Create(GetPosition(input, start), GetPosition(input, end));
    }

    internal static HtmlSourcePosition GetPosition(string input, int offset)
    {
        int line = 1;
        int column = 1;
        int position = 0;
        while (position < offset)
        {
            char value = input[position];
            if (value == '\r')
            {
                line = line + 1;
                column = 1;
                position = position + 1;
                if (position < offset && input[position] == '\n')
                {
                    position = position + 1;
                }
            }
            else if (value == '\n')
            {
                line = line + 1;
                column = 1;
                position = position + 1;
            }
            else
            {
                column = column + 1;
                position = position + 1;
            }
        }

        return HtmlSourcePosition.CreateMapped(offset, offset, line, column);
    }

    internal static HtmlLimits CreateConvenienceLimits(int inputLength)
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        if (inputLength > limits.GetMaximumInputCharacters())
        {
            limits.SetMaximumInputCharacters(inputLength);
        }

        limits.SetMaximumOutputCharacters(int.MaxValue);
        return limits;
    }

    internal static void ValidateInput(string input)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }
    }

    internal static void ValidateInputAndLimits(string input, HtmlLimits limits)
    {
        ValidateInput(input);
        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }
    }

    internal static HtmlCharacterReferenceContext GetReferenceContext(HtmlEntityContext context)
    {
        if (context == HtmlEntityContext.Text)
        {
            return HtmlCharacterReferenceContext.Data;
        }

        if (context == HtmlEntityContext.Attribute)
        {
            return HtmlCharacterReferenceContext.Attribute;
        }

        throw new ArgumentOutOfRangeException("context", (int)context,
            "Unknown entity context.");
    }

    internal static void ValidateMode(HtmlEntityEncoding mode)
    {
        if ((int)mode < 0 || (int)mode > 3)
        {
            throw new ArgumentOutOfRangeException("mode", (int)mode,
                "Unknown entity-encoding mode.");
        }
    }
}
