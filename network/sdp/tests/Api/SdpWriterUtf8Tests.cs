using System.Text;

namespace sdp.Tests.Api;

public class SdpWriterUtf8Tests
{
    [Test]
    public void Writer_GeneratesExactUtf8WithoutBomForUnicodeDescriptions()
    {
        SdpSessionDescription description = MinimalDescription("Café 会議");
        ISdpWriter writer = new SdpWriter();
        SdpWriteResult utf8 = writer.WriteUtf8Bytes(description, new SdpWriteOptions());
        string expectedText =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 192.0.2.1\r\n" +
            "s=Café 会議\r\n" +
            "c=IN IP4 192.0.2.1\r\n" +
            "t=0 0\r\n";
        byte[] expected = Encoding.UTF8.GetBytes(expectedText);

        Assert.Multiple(() =>
        {
            Assert.That(utf8.Success, Is.True);
            Assert.That(utf8.Text, Is.Empty);
            Assert.That(utf8.CharactersWritten, Is.Zero);
            Assert.That(utf8.Utf8Bytes, Is.EqualTo(expected));
            Assert.That(Encoding.UTF8.GetString(utf8.Utf8Bytes), Is.EqualTo(expectedText));
            Assert.That(utf8.BytesWritten, Is.EqualTo(expected.Length));
            Assert.That(utf8.Utf8Bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
            Assert.That(utf8.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void Writer_PreservesLosslessTextWhenGeneratingUtf8()
    {
        string input =
            "v=0\n" +
            "o=- 1 1 IN IP4 192.0.2.1\r\n" +
            "s=Café  session\r" +
            "c=IN IP4 192.0.2.1\n" +
            "t=0 0";
        SdpParseResult parsed = new SdpParser().ParseText(input, new SdpParseOptions
        {
            Mode = SdpParseMode.Lossless,
            AllowLfLineEndings = true,
            AllowCrLineEndings = true
        });

        SdpWriteResult result = new SdpWriter().WriteUtf8Bytes(parsed.Description!, new SdpWriteOptions { Mode = SdpWriteMode.Lossless });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Utf8Bytes, Is.EqualTo(Encoding.UTF8.GetBytes(input)));
            Assert.That(result.BytesWritten, Is.EqualTo(Encoding.UTF8.GetByteCount(input)));
        });
    }

    [Test]
    public void Writer_ReportsUtf8ByteLimitWithoutReturningPartialData()
    {
        SdpSessionDescription description = MinimalDescription("é");
        ISdpWriter writer = new SdpWriter();
        SdpWriteResult complete = writer.WriteUtf8Bytes(description, new SdpWriteOptions());
        SdpWriteOptions limitedOptions = new SdpWriteOptions { MaximumOutputBytes = complete.BytesWritten - 1 };

        SdpWriteResult limited = writer.WriteUtf8Bytes(description, limitedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Text, Is.Empty);
            Assert.That(limited.Utf8Bytes, Is.Empty);
            Assert.That(limited.CharactersWritten, Is.Zero);
            Assert.That(limited.BytesWritten, Is.Zero);
            Assert.That(limited.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(limited.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.WriteOutputByteLimitExceeded));
        });
    }

    [Test]
    public void Writer_ReportsExactBufferSizeAndNeverPartiallyWrites()
    {
        SdpSessionDescription description = MinimalDescription("Café");
        ISdpWriter writer = new SdpWriter();
        SdpWriteResult allocated = writer.WriteUtf8Bytes(description, new SdpWriteOptions());
        byte[] tooSmall = Enumerable.Repeat((byte)0xA5, allocated.BytesWritten - 1).ToArray();
        byte[] exact = new byte[allocated.BytesWritten];
        byte[] larger = Enumerable.Repeat((byte)0x5A, allocated.BytesWritten + 4).ToArray();

        SdpBufferWriteResult insufficient = writer.WriteUtf8Buffer(description, tooSmall, new SdpWriteOptions());
        SdpBufferWriteResult exactResult = writer.WriteUtf8Buffer(description, exact, new SdpWriteOptions());
        SdpBufferWriteResult largerResult = writer.WriteUtf8Buffer(description, larger, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(insufficient.Success, Is.False);
            Assert.That(insufficient.BufferTooSmall, Is.True);
            Assert.That(insufficient.RequiredBytes, Is.EqualTo(allocated.BytesWritten));
            Assert.That(insufficient.BytesWritten, Is.Zero);
            Assert.That(insufficient.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.WriteBufferTooSmall));
            Assert.That(tooSmall, Has.All.EqualTo((byte)0xA5));
            Assert.That(exactResult.Success, Is.True);
            Assert.That(exactResult.BufferTooSmall, Is.False);
            Assert.That(exactResult.RequiredBytes, Is.EqualTo(allocated.BytesWritten));
            Assert.That(exactResult.BytesWritten, Is.EqualTo(allocated.BytesWritten));
            Assert.That(exact, Is.EqualTo(allocated.Utf8Bytes));
            Assert.That(largerResult.Success, Is.True);
            Assert.That(largerResult.BytesWritten, Is.EqualTo(allocated.BytesWritten));
            Assert.That(larger.Take(allocated.BytesWritten), Is.EqualTo(allocated.Utf8Bytes));
            Assert.That(larger.Skip(allocated.BytesWritten), Has.All.EqualTo((byte)0x5A));
        });
    }

    [Test]
    public void Writer_AppliesByteLimitBeforeCallerBufferCapacity()
    {
        SdpSessionDescription description = MinimalDescription("é");
        ISdpWriter writer = new SdpWriter();
        SdpWriteResult complete = writer.WriteUtf8Bytes(description, new SdpWriteOptions());
        byte[] destination = Enumerable.Repeat((byte)0x7C, complete.BytesWritten).ToArray();

        SdpBufferWriteResult result = writer.WriteUtf8Buffer(description, destination, new SdpWriteOptions
        {
            MaximumOutputBytes = complete.BytesWritten - 1
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.BufferTooSmall, Is.False);
            Assert.That(result.RequiredBytes, Is.EqualTo(complete.BytesWritten));
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.WriteOutputByteLimitExceeded));
            Assert.That(destination, Has.All.EqualTo((byte)0x7C));
        });
    }

    [Test]
    public void Writer_PropagatesGenerationFailuresWithoutTouchingCallerBuffer()
    {
        ISdpWriter writer = new SdpWriter();
        byte[] destination = Enumerable.Repeat((byte)0x33, 64).ToArray();

        SdpBufferWriteResult result = writer.WriteUtf8Buffer(new SdpSessionDescription(), destination, new SdpWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.BufferTooSmall, Is.False);
            Assert.That(result.RequiredBytes, Is.Zero);
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(result.Diagnostics, Is.Not.Empty);
            Assert.That(destination, Has.All.EqualTo((byte)0x33));
        });
    }

    [Test]
    public void Writer_Utf8EntryPointsRejectNullArgumentsAndInvalidOptions()
    {
        ISdpWriter writer = new SdpWriter();
        SdpSessionDescription description = MinimalDescription("Session");
        byte[] destination = new byte[256];

        Assert.Multiple(() =>
        {
            Assert.That(() => writer.WriteUtf8Bytes(null!, new SdpWriteOptions()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => writer.WriteUtf8Bytes(description, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
            Assert.That(() => writer.WriteUtf8Buffer(null!, destination, new SdpWriteOptions()), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("description"));
            Assert.That(() => writer.WriteUtf8Buffer(description, destination, null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("options"));
            Assert.That(() => writer.WriteUtf8Bytes(description, new SdpWriteOptions { MaximumOutputBytes = -1 }), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("options"));
            Assert.That(() => writer.WriteUtf8Buffer(description, destination, new SdpWriteOptions { MaximumOutputBytes = -1 }), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("options"));
        });
    }

    static SdpSessionDescription MinimalDescription(string sessionName)
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
            sessionName,
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
