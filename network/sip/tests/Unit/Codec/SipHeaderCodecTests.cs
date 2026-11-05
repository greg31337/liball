namespace sip.Tests;

public class SipHeaderCodecTests
{
    [Test]
    public void ParsesGenericHeaderAndOwnsPreservedBytes()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        byte[] source = System.Text.Encoding.UTF8.GetBytes("Subject \t:  Project café \t\r\n");
        int sourceLength = source.Length;

        SipHeaderParseResult result = codec.ParseHeader(source, new SipParseOptions());
        source[0] = (byte)'X';

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(result.Consumed, Is.EqualTo(sourceLength));
        Assert.That(result.RequiredBytes, Is.EqualTo(sourceLength));
        Assert.That(result.Header!.Name, Is.EqualTo("Subject"));
        Assert.That(result.Header.Value, Is.EqualTo("Project café"));
        Assert.That(result.Header.OriginalBytes[0], Is.EqualTo((byte)'S'));
        Assert.That(result.NameAddress, Is.Null);
    }

    [Test]
    public void HeaderFoldingRequiresExplicitCompatibilityOption()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        byte[] source = System.Text.Encoding.ASCII.GetBytes("Subject: first\r\n\tsecond\r\n");
        SipParseOptions compatible = new SipParseOptions();
        compatible.AllowHeaderFolding = true;

        SipHeaderParseResult strict = codec.ParseHeader(source, new SipParseOptions());
        SipHeaderParseResult accepted = codec.ParseHeader(source, compatible);

        Assert.That(strict.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(accepted.Status, Is.EqualTo(SipParseStatus.Complete));
        Assert.That(accepted.Header!.Value, Is.EqualTo("first second"));
        SipHeaderParseResult collapsed = codec.ParseHeader(
            System.Text.Encoding.ASCII.GetBytes("Subject: first \t\r\n  second"), compatible);
        Assert.That(collapsed.Header!.Value, Is.EqualTo("first second"));
        Assert.That(codec.ParseHeader(System.Text.Encoding.ASCII.GetBytes("Subject\r\n : value"), compatible).Status,
            Is.EqualTo(SipParseStatus.Error));
    }

    [Test]
    public void HeaderErrorsUseUtf8ByteOffsets()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        byte[] source = System.Text.Encoding.UTF8.GetBytes("Subject: café\u007F");

        SipHeaderParseResult result = codec.ParseHeader(source, new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ErrorOffset, Is.EqualTo(14));
    }

    [TestCase("NoColon", 0)]
    [TestCase("Bad Name: value", 0)]
    [TestCase("Subject: bad\nvalue", 12)]
    [TestCase("Subject: bad\u007Fvalue", 12)]
    public void MalformedGenericHeadersReturnErrors(string text, int expectedOffset)
    {
        SipHeaderCodec codec = new SipHeaderCodec();

        SipHeaderParseResult result = codec.ParseHeader(System.Text.Encoding.UTF8.GetBytes(text), new SipParseOptions());

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ErrorOffset, Is.EqualTo(expectedOffset));
        Assert.That(result.Header, Is.Null);
    }

    [Test]
    public void HeaderParsingHonorsConfiguredLineLimit()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipParseOptions options = new SipParseOptions();
        options.MaxHeaderLineBytes = 8;

        SipHeaderParseResult result = codec.ParseHeader(
            System.Text.Encoding.ASCII.GetBytes("Subject: value"), options);

        Assert.That(result.Status, Is.EqualTo(SipParseStatus.Error));
        Assert.That(result.ErrorOffset, Is.EqualTo(8));
        Assert.That(result.ErrorMessage, Does.Contain("configured size"));
    }

    [Test]
    public void ValidatesAndFormatsGenericHeaders()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeader header = new SipHeader();
        header.Name = "X-Trace";
        header.Value = "café";

        SipValidationIssue[] issues = codec.ValidateHeader(header);
        SipWriteResult result = codec.FormatHeader(header, new SipWriteOptions());

        Assert.That(issues, Is.Empty);
        Assert.That(result.Success, Is.True);
        Assert.That(System.Text.Encoding.UTF8.GetString(result.Bytes), Is.EqualTo("X-Trace: café\r\n"));
        Assert.That(result.Written, Is.EqualTo(result.Bytes.Length));
        Assert.That(result.RequiredBytes, Is.EqualTo(result.Bytes.Length));
    }

    [Test]
    public void FormattingCanReusePreservedBytesWithoutSharingThem()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeader header = new SipHeader();
        header.Name = "Subject";
        header.Value = "value";
        header.OriginalBytes = System.Text.Encoding.ASCII.GetBytes("s:  value\r\n");
        SipWriteOptions options = new SipWriteOptions();
        options.PreserveOriginal = true;

        SipWriteResult result = codec.FormatHeader(header, options);
        result.Bytes[0] = (byte)'X';

        Assert.That(result.Success, Is.True);
        Assert.That(header.OriginalBytes[0], Is.EqualTo((byte)'s'));
    }

    [Test]
    public void InvalidModelsAndOutputLimitsFailFormatting()
    {
        SipHeaderCodec codec = new SipHeaderCodec();
        SipHeader invalid = new SipHeader();
        invalid.Name = "Bad Name";
        invalid.Value = "value";
        SipHeader valid = new SipHeader();
        valid.Name = "Subject";
        valid.Value = "value";
        SipWriteOptions limited = new SipWriteOptions();
        limited.MaxHeaderLineBytes = 5;

        Assert.That(codec.ValidateHeader(invalid), Is.Not.Empty);
        Assert.That(codec.FormatHeader(invalid, new SipWriteOptions()).Success, Is.False);
        SipWriteResult tooLong = codec.FormatHeader(valid, limited);
        Assert.That(tooLong.Success, Is.False);
        Assert.That(tooLong.RequiredBytes, Is.EqualTo(16));
    }
}
