using System.Text;

namespace jmap.Tests;

public class BodyPartMapperTests
{
    [Test]
    public void MapsStructureAlternativeRelatedAttachmentsAndPartIdentifiers()
    {
        string message =
            "Content-Type: multipart/mixed; boundary=outer\r\n\r\n" +
            "--outer\r\nContent-Type: text/plain\r\nContent-Disposition: inline\r\n\r\nOpening\r\n" +
            "--outer\r\nContent-Type: multipart/alternative; boundary=alternative\r\n\r\n" +
            "--alternative\r\nContent-Type: text/plain\r\n\r\nplain choice\r\n" +
            "--alternative\r\nContent-Type: text/html\r\n\r\n<p>html choice</p>\r\n" +
            "--alternative--\r\n" +
            "--outer\r\nContent-Type: multipart/related; boundary=related\r\n\r\n" +
            "--related\r\nContent-Type: text/html\r\n\r\n<p>Related body</p>\r\n" +
            "--related\r\nContent-Type: image/png\r\nContent-Disposition: inline\r\n" +
            "Content-ID: <image@example.test>\r\nContent-Transfer-Encoding: base64\r\n\r\nAAEC\r\n" +
            "--related--\r\n" +
            "--outer\r\nContent-Type: application/pdf\r\n" +
            "Content-Disposition: attachment; filename=report.pdf\r\n\r\nPDF\r\n" +
            "--outer\r\nContent-Type: text/plain\r\n" +
            "Content-Disposition: attachment; filename=notes.txt\r\n\r\nnotes\r\n" +
            "--outer\r\nContent-Type: message/rfc822\r\n\r\n" +
            "From: nested@example.test\r\nContent-Type: text/plain\r\n\r\nnested body\r\n" +
            "--outer--\r\n";

        MimeMessageReadResult parsed = new MimeMessageReader().ReadMessage(Encoding.ASCII.GetBytes(message));
        BodyPartMappingResult mapped = new BodyPartMapper().MapMessage(parsed, 0);
        EmailBodyPart root = mapped.BodyStructure!;
        EmailBodyPart alternative = root.SubParts![1];
        EmailBodyPart related = root.SubParts[2];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.PartId, Is.Null);
            Assert.That(root.BlobId, Is.Null);
            Assert.That(root.HasSize, Is.True);
            Assert.That(root.Type, Is.EqualTo("multipart/mixed"));
            Assert.That(root.SubParts, Has.Count.EqualTo(6));
            Assert.That(root.SubParts[0].PartId, Is.EqualTo("1"));
            Assert.That(alternative.PartId, Is.Null);
            Assert.That(alternative.SubParts![0].PartId, Is.EqualTo("2.1"));
            Assert.That(alternative.SubParts[1].PartId, Is.EqualTo("2.2"));
            Assert.That(related.SubParts![1].PartId, Is.EqualTo("3.2"));
            Assert.That(root.SubParts[5].PartId, Is.EqualTo("6"));
            Assert.That(root.SubParts[5].SubParts, Is.Null);
            Assert.That(root.SubParts[0].Charset, Is.EqualTo("us-ascii"));
            Assert.That(related.Charset, Is.Null);
            Assert.That(related.SubParts[1].Cid, Is.EqualTo("image@example.test"));
            Assert.That(related.SubParts[1].BlobId, Does.StartWith("m"));
            Assert.That(related.SubParts[1].HasSize, Is.True);
            Assert.That(related.SubParts[1].Size, Is.EqualTo(3));
            Assert.That(mapped.TextBody.Select(part => part.PartId), Is.EqualTo(new[] { "1", "2.1", "3.1" }));
            Assert.That(mapped.HtmlBody.Select(part => part.PartId), Is.EqualTo(new[] { "1", "2.2", "3.1" }));
            Assert.That(mapped.Attachments.Select(part => part.PartId), Is.EqualTo(new[] { "3.2", "4", "5", "6" }));
            Assert.That(mapped.HasAttachment, Is.True);
            Assert.That(mapped.BodyValues.Keys, Is.EquivalentTo(new[] { "1", "2.1", "2.2", "3.1", "5" }));
            Assert.That(mapped.TextBody[0], Is.SameAs(root.SubParts[0]));
            Assert.That(mapped.Attachments[0], Is.SameAs(related.SubParts[1]));
            Assert.That(mapped.Preview, Is.EqualTo("Opening"));
        }
    }

    [Test]
    public void FallsBackToAvailableAlternativeForBothBodyPreferences()
    {
        string message =
            "Content-Type: multipart/alternative; boundary=a\r\n\r\n" +
            "--a\r\nContent-Type: text/html\r\n\r\n<p>only html</p>\r\n--a--\r\n";

        MimeMessageReadResult parsed = new MimeMessageReader().ReadMessage(Encoding.ASCII.GetBytes(message));
        BodyPartMappingResult mapped = new BodyPartMapper().MapMessage(parsed, 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mapped.TextBody, Has.Count.EqualTo(1));
            Assert.That(mapped.HtmlBody, Has.Count.EqualTo(1));
            Assert.That(mapped.TextBody[0], Is.SameAs(mapped.HtmlBody[0]));
            Assert.That(mapped.TextBody[0].Type, Is.EqualTo("text/html"));
            Assert.That(mapped.Preview, Is.EqualTo("only html"));
            Assert.That(mapped.Attachments, Is.Empty);
            Assert.That(mapped.HasAttachment, Is.False);
        }
    }

    [Test]
    public void NormalizesBodyValuesAndTruncatesOnlyAtUtf8Boundaries()
    {
        string message =
            "Content-Type: text/plain; charset=utf-8\r\n" +
            "Content-Transfer-Encoding: 8bit\r\n\r\n" +
            "ab\r\né🙂z\rnext";

        MimeMessageReadResult parsed = new MimeMessageReader().ReadMessage(Encoding.UTF8.GetBytes(message));
        BodyPartMappingResult mapped = new BodyPartMapper().MapMessage(parsed, 8);
        EmailBodyValue value = mapped.BodyValues["1"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(value.Value, Is.EqualTo("ab\né"));
            Assert.That(Encoding.UTF8.GetByteCount(value.Value!), Is.EqualTo(5));
            Assert.That(value.IsTruncated, Is.True);
            Assert.That(value.IsEncodingProblem, Is.False);
            Assert.That(mapped.Preview, Is.EqualTo("ab é🙂z next"));
        }
    }

    [Test]
    public void PropagatesEncodingProblemsIntoBodyValues()
    {
        string message =
            "Content-Type: text/plain; charset=utf-8\r\n" +
            "Content-Transfer-Encoding: base64\r\n\r\n" +
            "wyg=";

        MimeMessageReadResult parsed = new MimeMessageReader().ReadMessage(Encoding.ASCII.GetBytes(message));
        BodyPartMappingResult mapped = new BodyPartMapper().MapMessage(parsed, 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mapped.BodyValues["1"].Value, Is.EqualTo("�("));
            Assert.That(mapped.BodyValues["1"].IsEncodingProblem, Is.True);
            Assert.That(mapped.BodyValues["1"].IsTruncated, Is.False);
        }
    }

    [Test]
    public void DoesNotTreatMalformedFilenameEncodingAsABodyEncodingProblem()
    {
        string message =
            "Content-Type: text/plain; charset=us-ascii\r\n" +
            "Content-Disposition: attachment; filename*=x-unknown''name.txt\r\n\r\n" +
            "body";

        MimeMessageReadResult parsed = new MimeMessageReader().ReadMessage(Encoding.ASCII.GetBytes(message));
        BodyPartMappingResult mapped = new BodyPartMapper().MapMessage(parsed, 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(parsed.Root!.HasMetadataEncodingProblem, Is.True);
            Assert.That(mapped.BodyValues["1"].IsEncodingProblem, Is.False);
        }
    }

    [Test]
    public void AvoidsTruncatingHtmlInsideATagAndUsesFullTextForPreview()
    {
        string message =
            "Content-Type: text/html; charset=us-ascii\r\n\r\n" +
            "<p>ok</p><a href='long'>next &amp; last</a>";

        MimeMessageReadResult parsed = new MimeMessageReader().ReadMessage(Encoding.ASCII.GetBytes(message));
        BodyPartMappingResult mapped = new BodyPartMapper().MapMessage(parsed, 15);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mapped.BodyValues["1"].Value, Is.EqualTo("<p>ok</p>"));
            Assert.That(mapped.BodyValues["1"].IsTruncated, Is.True);
            Assert.That(mapped.Preview, Is.EqualTo("ok next & last"));
        }
    }

    [Test]
    public void LimitsPreviewByUnicodeCharactersWithoutSplittingSurrogates()
    {
        string body = new string('a', 255) + "🙂tail";
        string message =
            "Content-Type: text/plain; charset=utf-8\r\n" +
            "Content-Transfer-Encoding: 8bit\r\n\r\n" + body;

        MimeMessageReadResult parsed = new MimeMessageReader().ReadMessage(Encoding.UTF8.GetBytes(message));
        BodyPartMappingResult mapped = new BodyPartMapper().MapMessage(parsed, 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mapped.Preview, Does.EndWith("🙂"));
            Assert.That(mapped.Preview.Length, Is.EqualTo(257));
            Assert.That(mapped.Preview, Does.Not.EndWith("tail"));
        }
    }

    [Test]
    public void TreatsInlineOnlyRelatedMediaAsNotDownloadable()
    {
        string message =
            "Content-Type: multipart/related; boundary=r\r\n\r\n" +
            "--r\r\nContent-Type: text/html\r\n\r\n<img src='cid:i'>\r\n" +
            "--r\r\nContent-Type: image/png\r\nContent-Disposition: inline\r\n\r\nimage\r\n" +
            "--r--\r\n";

        MimeMessageReadResult parsed = new MimeMessageReader().ReadMessage(Encoding.ASCII.GetBytes(message));
        BodyPartMappingResult mapped = new BodyPartMapper().MapMessage(parsed, 0);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mapped.Attachments, Has.Count.EqualTo(1));
            Assert.That(mapped.Attachments[0].Type, Is.EqualTo("image/png"));
            Assert.That(mapped.Attachments[0].Disposition, Is.EqualTo("inline"));
            Assert.That(mapped.HasAttachment, Is.False);
        }
    }
}
