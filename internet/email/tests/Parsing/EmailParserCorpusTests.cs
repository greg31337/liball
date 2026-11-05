using System.Text;
using email;

namespace email.Tests;

public class EmailParserCorpusTests
{
    [Test]
    public void ParsesRepresentativeRfc5322AndRfc2046Examples()
    {
        const string internetMessage = "From: John Doe <jdoe@machine.example>\r\nTo: Mary Smith <mary@example.net>\r\nSubject: Saying Hello\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nMessage-ID: <1234@local.machine.example>\r\n\r\nThis is a message just to say hello.\r\n";
        EmailParseResult internetResult = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(internetMessage), new EmailParseOptionsOperations().CreatePreserving());

        Assert.That(internetResult.IsComplete(), Is.True);
        Assert.That(internetResult.IsSuccess(), Is.True);
        Assert.That(internetResult.GetIssues().GetCount(), Is.Zero);
        Assert.That(internetResult.GetMessage()!.GetHeaders()!.GetFirst("From")!.GetDecodedValue(), Does.Contain("John Doe"));
        Assert.That(internetResult.GetMessage()!.GetHeaders()!.GetFirst("Message-ID")!.GetDecodedValue(), Is.EqualTo("<1234@local.machine.example>"));
        Assert.That(Text(internetResult.GetMessage()!.GetBody()!.GetRawContent()!.Materialize(100)), Is.EqualTo("This is a message just to say hello.\r\n"));

        const string mimeMessage = "From: Nathaniel Borenstein <nsb@bellcore.com>\r\nMIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=\"simple boundary\"\r\n\r\nThis is the preamble.\r\n--simple boundary\r\nContent-Type: text/plain; charset=us-ascii\r\n\r\nFirst part.\r\n--simple boundary\r\nContent-Type: text/plain; charset=us-ascii\r\nContent-Transfer-Encoding: quoted-printable\r\n\r\nSecond=20part.\r\n--simple boundary--\r\nThis is the epilogue.";
        EmailParseResult mimeResult = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(mimeMessage), new EmailParseOptionsOperations().CreatePreserving());
        MimeEntity multipart = mimeResult.GetMessage()!.GetBody()!;

        Assert.That(mimeResult.IsComplete(), Is.True);
        Assert.That(mimeResult.IsSuccess(), Is.True);
        Assert.That(multipart!.GetKind(), Is.EqualTo(MimeEntityKind.Multipart));
        Assert.That(multipart.GetChildCount(), Is.EqualTo(2));
        Assert.That(Text(multipart.GetPreamble()!.Materialize(100)), Is.EqualTo("This is the preamble."));
        Assert.That(Text(multipart.GetChildAt(0).GetRawContent()!.Materialize(100)), Is.EqualTo("First part."));
        Assert.That(Text(multipart.GetChildAt(1).GetRawContent()!.Materialize(100)), Is.EqualTo("Second=20part."));
        Assert.That(Text(multipart.GetEpilogue()!.Materialize(100)), Is.EqualTo("This is the epilogue."));
    }

    [Test]
    public void ParsesADeepMultipartTreeWithinTheConfiguredLimit()
    {
        const int depth = 20;
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(BuildDeepMultipart(depth), new EmailParseOptionsOperations().CreatePreserving());
        MimeEntity current = result.GetEntity()!;

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
        int index = 0;
        while (index < depth)
        {
            Assert.That(current!.GetKind(), Is.EqualTo(MimeEntityKind.Multipart), "depth " + index);
            Assert.That(current.GetChildCount(), Is.EqualTo(1), "depth " + index);
            MimeEntity child = current.GetChildAt(0);
            Assert.That(child.GetParent(), Is.SameAs(current), "depth " + index);
            current = child;
            index++;
        }

        Assert.That(current!.GetKind(), Is.EqualTo(MimeEntityKind.Leaf));
        Assert.That(Text(current.GetRawContent()!.Materialize(100)), Is.EqualTo("deepest body"));
    }

    [Test]
    public void RetainsEmptyPartsWithAndWithoutContentHeaders()
    {
        const string source = "Content-Type: multipart/mixed; boundary=empty\r\n\r\n--empty\r\n\r\n\r\n--empty\r\nContent-Type: application/octet-stream\r\n\r\n\r\n--empty--\r\n";

        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreatePreserving());
        MimeEntity multipart = result.GetEntity()!;

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(multipart!.GetChildCount(), Is.EqualTo(2));
        Assert.That(multipart.GetChildAt(0).GetRawContent()!.GetLength(), Is.Zero);
        Assert.That(multipart.GetChildAt(0).GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(multipart.GetChildAt(1).GetRawContent()!.GetLength(), Is.Zero);
        Assert.That(multipart.GetChildAt(1).GetMediaType()!.IsType("application", "octet-stream"), Is.True);
    }

    [Test]
    public void HugeFoldedHeaderParsesNormallyAndStopsAtTheBoundedUntrustedLimit()
    {
        byte[] source = BuildHugeHeader(200, 900);

        EmailParseResult normal = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, new EmailParseOptionsOperations().CreatePreserving());
        EmailParseResult bounded = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, new EmailParseOptionsOperations().CreateBoundedUntrusted());

        Assert.That(normal.IsComplete(), Is.True);
        Assert.That(normal.IsSuccess(), Is.True);
        Assert.That(normal.GetMessage()!.GetHeaders()!.GetCount(), Is.EqualTo(1));
        Assert.That(normal.GetMessage()!.GetHeaders()!.GetAt(0).GetRawFieldBytes().Length, Is.GreaterThan(180000));
        Assert.That(Text(normal.GetMessage()!.GetBody()!.GetRawContent()!.Materialize(10)), Is.EqualTo("body"));

        Assert.That(bounded.IsComplete(), Is.False);
        Assert.That(bounded.HasMessage(), Is.True);
        Assert.That(bounded.GetIssues().ContainsCode(EmailIssueCode.LimitExceeded), Is.True);
        Assert.That(bounded.GetMessage()!.GetHeaders()!.GetCount(), Is.EqualTo(1));
        Assert.That(bounded.GetMessage()!.GetHeaders()!.GetAt(0).GetRawFieldBytes().Length, Is.LessThan(normal.GetMessage()!.GetHeaders()!.GetAt(0).GetRawFieldBytes().Length));
    }

    [Test]
    public void MalformedSpamLikeInputProducesAUsefulTolerantTree()
    {
        const string source = " orphan fold\nBad Header: malformed name\nSubject: cheap pills\nSubject: cheaper pills\nContent-Type: multipart/mixed; boundary=spam\n\n--spam\nContent-Transfer-Encoding: base64 garbage\n\n%%%not-base64%%%\n--spam--\n";

        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreatePreserving());

        Assert.That(result.IsComplete(), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetMessage()!.GetHeaders()!.GetCountNamed("Subject"), Is.EqualTo(2));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderContinuationWithoutField), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderNameMalformed), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HeaderDuplicateSingleton), Is.True);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.TransferEncodingMalformed), Is.True);
        Assert.That(result.GetMessage()!.GetBody()!.GetChildCount(), Is.EqualTo(1));
        Assert.That(Text(result.GetMessage()!.GetBody()!.GetChildAt(0).GetRawContent()!.Materialize(100)), Is.EqualTo("%%%not-base64%%%"));
    }

    [Test]
    public void EveryTwoChunkSplitMatchesWholeParsingForRecoveredInputInBothModes()
    {
        byte[] source = Bytes("Subject: first\nSubject: second\nContent-Type: multipart/mixed; boundary=split\n\n--split\n\nbody\n--split--\n");
        EmailParseOptions[] modes = new EmailParseOptions[] { new EmailParseOptionsOperations().CreatePreserving(), new EmailParseOptionsOperations().CreateStrict() };
        int modeIndex = 0;
        while (modeIndex < modes.Length)
        {
            EmailParseOptions options = modes[modeIndex];
            EmailParseResult expected = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, options);
            int split = 0;
            while (split <= source.Length)
            {
                EmailFeedParser parser = new EmailFeedParserOperations().CreateWithOptions(options);
                parser.Feed(new ReadOnlySpan<byte>(source, 0, split));
                parser.Feed(new ReadOnlySpan<byte>(source, split, source.Length - split));
                EmailParseResult actual = parser.Complete();

                AssertEquivalentSummary(actual, expected, "mode " + modeIndex + ", split " + split);
                parser.Close();
                split++;
            }

            modeIndex++;
        }
    }

    [TestCase("Subject: example\n\nbody", EmailIssueCode.HeaderNonCanonicalLineEnding)]
    [TestCase(" orphan\r\nSubject: example\r\n\r\nbody", EmailIssueCode.HeaderContinuationWithoutField)]
    [TestCase("Subject: first\r\nSubject: second\r\n\r\nbody", EmailIssueCode.HeaderDuplicateSingleton)]
    [TestCase("Content-Transfer-Encoding: base64 garbage\r\n\r\nbody", EmailIssueCode.TransferEncodingMalformed)]
    public void StrictModePromotesRecoverableSyntaxThatTolerantModeRetains(string source, EmailIssueCode issueCode)
    {
        EmailParseResult tolerant = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreatePreserving());
        EmailParseResult strict = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(source), new EmailParseOptionsOperations().CreateStrict());

        Assert.That(tolerant.IsComplete(), Is.True);
        Assert.That(tolerant.IsSuccess(), Is.True);
        Assert.That(tolerant.HasMessage(), Is.True);
        Assert.That(tolerant.GetIssues().ContainsCode(issueCode), Is.True);
        Assert.That(strict.IsComplete(), Is.True);
        Assert.That(strict.IsSuccess(), Is.False);
        Assert.That(strict.HasMessage(), Is.True);
        Assert.That(strict.GetIssues().ContainsCode(issueCode), Is.True);
    }

    static byte[] BuildDeepMultipart(int depth)
    {
        string entity = "Content-Type: text/plain\r\n\r\ndeepest body";
        int index = depth - 1;
        while (index >= 0)
        {
            string boundary = "depth-" + index;
            entity = "Content-Type: multipart/mixed; boundary=" + boundary + "\r\n\r\n--" + boundary + "\r\n" + entity + "\r\n--" + boundary + "--\r\n";
            index--;
        }

        return Bytes(entity);
    }

    static byte[] BuildHugeHeader(int continuationCount, int continuationLength)
    {
        StringBuilder value = new StringBuilder();
        value.Append("X-Huge: start\r\n");
        int index = 0;
        while (index < continuationCount)
        {
            value.Append(' ');
            value.Append('x', continuationLength);
            value.Append("\r\n");
            index++;
        }

        value.Append("\r\nbody");
        return Bytes(value.ToString());
    }

    static void AssertEquivalentSummary(EmailParseResult actual, EmailParseResult expected, string label)
    {
        Assert.That(actual.IsComplete(), Is.EqualTo(expected.IsComplete()), label);
        Assert.That(actual.IsSuccess(), Is.EqualTo(expected.IsSuccess()), label);
        Assert.That(actual.GetSourceByteCount(), Is.EqualTo(expected.GetSourceByteCount()), label);
        Assert.That(actual.GetIssues().GetCount(), Is.EqualTo(expected.GetIssues().GetCount()), label);
        int issueIndex = 0;
        while (issueIndex < actual.GetIssues().GetCount())
        {
            Assert.That(actual.GetIssues().GetAt(issueIndex).GetCode(), Is.EqualTo(expected.GetIssues().GetAt(issueIndex).GetCode()), label);
            Assert.That(actual.GetIssues().GetAt(issueIndex).GetSeverity(), Is.EqualTo(expected.GetIssues().GetAt(issueIndex).GetSeverity()), label);
            issueIndex++;
        }

        Assert.That(actual.HasMessage(), Is.EqualTo(expected.HasMessage()), label);
        if (!actual.HasMessage())
        {
            return;
        }

        Assert.That(actual.GetMessage()!.GetHeaders()!.GetCount(), Is.EqualTo(expected.GetMessage()!.GetHeaders()!.GetCount()), label);
        Assert.That(actual.GetMessage()!.HasBody(), Is.EqualTo(expected.GetMessage()!.HasBody()), label);
        if (actual.GetMessage()!.HasBody())
        {
            AssertEquivalentEntitySummary(actual.GetMessage()!.GetBody()!, expected.GetMessage()!.GetBody()!, label);
        }
    }

    static void AssertEquivalentEntitySummary(MimeEntity actual, MimeEntity expected, string label)
    {
        Assert.That(actual.GetKind(), Is.EqualTo(expected.GetKind()), label);
        Assert.That(actual.GetChildCount(), Is.EqualTo(expected.GetChildCount()), label);
        Assert.That(actual.HasRawContent(), Is.EqualTo(expected.HasRawContent()), label);
        if (actual.HasRawContent())
        {
            Assert.That(actual.GetRawContent()!.Materialize(1000), Is.EqualTo(expected.GetRawContent()!.Materialize(1000)), label);
        }

        int index = 0;
        while (index < actual.GetChildCount())
        {
            AssertEquivalentEntitySummary(actual.GetChildAt(index), expected.GetChildAt(index), label);
            index++;
        }
    }

    static byte[] Bytes(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return Encoding.ASCII.GetString(value);
    }
}
