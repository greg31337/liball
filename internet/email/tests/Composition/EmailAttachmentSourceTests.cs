using System.Text;
using email;

namespace email.Tests;

public class EmailAttachmentSourceTests
{
    [Test]
    public void MemorySourceCopiesBytesAndInfersSafeAttachmentMetadata()
    {
        byte[] input = Encoding.ASCII.GetBytes("original");
        EmailAttachmentSource source = new EmailAttachmentSourceOperations().CreateMemoryWithValueAndFilename(input, "../images/photo.png");
        input[0] = (byte)'X';

        MimeEntity entity = source.CreateAttachmentEntity();

        Assert.That(source.GetContent().GetKind(), Is.EqualTo(EmailContentKind.Memory));
        Assert.That(source.GetContent().GetOwnership(), Is.EqualTo(EmailContentOwnership.Owned));
        Assert.That(Encoding.ASCII.GetString(entity.GetContent()!.Materialize(100)), Is.EqualTo("original"));
        Assert.That(source.GetFilename(), Is.EqualTo("photo.png"));
        Assert.That(entity.GetMediaType()!.IsType("image", "png"), Is.True);
        Assert.That(entity.GetDisposition()!.GetDispositionType(), Is.EqualTo("attachment"));
        Assert.That(new EmailFilename().Decode(entity), Is.EqualTo("photo.png"));
        Assert.That(entity.GetTransferEncodingName(), Is.EqualTo("base64"));
    }

    [Test]
    public void StreamSourcesPreservePositionAndCallerSelectedOwnership()
    {
        MemoryStream borrowedStream = new MemoryStream(Encoding.ASCII.GetBytes("skip-body"));
        borrowedStream.Position = 5;
        EmailAttachmentSource borrowed = new EmailAttachmentSourceOperations().CreateStreamWithValueAndOwnershipAndFilename(borrowedStream, EmailContentOwnership.Borrowed, "body.txt");

        Assert.That(borrowed.GetContent().GetLength(), Is.EqualTo(4));
        Assert.That(Encoding.ASCII.GetString(borrowed.GetContent().Materialize(100)), Is.EqualTo("body"));
        borrowed.GetContent().Close();
        Assert.That(borrowedStream.CanRead, Is.True);

        MemoryStream ownedStream = new MemoryStream(Encoding.ASCII.GetBytes("owned"));
        EmailAttachmentSource owned = new EmailAttachmentSourceOperations().CreateStreamWithValueAndOwnership(ownedStream, EmailContentOwnership.Owned);
        owned.GetContent().Close();
        Assert.That(ownedStream.CanRead, Is.False);
    }

    [Test]
    public void FileSourcesUseLocalBorrowedContentAndNeverTreatUrlsAsPaths()
    {
        string path = Path.Combine(Path.GetTempPath(), "email-attachment-" + Guid.NewGuid().ToString("N") + ".pdf");
        File.WriteAllBytes(path, Encoding.ASCII.GetBytes("pdf"));
        try
        {
            EmailAttachmentSource source = new EmailAttachmentSourceOperations().CreateFileWithPath(path);

            Assert.That(source.GetContent().GetKind(), Is.EqualTo(EmailContentKind.File));
            Assert.That(source.GetContent().GetOwnership(), Is.EqualTo(EmailContentOwnership.Borrowed));
            Assert.That(source.GetContent().GetFilePath(), Is.EqualTo(Path.GetFullPath(path)));
            Assert.That(source.GetFilename(), Is.EqualTo(Path.GetFileName(path)));
            Assert.That(source.GetMediaType().IsType("application", "pdf"), Is.True);
            source.GetContent().Close();
            Assert.That(File.Exists(path), Is.True);
        }
        finally
        {
            File.Delete(path);
        }

        Assert.Throws<ArgumentException>(() => new EmailAttachmentSourceOperations().CreateFileWithPath("https://example.test/file.pdf"));
        Assert.Throws<ArgumentException>(() => new EmailAttachmentSourceOperations().CreateFileWithPath("data:text/plain,hello"));
    }

    [Test]
    public void ExistingContentIsReusedAndClosedContentIsRejected()
    {
        EmailContent content = new EmailContentOperations().CreateMemoryCopy(Encoding.ASCII.GetBytes("shared"), false);
        EmailAttachmentSource source = new EmailAttachmentSourceOperations().CreateContentWithValueAndFilename(content, "shared.json");

        Assert.That(source.GetContent(), Is.SameAs(content));
        Assert.That(source.CreateAttachmentEntity().GetContent(), Is.SameAs(content));
        Assert.That(source.GetMediaType().IsType("application", "json"), Is.True);

        content.Close();
        Assert.Throws<ObjectDisposedException>(() => source.CreateAttachmentEntity());
        Assert.Throws<ObjectDisposedException>(() => new EmailAttachmentSourceOperations().CreateContentWithValue(content));
    }

    [Test]
    public void SourceMetadataCreatesInlineResourcesAndSupportsSpecializedLeafKinds()
    {
        EmailMessageId identifier = ParseContentId("<logo@example.test>");
        EmailAttachmentSource source = new EmailAttachmentSourceOperations().CreateMemoryWithValueAndFilename(Encoding.ASCII.GetBytes("svg"), "logo.svg");
        source.SetMediaType("image/png");
        source.SetContentId(identifier);
        source.SetContentLocation("images/logo.png");
        source.SetDescription("Company logo");

        MimeEntity resource = source.CreateResourceEntity();

        Assert.That(resource.GetKind(), Is.EqualTo(MimeEntityKind.Leaf));
        Assert.That(resource.GetMediaType()!.IsType("image", "png"), Is.True);
        Assert.That(resource.GetDisposition()!.GetDispositionType(), Is.EqualTo("inline"));
        Assert.That(resource.GetContentId(), Is.SameAs(identifier));
        Assert.That(resource.GetContentLocation(), Is.EqualTo("images/logo.png"));
        Assert.That(resource.GetContentDescription(), Is.EqualTo("Company logo"));
        Assert.That(new EmailFilename().Decode(resource), Is.EqualTo("logo.svg"));

        EmailAttachmentSource calendar = new EmailAttachmentSourceOperations().CreateMemoryWithValueAndFilename(Encoding.ASCII.GetBytes("BEGIN:VCALENDAR"), "invite.ics");
        Assert.That(calendar.CreateAttachmentEntity().GetKind(), Is.EqualTo(MimeEntityKind.Calendar));
        Assert.Throws<ArgumentException>(() => calendar.SetMediaType("message/rfc822"));
        Assert.Throws<InvalidOperationException>(() => new EmailAttachmentSourceOperations().CreateMemoryWithValueAndFilename(new byte[0], "resource.bin").CreateResourceEntity());
    }

    [Test]
    public void ComposerSnapshotsSourcesAndPlacesResourcesAndAttachments()
    {
        EmailComposer composer = RelaxedComposer();
        composer.SetText("plain");
        composer.SetHtml("<img src=\"cid:logo@example.test\">");

        EmailAttachmentSource resource = new EmailAttachmentSourceOperations().CreateMemoryWithValueAndFilename(Encoding.ASCII.GetBytes("logo"), "logo.png");
        resource.SetContentId(ParseContentId("<logo@example.test>"));
        composer.AddRelatedResource(resource);
        resource.SetFilename("changed.gif");

        EmailAttachmentSource attachment = new EmailAttachmentSourceOperations().CreateMemoryWithValueAndFilename(Encoding.ASCII.GetBytes("report"), "report.txt");
        composer.AddAttachment(attachment);
        attachment.SetFilename("changed.pdf");

        MimeEntity root = composer.Build().GetBody()!;

        Assert.That(root!.GetMediaType()!.IsType("multipart", "mixed"), Is.True);
        Assert.That(root.GetChildCount(), Is.EqualTo(2));
        MimeEntity related = root.GetChildAt(0);
        Assert.That(related.GetMediaType()!.IsType("multipart", "related"), Is.True);
        Assert.That(related.GetChildCount(), Is.EqualTo(2));
        Assert.That(related.GetChildAt(0).GetMediaType()!.IsType("multipart", "alternative"), Is.True);
        Assert.That(new EmailFilename().Decode(related.GetChildAt(1)), Is.EqualTo("logo.png"));
        Assert.That(related.GetChildAt(1).GetDisposition()!.GetDispositionType(), Is.EqualTo("inline"));
        Assert.That(new EmailFilename().Decode(root.GetChildAt(1)), Is.EqualTo("report.txt"));
        Assert.That(root.GetChildAt(1).GetDisposition()!.GetDispositionType(), Is.EqualTo("attachment"));
        Assert.That(composer.Validate().IsValid(), Is.True);
    }

    [Test]
    public void ComposerRejectsInvalidResourcesAndEnforcesCountAndAggregateSizeLimits()
    {
        EmailComposerOptions countOptions = RelaxedOptions();
        countOptions.SetMaximumAttachmentCount(1);
        EmailComposer countLimited = new EmailComposerOperations().CreateWithOptions(countOptions);
        countLimited.AddAttachment(new EmailAttachmentSourceOperations().CreateMemoryWithValueAndFilename(new byte[0], "one.bin"));
        EmailAttachmentSource related = new EmailAttachmentSourceOperations().CreateMemoryWithValueAndFilename(new byte[0], "logo.png");
        related.SetContentLocation("logo.png");
        Assert.Throws<EmailLimitException>(() => countLimited.AddRelatedResource(related));
        Assert.Throws<ArgumentException>(() => RelaxedComposer().AddRelatedResource(new EmailAttachmentSourceOperations().CreateMemoryWithValueAndFilename(new byte[0], "missing.bin")));
        Assert.Throws<ArgumentOutOfRangeException>(() => countOptions.SetMaximumAttachmentCount(0));

        EmailComposerOptions sizeOptions = RelaxedOptions();
        sizeOptions.SetMaximumBodyBytes(3);
        EmailComposer sizeLimited = new EmailComposerOperations().CreateWithOptions(sizeOptions);
        sizeLimited.SetText("12");
        sizeLimited.AddAttachment(new EmailAttachmentSourceOperations().CreateMemoryWithValueAndFilename(Encoding.ASCII.GetBytes("3"), "three.txt"));
        Assert.Throws<EmailLimitException>(() => sizeLimited.AddAttachment(new EmailAttachmentSourceOperations().CreateMemoryWithValueAndFilename(Encoding.ASCII.GetBytes("4"), "four.txt")));
        Assert.Throws<EmailLimitException>(() => sizeLimited.SetText("123"));
        Assert.That(sizeLimited.Build().GetBody()!.GetChildCount(), Is.EqualTo(2));

        countLimited.ClearAttachments();
        countLimited.AddRelatedResource(related);
        countLimited.ClearRelatedResources();
        Assert.That(countLimited.Build().GetBody()!.GetKind(), Is.EqualTo(MimeEntityKind.Leaf));
    }

    static EmailComposer RelaxedComposer()
    {
        return new EmailComposerOperations().CreateWithOptions(RelaxedOptions());
    }

    static EmailComposerOptions RelaxedOptions()
    {
        EmailComposerOptions options = new EmailComposerOptions();
        options.SetRequireFrom(false);
        options.SetRequireDate(false);
        options.SetRequireMessageId(false);
        options.SetRequireRecipient(false);
        return options;
    }

    static EmailMessageId ParseContentId(string value)
    {
        EmailMessageIdParseResult result = new EmailMessageIdParser().ParseContentIdWithInputAndMode(Encoding.ASCII.GetBytes(value), EmailMessageIdParseMode.Strict);
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetIdentifier()!;
    }
}
