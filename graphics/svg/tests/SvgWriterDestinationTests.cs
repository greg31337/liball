namespace svg.Tests;

public class SvgWriterDestinationTests
{
    [Test]
    public void StringTextWriterStreamAndUtf8ArrayProduceTheSameCallerOwnedOutput()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><text>Grüße ✓</text></svg>");
        SvgWriteOptions options = new SvgWriteOptions();
        SvgWriteResult text = new SvgWriter().WriteWithDocumentAndOptions(document, options);
        StringWriter textWriter = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        MemoryStream stream = new MemoryStream();

        SvgWriteResult textWriterResult = new SvgWriter().WriteWithSvgDocumentAndTextWriterAndSvgWriteOptions(document, textWriter, options);
        SvgWriteResult streamResult = new SvgWriter().WriteWithSvgDocumentAndStreamAndSvgWriteOptions(document, stream, options);
        SvgWriteBytesResult bytes = new SvgWriter().WriteUtf8WithDocumentAndOptions(document, options);
        textWriter.Write("tail");
        stream.WriteByte(0x7f);

        Assert.Multiple(() =>
        {
            Assert.That(text.Success, Is.True, text.Message);
            Assert.That(textWriterResult.Success, Is.True, textWriterResult.Message);
            Assert.That(streamResult.Success, Is.True, streamResult.Message);
            Assert.That(bytes.Success, Is.True, bytes.WriteResult.Message);
            Assert.That(textWriter.ToString(), Is.EqualTo(text.Text + "tail"));
            Assert.That(stream.CanWrite, Is.True);
            Assert.That(stream.ToArray()[^1], Is.EqualTo(0x7f));
            Assert.That(stream.ToArray()[..^1], Is.EqualTo(bytes.Bytes));
            Assert.That(bytes.Bytes, Is.EqualTo(new System.Text.UTF8Encoding(false).GetBytes(text.Text)));
            Assert.That(bytes.ByteCount, Is.EqualTo(bytes.Bytes.Length));
            Assert.That(bytes.Compressed, Is.False);
            Assert.That(bytes.Bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xef, 0xbb, 0xbf }));
        });

        textWriter.Dispose();
        stream.Dispose();
    }

    [Test]
    public void CallerSpansReportRequiredCapacityAndNeverWritePartialOutput()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><text>✓</text></svg>");
        SvgWriteOptions options = new SvgWriteOptions();
        SvgWriteBytesResult expectedBytes = new SvgWriter().WriteUtf8WithDocumentAndOptions(document, options);
        SvgWriteResult expectedText = new SvgWriter().WriteWithDocumentAndOptions(document, options);
        byte[] smallBytes = Enumerable.Repeat((byte)0x7f, expectedBytes.ByteCount - 1).ToArray();
        char[] smallCharacters = Enumerable.Repeat('x', expectedText.Text.Length - 1).ToArray();
        byte[] exactBytes = new byte[expectedBytes.ByteCount];
        char[] exactCharacters = new char[expectedText.Text.Length];

        SvgWriteBufferResult smallByteResult = new SvgWriter().WriteUtf8WithDocumentAndDestinationAndOptions(document, smallBytes.AsSpan(), options);
        SvgWriteBufferResult smallCharacterResult = new SvgWriter().WriteWithSvgDocumentAndSpanOfCharAndSvgWriteOptions(document, smallCharacters.AsSpan(), options);
        SvgWriteBufferResult byteResult = new SvgWriter().WriteUtf8WithDocumentAndDestinationAndOptions(document, exactBytes.AsSpan(), options);
        SvgWriteBufferResult characterResult = new SvgWriter().WriteWithSvgDocumentAndSpanOfCharAndSvgWriteOptions(document, exactCharacters.AsSpan(), options);

        Assert.Multiple(() =>
        {
            Assert.That(smallByteResult.Error, Is.EqualTo(SvgWriteError.DestinationTooSmall));
            Assert.That(smallByteResult.RequiredLength, Is.EqualTo(expectedBytes.ByteCount));
            Assert.That(smallByteResult.WrittenLength, Is.Zero);
            Assert.That(smallBytes, Has.All.EqualTo(0x7f));
            Assert.That(smallCharacterResult.Error, Is.EqualTo(SvgWriteError.DestinationTooSmall));
            Assert.That(smallCharacterResult.RequiredLength, Is.EqualTo(expectedText.Text.Length));
            Assert.That(smallCharacters, Has.All.EqualTo('x'));
            Assert.That(byteResult.Success, Is.True, byteResult.Message);
            Assert.That(byteResult.Kind, Is.EqualTo(SvgWriteBufferKind.Utf8Bytes));
            Assert.That(exactBytes, Is.EqualTo(expectedBytes.Bytes));
            Assert.That(characterResult.Success, Is.True, characterResult.Message);
            Assert.That(characterResult.Kind, Is.EqualTo(SvgWriteBufferKind.Utf16Characters));
            Assert.That(new string(exactCharacters), Is.EqualTo(expectedText.Text));
        });
    }

    [Test]
    public void SvgzIsDeterministicReloadableAndLeavesDestinationStreamsOpen()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><text>compressed ✓</text></svg>");
        SvgWriteOptions options = new SvgWriteOptions();
        SvgWriteBytesResult first = new SvgWriter().WriteSvgzWithDocumentAndOptions(document, options);
        SvgWriteBytesResult second = new SvgWriter().WriteSvgzWithDocumentAndOptions(document, options);
        MemoryStream destination = new MemoryStream();

        SvgWriteBytesResult streamed = new SvgWriter().WriteSvgzWithDocumentAndDestinationAndOptions(document, destination, options);
        SvgLoadResult reloaded = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(first.Bytes, new SvgLoadOptions());
        destination.WriteByte(0x7f);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, first.WriteResult.Message);
            Assert.That(first.Compressed, Is.True);
            Assert.That(first.Bytes, Is.EqualTo(second.Bytes));
            Assert.That(first.Bytes[0], Is.EqualTo(0x1f));
            Assert.That(first.Bytes[1], Is.EqualTo(0x8b));
            Assert.That(streamed.Bytes, Is.EqualTo(first.Bytes));
            Assert.That(destination.CanWrite, Is.True);
            Assert.That(destination.ToArray()[..^1], Is.EqualTo(first.Bytes));
            Assert.That(reloaded.Success, Is.True, Diagnostics(reloaded.Diagnostics));
            Assert.That(new SvgNodeApi().GetTextContent(reloaded.Document.DocumentElement), Is.EqualTo("compressed ✓"));
        });

        destination.Dispose();
    }

    [Test]
    public void DataUrlsSupportUnencodedPercentEncodedAndBase64Utf8Payloads()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><text>✓</text></svg>");
        SvgWriteOptions options = new SvgWriteOptions();
        SvgWriteBytesResult expected = new SvgWriter().WriteUtf8WithDocumentAndOptions(document, options);

        SvgWriteDataUrlResult raw = new SvgWriter().WriteDataUrlWithDocumentAndEncodingAndOptions(document, SvgDataUrlWriteEncoding.Unencoded, options);
        SvgWriteDataUrlResult percent = new SvgWriter().WriteDataUrlWithDocumentAndEncodingAndOptions(document, SvgDataUrlWriteEncoding.PercentEncoded, options);
        SvgWriteDataUrlResult base64 = new SvgWriter().WriteDataUrlWithDocumentAndEncodingAndOptions(document, SvgDataUrlWriteEncoding.Base64, options);
        SvgDataUrlResult parsedPercent = new SvgDataUrl().ParseWithValue(percent.DataUrl);
        SvgDataUrlResult parsedBase64 = new SvgDataUrl().ParseWithValue(base64.DataUrl);
        SvgWriteDataUrlResult invalid = new SvgWriter().WriteDataUrlWithDocumentAndEncodingAndOptions(document, (SvgDataUrlWriteEncoding)99, options);

        Assert.Multiple(() =>
        {
            Assert.That(raw.Success, Is.True, raw.Message);
            Assert.That(raw.DataUrl, Is.EqualTo("data:image/svg+xml;charset=utf-8," + expected.WriteResult.Text));
            Assert.That(raw.DataUrl, Does.Contain("<svg"));
            Assert.That(percent.Success, Is.True, percent.Message);
            Assert.That(percent.DataUrl, Does.Not.Contain("<svg"));
            Assert.That(parsedPercent.Success, Is.True);
            Assert.That(parsedPercent.Bytes, Is.EqualTo(expected.Bytes));
            Assert.That(base64.Success, Is.True, base64.Message);
            Assert.That(base64.DataUrl, Does.Contain(";base64,"));
            Assert.That(parsedBase64.Success, Is.True);
            Assert.That(parsedBase64.Bytes, Is.EqualTo(expected.Bytes));
            Assert.That(new[] { raw, percent, base64 }, Has.All.Matches<SvgWriteDataUrlResult>(result => result.ByteCount == expected.ByteCount));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Error, Is.EqualTo(SvgWriteError.InvalidArgument));
        });
    }

    [Test]
    public void Utf8DestinationsUseMatchingDeclarationsWithoutMutatingOptions()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'/>");
        SvgWriteOptions options = new SvgWriteOptions();
        options.DeclarationPolicy = SvgXmlDeclarationPolicy.Include;
        options.DeclarationEncoding = "utf-16";

        SvgWriteResult text = new SvgWriter().WriteWithDocumentAndOptions(document, options);
        SvgWriteBytesResult bytes = new SvgWriter().WriteUtf8WithDocumentAndOptions(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(text.Text, Does.Contain("encoding=\"utf-16\""));
            Assert.That(bytes.WriteResult.Text, Does.Contain("encoding=\"utf-8\""));
            Assert.That(options.DeclarationEncoding, Is.EqualTo("utf-16"));
        });
    }

    [Test]
    public void SerializationFailuresDoNotTouchCallerDestinations()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'/>");
        SvgWriteOptions options = new SvgWriteOptions();
        options.MaximumCharacters = 1;
        StringWriter writer = new StringWriter();
        writer.Write("prefix");
        MemoryStream stream = new MemoryStream(new byte[] { 1, 2, 3 }, true);
        stream.Position = stream.Length;

        SvgWriteResult writerResult = new SvgWriter().WriteWithSvgDocumentAndTextWriterAndSvgWriteOptions(document, writer, options);
        SvgWriteResult streamResult = new SvgWriter().WriteWithSvgDocumentAndStreamAndSvgWriteOptions(document, stream, options);

        Assert.Multiple(() =>
        {
            Assert.That(writerResult.Success, Is.False);
            Assert.That(streamResult.Success, Is.False);
            Assert.That(writer.ToString(), Is.EqualTo("prefix"));
            Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(writerResult.Error, Is.EqualTo(SvgWriteError.LimitExceeded));
        });

        writer.Dispose();
        stream.Dispose();
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, Diagnostics(result.Diagnostics));
        return result.Document;
    }

    static string Diagnostics(List<SvgDiagnostic> diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(diagnostic => diagnostic.Message));
    }
}
