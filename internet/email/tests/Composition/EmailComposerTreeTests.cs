using System.Text;
using email;

namespace email.Tests;

public class EmailComposerTreeTests
{
    [Test]
    public void AutomaticTreeUsesStableBodyResourceAndAttachmentOrdering()
    {
        EmailComposer composer = RelaxedComposer();
        composer.SetText("plain");
        composer.SetWatchHtml("watch");
        composer.SetAmpHtml("amp");
        composer.SetHtml("html");
        composer.SetCalendar("BEGIN:VCALENDAR\r\nEND:VCALENDAR\r\n", "PUBLISH");
        composer.AddAlternative("last", "application", "x-last", "us-ascii");
        composer.AddRelatedResource(Resource("first.png", "images/first.png", "first"));
        composer.AddRelatedResource(Resource("second.png", "images/second.png", "second"));
        composer.AddAttachment(Attachment("first.txt", "first attachment"));
        composer.AddAttachment(Attachment("second.json", "second attachment"));

        MimeEntity mixed = composer.Build().GetBody()!;
        MimeEntity related = mixed!.GetChildAt(0);
        MimeEntity alternatives = related.GetChildAt(0);

        Assert.That(mixed.GetMediaType()!.IsType("multipart", "mixed"), Is.True);
        Assert.That(mixed.GetChildCount(), Is.EqualTo(3));
        Assert.That(related.GetMediaType()!.IsType("multipart", "related"), Is.True);
        Assert.That(related.GetChildCount(), Is.EqualTo(3));
        Assert.That(alternatives.GetMediaType()!.IsType("multipart", "alternative"), Is.True);
        Assert.That(alternatives.GetChildCount(), Is.EqualTo(6));
        Assert.That(alternatives.GetChildAt(0).GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(alternatives.GetChildAt(1).GetMediaType()!.IsType("text", "watch-html"), Is.True);
        Assert.That(alternatives.GetChildAt(2).GetMediaType()!.IsType("text", "x-amp-html"), Is.True);
        Assert.That(alternatives.GetChildAt(3).GetMediaType()!.IsType("text", "html"), Is.True);
        Assert.That(alternatives.GetChildAt(4).GetMediaType()!.IsType("text", "calendar"), Is.True);
        Assert.That(alternatives.GetChildAt(5).GetMediaType()!.IsType("application", "x-last"), Is.True);
        Assert.That(new EmailFilename().Decode(related.GetChildAt(1)), Is.EqualTo("first.png"));
        Assert.That(new EmailFilename().Decode(related.GetChildAt(2)), Is.EqualTo("second.png"));
        Assert.That(new EmailFilename().Decode(mixed.GetChildAt(1)), Is.EqualTo("first.txt"));
        Assert.That(new EmailFilename().Decode(mixed.GetChildAt(2)), Is.EqualTo("second.json"));
    }

    [Test]
    public void AutomaticTreeAddsOnlyTheWrappersRequiredByItsContent()
    {
        EmailComposer bodyOnly = RelaxedComposer();
        Assert.That(bodyOnly.Build().GetBody()!.GetKind(), Is.EqualTo(MimeEntityKind.Leaf));

        EmailComposer attachmentOnly = RelaxedComposer();
        attachmentOnly.AddAttachment(Attachment("file.bin", "attachment"));
        MimeEntity mixed = attachmentOnly.Build().GetBody()!;
        Assert.That(mixed!.GetMediaType()!.IsType("multipart", "mixed"), Is.True);
        Assert.That(mixed.GetChildAt(0).GetKind(), Is.EqualTo(MimeEntityKind.Leaf));

        EmailComposer resourceOnly = RelaxedComposer();
        resourceOnly.AddRelatedResource(Resource("logo.png", "logo.png", "resource"));
        MimeEntity related = resourceOnly.Build().GetBody()!;
        Assert.That(related!.GetMediaType()!.IsType("multipart", "related"), Is.True);
        Assert.That(related.GetChildAt(0).GetKind(), Is.EqualTo(MimeEntityKind.Leaf));

        EmailComposer both = RelaxedComposer();
        both.AddRelatedResource(Resource("logo.png", "logo.png", "resource"));
        both.AddAttachment(Attachment("file.bin", "attachment"));
        MimeEntity outer = both.Build().GetBody()!;
        Assert.That(outer!.GetMediaType()!.IsType("multipart", "mixed"), Is.True);
        Assert.That(outer.GetChildAt(0).GetMediaType()!.IsType("multipart", "related"), Is.True);
    }

    [Test]
    public void RepeatedAutomaticBuildsHaveStableBoundariesAndIndependentTrees()
    {
        EmailComposer composer = RelaxedComposer();
        composer.SetText("plain");
        composer.SetHtml("html");
        composer.AddRelatedResource(Resource("logo.png", "logo.png", "resource"));
        composer.AddAttachment(Attachment("file.bin", "attachment"));

        EmailMessage first = composer.Build();
        EmailMessage second = composer.Build();
        MimeEntity firstMixed = first.GetBody()!;
        MimeEntity secondMixed = second.GetBody()!;

        Assert.That(Boundary(firstMixed!), Is.EqualTo(Boundary(secondMixed!)));
        Assert.That(Boundary(firstMixed.GetChildAt(0)), Is.EqualTo(Boundary(secondMixed.GetChildAt(0))));
        Assert.That(Boundary(firstMixed.GetChildAt(0).GetChildAt(0)), Is.EqualTo(Boundary(secondMixed.GetChildAt(0).GetChildAt(0))));

        EmailWriteOptions writeOptions = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        writeOptions.SetDeterministicOutput(true);
        writeOptions.SetDeterministicSeed(Encoding.ASCII.GetBytes("composer-tree-test"));
        Assert.That(new EmailWriter().ToArrayWithMessageAndOptions(first, writeOptions), Is.EqualTo(new EmailWriter().ToArrayWithMessageAndOptions(second, writeOptions)));

        firstMixed.RemoveChildAt(1);
        Assert.That(firstMixed.GetChildCount(), Is.EqualTo(1));
        Assert.That(secondMixed.GetChildCount(), Is.EqualTo(2));
        Assert.That(composer.Build().GetBody()!.GetChildCount(), Is.EqualTo(2));
    }

    [Test]
    public void RawEntityIsAnExactTreeEscapeHatch()
    {
        EmailComposer composer = RelaxedComposer();
        composer.SetText("managed");
        composer.AddRelatedResource(Resource("logo.png", "logo.png", "resource"));
        composer.AddAttachment(Attachment("file.bin", "attachment"));
        MimeEntity json = new EmailEntityFactory().CreateContent("json", "application", "json", "us-ascii");
        MimeEntity text = new EmailEntityFactory().CreateTextWithValue("text");
        MimeEntity exact = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("x-exact", new List<MimeEntity> { json, text });

        composer.SetRawEntity(exact);
        exact.MoveChild(0, 1);
        MimeEntity built = composer.Build().GetBody()!;

        Assert.That(built!.GetMediaType()!.IsType("multipart", "x-exact"), Is.True);
        Assert.That(built.GetChildCount(), Is.EqualTo(2));
        Assert.That(built.GetChildAt(0).GetMediaType()!.IsType("application", "json"), Is.True);
        Assert.That(built.GetChildAt(1).GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(Boundary(built), Is.Null);

        composer.ClearRawEntity();
        MimeEntity automatic = composer.Build().GetBody()!;
        Assert.That(automatic!.GetMediaType()!.IsType("multipart", "mixed"), Is.True);
        Assert.That(automatic.GetChildAt(0).GetMediaType()!.IsType("multipart", "related"), Is.True);
    }

    [Test]
    public void SuccessfulAutomaticAdditionLeavesExactModeWhileInvalidAdditionIsAtomic()
    {
        EmailComposer composer = RelaxedComposer();
        composer.SetText("managed");
        composer.SetRawEntity(new EmailEntityFactory().CreateContent("exact", "application", "x-exact", "us-ascii"));

        Assert.Throws<ArgumentException>(() => composer.AddRelatedResource(Attachment("invalid.bin", "invalid")));
        Assert.That(composer.Build().GetBody()!.GetMediaType()!.IsType("application", "x-exact"), Is.True);

        composer.AddAttachment(Attachment("file.bin", "attachment"));
        MimeEntity automatic = composer.Build().GetBody()!;
        Assert.That(automatic!.GetMediaType()!.IsType("multipart", "mixed"), Is.True);
        Assert.That(automatic.GetChildAt(0).GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(new EmailFilename().Decode(automatic.GetChildAt(1)), Is.EqualTo("file.bin"));
    }

    [Test]
    public void ExactTreeSizeIgnoresHiddenAutomaticSourcesAndFailedTransitionPreservesIt()
    {
        EmailComposerOptions options = RelaxedOptions();
        options.SetMaximumBodyBytes(3);
        EmailComposer composer = new EmailComposerOperations().CreateWithOptions(options);
        composer.SetText("12");
        composer.AddAttachment(Attachment("one.bin", "3"));
        composer.SetRawEntity(new EmailEntityFactory().CreateContent("raw", "application", "x-exact", "us-ascii"));

        Assert.That(composer.Build().GetBody()!.GetMediaType()!.IsType("application", "x-exact"), Is.True);
        Assert.Throws<EmailLimitException>(() => composer.AddAttachment(Attachment("two.bin", "4")));
        Assert.That(composer.Build().GetBody()!.GetMediaType()!.IsType("application", "x-exact"), Is.True);

        composer.ClearRawEntity();
        Assert.That(composer.Build().GetBody()!.GetMediaType()!.IsType("multipart", "mixed"), Is.True);
    }

    static EmailAttachmentSource Attachment(string filename, string content)
    {
        return new EmailAttachmentSourceOperations().CreateMemoryWithValueAndFilename(Encoding.ASCII.GetBytes(content), filename);
    }

    static EmailAttachmentSource Resource(string filename, string location, string content)
    {
        EmailAttachmentSource source = Attachment(filename, content);
        source.SetContentLocation(location);
        return source;
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

    static string? Boundary(MimeEntity entity)
    {
        EmailRfc2231DecodedParameter boundary = entity.GetMediaType()!.GetParameters().GetDecoded("boundary")!;
        return boundary == null ? null : Encoding.ASCII.GetString(boundary.GetDecodedBytes())!;
    }
}
