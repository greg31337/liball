namespace sip.Tests;

public class SipMimeComprehensiveTests
{
    [Test]
    public void ParsesNestedMultipartWithQuotedBoundariesAndDigestDefaults()
    {
        string source = "Content-Type: multipart/mixed;boundary=\"outer:part\"\r\n\r\n"
            + "preamble\r\n"
            + "--outer:part \t\r\n"
            + "Content-Type: multipart/digest;boundary=\"inner?part\"\r\n\r\n"
            + "--inner?part\r\n"
            + "\r\n"
            + "From: <sip:alice@example.com>\r\n"
            + "To: <sip:bob@example.com>\r\n"
            + "\r\n"
            + "--inner?part--\t\r\n"
            + "--outer:part--\r\n"
            + "epilogue";
        SipMimeCodec codec = new SipMimeCodec();

        SipParseResult parsed = codec.ParseMimeEntity(Bytes(source), new SipParseOptions());
        SipWriteOptions canonical = new SipWriteOptions();
        canonical.PreserveOriginal = false;
        SipWriteResult written = codec.FormatMimeEntity(parsed.MimeEntity!, canonical);
        SipParseResult reparsed = codec.ParseMimeEntity(written.Bytes, new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(ParameterValue(parsed.MimeEntity!.ContentType!, "boundary"),
                Is.EqualTo("outer:part"));
            Assert.That(Text(parsed.MimeEntity.Preamble), Is.EqualTo("preamble"));
            Assert.That(Text(parsed.MimeEntity.Epilogue), Is.EqualTo("epilogue"));
            Assert.That(parsed.MimeEntity.Parts[0].Parts, Has.Length.EqualTo(1));
            Assert.That(parsed.MimeEntity.Parts[0].Parts[0].ContentType!.Type,
                Is.EqualTo("message"));
            Assert.That(parsed.MimeEntity.Parts[0].Parts[0].ContentType!.Subtype,
                Is.EqualTo("rfc822"));
            Assert.That(written.Success, Is.True);
            Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(reparsed.MimeEntity!.Parts[0].Parts[0].Body,
                Is.EqualTo(parsed.MimeEntity.Parts[0].Parts[0].Body));
        });
    }

    [Test]
    public void ParsesHeaderlessAndEmptyMultipartParts()
    {
        byte[] source = Bytes("Content-Type: multipart/mixed;boundary=empty\r\n\r\n"
            + "--empty\r\n"
            + "\r\n"
            + "--empty\r\n"
            + "Content-Type: application/octet-stream\r\n"
            + "\r\n"
            + "--empty\r\n"
            + "\r\n"
            + "line\r\n"
            + "\r\n"
            + "--empty--\r\n");

        SipParseResult parsed = new SipMimeCodec().ParseMimeEntity(source, new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(parsed.MimeEntity!.Parts, Has.Length.EqualTo(3));
            Assert.That(parsed.MimeEntity.Parts[0].ContentType!.Type, Is.EqualTo("text"));
            Assert.That(parsed.MimeEntity.Parts[0].ContentType!.Subtype, Is.EqualTo("plain"));
            Assert.That(parsed.MimeEntity.Parts[0].Body, Is.Empty);
            Assert.That(parsed.MimeEntity.Parts[1].ContentType!.Type, Is.EqualTo("application"));
            Assert.That(parsed.MimeEntity.Parts[1].ContentType!.Subtype,
                Is.EqualTo("octet-stream"));
            Assert.That(parsed.MimeEntity.Parts[1].Body, Is.Empty);
            Assert.That(parsed.MimeEntity.Parts[2].Body, Is.EqualTo(Bytes("line\r\n")));
        });
    }

    [Test]
    public void PreservesBinaryMultipartContentAndNearBoundaryLinesAcrossRoundTrip()
    {
        byte[] binary = Bytes("prefix\r\n--binarx\r\nx--binary\r\n");
        binary = binary.Concat(new byte[] { 0, 128, 255 }).ToArray();
        byte[] source = Bytes("Content-Type: multipart/mixed;boundary=binary\r\n\r\n"
            + "--binary\r\nContent-Type: application/octet-stream\r\n\r\n")
            .Concat(binary)
            .Concat(Bytes("\r\n--binary--\r\n"))
            .ToArray();
        SipMimeCodec codec = new SipMimeCodec();

        SipParseResult parsed = codec.ParseMimeEntity(source, new SipParseOptions());
        SipWriteOptions canonical = new SipWriteOptions();
        canonical.PreserveOriginal = false;
        SipWriteResult written = codec.FormatMimeEntity(parsed.MimeEntity!, canonical);
        SipParseResult reparsed = codec.ParseMimeEntity(written.Bytes, new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(parsed.MimeEntity!.Parts[0].Body, Is.EqualTo(binary));
            Assert.That(written.Success, Is.True);
            Assert.That(reparsed.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(reparsed.MimeEntity!.Parts[0].Body, Is.EqualTo(binary));
        });
    }

    [Test]
    public void AppliesDefaultDispositionsAndKeepsMixedPartHandlingIndependent()
    {
        byte[] alternative = Bytes("Content-Type: multipart/alternative;boundary=choice\r\n\r\n"
            + "--choice\r\nContent-Type: text/plain\r\n\r\ntext\r\n"
            + "--choice\r\nContent-Type: application/sdp\r\n\r\nv=0\r\n"
            + "--choice--\r\n");
        byte[] mixed = Bytes("Content-Type: multipart/mixed;boundary=mixed\r\n"
            + "Content-Disposition: session;handling=optional\r\n\r\n"
            + "--mixed\r\nContent-Type: text/plain\r\n"
            + "Content-Disposition: render;handling=required\r\n\r\ntext\r\n"
            + "--mixed--\r\n");
        SipMimeCodec codec = new SipMimeCodec();

        SipParseResult alternativeResult = codec.ParseMimeEntity(alternative, new SipParseOptions());
        SipParseResult mixedResult = codec.ParseMimeEntity(mixed, new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(alternativeResult.Status, Is.EqualTo(SipParseStatus.Error));
            Assert.That(alternativeResult.ValidationIssues,
                Has.Some.Matches<SipValidationIssue>(issue =>
                    issue.Code == "mime-alternative-disposition"));
            Assert.That(mixedResult.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(mixedResult.MimeEntity!.ContentDisposition!.Disposition,
                Is.EqualTo(SipBodyDisposition.Session));
            Assert.That(ParameterValue(mixedResult.MimeEntity.ContentDisposition!, "handling"),
                Is.EqualTo("optional"));
            Assert.That(mixedResult.MimeEntity.Parts[0].ContentDisposition!.Disposition,
                Is.EqualTo(SipBodyDisposition.Render));
            Assert.That(ParameterValue(mixedResult.MimeEntity.Parts[0].ContentDisposition!, "handling"),
                Is.EqualTo("required"));
        });
    }

    [Test]
    public void RejectsMalformedBoundaryParametersAndDelimiterLines()
    {
        SipMimeCodec codec = new SipMimeCodec();
        string tooLong = new string('x', 71);
        byte[][] sources =
        [
            Bytes("Content-Type: multipart/mixed\r\n\r\nbody"),
            Bytes("Content-Type: multipart/mixed;boundary=\"bad \"\r\n\r\nbody"),
            Bytes("Content-Type: multipart/mixed;boundary=" + tooLong + "\r\n\r\nbody"),
            Bytes("Content-Type: multipart/mixed;boundary=b\r\n\r\n"
                + "--b-extra\r\n\r\ndata\r\n--b--junk\r\n")
        ];

        SipParseResult[] results = sources.Select(source =>
            codec.ParseMimeEntity(source, new SipParseOptions())).ToArray();

        Assert.That(results, Has.All.Matches<SipParseResult>(result =>
            result.Status == SipParseStatus.Error));
    }

    [Test]
    public void EnforcesMultipartDepthPartCountAndSizeAtExactBoundaries()
    {
        byte[] nested = Bytes("Content-Type: multipart/mixed;boundary=outer\r\n\r\n"
            + "--outer\r\nContent-Type: multipart/mixed;boundary=inner\r\n\r\n"
            + "--inner\r\n\r\ndata\r\n--inner--\r\n"
            + "--outer--\r\n");
        byte[] twoParts = Bytes("Content-Type: multipart/mixed;boundary=two\r\n\r\n"
            + "--two\r\n\r\none\r\n--two\r\n\r\ntwo\r\n--two--\r\n");
        SipMimeCodec codec = new SipMimeCodec();
        SipParseOptions depthAccepted = new SipParseOptions();
        depthAccepted.MaxMimeDepth = 2;
        SipParseOptions depthRejected = new SipParseOptions();
        depthRejected.MaxMimeDepth = 1;
        SipParseOptions countAccepted = new SipParseOptions();
        countAccepted.MaxListValueCount = 2;
        SipParseOptions countRejected = new SipParseOptions();
        countRejected.MaxListValueCount = 1;
        SipParseOptions sizeAccepted = new SipParseOptions();
        sizeAccepted.MaxMessageBytes = nested.Length;
        sizeAccepted.MaxBodyBytes = nested.Length;
        SipParseOptions sizeRejected = new SipParseOptions();
        sizeRejected.MaxMessageBytes = nested.Length - 1;

        Assert.Multiple(() =>
        {
            Assert.That(codec.ParseMimeEntity(nested, depthAccepted).Status,
                Is.EqualTo(SipParseStatus.Complete));
            Assert.That(codec.ParseMimeEntity(nested, depthRejected).Status,
                Is.EqualTo(SipParseStatus.Error));
            Assert.That(codec.ParseMimeEntity(twoParts, countAccepted).Status,
                Is.EqualTo(SipParseStatus.Complete));
            Assert.That(codec.ParseMimeEntity(twoParts, countRejected).Status,
                Is.EqualTo(SipParseStatus.Error));
            Assert.That(codec.ParseMimeEntity(nested, sizeAccepted).Status,
                Is.EqualTo(SipParseStatus.Complete));
            Assert.That(codec.ParseMimeEntity(nested, sizeRejected).Status,
                Is.EqualTo(SipParseStatus.Error));
        });
    }

    [Test]
    public void ParsesRfcSipFragmentFormsAndRejectsMalformedFragments()
    {
        SipMimeCodec codec = new SipMimeCodec();
        byte[][] valid =
        [
            EntityWithBody(""),
            EntityWithBody("INVITE sip:alice@atlanta.com SIP/2.0"),
            EntityWithBody("SIP/2.0 603 Declined"),
            EntityWithBody("From: <sip:alice@atlanta.com>\r\n"
                + "To: <sip:bob@biloxi.com>\r\n"),
            EntityWithBody("Content-Type: text/plain\r\nContent-Length: 11\r\n\r\nHi There!\r\n")
        ];
        byte[][] invalid =
        [
            EntityWithBody("INVITE"),
            EntityWithBody("SIP/2.0"),
            EntityWithBody("Content-Type: text/plain\r\nContent-Length: 12\r\n\r\nHi There!\r\n")
        ];

        SipParseResult[] validResults = valid.Select(source =>
            codec.ParseMimeEntity(source, new SipParseOptions())).ToArray();
        SipParseResult[] invalidResults = invalid.Select(source =>
            codec.ParseMimeEntity(source, new SipParseOptions())).ToArray();
        SipWriteOptions canonical = new SipWriteOptions();
        canonical.PreserveOriginal = false;
        SipWriteResult roundTripBytes = codec.FormatMimeEntity(validResults[4].MimeEntity!, canonical);
        SipParseResult roundTrip = codec.ParseMimeEntity(roundTripBytes.Bytes, new SipParseOptions());

        Assert.Multiple(() =>
        {
            Assert.That(validResults, Has.All.Matches<SipParseResult>(result =>
                result.Status == SipParseStatus.Complete));
            Assert.That(validResults[0].MimeEntity!.SipFragment!.Kind,
                Is.EqualTo(SipMessageKind.Unknown));
            Assert.That(validResults[1].MimeEntity!.SipFragment!.Kind,
                Is.EqualTo(SipMessageKind.Request));
            Assert.That(validResults[2].MimeEntity!.SipFragment!.Kind,
                Is.EqualTo(SipMessageKind.Response));
            Assert.That(validResults[3].MimeEntity!.SipFragment!.Headers, Has.Length.EqualTo(2));
            Assert.That(validResults[4].MimeEntity!.SipFragment!.Body,
                Is.EqualTo(Bytes("Hi There!\r\n")));
            Assert.That(invalidResults, Has.All.Matches<SipParseResult>(result =>
                result.Status == SipParseStatus.Error));
            Assert.That(roundTripBytes.Success, Is.True);
            Assert.That(roundTrip.Status, Is.EqualTo(SipParseStatus.Complete));
            Assert.That(roundTrip.MimeEntity!.SipFragment!.Body,
                Is.EqualTo(Bytes("Hi There!\r\n")));
        });
    }

    static string? ParameterValue(SipMediaType mediaType, string name)
    {
        return mediaType.Parameters.Single(parameter => parameter.Name!.Equals(
            name, StringComparison.OrdinalIgnoreCase)).Value;
    }

    static string? ParameterValue(SipContentDisposition disposition, string name)
    {
        return disposition.Parameters.Single(parameter => parameter.Name!.Equals(
            name, StringComparison.OrdinalIgnoreCase)).Value;
    }

    static byte[] EntityWithBody(string body)
    {
        return Bytes("Content-Type: message/sipfrag\r\n\r\n" + body);
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
