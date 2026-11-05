using System.Collections.Generic;

namespace unicode;

/// <summary>
/// Prepares Unicode text and layout metadata for use in PDF content and accessibility data.
/// </summary>
public class UnicodePdfText : IUnicodePdfText
{
    internal static readonly UnicodePdfText Instance = new UnicodePdfText();

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public UnicodePdfText()
    {
    }
    /// <summary>
    /// Replaces malformed UTF-16 and normalizes text to NFC for stable PDF extraction.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string PrepareActualText(string text)
    {
        return PrepareAccessibilityText(text, UnicodePdfTextContext.ActualText);
    }

    /// <summary>
    /// Replaces malformed UTF-16 and normalizes text to NFC for a PDF accessibility context.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="context">The PDF text context.</param>
    /// <returns>The prepared .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>context</c> is invalid.</exception>
    public string PrepareAccessibilityText(string text, UnicodePdfTextContext context)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        ValidateTextContext(context);

        string scalarSafe = UnicodeScalarReader.Instance.ReplaceInvalid(text);
        return UnicodeNormalization.Instance.Normalize(scalarSafe, UnicodeNormalizationForm.Nfc);
    }

    /// <summary>Prepares alternative text for a PDF structure element.</summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The prepared .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string PrepareAltText(string text)
    {
        return PrepareAccessibilityText(text, UnicodePdfTextContext.AltText);
    }

    /// <summary>Prepares text for a PDF bookmark.</summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The prepared .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string PrepareBookmarkText(string text)
    {
        return PrepareAccessibilityText(text, UnicodePdfTextContext.Bookmark);
    }

    /// <summary>Prepares text for a PDF outline item.</summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The prepared .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string PrepareOutlineText(string text)
    {
        return PrepareAccessibilityText(text, UnicodePdfTextContext.Outline);
    }

    /// <summary>Prepares text associated with a PDF structure element.</summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The prepared .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string PrepareStructureElementText(string text)
    {
        return PrepareAccessibilityText(text, UnicodePdfTextContext.StructureElement);
    }

    /// <summary>Prepares PDF document metadata text.</summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The prepared .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public string PrepareMetadataText(string text)
    {
        return PrepareAccessibilityText(text, UnicodePdfTextContext.Metadata);
    }

    /// <summary>Reports whether text is accessibility-ready for the requested PDF context.</summary>
    /// <param name="text">The UTF-16 text to inspect.</param>
    /// <param name="context">The PDF text context.</param>
    /// <returns><c>true</c> when the text is valid, permitted, and NFC-normalized.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>context</c> is invalid.</exception>
    public bool IsAccessibilityTextPrepared(string text, UnicodePdfTextContext context)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        ValidateTextContext(context);

        if (!ContainsOnlyValidAccessibilityScalars(text, context))
        {
            return false;
        }

        return string.Equals(text, PrepareAccessibilityText(text, context), System.StringComparison.Ordinal);
    }

    /// <summary>Reports whether text contains only accessibility-safe Unicode scalars.</summary>
    /// <param name="text">The UTF-16 text to inspect.</param>
    /// <param name="context">The PDF text context.</param>
    /// <returns><c>true</c> when all input is well formed and no scalar is prohibited or U+FFFD.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>context</c> is invalid.</exception>
    public bool ContainsOnlyValidAccessibilityScalars(string text, UnicodePdfTextContext context)
    {
        System.ArgumentNullException.ThrowIfNull(text);
        ValidateTextContext(context);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        int index = 0;
        while (index < scalars.Length)
        {
            if (scalars[index].IsReplacement)
            {
                return false;
            }

            if (scalars[index].CodePoint == UnicodeScalar.ReplacementCharacter)
            {
                return false;
            }

            if (IsProhibitedAccessibilityScalar(scalars[index].CodePoint))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    /// <summary>
    /// Reports whether text is already in the normalized form used for PDF ActualText.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public bool IsActualTextPrepared(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (!ContainsOnlyValidActualTextScalars(text))
        {
            return false;
        }

        return string.Equals(text, PrepareActualText(text), System.StringComparison.Ordinal);
    }

    /// <summary>
    /// Reports whether text contains only valid Unicode scalars allowed in PDF ActualText.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public bool ContainsOnlyValidActualTextScalars(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        int index = 0;
        while (index < scalars.Length)
        {
            if (scalars[index].IsReplacement)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void ValidateTextContext(UnicodePdfTextContext context)
    {
        if (context < UnicodePdfTextContext.ActualText || context > UnicodePdfTextContext.Metadata)
        {
            throw new System.ArgumentOutOfRangeException("context");
        }
    }

    internal static bool IsProhibitedAccessibilityScalar(int codePoint)
    {
        if (codePoint < 0x20)
        {
            if (codePoint == 0x09 || codePoint == 0x0A || codePoint == 0x0D)
            {
                return false;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns extended grapheme-cluster ranges that PDF layout should keep intact.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodeSegmentRecord[] GetLayoutClusters(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return UnicodeSegments.Instance.GetGraphemeSegments(text);
    }

    /// <summary>
    /// Groups adjacent grapheme-safe text into resolved script runs for PDF font selection.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodePdfScriptRun[] GetScriptRuns(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        if (scalars.Length == 0)
        {
            return new UnicodePdfScriptRun[]
            {
            };
        }

        UnicodeScript[] scripts = ResolveLayoutScripts(scalars);
        List<UnicodePdfScriptRun> runs = new List<UnicodePdfScriptRun>();
        int startUtf16Index = scalars[0].Utf16Index;
        UnicodeScript currentScript = scripts[0];
        int index = 1;

        while (index < scalars.Length)
        {
            if (scripts[index] != currentScript)
            {
                runs.Add(new UnicodePdfScriptRun(startUtf16Index, scalars[index].Utf16Index, currentScript));
                startUtf16Index = scalars[index].Utf16Index;
                currentScript = scripts[index];
            }

            index = index + 1;
        }

        runs.Add(new UnicodePdfScriptRun(startUtf16Index, text.Length, currentScript));
        return runs.ToArray();
    }

    /// <summary>
    /// Groups adjacent scalars into simple left-to-right and right-to-left PDF layout runs.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodePdfBidiRun[] GetBidiRuns(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] scalars = UnicodeScalarReader.Instance.ReadAll(text);
        if (scalars.Length == 0)
        {
            return new UnicodePdfBidiRun[]
            {
            };
        }

        UnicodePdfBidiDirection[] directions = ResolveLayoutBidiDirections(scalars);
        List<UnicodePdfBidiRun> runs = new List<UnicodePdfBidiRun>();
        int startUtf16Index = scalars[0].Utf16Index;
        UnicodePdfBidiDirection currentDirection = directions[0];
        int index = 1;

        while (index < scalars.Length)
        {
            if (directions[index] != currentDirection)
            {
                runs.Add(new UnicodePdfBidiRun(startUtf16Index, scalars[index].Utf16Index, currentDirection));
                startUtf16Index = scalars[index].Utf16Index;
                currentDirection = directions[index];
            }

            index = index + 1;
        }

        runs.Add(new UnicodePdfBidiRun(startUtf16Index, text.Length, currentDirection));
        return runs.ToArray();
    }

    /// <summary>
    /// Returns consolidated Unicode metadata for PDF text layout.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The grapheme, word, line, script, bidi, bracket, mirror, and width metadata.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodePdfLayoutInfo GetLayoutInfo(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodePdfScalarLayout[] scalars = GetScalarLayout(text);
        UnicodeSegmentRecord[] segments = UnicodeSegments.Instance.GetGraphemeSegments(text);
        UnicodePdfLayoutCluster[] clusters = new UnicodePdfLayoutCluster[segments.Length];
        int scalarIndex = 0;
        int segmentIndex = 0;
        while (segmentIndex < segments.Length)
        {
            while (scalarIndex < scalars.Length)
            {
                if (scalars[scalarIndex].Utf16Index >= segments[segmentIndex].StartUtf16Index)
                {
                    break;
                }

                scalarIndex = scalarIndex + 1;
            }

            UnicodeScript script = UnicodeScript.Common;
            UnicodePdfBidiDirection direction = UnicodePdfBidiDirection.Neutral;
            if (scalarIndex < scalars.Length)
            {
                script = scalars[scalarIndex].Script;
                direction = scalars[scalarIndex].Direction;
            }

            clusters[segmentIndex] = new UnicodePdfLayoutCluster(
                segments[segmentIndex].StartUtf16Index,
                segments[segmentIndex].EndUtf16Index,
                script,
                direction,
                UnicodeSegments.GetSegmentDisplayWidth(text, segments[segmentIndex]));
            segmentIndex = segmentIndex + 1;
        }

        return new UnicodePdfLayoutInfo(
            clusters,
            UnicodeSegments.Instance.GetWordSegments(text),
            UnicodeSegments.Instance.GetLineBreakOpportunities(text),
            GetScriptRuns(text),
            GetBidiRuns(text),
            scalars);
    }

    /// <summary>
    /// Returns scalar-level script, bidi, bracket, mirror, and width metadata.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing one record per decoded scalar.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    public UnicodePdfScalarLayout[] GetScalarLayout(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        UnicodeScalarRecord[] records = UnicodeScalarReader.Instance.ReadAll(text);
        UnicodeScript[] scripts = ResolveLayoutScripts(records);
        UnicodePdfBidiDirection[] directions = ResolveLayoutBidiDirections(records);
        UnicodePdfScalarLayout[] result = new UnicodePdfScalarLayout[records.Length];
        int index = 0;
        while (index < records.Length)
        {
            int codePoint = records[index].CodePoint;
            result[index] = new UnicodePdfScalarLayout(
                codePoint,
                records[index].Utf16Index,
                records[index].Utf16Length,
                scripts[index],
                directions[index],
                UnicodeProperties.Instance.GetBidiPairedBracketType(codePoint),
                UnicodeProperties.Instance.GetBidiPairedBracket(codePoint),
                UnicodeProperties.Instance.IsBidiMirrored(codePoint),
                UnicodeProperties.Instance.GetBidiMirror(codePoint),
                GetTextWidthClass(codePoint));
            index = index + 1;
        }

        return result;
    }

    /// <summary>
    /// Classifies one Unicode scalar as zero-width, narrow, or wide for PDF layout heuristics.
    /// </summary>
    /// <param name="codePoint">The Unicode scalar to inspect.</param>
    /// <returns>The scalar width classification.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>codePoint</c> is not a Unicode scalar.</exception>
    public UnicodePdfTextWidth GetTextWidthClass(int codePoint)
    {
        if (!UnicodeScalar.Instance.IsValid(codePoint))
        {
            throw new System.ArgumentOutOfRangeException("codePoint");
        }

        UnicodeGeneralCategory category = UnicodeProperties.Instance.GetGeneralCategory(codePoint);
        if (UnicodeSegments.IsZeroWidthDisplayScalar(codePoint, category))
        {
            return UnicodePdfTextWidth.Zero;
        }

        UnicodeEastAsianWidth width = UnicodeProperties.Instance.GetEastAsianWidth(codePoint);
        if (width == UnicodeEastAsianWidth.Wide || width == UnicodeEastAsianWidth.Fullwidth)
        {
            return UnicodePdfTextWidth.Wide;
        }

        if (UnicodeProperties.Instance.HasBinaryProperty(codePoint, UnicodeBinaryProperty.EmojiPresentation))
        {
            return UnicodePdfTextWidth.Wide;
        }

        return UnicodePdfTextWidth.Narrow;
    }

    internal static UnicodePdfBidiDirection[] ResolveLayoutBidiDirections(UnicodeScalarRecord[] scalars)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        UnicodePdfBidiDirection[] directions = new UnicodePdfBidiDirection[scalars.Length];
        int index = 0;
        while (index < scalars.Length)
        {
            UnicodePdfBidiDirection direction = GetBidiDirection(UnicodeProperties.Instance.GetBidiClass(scalars[index].CodePoint));
            if (direction == UnicodePdfBidiDirection.Neutral)
            {
                direction = FindNearestStrongBidiDirection(scalars, index);
            }

            directions[index] = direction;
            index = index + 1;
        }

        return directions;
    }

    internal static UnicodePdfBidiDirection FindNearestStrongBidiDirection(UnicodeScalarRecord[] scalars, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        if (index < 0)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        if (index >= scalars.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        int scanIndex = index - 1;
        while (scanIndex >= 0)
        {
            UnicodePdfBidiDirection direction = GetBidiDirection(UnicodeProperties.Instance.GetBidiClass(scalars[scanIndex].CodePoint));
            if (direction != UnicodePdfBidiDirection.Neutral)
            {
                return direction;
            }

            scanIndex = scanIndex - 1;
        }

        scanIndex = index + 1;
        while (scanIndex < scalars.Length)
        {
            UnicodePdfBidiDirection direction = GetBidiDirection(UnicodeProperties.Instance.GetBidiClass(scalars[scanIndex].CodePoint));
            if (direction != UnicodePdfBidiDirection.Neutral)
            {
                return direction;
            }

            scanIndex = scanIndex + 1;
        }

        return UnicodePdfBidiDirection.Neutral;
    }

    internal static UnicodePdfBidiDirection GetBidiDirection(UnicodeBidiClass bidiClass)
    {
        if (bidiClass == UnicodeBidiClass.RightToLeft)
        {
            return UnicodePdfBidiDirection.RightToLeft;
        }

        if (bidiClass == UnicodeBidiClass.ArabicLetter)
        {
            return UnicodePdfBidiDirection.RightToLeft;
        }

        if (bidiClass == UnicodeBidiClass.ArabicNumber)
        {
            return UnicodePdfBidiDirection.RightToLeft;
        }

        if (bidiClass == UnicodeBidiClass.LeftToRight)
        {
            return UnicodePdfBidiDirection.LeftToRight;
        }

        if (bidiClass == UnicodeBidiClass.EuropeanNumber)
        {
            return UnicodePdfBidiDirection.LeftToRight;
        }

        return UnicodePdfBidiDirection.Neutral;
    }

    internal static UnicodeScript[] ResolveLayoutScripts(UnicodeScalarRecord[] scalars)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        UnicodeScript[] scripts = new UnicodeScript[scalars.Length];
        int index = 0;
        while (index < scalars.Length)
        {
            UnicodeScript script = UnicodeProperties.Instance.GetScript(scalars[index].CodePoint);
            if (IsNeutralLayoutScript(script))
            {
                script = FindNearestStrongLayoutScript(scalars, index);
            }

            scripts[index] = script;
            index = index + 1;
        }

        return scripts;
    }

    internal static UnicodeScript FindNearestStrongLayoutScript(UnicodeScalarRecord[] scalars, int index)
    {
        System.ArgumentNullException.ThrowIfNull(scalars);

        if (index < 0)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        if (index >= scalars.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        int scanIndex = index - 1;
        while (scanIndex >= 0)
        {
            UnicodeScript script = UnicodeProperties.Instance.GetScript(scalars[scanIndex].CodePoint);
            if (!IsNeutralLayoutScript(script))
            {
                return script;
            }

            scanIndex = scanIndex - 1;
        }

        scanIndex = index + 1;
        while (scanIndex < scalars.Length)
        {
            UnicodeScript script = UnicodeProperties.Instance.GetScript(scalars[scanIndex].CodePoint);
            if (!IsNeutralLayoutScript(script))
            {
                return script;
            }

            scanIndex = scanIndex + 1;
        }

        return UnicodeScript.Common;
    }

    internal static bool IsNeutralLayoutScript(UnicodeScript script)
    {
        if (script == UnicodeScript.Common)
        {
            return true;
        }

        if (script == UnicodeScript.Inherited)
        {
            return true;
        }

        return script == UnicodeScript.Unknown;
    }

    /// <summary>
    /// Escapes bytes for a PDF literal string, including delimiters, control bytes, and non-printable bytes.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    public string EscapeLiteralString(byte[] bytes)
    {
        System.ArgumentNullException.ThrowIfNull(bytes);

        byte[] escaped = new byte[GetLiteralStringLength(bytes)];
        EscapeLiteralStringBuffer(bytes, escaped);
        return System.Text.Encoding.ASCII.GetString(escaped);
    }

    /// <summary>
    /// Escapes PDF literal-string content into a caller-provided ASCII byte destination without partial writes.
    /// </summary>
    /// <param name="bytes">The bytes to escape.</param>
    /// <param name="destination">The destination for escaped ASCII bytes.</param>
    /// <returns>The write result, including the required byte length.</returns>
    public UnicodeBufferResult EscapeLiteralStringBuffer(
        System.ReadOnlySpan<byte> bytes,
        System.Span<byte> destination)
    {
        int requiredLength = GetLiteralStringLength(bytes);
        if (destination.Length < requiredLength)
        {
            return new UnicodeBufferResult(false, requiredLength, 0);
        }

        int sourceIndex = 0;
        int destinationIndex = 0;
        while (sourceIndex < bytes.Length)
        {
            destinationIndex = WriteLiteralByte(destination, destinationIndex, bytes[sourceIndex]);
            sourceIndex = sourceIndex + 1;
        }

        return new UnicodeBufferResult(true, requiredLength, destinationIndex);
    }

    /// <summary>
    /// Formats bytes as uppercase PDF hexadecimal-string content.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    public string ToHexString(byte[] bytes)
    {
        System.ArgumentNullException.ThrowIfNull(bytes);

        byte[] hexadecimal = new byte[GetHexStringLength(bytes)];
        ToHexStringBuffer(bytes, hexadecimal);
        return System.Text.Encoding.ASCII.GetString(hexadecimal);
    }

    /// <summary>
    /// Formats PDF hexadecimal-string content into a caller-provided ASCII byte destination without partial writes.
    /// </summary>
    /// <param name="bytes">The bytes to format.</param>
    /// <param name="destination">The destination for uppercase hexadecimal ASCII bytes.</param>
    /// <returns>The write result, including the required byte length.</returns>
    public UnicodeBufferResult ToHexStringBuffer(
        System.ReadOnlySpan<byte> bytes,
        System.Span<byte> destination)
    {
        int requiredLength = GetHexStringLength(bytes);
        if (destination.Length < requiredLength)
        {
            return new UnicodeBufferResult(false, requiredLength, 0);
        }

        int sourceIndex = 0;
        int destinationIndex = 0;
        while (sourceIndex < bytes.Length)
        {
            int value = bytes[sourceIndex];
            destination[destinationIndex] = (byte)ToHexDigit(value >> 4);
            destination[destinationIndex + 1] = (byte)ToHexDigit(value & 0x0F);
            sourceIndex = sourceIndex + 1;
            destinationIndex = destinationIndex + 2;
        }

        return new UnicodeBufferResult(true, requiredLength, destinationIndex);
    }

    internal static int GetLiteralStringLength(System.ReadOnlySpan<byte> bytes)
    {
        int requiredLength = 0;
        int index = 0;
        while (index < bytes.Length)
        {
            int increment = 1;
            byte value = bytes[index];
            if (value == 0x08 || value == 0x09 || value == 0x0A || value == 0x0C || value == 0x0D)
            {
                increment = 2;
            }
            else if (value == 0x28 || value == 0x29 || value == 0x5C)
            {
                increment = 2;
            }
            else if (value < 0x20 || value > 0x7E)
            {
                increment = 4;
            }

            if (requiredLength > int.MaxValue - increment)
            {
                throw new System.ArgumentException("Bytes are too long to escape.", "bytes");
            }

            requiredLength = requiredLength + increment;
            index = index + 1;
        }

        return requiredLength;
    }

    internal static int GetHexStringLength(System.ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > int.MaxValue / 2)
        {
            throw new System.ArgumentException("Bytes are too long to format.", "bytes");
        }

        return bytes.Length * 2;
    }

    internal static int WriteLiteralByte(
        System.Span<byte> destination,
        int index,
        byte value)
    {
        if (value == 0x08)
        {
            destination[index] = 0x5C;
            destination[index + 1] = 0x62;
            return index + 2;
        }

        if (value == 0x09)
        {
            destination[index] = 0x5C;
            destination[index + 1] = 0x74;
            return index + 2;
        }

        if (value == 0x0A)
        {
            destination[index] = 0x5C;
            destination[index + 1] = 0x6E;
            return index + 2;
        }

        if (value == 0x0C)
        {
            destination[index] = 0x5C;
            destination[index + 1] = 0x66;
            return index + 2;
        }

        if (value == 0x0D)
        {
            destination[index] = 0x5C;
            destination[index + 1] = 0x72;
            return index + 2;
        }

        if (value == 0x28 || value == 0x29 || value == 0x5C)
        {
            destination[index] = 0x5C;
            destination[index + 1] = value;
            return index + 2;
        }

        if (value >= 0x20 && value <= 0x7E)
        {
            destination[index] = value;
            return index + 1;
        }

        destination[index] = 0x5C;
        destination[index + 1] = (byte)(0x30 + ((value >> 6) & 0x07));
        destination[index + 2] = (byte)(0x30 + ((value >> 3) & 0x07));
        destination[index + 3] = (byte)(0x30 + (value & 0x07));
        return index + 4;
    }

    internal static void AppendLiteralByte(System.Text.StringBuilder builder, byte value)
    {
        System.ArgumentNullException.ThrowIfNull(builder);

        if (value == 0x08)
        {
            builder.Append("\\b");
            return;
        }

        if (value == 0x09)
        {
            builder.Append("\\t");
            return;
        }

        if (value == 0x0A)
        {
            builder.Append("\\n");
            return;
        }

        if (value == 0x0C)
        {
            builder.Append("\\f");
            return;
        }

        if (value == 0x0D)
        {
            builder.Append("\\r");
            return;
        }

        if (value == 0x28)
        {
            builder.Append("\\(");
            return;
        }

        if (value == 0x29)
        {
            builder.Append("\\)");
            return;
        }

        if (value == 0x5C)
        {
            builder.Append("\\\\");
            return;
        }

        if (value >= 0x20)
        {
            if (value <= 0x7E)
            {
                builder.Append((char)value);
                return;
            }
        }

        AppendOctalByte(builder, value);
    }

    internal static void AppendOctalByte(System.Text.StringBuilder builder, byte value)
    {
        System.ArgumentNullException.ThrowIfNull(builder);

        builder.Append('\\');
        builder.Append((char)('0' + ((value >> 6) & 0x07)));
        builder.Append((char)('0' + ((value >> 3) & 0x07)));
        builder.Append((char)('0' + (value & 0x07)));
    }

    internal static char ToHexDigit(int value)
    {
        if (value < 0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        if (value > 15)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        if (value < 10)
        {
            return (char)('0' + value);
        }

        return (char)('A' + (value - 10));
    }
}
