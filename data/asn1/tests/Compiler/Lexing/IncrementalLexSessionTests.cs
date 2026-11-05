using System.Text;
using asn1;

namespace asn1.Tests;

public class IncrementalLexSessionTests
{
    [Test]
    public void SessionComposesEveryTokenFamilyWithoutLosingSourceText()
    {
        string source = "MyModule DEFINITIONS ::= BEGIN\n"
            + "-- note --\n"
            + "value INTEGER ::= 42\n"
            + "real REAL ::= 1.5E-2\n"
            + "bits BIT STRING ::= '0101'B\n"
            + "hex OCTET STRING ::= 'AF'H\n"
            + "text UTF8String ::= \"a\"\"😀\"\n"
            + "tuple IA5String ::= {1, 2}\n"
            + "quad UniversalString ::= {0,0,0,65}\n"
            + "&Type @.component [[ ... ]] [XER:1] <INTEGER>7</INTEGER> = END";
        IncrementalLexSession session = CreateSession("all-tokens.asn");
        session.SupplyTextChunk(source, true);

        List<AsnToken> tokens = DrainToEnd(session);

        Assert.That(string.Concat(tokens.Select(token => token.Text)), Is.EqualTo(source));
        Assert.That(tokens[^1].Kind, Is.EqualTo(AsnTokenKind.EndOfInput));
        Assert.That(tokens[^1].Span?.Start.Offset, Is.EqualTo(source.Length));
        Assert.That(tokens[^1].Span?.End.Offset, Is.EqualTo(source.Length));
        Assert.That(tokens.Select(token => token.Kind), Does.Contain(AsnTokenKind.Comment));
        Assert.That(tokens.Select(token => token.Kind), Does.Contain(AsnTokenKind.ReservedWord));
        Assert.That(tokens.Select(token => token.Kind), Does.Contain(AsnTokenKind.TypeReference));
        Assert.That(tokens.Select(token => token.Kind), Does.Contain(AsnTokenKind.Identifier));
        Assert.That(tokens.Select(token => token.Kind), Does.Contain(AsnTokenKind.IntegerLiteral));
        Assert.That(tokens.Select(token => token.Kind), Does.Contain(AsnTokenKind.RealLiteral));
        Assert.That(tokens.Select(token => token.Kind), Does.Contain(AsnTokenKind.BinaryStringLiteral));
        Assert.That(tokens.Select(token => token.Kind), Does.Contain(AsnTokenKind.HexStringLiteral));
        Assert.That(tokens.Select(token => token.Kind), Does.Contain(AsnTokenKind.CharacterStringLiteral));
        Assert.That(tokens.Select(token => token.Kind), Does.Contain(AsnTokenKind.TupleLiteral));
        Assert.That(tokens.Select(token => token.Kind), Does.Contain(AsnTokenKind.QuadrupleLiteral));
        Assert.That(tokens.Select(token => token.Kind), Does.Contain(AsnTokenKind.TypeFieldReference));
        Assert.That(tokens.Select(token => token.Kind), Does.Contain(AsnTokenKind.LeftVersionBracket));
        Assert.That(tokens.Select(token => token.Kind), Does.Contain(AsnTokenKind.Ellipsis));
        Assert.That(tokens.Select(token => token.Kind), Does.Contain(AsnTokenKind.XmlValue));
        Assert.That(tokens.Select(token => token.Kind), Does.Contain(AsnTokenKind.Equals));
        Assert.That(session.GetConsumedCharacterCount(), Is.EqualTo(source.Length));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        Assert.That(session.ScanNext().Status, Is.EqualTo(IncrementalLexStatus.EndOfInput));
    }

    [Test]
    public void EveryUtf16SplitProducesTheSameTokensLocationsAndDiagnostics()
    {
        string source = "\uFEFFName\u2011Part ::= \"é😀\"\r\n--π--\n<X>λ&amp;</X> [[...]] '01'B";
        LexSnapshot expected = LexTextInOneChunk(source);

        int split = 0;
        while (split <= source.Length)
        {
            IncrementalLexSession session = CreateSession("text-splits.asn");
            session.SupplyTextChunk(source.Substring(0, split), false);
            List<AsnToken> actualTokens = DrainUntilNeedMore(session);
            session.SupplyTextChunk(source.Substring(split), true);
            actualTokens.AddRange(DrainToEnd(session));

            AssertTokensEqual(expected.Tokens, actualTokens, "UTF-16 split " + split);
            AssertDiagnosticsEqual(expected.Diagnostics, session.GetDiagnostics(), "UTF-16 split " + split);
            split = split + 1;
        }
    }

    [Test]
    public void EveryMalformedInputSplitPreservesExactRecoveryBoundaries()
    {
        string source = "bad- GOOD 01 value 1e+ next '02'B after \"a\u0001b\" done _ END";
        LexSnapshot expected = LexTextInOneChunk(source);

        Assert.That(
            expected.Diagnostics.Select(result => result.Code),
            Is.EqualTo(new[]
            {
                "ASNL0003",
                "ASNL0004",
                "ASNL0005",
                "ASNL0006",
                "ASNL0009",
                "ASNL0014"
            }));
        Assert.That(
            expected.Diagnostics.Select(result => result.Span?.Start.Offset),
            Is.EqualTo(new[] { 0, 10, 19, 28, 40, 51 }));
        Assert.That(
            expected.Tokens.Where(token => token.Kind == AsnTokenKind.Invalid)
                .Select(token => token.Text),
            Is.EqualTo(new[] { "bad-", "01", "1e+", "'02'B", "\"a\u0001b\"", "_" }));
        Assert.That(
            expected.Tokens.Select(token => token.Text),
            Does.Contain("END"));

        int split = 0;
        while (split <= source.Length)
        {
            IncrementalLexSession session = CreateSession("malformed-splits.asn");
            session.SupplyTextChunk(source.Substring(0, split), false);
            List<AsnToken> actualTokens = DrainUntilNeedMore(session);
            session.SupplyTextChunk(source.Substring(split), true);
            actualTokens.AddRange(DrainToEnd(session));

            AssertTokensEqual(expected.Tokens, actualTokens, "malformed split " + split);
            AssertDiagnosticsEqual(
                expected.Diagnostics,
                session.GetDiagnostics(),
                "malformed split " + split);
            split = split + 1;
        }
    }

    [Test]
    public void OneCodeUnitChunksResumeAllOpenTokenStates()
    {
        string source = "Name--c--\r\n1.2E-3 '01'B \"a\"\"😀\" {1,2} [[...]] <X>&amp;</X>";
        LexSnapshot expected = LexTextInOneChunk(source);
        IncrementalLexSession session = CreateSession("unit-chunks.asn");
        List<AsnToken> actual = new List<AsnToken>();

        int index = 0;
        while (index < source.Length)
        {
            bool isFinal = index == source.Length - 1;
            session.SupplyTextChunk(source.Substring(index, 1), isFinal);
            if (isFinal)
            {
                actual.AddRange(DrainToEnd(session));
            }
            else
            {
                actual.AddRange(DrainUntilNeedMore(session));
            }

            index = index + 1;
        }

        AssertTokensEqual(expected.Tokens, actual, "single UTF-16 code-unit chunks");
        AssertDiagnosticsEqual(expected.Diagnostics, session.GetDiagnostics(), "single UTF-16 code-unit chunks");
    }

    [Test]
    public void EveryUtf8ByteSplitProducesTheSameUnicodeTokenStream()
    {
        string source = "\uFEFFMód ::= \"é😀\"\r\n--π--\n<X>λ&amp;</X>";
        byte[] bytes = Encoding.UTF8.GetBytes(source);
        LexSnapshot expected = LexUtf8InOneChunk(bytes);

        int split = 0;
        while (split <= bytes.Length)
        {
            byte[] first = bytes.Take(split).ToArray();
            byte[] second = bytes.Skip(split).ToArray();
            IncrementalLexSession session = CreateSession("utf8-splits.asn");
            session.SupplyUtf8Chunk(first, false);
            List<AsnToken> actualTokens = DrainUntilNeedMore(session);
            session.SupplyUtf8Chunk(second, true);
            actualTokens.AddRange(DrainToEnd(session));

            AssertTokensEqual(expected.Tokens, actualTokens, "UTF-8 split " + split);
            AssertDiagnosticsEqual(expected.Diagnostics, session.GetDiagnostics(), "UTF-8 split " + split);
            split = split + 1;
        }
    }

    [Test]
    public void InvalidUtf8BecomesLocatedInvalidTokensAndDiagnostics()
    {
        IncrementalLexSession session = CreateSession("invalid-utf8.asn");
        session.SupplyUtf8Chunk([0xC0], false);
        List<AsnToken> tokens = DrainUntilNeedMore(session);
        session.SupplyUtf8Chunk([0xAF], true);
        tokens.AddRange(DrainToEnd(session));

        Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new[]
        {
            AsnTokenKind.Invalid,
            AsnTokenKind.Invalid,
            AsnTokenKind.EndOfInput
        }));
        Assert.That(tokens[0].Text, Is.EqualTo("\uFFFD"));
        Assert.That(tokens[0].Span?.Start.Offset, Is.Zero);
        Assert.That(tokens[1].Span?.Start.Offset, Is.EqualTo(1));
        Assert.That(session.GetDiagnostics(), Has.Length.EqualTo(2));
    }

    [Test]
    public void SessionRejectsMixingTextAndUtf8ChunkModes()
    {
        IncrementalLexSession textSession = CreateSession("text.asn");
        textSession.SupplyTextChunk("A", false);
        Assert.That(
            () => textSession.SupplyUtf8Chunk([65], true),
            Throws.TypeOf<InvalidOperationException>());

        IncrementalLexSession utf8Session = CreateSession("utf8.asn");
        utf8Session.SupplyUtf8Chunk([65], false);
        Assert.That(
            () => utf8Session.SupplyTextChunk("B", true),
            Throws.TypeOf<InvalidOperationException>());
    }

    private static IncrementalLexSession CreateSession(string sourceName)
    {
        return new IncrementalLexSession(sourceName, false, new AsnResourceLimits());
    }

    private static LexSnapshot LexTextInOneChunk(string source)
    {
        IncrementalLexSession session = CreateSession("text-splits.asn");
        session.SupplyTextChunk(source, true);
        return new LexSnapshot
        {
            Tokens = DrainToEnd(session).ToArray(),
            Diagnostics = session.GetDiagnostics()
        };
    }

    private static LexSnapshot LexUtf8InOneChunk(byte[] source)
    {
        IncrementalLexSession session = CreateSession("utf8-splits.asn");
        session.SupplyUtf8Chunk(source, true);
        return new LexSnapshot
        {
            Tokens = DrainToEnd(session).ToArray(),
            Diagnostics = session.GetDiagnostics()
        };
    }

    private static List<AsnToken> DrainUntilNeedMore(IncrementalLexSession session)
    {
        List<AsnToken> tokens = new List<AsnToken>();
        while (true)
        {
            IncrementalLexResult result = session.ScanNext();
            if (result.Status == IncrementalLexStatus.NeedMoreData)
            {
                return tokens;
            }

            Assert.That(result.Status, Is.EqualTo(IncrementalLexStatus.Token));
            Assert.That(result.Token, Is.Not.Null);
            tokens.Add(result.Token ?? throw new InvalidOperationException());
        }
    }

    private static List<AsnToken> DrainToEnd(IncrementalLexSession session)
    {
        List<AsnToken> tokens = new List<AsnToken>();
        while (true)
        {
            IncrementalLexResult result = session.ScanNext();
            if (result.Status == IncrementalLexStatus.EndOfInput)
            {
                return tokens;
            }

            Assert.That(result.Status, Is.EqualTo(IncrementalLexStatus.Token));
            Assert.That(result.Token, Is.Not.Null);
            tokens.Add(result.Token ?? throw new InvalidOperationException());
        }
    }

    private static void AssertTokensEqual(AsnToken[] expected, List<AsnToken> actual, string context)
    {
        Assert.That(actual, Has.Count.EqualTo(expected.Length), context);
        int index = 0;
        while (index < expected.Length)
        {
            Assert.That(actual[index].Kind, Is.EqualTo(expected[index].Kind), context + " token " + index);
            Assert.That(actual[index].Text, Is.EqualTo(expected[index].Text), context + " token " + index);
            Assert.That(
                actual[index].Span?.Start.Offset,
                Is.EqualTo(expected[index].Span?.Start.Offset),
                context + " token " + index);
            Assert.That(
                actual[index].Span?.Start.Line,
                Is.EqualTo(expected[index].Span?.Start.Line),
                context + " token " + index);
            Assert.That(
                actual[index].Span?.Start.Column,
                Is.EqualTo(expected[index].Span?.Start.Column),
                context + " token " + index);
            Assert.That(
                actual[index].Span?.End.Offset,
                Is.EqualTo(expected[index].Span?.End.Offset),
                context + " token " + index);
            Assert.That(
                actual[index].Span?.End.Line,
                Is.EqualTo(expected[index].Span?.End.Line),
                context + " token " + index);
            Assert.That(
                actual[index].Span?.End.Column,
                Is.EqualTo(expected[index].Span?.End.Column),
                context + " token " + index);
            index = index + 1;
        }
    }

    private static void AssertDiagnosticsEqual(
        AsnDiagnostic[] expected,
        AsnDiagnostic[] actual,
        string context)
    {
        Assert.That(actual.Select(result => result.Code), Is.EqualTo(expected.Select(result => result.Code)), context);
        Assert.That(actual.Select(result => result.Message), Is.EqualTo(expected.Select(result => result.Message)), context);
        Assert.That(
            actual.Select(result => result.Span?.Start.Offset),
            Is.EqualTo(expected.Select(result => result.Span?.Start.Offset)),
            context);
    }

    private class LexSnapshot
    {
        public AsnToken[] Tokens = [];
        public AsnDiagnostic[] Diagnostics = [];
    }
}
