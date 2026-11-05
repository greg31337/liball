using System.Text;

namespace sdp.Tests.Api;

public class SdpWriterValidationTests
{
    [Test]
    public void Writer_RejectsUninitializedCollectionsWithoutReturningPartialOutput()
    {
        SdpSessionDescription description = MinimalDescription();
        description.Bandwidths = null!;
        ISdpWriter writer = new SdpWriter();
        byte[] destination = Enumerable.Repeat((byte)0x6D, 128).ToArray();

        SdpWriteResult text = writer.WriteText(description, new SdpWriteOptions());
        SdpWriteResult utf8 = writer.WriteUtf8Bytes(description, new SdpWriteOptions());
        SdpBufferWriteResult buffered = writer.WriteUtf8Buffer(description, destination, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(text.Success, Is.False);
            Assert.That(text.Text, Is.Empty);
            Assert.That(text.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.WriteInvalidDescription));
            Assert.That(utf8.Success, Is.False);
            Assert.That(utf8.Utf8Bytes, Is.Empty);
            Assert.That(utf8.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.WriteInvalidDescription));
            Assert.That(buffered.Success, Is.False);
            Assert.That(buffered.BytesWritten, Is.Zero);
            Assert.That(destination, Has.All.EqualTo((byte)0x6D));
        });
    }

    [Test]
    public void Writer_ValidatesIncludedEncryptionKeysWithoutDisclosingTheirValues()
    {
        const string sensitiveValue = "top-secret\r\ninjected";
        SdpSessionDescription description = MinimalDescription();
        description.EncryptionKey = new SdpEncryptionKey
        {
            Method = "clear",
            HasValue = true,
            Value = sensitiveValue,
            RawText = "clear:" + sensitiveValue
        };

        SdpWriteResult result = new SdpWriter().WriteText(description, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.WriteInvalidDescription));
            Assert.That(result.Diagnostics[0].FieldName, Is.EqualTo('k'));
            Assert.That(result.Diagnostics[0].Detail, Does.Not.Contain(sensitiveValue));
            Assert.That(result.Diagnostics[0].Detail, Does.Not.Contain("top-secret"));
        });
    }

    [Test]
    public void Writer_DoesNotValidateFieldsExcludedByWriteOptions()
    {
        SdpSessionDescription description = MinimalDescription();
        description.EncryptionKey = new SdpEncryptionKey
        {
            Method = "clear",
            HasValue = false,
            Value = "unrepresentable-secret"
        };
        description.Attributes.Add(new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = "not a token",
            Value = "unrepresentable\r\nvalue"
        });
        SdpWriteOptions options = new SdpWriteOptions
        {
            IncludeObsoleteFields = false,
            IncludeUnknownFields = false
        };

        SdpWriteResult result = new SdpWriter().WriteText(description, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Does.Not.Contain("k="));
            Assert.That(result.Text, Does.Not.Contain("not a token"));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void LosslessWriter_RejectsInconsistentRetainedLinesAndInvalidEdits()
    {
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 192.0.2.1\r\n" +
            "s=Original\r\n" +
            "c=IN IP4 192.0.2.1\r\n" +
            "t=0 0\r\n";
        SdpParseResult firstParsed = new SdpParser().ParseText(input, LosslessParseOptions());
        SdpSessionDescription inconsistentRaw = firstParsed.Description!;
        inconsistentRaw.RawLines[2].Text = "s=Changed-without-matching-value";
        SdpParseResult secondParsed = new SdpParser().ParseText(input, LosslessParseOptions());
        SdpSessionDescription invalidEdit = secondParsed.Description!;
        invalidEdit.SessionName = "Injected\r\na=x:line";
        SdpWriteOptions options = new SdpWriteOptions { Mode = SdpWriteMode.Lossless };

        SdpWriteResult rawResult = new SdpWriter().WriteText(inconsistentRaw, options);
        SdpWriteResult editResult = new SdpWriter().WriteText(invalidEdit, options);

        Assert.Multiple(() =>
        {
            Assert.That(rawResult.Success, Is.False);
            Assert.That(rawResult.Text, Is.Empty);
            Assert.That(rawResult.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.WriteInvalidDescription));
            Assert.That(editResult.Success, Is.False);
            Assert.That(editResult.Text, Is.Empty);
            Assert.That(editResult.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.WriteInvalidDescription));
        });
    }

    [Test]
    public void Writer_ThrowingTextPathMatchesResultPathAndReportsStableFailureCode()
    {
        ISdpWriter writer = new SdpWriter();
        SdpSessionDescription valid = MinimalDescription();
        SdpWriteOptions options = new SdpWriteOptions();
        SdpWriteResult expected = writer.WriteText(valid, options);
        string actual = writer.WriteTextOrThrow(valid, options);
        SdpSessionDescription invalid = MinimalDescription();
        invalid.SessionName = "private-value\r\ninjected";

        SdpGenerationException exception = Assert.Throws<SdpGenerationException>(() => writer.WriteTextOrThrow(invalid, options))!;

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected.Text));
            Assert.That(exception.Code, Is.EqualTo(SdpDiagnosticCodes.WriteInvalidDescription));
            Assert.That(exception.Message, Is.EqualTo("SDP generation failed."));
            Assert.That(exception.Message, Does.Not.Contain("private-value"));
            Assert.That(exception.InnerException, Is.Null);
        });
    }

    [Test]
    public void Writer_ThrowingUtf8PathMatchesResultPathAndReportsOutputLimits()
    {
        ISdpWriter writer = new SdpWriter();
        SdpSessionDescription description = MinimalDescription();
        SdpWriteOptions completeOptions = new SdpWriteOptions();
        SdpWriteResult expected = writer.WriteUtf8Bytes(description, completeOptions);
        byte[] actual = writer.WriteUtf8BytesOrThrow(description, completeOptions);
        SdpWriteOptions limitedOptions = new SdpWriteOptions { MaximumOutputBytes = expected.BytesWritten - 1 };

        SdpGenerationException exception = Assert.Throws<SdpGenerationException>(() => writer.WriteUtf8BytesOrThrow(description, limitedOptions))!;

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected.Utf8Bytes));
            Assert.That(Encoding.UTF8.GetString(actual), Does.Contain("s=Minimal\r\n"));
            Assert.That(exception.Code, Is.EqualTo(SdpDiagnosticCodes.WriteOutputByteLimitExceeded));
        });
    }

    [Test]
    public void Writer_ThrowingPathsKeepProgrammerErrorsAsArgumentExceptions()
    {
        ISdpWriter writer = new SdpWriter();
        SdpSessionDescription description = MinimalDescription();

        Assert.Multiple(() =>
        {
            Assert.That(() => writer.WriteTextOrThrow(null!, new SdpWriteOptions()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => writer.WriteUtf8BytesOrThrow(description, new SdpWriteOptions { Mode = SdpWriteMode.Unknown }), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("options"));
        });
    }

    static SdpParseOptions LosslessParseOptions()
    {
        return new SdpParseOptions
        {
            Mode = SdpParseMode.Lossless,
            AllowLfLineEndings = true,
            AllowCrLineEndings = true
        };
    }

    static SdpSessionDescription MinimalDescription()
    {
        return new SdpSessionFactory().CreateSession(
            new SdpOrigin
            {
                Username = "-",
                SessionId = "1",
                SessionVersion = "1",
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                UnicastAddress = "192.0.2.1",
                AddressKind = SdpAddressKind.Ipv4
            },
            "Minimal",
            new SdpConnectionData
            {
                NetworkType = SdpNetworkTypes.Internet,
                AddressType = SdpAddressTypes.Ip4,
                Address = "192.0.2.1",
                AddressKind = SdpAddressKind.Ipv4
            },
            new SdpTiming { StartTime = "0", StopTime = "0" });
    }
}
