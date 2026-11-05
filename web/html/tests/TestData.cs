using System.Globalization;
using System.Text;
using System.Text.Json;

namespace UniversalHtml.Tests;

enum Html5libTokenizerDisposition
{
    Passed = 0,
    Failed = 1,
    Skipped = 2
}

class Html5libTokenizerRun
{
    public string FileName { get; set; }
    public string Description { get; set; }
    public string State { get; set; }
    public Html5libTokenizerDisposition Disposition { get; set; }
    public string Detail { get; set; }
}

class Html5libTokenizerStateSummary
{
    public string State { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }

    public int GetTotal()
    {
        return this.Passed + this.Failed + this.Skipped;
    }
}

class Html5libTokenizerReport
{
    Html5libTokenizerRun[] runs;

    public Html5libTokenizerReport(Html5libTokenizerRun[] runs)
    {
        this.runs = runs;
    }

    public Html5libTokenizerRun[] GetRuns()
    {
        return this.runs.ToArray();
    }

    public Html5libTokenizerStateSummary[] GetStateSummaries()
    {
        return this.runs
            .GroupBy(value => value.State, StringComparer.Ordinal)
            .OrderBy(value => value.Key, StringComparer.Ordinal)
            .Select(group => new Html5libTokenizerStateSummary
            {
                State = group.Key,
                Passed = group.Count(value =>
                    value.Disposition == Html5libTokenizerDisposition.Passed),
                Failed = group.Count(value =>
                    value.Disposition == Html5libTokenizerDisposition.Failed),
                Skipped = group.Count(value =>
                    value.Disposition == Html5libTokenizerDisposition.Skipped)
            })
            .ToArray();
    }

    public string FormatSummary()
    {
        StringBuilder output = new StringBuilder();
        foreach (Html5libTokenizerStateSummary summary in this.GetStateSummaries())
        {
            output.Append(summary.State);
            output.Append(": passed=");
            output.Append(summary.Passed.ToString(CultureInfo.InvariantCulture));
            output.Append(", failed=");
            output.Append(summary.Failed.ToString(CultureInfo.InvariantCulture));
            output.Append(", skipped=");
            output.Append(summary.Skipped.ToString(CultureInfo.InvariantCulture));
            output.AppendLine();
        }

        return output.ToString();
    }
}

class Html5libExpectedToken
{
    public string Kind { get; set; }
    public string Name { get; set; }
    public string Data { get; set; }
    public string PublicIdentifier { get; set; }
    public string SystemIdentifier { get; set; }
    public bool CorrectDoctype { get; set; }
    public bool SelfClosing { get; set; }
    public Dictionary<string, string> Attributes { get; set; }
}

static class Html5libTokenizerAdapter
{
    const string XmlViolationSkip =
        "The fixture requires optional XML-infoset coercion, not HTML tokenization.";

    public static Html5libTokenizerReport RunPinnedSuite()
    {
        string directory = Path.Combine(FindRepositoryRoot(), "tests", "data",
            "html5lib", "tokenizer");
        List<Html5libTokenizerRun> runs = new List<Html5libTokenizerRun>();
        foreach (string path in Directory.GetFiles(directory, "*.test")
            .OrderBy(value => value, StringComparer.Ordinal))
        {
            AddFileRuns(path, runs);
        }

        return new Html5libTokenizerReport(runs.ToArray());
    }

    static void AddFileRuns(string path, List<Html5libTokenizerRun> runs)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement root = document.RootElement;
        JsonElement tests;
        bool xmlViolation = root.TryGetProperty("xmlViolationTests", out tests);
        if (!xmlViolation)
        {
            tests = root.GetProperty("tests");
        }

        foreach (JsonElement test in tests.EnumerateArray())
        {
            bool doubleEscaped = test.TryGetProperty("doubleEscaped", out JsonElement escaped)
                && escaped.GetBoolean();
            string description = ReadString(test.GetProperty("description"), doubleEscaped);
            string input = ReadString(test.GetProperty("input"), doubleEscaped);
            Html5libExpectedToken[] expected = ReadExpectedTokens(
                test.GetProperty("output"), doubleEscaped);
            string lastStartTag = test.TryGetProperty("lastStartTag", out JsonElement last)
                ? ReadString(last, doubleEscaped) : null;
            string[] states = ReadStates(test);
            foreach (string state in states)
            {
                if (xmlViolation)
                {
                    runs.Add(CreateRun(path, description, state,
                        Html5libTokenizerDisposition.Skipped, XmlViolationSkip));
                    continue;
                }

                runs.Add(RunCase(path, description, state, input,
                    lastStartTag, expected));
            }
        }
    }

    static Html5libTokenizerRun RunCase(string path, string description,
        string state, string input, string lastStartTag,
        Html5libExpectedToken[] expected)
    {
        try
        {
            HtmlTokenizerContext tokenizer = new HtmlTokenizerContext(
                HtmlTokenizerOptions.CreateDefault(), null, false);
            tokenizer.SetState(MapState(state));
            if (lastStartTag != null)
            {
                tokenizer.SetLastStartTagName(lastStartTag);
            }

            tokenizer.Feed(input.AsSpan(), true);
            Html5libExpectedToken[] actual = ReadActualTokens(tokenizer);
            string difference = FindDifference(expected, actual);
            return CreateRun(path, description, state,
                difference == null ? Html5libTokenizerDisposition.Passed
                    : Html5libTokenizerDisposition.Failed,
                difference ?? "");
        }
        catch (Exception exception)
        {
            return CreateRun(path, description, state,
                Html5libTokenizerDisposition.Failed,
                exception.GetType().Name + ": " + exception.Message);
        }
    }

    static Html5libExpectedToken[] ReadActualTokens(HtmlTokenizerContext tokenizer)
    {
        List<Html5libExpectedToken> tokens = new List<Html5libExpectedToken>();
        while (true)
        {
            HtmlTokenReadResult result = tokenizer.Read();
            if (result.GetStatus() == HtmlTokenReadStatus.Complete)
            {
                break;
            }

            if (result.GetStatus() != HtmlTokenReadStatus.TokenAvailable)
            {
                throw new InvalidOperationException(
                    "The tokenizer ended with status " + result.GetStatus() + ".");
            }

            HtmlToken token = result.GetToken();
            if (token.GetKind() != HtmlTokenKind.EndOfFile)
            {
                AddActualToken(tokens, token);
            }
        }

        return tokens.ToArray();
    }

    static void AddActualToken(List<Html5libExpectedToken> tokens, HtmlToken token)
    {
        if (token.GetKind() == HtmlTokenKind.Character)
        {
            if (tokens.Count > 0 && tokens[tokens.Count - 1].Kind == "Character")
            {
                tokens[tokens.Count - 1].Data += token.GetData();
                return;
            }

            tokens.Add(new Html5libExpectedToken
            {
                Kind = "Character",
                Data = token.GetData()
            });
            return;
        }

        if (token.GetKind() == HtmlTokenKind.Comment)
        {
            tokens.Add(new Html5libExpectedToken
            {
                Kind = "Comment",
                Data = token.GetData()
            });
            return;
        }

        if (token.GetKind() == HtmlTokenKind.Doctype)
        {
            tokens.Add(new Html5libExpectedToken
            {
                Kind = "DOCTYPE",
                Name = token.HasName() ? token.GetName() : null,
                PublicIdentifier = token.HasPublicIdentifier()
                    ? token.GetPublicIdentifier() : null,
                SystemIdentifier = token.HasSystemIdentifier()
                    ? token.GetSystemIdentifier() : null,
                CorrectDoctype = !token.IsForceQuirks()
            });
            return;
        }

        if (token.GetKind() == HtmlTokenKind.StartTag)
        {
            Dictionary<string, string> attributes = new Dictionary<string, string>(
                StringComparer.Ordinal);
            foreach (HtmlAttribute attribute in token.GetAttributes())
            {
                attributes.Add(attribute.GetName(), attribute.GetValue());
            }

            tokens.Add(new Html5libExpectedToken
            {
                Kind = "StartTag",
                Name = token.GetName(),
                Attributes = attributes,
                SelfClosing = token.IsSelfClosing()
            });
            return;
        }

        tokens.Add(new Html5libExpectedToken
        {
            Kind = "EndTag",
            Name = token.GetName()
        });
    }

    static Html5libExpectedToken[] ReadExpectedTokens(JsonElement output,
        bool doubleEscaped)
    {
        List<Html5libExpectedToken> tokens = new List<Html5libExpectedToken>();
        foreach (JsonElement value in output.EnumerateArray())
        {
            JsonElement.ArrayEnumerator fields = value.EnumerateArray();
            fields.MoveNext();
            string kind = ReadString(fields.Current, doubleEscaped);
            Html5libExpectedToken token = new Html5libExpectedToken { Kind = kind };
            if (kind == "Character" || kind == "Comment")
            {
                fields.MoveNext();
                token.Data = ReadString(fields.Current, doubleEscaped);
            }
            else if (kind == "DOCTYPE")
            {
                fields.MoveNext();
                token.Name = ReadNullableString(fields.Current, doubleEscaped);
                fields.MoveNext();
                token.PublicIdentifier = ReadNullableString(fields.Current, doubleEscaped);
                fields.MoveNext();
                token.SystemIdentifier = ReadNullableString(fields.Current, doubleEscaped);
                fields.MoveNext();
                token.CorrectDoctype = fields.Current.GetBoolean();
            }
            else if (kind == "StartTag")
            {
                fields.MoveNext();
                token.Name = ReadString(fields.Current, doubleEscaped);
                fields.MoveNext();
                token.Attributes = ReadAttributes(fields.Current, doubleEscaped);
                token.SelfClosing = fields.MoveNext() && fields.Current.GetBoolean();
            }
            else
            {
                fields.MoveNext();
                token.Name = ReadString(fields.Current, doubleEscaped);
            }

            tokens.Add(token);
        }

        return tokens.ToArray();
    }

    static Dictionary<string, string> ReadAttributes(JsonElement value,
        bool doubleEscaped)
    {
        Dictionary<string, string> attributes = new Dictionary<string, string>(
            StringComparer.Ordinal);
        foreach (JsonProperty property in value.EnumerateObject())
        {
            string name = doubleEscaped ? DecodeDoubleEscaped(property.Name) : property.Name;
            attributes.Add(name, ReadString(property.Value, doubleEscaped));
        }

        return attributes;
    }

    static string FindDifference(Html5libExpectedToken[] expected,
        Html5libExpectedToken[] actual)
    {
        if (expected.Length != actual.Length)
        {
            return "Expected " + expected.Length.ToString(CultureInfo.InvariantCulture)
                + " tokens but received "
                + actual.Length.ToString(CultureInfo.InvariantCulture) + ".";
        }

        int index = 0;
        while (index < expected.Length)
        {
            if (!TokensEqual(expected[index], actual[index]))
            {
                return "Token " + index.ToString(CultureInfo.InvariantCulture)
                    + " differs: expected " + FormatToken(expected[index])
                    + ", received " + FormatToken(actual[index]) + ".";
            }

            index++;
        }

        return null;
    }

    static bool TokensEqual(Html5libExpectedToken expected,
        Html5libExpectedToken actual)
    {
        if (expected.Kind != actual.Kind || expected.Name != actual.Name
            || expected.Data != actual.Data
            || expected.PublicIdentifier != actual.PublicIdentifier
            || expected.SystemIdentifier != actual.SystemIdentifier
            || expected.CorrectDoctype != actual.CorrectDoctype
            || expected.SelfClosing != actual.SelfClosing)
        {
            return false;
        }

        if (expected.Attributes == null || actual.Attributes == null)
        {
            return expected.Attributes == actual.Attributes;
        }

        return expected.Attributes.Count == actual.Attributes.Count
            && expected.Attributes.All(value => actual.Attributes.TryGetValue(
                value.Key, out string found) && found == value.Value);
    }

    static string FormatToken(Html5libExpectedToken token)
    {
        if (token.Kind == "Character" || token.Kind == "Comment")
        {
            return token.Kind + " " + JsonSerializer.Serialize(token.Data);
        }

        if (token.Kind == "DOCTYPE")
        {
            return token.Kind + " " + JsonSerializer.Serialize(new object[]
            {
                token.Name, token.PublicIdentifier, token.SystemIdentifier,
                token.CorrectDoctype
            });
        }

        if (token.Kind == "StartTag")
        {
            string attributes = string.Join(",", token.Attributes
                .OrderBy(value => value.Key, StringComparer.Ordinal)
                .Select(value => JsonSerializer.Serialize(value.Key)
                    + ":" + JsonSerializer.Serialize(value.Value)));
            return token.Kind + " " + JsonSerializer.Serialize(token.Name)
                + " {" + attributes + "} selfClosing=" + token.SelfClosing;
        }

        return token.Kind + " " + JsonSerializer.Serialize(token.Name);
    }

    static string[] ReadStates(JsonElement test)
    {
        if (!test.TryGetProperty("initialStates", out JsonElement states))
        {
            return new[] { "Data state" };
        }

        return states.EnumerateArray().Select(value => value.GetString()).ToArray();
    }

    static HtmlTokenizerState MapState(string state)
    {
        switch (state)
        {
            case "Data state":
                return HtmlTokenizerState.Data;
            case "PLAINTEXT state":
                return HtmlTokenizerState.PlainText;
            case "RCDATA state":
                return HtmlTokenizerState.Rcdata;
            case "RAWTEXT state":
                return HtmlTokenizerState.RawText;
            case "Script data state":
                return HtmlTokenizerState.ScriptData;
            case "CDATA section state":
                return HtmlTokenizerState.CdataSection;
            default:
                throw new InvalidDataException("Unknown html5lib tokenizer state " + state + ".");
        }
    }

    static Html5libTokenizerRun CreateRun(string path, string description,
        string state, Html5libTokenizerDisposition disposition, string detail)
    {
        return new Html5libTokenizerRun
        {
            FileName = Path.GetFileName(path),
            Description = description,
            State = state,
            Disposition = disposition,
            Detail = detail
        };
    }

    static string ReadNullableString(JsonElement value, bool doubleEscaped)
    {
        return value.ValueKind == JsonValueKind.Null
            ? null : ReadString(value, doubleEscaped);
    }

    static string ReadString(JsonElement value, bool doubleEscaped)
    {
        string result = value.GetString();
        return doubleEscaped ? DecodeDoubleEscaped(result) : result;
    }

    static string DecodeDoubleEscaped(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (index + 5 < value.Length && value[index] == '\\'
                && value[index + 1] == 'u'
                && IsHex(value[index + 2]) && IsHex(value[index + 3])
                && IsHex(value[index + 4]) && IsHex(value[index + 5]))
            {
                int scalar = int.Parse(value.Substring(index + 2, 4),
                    NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
                result.Append((char)scalar);
                index += 6;
            }
            else
            {
                result.Append(value[index]);
                index++;
            }
        }

        return result.ToString();
    }

    static bool IsHex(char value)
    {
        return value >= '0' && value <= '9'
            || value >= 'A' && value <= 'F'
            || value >= 'a' && value <= 'f';
    }

    static string FindRepositoryRoot()
    {
        DirectoryInfo directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "tests", "data",
                "html5lib", "tokenizer", "test1.test")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find the pinned html5lib tokenizer fixtures.");
    }
}
