namespace ttf;

/// <summary>Loads and matches a bounded caller-supplied hyphenation pattern dictionary.</summary>
/// <remarks>
/// Input is strict UTF-8. Blank lines and text following <c>%</c> or <c>#</c>
/// are ignored. Bare whitespace-separated entries are Liang patterns: ASCII
/// digits are inter-letter weights and a leading or trailing period anchors a
/// word boundary. The directives <c>LEFTHYPHENMIN</c> and
/// <c>RIGHTHYPHENMIN</c> set edge minima. An <c>exception</c> directive accepts
/// whitespace-separated words containing ASCII hyphens at the allowed breaks.
/// TeX <c>\patterns{}</c> and <c>\hyphenation{}</c> blocks are also accepted.
/// Matching uses invariant one-scalar case conversion and never retains input
/// bytes or a caller stream.
/// </remarks>
public class HyphenationDictionary : IHyphenationDictionary
{
    HyphenationNode root;
    System.Collections.Generic.Dictionary<string, int[]> exceptions;
    int leftMinimum;
    int rightMinimum;
    int patternCount;
    int exceptionCount;
    int nodeCount;
    HyphenationLimits limits;
    int blockMode;

    HyphenationDictionary(HyphenationLimits limits)
    {
        this.limits = limits;
        root = new HyphenationNode();
        exceptions = new System.Collections.Generic.Dictionary<string, int[]>(
            System.StringComparer.Ordinal);
        leftMinimum = 2;
        rightMinimum = 2;
        nodeCount = 1;
    }

    /// <summary>Loads a dictionary from copied bytes using conservative default limits.</summary>
    /// <param name="data">The complete UTF-8 dictionary bytes.</param>
    /// <returns>The loaded immutable dictionary.</returns>
    /// <exception cref="System.ArgumentNullException"><c>data</c> is null.</exception>
    /// <exception cref="System.Text.DecoderFallbackException">The input is not valid UTF-8.</exception>
    /// <exception cref="HyphenationFormatException">The dictionary is malformed.</exception>
    /// <exception cref="FontLimitException">A resource limit is exceeded.</exception>
    internal static HyphenationDictionary Load(byte[] data)
    {
        return Load(data, HyphenationLimits.Create());
    }

    /// <summary>Loads a dictionary from copied bytes using explicit limits.</summary>
    /// <param name="data">The complete UTF-8 dictionary bytes.</param>
    /// <param name="limits">The hard loading and matching limits.</param>
    /// <returns>The loaded immutable dictionary.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>data</c> or <c>limits</c> is null.
    /// </exception>
    /// <exception cref="System.Text.DecoderFallbackException">The input is not valid UTF-8.</exception>
    /// <exception cref="HyphenationFormatException">The dictionary is malformed.</exception>
    /// <exception cref="FontLimitException">A resource limit is exceeded.</exception>
    internal static HyphenationDictionary Load(byte[] data, HyphenationLimits limits)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        ValidateLimits(limits);
        if (data.Length > limits.GetMaxInputBytes())
        {
            throw FontLimitException.Create(
                "The hyphenation dictionary exceeds the configured input limit.");
        }

        byte[] ownedData = new byte[data.Length];
        System.Buffer.BlockCopy(data, 0, ownedData, 0, data.Length);
        return Parse(ownedData, limits);
    }

    /// <summary>Loads a dictionary from a caller-owned stream using default limits.</summary>
    /// <param name="stream">The readable stream at the first dictionary byte.</param>
    /// <returns>The loaded immutable dictionary.</returns>
    /// <exception cref="System.ArgumentNullException"><c>stream</c> is null.</exception>
    /// <exception cref="System.ArgumentException"><c>stream</c> is not readable.</exception>
    /// <exception cref="System.IO.IOException">The stream cannot be read.</exception>
    /// <exception cref="System.Text.DecoderFallbackException">The input is not valid UTF-8.</exception>
    /// <exception cref="HyphenationFormatException">The dictionary is malformed.</exception>
    /// <exception cref="FontLimitException">A resource limit is exceeded.</exception>
    internal static HyphenationDictionary Load(System.IO.Stream stream)
    {
        return Load(stream, HyphenationLimits.Create());
    }

    /// <summary>Loads a dictionary from a caller-owned stream using explicit limits.</summary>
    /// <remarks>The stream is read from its current position through end and is left open.</remarks>
    /// <param name="stream">The readable stream at the first dictionary byte.</param>
    /// <param name="limits">The hard loading and matching limits.</param>
    /// <returns>The loaded immutable dictionary.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>stream</c> or <c>limits</c> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException"><c>stream</c> is not readable.</exception>
    /// <exception cref="System.IO.IOException">The stream cannot be read.</exception>
    /// <exception cref="System.Text.DecoderFallbackException">The input is not valid UTF-8.</exception>
    /// <exception cref="HyphenationFormatException">The dictionary is malformed.</exception>
    /// <exception cref="FontLimitException">A resource limit is exceeded.</exception>
    internal static HyphenationDictionary Load(
        System.IO.Stream stream,
        HyphenationLimits limits)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }

        ValidateLimits(limits);
        if (!stream.CanRead)
        {
            throw new System.ArgumentException(
                "The hyphenation dictionary stream must be readable.",
                "stream");
        }

        int maximumLength = limits.GetMaxInputBytes();
        int bufferLength = maximumLength < 81920 ? maximumLength : 81920;
        byte[] buffer = new byte[bufferLength];
        using (System.IO.MemoryStream output = new System.IO.MemoryStream())
        {
            while (true)
            {
                int remaining = maximumLength - (int)output.Length;
                int requestedLength = remaining == 0
                    ? 1
                    : (remaining < buffer.Length ? remaining : buffer.Length);
                int readLength = stream.Read(buffer, 0, requestedLength);
                if (readLength == 0)
                {
                    break;
                }

                if (remaining == 0)
                {
                    throw FontLimitException.Create(
                        "The hyphenation dictionary stream exceeds the configured input limit.");
                }

                output.Write(buffer, 0, readLength);
            }

            return Parse(output.ToArray(), limits);
        }
    }

    /// <summary>Returns the effective minimum scalars retained before a break.</summary>
    /// <returns>The left-edge minimum.</returns>
    public int GetLeftMinimum()
    {
        return leftMinimum;
    }

    /// <summary>Returns the effective minimum scalars retained after a break.</summary>
    /// <returns>The right-edge minimum.</returns>
    public int GetRightMinimum()
    {
        return rightMinimum;
    }

    /// <summary>Returns the number of loaded pattern declarations.</summary>
    /// <returns>The pattern declaration count.</returns>
    public int GetPatternCount()
    {
        return patternCount;
    }

    /// <summary>Returns the number of loaded exception declarations.</summary>
    /// <returns>The exception declaration count.</returns>
    public int GetExceptionCount()
    {
        return exceptionCount;
    }

    /// <summary>Matches a word and returns its allowed internal hyphenation boundaries.</summary>
    /// <param name="word">The word to match.</param>
    /// <returns>Immutable hyphenation opportunities in ascending source order.</returns>
    /// <exception cref="System.ArgumentNullException"><c>word</c> is null.</exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// <c>word</c> contains malformed UTF-16.
    /// </exception>
    /// <exception cref="FontLimitException">The configured word length is exceeded.</exception>
    public HyphenationMatch Match(string word)
    {
        RuneBuffer runes = Utf16Decoder.Decode(
            word,
            Utf16DecodingMode.Strict,
            limits.GetMaxWordLength());
        int runeCount = runes.GetCount();
        if (runeCount == 0)
        {
            return new HyphenationMatch(0, new int[0], new int[0]);
        }

        string normalizedWord = BuildNormalizedWord(runes);
        int[]? exceptionPoints = null;
        if (exceptions.ContainsKey(normalizedWord))
        {
            exceptionPoints = exceptions[normalizedWord];
        }

        bool[] breaks = new bool[runeCount + 1];
        if (exceptionPoints != null)
        {
            int exceptionIndex = 0;
            while (exceptionIndex < exceptionPoints.Length)
            {
                int point = exceptionPoints[exceptionIndex];
                if (IsWithinMinima(point, runeCount))
                {
                    breaks[point] = true;
                }

                exceptionIndex++;
            }
        }
        else
        {
            MatchPatterns(runes, breaks);
        }

        int resultCount = 0;
        int runeIndex = 1;
        while (runeIndex < runeCount)
        {
            if (breaks[runeIndex])
            {
                resultCount++;
            }

            runeIndex++;
        }

        int[] resultRunes = new int[resultCount];
        int[] resultUtf16 = new int[resultCount];
        int resultIndex = 0;
        runeIndex = 1;
        while (runeIndex < runeCount)
        {
            if (breaks[runeIndex])
            {
                resultRunes[resultIndex] = runeIndex;
                resultUtf16[resultIndex] = runes.GetUtf16Start(runeIndex);
                resultIndex++;
            }

            runeIndex++;
        }

        return new HyphenationMatch(word.Length, resultRunes, resultUtf16);
    }

    /// <summary>Matches a word and returns its allowed internal hyphenation boundaries.</summary>
    /// <param name="word">The word to match.</param>
    /// <returns>Immutable hyphenation opportunities in ascending source order.</returns>
    /// <exception cref="System.ArgumentNullException"><c>word</c> is null.</exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// <c>word</c> contains malformed UTF-16.
    /// </exception>
    /// <exception cref="FontLimitException">The configured word length is exceeded.</exception>
    public HyphenationMatch FindHyphenationPoints(string word)
    {
        return Match(word);
    }

    static void ValidateLimits(HyphenationLimits limits)
    {
        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }
    }

    static HyphenationDictionary Parse(byte[] data, HyphenationLimits limits)
    {
        System.Text.UTF8Encoding encoding =
            new System.Text.UTF8Encoding(false, true);
        string text = encoding.GetString(data);
        HyphenationDictionary dictionary = new HyphenationDictionary(limits);
        using (System.IO.StringReader reader = new System.IO.StringReader(text))
        {
            string? line = reader.ReadLine();
            int lineNumber = 1;
            while (line != null)
            {
                dictionary.ParseLine(line, lineNumber);
                line = reader.ReadLine();
                lineNumber++;
            }
        }

        if (dictionary.blockMode != 0)
        {
            throw new HyphenationFormatException(
                "The hyphenation dictionary contains an unterminated block.");
        }

        return dictionary;
    }

    void ParseLine(string sourceLine, int lineNumber)
    {
        string line = StripComment(sourceLine).Trim();
        if (line.Length != 0 && line[0] == '\uFEFF')
        {
            line = line.Substring(1).Trim();
        }

        if (line.Length == 0)
        {
            return;
        }

        if (blockMode != 0)
        {
            ParseBlockText(line, lineNumber);
            return;
        }

        if (string.Equals(line, "UTF-8", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(line, "UTF8", System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (StartsWithIgnoreCase(line, "\\patterns{"))
        {
            blockMode = 1;
            ParseBlockText(line.Substring(10), lineNumber);
            return;
        }

        if (StartsWithIgnoreCase(line, "\\hyphenation{"))
        {
            blockMode = 2;
            ParseBlockText(line.Substring(13), lineNumber);
            return;
        }

        if (TryParseMinimumAssignment(line, "\\lefthyphenmin=", true, lineNumber) ||
            TryParseMinimumAssignment(line, "\\righthyphenmin=", false, lineNumber) ||
            TryParseMinimumAssignment(line, "lefthyphenmin=", true, lineNumber) ||
            TryParseMinimumAssignment(line, "righthyphenmin=", false, lineNumber))
        {
            return;
        }

        System.Collections.Generic.List<string> tokens = SplitWhitespace(line);
        if (tokens.Count == 0)
        {
            return;
        }

        string first = tokens[0];
        if (IsLeftMinimumDirective(first) || IsRightMinimumDirective(first))
        {
            if (tokens.Count != 2)
            {
                ThrowLineFormat(lineNumber, "A hyphen minimum directive requires one integer.");
            }

            SetMinimum(tokens[1], IsLeftMinimumDirective(first), lineNumber);
            return;
        }

        int startIndex = 0;
        bool isException = false;
        if (string.Equals(first, "pattern", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(first, "patterns", System.StringComparison.OrdinalIgnoreCase))
        {
            startIndex = 1;
        }
        else if (string.Equals(first, "exception", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(first, "exceptions", System.StringComparison.OrdinalIgnoreCase))
        {
            startIndex = 1;
            isException = true;
        }

        if (startIndex == tokens.Count)
        {
            ThrowLineFormat(lineNumber, "A dictionary directive requires at least one entry.");
        }

        ParseTokens(tokens, startIndex, isException, lineNumber);
    }

    void ParseBlockText(string line, int lineNumber)
    {
        int closeIndex = line.IndexOf('}');
        string contents = closeIndex < 0 ? line : line.Substring(0, closeIndex);
        System.Collections.Generic.List<string> tokens = SplitWhitespace(contents);
        ParseTokens(tokens, 0, blockMode == 2, lineNumber);
        if (closeIndex >= 0)
        {
            if (line.Substring(closeIndex + 1).Trim().Length != 0)
            {
                ThrowLineFormat(lineNumber, "Unexpected text follows a dictionary block.");
            }

            blockMode = 0;
        }
    }

    void ParseTokens(
        System.Collections.Generic.List<string> tokens,
        int startIndex,
        bool isException,
        int lineNumber)
    {
        int index = startIndex;
        while (index < tokens.Count)
        {
            if (isException)
            {
                AddException(tokens[index], lineNumber);
            }
            else
            {
                AddPattern(tokens[index], lineNumber);
            }

            index++;
        }
    }

    void AddPattern(string token, int lineNumber)
    {
        if (patternCount == limits.GetMaxPatterns())
        {
            throw FontLimitException.Create(
                "The hyphenation dictionary exceeds the configured pattern limit.");
        }

        RuneBuffer tokenRunes = DecodeEntry(
            token,
            limits.GetMaxPatternLength(),
            "pattern");
        System.Collections.Generic.List<int> key =
            new System.Collections.Generic.List<int>();
        int[] temporaryWeights = new int[tokenRunes.GetCount() + 1];
        bool previousWasDigit = false;
        int runeIndex = 0;
        while (runeIndex < tokenRunes.GetCount())
        {
            int scalar = tokenRunes.GetScalarValue(runeIndex);
            if (scalar >= '0' && scalar <= '9')
            {
                if (previousWasDigit)
                {
                    ThrowLineFormat(lineNumber, "Pattern weights must be single ASCII digits.");
                }

                temporaryWeights[key.Count] = scalar - '0';
                previousWasDigit = true;
            }
            else
            {
                key.Add(NormalizeScalar(scalar));
                previousWasDigit = false;
            }

            runeIndex++;
        }

        if (key.Count == 0 || (key.Count == 1 && key[0] == '.'))
        {
            ThrowLineFormat(lineNumber, "A pattern must contain a non-period character.");
        }

        int periodIndex = 0;
        while (periodIndex < key.Count)
        {
            if (key[periodIndex] == '.' &&
                periodIndex != 0 &&
                periodIndex + 1 != key.Count)
            {
                ThrowLineFormat(lineNumber, "A pattern period must anchor an edge.");
            }

            periodIndex++;
        }

        int[] weights = new int[key.Count + 1];
        System.Array.Copy(temporaryWeights, weights, weights.Length);
        HyphenationNode node = root;
        int keyIndex = 0;
        while (keyIndex < key.Count)
        {
            int scalar = key[keyIndex];
            if (!node.HasChild(scalar))
            {
                if (nodeCount == limits.GetMaxTrieNodes())
                {
                    throw FontLimitException.Create(
                        "The hyphenation dictionary exceeds the configured trie node limit.");
                }

                node.AddChild(scalar, new HyphenationNode());
                nodeCount++;
            }

            node = node.GetChild(scalar);
            keyIndex++;
        }

        node.MergeWeights(weights);
        patternCount++;
    }

    void AddException(string token, int lineNumber)
    {
        if (exceptionCount == limits.GetMaxExceptions())
        {
            throw FontLimitException.Create(
                "The hyphenation dictionary exceeds the configured exception limit.");
        }

        RuneBuffer tokenRunes = DecodeEntry(
            token,
            limits.GetMaxInputBytes(),
            "exception");
        System.Text.StringBuilder word = new System.Text.StringBuilder();
        System.Collections.Generic.List<int> points =
            new System.Collections.Generic.List<int>();
        int wordRuneCount = 0;
        bool previousWasHyphen = true;
        int runeIndex = 0;
        while (runeIndex < tokenRunes.GetCount())
        {
            int scalar = tokenRunes.GetScalarValue(runeIndex);
            if (scalar == '-')
            {
                if (previousWasHyphen)
                {
                    ThrowLineFormat(lineNumber, "An exception contains an empty segment.");
                }

                points.Add(wordRuneCount);
                previousWasHyphen = true;
            }
            else
            {
                AppendScalar(word, NormalizeScalar(scalar));
                wordRuneCount++;
                if (wordRuneCount > limits.GetMaxWordLength())
                {
                    throw FontLimitException.Create(
                        "A hyphenation exception exceeds the configured word length limit.");
                }

                previousWasHyphen = false;
            }

            runeIndex++;
        }

        if (previousWasHyphen || points.Count == 0)
        {
            ThrowLineFormat(lineNumber, "An exception must contain an internal ASCII hyphen.");
        }

        string key = word.ToString();
        if (exceptions.ContainsKey(key))
        {
            ThrowLineFormat(lineNumber, "An exception word is declared more than once.");
        }

        exceptions.Add(key, points.ToArray());
        exceptionCount++;
    }

    RuneBuffer DecodeEntry(string token, int maximumLength, string kind)
    {
        if (token.Length == 0)
        {
            throw new HyphenationFormatException(
                "A hyphenation " + kind + " entry is empty.");
        }

        return Utf16Decoder.Decode(
            token,
            Utf16DecodingMode.Strict,
            maximumLength);
    }

    void MatchPatterns(RuneBuffer runes, bool[] breaks)
    {
        int runeCount = runes.GetCount();
        int[] padded = new int[runeCount + 2];
        padded[0] = '.';
        int index = 0;
        while (index < runeCount)
        {
            padded[index + 1] = NormalizeScalar(runes.GetScalarValue(index));
            index++;
        }

        padded[padded.Length - 1] = '.';
        int[] scores = new int[padded.Length + 1];
        int start = 0;
        while (start < padded.Length)
        {
            HyphenationNode node = root;
            int current = start;
            while (current < padded.Length && node.HasChild(padded[current]))
            {
                node = node.GetChild(padded[current]);
                int[]? weights = node.GetWeights();
                if (weights != null)
                {
                    int weightIndex = 0;
                    while (weightIndex < weights.Length)
                    {
                        int scoreIndex = start + weightIndex;
                        if (scoreIndex < scores.Length &&
                            weights[weightIndex] > scores[scoreIndex])
                        {
                            scores[scoreIndex] = weights[weightIndex];
                        }

                        weightIndex++;
                    }
                }

                current++;
            }

            start++;
        }

        index = 1;
        while (index < runeCount)
        {
            if ((scores[index + 1] & 1) != 0 && IsWithinMinima(index, runeCount))
            {
                breaks[index] = true;
            }

            index++;
        }
    }

    bool IsWithinMinima(int index, int runeCount)
    {
        return index >= leftMinimum && runeCount - index >= rightMinimum;
    }

    void SetMinimum(string value, bool isLeft, int lineNumber)
    {
        if (value.Length == 0)
        {
            ThrowLineFormat(lineNumber, "A hyphen minimum is outside the supported range.");
        }

        int parsed = 0;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                ThrowLineFormat(lineNumber, "A hyphen minimum is outside the supported range.");
            }

            int digit = character - '0';
            int maximum = limits.GetMaxWordLength();
            if (parsed > maximum / 10 ||
                (parsed == maximum / 10 && digit > maximum % 10))
            {
                ThrowLineFormat(lineNumber, "A hyphen minimum is outside the supported range.");
            }

            parsed = parsed * 10 + digit;
            index++;
        }

        if (isLeft)
        {
            leftMinimum = parsed;
        }
        else
        {
            rightMinimum = parsed;
        }
    }

    bool TryParseMinimumAssignment(
        string line,
        string prefix,
        bool isLeft,
        int lineNumber)
    {
        if (!StartsWithIgnoreCase(line, prefix))
        {
            return false;
        }

        SetMinimum(line.Substring(prefix.Length).Trim(), isLeft, lineNumber);
        return true;
    }

    static bool IsLeftMinimumDirective(string value)
    {
        return string.Equals(value, "LEFTHYPHENMIN", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "LEFTMIN", System.StringComparison.OrdinalIgnoreCase);
    }

    static bool IsRightMinimumDirective(string value)
    {
        return string.Equals(value, "RIGHTHYPHENMIN", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "RIGHTMIN", System.StringComparison.OrdinalIgnoreCase);
    }

    static string StripComment(string line)
    {
        int percent = line.IndexOf('%');
        int hash = line.IndexOf('#');
        int index = percent;
        if (index < 0 || (hash >= 0 && hash < index))
        {
            index = hash;
        }

        return index < 0 ? line : line.Substring(0, index);
    }

    static bool StartsWithIgnoreCase(string text, string value)
    {
        return text.StartsWith(value, System.StringComparison.OrdinalIgnoreCase);
    }

    static System.Collections.Generic.List<string> SplitWhitespace(string text)
    {
        System.Collections.Generic.List<string> values =
            new System.Collections.Generic.List<string>();
        int index = 0;
        while (index < text.Length)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            int start = index;
            while (index < text.Length && !char.IsWhiteSpace(text[index]))
            {
                index++;
            }

            if (index > start)
            {
                values.Add(text.Substring(start, index - start));
            }
        }

        return values;
    }

    static string BuildNormalizedWord(RuneBuffer runes)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < runes.GetCount())
        {
            AppendScalar(builder, NormalizeScalar(runes.GetScalarValue(index)));
            index++;
        }

        return builder.ToString();
    }

    static int NormalizeScalar(int scalar)
    {
        System.Text.Rune rune = new System.Text.Rune(scalar);
        return System.Text.Rune.ToLowerInvariant(rune).Value;
    }

    static void AppendScalar(System.Text.StringBuilder builder, int scalar)
    {
        builder.Append(new System.Text.Rune(scalar).ToString());
    }

    static void ThrowLineFormat(int lineNumber, string message)
    {
        throw new HyphenationFormatException(
            "Hyphenation dictionary line " + lineNumber + ": " + message);
    }
}
