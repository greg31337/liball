namespace sdp.Tests.Unit.Attributes;

public class SdpFormatParametersCodecTests
{
    [TestCase("0 mode=baseline", 0)]
    [TestCase("127 mode=extension", 127)]
    public void PayloadBounds_DecodeAndEncodeSuccessfully(string value, int payloadType)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "fmtp", Value = value };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.FormatParametersAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.FormatParametersAttribute?.PayloadType, Is.EqualTo(payloadType));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.Value, Is.EqualTo(value));
        });
    }

    [Test]
    public void StructuredParameters_PreserveOrderFlagsDuplicatesQuotesAndRawText()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        string parameterText = "minptime = 10 ; profile=first;stereo;profile=second;mode=\"semi;colon\";empty=";
        SdpAttribute source = new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = "FMTP",
            Value = " 111   " + parameterText,
            RawText = "FMTP: 111   " + parameterText
        };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpFormatParametersAttribute projection = decoded.FormatParametersAttribute ?? new SdpFormatParametersAttribute();
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.PayloadType, Is.EqualTo(111));
            Assert.That(projection.RawParameters, Is.EqualTo(parameterText));
            Assert.That(projection.Parameters, Has.Count.EqualTo(6));
            Assert.That(projection.Parameters.Select(x => x.Name), Is.EqualTo(new[] { "minptime", "profile", "stereo", "profile", "mode", "empty" }));
            Assert.That(projection.Parameters[0].Value, Is.EqualTo("10"));
            Assert.That(projection.Parameters[0].RawText, Is.EqualTo("minptime = 10"));
            Assert.That(projection.Parameters[2].HasValue, Is.False);
            Assert.That(projection.Parameters[2].Value, Is.Empty);
            Assert.That(projection.Parameters[3].Value, Is.EqualTo("second"));
            Assert.That(projection.Parameters[4].Value, Is.EqualTo("\"semi;colon\""));
            Assert.That(projection.Parameters[5].HasValue, Is.True);
            Assert.That(projection.Parameters[5].Value, Is.Empty);
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.FormatParametersAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.Name, Is.EqualTo("fmtp"));
            Assert.That(encoded.Attribute?.Value, Is.EqualTo("111 minptime=10;profile=first;stereo;profile=second;mode=\"semi;colon\";empty="));
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("fmtp:111 minptime=10;profile=first;stereo;profile=second;mode=\"semi;colon\";empty="));
        });
    }

    [Test]
    public void StructuredParameters_HandleTabsEscapedQuotesEmbeddedEqualsAndEmptyValues()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = "fmtp",
            Value = "\t96\t mode = \"semi;quoted\\\"value\" ; equals = a=b ; empty= ; flag\t"
        };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpFormatParametersAttribute projection = decoded.FormatParametersAttribute ?? new SdpFormatParametersAttribute();
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(projection.Parameters.Select(x => x.Name), Is.EqualTo(new[] { "mode", "equals", "empty", "flag" }));
            Assert.That(projection.Parameters.Select(x => x.HasValue), Is.EqualTo(new[] { true, true, true, false }));
            Assert.That(projection.Parameters.Select(x => x.Value), Is.EqualTo(new[] { "\"semi;quoted\\\"value\"", "a=b", string.Empty, string.Empty }));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.Value, Is.EqualTo("96 mode=\"semi;quoted\\\"value\";equals=a=b;empty=;flag"));
        });
    }

    [TestCase("config=\"unterminated;mode=1")]
    [TestCase("one=1;;two=2")]
    [TestCase("profile(data);mode=1")]
    [TestCase(";leading=separator")]
    [TestCase("trailing=separator;")]
    public void OpaqueParameterSyntax_UsesRawFallbackWithoutDataLoss(string rawParameters)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = "fmtp",
            Value = "96 " + rawParameters,
            RawText = "fmtp:96 " + rawParameters
        };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpFormatParametersAttribute projection = decoded.FormatParametersAttribute ?? new SdpFormatParametersAttribute();
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Diagnostics, Is.Empty);
            Assert.That(projection.RawParameters, Is.EqualTo(rawParameters));
            Assert.That(projection.Parameters, Is.Empty);
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("fmtp:96 " + rawParameters));
        });
    }

    [TestCase(SdpAttributeKind.Property, "")]
    [TestCase(SdpAttributeKind.Value, "")]
    [TestCase(SdpAttributeKind.Value, "96")]
    [TestCase(SdpAttributeKind.Value, "96   ")]
    [TestCase(SdpAttributeKind.Value, "01 mode=1")]
    [TestCase(SdpAttributeKind.Value, "128 mode=1")]
    [TestCase(SdpAttributeKind.Value, "-1 mode=1")]
    [TestCase(SdpAttributeKind.Value, "96 mode=one\ntwo")]
    public void InvalidOuterFmtpSyntax_FailsAndRetainsTheSource(SdpAttributeKind kind, string value)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = kind, Name = "fmtp", Value = value, RawText = "fmtp:" + value };
        source.Location.LineNumber = 12;

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.FormatParametersAttribute, Is.Null);
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Severity, Is.EqualTo(SdpValidationSeverity.Error));
            Assert.That(result.Diagnostics[0].AttributeName, Is.EqualTo("fmtp"));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(12));
        });
    }

    [TestCase("01 mode=1")]
    [TestCase("128 mode=1")]
    [TestCase("-1 mode=1")]
    public void InvalidFmtpPayload_UsesPayloadDiagnostic(string value)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "fmtp", Value = value };

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationPayloadType));
    }

    [Test]
    public void CallerConstructedStructuredParameters_EncodeCanonically()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpFormatParametersAttribute projection = new SdpFormatParametersAttribute { PayloadType = 111 };
        projection.Parameters.Add(new SdpFormatParameter { Name = "minptime", HasValue = true, Value = "10" });
        projection.Parameters.Add(new SdpFormatParameter { Name = "stereo", HasValue = false });
        projection.Parameters.Add(new SdpFormatParameter { Name = "profile", HasValue = true, Value = "first" });
        projection.Parameters.Add(new SdpFormatParameter { Name = "profile", HasValue = true, Value = "\"second;alternate\"" });

        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(projection);
        SdpAttributeDecodeResult decodedAgain = codec.DecodeAttribute(encoded.Attribute!);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("fmtp:111 minptime=10;stereo;profile=first;profile=\"second;alternate\""));
            Assert.That(decodedAgain.Success, Is.True);
            Assert.That(decodedAgain.FormatParametersAttribute?.Parameters, Has.Count.EqualTo(4));
            Assert.That(decodedAgain.FormatParametersAttribute?.Parameters[1].HasValue, Is.False);
            Assert.That(decodedAgain.FormatParametersAttribute?.Parameters[3].Value, Is.EqualTo("\"second;alternate\""));
        });
    }

    [Test]
    public void CallerConstructedOpaqueParameters_AreEmittedWithoutInterpretation()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpFormatParametersAttribute projection = new SdpFormatParametersAttribute
        {
            PayloadType = 96,
            RawParameters = "codec-specific(opaque);unbalanced=\"value"
        };

        SdpAttributeEncodeResult result = codec.EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Attribute?.Value, Is.EqualTo("96 codec-specific(opaque);unbalanced=\"value"));
            Assert.That(result.Attribute?.RawText, Is.EqualTo("fmtp:96 codec-specific(opaque);unbalanced=\"value"));
        });
    }

    [Test]
    public void InvalidCallerConstructedParameters_ReturnStructuredFailures()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpFormatParametersAttribute badPayload = new SdpFormatParametersAttribute { PayloadType = 128, RawParameters = "mode=1" };
        SdpFormatParametersAttribute empty = new SdpFormatParametersAttribute { PayloadType = 96 };
        SdpFormatParametersAttribute badName = new SdpFormatParametersAttribute { PayloadType = 96 };
        badName.Parameters.Add(new SdpFormatParameter { Name = "bad name" });
        SdpFormatParametersAttribute unsafeDelimiter = new SdpFormatParametersAttribute { PayloadType = 96 };
        unsafeDelimiter.Parameters.Add(new SdpFormatParameter { Name = "mode", HasValue = true, Value = "one;two" });
        SdpFormatParametersAttribute unbalancedQuote = new SdpFormatParametersAttribute { PayloadType = 96 };
        unbalancedQuote.Parameters.Add(new SdpFormatParameter { Name = "mode", HasValue = true, Value = "\"unterminated" });
        SdpFormatParametersAttribute invalidOpaque = new SdpFormatParametersAttribute { PayloadType = 96, RawParameters = "one\ntwo" };

        SdpAttributeEncodeResult payloadResult = codec.EncodeAttribute(badPayload);
        SdpAttributeEncodeResult emptyResult = codec.EncodeAttribute(empty);
        SdpAttributeEncodeResult nameResult = codec.EncodeAttribute(badName);
        SdpAttributeEncodeResult delimiterResult = codec.EncodeAttribute(unsafeDelimiter);
        SdpAttributeEncodeResult quoteResult = codec.EncodeAttribute(unbalancedQuote);
        SdpAttributeEncodeResult opaqueResult = codec.EncodeAttribute(invalidOpaque);

        Assert.Multiple(() =>
        {
            Assert.That(payloadResult.Success, Is.False);
            Assert.That(payloadResult.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationPayloadType));
            Assert.That(emptyResult.Success, Is.False);
            Assert.That(nameResult.Success, Is.False);
            Assert.That(delimiterResult.Success, Is.False);
            Assert.That(quoteResult.Success, Is.False);
            Assert.That(quoteResult.Attribute, Is.Null);
            Assert.That(opaqueResult.Success, Is.False);
        });
    }
}
