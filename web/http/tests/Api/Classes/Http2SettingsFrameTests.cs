namespace http.Tests;

public class Http2SettingsFrameTests
{
    [Test]
    public void ParsingAppliesCoreSettingsAndValidatesNamedExtensions()
    {
        byte[] payload = SettingsPayload(
            Setting(Http2SettingIdentifiers.HeaderTableSize, 0xffffffffu),
            Setting(Http2SettingIdentifiers.EnablePush, 0u),
            Setting(Http2SettingIdentifiers.MaxConcurrentStreams, 0xfffffffeu),
            Setting(Http2SettingIdentifiers.InitialWindowSize, 0x7fffffffu),
            Setting(Http2SettingIdentifiers.MaxFrameSize, 0x00ffffffu),
            Setting(Http2SettingIdentifiers.MaxHeaderListSize, 0xffffffffu),
            Setting(Http2SettingIdentifiers.EnableConnectProtocol, 1u),
            Setting(Http2SettingIdentifiers.NoRfc7540Priorities, 1u),
            Setting(Http2SettingIdentifiers.TlsRenegPermitted, 2u),
            Setting(Http2SettingIdentifiers.EnableMetadata, 1u));

        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildSettingsWire(0x80, 0, payload));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Frame, Is.Not.Null);
            Assert.That(result.Frame!.Acknowledgement, Is.False);
            Assert.That(result.Frame.Settings, Is.Not.Null);
            Assert.That(result.Frame.Settings!.Values, Has.Count.EqualTo(10));
            Assert.That(result.Frame.Settings.Values.All(item => item.IsKnown),
                Is.True);
            Assert.That(result.Frame.Settings.Values.All(item => item.IsValid),
                Is.True);
            Assert.That(result.Frame.Settings.HasHeaderTableSize, Is.True);
            Assert.That(result.Frame.Settings.HeaderTableSize,
                Is.EqualTo(4294967295L));
            Assert.That(result.Frame.Settings.EnablePush, Is.False);
            Assert.That(result.Frame.Settings.MaxConcurrentStreams,
                Is.EqualTo(4294967294L));
            Assert.That(result.Frame.Settings.InitialWindowSize,
                Is.EqualTo(0x7fffffff));
            Assert.That(result.Frame.Settings.MaxFrameSize,
                Is.EqualTo(0x00ffffff));
            Assert.That(result.Frame.Settings.MaxHeaderListSize,
                Is.EqualTo(4294967295L));
            Assert.That(result.Frame.Settings.EnableConnectProtocol, Is.True);
            Assert.That(result.Frame.Settings.NoRfc7540Priorities, Is.True);
            Assert.That(result.Frame.Flags, Is.EqualTo(0x80));
            Assert.That(result.Frame.Payload, Is.EqualTo(payload));
        });
    }

    [Test]
    public void DuplicateAndUnknownSettingsRemainOrderedWithLastValueEffective()
    {
        byte[] payload = SettingsPayload(
            Setting(Http2SettingIdentifiers.HeaderTableSize, 100u),
            Setting(0x07, 0xf1234567u),
            Setting(Http2SettingIdentifiers.HeaderTableSize, 200u));

        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildSettingsWire(0, 0, payload));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Frame!.Settings!.Values.Select(item => item.Identifier),
                Is.EqualTo(new[]
                {
                    Http2SettingIdentifiers.HeaderTableSize,
                    0x07,
                    Http2SettingIdentifiers.HeaderTableSize
                }));
            Assert.That(result.Frame.Settings.Values[1].Value,
                Is.EqualTo(0xf1234567u));
            Assert.That(result.Frame.Settings.Values[1].IsKnown, Is.False);
            Assert.That(result.Frame.Settings.Values[1].IsValid, Is.False);
            Assert.That(result.Frame.Settings.HeaderTableSize, Is.EqualTo(200));
        });
    }

    [Test]
    public void AcknowledgementRequiresAndSerializesAnEmptyPayload()
    {
        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult parsed = codec.ParseHttp2Frame(
            BuildSettingsWire(0xa1, 0, Array.Empty<byte>()));
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
        frame.Flags = 0xa0;
        frame.Acknowledgement = true;
        Http2FrameResult serialized = codec.SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Frame!.Acknowledgement, Is.True);
            Assert.That(parsed.Frame.Settings, Is.Not.Null);
            Assert.That(parsed.Frame.Settings!.Values, Is.Empty);
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(serialized.Bytes, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x00, 0x04, 0xa1,
                0x00, 0x00, 0x00, 0x00
            }));
            Assert.That(serialized.Frame!.Acknowledgement, Is.True);
            Assert.That(serialized.Frame.Settings!.Values, Is.Empty);
        });
    }

    [Test]
    public void AcknowledgementWithParametersIsRejectedBothWays()
    {
        byte[] payload = SettingsPayload(
            Setting(Http2SettingIdentifiers.EnablePush, 0u));
        Http2FrameResult parsed = new Http2FrameCodec().ParseHttp2Frame(
            BuildSettingsWire(0x01, 0, payload));
        Http2Frame frame = SettingsFrame(
            Setting(Http2SettingIdentifiers.EnablePush, 0u));
        frame.Acknowledgement = true;
        Http2FrameResult serialized =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(parsed.ErrorCode,
                Is.EqualTo("http2-settings-ack-payload"));
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(serialized.ErrorCode,
                Is.EqualTo("http2-settings-ack-payload"));
        });
    }

    [TestCase(1)]
    [TestCase(5)]
    [TestCase(7)]
    public void ParsingRejectsPayloadLengthsNotDivisibleBySix(int payloadLength)
    {
        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildSettingsWire(0, 0, new byte[payloadLength]));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http2-settings-frame-size"));
            Assert.That(result.ErrorPosition, Is.Zero);
            Assert.That(result.Consumed,
                Is.EqualTo(Http2Constants.FrameHeaderSize + payloadLength));
        });
    }

    [Test]
    public void ParsingAndSerializationRequireStreamZero()
    {
        Http2FrameResult parsed = new Http2FrameCodec().ParseHttp2Frame(
            BuildSettingsWire(0, 1, Array.Empty<byte>()));
        Http2Frame frame = SettingsFrame();
        frame.StreamId = 1;
        Http2FrameResult serialized =
            new Http2FrameCodec().SerializeHttp2Frame(frame);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(parsed.ErrorCode,
                Is.EqualTo("http2-settings-stream-nonzero"));
            Assert.That(parsed.ErrorPosition, Is.EqualTo(5));
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(serialized.ErrorCode,
                Is.EqualTo("http2-settings-stream-nonzero"));
        });
    }

    [TestCase(Http2SettingIdentifiers.EnablePush, 2u)]
    [TestCase(Http2SettingIdentifiers.InitialWindowSize, 0x80000000u)]
    [TestCase(Http2SettingIdentifiers.MaxFrameSize, 16383u)]
    [TestCase(Http2SettingIdentifiers.MaxFrameSize, 0x01000000u)]
    [TestCase(Http2SettingIdentifiers.EnableConnectProtocol, 2u)]
    [TestCase(Http2SettingIdentifiers.NoRfc7540Priorities, 2u)]
    [TestCase(Http2SettingIdentifiers.TlsRenegPermitted, 4u)]
    [TestCase(Http2SettingIdentifiers.EnableMetadata, 2u)]
    public void InvalidRegisteredValuesAreRejectedAndIdentified(
        int identifier,
        uint value)
    {
        Http2FrameResult result = new Http2FrameCodec().ParseHttp2Frame(
            BuildSettingsWire(0, 0, SettingsPayload(Setting(identifier, value))));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.ErrorCode,
                Is.EqualTo("http2-settings-value-invalid"));
            Assert.That(result.ErrorPosition,
                Is.EqualTo(Http2Constants.FrameHeaderSize + 2));
            Assert.That(result.Frame!.Settings!.Values, Has.Count.EqualTo(1));
            Assert.That(result.Frame.Settings.Values[0].IsKnown, Is.True);
            Assert.That(result.Frame.Settings.Values[0].IsValid, Is.False);
        });
    }

    [Test]
    public void SerializationPreservesDuplicateAndUnknownEntriesExactly()
    {
        Http2Frame frame = SettingsFrame(
            Setting(Http2SettingIdentifiers.HeaderTableSize, 100u),
            Setting(0xf0f0, 0xf1234567u),
            Setting(Http2SettingIdentifiers.HeaderTableSize, 200u));
        frame.Flags = 0xa1;

        Http2FrameCodec codec = new Http2FrameCodec();
        Http2FrameResult serialized = codec.SerializeHttp2Frame(frame);
        Http2FrameResult parsed = codec.ParseHttp2Frame(serialized.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(serialized.Status,
                Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(serialized.Bytes, Is.EqualTo(new byte[]
            {
                0x00, 0x00, 0x12, 0x04, 0xa0,
                0x00, 0x00, 0x00, 0x00,
                0x00, 0x01, 0x00, 0x00, 0x00, 0x64,
                0xf0, 0xf0, 0xf1, 0x23, 0x45, 0x67,
                0x00, 0x01, 0x00, 0x00, 0x00, 0xc8
            }));
            Assert.That(serialized.RequiredSize, Is.EqualTo(27));
            Assert.That(serialized.Frame!.Settings!.Values[1].IsKnown,
                Is.False);
            Assert.That(parsed.Frame!.Settings!.Values.Select(item => item.Value),
                Is.EqualTo(new uint[] { 100u, 0xf1234567u, 200u }));
            Assert.That(parsed.Frame.Settings.HeaderTableSize, Is.EqualTo(200));
        });
    }

    [Test]
    public void SerializationRejectsMissingSettingsInvalidIdsAndValues()
    {
        Http2Frame missing = new Http2Frame();
        missing.FrameType = Http2FrameType.Settings;
        Http2Frame negative = SettingsFrame(Setting(-1, 0u));
        Http2Frame excessive = SettingsFrame(Setting(0x10000, 0u));
        Http2Frame invalid = SettingsFrame(
            Setting(Http2SettingIdentifiers.EnablePush, 2u));
        Http2FrameCodec codec = new Http2FrameCodec();

        Http2FrameResult missingResult = codec.SerializeHttp2Frame(missing);
        Http2FrameResult negativeResult = codec.SerializeHttp2Frame(negative);
        Http2FrameResult excessiveResult = codec.SerializeHttp2Frame(excessive);
        Http2FrameResult invalidResult = codec.SerializeHttp2Frame(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.ErrorCode,
                Is.EqualTo("invalid-http2-settings-frame"));
            Assert.That(negativeResult.ErrorCode,
                Is.EqualTo("invalid-http2-setting-identifier"));
            Assert.That(excessiveResult.ErrorCode,
                Is.EqualTo("invalid-http2-setting-identifier"));
            Assert.That(invalidResult.ErrorCode,
                Is.EqualTo("http2-settings-value-invalid"));
            Assert.That(invalidResult.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
        });
    }

    [Test]
    public void SerializationHonorsFrameLimitAndBoundedOutput()
    {
        Http2Frame frame = SettingsFrame(
            Setting(Http2SettingIdentifiers.EnablePush, 0u),
            Setting(Http2SettingIdentifiers.MaxConcurrentStreams, 100u));
        Http2FrameCodec limited = new Http2FrameCodec();
        limited.configuredMaximumFrameSize = 11;
        limited.peerMaximumFrameSize = 11;
        Http2FrameCodec codec = new Http2FrameCodec();
        byte[] exact = new byte[21];
        byte[] shortOutput = Enumerable.Repeat((byte)0xcc, 20).ToArray();

        Http2FrameResult rejected = limited.SerializeHttp2Frame(frame);
        Http2FrameResult written = codec.WriteHttp2Frame(frame, exact);
        Http2FrameResult needOutput =
            codec.WriteHttp2Frame(frame, shortOutput);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Status,
                Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(rejected.ErrorCode,
                Is.EqualTo("http2-frame-size-limit-exceeded"));
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(written.Written, Is.EqualTo(21));
            Assert.That(written.Bytes, Is.Empty);
            Assert.That(needOutput.Status,
                Is.EqualTo(HttpOperationStatus.NeedOutput));
            Assert.That(needOutput.Written, Is.Zero);
            Assert.That(needOutput.RequiredSize, Is.EqualTo(21));
            Assert.That(shortOutput,
                Is.EqualTo(Enumerable.Repeat((byte)0xcc, 20).ToArray()));
        });
    }

    static Http2Frame SettingsFrame(params Http2Setting[] settings)
    {
        Http2Frame frame = new Http2Frame();
        frame.FrameType = Http2FrameType.Settings;
        frame.Settings = new Http2Settings();
        frame.Settings.Values.AddRange(settings);
        return frame;
    }

    static Http2Setting Setting(int identifier, uint value)
    {
        Http2Setting setting = new Http2Setting();
        setting.Identifier = identifier;
        setting.Value = value;
        return setting;
    }

    static byte[] SettingsPayload(params Http2Setting[] settings)
    {
        byte[] payload = new byte[settings.Length * Http2Constants.SettingSize];
        int index = 0;
        while (index < settings.Length)
        {
            Http2Setting setting = settings[index];
            int offset = index * Http2Constants.SettingSize;
            payload[offset] = (byte)(setting.Identifier >> 8);
            payload[offset + 1] = (byte)setting.Identifier;
            payload[offset + 2] = (byte)(setting.Value >> 24);
            payload[offset + 3] = (byte)(setting.Value >> 16);
            payload[offset + 4] = (byte)(setting.Value >> 8);
            payload[offset + 5] = (byte)setting.Value;
            index = index + 1;
        }

        return payload;
    }

    static byte[] BuildSettingsWire(byte flags, int streamId, byte[] payload)
    {
        byte[] result = new byte[Http2Constants.FrameHeaderSize + payload.Length];
        result[0] = (byte)(payload.Length >> 16);
        result[1] = (byte)(payload.Length >> 8);
        result[2] = (byte)payload.Length;
        result[3] = 0x04;
        result[4] = flags;
        result[5] = (byte)(streamId >> 24);
        result[6] = (byte)(streamId >> 16);
        result[7] = (byte)(streamId >> 8);
        result[8] = (byte)streamId;
        payload.CopyTo(result, Http2Constants.FrameHeaderSize);
        return result;
    }
}
