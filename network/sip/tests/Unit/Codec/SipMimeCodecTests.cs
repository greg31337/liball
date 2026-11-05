namespace sip.Tests;

public class SipMimeCodecTests
{
    [Test]
    public void ParsesAndFormatsOpaqueBinaryEntityWithUnknownValues()
    {
        byte[] headers = Bytes("Content-Type: application/x-widget;version=2\r\n"
            + "Content-Disposition: x-display;handling=optional;mode=fast\r\n"
            + "Content-Transfer-Encoding: x-binary\r\n"
            + "X-Part: retained\r\n\r\n");
        byte[] body = [0, 255, 13, 10, 65];
        byte[] source = headers.Concat(body).ToArray();
        SipMimeCodec codec = new SipMimeCodec();

        SipParseResult parsed = codec.ParseMimeEntity(source, new SipParseOptions());
        SipWriteOptions canonical = new SipWriteOptions();
        canonical.PreserveOriginal = false;
        SipWriteResult written = codec.FormatMimeEntity(parsed.MimeEntity!, canonical);
        SipParseResult reparsed = codec.ParseMimeEntity(written.Bytes, new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(parsed.MimeEntity!.ContentType!.Type, Is.EqualTo("application"));
            Assert.That(parsed.MimeEntity.ContentType.Subtype, Is.EqualTo("x-widget"));
            Assert.That(parsed.MimeEntity.ContentDisposition!.Disposition, Is.EqualTo(SipBodyDisposition.Unknown));
            Assert.That(parsed.MimeEntity.ContentDisposition.DispositionToken, Is.EqualTo("x-display"));
            Assert.That(parsed.MimeEntity.ContentTransferEncoding, Is.EqualTo("x-binary"));
            Assert.That(parsed.MimeEntity.Body, Is.EqualTo(body));
            Assert.That(written.Success, Is.True);
            Assert.That(reparsed.MimeEntity!.Body, Is.EqualTo(body));
            Assert.That(reparsed.MimeEntity.ContentTransferEncoding, Is.EqualTo("x-binary"));
            Assert.That(reparsed.MimeEntity.Headers.Select(header => header.Name), Does.Contain("X-Part"));
        });
    }

    [Test]
    public void ParsesNestedMultipartWithPreambleEpilogueAndBinaryParts()
    {
        string text = "Content-Type: multipart/mixed;boundary=outer\r\n"
            + "Content-Disposition: render;handling=required\r\n\r\n"
            + "outer preamble\r\n"
            + "--outer\r\n"
            + "Content-Type: text/plain\r\n"
            + "Content-Disposition: render;handling=optional\r\n\r\n"
            + "first body\r\n"
            + "--outer\r\n"
            + "Content-Type: multipart/alternative;boundary=inner\r\n"
            + "Content-Disposition: render;handling=required\r\n\r\n"
            + "inner preamble\r\n"
            + "--inner\r\n"
            + "Content-Type: text/plain\r\n"
            + "Content-Disposition: render;handling=optional\r\n\r\n"
            + "plain\r\n"
            + "--inner\r\n"
            + "Content-Type: application/json\r\n"
            + "Content-Disposition: render;handling=optional\r\n\r\n"
            + "{}\r\n"
            + "--inner--\r\n"
            + "inner epilogue\r\n"
            + "--outer--\r\n"
            + "outer epilogue";
        SipMimeCodec codec = new SipMimeCodec();

        SipParseResult parsed = codec.ParseMimeEntity(Bytes(text), new SipParseOptions());
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = false;
        SipWriteResult written = codec.FormatMimeEntity(parsed.MimeEntity!, options);
        SipParseResult reparsed = codec.ParseMimeEntity(written.Bytes, new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(parsed.MimeEntity!.Parts, Has.Length.EqualTo(2));
            Assert.That(Text(parsed.MimeEntity.Preamble), Is.EqualTo("outer preamble"));
            Assert.That(Text(parsed.MimeEntity.Epilogue), Is.EqualTo("outer epilogue"));
            Assert.That(Text(parsed.MimeEntity.Parts[0].Body), Is.EqualTo("first body"));
            Assert.That(parsed.MimeEntity.Parts[1].Parts, Has.Length.EqualTo(2));
            Assert.That(Text(parsed.MimeEntity.Parts[1].Preamble), Is.EqualTo("inner preamble"));
            Assert.That(Text(parsed.MimeEntity.Parts[1].Epilogue), Is.EqualTo("inner epilogue"));
            Assert.That(Text(parsed.MimeEntity.Parts[1].Parts[1].Body), Is.EqualTo("{}"));
            Assert.That(written.Success, Is.True);
            Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(reparsed.MimeEntity!.Parts[1].Parts, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void FormatterCreatesSafeBoundaryAndRejectsExplicitCollision()
    {
        SipMimeEntity root = new SipMimeEntity();
        root.ContentType = MediaType("multipart", "mixed");
        root.Parts = [Part("alpha"), Part("beta")];
        SipMimeCodec codec = new SipMimeCodec();

        SipWriteResult generated = codec.FormatMimeEntity(root, new SipWriteOptions());
        SipParseResult parsed = codec.ParseMimeEntity(generated.Bytes, new SipParseOptions());
        string? boundary = FindParameter(parsed.MimeEntity!.ContentType!.Parameters, "boundary");

        SipMimeEntity collision = new SipMimeEntity();
        collision.ContentType = MediaType("multipart", "mixed");
        collision.ContentType.Parameters = [Parameter("boundary", "unsafe")];
        collision.Parts = [Part("first\r\n--unsafe-hidden")];
        SipWriteResult rejected = codec.FormatMimeEntity(collision, new SipWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(generated.Success, Is.True);
            Assert.That(root.ContentType!.Parameters, Is.Empty);
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(boundary, Does.Match("^sip-[0-9a-f]{36}$"));
            Assert.That(parsed.MimeEntity.Parts.Select(part => Text(part.Body)),
                Is.EqualTo(new string[] { "alpha", "beta" }));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.ErrorMessage, Does.Contain("boundary"));
        });
    }

    [Test]
    public void PublicValidationReportsInconsistentMultipartModel()
    {
        SipMimeEntity entity = new SipMimeEntity();
        entity.ContentType = MediaType("multipart", "mixed");
        entity.ContentTransferEncoding = "base64";
        entity.Body = Bytes("raw");
        SipMimeCodec codec = new SipMimeCodec();

        SipValidationIssue[] issues = codec.ValidateMimeEntity(entity);

        Assert.Multiple(() =>
        {
            Assert.That(issues, Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "mime-boundary"));
            Assert.That(issues,
                Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "mime-multipart-body"));
            Assert.That(issues,
                Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "mime-parts-required"));
            Assert.That(issues,
                Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "mime-multipart-transfer-encoding"));
            Assert.That(issues.All(issue => issue.Severity == SipValidationSeverity.Error), Is.True);
        });
    }

    [Test]
    public void StrictHandlingValidationBecomesACompatibilityWarning()
    {
        byte[] source = Bytes("Content-Type: text/plain\r\n"
            + "Content-Disposition: render;handling=maybe\r\n\r\nhello");
        SipMimeCodec codec = new SipMimeCodec();
        SipParseOptions compatible = new SipParseOptions();
        compatible.Strict = false;

        SipParseResult strict = codec.ParseMimeEntity(source, new SipParseOptions());
        SipParseResult accepted = codec.ParseMimeEntity(source, compatible);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(strict.ValidationIssues,
                Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "mime-handling"));
            Assert.That(accepted.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(accepted.ValidationIssues,
                Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "mime-handling"
                    && issue.Severity == SipValidationSeverity.Warning));
        });
    }

    [Test]
    public void MultipartAlternativeRequiresMatchingDispositionTypes()
    {
        string text = "Content-Type: multipart/alternative;boundary=choice\r\n"
            + "Content-Disposition: render;handling=required\r\n\r\n"
            + "--choice\r\nContent-Type: text/plain\r\n"
            + "Content-Disposition: render;handling=optional\r\n\r\none\r\n"
            + "--choice\r\nContent-Type: application/sdp\r\n"
            + "Content-Disposition: session;handling=optional\r\n\r\ntwo\r\n"
            + "--choice--\r\n";
        SipMimeCodec codec = new SipMimeCodec();
        SipParseOptions compatible = new SipParseOptions();
        compatible.Strict = false;

        SipParseResult strict = codec.ParseMimeEntity(Bytes(text), new SipParseOptions());
        SipParseResult accepted = codec.ParseMimeEntity(Bytes(text), compatible);

        Assert.That(strict.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(strict.ValidationIssues,
            Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "mime-alternative-disposition"));
        Assert.That(accepted.Status, Is.EqualTo(SipParseStatus.Complete));
    }

    [Test]
    public void UnknownTransferEncodingIsPreservedButMultipartEncodingIsRestricted()
    {
        SipMimeCodec codec = new SipMimeCodec();
        byte[] opaque = Bytes("Content-Type: application/x-data\r\n"
            + "Content-Transfer-Encoding: x-passthrough\r\n\r\ndata");
        byte[] multipart = Bytes("Content-Type: multipart/mixed;boundary=b\r\n"
            + "Content-Transfer-Encoding: base64\r\n\r\n"
            + "--b\r\n\r\ndata\r\n--b--\r\n");
        SipParseOptions compatible = new SipParseOptions();
        compatible.Strict = false;

        SipParseResult opaqueResult = codec.ParseMimeEntity(opaque, new SipParseOptions());
        SipParseResult strictMultipart = codec.ParseMimeEntity(multipart, new SipParseOptions());
        SipParseResult compatibleMultipart = codec.ParseMimeEntity(multipart, compatible);

        Assert.Multiple(() =>
        {
            Assert.That(opaqueResult.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(opaqueResult.MimeEntity!.ContentTransferEncoding, Is.EqualTo("x-passthrough"));
            Assert.That(opaqueResult.MimeEntity.Body, Is.EqualTo(Bytes("data")));
            Assert.That(strictMultipart.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(compatibleMultipart.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(compatibleMultipart.ValidationIssues,
                Has.Some.Matches<SipValidationIssue>(issue => issue.Code == "mime-multipart-transfer-encoding"));
        });
    }

    [Test]
    public void ParsesCompleteAndHeaderOnlySipFragments()
    {
        string responseFragment = "SIP/2.0 486 Busy Here\r\n"
            + "Warning: 399 example.com \"Declined\"\r\n";
        string headerFragment = "From: <sip:alice@example.com>;tag=one\r\n"
            + "To: <sip:bob@example.com>\r\n";
        SipMimeCodec codec = new SipMimeCodec();

        SipParseResult response = codec.ParseMimeEntity(
            EntityWithBody("message/sipfrag", responseFragment), new SipParseOptions());
        SipParseResult headers = codec.ParseMimeEntity(
            EntityWithBody("message/sipfrag", headerFragment), new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(response.MimeEntity!.SipFragment!.Kind, Is.EqualTo(SipMessageKind.Response));
            Assert.That(response.MimeEntity.SipFragment.StatusCode, Is.EqualTo(486));
            Assert.That(headers.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(headers.MimeEntity!.SipFragment!.Kind, Is.EqualTo(SipMessageKind.Unknown));
            Assert.That(headers.MimeEntity.SipFragment.Headers.Select(header => header.Name),
                Is.EqualTo(new string?[] { SipHeaderNames.From, SipHeaderNames.To }));
        });
    }

    [Test]
    public void RejectsSipFragmentBodyWithoutDescribingHeaders()
    {
        byte[] invalid = EntityWithBody("message/sipfrag",
            "MESSAGE sip:bob@example.com SIP/2.0\r\n\r\nhello");

        SipParseResult result = new SipMimeCodec().ParseMimeEntity(invalid, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ErrorMessage, Does.Contain("Content-Type"));
    }

    [Test]
    public void PreservationModeReturnsExactMimeBytes()
    {
        byte[] source = Bytes("content-type: text/plain; charset=us-ascii\r\n"
            + "X-Spacing:  retained\r\n\r\nbody\r\n");
        SipMimeCodec codec = new SipMimeCodec();
        SipParseResult parsed = codec.ParseMimeEntity(source, new SipParseOptions());
        SipWriteOptions preserve = new SipWriteOptions();
        preserve.PreserveOriginal = true;

        SipWriteResult written = codec.FormatMimeEntity(parsed.MimeEntity!, preserve);

        Assert.That(written.Success, Is.True);
        Assert.That(written.Bytes, Is.EqualTo(source));
        Assert.That(written.Bytes, Is.Not.SameAs(source));
    }

    [Test]
    public void EnforcesMalformedBoundaryDepthAndBodyLimits()
    {
        SipMimeCodec codec = new SipMimeCodec();
        byte[] missingClose = Bytes("Content-Type: multipart/mixed;boundary=b\r\n\r\n"
            + "--b\r\n\r\ndata");
        byte[] nested = Bytes("Content-Type: multipart/mixed;boundary=outer\r\n\r\n"
            + "--outer\r\nContent-Type: multipart/mixed;boundary=inner\r\n\r\n"
            + "--inner\r\n\r\ndata\r\n--inner--\r\n\r\n--outer--\r\n");
        SipParseOptions shallow = new SipParseOptions();
        shallow.MaxMimeDepth = 1;
        SipParseOptions limited = new SipParseOptions();
        limited.MaxBodyBytes = 3;

        SipParseResult boundary = codec.ParseMimeEntity(missingClose, new SipParseOptions());
        SipParseResult depth = codec.ParseMimeEntity(nested, shallow);
        SipParseResult body = codec.ParseMimeEntity(
            Bytes("Content-Type: text/plain\r\n\r\nfour"), limited);

        Assert.Multiple(() =>
        {
            Assert.That(boundary.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(boundary.ErrorMessage, Does.Contain("closing boundary"));
            Assert.That(depth.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(depth.ErrorMessage, Does.Contain("depth"));
            Assert.That(body.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(body.ErrorMessage, Does.Contain("body"));
        });
    }

    static SipMimeEntity Part(string body)
    {
        SipMimeEntity entity = new SipMimeEntity();
        entity.ContentType = MediaType("text", "plain");
        entity.Body = Bytes(body);
        return entity;
    }

    static SipMediaType MediaType(string type, string subtype)
    {
        SipMediaType mediaType = new SipMediaType();
        mediaType.Type = type;
        mediaType.Subtype = subtype;
        return mediaType;
    }

    static SipUriParameter Parameter(string name, string value)
    {
        SipUriParameter parameter = new SipUriParameter();
        parameter.Name = name;
        parameter.Value = value;
        parameter.HasValue = true;
        return parameter;
    }

    static string? FindParameter(SipUriParameter[] parameters, string name)
    {
        return parameters.Single(parameter => parameter.Name!.Equals(
            name, StringComparison.OrdinalIgnoreCase)).Value;
    }

    static byte[] EntityWithBody(string mediaType, string body)
    {
        return Bytes("Content-Type: " + mediaType + "\r\n\r\n" + body);
    }

    static byte[] Bytes(string text)
    {
        return System.Text.Encoding.UTF8.GetBytes(text);
    }

    static string Text(byte[] bytes)
    {
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
