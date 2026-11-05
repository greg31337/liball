using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlEncodingDetectorTests
{
    [TestCase(" UTF-8\t", "UTF-8")]
    [TestCase("uNiCoDe11UtF8", "UTF-8")]
    [TestCase("ascii", "windows-1252")]
    [TestCase("ISO_8859-1:1987", "windows-1252")]
    [TestCase("csisolatincyrillic", "ISO-8859-5")]
    [TestCase("logical", "ISO-8859-8-I")]
    [TestCase("x-mac-ukrainian", "x-mac-cyrillic")]
    [TestCase("gb_2312-80", "GBK")]
    [TestCase("windows-31j", "Shift_JIS")]
    [TestCase("ks_c_5601-1989", "EUC-KR")]
    [TestCase("iso-2022-kr", "replacement")]
    [TestCase("unicodefeff", "UTF-16LE")]
    [TestCase("not-an-encoding", "")]
    [TestCase("utf -8", "")]
    public void NormalizesWhatwgEncodingLabels(string label, string expected)
    {
        Assert.That(HtmlEncodingDetector.GetEncodingName(label), Is.EqualTo(expected));
        Assert.That(HtmlEncodingDetector.GetEncodingName(label.AsSpan()), Is.EqualTo(expected));
    }

    [TestCase(new byte[] { 0xEF, 0xBB, 0xBF }, "UTF-8", 3)]
    [TestCase(new byte[] { 0xFE, 0xFF }, "UTF-16BE", 2)]
    [TestCase(new byte[] { 0xFF, 0xFE }, "UTF-16LE", 2)]
    public void BomIsCertainAndOverridesTransportAndMeta(byte[] bom, string expected, int length)
    {
        byte[] markup = Encoding.ASCII.GetBytes("<meta charset=shift_jis>");
        byte[] input = new byte[bom.Length + markup.Length];
        bom.CopyTo(input, 0);
        markup.CopyTo(input, bom.Length);

        HtmlEncodingDetectionResult result = HtmlEncodingDetector.Detect(input, "koi8-r");

        Assert.Multiple(() =>
        {
            Assert.That(result.GetEncodingName(), Is.EqualTo(expected));
            Assert.That(result.GetConfidence(), Is.EqualTo(HtmlEncodingConfidence.Certain));
            Assert.That(result.GetSource(), Is.EqualTo(HtmlEncodingSource.Bom));
            Assert.That(result.GetByteOrderMarkLength(), Is.EqualTo(length));
            Assert.That(result.HasDeclarationByteOffset(), Is.False);
        });
    }

    [Test]
    public void SupportedTransportLabelIsCertainAndOverridesMeta()
    {
        HtmlEncodingDetectionResult result = Detect("<meta charset=utf-8>", " LATIN1 ");

        Assert.Multiple(() =>
        {
            Assert.That(result.GetEncodingName(), Is.EqualTo("windows-1252"));
            Assert.That(result.GetConfidence(), Is.EqualTo(HtmlEncodingConfidence.Certain));
            Assert.That(result.GetSource(), Is.EqualTo(HtmlEncodingSource.Transport));
            Assert.That(result.GetByteOrderMarkLength(), Is.Zero);
        });
    }

    [Test]
    public void InvalidTransportLabelFallsThroughToDirectMetaCharset()
    {
        const string markup = "xx<META data-x=1 CHARSET = 'Shift_JIS'>";
        HtmlEncodingDetectionResult result = Detect(markup, "unknown");

        Assert.Multiple(() =>
        {
            Assert.That(result.GetEncodingName(), Is.EqualTo("Shift_JIS"));
            Assert.That(result.GetConfidence(), Is.EqualTo(HtmlEncodingConfidence.Tentative));
            Assert.That(result.GetSource(), Is.EqualTo(HtmlEncodingSource.Meta));
            Assert.That(result.HasDeclarationByteOffset(), Is.True);
            Assert.That(result.GetDeclarationByteOffset(), Is.EqualTo(2));
        });
    }

    [Test]
    public void HttpEquivContentDeclarationWorksInEitherAttributeOrder()
    {
        HtmlEncodingDetectionResult contentFirst = Detect(
            "<meta content=\"text/html; charset = koi8-r\" http-equiv=CONTENT-TYPE>", "");
        HtmlEncodingDetectionResult pragmaFirst = Detect(
            "<meta HTTP-EQUIV='content-type' content='text/html;charset=\"euc-jp\"'>", "");

        Assert.That(contentFirst.GetEncodingName(), Is.EqualTo("KOI8-R"));
        Assert.That(contentFirst.GetSource(), Is.EqualTo(HtmlEncodingSource.Meta));
        Assert.That(pragmaFirst.GetEncodingName(), Is.EqualTo("EUC-JP"));
    }

    [Test]
    public void ContentDeclarationRequiresTheContentTypePragma()
    {
        HtmlEncodingDetectionResult missing = Detect("<meta content='text/html;charset=utf-8'>", "");
        HtmlEncodingDetectionResult wrong = Detect(
            "<meta http-equiv=refresh content='text/html;charset=utf-8'>", "");

        Assert.That(missing.GetEncodingName(), Is.EqualTo("windows-1252"));
        Assert.That(missing.GetSource(), Is.EqualTo(HtmlEncodingSource.Default));
        Assert.That(wrong.GetSource(), Is.EqualTo(HtmlEncodingSource.Default));
    }

    [Test]
    public void FirstDuplicateAttributeAndFirstValidMetaDeclarationWin()
    {
        HtmlEncodingDetectionResult duplicate = Detect(
            "<meta charset=iso-8859-1 charset=utf-8><meta charset=koi8-u>", "");

        Assert.That(duplicate.GetEncodingName(), Is.EqualTo("windows-1252"));
        Assert.That(duplicate.GetDeclarationByteOffset(), Is.Zero);
    }

    [TestCase("<meta charset=utf-16>", "UTF-8")]
    [TestCase("<meta charset=utf-16be>", "UTF-8")]
    [TestCase("<meta charset=x-user-defined>", "windows-1252")]
    public void MetaDeclarationsRemapUnsafeEncodings(string markup, string expected)
    {
        Assert.That(Detect(markup, "").GetEncodingName(), Is.EqualTo(expected));
    }

    [Test]
    public void PrescanSkipsCommentsAndOtherTagAttributes()
    {
        const string markup = "<!-- <meta charset=utf-8> -->"
            + "<div title='<meta charset=gbk>'></div><meta charset=koi8-u>";

        HtmlEncodingDetectionResult result = Detect(markup, "");

        Assert.That(result.GetEncodingName(), Is.EqualTo("KOI8-U"));
        Assert.That(result.GetDeclarationByteOffset(), Is.EqualTo(markup.LastIndexOf("<meta", StringComparison.Ordinal)));
    }

    [Test]
    public void PrescanIsLimitedToTheFirst1024Bytes()
    {
        string outside = new string('x', 1024) + "<meta charset=utf-8>";
        string inside = new string('x', 990) + "<meta charset=utf-8>";

        Assert.That(Detect(outside, "").GetSource(), Is.EqualTo(HtmlEncodingSource.Default));
        Assert.That(Detect(inside, "").GetSource(), Is.EqualTo(HtmlEncodingSource.Meta));
    }

    [TestCase(new byte[] { 0x3C, 0x00, 0x3F, 0x00, 0x78, 0x00 }, "UTF-16LE")]
    [TestCase(new byte[] { 0x00, 0x3C, 0x00, 0x3F, 0x00, 0x78 }, "UTF-16BE")]
    public void XmlBytePrefixesAreTentativeEncodingDeclarations(byte[] input, string expected)
    {
        HtmlEncodingDetectionResult result = HtmlEncodingDetector.Detect(input);

        Assert.That(result.GetEncodingName(), Is.EqualTo(expected));
        Assert.That(result.GetConfidence(), Is.EqualTo(HtmlEncodingConfidence.Tentative));
        Assert.That(result.GetSource(), Is.EqualTo(HtmlEncodingSource.XmlDeclaration));
        Assert.That(result.GetDeclarationByteOffset(), Is.Zero);
    }

    [Test]
    public void XmlDeclarationIsUsedOnlyAsAFallbackAndRemapsUtf16()
    {
        HtmlEncodingDetectionResult xml = Detect("<?xml version='1.0' encoding='utf-16'?><p>x", "");
        HtmlEncodingDetectionResult meta = Detect(
            "<?xml version='1.0' encoding='koi8-r'?><meta charset=utf-8>", "");

        Assert.That(xml.GetEncodingName(), Is.EqualTo("UTF-8"));
        Assert.That(xml.GetSource(), Is.EqualTo(HtmlEncodingSource.XmlDeclaration));
        Assert.That(meta.GetEncodingName(), Is.EqualTo("UTF-8"));
        Assert.That(meta.GetSource(), Is.EqualTo(HtmlEncodingSource.Meta));
    }

    [Test]
    public void ConfiguredDefaultIsTentativeAndInvalidDefaultUsesWindows1252()
    {
        HtmlEncodingDetectionResult configured = HtmlEncodingDetector.Detect(
            Encoding.ASCII.GetBytes("plain text"), "", "gb18030");
        HtmlEncodingDetectionResult invalid = HtmlEncodingDetector.Detect(
            ReadOnlySpan<byte>.Empty, "", "not-an-encoding");

        Assert.That(configured.GetEncodingName(), Is.EqualTo("gb18030"));
        Assert.That(configured.GetConfidence(), Is.EqualTo(HtmlEncodingConfidence.Tentative));
        Assert.That(configured.GetSource(), Is.EqualTo(HtmlEncodingSource.Default));
        Assert.That(configured.HasDeclarationByteOffset(), Is.False);
        Assert.That(configured.GetDeclarationByteOffset(), Is.EqualTo(-1));
        Assert.That(invalid.GetEncodingName(), Is.EqualTo("windows-1252"));
    }

    [Test]
    public void RejectsNullLabels()
    {
        Assert.Throws<ArgumentNullException>(() => HtmlEncodingDetector.GetEncodingName((string)null));
        Assert.Throws<ArgumentNullException>(() => HtmlEncodingDetector.Detect(ReadOnlySpan<byte>.Empty, null));
        Assert.Throws<ArgumentNullException>(() => HtmlEncodingDetector.Detect(ReadOnlySpan<byte>.Empty, "", null));
    }

    static HtmlEncodingDetectionResult Detect(string markup, string transportLabel)
    {
        return HtmlEncodingDetector.Detect(Encoding.ASCII.GetBytes(markup), transportLabel);
    }
}
