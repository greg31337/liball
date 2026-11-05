using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlPublicInputApiCompletionTests
{
    [Test]
    public void ByteDecoderApiSupportsEveryDecodeEntryPoint()
    {
        IHtmlByteDecoderApi api = new HtmlByteDecoderApi();
        byte[] declaredUtf8 = Encoding.UTF8.GetBytes(
            "<meta charset=utf-8>\u20ac");
        byte[] utf8Bom = [0xEF, 0xBB, 0xBF, 0xC2, 0xA2];
        HtmlEncodingDetectionResult detection =
            HtmlEncodingDetector.Detect(utf8Bom, "windows-1252");

        HtmlDecodedInput detected = api.Decode(declaredUtf8);
        HtmlDecodedInput labeled = api.DecodeWithLabel(
            new byte[] { 0x41, 0x80 }, "latin1");
        HtmlDecodedInput reused = api.DecodeWithDetection(
            utf8Bom, detection);

        Assert.Multiple(() =>
        {
            Assert.That(detected.GetEncodingName(), Is.EqualTo("UTF-8"));
            Assert.That(detected.GetString(), Does.EndWith("\u20ac"));
            Assert.That(labeled.GetEncodingName(),
                Is.EqualTo("windows-1252"));
            Assert.That(labeled.GetString(), Is.EqualTo("A\u20ac"));
            Assert.That(reused.GetString(), Is.EqualTo("\u00a2"));
            Assert.That(reused.GetByteOffset(0), Is.EqualTo(3));
            Assert.That(reused.GetCharacterOffset(5), Is.EqualTo(1));
        });
    }

    [Test]
    public void ByteDecoderApiExposesMalformedUnsupportedAndInvalidStates()
    {
        IHtmlByteDecoderApi api = new HtmlByteDecoderApi();
        HtmlDecodedInput malformed = api.DecodeWithLabel(
            new byte[] { 0xE2, 0x82 }, "utf-8");
        HtmlDecodedInput unsupported = api.DecodeWithLabel(
            new byte[] { 0x41 }, "not-an-encoding");
        HtmlEncodingDetectionResult impossible =
            new HtmlEncodingDetectionResult("UTF-8",
                HtmlEncodingConfidence.Certain, HtmlEncodingSource.Bom,
                2, -1);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.IsComplete(), Is.True);
            Assert.That(malformed.HadErrors(), Is.True);
            Assert.That(malformed.GetErrors(), Has.Length.EqualTo(1));
            Assert.That(malformed.GetErrors()[0].GetCode(), Is.EqualTo(
                HtmlDiagnosticCode.EncodingInvalidSequence));
            Assert.That(unsupported.IsSupported(), Is.False);
            Assert.That(unsupported.IsComplete(), Is.False);
            Assert.That(unsupported.IsPartial(), Is.False);
            Assert.That(unsupported.WasCancelled(), Is.False);
            Assert.That(unsupported.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.EncodingUnsupportedLabel));
        });
        Assert.Throws<ArgumentNullException>(() => api.DecodeWithLabel(
            ReadOnlySpan<byte>.Empty, null));
        Assert.Throws<ArgumentNullException>(() => api.DecodeWithDetection(
            ReadOnlySpan<byte>.Empty, null));
        Assert.Throws<ArgumentException>(() => api.DecodeWithDetection(
            new byte[] { 0xEF }, impossible));
    }

    [Test]
    public void EncodingDetectorApiSupportsEveryDetectionEntryPoint()
    {
        IHtmlEncodingDetectorApi api = new HtmlEncodingDetectorApi();
        byte[] meta = Encoding.ASCII.GetBytes("xx<meta charset=koi8-r>");

        HtmlEncodingDetectionResult detected = api.Detect(meta);
        HtmlEncodingDetectionResult transport =
            api.DetectWithTransportLabel(meta, "latin1");
        HtmlEncodingDetectionResult configured = api.DetectWithLabels(
            Encoding.ASCII.GetBytes("plain"), "", "gb18030");

        Assert.Multiple(() =>
        {
            Assert.That(api.GetEncodingName(" UTF8 "),
                Is.EqualTo("UTF-8"));
            Assert.That(api.GetEncodingNameFromSpan("Shift_JIS".AsSpan()),
                Is.EqualTo("Shift_JIS"));
            Assert.That(detected.GetEncodingName(), Is.EqualTo("KOI8-R"));
            Assert.That(detected.GetSource(),
                Is.EqualTo(HtmlEncodingSource.Meta));
            Assert.That(detected.GetDeclarationByteOffset(), Is.EqualTo(2));
            Assert.That(transport.GetEncodingName(),
                Is.EqualTo("windows-1252"));
            Assert.That(transport.GetConfidence(),
                Is.EqualTo(HtmlEncodingConfidence.Certain));
            Assert.That(transport.GetSource(),
                Is.EqualTo(HtmlEncodingSource.Transport));
            Assert.That(configured.GetEncodingName(),
                Is.EqualTo("gb18030"));
            Assert.That(configured.GetSource(),
                Is.EqualTo(HtmlEncodingSource.Default));
        });
    }

    [Test]
    public void EncodingDetectorApiValidatesReferenceLabels()
    {
        IHtmlEncodingDetectorApi api = new HtmlEncodingDetectorApi();

        Assert.Throws<ArgumentNullException>(() =>
            api.GetEncodingName(null));
        Assert.Throws<ArgumentNullException>(() =>
            api.DetectWithTransportLabel(ReadOnlySpan<byte>.Empty, null));
        Assert.Throws<ArgumentNullException>(() => api.DetectWithLabels(
            ReadOnlySpan<byte>.Empty, "", null));
        Assert.That(api.GetEncodingName("not-an-encoding"), Is.Empty);
    }

    [Test]
    public void EntitiesApiSupportsEveryUnboundedEntryPoint()
    {
        IHtmlEntitiesApi api = new HtmlEntitiesApi();

        Assert.Multiple(() =>
        {
            Assert.That(api.Decode("&copy;"), Is.EqualTo("\u00a9"));
            Assert.That(api.DecodeInContext("&amp=1",
                HtmlEntityContext.Attribute), Is.EqualTo("&amp=1"));
            Assert.That(api.Encode("<&>", HtmlEntityEncoding.Minimal),
                Is.EqualTo("&lt;&amp;&gt;"));
            Assert.That(api.EncodeInContext("\"&",
                HtmlEntityContext.Attribute, HtmlEntityEncoding.Minimal),
                Is.EqualTo("&quot;&amp;"));
            Assert.That(api.DecodeText("&lt;p&gt;"), Is.EqualTo("<p>"));
            Assert.That(api.DecodeAttribute("&quot;x&quot;"),
                Is.EqualTo("\"x\""));
            Assert.That(api.EncodeText("<\u00a9",
                HtmlEntityEncoding.AsciiOnly), Is.EqualTo("&lt;&copy;"));
            Assert.That(api.EncodeAttribute("\"\u00a9",
                HtmlEntityEncoding.AsciiOnly),
                Is.EqualTo("&quot;&copy;"));
        });
    }

    [Test]
    public void EntitiesApiSupportsEveryBoundedEntryPoint()
    {
        IHtmlEntitiesApi api = new HtmlEntitiesApi();
        HtmlLimits limits = HtmlLimits.CreateDefault();

        HtmlEntityResult decoded = api.DecodeWithLimits("&copy;",
            HtmlEntityContext.Text, limits, CancellationToken.None);
        HtmlEntityResult encoded = api.EncodeWithLimits("<&",
            HtmlEntityContext.Text, HtmlEntityEncoding.Minimal, limits,
            CancellationToken.None);
        HtmlEntityResult decodedText = api.DecodeTextWithLimits("&amp;",
            limits, CancellationToken.None);
        HtmlEntityResult decodedAttribute =
            api.DecodeAttributeWithLimits("&amp=1", limits,
                CancellationToken.None);
        HtmlEntityResult encodedText = api.EncodeTextWithLimits("<",
            HtmlEntityEncoding.Minimal, limits, CancellationToken.None);
        HtmlEntityResult encodedAttribute =
            api.EncodeAttributeWithLimits("\"",
                HtmlEntityEncoding.Minimal, limits,
                CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.GetString(), Is.EqualTo("\u00a9"));
            Assert.That(encoded.GetString(), Is.EqualTo("&lt;&amp;"));
            Assert.That(decodedText.GetString(), Is.EqualTo("&"));
            Assert.That(decodedAttribute.GetString(),
                Is.EqualTo("&amp=1"));
            Assert.That(encodedText.GetString(), Is.EqualTo("&lt;"));
            Assert.That(encodedAttribute.GetString(),
                Is.EqualTo("&quot;"));
            Assert.That(new[] { decoded, encoded, decodedText,
                decodedAttribute, encodedText, encodedAttribute },
                Has.All.Matches<HtmlEntityResult>(value =>
                    value.IsComplete()
                    && !value.WasLimitExceeded()
                    && !value.WasCancelled()));
        });
    }

    [Test]
    public void EntitiesApiExposesMalformedLimitAndDiagnosticStates()
    {
        IHtmlEntitiesApi api = new HtmlEntitiesApi();
        HtmlLimits diagnosticLimits = HtmlLimits.CreateDefault();
        diagnosticLimits.SetMaximumDiagnostics(1);
        HtmlEntityResult malformed = api.DecodeTextWithLimits(
            "&bogus;&bogus;&bogus;", diagnosticLimits,
            CancellationToken.None);
        HtmlLimits outputLimits = HtmlLimits.CreateDefault();
        outputLimits.SetMaximumOutputCharacters(3);
        HtmlEntityResult limited = api.EncodeTextWithLimits("<",
            HtmlEntityEncoding.Minimal, outputLimits,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.IsComplete(), Is.True);
            Assert.That(malformed.GetRetainedDiagnosticCount(),
                Is.EqualTo(1));
            Assert.That(malformed.GetDroppedDiagnosticCount(),
                Is.EqualTo(2));
            Assert.That(malformed.WereDiagnosticsTruncated(), Is.True);
            Assert.That(limited.IsComplete(), Is.False);
            Assert.That(limited.WasLimitExceeded(), Is.True);
            Assert.That(limited.IsPartial(), Is.False);
            Assert.That(limited.GetFailureCode(), Is.EqualTo(
                HtmlDiagnosticCode.ResourceLimitExceeded));
        });
    }

    [Test]
    public void EveryBoundedEntitiesApiOperationHonorsCancellation()
    {
        IHtmlEntitiesApi api = new HtmlEntitiesApi();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            api.DecodeWithLimits("&amp;", HtmlEntityContext.Text, limits,
                cancellation.Token));
        Assert.Throws<OperationCanceledException>(() =>
            api.EncodeWithLimits("&", HtmlEntityContext.Text,
                HtmlEntityEncoding.Minimal, limits, cancellation.Token));
        Assert.Throws<OperationCanceledException>(() =>
            api.DecodeTextWithLimits("&amp;", limits,
                cancellation.Token));
        Assert.Throws<OperationCanceledException>(() =>
            api.DecodeAttributeWithLimits("&amp;", limits,
                cancellation.Token));
        Assert.Throws<OperationCanceledException>(() =>
            api.EncodeTextWithLimits("&", HtmlEntityEncoding.Minimal,
                limits, cancellation.Token));
        Assert.Throws<OperationCanceledException>(() =>
            api.EncodeAttributeWithLimits("&", HtmlEntityEncoding.Minimal,
                limits, cancellation.Token));
    }

    [Test]
    public void EntitiesApiValidatesNullAndEnumArguments()
    {
        IHtmlEntitiesApi api = new HtmlEntitiesApi();
        HtmlLimits limits = HtmlLimits.CreateDefault();

        Assert.Throws<ArgumentNullException>(() => api.Decode(null));
        Assert.Throws<ArgumentNullException>(() => api.DecodeInContext(
            null, HtmlEntityContext.Text));
        Assert.Throws<ArgumentNullException>(() => api.DecodeWithLimits(
            "value", HtmlEntityContext.Text, null,
            CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => api.EncodeText(null,
            HtmlEntityEncoding.Minimal));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            api.DecodeInContext("value", (HtmlEntityContext)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => api.Encode(
            "value", (HtmlEntityEncoding)99));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            api.EncodeWithLimits("value", HtmlEntityContext.Text,
                (HtmlEntityEncoding)99, limits, CancellationToken.None));
    }
}
