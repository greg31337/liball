namespace barcode.Tests;

public class Gs1SyntaxDictionaryCorpusTests
{
    class CorpusCase
    {
        internal string LinterName = "";
        internal string Data = "";
        internal bool Success;
        internal string Code = "";
        internal int ErrorOffset;
        internal int ErrorLength;
        internal string Highlight = "";
        internal string Source = "";
    }

    [Test]
    public void EveryLiteralReferenceLinterVectorMatchesTheManagedImplementation()
    {
        global::System.Collections.Generic.List<CorpusCase> cases =
            ReadLiteralCorpusCases();
        global::System.Collections.Generic.List<string> failures =
            new global::System.Collections.Generic.List<string>();

        foreach (CorpusCase testCase in cases)
        {
            Gs1LinterResult result = Gs1ComponentLinters.Lint(
                testCase.LinterName,
                testCase.Data);
            if (result.Success != testCase.Success
                || result.Code != testCase.Code
                || (!testCase.Success
                    && CreateHighlight(
                        testCase.Data,
                        result.ErrorOffset,
                        result.ErrorLength) != testCase.Highlight))
            {
                failures.Add(
                    $"{testCase.Source}: {testCase.LinterName}({Describe(testCase.Data)}) "
                    + $"expected {testCase.Code} [{testCase.ErrorOffset},{testCase.ErrorLength}], "
                    + $"observed {result.Code} [{result.ErrorOffset},{result.ErrorLength}]");
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(cases, Has.Count.EqualTo(4766));
            Assert.That(failures, Is.Empty);
        });
    }

    [Test]
    public void LoopGeneratedGcpPrefixVectorsMatchTheReferenceCorpus()
    {
        global::System.Collections.Generic.List<CorpusCase> cases =
            CreateGcpPrefixCases();
        global::System.Collections.Generic.List<string> failures =
            new global::System.Collections.Generic.List<string>();

        foreach (CorpusCase testCase in cases)
        {
            Gs1LinterResult result = Gs1ComponentLinters.Lint(
                testCase.LinterName,
                testCase.Data);
            if (result.Success != testCase.Success
                || result.Code != testCase.Code
                || (!testCase.Success
                    && (result.ErrorOffset != testCase.ErrorOffset
                        || result.ErrorLength != testCase.ErrorLength)))
            {
                failures.Add(
                    $"{testCase.LinterName}({Describe(testCase.Data)}) "
                    + $"expected {testCase.Code} [{testCase.ErrorOffset},{testCase.ErrorLength}], "
                    + $"observed {result.Code} [{result.ErrorOffset},{result.ErrorLength}]");
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(cases, Has.Count.EqualTo(23));
            Assert.That(failures, Is.Empty);
        });
    }

    static global::System.Collections.Generic.List<CorpusCase>
        ReadLiteralCorpusCases()
    {
        global::System.Collections.Generic.List<CorpusCase> cases =
            new global::System.Collections.Generic.List<CorpusCase>();
        string archivePath = FindCorpusArchive();
        using global::System.IO.Compression.ZipArchive archive =
            global::System.IO.Compression.ZipFile.OpenRead(archivePath);
        foreach (global::System.IO.Compression.ZipArchiveEntry entry
            in archive.Entries)
        {
            string name = ReadLinterName(entry.FullName);
            if (name.Length == 0
                || !Gs1ComponentLinters.SupportsLinter(name))
            {
                continue;
            }

            using global::System.IO.StreamReader reader =
                new global::System.IO.StreamReader(entry.Open());
            string source = reader.ReadToEnd();
            source = global::System.Text.RegularExpressions.Regex.Replace(
                source,
                "#ifdef GS1_LINTER_CUSTOM_GCP_LOOKUP.*?#endif",
                "",
                global::System.Text.RegularExpressions.RegexOptions.Singleline);
            ReadCalls(cases, name, entry.Name, source);
        }

        return cases;
    }

    static string FindCorpusArchive()
    {
        global::System.IO.DirectoryInfo? directory =
            new global::System.IO.DirectoryInfo(
                TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            string candidate = global::System.IO.Path.Combine(
                directory.FullName,
                "specs",
                "GS1-Syntax-Dictionary-and-Tests.zip");
            if (global::System.IO.File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new global::System.IO.FileNotFoundException(
            "The checked-in GS1 Syntax Dictionary corpus was not found.");
    }

    static string ReadLinterName(string entryName)
    {
        const string prefix = "gs1-syntax-dictionary-main/src/lint_";
        if (!entryName.StartsWith(prefix, global::System.StringComparison.Ordinal)
            || !entryName.EndsWith(".c", global::System.StringComparison.Ordinal))
        {
            return "";
        }

        return entryName.Substring(
            prefix.Length,
            entryName.Length - prefix.Length - 2);
    }

    static void ReadCalls(
        global::System.Collections.Generic.List<CorpusCase> cases,
        string linterName,
        string sourceName,
        string source)
    {
        int searchIndex = 0;
        while (searchIndex < source.Length)
        {
            int passIndex = source.IndexOf(
                "UNIT_TEST_PASS",
                searchIndex,
                global::System.StringComparison.Ordinal);
            int failIndex = source.IndexOf(
                "UNIT_TEST_FAIL",
                searchIndex,
                global::System.StringComparison.Ordinal);
            int callIndex = FirstCallIndex(passIndex, failIndex);
            if (callIndex < 0)
            {
                return;
            }

            bool success = callIndex == passIndex;
            int openIndex = source.IndexOf('(', callIndex);
            int closeIndex = FindClosingParenthesis(source, openIndex);
            string arguments = source.Substring(
                openIndex + 1,
                closeIndex - openIndex - 1);
            string[] values = SplitArguments(arguments);
            string data;
            if ((success && values.Length == 2
                    || !success && values.Length == 4)
                && TryDecodeStringArgument(values[1], out data))
            {
                CorpusCase testCase = new CorpusCase();
                testCase.LinterName = linterName;
                testCase.Data = data;
                testCase.Success = success;
                testCase.Code = success
                    ? "GS1_LINTER_OK"
                    : values[2].Trim();
                testCase.Source = sourceName;
                if (!success)
                {
                    string highlight;
                    if (!TryDecodeStringArgument(values[3], out highlight)
                        || !TryLocateHighlight(
                            data,
                            highlight,
                            out testCase.ErrorOffset,
                            out testCase.ErrorLength))
                    {
                        throw new global::System.InvalidOperationException(
                            $"Unable to parse corpus highlight in {sourceName}.");
                    }

                    testCase.Highlight = highlight;
                }

                cases.Add(testCase);
            }

            searchIndex = closeIndex + 1;
        }
    }

    static int FirstCallIndex(int passIndex, int failIndex)
    {
        if (passIndex < 0)
        {
            return failIndex;
        }

        if (failIndex < 0)
        {
            return passIndex;
        }

        return passIndex < failIndex ? passIndex : failIndex;
    }

    static int FindClosingParenthesis(string source, int openIndex)
    {
        int depth = 1;
        bool inString = false;
        bool escaped = false;
        int index = openIndex + 1;
        while (index < source.Length)
        {
            char character = source[index];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    inString = false;
                }
            }
            else if (character == '"')
            {
                inString = true;
            }
            else if (character == '(')
            {
                depth++;
            }
            else if (character == ')')
            {
                depth--;
                if (depth == 0)
                {
                    return index;
                }
            }

            index++;
        }

        throw new global::System.InvalidOperationException(
            "Unterminated linter corpus invocation.");
    }

    static string[] SplitArguments(string arguments)
    {
        global::System.Collections.Generic.List<string> values =
            new global::System.Collections.Generic.List<string>();
        int start = 0;
        int depth = 0;
        bool inString = false;
        bool escaped = false;
        int index = 0;
        while (index < arguments.Length)
        {
            char character = arguments[index];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    inString = false;
                }
            }
            else if (character == '"')
            {
                inString = true;
            }
            else if (character == '(')
            {
                depth++;
            }
            else if (character == ')')
            {
                depth--;
            }
            else if (character == ',' && depth == 0)
            {
                values.Add(arguments.Substring(start, index - start).Trim());
                start = index + 1;
            }

            index++;
        }

        values.Add(arguments.Substring(start).Trim());
        return values.ToArray();
    }

    static bool TryDecodeStringArgument(string argument, out string value)
    {
        global::System.Text.StringBuilder decoded =
            new global::System.Text.StringBuilder();
        int index = 0;
        bool found = false;
        while (index < argument.Length)
        {
            while (index < argument.Length
                && char.IsWhiteSpace(argument[index]))
            {
                index++;
            }

            if (index >= argument.Length)
            {
                break;
            }

            if (argument[index] != '"')
            {
                value = "";
                return false;
            }

            found = true;
            index++;
            while (index < argument.Length && argument[index] != '"')
            {
                char character = argument[index++];
                if (character != '\\')
                {
                    decoded.Append(character);
                    continue;
                }

                if (index >= argument.Length)
                {
                    value = "";
                    return false;
                }

                char escape = argument[index++];
                if (escape == 'x')
                {
                    int numeric = 0;
                    int digits = 0;
                    while (index < argument.Length
                        && digits < 2
                        && IsHexDigit(argument[index]))
                    {
                        numeric = numeric * 16 + HexValue(argument[index]);
                        index++;
                        digits++;
                    }

                    if (digits == 0)
                    {
                        value = "";
                        return false;
                    }

                    decoded.Append((char)numeric);
                }
                else
                {
                    decoded.Append(DecodeSimpleEscape(escape));
                }
            }

            if (index >= argument.Length || argument[index] != '"')
            {
                value = "";
                return false;
            }

            index++;
        }

        value = decoded.ToString();
        return found;
    }

    static char DecodeSimpleEscape(char escape)
    {
        switch (escape)
        {
            case 'n':
                return '\n';
            case 'r':
                return '\r';
            case 't':
                return '\t';
            default:
                return escape;
        }
    }

    static bool IsHexDigit(char character)
    {
        return character >= '0' && character <= '9'
            || character >= 'A' && character <= 'F'
            || character >= 'a' && character <= 'f';
    }

    static int HexValue(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return character - '0';
        }

        if (character >= 'A' && character <= 'F')
        {
            return character - 'A' + 10;
        }

        return character - 'a' + 10;
    }

    static bool TryLocateHighlight(
        string data,
        string highlight,
        out int offset,
        out int length)
    {
        int candidateOffset = 0;
        while (candidateOffset <= data.Length)
        {
            int candidateLength = 0;
            while (candidateOffset + candidateLength <= data.Length)
            {
                string candidate = data.Substring(0, candidateOffset)
                    + "*"
                    + data.Substring(candidateOffset, candidateLength)
                    + "*"
                    + data.Substring(candidateOffset + candidateLength);
                if (candidate == highlight)
                {
                    offset = candidateOffset;
                    length = candidateLength;
                    return true;
                }

                candidateLength++;
            }

            candidateOffset++;
        }

        offset = 0;
        length = 0;
        return false;
    }

    static string CreateHighlight(string data, int offset, int length)
    {
        if (offset < 0
            || length < 0
            || offset > data.Length
            || offset + length > data.Length)
        {
            return "<invalid span>";
        }

        return data.Substring(0, offset)
            + "*"
            + data.Substring(offset, length)
            + "*"
            + data.Substring(offset + length);
    }

    static global::System.Collections.Generic.List<CorpusCase>
        CreateGcpPrefixCases()
    {
        global::System.Collections.Generic.List<CorpusCase> cases =
            new global::System.Collections.Generic.List<CorpusCase>();
        AddSuccess(cases, "gcppos1", "01234");
        AddSuccess(cases, "gcppos1", "0123");
        int length = 3;
        while (length >= 0)
        {
            AddFailure(
                cases,
                "gcppos1",
                "012".Substring(0, length),
                "GS1_LINTER_TOO_SHORT_FOR_GCP",
                0,
                length);
            length--;
        }

        AddSuccess(cases, "gcppos1", "0123A");
        int invalidOffset = 3;
        while (invalidOffset >= 0)
        {
            string data = "0123A";
            char[] characters = data.ToCharArray();
            int index = invalidOffset;
            while (index < 4)
            {
                characters[index] = 'A';
                index++;
            }

            AddFailure(
                cases,
                "gcppos1",
                new string(characters),
                "GS1_LINTER_INVALID_GCP_PREFIX",
                invalidOffset,
                1);
            invalidOffset--;
        }

        AddSuccess(cases, "gcppos2", "I01234");
        AddSuccess(cases, "gcppos2", "I0123");
        length = 3;
        while (length >= 1)
        {
            AddFailure(
                cases,
                "gcppos2",
                "I" + "012".Substring(0, length),
                "GS1_LINTER_TOO_SHORT_FOR_GCP",
                1,
                length);
            length--;
        }

        AddFailure(
            cases,
            "gcppos2",
            "I",
            "GS1_LINTER_TOO_SHORT_FOR_GCP",
            0,
            1);
        AddFailure(
            cases,
            "gcppos2",
            "",
            "GS1_LINTER_TOO_SHORT_FOR_GCP",
            0,
            0);
        AddSuccess(cases, "gcppos2", "I0123A");
        invalidOffset = 4;
        while (invalidOffset >= 1)
        {
            char[] characters = "I0123A".ToCharArray();
            int index = invalidOffset;
            while (index <= 4)
            {
                characters[index] = 'A';
                index++;
            }

            AddFailure(
                cases,
                "gcppos2",
                new string(characters),
                "GS1_LINTER_INVALID_GCP_PREFIX",
                invalidOffset,
                1);
            invalidOffset--;
        }

        return cases;
    }

    static void AddSuccess(
        global::System.Collections.Generic.List<CorpusCase> cases,
        string name,
        string data)
    {
        CorpusCase testCase = new CorpusCase();
        testCase.LinterName = name;
        testCase.Data = data;
        testCase.Success = true;
        testCase.Code = "GS1_LINTER_OK";
        cases.Add(testCase);
    }

    static void AddFailure(
        global::System.Collections.Generic.List<CorpusCase> cases,
        string name,
        string data,
        string code,
        int offset,
        int length)
    {
        CorpusCase testCase = new CorpusCase();
        testCase.LinterName = name;
        testCase.Data = data;
        testCase.Code = code;
        testCase.ErrorOffset = offset;
        testCase.ErrorLength = length;
        cases.Add(testCase);
    }

    static string Describe(string value)
    {
        if (value.Length == 0)
        {
            return "<empty>";
        }

        return value.Replace("\r", "\\r").Replace("\n", "\\n");
    }
}
