using System.Text;
using email;

namespace email.Tests;

public class EmailComposerBodyTests
{
    [Test]
    public void TextBodyUsesExplicitCharsetAndRemainsSinglePart()
    {
        EmailComposer composer = RelaxedComposer();
        composer.SetTextWithCharset("héllo", "utf-8");

        EmailMessage message = composer.Build();
        MimeEntity body = message.GetBody()!;

        Assert.That(body!.GetKind(), Is.EqualTo(MimeEntityKind.Leaf));
        Assert.That(body.GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(Parameter(body, "charset"), Is.EqualTo("utf-8"));
        Assert.That(ContentText(body), Is.EqualTo("héllo"));
        Assert.That(body.GetTransferEncodingName(), Is.EqualTo("8bit"));
        Assert.That(composer.Validate().IsValid(), Is.True);
    }

    [Test]
    public void StandardRepresentationsBuildStableMultipartAlternativeOrder()
    {
        EmailComposer composer = RelaxedComposer();
        composer.SetText("plain");
        composer.SetWatchHtml("<p>watch</p>");
        composer.SetAmpHtml("<html amp4email>amp</html>");
        composer.SetHtml("<p>html</p>");
        composer.SetCalendarWithCharset("BEGIN:VCALENDAR\r\nMETHOD:REQUEST\r\nEND:VCALENDAR\r\n", "request", "utf-8");

        MimeEntity body = composer.Build().GetBody()!;

        Assert.That(body!.GetKind(), Is.EqualTo(MimeEntityKind.Multipart));
        Assert.That(body.GetMediaType()!.IsType("multipart", "alternative"), Is.True);
        Assert.That(Parameter(body, "boundary"), Is.Not.Empty);
        Assert.That(body.GetChildCount(), Is.EqualTo(5));
        Assert.That(body.GetChildAt(0).GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(body.GetChildAt(1).GetMediaType()!.IsType("text", "watch-html"), Is.True);
        Assert.That(body.GetChildAt(2).GetMediaType()!.IsType("text", "x-amp-html"), Is.True);
        Assert.That(body.GetChildAt(3).GetMediaType()!.IsType("text", "html"), Is.True);
        Assert.That(body.GetChildAt(4).GetKind(), Is.EqualTo(MimeEntityKind.Calendar));
        Assert.That(body.GetChildAt(4).GetMediaType()!.IsType("text", "calendar"), Is.True);
        Assert.That(Parameter(body.GetChildAt(4), "method"), Is.EqualTo("REQUEST"));
        Assert.That(composer.Validate().IsValid(), Is.True);
    }

    [Test]
    public void ExplicitContentAndArbitraryAlternativesPreserveCallerOrder()
    {
        EmailComposer composer = RelaxedComposer();
        composer.SetContent("{\"ok\":true}", "application", "json", "utf-8");
        composer.AddAlternative("<note/>", "application", "xml", "utf-8");
        MimeEntity custom = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("markdown", "markdown", "utf-8");
        composer.AddAlternativeEntity(custom);

        MimeEntity body = composer.Build().GetBody()!;

        Assert.That(body!.GetChildCount(), Is.EqualTo(3));
        Assert.That(body.GetChildAt(0).GetMediaType()!.IsType("application", "json"), Is.True);
        Assert.That(body.GetChildAt(1).GetMediaType()!.IsType("application", "xml"), Is.True);
        Assert.That(body.GetChildAt(2).GetMediaType()!.IsType("text", "markdown"), Is.True);
        Assert.That(ContentText(body.GetChildAt(0)), Is.EqualTo("{\"ok\":true}"));
        Assert.That(custom.HasParent(), Is.False);
    }

    [Test]
    public void RawEntityIsClonedAndActsAsExactBodyOverride()
    {
        EmailComposer composer = RelaxedComposer();
        composer.SetText("managed");
        MimeEntity raw = new EmailEntityFactory().CreateContent("raw", "application", "x-custom", "utf-8");
        composer.SetRawEntity(raw);
        raw.ReplaceContent(new EmailContentOperations().CreateMemoryCopy(Encoding.UTF8.GetBytes("changed"), false));

        MimeEntity first = composer.Build().GetBody()!;
        Assert.That(first!.GetMediaType()!.IsType("application", "x-custom"), Is.True);
        Assert.That(ContentText(first), Is.EqualTo("raw"));
        Assert.That(first, Is.Not.SameAs(raw));

        composer.ClearRawEntity();
        Assert.That(composer.Build().GetBody()!.GetMediaType()!.IsType("text", "plain"), Is.True);

        composer.SetRawEntity(raw);
        composer.SetHtml("<p>managed again</p>");
        MimeEntity managed = composer.Build().GetBody()!;
        Assert.That(managed!.GetMediaType()!.IsType("multipart", "alternative"), Is.True);
        Assert.That(managed.GetChildAt(1).GetMediaType()!.IsType("text", "html"), Is.True);
    }

    [Test]
    public void ClearingRepresentationsAndBodyRestoresSafeEmptyTextDefault()
    {
        EmailComposer composer = RelaxedComposer();
        composer.SetHtml("<p>html</p>");
        composer.AddAlternative("extra", "text", "x-extra", "utf-8");
        composer.ClearHtml();
        composer.ClearAlternatives();
        composer.ClearText();

        MimeEntity fallback = composer.Build().GetBody()!;
        Assert.That(fallback!.GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(ContentText(fallback), Is.Empty);

        composer.SetHtml("<p>again</p>");
        composer.ClearBody();
        MimeEntity cleared = composer.Build().GetBody()!;
        Assert.That(cleared!.GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(ContentText(cleared), Is.Empty);
    }

    [Test]
    public void AlternativeBoundariesAndSerializedOutputAreDeterministic()
    {
        EmailComposer composer = RelaxedComposer();
        composer.SetText("plain");
        composer.SetHtml("<p>html</p>");

        EmailMessage first = composer.Build();
        EmailMessage second = composer.Build();
        EmailWriteOptions writeOptions = new EmailWriteOptionsOperations().CreateCanonicalInternet();
        writeOptions.SetDeterministicOutput(true);
        writeOptions.SetDeterministicSeed(Encoding.ASCII.GetBytes("composer-test"));
        byte[] firstBytes = new EmailWriter().ToArrayWithMessageAndOptions(first, writeOptions);
        byte[] secondBytes = new EmailWriter().ToArrayWithMessageAndOptions(second, writeOptions);

        Assert.That(Parameter(first.GetBody()!, "boundary"), Is.EqualTo(Parameter(second.GetBody()!, "boundary")));
        Assert.That(firstBytes, Is.EqualTo(secondBytes));
    }

    [Test]
    public void BodyAndAlternativeLimitsRejectBeforeChangingState()
    {
        EmailComposerOptions alternativeOptions = RelaxedOptions();
        alternativeOptions.SetMaximumAlternativeCount(1);
        EmailComposer alternativeLimited = new EmailComposerOperations().CreateWithOptions(alternativeOptions);
        Assert.Throws<EmailLimitException>(() => alternativeLimited.SetHtml("html"));
        Assert.That(alternativeLimited.Build().GetBody()!.GetMediaType()!.IsType("text", "plain"), Is.True);

        EmailComposerOptions bodyOptions = RelaxedOptions();
        bodyOptions.SetMaximumBodyBytes(3);
        EmailComposer bodyLimited = new EmailComposerOperations().CreateWithOptions(bodyOptions);
        Assert.Throws<EmailLimitException>(() => bodyLimited.SetText("four"));
        Assert.That(ContentText(bodyLimited.Build().GetBody()!), Is.Empty);

        EmailComposerOptions aggregateOptions = RelaxedOptions();
        aggregateOptions.SetMaximumBodyBytes(5);
        EmailComposer aggregateLimited = new EmailComposerOperations().CreateWithOptions(aggregateOptions);
        aggregateLimited.SetText("123");
        Assert.Throws<EmailLimitException>(() => aggregateLimited.AddAlternative("456", "text", "x-extra", "us-ascii"));
        Assert.That(aggregateLimited.Build().GetBody()!.GetMediaType()!.IsType("text", "plain"), Is.True);

        Assert.Throws<ArgumentNullException>(() => bodyLimited.SetText(null!));
        Assert.Throws<ArgumentNullException>(() => bodyLimited.SetRawEntity(null));
        Assert.Throws<ArgumentException>(() => bodyLimited.SetContent("value", "multipart", "mixed", "utf-8"));
        Assert.Throws<ArgumentException>(() => bodyLimited.SetCalendarWithCharset("value", "bad method", "utf-8"));
        Assert.Throws<ArgumentException>(() => bodyLimited.SetHtmlWithCharset("value", "unknown-charset"));
        Assert.Throws<ArgumentOutOfRangeException>(() => bodyOptions.SetMaximumAlternativeCount(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => bodyOptions.SetMaximumBodyBytes(0));
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

    static string ContentText(MimeEntity entity)
    {
        return Encoding.UTF8.GetString(entity.GetContent()!.Materialize(EmailComposerOptionsConstants.DefaultMaximumBodyBytes));
    }

    static string? Parameter(MimeEntity entity, string name)
    {
        EmailParameter parameter = entity.GetMediaType()!.GetParameters().GetFirst(name)!;
        return parameter == null ? null : Encoding.ASCII.GetString(parameter.GetValueBytes())!;
    }
}
