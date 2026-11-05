using System.Text;

namespace jmap.Tests;

public class MimeMessageReaderTests
{
    [Test]
    public void RecursivelyReadsNestedMultipartBodies()
    {
        string message =
            "Content-Type: multipart/mixed; boundary=outer\r\n\r\n" +
            "--outer\r\nContent-Type: multipart/alternative; boundary=inner\r\n\r\n" +
            "--inner\r\nContent-Type: text/plain\r\n\r\nplain\r\n" +
            "--inner\r\nContent-Type: text/html\r\n\r\n<p>html</p>\r\n" +
            "--inner--\r\n" +
            "--outer--\r\n";

        MimeMessageReadResult result = new MimeMessageReader().ReadMessage(Encoding.ASCII.GetBytes(message));
        MimeEntity alternative = result.Root!.SubParts![0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(alternative.Type, Is.EqualTo("multipart/alternative"));
            Assert.That(alternative.SubParts, Has.Count.EqualTo(2));
            Assert.That(alternative.SubParts![0].DecodedText, Is.EqualTo("plain"));
            Assert.That(alternative.SubParts[1].DecodedText, Is.EqualTo("<p>html</p>"));
            Assert.That(result.HasMalformedInput, Is.False);
            Assert.That(result.IsLimitExceeded, Is.False);
        }
    }

    [Test]
    public void ReadsMultipartTransferEncodingsAndContentMetadata()
    {
        string message =
            "MIME-Version: 1.0\r\n" +
            "Content-Type: multipart/mixed; boundary=outer\r\n" +
            "\r\n" +
            "preamble\r\n" +
            "--outer\r\n" +
            "Content-Type: text/plain; charset=iso-8859-1\r\n" +
            "Content-Transfer-Encoding: quoted-printable\r\n" +
            "Content-Language: en-US, fr\r\n" +
            "Content-Location: notes/readme.txt\r\n" +
            "\r\n" +
            "caf=E9\r\n" +
            "--outer\r\n" +
            "Content-Type: application/x-example\r\n" +
            "Content-Transfer-Encoding: base64\r\n" +
            "Content-Disposition: attachment; filename*0*=utf-8''caf%C3%A9; filename*1*=.bin\r\n" +
            "Content-ID: <part.2@example.test>\r\n" +
            "\r\n" +
            "AAEC\r\n" +
            "--outer--\r\n" +
            "epilogue";

        MimeMessageReadResult result = new MimeMessageReader().ReadMessage(Encoding.ASCII.GetBytes(message));
        MimeEntity root = result.Root!;
        MimeEntity text = root.SubParts![0];
        MimeEntity binary = root.SubParts[1];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.HasMalformedInput, Is.False);
            Assert.That(result.IsLimitExceeded, Is.False);
            Assert.That(result.Headers[0].Name, Is.EqualTo("MIME-Version"));
            Assert.That(root.Type, Is.EqualTo("multipart/mixed"));
            Assert.That(root.SubParts, Has.Count.EqualTo(2));
            Assert.That(text.Type, Is.EqualTo("text/plain"));
            Assert.That(text.Charset, Is.EqualTo("iso-8859-1"));
            Assert.That(text.DecodedText, Is.EqualTo("café"));
            Assert.That(text.Language, Is.EqualTo(new[] { "en-US", "fr" }));
            Assert.That(text.Location, Is.EqualTo("notes/readme.txt"));
            Assert.That(binary.Type, Is.EqualTo("application/x-example"));
            Assert.That(binary.Disposition, Is.EqualTo("attachment"));
            Assert.That(binary.Name, Is.EqualTo("café.bin"));
            Assert.That(binary.ContentId, Is.EqualTo("part.2@example.test"));
            Assert.That(binary.DecodedBody, Is.EqualTo(new byte[] { 0, 1, 2 }));
            Assert.That(binary.DecodedText, Is.Null);
        }
    }

    [Test]
    public void ReadsTransferEncodedEmbeddedMessage()
    {
        string embedded =
            "From: sender@example.test\r\n" +
            "Subject: nested\r\n" +
            "Content-Type: text/plain; charset=utf-8\r\n" +
            "Content-Transfer-Encoding: 8bit\r\n" +
            "\r\n" +
            "Snowman: ☃";
        string message =
            "Content-Type: message/rfc822\r\n" +
            "Content-Transfer-Encoding: base64\r\n" +
            "\r\n" +
            Convert.ToBase64String(Encoding.UTF8.GetBytes(embedded));

        MimeMessageReadResult result = new MimeMessageReader().ReadMessage(Encoding.ASCII.GetBytes(message));
        MimeEntity outer = result.Root!;
        MimeMessageReadResult nested = outer.EmbeddedMessage!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.HasMalformedInput, Is.False);
            Assert.That(outer.Type, Is.EqualTo("message/rfc822"));
            Assert.That(outer.SubParts, Is.Null);
            Assert.That(nested.Headers.Select(header => header.Name),
                Is.EqualTo(new[] { "From", "Subject", "Content-Type", "Content-Transfer-Encoding" }));
            Assert.That(nested.Root!.DecodedText, Is.EqualTo("Snowman: ☃"));
        }
    }

    [Test]
    public void KeepsUnknownMediaAndTransferEncodingOpaqueWhileReportingProblem()
    {
        string message =
            "Content-Type: model/vnd.example\r\n" +
            "Content-Transfer-Encoding: x-private\r\n" +
            "\r\n" +
            "opaque bytes";

        MimeMessageReadResult result = new MimeMessageReader().ReadMessage(Encoding.ASCII.GetBytes(message));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Root!.Type, Is.EqualTo("model/vnd.example"));
            Assert.That(Encoding.ASCII.GetString(result.Root.DecodedBody), Is.EqualTo("opaque bytes"));
            Assert.That(result.Root.DecodedText, Is.Null);
            Assert.That(result.Root.IsEncodingProblem, Is.True);
            Assert.That(result.HasMalformedInput, Is.True);
        }
    }

    [Test]
    public void ReportsMalformedBase64WithoutThrowing()
    {
        string message =
            "Content-Type: application/octet-stream\r\n" +
            "Content-Transfer-Encoding: base64\r\n" +
            "\r\n" +
            "YWJj$ZA";

        MimeMessageReadResult result = new MimeMessageReader().ReadMessage(Encoding.ASCII.GetBytes(message));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Encoding.ASCII.GetString(result.Root!.DecodedBody), Is.EqualTo("abcd"));
            Assert.That(result.Root.IsEncodingProblem, Is.True);
            Assert.That(result.HasMalformedInput, Is.True);
        }
    }

    [Test]
    public void RemovesQuotedPrintableTrailingWhitespaceButPreservesEncodedWhitespace()
    {
        string message =
            "Content-Type: text/plain; charset=us-ascii\r\n" +
            "Content-Transfer-Encoding: quoted-printable\r\n" +
            "\r\n" +
            "first  \r\nsecond=20\r\nthird";

        MimeMessageReadResult result = new MimeMessageReader().ReadMessage(Encoding.ASCII.GetBytes(message));

        Assert.That(result.Root!.DecodedText, Is.EqualTo("first\r\nsecond \r\nthird"));
        Assert.That(result.HasMalformedInput, Is.False);
    }

    [Test]
    public void FlagsBrokenRfc2231ContinuationWhileRetainingAvailableName()
    {
        string message =
            "Content-Type: application/octet-stream\r\n" +
            "Content-Disposition: attachment; filename*0*=utf-8''first; filename*2*=third\r\n" +
            "\r\nbody";

        MimeMessageReadResult result = new MimeMessageReader().ReadMessage(Encoding.ASCII.GetBytes(message));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Root!.Name, Is.EqualTo("first"));
            Assert.That(result.Root.HasMetadataEncodingProblem, Is.True);
            Assert.That(result.HasMalformedInput, Is.True);
        }
    }

    [Test]
    public void EnforcesMessageDecodedPartCountAndNestingLimits()
    {
        MimeMessageReader messageReader = new MimeMessageReader();
        messageReader.Limits.MaximumMessageBytes = 4;
        MimeMessageReadResult oversized = messageReader.ReadMessage(Encoding.ASCII.GetBytes("12345"));

        MimeMessageReader decodedReader = new MimeMessageReader();
        decodedReader.Limits.MaximumDecodedPartBytes = 4;
        MimeMessageReadResult truncated = decodedReader.ReadMessage(Encoding.ASCII.GetBytes(
            "Content-Type: text/plain; charset=us-ascii\r\nContent-Transfer-Encoding: base64\r\n\r\nYWJjZGVm"));

        MimeMessageReader depthReader = new MimeMessageReader();
        depthReader.Limits.MaximumDepth = 0;
        MimeMessageReadResult tooDeep = depthReader.ReadMessage(
            AdversarialFixtures.NestedMessageRfc822(1));

        MimeMessageReader partReader = new MimeMessageReader();
        partReader.Limits.MaximumParts = 2;
        MimeMessageReadResult tooMany = partReader.ReadMessage(Encoding.ASCII.GetBytes(
            "Content-Type: multipart/mixed; boundary=b\r\n\r\n" +
            "--b\r\n\r\none\r\n--b\r\n\r\ntwo\r\n--b--\r\n"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(oversized.Root, Is.Null);
            Assert.That(oversized.IsLimitExceeded, Is.True);
            Assert.That(Encoding.ASCII.GetString(truncated.Root!.DecodedBody), Is.EqualTo("abcd"));
            Assert.That(truncated.Root.DecodedText, Is.EqualTo("abcd"));
            Assert.That(truncated.Root.IsTruncated, Is.True);
            Assert.That(truncated.IsLimitExceeded, Is.True);
            Assert.That(tooDeep.Root!.EmbeddedMessage!.Root, Is.Null);
            Assert.That(tooDeep.IsLimitExceeded, Is.True);
            Assert.That(tooMany.Root!.SubParts, Has.Count.EqualTo(1));
            Assert.That(tooMany.IsLimitExceeded, Is.True);
        }
    }

    [Test]
    public void UsesDigestDefaultAndFlagsMissingClosingBoundary()
    {
        string message =
            "Content-Type: multipart/digest; boundary=d\r\n\r\n" +
            "--d\r\n" +
            "From: sender@example.test\r\nSubject: digest item\r\n\r\nbody";

        MimeMessageReadResult result = new MimeMessageReader().ReadMessage(Encoding.ASCII.GetBytes(message));
        MimeEntity item = result.Root!.SubParts![0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.Type, Is.EqualTo("message/rfc822"));
            Assert.That(item.EmbeddedMessage!.Root!.DecodedText, Is.EqualTo("body"));
            Assert.That(result.Root.IsEncodingProblem, Is.True);
            Assert.That(result.HasMalformedInput, Is.True);
        }
    }
}
