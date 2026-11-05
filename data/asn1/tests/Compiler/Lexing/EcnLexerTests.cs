using asn1;

namespace asn1.Tests;

public class EcnLexerTests
{
    [Test]
    public void OneCharacterChunksPreserveEcnReferencesWordsAndNonEcnText()
    {
        string source = "#Payload ENCODING-DEFINITIONS ENCODE MAPPING ORDERED "
            + "NON-ECN-BEGIN\nextern C; { return 1; } /* raw */\n"
            + "NON-ECN-END";
        IncrementalLexSession session = new IncrementalLexSession(
            "ecn.edm",
            false,
            new AsnResourceLimits());
        System.Collections.Generic.List<AsnToken> tokens =
            new System.Collections.Generic.List<AsnToken>();

        int index = 0;
        while (index < source.Length)
        {
            session.SupplyTextChunk(
                source.Substring(index, 1),
                index == source.Length - 1);
            this.DrainAvailableTokens(session, tokens);
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(
                string.Concat(tokens
                    .Where(token => token.Kind != AsnTokenKind.EndOfInput)
                    .Select(token => token.Text)),
                Is.EqualTo(source));
            Assert.That(tokens.Single(token => token.Text == "#Payload").Kind,
                Is.EqualTo(AsnTokenKind.EncodingClassReference));
            Assert.That(tokens.Single(token => token.Text
                == "ENCODING-DEFINITIONS").Kind,
                Is.EqualTo(AsnTokenKind.ReservedWord));
            Assert.That(tokens.Single(token => token.Kind
                == AsnTokenKind.NonEcnText).Text,
                Is.EqualTo("\nextern C; { return 1; } /* raw */\n"));
            Assert.That(session.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void LowerCaseEncodingClassReferenceIsDiagnosed()
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "bad-ecn.edm",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk("#lower", true);
        System.Collections.Generic.List<AsnToken> tokens =
            new System.Collections.Generic.List<AsnToken>();
        this.DrainAvailableTokens(session, tokens);

        Assert.Multiple(() =>
        {
            Assert.That(tokens[0].Kind, Is.EqualTo(AsnTokenKind.Invalid));
            Assert.That(tokens[0].Text, Is.EqualTo("#lower"));
            Assert.That(session.GetDiagnostics().Single().Code,
                Is.EqualTo("ASNL0016"));
        });
    }

    [Test]
    public void UnterminatedNonEcnTextProducesALocatedLexicalFailure()
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "unterminated.edm",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk("NON-ECN-BEGIN raw", true);
        System.Collections.Generic.List<AsnToken> tokens =
            new System.Collections.Generic.List<AsnToken>();
        this.DrainAvailableTokens(session, tokens);

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(token => token.Kind), Does.Contain(
                AsnTokenKind.Invalid));
            Assert.That(session.GetDiagnostics().Single().Code,
                Is.EqualTo("ASNL0015"));
            Assert.That(session.GetDiagnostics().Single().Span?.Start.Offset,
                Is.EqualTo("NON-ECN-BEGIN".Length));
        });
    }

    [Test]
    public void EquivalentHyphensDelimitNonEcnTextAtEverySplitPoint()
    {
        string source = "NON\u2011ECN\u2011BEGIN\npayload\ud83d\ude00\r\nNON\u2011ECN\u2011END";

        int textSplit = 0;
        while (textSplit <= source.Length)
        {
            IncrementalLexSession session = new IncrementalLexSession(
                "unicode-ecn.edm",
                false,
                new AsnResourceLimits());
            System.Collections.Generic.List<AsnToken> tokens =
                new System.Collections.Generic.List<AsnToken>();
            session.SupplyTextChunk(source.Substring(0, textSplit), false);
            this.DrainAvailableTokens(session, tokens);
            session.SupplyTextChunk(source.Substring(textSplit), true);
            this.DrainAvailableTokens(session, tokens);

            this.AssertEquivalentHyphenTokens(
                source,
                tokens,
                session.GetDiagnostics(),
                "UTF-16 split " + textSplit);
            textSplit = textSplit + 1;
        }

        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(source);
        int byteSplit = 0;
        while (byteSplit <= utf8.Length)
        {
            IncrementalLexSession session = new IncrementalLexSession(
                "unicode-ecn.edm",
                false,
                new AsnResourceLimits());
            System.Collections.Generic.List<AsnToken> tokens =
                new System.Collections.Generic.List<AsnToken>();
            session.SupplyUtf8Chunk(utf8.Take(byteSplit).ToArray(), false);
            this.DrainAvailableTokens(session, tokens);
            session.SupplyUtf8Chunk(utf8.Skip(byteSplit).ToArray(), true);
            this.DrainAvailableTokens(session, tokens);

            this.AssertEquivalentHyphenTokens(
                source,
                tokens,
                session.GetDiagnostics(),
                "UTF-8 split " + byteSplit);
            byteSplit = byteSplit + 1;
        }
    }

    private void AssertEquivalentHyphenTokens(
        string source,
        System.Collections.Generic.List<AsnToken> tokens,
        AsnDiagnostic[] diagnostics,
        string splitDescription)
    {
        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Is.Empty, splitDescription);
            Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                AsnTokenKind.ReservedWord,
                AsnTokenKind.NonEcnText,
                AsnTokenKind.ReservedWord,
                AsnTokenKind.EndOfInput
            }), splitDescription);
            Assert.That(
                string.Concat(tokens.Select(token => token.Text)),
                Is.EqualTo(source),
                splitDescription);
            Assert.That(tokens[0].Span?.Start.Offset, Is.Zero, splitDescription);
            Assert.That(tokens[0].Span?.End.Offset, Is.EqualTo(13), splitDescription);
            Assert.That(tokens[1].Text, Is.EqualTo("\npayload\ud83d\ude00\r\n"), splitDescription);
            Assert.That(tokens[1].Span?.Start.Offset, Is.EqualTo(13), splitDescription);
            Assert.That(tokens[1].Span?.End.Offset, Is.EqualTo(25), splitDescription);
            Assert.That(tokens[2].Text, Is.EqualTo("NON\u2011ECN\u2011END"), splitDescription);
            Assert.That(tokens[2].Span?.Start.Offset, Is.EqualTo(25), splitDescription);
            Assert.That(tokens[2].Span?.End.Offset, Is.EqualTo(36), splitDescription);
            Assert.That(tokens[2].Span?.Start.Line, Is.EqualTo(3), splitDescription);
            Assert.That(tokens[2].Span?.Start.Column, Is.EqualTo(1), splitDescription);
            Assert.That(tokens[3].Span?.Start.Offset, Is.EqualTo(source.Length), splitDescription);
        });
    }

    private void DrainAvailableTokens(
        IncrementalLexSession session,
        System.Collections.Generic.List<AsnToken> tokens)
    {
        while (true)
        {
            IncrementalLexResult result = session.ScanNext();
            if (result.Status == IncrementalLexStatus.NeedMoreData
                || result.Status == IncrementalLexStatus.EndOfInput)
            {
                return;
            }

            if (result.Token != null)
            {
                tokens.Add(result.Token);
                if (result.Token.Kind == AsnTokenKind.EndOfInput)
                {
                    return;
                }
            }
        }
    }
}
