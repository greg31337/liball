using System.Text;
using email;

namespace email.Tests;

public class EmailHtmlDataUriExtractorTests
{
    [Test]
    public void ExtractsBase64AndPercentEncodedUrisIntoRelatedParts()
    {
        string html = "<img alt='png' src='data:image/png;base64,UE5H'><img src=\"data:image/gif,GIF89a\">";

        EmailHtmlDataUriResult result = new EmailHtmlProcessor().ExtractDataUrisWithHtml(html);

        Assert.That(result.GetExtractionCount(), Is.EqualTo(2));
        Assert.That(result.GetRelatedPartCount(), Is.EqualTo(2));
        Assert.That(result.GetExtractedByteCount(), Is.EqualTo(9));
        Assert.That(result.GetHtml(), Is.EqualTo("<img alt='png' src=\"cid:" + result.GetExtraction(0).GetContentId()
            + "\"><img src=\"cid:" + result.GetExtraction(1).GetContentId() + "\">"));
        AssertPart(result.GetRelatedPart(0), "image", "png", Encoding.ASCII.GetBytes("PNG"), result.GetExtraction(0).GetContentId());
        AssertPart(result.GetRelatedPart(1), "image", "gif", Encoding.ASCII.GetBytes("GIF89a"), result.GetExtraction(1).GetContentId());
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HtmlDataUriExtracted), Is.True);
        Assert.That(result.GetRejectedDataUriCount(), Is.Zero);
        Assert.That(result.WasModified(), Is.True);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void UsesDeterministicContentIdsAndDeduplicatesIdenticalResources()
    {
        const string uri = "data:image/png;base64,QUJD";
        string html = "<img src='" + uri + "'><a href='" + uri + "'>same</a>";

        EmailHtmlDataUriResult first = new EmailHtmlProcessor().ExtractDataUrisWithHtml(html);
        EmailHtmlDataUriResult second = new EmailHtmlProcessor().ExtractDataUrisWithHtml(html);

        Assert.That(first.GetRelatedPartCount(), Is.EqualTo(1));
        Assert.That(first.GetExtractionCount(), Is.EqualTo(2));
        Assert.That(first.GetExtraction(0).ReusedPart(), Is.False);
        Assert.That(first.GetExtraction(1).ReusedPart(), Is.True);
        Assert.That(first.GetExtraction(1).GetRelatedPart(), Is.SameAs(first.GetRelatedPart(0)));
        Assert.That(first.GetExtraction(0).GetContentId(), Is.EqualTo(first.GetExtraction(1).GetContentId()));
        Assert.That(first.GetExtraction(0).GetContentId(), Does.Match("^data-uri-[0-9a-f]{64}@email\\.invalid$"));
        Assert.That(second.GetExtraction(0).GetContentId(), Is.EqualTo(first.GetExtraction(0).GetContentId()));
        Assert.That(second.GetHtml(), Is.EqualTo(first.GetHtml()));
        Assert.That(first.GetExtractedByteCount(), Is.EqualTo(3));
    }

    [Test]
    public void RewritesSrcSetInlineCssAndStyleBlocksWithoutTouchingScripts()
    {
        string html = "<source srcset='data:image/png;base64,QQ== 1x, remote.png 2x'>"
            + "<div style=\"background:url(&quot;data:image/gif;base64,Qg==&quot;);color:red\"></div>"
            + "<style>.x{background:url(data:image/png,C)}</style>"
            + "<script>url(data:image/png,D)</script>";

        EmailHtmlDataUriResult result = new EmailHtmlProcessor().ExtractDataUrisWithHtml(html);

        Assert.That(result.GetExtractionCount(), Is.EqualTo(3));
        Assert.That(result.GetHtml(), Is.EqualTo("<source srcset=\"cid:" + result.GetExtraction(0).GetContentId() + " 1x, remote.png 2x\">"
            + "<div style=\"background:url(&quot;cid:" + result.GetExtraction(1).GetContentId() + "&quot;);color:red\"></div>"
            + "<style>.x{background:url(cid:" + result.GetExtraction(2).GetContentId() + ")}</style>"
            + "<script>url(data:image/png,D)</script>"));
        Assert.That(result.GetExtractions().Select(item => Encoding.ASCII.GetString(item.GetRelatedPart().GetDecodedContent()!.Materialize(10))), Is.EqualTo(new[] { "A", "B", "C" }));
    }

    [Test]
    public void LeavesMalformedAndDisallowedDataUrisUnchangedWithDiagnostics()
    {
        string html = "<img src='data:image/svg+xml;base64,PHN2Zz4='>"
            + "<img src='data:image/png;base64,%%%'><img src='data:image/png,%GG'>"
            + "<a href='data:text/plain,hello'>text</a>";

        EmailHtmlDataUriResult result = new EmailHtmlProcessor().ExtractDataUrisWithHtml(html);

        Assert.That(result.GetHtml(), Is.EqualTo(html));
        Assert.That(result.GetRelatedPartCount(), Is.Zero);
        Assert.That(result.GetExtractionCount(), Is.Zero);
        Assert.That(result.GetRejectedDataUriCount(), Is.EqualTo(4));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HtmlDataUriRejected), Is.True);
        Assert.That(result.WasModified(), Is.False);
        Assert.That(result.IsSuccess(), Is.True);
    }

    [Test]
    public void CustomAllowlistSupportsParameterizedMediaTypes()
    {
        EmailHtmlDataUriOptions options = new EmailHtmlDataUriOptions();
        options.SetAllowedMediaTypes(new List<string> { "text/plain" });

        EmailHtmlDataUriResult result = new EmailHtmlProcessor().ExtractDataUrisWithHtmlAndOptions("<a href='data:text/plain;charset=utf-8,hello%20world'>text</a>", options);

        Assert.That(result.GetExtractionCount(), Is.EqualTo(1));
        MimeEntity part = result.GetRelatedPart(0);
        Assert.That(part.GetMediaType()!.IsType("text", "plain"), Is.True);
        Assert.That(Encoding.ASCII.GetString(part.GetMediaType()!.GetParameters().GetFirst("charset")!.GetValueBytes()), Is.EqualTo("utf-8"));
        Assert.That(Encoding.ASCII.GetString(part.GetDecodedContent()!.Materialize(20)), Is.EqualTo("hello world"));
        Assert.That(options.GetAllowedMediaTypes(), Is.EqualTo(new[] { "text/plain" }));
    }

    [Test]
    public void MimeOverloadDecodesHtmlBeforeExtraction()
    {
        MimeEntity entity = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("<img src='data:image/png;base64,w6k%3D'>", "html", "utf-8");

        EmailHtmlDataUriResult result = new EmailHtmlProcessor().ExtractDataUrisWithEntity(entity);

        Assert.That(result.GetExtractionCount(), Is.EqualTo(1));
        Assert.That(result.GetRelatedPart(0).GetDecodedContent()!.Materialize(10), Is.EqualTo(Encoding.UTF8.GetBytes("é")));
        Assert.That(result.HasSourceTextDecodeResult(), Is.True);
        Assert.That(result.GetSourceTextDecodeResult()!.GetSelectedCharset(), Is.EqualTo("utf-8"));
        Assert.Throws<ArgumentException>(() => new EmailHtmlProcessor().ExtractDataUrisWithEntity(new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("plain", "plain", "utf-8")));
    }

    [Test]
    public void EnforcesInputOutputUrlTypeCountAndByteBounds()
    {
        EmailHtmlLimits input = new EmailHtmlLimits();
        input.SetMaxInputCharacters(3);
        AssertLimit("MaxHtmlInputCharacters", () => new EmailHtmlProcessor().ExtractDataUrisWithHtmlAndOptions("four", WithLimits(input)));

        EmailHtmlLimits output = new EmailHtmlLimits();
        output.SetMaxOutputCharacters(30);
        AssertLimit("MaxHtmlOutputCharacters", () => new EmailHtmlProcessor().ExtractDataUrisWithHtmlAndOptions("<img src='data:image/png,A'>", WithLimits(output)));

        EmailHtmlLimits url = new EmailHtmlLimits();
        url.SetMaxUrlCharacters(10);
        AssertLimit("MaxHtmlUrlCharacters", () => new EmailHtmlProcessor().ExtractDataUrisWithHtmlAndOptions("<img src='data:image/png,A'>", WithLimits(url)));

        EmailHtmlDataUriOptions type = new EmailHtmlDataUriOptions();
        type.SetMaximumMediaTypeCharacters(8);
        AssertLimit("MaxDataUriMediaTypeCharacters", () => new EmailHtmlProcessor().ExtractDataUrisWithHtmlAndOptions("<img src='data:image/png,A'>", type));

        EmailHtmlLimits count = new EmailHtmlLimits();
        count.SetMaxDataUriCount(1);
        AssertLimit("MaxHtmlDataUriCount", () => new EmailHtmlProcessor().ExtractDataUrisWithHtmlAndOptions("<img src='data:image/png,A'><img src='data:image/png,B'>", WithLimits(count)));

        EmailHtmlLimits bytes = new EmailHtmlLimits();
        bytes.SetMaxDataUriBytes(1);
        AssertLimit("MaxHtmlDataUriBytes", () => new EmailHtmlProcessor().ExtractDataUrisWithHtmlAndOptions("<img src='data:image/png,AB'>", WithLimits(bytes)));
    }

    [Test]
    public void OptionsAndResultsAreDefensiveAndValidateInputs()
    {
        EmailHtmlDataUriOptions options = new EmailHtmlDataUriOptions();
        List<string> types = options.GetAllowedMediaTypes();
        types.Clear();
        Assert.That(options.GetAllowedMediaTypes(), Does.Contain("image/png"));

        EmailHtmlDataUriResult result = new EmailHtmlProcessor().ExtractDataUrisWithHtmlAndOptions("<img src='data:image/png,A'>", options);
        result.GetRelatedParts().Clear();
        result.GetExtractions().Clear();
        result.GetIssues().Clear();

        Assert.That(result.GetRelatedPartCount(), Is.EqualTo(1));
        Assert.That(result.GetExtractionCount(), Is.EqualTo(1));
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.HtmlDataUriExtracted), Is.True);
        Assert.That(result.GetExtraction(0).GetSourceOffset(), Is.GreaterThan(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetRelatedPart(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => result.GetExtraction(1));
        Assert.Throws<ArgumentNullException>(() => new EmailHtmlProcessor().ExtractDataUrisWithHtml((string)null!));
        Assert.Throws<ArgumentNullException>(() => new EmailHtmlProcessor().ExtractDataUrisWithHtmlAndOptions("", null!));
        Assert.Throws<ArgumentNullException>(() => options.SetLimits(null!));
        Assert.Throws<ArgumentNullException>(() => options.SetAllowedMediaTypes(null!));
        Assert.Throws<ArgumentException>(() => options.AddAllowedMediaType("invalid"));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumMediaTypeCharacters(0));
    }

    static EmailHtmlDataUriOptions WithLimits(EmailHtmlLimits limits)
    {
        EmailHtmlDataUriOptions options = new EmailHtmlDataUriOptions();
        options.SetLimits(limits);
        return options;
    }

    static void AssertPart(MimeEntity part, string type, string subtype, byte[] bytes, string contentId)
    {
        Assert.That(part.GetMediaType()!.IsType(type, subtype), Is.True);
        Assert.That(part.GetDecodedContent()!.Materialize(bytes.Length), Is.EqualTo(bytes));
        Assert.That(part.GetTransferEncodingName(), Is.EqualTo("base64"));
        Assert.That(part.GetContentId()!.GetIdLeft() + "@" + part.GetContentId()!.GetIdRight(), Is.EqualTo(contentId));
    }

    static void AssertLimit(string expectedName, TestDelegate action)
    {
        EmailLimitException exception = Assert.Throws<EmailLimitException>(action);
        Assert.That(exception.GetLimitName(), Is.EqualTo(expectedName));
    }
}
