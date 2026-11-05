namespace sdp.Tests.Unit.Attributes;

public class SdpDataChannelAttributeCodecTests
{
    [Test]
    public void MapWithoutParameters_RoundTripsItsChannelIdentifier()
    {
        SdpAttribute source = ValueAttribute("DCMAP", "0");
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.DataChannelAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.DataChannelAttribute!.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.DataChannelAttribute.ChannelId, Is.Zero);
            Assert.That(decoded.DataChannelAttribute.Parameters, Is.Empty);
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.DataChannelAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(encoded.Attribute.Name, Is.EqualTo(SdpAttributeNames.DataChannelMap));
            Assert.That(encoded.Attribute.RawText, Is.EqualTo("dcmap:0"));
        });
    }

    [Test]
    public void MapParameters_PreserveOrderDuplicatesUnknownsAndQuotedSemicolons()
    {
        string value = "7 subprotocol=\"msrp\";ordered=false;label=\"Label 1\";max-time=60000;priority=512;X-Future=\"alpha;beta\";flag;X-Future=again";
        SdpAttribute source = ValueAttribute("dcmap", value);
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.DataChannelAttribute!);
        SdpDataChannelAttribute channel = decoded.DataChannelAttribute!;

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(channel.ChannelId, Is.EqualTo(7));
            Assert.That(channel.Parameters.Select(parameter => parameter.Name), Is.EqualTo(new[]
            {
                "subprotocol", "ordered", "label", "max-time", "priority", "X-Future", "flag", "X-Future"
            }));
            Assert.That(channel.Parameters.Select(parameter => parameter.HasValue), Is.EqualTo(new[]
            {
                true, true, true, true, true, true, false, true
            }));
            Assert.That(channel.Parameters.Select(parameter => parameter.Value), Is.EqualTo(new[]
            {
                "msrp", "false", "Label 1", "60000", "512", "\"alpha;beta\"", string.Empty, "again"
            }));
            Assert.That(channel.Parameters[2].RawText, Is.EqualTo("label=\"Label 1\""));
            Assert.That(channel.Parameters[5].RawText, Is.EqualTo("X-Future=\"alpha;beta\""));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.Value, Is.EqualTo(value));
        });
    }

    [Test]
    public void MapQuotedValues_PreservePercentEscapesAndCanonicalizeKnownNames()
    {
        SdpAttribute source = ValueAttribute("dcmap", "00004 LABEL=\"foo%09bar\";SUBPROTOCOL=\"\"");
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.DataChannelAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.DataChannelAttribute!.ChannelId, Is.EqualTo(4));
            Assert.That(decoded.DataChannelAttribute.Parameters[0].Name, Is.EqualTo("LABEL"));
            Assert.That(decoded.DataChannelAttribute.Parameters[0].Value, Is.EqualTo("foo%09bar"));
            Assert.That(decoded.DataChannelAttribute.Parameters[1].Value, Is.Empty);
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.RawText, Is.EqualTo("dcmap:4 label=\"foo%09bar\";subprotocol=\"\""));
        });
    }

    [TestCase(SdpAttributeKind.Property, "0")]
    [TestCase(SdpAttributeKind.Value, "")]
    [TestCase(SdpAttributeKind.Value, "100000")]
    [TestCase(SdpAttributeKind.Value, "x")]
    [TestCase(SdpAttributeKind.Value, "1 ")]
    [TestCase(SdpAttributeKind.Value, "1\tordered=true")]
    [TestCase(SdpAttributeKind.Value, "1 ;ordered=true")]
    [TestCase(SdpAttributeKind.Value, "1 ordered")]
    [TestCase(SdpAttributeKind.Value, "1 label=plain")]
    [TestCase(SdpAttributeKind.Value, "1 label=\"bad%XX\"")]
    [TestCase(SdpAttributeKind.Value, "1 label=\"unterminated")]
    [TestCase(SdpAttributeKind.Value, "1 ordered=true;")]
    [TestCase(SdpAttributeKind.Value, "1 ordered=true;;priority=1")]
    [TestCase(SdpAttributeKind.Value, "1 bad name=value")]
    [TestCase(SdpAttributeKind.Value, "1 x=\"bad%XX\"")]
    public void InvalidMapSyntax_FailsWithoutAProjection(SdpAttributeKind kind, string value)
    {
        AssertInvalid(kind, SdpAttributeNames.DataChannelMap, value);
    }

    [Test]
    public void InvalidCallerConstructedMaps_FailEncoding()
    {
        SdpDataChannelAttribute negative = new SdpDataChannelAttribute { ChannelId = -1 };
        SdpDataChannelAttribute high = new SdpDataChannelAttribute { ChannelId = 100000 };
        SdpDataChannelAttribute badName = MapWithParameter(new SdpDataChannelParameter { Name = "bad name" });
        SdpDataChannelAttribute missingKnownValue = MapWithParameter(new SdpDataChannelParameter { Name = SdpDataChannelTokens.Ordered });
        SdpDataChannelAttribute invalidQuotedContent = MapWithParameter(new SdpDataChannelParameter
        {
            Name = SdpDataChannelTokens.Label,
            HasValue = true,
            Value = "bad%escape"
        });
        SdpDataChannelAttribute invalidUnknownValue = MapWithParameter(new SdpDataChannelParameter
        {
            Name = "future",
            HasValue = true,
            Value = "two words"
        });
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeEncodeResult[] results =
        {
            codec.EncodeAttribute(negative),
            codec.EncodeAttribute(high),
            codec.EncodeAttribute(badName),
            codec.EncodeAttribute(missingKnownValue),
            codec.EncodeAttribute(invalidQuotedContent),
            codec.EncodeAttribute(invalidUnknownValue)
        };

        Assert.That(results, Has.All.Matches<SdpAttributeEncodeResult>(result =>
            !result.Success && result.Attribute == null && result.Diagnostics.Count == 1));
    }

    [TestCase("2 accept-types:text/plain", 2, SdpAttributeKind.Value, "accept-types", "text/plain")]
    [TestCase("4 inactive", 4, SdpAttributeKind.Property, "inactive", "")]
    [TestCase("7 x-empty:", 7, SdpAttributeKind.Value, "x-empty", "")]
    [TestCase("00003 path:msrp://example.test/id;dc", 3, SdpAttributeKind.Value, "path", "msrp://example.test/id;dc")]
    public void SubprotocolAttribute_PreservesEmbeddedPropertyOrValue(string value, int channelId, SdpAttributeKind kind, string name, string embeddedValue)
    {
        SdpAttribute source = ValueAttribute("DCSA", value);
        source.Location.CharacterOffset = 100;
        source.Location.ByteOffset = 200;
        source.Location.LineNumber = 41;
        source.Location.ColumnNumber = 3;
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.DataChannelSubprotocolAttribute!);
        SdpDataChannelSubprotocolAttribute projection = decoded.DataChannelSubprotocolAttribute!;

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(projection.SourceAttribute, Is.SameAs(source));
            Assert.That(projection.ChannelId, Is.EqualTo(channelId));
            Assert.That(projection.EmbeddedAttribute.Kind, Is.EqualTo(kind));
            Assert.That(projection.EmbeddedAttribute.Name, Is.EqualTo(name));
            Assert.That(projection.EmbeddedAttribute.Value, Is.EqualTo(embeddedValue));
            Assert.That(projection.EmbeddedAttribute.RawText, Is.EqualTo(value.Substring(value.IndexOf(' ') + 1)));
            Assert.That(projection.EmbeddedAttribute.Location.CharacterOffset, Is.EqualTo(108 + value.IndexOf(' ')));
            Assert.That(projection.EmbeddedAttribute.Location.ByteOffset, Is.EqualTo(208 + value.IndexOf(' ')));
            Assert.That(projection.EmbeddedAttribute.Location.LineNumber, Is.EqualTo(41));
            Assert.That(projection.EmbeddedAttribute.Location.ColumnNumber, Is.EqualTo(11 + value.IndexOf(' ')));
            Assert.That(decoded.TypedAttribute, Is.SameAs(projection));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.RawText, Is.EqualTo("dcsa:" + channelId + " " + projection.EmbeddedAttribute.RawText));
        });
    }

    [Test]
    public void SubprotocolAttribute_PreservesTheCompleteEmbeddedAttributeValue()
    {
        string value = "2 accept-types:message/cpim text/plain";
        SdpAttributeDecodeResult decoded = new SdpAttributeCodec().DecodeAttribute(ValueAttribute("dcsa", value));

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.DataChannelSubprotocolAttribute!.EmbeddedAttribute.Value, Is.EqualTo("message/cpim text/plain"));
            Assert.That(decoded.DataChannelSubprotocolAttribute.EmbeddedAttribute.RawText, Is.EqualTo("accept-types:message/cpim text/plain"));
        });
    }

    [TestCase(SdpAttributeKind.Property, "2 accept-types:text/plain")]
    [TestCase(SdpAttributeKind.Value, "")]
    [TestCase(SdpAttributeKind.Value, "1")]
    [TestCase(SdpAttributeKind.Value, "1 ")]
    [TestCase(SdpAttributeKind.Value, "100000 x:value")]
    [TestCase(SdpAttributeKind.Value, "x x:value")]
    [TestCase(SdpAttributeKind.Value, "1\tx:value")]
    [TestCase(SdpAttributeKind.Value, "1 bad name:value")]
    [TestCase(SdpAttributeKind.Value, "1 good:bad\nvalue")]
    public void InvalidSubprotocolAttributeSyntax_FailsWithoutAProjection(SdpAttributeKind kind, string value)
    {
        AssertInvalid(kind, SdpAttributeNames.DataChannelSubprotocol, value);
    }

    [Test]
    public void InvalidCallerConstructedSubprotocolAttributes_FailEncoding()
    {
        SdpDataChannelSubprotocolAttribute negative = SubprotocolAttribute(-1, ValueAttribute("x", "value"));
        SdpDataChannelSubprotocolAttribute high = SubprotocolAttribute(100000, ValueAttribute("x", "value"));
        SdpDataChannelSubprotocolAttribute missingName = SubprotocolAttribute(1, new SdpAttribute { Kind = SdpAttributeKind.Property });
        SdpDataChannelSubprotocolAttribute unknownKind = SubprotocolAttribute(1, new SdpAttribute { Kind = SdpAttributeKind.Unknown, Name = "inactive" });
        SdpDataChannelSubprotocolAttribute invalidValue = SubprotocolAttribute(1, ValueAttribute("x", "bad\nvalue"));
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeEncodeResult[] results =
        {
            codec.EncodeAttribute(negative),
            codec.EncodeAttribute(high),
            codec.EncodeAttribute(missingName),
            codec.EncodeAttribute(unknownKind),
            codec.EncodeAttribute(invalidValue)
        };

        Assert.That(results, Has.All.Matches<SdpAttributeEncodeResult>(result =>
            !result.Success && result.Attribute == null && result.Diagnostics.Count == 1));
    }

    static void AssertInvalid(SdpAttributeKind kind, string name, string value)
    {
        SdpAttribute source = new SdpAttribute
        {
            Kind = kind,
            Name = name,
            Value = value,
            RawText = "opaque-source"
        };
        source.Location.LineNumber = 29;

        SdpAttributeDecodeResult result = new SdpAttributeCodec().DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.SourceAttribute!.RawText, Is.EqualTo("opaque-source"));
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.DataChannelAttribute, Is.Null);
            Assert.That(result.DataChannelSubprotocolAttribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].AttributeName, Is.EqualTo(name));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(29));
        });
    }

    static SdpDataChannelAttribute MapWithParameter(SdpDataChannelParameter parameter)
    {
        SdpDataChannelAttribute result = new SdpDataChannelAttribute { ChannelId = 1 };
        result.Parameters.Add(parameter);
        return result;
    }

    static SdpDataChannelSubprotocolAttribute SubprotocolAttribute(int channelId, SdpAttribute embedded)
    {
        return new SdpDataChannelSubprotocolAttribute { ChannelId = channelId, EmbeddedAttribute = embedded };
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }
}
