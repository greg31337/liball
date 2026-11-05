namespace svg.Tests;

public class SvgContainerReaderTests
{
    [Test]
    public void CharacterSourcesAreAlreadyDecodedPlainXml()
    {
        SvgLoadResult text = new SvgLoader().LoadWithStringAndSvgLoadOptions("<svg>é</svg>", new SvgLoadOptions());
        System.Xml.XmlReader reader = System.Xml.XmlReader.Create(new System.IO.StringReader("<svg/>"));
        SvgLoadResult xmlReader = new SvgLoader().LoadWithXmlReaderAndSvgLoadOptions(reader, new SvgLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(text.Success, Is.True);
            Assert.That(text.DecodedCharacters, Is.EqualTo("<svg>é</svg>"));
            Assert.That(text.DetectedContainer, Is.EqualTo("xml"));
            Assert.That(text.DetectedEncoding, Is.EqualTo("utf-16"));
            Assert.That(text.DetectedMediaType, Is.EqualTo(SvgMimeTypes.Svg));
            Assert.That(xmlReader.Success, Is.True);
            Assert.That(xmlReader.DetectedContainer, Is.EqualTo("xml-reader"));
            Assert.That(xmlReader.DecodedCharacters, Is.Null);
        });

        reader.Dispose();
    }

    [TestCase(false, "utf-16")]
    [TestCase(true, "utf-16be")]
    public void Utf16ByteOrderMarksAndDeclarationsAreDetected(bool bigEndian, string expectedEncoding)
    {
        string xml = "<?xml version=\"1.0\" encoding=\"UTF-16\"?><svg>é</svg>";
        System.Text.Encoding encoding = new System.Text.UnicodeEncoding(bigEndian, true, true);
        byte[] bytes = WithPreamble(encoding, xml);

        SvgLoadResult result = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(bytes, new SvgLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DecodedCharacters, Is.EqualTo(xml));
            Assert.That(result.DetectedEncoding, Is.EqualTo(expectedEncoding));
            Assert.That(result.DetectedContainer, Is.EqualTo("xml"));
            Assert.That(result.Source.Bytes, Is.EqualTo(bytes));
        });
    }

    [Test]
    public void Utf8BomAndDeclarationAreDecodedStrictly()
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?><svg>é😀</svg>";
        System.Text.Encoding encoding = new System.Text.UTF8Encoding(true, true);
        byte[] bytes = WithPreamble(encoding, xml);

        SvgLoadResult result = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(bytes, new SvgLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DecodedCharacters, Is.EqualTo(xml));
            Assert.That(result.DetectedEncoding, Is.EqualTo("utf-8"));
            Assert.That(result.DetectedContainer, Is.EqualTo("xml"));
        });
    }

    [Test]
    public void XmlDeclarationsCanSelectSupportedNonUnicodeEncodings()
    {
        string xml = "<?xml version='1.0' encoding='iso-8859-1'?><svg>é</svg>";
        byte[] bytes = System.Text.Encoding.Latin1.GetBytes(xml);

        SvgLoadResult result = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(bytes, new SvgLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DecodedCharacters, Is.EqualTo(xml));
            Assert.That(result.DetectedEncoding, Is.EqualTo("iso-8859-1"));
        });
    }

    [Test]
    public void UnsupportedMismatchedAndMalformedEncodingsReturnLocatedDiagnostics()
    {
        string unsupportedXml = "<?xml version='1.0' encoding='x-not-an-encoding'?><svg/>";
        SvgLoadOptions sourceOptions = new SvgLoadOptions();
        sourceOptions.BaseUri = "memory:unsupported";
        SvgLoadResult unsupported = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(System.Text.Encoding.ASCII.GetBytes(unsupportedXml), sourceOptions);

        string mismatchXml = "<?xml version='1.0' encoding='UTF-16'?><svg/>";
        byte[] mismatchBytes = WithPreamble(new System.Text.UTF8Encoding(true, true), mismatchXml);
        SvgLoadResult mismatch = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(mismatchBytes, new SvgLoadOptions());

        byte[] malformedBytes = new byte[] { (byte)'<', (byte)'s', (byte)'v', (byte)'g', (byte)'>', 195, (byte)'<' };
        SvgLoadResult malformed = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(malformedBytes, new SvgLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(unsupported.Success, Is.False);
            Assert.That(unsupported.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.UnsupportedEncoding));
            Assert.That(unsupported.Diagnostics[0].Source.StartOffset, Is.EqualTo(unsupportedXml.IndexOf("x-not", System.StringComparison.Ordinal)));
            Assert.That(unsupported.Diagnostics[0].Source.Source.Uri, Is.EqualTo("memory:unsupported"));
            Assert.That(mismatch.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.UnsupportedEncoding));
            Assert.That(mismatch.Diagnostics[0].Source.StartOffset, Is.EqualTo(mismatchXml.IndexOf("UTF-16", System.StringComparison.Ordinal)));
            Assert.That(malformed.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.UnsupportedEncoding));
            Assert.That(malformed.Diagnostics[0].Source.Source.Uri, Is.Empty);
            Assert.That(malformed.Diagnostics[0].Source.StartOffset, Is.EqualTo(-1));
        });
    }

    [Test]
    public void GzipMagicSelectsSvgzRegardlessOfFilenameExtension()
    {
        string xml = "<?xml version='1.0'?><svg>compressed</svg>";
        byte[] compressed = Compress(System.Text.Encoding.UTF8.GetBytes(xml));
        string gzipPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString("N") + ".svg");
        string plainPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString("N") + ".svgz");
        try
        {
            System.IO.File.WriteAllBytes(gzipPath, compressed);
            System.IO.File.WriteAllText(plainPath, xml, new System.Text.UTF8Encoding(false));

            SvgLoadResult gzip = new SvgLoader().LoadPath(gzipPath, new SvgLoadOptions());
            SvgLoadResult plain = new SvgLoader().LoadPath(plainPath, new SvgLoadOptions());

            Assert.Multiple(() =>
            {
                Assert.That(gzip.Success, Is.True);
                Assert.That(gzip.DetectedContainer, Is.EqualTo("gzip"));
                Assert.That(gzip.DetectedEncoding, Is.EqualTo("utf-8"));
                Assert.That(gzip.DecodedCharacters, Is.EqualTo(xml));
                Assert.That(gzip.Source.Bytes, Is.EqualTo(compressed));
                Assert.That(plain.Success, Is.True);
                Assert.That(plain.DetectedContainer, Is.EqualTo("xml"));
                Assert.That(plain.DecodedCharacters, Is.EqualTo(xml));
            });
        }
        finally
        {
            System.IO.File.Delete(gzipPath);
            System.IO.File.Delete(plainPath);
        }
    }

    [Test]
    public void MalformedAndTruncatedGzipContainersReturnFormatDiagnostics()
    {
        byte[] truncated = new byte[] { 31, 139, 8, 0, 0, 0 };
        byte[] corrupt = Compress(System.Text.Encoding.UTF8.GetBytes("<svg/>"));
        corrupt[corrupt.Length - 8] = (byte)(corrupt[corrupt.Length - 8] ^ 255);

        SvgLoadResult truncatedResult = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(truncated, new SvgLoadOptions());
        SvgLoadResult corruptResult = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(corrupt, new SvgLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(truncatedResult.Success, Is.False);
            Assert.That(truncatedResult.DetectedContainer, Is.EqualTo("gzip"));
            Assert.That(truncatedResult.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InvalidContainer));
            Assert.That(truncatedResult.Diagnostics[0].Source.Source.Uri, Is.Empty);
            Assert.That(corruptResult.Success, Is.False);
            Assert.That(corruptResult.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InvalidContainer));
        });
    }

    [Test]
    public void GzipOutputAndExpansionRatioLimitsAreEnforcedDuringDecompression()
    {
        string xml = "<svg>" + new string('x', 4096) + "</svg>";
        byte[] compressed = Compress(System.Text.Encoding.UTF8.GetBytes(xml));
        SvgLoadOptions outputOptions = new SvgLoadOptions();
        outputOptions.Limits.Input.MaxGzipOutputBytes = 100;
        SvgLoadOptions ratioOptions = new SvgLoadOptions();
        ratioOptions.Limits.Input.MaxGzipExpansionRatio = 2;

        SvgLoadResult output = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(compressed, outputOptions);
        SvgLoadResult ratio = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(compressed, ratioOptions);

        Assert.Multiple(() =>
        {
            Assert.That(output.Success, Is.False);
            Assert.That(output.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(ratio.Success, Is.False);
            Assert.That(ratio.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
        });
    }

    [Test]
    public void DecodedCharacterLimitsApplyToPlainAndCompressedXml()
    {
        string xml = "<svg>1234567890</svg>";
        byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(xml);
        byte[] compressed = Compress(plainBytes);
        SvgLoadOptions plainOptions = new SvgLoadOptions();
        plainOptions.Limits.Input.MaxInputCharacters = 8;
        SvgLoadOptions gzipOptions = new SvgLoadOptions();
        gzipOptions.Limits.Input.MaxInputCharacters = 8;

        SvgLoadResult plain = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(plainBytes, plainOptions);
        SvgLoadResult gzip = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(compressed, gzipOptions);

        Assert.Multiple(() =>
        {
            Assert.That(plain.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(plain.Diagnostics[0].Source.StartOffset, Is.EqualTo(8));
            Assert.That(gzip.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(gzip.Diagnostics[0].Source.StartOffset, Is.EqualTo(8));
        });
    }

    static byte[] WithPreamble(System.Text.Encoding encoding, string text)
    {
        byte[] preamble = encoding.GetPreamble();
        byte[] content = encoding.GetBytes(text);
        byte[] bytes = new byte[preamble.Length + content.Length];
        System.Array.Copy(preamble, 0, bytes, 0, preamble.Length);
        System.Array.Copy(content, 0, bytes, preamble.Length, content.Length);
        return bytes;
    }

    static byte[] Compress(byte[] value)
    {
        using System.IO.MemoryStream output = new System.IO.MemoryStream();
        using (System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.SmallestSize, true))
        {
            gzip.Write(value, 0, value.Length);
        }
        return output.ToArray();
    }
}
