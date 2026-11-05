using System.Collections.Generic;
using System.IO;

namespace csv;

/// <summary>
/// Infers likely CSV syntax, headers, and column types from bounded samples.
/// </summary>
internal static class CsvSniffer
{
    /// <summary>
    /// Examines a bounded sample for delimiter, quote, escape, newline, and comment rules.
    /// </summary>
    /// <param name="sample">The bounded sample; null is treated as empty text.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns>A detected dialect, or a clone of the enabled override dialect.</returns>
    internal static CsvDialect Sniff(string? sample, CsvSnifferOptions? options)
    {
        CsvSnifferOptions snifferOptions = ResolveOptions(options);
        string text = LimitSample(sample, snifferOptions);
        if (snifferOptions.HasOverrideDialect)
        {
            CsvDialect overrideDialect = snifferOptions.OverrideDialect.Clone();
            return overrideDialect;
        }

        CsvDialect dialect = CsvDialect.CreateDefault();
        string[] lines = CreateSampleLines(text, snifferOptions);
        dialect.Quote = DetectQuote(text, snifferOptions);
        ApplyEscape(dialect, text);
        dialect.Delimiter = DetectDelimiter(lines, snifferOptions, dialect.Quote);
        ApplyNewLine(dialect, text);
        ApplyComment(dialect, lines, snifferOptions);
        return dialect;
    }

    /// <summary>
    /// Compares sampled rows to decide whether the first appears to contain names.
    /// </summary>
    /// <param name="sample">The bounded sample; null is treated as empty text.</param>
    /// <param name="dialect">The dialect used to parse the sample.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns><c>true</c> when the first sampled row looks like names rather than data; otherwise <c>false</c>.</returns>
    internal static bool DetectHeader(string? sample, CsvDialect dialect, CsvSnifferOptions? options)
    {
        CsvSnifferOptions snifferOptions = ResolveOptions(options);
        if (!snifferOptions.DetectHeader)
        {
            return false;
        }

        string text = LimitSample(sample, snifferOptions);
        CsvReaderOptions readerOptions = CsvReaderOptions.CreateDefault();
        readerOptions.Dialect = dialect.Clone();
        readerOptions.SkipBlankLines = true;
        readerOptions.AllowComments = dialect.HasComment;
        readerOptions.ErrorMode = CsvErrorMode.Relaxed;
        CsvReader reader = CsvReader.Create(new StringReader(text), readerOptions);
        bool hasFirst = reader.Read();
        if (!hasFirst)
        {
            return false;
        }

        CsvRow first = reader.CurrentRow.Clone();
        bool hasSecond = reader.Read();
        if (!hasSecond)
        {
            return false;
        }

        CsvRow second = reader.CurrentRow.Clone();
        bool detected = RowsLookLikeHeader(first, second);
        return detected;
    }

    /// <summary>
    /// Infers column metadata from a sampled table.
    /// </summary>
    /// <param name="sample">The bounded sample.</param>
    /// <param name="options">Operation options; null uses defaults where supported.</param>
    /// <returns>The schema inferred from <c>sample</c>.</returns>
    internal static CsvSchema DetectTypes(CsvTable sample, CsvInferenceOptions? options)
    {
        CsvSchema schema = CsvSchema.InferFromSample(sample, options);
        return schema;
    }

    internal static CsvSnifferOptions ResolveOptions(CsvSnifferOptions? options)
    {
        if (options == null)
        {
            CsvSnifferOptions defaultOptions = CsvSnifferOptions.CreateDefault();
            return defaultOptions;
        }

        return options;
    }

    internal static string LimitSample(string? sample, CsvSnifferOptions options)
    {
        if (sample == null)
        {
            return "";
        }

        string limited = sample;
        if (options.MaxSampleCharacters > 0 && sample.Length > options.MaxSampleCharacters)
        {
            limited = sample.Substring(0, options.MaxSampleCharacters);
        }

        string bounded = LimitSampleRows(limited, options);
        return bounded;
    }

    internal static string LimitSampleRows(string sample, CsvSnifferOptions options)
    {
        if (options.MaxSampleRows <= 0)
        {
            return sample;
        }

        string limited = "";
        int rowCount = 0;
        int index = 0;
        while (index < sample.Length)
        {
            char character = sample[index];
            limited = limited + character;
            if (character == '\r')
            {
                if (index + 1 < sample.Length && sample[index + 1] == '\n')
                {
                    index = index + 1;
                    limited = limited + sample[index];
                }

                rowCount = rowCount + 1;
            }
            else if (character == '\n')
            {
                rowCount = rowCount + 1;
            }

            if (rowCount >= options.MaxSampleRows)
            {
                return limited;
            }

            index = index + 1;
        }

        return limited;
    }

    internal static string[] CreateSampleLines(string sample, CsvSnifferOptions options)
    {
        List<string> lines = new List<string>();
        string current = "";
        int index = 0;
        while (index < sample.Length && ShouldReadMoreLines(lines.Count, options))
        {
            char character = sample[index];
            if (character == '\r')
            {
                lines.Add(current);
                current = "";
                if (index + 1 < sample.Length && sample[index + 1] == '\n')
                {
                    index = index + 1;
                }
            }
            else if (character == '\n')
            {
                lines.Add(current);
                current = "";
            }
            else
            {
                current = current + character;
            }

            index = index + 1;
        }

        if (current.Length > 0 && ShouldReadMoreLines(lines.Count, options))
        {
            lines.Add(current);
        }

        return lines.ToArray();
    }

    internal static bool ShouldReadMoreLines(int count, CsvSnifferOptions options)
    {
        if (options.MaxSampleRows <= 0)
        {
            return true;
        }

        if (count < options.MaxSampleRows)
        {
            return true;
        }

        return false;
    }

    internal static char DetectQuote(string sample, CsvSnifferOptions options)
    {
        char bestQuote = '"';
        int bestScore = -1;
        int index = 0;
        while (index < options.CandidateQuotes.Length)
        {
            char quote = options.CandidateQuotes[index];
            int score = CountCharacter(sample, quote);
            if (score > bestScore)
            {
                bestScore = score;
                bestQuote = quote;
            }

            index = index + 1;
        }

        return bestQuote;
    }

    internal static void ApplyEscape(CsvDialect dialect, string sample)
    {
        if (ContainsEscapedQuote(sample, '\\', dialect.Quote))
        {
            dialect.Escape = '\\';
            dialect.UseDoubleQuoteEscaping = false;
            return;
        }

        if (ContainsDoubledQuote(sample, dialect.Quote))
        {
            dialect.Escape = dialect.Quote;
            dialect.UseDoubleQuoteEscaping = true;
            return;
        }

        dialect.Escape = dialect.Quote;
        dialect.UseDoubleQuoteEscaping = true;
    }

    internal static char DetectDelimiter(string[] lines, CsvSnifferOptions options, char quote)
    {
        char bestDelimiter = ',';
        int bestScore = -1;
        int index = 0;
        while (index < options.CandidateDelimiters.Length)
        {
            char delimiter = options.CandidateDelimiters[index];
            int score = ScoreDelimiter(lines, delimiter, quote);
            if (score > bestScore)
            {
                bestScore = score;
                bestDelimiter = delimiter;
            }

            index = index + 1;
        }

        return bestDelimiter;
    }

    internal static int ScoreDelimiter(string[] lines, char delimiter, char quote)
    {
        long score = 0;
        int expected = -1;
        int index = 0;
        while (index < lines.Length)
        {
            string line = lines[index];
            if (line.Trim().Length > 0)
            {
                int count = CountDelimiterOutsideQuotes(line, delimiter, quote);
                if (count > 0)
                {
                    if (expected < 0)
                    {
                        expected = count;
                    }

                    if (count == expected)
                    {
                        score = score + 1000;
                    }
                    else
                    {
                        score = score - 100;
                        score = score - Absolute(count - expected);
                    }

                    score = score + count;
                }
            }

            index = index + 1;
        }

        return ClampScore(score);
    }

    internal static int CountDelimiterOutsideQuotes(string line, char delimiter, char quote)
    {
        bool inQuote = false;
        int count = 0;
        int index = 0;
        while (index < line.Length)
        {
            char character = line[index];
            if (character == quote)
            {
                if (inQuote && index + 1 < line.Length && line[index + 1] == quote)
                {
                    index = index + 1;
                }
                else
                {
                    inQuote = !inQuote;
                }
            }
            else if (!inQuote && character == delimiter)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static void ApplyNewLine(CsvDialect dialect, string sample)
    {
        int crlf = CountCrlf(sample);
        int lf = CountBareLf(sample);
        int cr = CountBareCr(sample);
        if (crlf >= lf && crlf >= cr && crlf > 0)
        {
            dialect.LineTerminator = "\r\n";
            dialect.NewLineMode = CsvNewLineMode.Crlf;
            return;
        }

        if (lf >= cr && lf > 0)
        {
            dialect.LineTerminator = "\n";
            dialect.NewLineMode = CsvNewLineMode.Lf;
            return;
        }

        if (cr > 0)
        {
            dialect.LineTerminator = "\r";
            dialect.NewLineMode = CsvNewLineMode.Cr;
        }
    }

    internal static void ApplyComment(CsvDialect dialect, string[] lines, CsvSnifferOptions options)
    {
        if (!options.DetectComments)
        {
            dialect.HasComment = false;
            dialect.Comment = '\0';
            return;
        }

        char bestComment = '\0';
        int bestScore = 0;
        int index = 0;
        while (index < options.CandidateComments.Length)
        {
            char comment = options.CandidateComments[index];
            int score = CountCommentLines(lines, comment);
            if (score > bestScore)
            {
                bestScore = score;
                bestComment = comment;
            }

            index = index + 1;
        }

        if (bestScore > 0)
        {
            dialect.HasComment = true;
            dialect.Comment = bestComment;
        }
        else
        {
            dialect.HasComment = false;
            dialect.Comment = '\0';
        }
    }

    internal static bool RowsLookLikeHeader(CsvRow first, CsvRow second)
    {
        int count = first.CountFields();
        if (second.CountFields() < count)
        {
            count = second.CountFields();
        }

        int headerEvidence = 0;
        int dataEvidence = 0;
        int index = 0;
        while (index < count)
        {
            string? firstField = first.GetField(index);
            string? secondField = second.GetField(index);
            string firstValue = "";
            string secondValue = "";
            if (firstField != null)
            {
                firstValue = firstField.Trim();
            }

            if (secondField != null)
            {
                secondValue = secondField.Trim();
            }
            bool firstTyped = LooksTyped(firstValue);
            bool secondTyped = LooksTyped(secondValue);
            if (!firstTyped && secondTyped && firstValue.Length > 0)
            {
                headerEvidence = headerEvidence + 1;
            }

            if (firstTyped && secondTyped)
            {
                dataEvidence = dataEvidence + 1;
            }

            index = index + 1;
        }

        if (headerEvidence > 0 && headerEvidence >= dataEvidence)
        {
            return true;
        }

        return false;
    }

    internal static bool LooksTyped(string value)
    {
        CsvConversionOptions options = CsvConversionOptions.CreateDefault();
        if (CsvColumnInference.IsBooleanText(value))
        {
            return true;
        }

        if (CsvColumnInference.IsInt64Text(value, options))
        {
            return true;
        }

        if (CsvColumnInference.IsDecimalText(value, options))
        {
            return true;
        }

        if (CsvColumnInference.IsDoubleText(value, options))
        {
            return true;
        }

        if (CsvColumnInference.IsDateOnlyText(value, options))
        {
            return true;
        }

        if (CsvColumnInference.IsTimeOnlyText(value, options))
        {
            return true;
        }

        if (CsvColumnInference.IsDateTimeText(value, options))
        {
            return true;
        }

        if (CsvConverter.IsGuidText(value))
        {
            return true;
        }

        return false;
    }

    internal static int CountCharacter(string sample, char target)
    {
        int count = 0;
        int index = 0;
        while (index < sample.Length)
        {
            if (sample[index] == target)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static bool ContainsDoubledQuote(string sample, char quote)
    {
        int index = 0;
        while (index + 1 < sample.Length)
        {
            if (sample[index] == quote && sample[index + 1] == quote)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool ContainsEscapedQuote(string sample, char escape, char quote)
    {
        int index = 0;
        while (index + 1 < sample.Length)
        {
            if (sample[index] == escape && sample[index + 1] == quote)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int CountCrlf(string sample)
    {
        int count = 0;
        int index = 0;
        while (index + 1 < sample.Length)
        {
            if (sample[index] == '\r' && sample[index + 1] == '\n')
            {
                count = count + 1;
                index = index + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static int CountBareLf(string sample)
    {
        int count = 0;
        int index = 0;
        while (index < sample.Length)
        {
            if (sample[index] == '\n')
            {
                if (index == 0 || sample[index - 1] != '\r')
                {
                    count = count + 1;
                }
            }

            index = index + 1;
        }

        return count;
    }

    internal static int CountBareCr(string sample)
    {
        int count = 0;
        int index = 0;
        while (index < sample.Length)
        {
            if (sample[index] == '\r')
            {
                if (index + 1 >= sample.Length || sample[index + 1] != '\n')
                {
                    count = count + 1;
                }
            }

            index = index + 1;
        }

        return count;
    }

    internal static int CountCommentLines(string[] lines, char comment)
    {
        int count = 0;
        int index = 0;
        while (index < lines.Length)
        {
            string trimmed = lines[index].TrimStart();
            if (trimmed.Length > 0 && trimmed[0] == comment)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static int ClampScore(long score)
    {
        if (score > 2147483647)
        {
            return 2147483647;
        }

        if (score < -2147483648)
        {
            return -2147483648;
        }

        return (int)score;
    }

    internal static long Absolute(int value)
    {
        long absolute = value;
        if (absolute < 0)
        {
            return 0 - absolute;
        }

        return absolute;
    }
}
