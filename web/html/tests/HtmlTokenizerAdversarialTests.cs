using System.Text;
using System.Text.Json;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTokenizerAdversarialTests
{
    [Test]
    public void OneCharacterChunksMatchOneShotAcrossTokenizerStateFamilies()
    {
        AdversarialCase[] cases =
        [
            new AdversarialCase(HtmlTokenizerState.Data, null,
                "a\r\n&amp;\U0001F600<!--x--><p A='&copy;'>z"),
            new AdversarialCase(HtmlTokenizerState.Rcdata, "title",
                "a&amp;\r</TiTlE>z"),
            new AdversarialCase(HtmlTokenizerState.RawText, "style",
                "a&amp;\r</StYlE>z"),
            new AdversarialCase(HtmlTokenizerState.PlainText, null,
                "a&amp;<b>\r\U0001F600"),
            new AdversarialCase(HtmlTokenizerState.ScriptData, "script",
                "<!--<script>x</script>--></script>tail"),
            new AdversarialCase(HtmlTokenizerState.Data, null,
                "<![CDATA[a]b]]c]]>", true)
        ];

        foreach (AdversarialCase value in cases)
        {
            TokenizerSnapshot expected = Tokenize(value, new[] { value.Input.Length });
            int[] chunks = Enumerable.Repeat(1, value.Input.Length).ToArray();
            TokenizerSnapshot actual = Tokenize(value, chunks);

            AssertSnapshot(actual, expected, value.Describe());
        }
    }

    [Test]
    public void EverySplitPositionMatchesOneShotForMalformedStateTransitions()
    {
        AdversarialCase[] cases =
        [
            new AdversarialCase(HtmlTokenizerState.Data, null,
                "<!--<!--x--!>tail"),
            new AdversarialCase(HtmlTokenizerState.Data, null,
                "<!DOCTYPE html PUBLIC \"id\" 'unterminated"),
            new AdversarialCase(HtmlTokenizerState.Data, null,
                "<p a='&notit;' b=&#x80 c=/>tail"),
            new AdversarialCase(HtmlTokenizerState.ScriptData, "script",
                "<!--<script>x</script>--></script>tail"),
            new AdversarialCase(HtmlTokenizerState.ScriptData, "script",
                "<!--open</not-script")
        ];

        foreach (AdversarialCase value in cases)
        {
            TokenizerSnapshot expected = Tokenize(value, new[] { value.Input.Length });
            int split = 0;
            while (split <= value.Input.Length)
            {
                TokenizerSnapshot actual = Tokenize(value,
                    new[] { split, value.Input.Length - split });
                AssertSnapshot(actual, expected, value.Describe() + " split=" + split);
                split++;
            }
        }
    }

    [Test]
    public void LongCharacterInputRemainsBoundedAndReconstructsExactly()
    {
        const int MaximumTokenCharacters = 1024;
        string input = new string('x', 250000);
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumTokenCharacters(MaximumTokenCharacters);
        AdversarialCase value = new AdversarialCase(HtmlTokenizerState.Data,
            null, input, false, limits);
        int[] chunks = CreateFixedChunks(input.Length, 997);

        TokenizerSnapshot snapshot = Tokenize(value, chunks, false);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Status, Is.EqualTo(HtmlTokenReadStatus.Complete));
            Assert.That(string.Concat(snapshot.RawCharacterTokens), Is.EqualTo(input));
            Assert.That(snapshot.RawCharacterTokens, Has.Count.GreaterThan(200));
            Assert.That(snapshot.RawCharacterTokens.All(token =>
                token.Length <= MaximumTokenCharacters), Is.True);
            Assert.That(snapshot.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void OversizedMalformedTokenStopsAtItsConfiguredBound()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumTokenCharacters(128);
        string input = "<!--" + new string('x', 10000);
        AdversarialCase value = new AdversarialCase(HtmlTokenizerState.Data,
            null, input, false, limits);

        TokenizerSnapshot snapshot = Tokenize(value,
            CreateFixedChunks(input.Length, 31));

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Status,
                Is.EqualTo(HtmlTokenReadStatus.LimitExceeded));
            Assert.That(snapshot.Tokens, Is.Empty);
            Assert.That(snapshot.Diagnostics,
                Does.Contain(HtmlDiagnosticCode.ResourceLimitExceeded));
        });
    }

    [Test]
    public void NullAndControlPreprocessingIsStableAcrossChunkBoundaries()
    {
        StringBuilder input = new StringBuilder();
        int value = 0;
        while (value <= 31)
        {
            input.Append((char)value);
            value++;
        }

        input.Append((char)127);
        input.Append("\r\n");
        string source = input.ToString();
        AdversarialCase test = new AdversarialCase(
            HtmlTokenizerState.PlainText, null, source);

        TokenizerSnapshot oneShot = Tokenize(test, new[] { source.Length });
        TokenizerSnapshot chunked = Tokenize(test,
            Enumerable.Repeat(1, source.Length).ToArray());
        string expected = source.Replace("\0", "\uFFFD")
            .Replace("\r\n", "\n").Replace("\r", "\n");

        AssertSnapshot(chunked, oneShot, "null/control preprocessing");
        Assert.That(string.Concat(oneShot.RawCharacterTokens), Is.EqualTo(expected));
        Assert.That(oneShot.Diagnostics.Count(code =>
            code == HtmlDiagnosticCode.InputNullCharacter), Is.EqualTo(1));
    }

    [Test]
    public void SeededFuzzIsDeterministicAndChunkInvariant()
    {
        string[] first = RunFuzz(0xC0FFEEu, 300);
        string[] second = RunFuzz(0xC0FFEEu, 300);

        Assert.That(second, Is.EqualTo(first));
    }

    static string[] RunFuzz(uint seed, int count)
    {
        DeterministicGenerator generator = new DeterministicGenerator(seed);
        List<string> outcomes = new List<string>();
        HtmlTokenizerState[] states =
        [
            HtmlTokenizerState.Data,
            HtmlTokenizerState.Rcdata,
            HtmlTokenizerState.RawText,
            HtmlTokenizerState.PlainText,
            HtmlTokenizerState.ScriptData
        ];
        int index = 0;
        while (index < count)
        {
            HtmlTokenizerState state = states[generator.Next(states.Length)];
            string lastStartTag = state == HtmlTokenizerState.Rcdata ? "title"
                : state == HtmlTokenizerState.RawText ? "style"
                : state == HtmlTokenizerState.ScriptData ? "script" : null;
            string input = generator.CreateInput(generator.Next(97));
            AdversarialCase value = new AdversarialCase(state, lastStartTag, input);
            TokenizerSnapshot expected = Tokenize(value, new[] { input.Length });
            int[] chunks = generator.CreateChunks(input.Length);
            TokenizerSnapshot actual = Tokenize(value, chunks);
            AssertSnapshot(actual, expected,
                "seed=0xC0FFEE case=" + index + " " + value.Describe());
            outcomes.Add(expected.Format());
            index++;
        }

        return outcomes.ToArray();
    }

    static TokenizerSnapshot Tokenize(AdversarialCase value, int[] chunks)
    {
        return Tokenize(value, chunks, true);
    }

    static TokenizerSnapshot Tokenize(AdversarialCase value, int[] chunks,
        bool coalesceCharacters)
    {
        HtmlTokenizerOptions options = HtmlTokenizerOptions.CreateDefault();
        options.SetLimits(value.Limits);
        HtmlTokenizerContext tokenizer = new HtmlTokenizerContext(options,
            null, false);
        tokenizer.SetState(value.State);
        tokenizer.SetCdataAllowed(value.CdataAllowed);
        if (value.LastStartTag != null)
        {
            tokenizer.SetLastStartTagName(value.LastStartTag);
        }

        TokenizerSnapshot snapshot = new TokenizerSnapshot(coalesceCharacters);
        int offset = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            int length = chunks[index];
            Assert.That(length, Is.GreaterThanOrEqualTo(0));
            Assert.That(offset + length, Is.LessThanOrEqualTo(value.Input.Length));
            bool isFinal = index == chunks.Length - 1;
            tokenizer.Feed(value.Input.AsSpan(offset, length), isFinal);
            offset += length;
            Drain(tokenizer, snapshot);
            if (snapshot.Status == HtmlTokenReadStatus.LimitExceeded)
            {
                break;
            }

            index++;
        }

        if (snapshot.Status != HtmlTokenReadStatus.LimitExceeded)
        {
            Assert.That(offset, Is.EqualTo(value.Input.Length));
        }

        return snapshot;
    }

    static void Drain(HtmlTokenizerContext tokenizer, TokenizerSnapshot snapshot)
    {
        int reads = 0;
        bool reading = true;
        while (reading)
        {
            HtmlTokenReadResult result = tokenizer.Read();
            foreach (HtmlDiagnostic diagnostic in result.GetDiagnostics())
            {
                snapshot.AddDiagnostic(diagnostic);
            }
            snapshot.Status = result.GetStatus();
            if (result.GetStatus() == HtmlTokenReadStatus.TokenAvailable)
            {
                snapshot.Add(result.GetToken());
            }
            else
            {
                reading = false;
            }

            reads++;
            if (reads > 1000000)
            {
                Assert.Fail("The tokenizer did not reach a read boundary.");
            }
        }
    }

    static int[] CreateFixedChunks(int length, int maximumChunk)
    {
        List<int> chunks = new List<int>();
        int remaining = length;
        while (remaining > 0)
        {
            int chunk = remaining < maximumChunk ? remaining : maximumChunk;
            chunks.Add(chunk);
            remaining -= chunk;
        }

        if (chunks.Count == 0)
        {
            chunks.Add(0);
        }

        return chunks.ToArray();
    }

    static void AssertSnapshot(TokenizerSnapshot actual,
        TokenizerSnapshot expected, string message)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Status, Is.EqualTo(expected.Status), message);
            Assert.That(actual.Tokens, Is.EqualTo(expected.Tokens), message);
            Assert.That(actual.GetCanonicalDiagnostics(),
                Is.EqualTo(expected.GetCanonicalDiagnostics()), message);
        });
    }

    class AdversarialCase
    {
        public HtmlTokenizerState State { get; }
        public string LastStartTag { get; }
        public string Input { get; }
        public bool CdataAllowed { get; }
        public HtmlLimits Limits { get; }

        public AdversarialCase(HtmlTokenizerState state, string lastStartTag,
            string input)
            : this(state, lastStartTag, input, false, HtmlLimits.CreateDefault())
        {
        }

        public AdversarialCase(HtmlTokenizerState state, string lastStartTag,
            string input, bool cdataAllowed)
            : this(state, lastStartTag, input, cdataAllowed,
                HtmlLimits.CreateDefault())
        {
        }

        public AdversarialCase(HtmlTokenizerState state, string lastStartTag,
            string input, bool cdataAllowed, HtmlLimits limits)
        {
            this.State = state;
            this.LastStartTag = lastStartTag;
            this.Input = input;
            this.CdataAllowed = cdataAllowed;
            this.Limits = limits;
        }

        public string Describe()
        {
            return this.State + " " + JsonSerializer.Serialize(this.Input);
        }
    }

    class TokenizerSnapshot
    {
        bool coalesceCharacters;

        public List<string> Tokens { get; }
        public List<string> RawCharacterTokens { get; }
        public List<HtmlDiagnosticCode> Diagnostics { get; }
        public List<string> DiagnosticRecords { get; }
        public HtmlTokenReadStatus Status { get; set; }

        public TokenizerSnapshot(bool coalesceCharacters)
        {
            this.coalesceCharacters = coalesceCharacters;
            this.Tokens = new List<string>();
            this.RawCharacterTokens = new List<string>();
            this.Diagnostics = new List<HtmlDiagnosticCode>();
            this.DiagnosticRecords = new List<string>();
            this.Status = HtmlTokenReadStatus.NeedMoreInput;
        }

        public void Add(HtmlToken token)
        {
            if (token.GetKind() == HtmlTokenKind.EndOfFile)
            {
                return;
            }

            if (token.GetKind() == HtmlTokenKind.Character)
            {
                string data = token.GetData();
                this.RawCharacterTokens.Add(data);
                if (this.coalesceCharacters && this.Tokens.Count > 0
                    && this.Tokens[this.Tokens.Count - 1].StartsWith("Character:"))
                {
                    string previous = JsonSerializer.Deserialize<string>(
                        this.Tokens[this.Tokens.Count - 1].Substring(10));
                    this.Tokens[this.Tokens.Count - 1] = "Character:"
                        + JsonSerializer.Serialize(previous + data);
                }
                else
                {
                    this.Tokens.Add("Character:" + JsonSerializer.Serialize(data));
                }

                return;
            }

            if (token.GetKind() == HtmlTokenKind.Comment)
            {
                this.Tokens.Add("Comment:" + JsonSerializer.Serialize(token.GetData()));
                return;
            }

            if (token.GetKind() == HtmlTokenKind.Doctype)
            {
                this.Tokens.Add("DOCTYPE:" + JsonSerializer.Serialize(new object[]
                {
                    token.HasName() ? token.GetName() : null,
                    token.HasPublicIdentifier() ? token.GetPublicIdentifier() : null,
                    token.HasSystemIdentifier() ? token.GetSystemIdentifier() : null,
                    !token.IsForceQuirks()
                }));
                return;
            }

            string attributes = string.Join(",", token.GetAttributes().Select(attribute =>
                JsonSerializer.Serialize(attribute.GetName()) + "="
                + JsonSerializer.Serialize(attribute.GetValue())));
            this.Tokens.Add(token.GetKind() + ":" + JsonSerializer.Serialize(
                token.GetName()) + ":" + attributes + ":" + token.IsSelfClosing());
        }

        public void AddDiagnostic(HtmlDiagnostic diagnostic)
        {
            this.Diagnostics.Add(diagnostic.GetCode());
            HtmlSourceSpan span = diagnostic.GetSpan();
            int start = span.IsKnown()
                ? span.GetStart().GetCharacterOffset() : -1;
            int end = span.IsKnown()
                ? span.GetEnd().GetCharacterOffset() : -1;
            this.DiagnosticRecords.Add(start + ":" + end + ":"
                + (int)diagnostic.GetCode());
        }

        public string[] GetCanonicalDiagnostics()
        {
            return this.DiagnosticRecords.OrderBy(value => value,
                StringComparer.Ordinal).ToArray();
        }

        public string Format()
        {
            return this.Status + "|" + string.Join("|", this.Tokens)
                + "|diagnostics=" + string.Join(",",
                    this.GetCanonicalDiagnostics());
        }
    }

    class DeterministicGenerator
    {
        static readonly char[] Alphabet =
        [
            'a', 'Z', '0', ' ', '\t', '\n', '\r', '\0', '<', '>', '/', '!',
            '-', '[', ']', '&', ';', '#', 'x', '=', '\'', '"', '\uD83D',
            '\uDE00', '\uD800', '\uDC00'
        ];

        uint state;

        public DeterministicGenerator(uint seed)
        {
            this.state = seed;
        }

        public int Next(int maximum)
        {
            this.state = this.state * 1664525u + 1013904223u;
            return (int)(this.state % (uint)maximum);
        }

        public string CreateInput(int length)
        {
            char[] result = new char[length];
            int index = 0;
            while (index < result.Length)
            {
                result[index] = Alphabet[this.Next(Alphabet.Length)];
                index++;
            }

            return new string(result);
        }

        public int[] CreateChunks(int length)
        {
            if (length == 0)
            {
                return new[] { 0 };
            }

            List<int> chunks = new List<int>();
            int remaining = length;
            while (remaining > 0)
            {
                int maximum = remaining < 11 ? remaining : 11;
                int chunk = 1 + this.Next(maximum);
                chunks.Add(chunk);
                remaining -= chunk;
            }

            return chunks.ToArray();
        }
    }
}
