namespace sdp.Tests.Unit.Attributes;

public class SdpGroupAttributeCodecTests
{
    [TestCase("LS", SdpGroupSemantic.LipSynchronization)]
    [TestCase("FID", SdpGroupSemantic.FlowIdentification)]
    [TestCase("SRF", SdpGroupSemantic.SingleReservationFlow)]
    [TestCase("ANAT", SdpGroupSemantic.AlternativeNetworkAddressTypes)]
    [TestCase("FEC", SdpGroupSemantic.ForwardErrorCorrection)]
    [TestCase("FEC-FR", SdpGroupSemantic.ForwardErrorCorrectionFramework)]
    [TestCase("CS", SdpGroupSemantic.CompositeSession)]
    [TestCase("DDP", SdpGroupSemantic.DecodingDependency)]
    [TestCase("DUP", SdpGroupSemantic.Duplication)]
    [TestCase("SPLICE", SdpGroupSemantic.Splice)]
    [TestCase("BUNDLE", SdpGroupSemantic.Bundle)]
    [TestCase("CLUE", SdpGroupSemantic.Clue)]
    [TestCase("V3C", SdpGroupSemantic.VolumetricVideo)]
    public void RegisteredGroupSemantic_DecodesAndEncodesTypedKind(string semanticToken, SdpGroupSemantic semantic)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = "GROUP",
            Value = " " + semanticToken + "   audio video audio ",
            RawText = "GROUP: " + semanticToken + "   audio video audio "
        };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpGroupAttribute projection = decoded.GroupAttribute ?? new SdpGroupAttribute();
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.Semantic, Is.EqualTo(semantic));
            Assert.That(projection.SemanticToken, Is.EqualTo(semanticToken));
            Assert.That(projection.IdentificationTags, Is.EqualTo(new[] { "audio", "video", "audio" }));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.GroupAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("group:" + semanticToken + " audio video audio"));
        });
    }

    [Test]
    public void ExtensionAndCapabilityGroups_RetainUnknownSemanticAndEmptyTagList()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute extensionSource = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "group", Value = "X-REL one two" };
        SdpAttribute capabilitySource = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "group", Value = "FID" };

        SdpAttributeDecodeResult extension = codec.DecodeAttribute(extensionSource);
        SdpAttributeDecodeResult capability = codec.DecodeAttribute(capabilitySource);
        SdpAttributeEncodeResult extensionEncoded = codec.EncodeAttribute(extension.GroupAttribute!);
        SdpAttributeEncodeResult capabilityEncoded = codec.EncodeAttribute(capability.GroupAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(extension.Success, Is.True);
            Assert.That(extension.GroupAttribute?.Semantic, Is.EqualTo(SdpGroupSemantic.Unknown));
            Assert.That(extension.GroupAttribute?.SemanticToken, Is.EqualTo("X-REL"));
            Assert.That(extension.GroupAttribute?.IdentificationTags, Is.EqualTo(new[] { "one", "two" }));
            Assert.That(extensionEncoded.Attribute?.RawText, Is.EqualTo("group:X-REL one two"));
            Assert.That(capability.Success, Is.True);
            Assert.That(capability.GroupAttribute?.Semantic, Is.EqualTo(SdpGroupSemantic.FlowIdentification));
            Assert.That(capability.GroupAttribute?.IdentificationTags, Is.Empty);
            Assert.That(capabilityEncoded.Attribute?.RawText, Is.EqualTo("group:FID"));
        });
    }

    [TestCase(SdpAttributeKind.Property, "BUNDLE audio")]
    [TestCase(SdpAttributeKind.Value, "")]
    [TestCase(SdpAttributeKind.Value, "bad/semantic audio")]
    [TestCase(SdpAttributeKind.Value, "BUNDLE bad/tag")]
    [TestCase(SdpAttributeKind.Value, "BUNDLE audio\nvideo")]
    public void InvalidGroup_FailsAndRetainsOpaqueSource(SdpAttributeKind kind, string value)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = kind, Name = "group", Value = value, RawText = "group:" + value };
        source.Location.LineNumber = 31;

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.GroupAttribute, Is.Null);
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(31));
        });
    }

    [TestCase("0")]
    [TestCase("audio")]
    [TestCase("audio-1")]
    [TestCase("!extension")]
    public void MediaId_DecodesAndEncodesTokenIdentificationTags(string mediaId)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "MID", Value = " " + mediaId + " ", RawText = "MID: " + mediaId + " " };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpMediaIdAttribute projection = decoded.MediaIdAttribute ?? new SdpMediaIdAttribute();
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(projection);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(projection.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.Id, Is.EqualTo(mediaId));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.MediaIdAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("mid:" + mediaId));
        });
    }

    [TestCase(SdpAttributeKind.Property, "audio")]
    [TestCase(SdpAttributeKind.Value, "")]
    [TestCase(SdpAttributeKind.Value, "audio video")]
    [TestCase(SdpAttributeKind.Value, "bad/tag")]
    [TestCase(SdpAttributeKind.Value, "audio\nvideo")]
    public void InvalidMediaId_ReturnsStructuredFailure(SdpAttributeKind kind, string value)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = kind, Name = "mid", Value = value };

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.MediaIdAttribute, Is.Null);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttribute));
        });
    }

    [Test]
    public void BundleOnly_DecodesAndEncodesAsCanonicalProperty()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Property, Name = "BUNDLE-ONLY", RawText = "BUNDLE-ONLY" };

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.CommonAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.CommonAttribute?.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.CommonAttribute?.Name, Is.EqualTo("bundle-only"));
            Assert.That(decoded.CommonAttribute?.Direction, Is.EqualTo(SdpDirection.Unknown));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute?.Kind, Is.EqualTo(SdpAttributeKind.Property));
            Assert.That(encoded.Attribute?.RawText, Is.EqualTo("bundle-only"));
        });
    }

    [Test]
    public void BundleOnly_WithValueSyntaxFails()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpAttribute source = new SdpAttribute { Kind = SdpAttributeKind.Value, Name = "bundle-only", Value = string.Empty, RawText = "bundle-only:" };

        SdpAttributeDecodeResult result = codec.DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.CommonAttribute, Is.Null);
        });
    }

    [Test]
    public void CallerConstructedGroupData_RejectsMismatchesAndInvalidTags()
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();
        SdpGroupAttribute mismatch = new SdpGroupAttribute { Semantic = SdpGroupSemantic.Bundle, SemanticToken = "FID" };
        SdpGroupAttribute badTag = new SdpGroupAttribute { Semantic = SdpGroupSemantic.Bundle };
        badTag.IdentificationTags.Add("bad/tag");
        SdpGroupAttribute missingExtension = new SdpGroupAttribute();
        SdpMediaIdAttribute badMediaId = new SdpMediaIdAttribute { Id = "two ids" };

        SdpAttributeEncodeResult mismatchResult = codec.EncodeAttribute(mismatch);
        SdpAttributeEncodeResult tagResult = codec.EncodeAttribute(badTag);
        SdpAttributeEncodeResult extensionResult = codec.EncodeAttribute(missingExtension);
        SdpAttributeEncodeResult mediaIdResult = codec.EncodeAttribute(badMediaId);

        Assert.Multiple(() =>
        {
            Assert.That(mismatchResult.Success, Is.False);
            Assert.That(tagResult.Success, Is.False);
            Assert.That(extensionResult.Success, Is.False);
            Assert.That(mediaIdResult.Success, Is.False);
            Assert.That(mediaIdResult.Attribute, Is.Null);
        });
    }
}
