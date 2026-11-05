namespace sdp.Tests.Unit.Attributes;

public class SdpImageAttributeCodecTests
{
    [TestCase("97 send [x=720,y=576,sar=1.1]", "97", true, "[x=720,y=576,sar=1.1]", false, "", "imageattr:97 send [x=720,y=576,sar=1.1]")]
    [TestCase("* send * recv *", "*", true, "*", true, "*", "imageattr:* send * recv *")]
    [TestCase(" 99 recv [x=330,y=250]   send [x=800,y=640,sar=[0.91,1.0,1.09]] [x=480,y=320] ", "99", true, "[x=800,y=640,sar=[0.91,1.0,1.09]] [x=480,y=320]", true, "[x=330,y=250]", "imageattr:99 send [x=800,y=640,sar=[0.91,1.0,1.09]] [x=480,y=320] recv [x=330,y=250]")]
    [TestCase("100 send [x=640,y=480,x-vendor=opaque]", "100", true, "[x=640,y=480,x-vendor=opaque]", false, "", "imageattr:100 send [x=640,y=480,x-vendor=opaque]")]
    [TestCase("127 send [x=[320:16:640],y=[240,480],sar=[0.9-1.1],par=[1.2-1.3],q=1.00,x-vendor=opaque]", "127", true, "[x=[320:16:640],y=[240,480],sar=[0.9-1.1],par=[1.2-1.3],q=1.00,x-vendor=opaque]", false, "", "imageattr:127 send [x=[320:16:640],y=[240,480],sar=[0.9-1.1],par=[1.2-1.3],q=1.00,x-vendor=opaque]")]
    public void ImageAttribute_DecodesDirectionsWildcardsAlternativesAndExtensions(string value, string payload, bool hasSend, string send, bool hasReceive, string receive, string rawText)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "IMAGEATTR", Value = value, RawText = "IMAGEATTR:" + value };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpImageAttribute projection = decoded.ImageAttribute ?? new SdpImageAttribute();
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(projection.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.PayloadType, Is.EqualTo(payload));
            Assert.That(projection.HasSendConstraints, Is.EqualTo(hasSend));
            Assert.That(projection.SendConstraints, Is.EqualTo(send));
            Assert.That(projection.HasReceiveConstraints, Is.EqualTo(hasReceive));
            Assert.That(projection.ReceiveConstraints, Is.EqualTo(receive));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.ImageAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo(rawText));
        });
    }

    [TestCase(SdpAttributeKind.Property, "97 send *", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "01 send *", "SDP_VALIDATION_PAYLOAD_TYPE")]
    [TestCase(SdpAttributeKind.Value, "128 send *", "SDP_VALIDATION_PAYLOAD_TYPE")]
    [TestCase(SdpAttributeKind.Value, "97", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "97 transmit *", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "97 send", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "97 send * send *", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "97 send [x=640,y=480", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "97 send * [x=640,y=480]", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "97 send [x=0,y=480]", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "97 send [x=1000000,y=480]", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "97 send [x=[640:320],y=480]", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "97 send [x=640]", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "97 send [x=640,y=480,sar=[1.1-0.9]]", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "97 send [x=640,y=480,par=1.2]", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "97 send [x=640,y=480,q=1.01]", "SDP_VALIDATION_ATTRIBUTE")]
    [TestCase(SdpAttributeKind.Value, "97 send [x=640,y=480,q=0.5,q=0.6]", "SDP_VALIDATION_ATTRIBUTE")]
    public void InvalidImageAttribute_FailsAndRetainsSource(SdpAttributeKind kind, string value, string code)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = kind, Name = "imageattr", Value = value, RawText = "imageattr:" + value };

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.ImageAttribute, Is.Null);
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(code));
        });
    }

    [Test]
    public void CallerConstructedImageAttribute_RejectsInconsistentAndUnsafeConstraints()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpImageAttribute hidden = new SdpImageAttribute { PayloadType = "96", SendConstraints = "*" };
        SdpImageAttribute none = new SdpImageAttribute { PayloadType = "96" };
        SdpImageAttribute unbalanced = new SdpImageAttribute { PayloadType = "96", HasSendConstraints = true, SendConstraints = "[x=640,y=480" };

        SdpAttributeEncodeResult hiddenResult = codec.EncodeAttribute(hidden);
        SdpAttributeEncodeResult noneResult = codec.EncodeAttribute(none);
        SdpAttributeEncodeResult unbalancedResult = codec.EncodeAttribute(unbalanced);

        Assert.Multiple(() =>
        {
            Assert.That(hiddenResult.Success, Is.False);
            Assert.That(noneResult.Success, Is.False);
            Assert.That(unbalancedResult.Success, Is.False);
            Assert.That(unbalancedResult.Attribute, Is.Null);
        });
    }
}
