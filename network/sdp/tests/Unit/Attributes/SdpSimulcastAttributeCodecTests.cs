namespace sdp.Tests.Unit.Attributes;

public class SdpSimulcastAttributeCodecTests
{
    [Test]
    public void SendFirstSimulcast_DecodesLayersAlternativesAndPauseStateInOrder()
    {
        string value = "send high;mid,~mid-vp9;~low recv remote-a,remote-b;~remote-low";
        SdpAttribute source = ValueAttribute("SIMULCAST", value);
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.SimulcastAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.SimulcastAttribute!.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.SimulcastAttribute.SendFirst, Is.True);
            Assert.That(decoded.SimulcastAttribute.LegacySyntax, Is.False);
            Assert.That(decoded.SimulcastAttribute.RawValue, Is.EqualTo(value));
            Assert.That(decoded.SimulcastAttribute.SendStreams, Has.Count.EqualTo(3));
            Assert.That(decoded.SimulcastAttribute.ReceiveStreams, Has.Count.EqualTo(2));
            Assert.That(decoded.SimulcastAttribute.SendStreams[0].Alternatives.Select(item => item.RidId), Is.EqualTo(new[] { "high" }));
            Assert.That(decoded.SimulcastAttribute.SendStreams[1].Alternatives.Select(item => item.RidId), Is.EqualTo(new[] { "mid", "mid-vp9" }));
            Assert.That(decoded.SimulcastAttribute.SendStreams[1].Alternatives.Select(item => item.Paused), Is.EqualTo(new[] { false, true }));
            Assert.That(decoded.SimulcastAttribute.SendStreams[2].Alternatives[0].Paused, Is.True);
            Assert.That(decoded.SimulcastAttribute.ReceiveStreams[0].Alternatives.Select(item => item.RidId), Is.EqualTo(new[] { "remote-a", "remote-b" }));
            Assert.That(decoded.SimulcastAttribute.ReceiveStreams[1].Alternatives[0].Paused, Is.True);
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.SimulcastAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.TypedAttribute, Is.SameAs(decoded.SimulcastAttribute));
            Assert.That(encoded.Attribute!.Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(encoded.Attribute.Name, Is.EqualTo(SdpAttributeNames.Simulcast));
            Assert.That(encoded.Attribute.Value, Is.EqualTo(value));
            Assert.That(encoded.Attribute.RawText, Is.EqualTo("simulcast:" + value));
        });
    }

    [Test]
    public void ReceiveFirstSimulcast_PreservesDirectionOrdering()
    {
        string value = "recv r1;r2,r3 send s1;~s2";
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(ValueAttribute("simulcast", value));
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.SimulcastAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.SimulcastAttribute!.SendFirst, Is.False);
            Assert.That(decoded.SimulcastAttribute.ReceiveStreams.SelectMany(stream => stream.Alternatives).Select(item => item.RidId), Is.EqualTo(new[] { "r1", "r2", "r3" }));
            Assert.That(decoded.SimulcastAttribute.SendStreams.SelectMany(stream => stream.Alternatives).Select(item => item.RidId), Is.EqualTo(new[] { "s1", "s2" }));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.Value, Is.EqualTo(value));
        });
    }

    [TestCase("send one;two,~three", true)]
    [TestCase("recv one,~two;three", false)]
    public void SingleDirectionSimulcast_DecodesAndEncodes(string value, bool send)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(ValueAttribute("simulcast", value));
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.SimulcastAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.SimulcastAttribute!.SendStreams.Count > 0, Is.EqualTo(send));
            Assert.That(decoded.SimulcastAttribute.ReceiveStreams.Count > 0, Is.EqualTo(!send));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.Value, Is.EqualTo(value));
        });
    }

    [TestCase(" recv pt=97;98 send pt=97")]
    [TestCase(" send rid=5;6;7 paused=6,7")]
    [TestCase("\trecv\trid=high,medium;low\tpaused=low")]
    [TestCase(" SEND RID=high;low PAUSED=low")]
    public void LegacyDraftSyntax_UsesRawFallbackWithoutDataLoss(string value)
    {
        SdpAttribute source = ValueAttribute("simulcast", value);
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.SimulcastAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Diagnostics, Is.Empty);
            Assert.That(decoded.SimulcastAttribute!.LegacySyntax, Is.True);
            Assert.That(decoded.SimulcastAttribute.RawValue, Is.EqualTo(value));
            Assert.That(decoded.SimulcastAttribute.SendStreams, Is.Empty);
            Assert.That(decoded.SimulcastAttribute.ReceiveStreams, Is.Empty);
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.Value, Is.EqualTo(value));
            Assert.That(encoded.Attribute.RawText, Is.EqualTo("simulcast:" + value));
        });
    }

    [TestCase(SdpAttributeKind.Property, "send one")]
    [TestCase(SdpAttributeKind.Value, "")]
    [TestCase(SdpAttributeKind.Value, "SEND one")]
    [TestCase(SdpAttributeKind.Value, "send")]
    [TestCase(SdpAttributeKind.Value, "send ")]
    [TestCase(SdpAttributeKind.Value, " send one")]
    [TestCase(SdpAttributeKind.Value, "send  one")]
    [TestCase(SdpAttributeKind.Value, "send one recv")]
    [TestCase(SdpAttributeKind.Value, "send one send two")]
    [TestCase(SdpAttributeKind.Value, "send one recv two extra")]
    [TestCase(SdpAttributeKind.Value, "send one;")]
    [TestCase(SdpAttributeKind.Value, "send ;one")]
    [TestCase(SdpAttributeKind.Value, "send one,,two")]
    [TestCase(SdpAttributeKind.Value, "send ~")]
    [TestCase(SdpAttributeKind.Value, "send bad.id")]
    [TestCase(SdpAttributeKind.Value, " send rid=")]
    [TestCase(SdpAttributeKind.Value, " send rid=one;")]
    [TestCase(SdpAttributeKind.Value, " send rid=one paused=")]
    [TestCase(SdpAttributeKind.Value, " send rid=one paused=one;two")]
    [TestCase(SdpAttributeKind.Value, " send rid=one garbage")]
    [TestCase(SdpAttributeKind.Value, "send one\trecv two")]
    [TestCase(SdpAttributeKind.Value, "send one\nrecv two")]
    public void InvalidSimulcastSyntax_FailsAndRetainsOpaqueSource(SdpAttributeKind kind, string value)
    {
        SdpAttribute source = new SdpAttribute
        {
            Kind = kind,
            Name = "simulcast",
            Value = value,
            RawText = "opaque-simulcast"
        };
        source.Location.LineNumber = 63;

        SdpAttributeDecodeResult result = new SdpAttributeCodec().DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.SourceAttribute!.RawText, Is.EqualTo("opaque-simulcast"));
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.SimulcastAttribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].AttributeName, Is.EqualTo("simulcast"));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(63));
        });
    }

    [Test]
    public void CallerConstructedSimulcast_EncodesRequestedDirectionAndAlternativeOrder()
    {
        SdpSimulcastAttribute simulcast = new SdpSimulcastAttribute { SendFirst = false };
        simulcast.ReceiveStreams.Add(Stream(Alternative("receive-high", false), Alternative("receive-low", true)));
        simulcast.SendStreams.Add(Stream(Alternative("send-high", true)));
        simulcast.SendStreams.Add(Stream(Alternative("send-low", false), Alternative("send-backup", false)));

        SdpAttributeEncodeResult result = new SdpAttributeCodec().EncodeAttribute(simulcast);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.TypedAttribute, Is.SameAs(simulcast));
            Assert.That(result.Attribute!.RawText, Is.EqualTo("simulcast:recv receive-high,~receive-low send ~send-high;send-low,send-backup"));
        });
    }

    [Test]
    public void InvalidCallerData_ReturnsOneDiagnosticWithoutAnAttribute()
    {
        SdpSimulcastAttribute empty = new SdpSimulcastAttribute();
        SdpSimulcastAttribute emptyStream = new SdpSimulcastAttribute();
        emptyStream.SendStreams.Add(new SdpSimulcastStream());
        SdpSimulcastAttribute invalidIdentifier = new SdpSimulcastAttribute();
        invalidIdentifier.SendStreams.Add(Stream(Alternative("bad.id", false)));
        SdpSimulcastAttribute mixedLegacy = new SdpSimulcastAttribute { LegacySyntax = true, RawValue = " send rid=one" };
        mixedLegacy.SendStreams.Add(Stream(Alternative("one", false)));
        SdpSimulcastAttribute invalidLegacy = new SdpSimulcastAttribute { LegacySyntax = true, RawValue = "legacy text" };
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeEncodeResult[] results =
        {
            codec.EncodeAttribute(empty),
            codec.EncodeAttribute(emptyStream),
            codec.EncodeAttribute(invalidIdentifier),
            codec.EncodeAttribute(mixedLegacy),
            codec.EncodeAttribute(invalidLegacy)
        };

        Assert.That(results, Has.All.Matches<SdpAttributeEncodeResult>(result =>
            !result.Success && result.Attribute == null && result.Diagnostics.Count == 1));
    }

    static SdpSimulcastAlternative Alternative(string identifier, bool paused)
    {
        return new SdpSimulcastAlternative { RidId = identifier, Paused = paused };
    }

    static SdpSimulcastStream Stream(params SdpSimulcastAlternative[] alternatives)
    {
        SdpSimulcastStream stream = new SdpSimulcastStream();
        stream.Alternatives.AddRange(alternatives);
        return stream;
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }
}
