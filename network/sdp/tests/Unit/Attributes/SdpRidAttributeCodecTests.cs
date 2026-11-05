namespace sdp.Tests.Unit.Attributes;

public class SdpRidAttributeCodecTests
{
    [Test]
    public void RidWithPayloadsAndKnownAndUnknownRestrictions_RoundTripsInOrder()
    {
        string value = "f-1_send send pt=96,0,127;max-width=1280;vendor-mode=alpha beta=gamma;max-width;empty=";
        SdpAttribute source = ValueAttribute("RID", value);
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(source);
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.RidAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Recognized, Is.True);
            Assert.That(decoded.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.RidAttribute!.SourceAttribute, Is.SameAs(source));
            Assert.That(decoded.RidAttribute.Id, Is.EqualTo("f-1_send"));
            Assert.That(decoded.RidAttribute.Direction, Is.EqualTo(SdpDirection.SendOnly));
            Assert.That(decoded.RidAttribute.PayloadTypes, Is.EqualTo(new[] { 96, 0, 127 }));
            Assert.That(decoded.RidAttribute.Restrictions, Has.Count.EqualTo(4));
            Assert.That(decoded.RidAttribute.Restrictions.Select(item => item.Name), Is.EqualTo(new[] { "max-width", "vendor-mode", "max-width", "empty" }));
            Assert.That(decoded.RidAttribute.Restrictions.Select(item => item.HasValue), Is.EqualTo(new[] { true, true, false, true }));
            Assert.That(decoded.RidAttribute.Restrictions.Select(item => item.Value), Is.EqualTo(new[] { "1280", "alpha beta=gamma", "", "" }));
            Assert.That(decoded.RidAttribute.Restrictions.Select(item => item.RawText), Is.EqualTo(new[] { "max-width=1280", "vendor-mode=alpha beta=gamma", "max-width", "empty=" }));
            Assert.That(decoded.TypedAttribute, Is.SameAs(decoded.RidAttribute));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.TypedAttribute, Is.SameAs(decoded.RidAttribute));
            Assert.That(encoded.Attribute!.Kind, Is.EqualTo(SdpAttributeKind.Value));
            Assert.That(encoded.Attribute.Name, Is.EqualTo(SdpAttributeNames.Rid));
            Assert.That(encoded.Attribute.Value, Is.EqualTo(value));
            Assert.That(encoded.Attribute.RawText, Is.EqualTo("rid:" + value));
        });
    }

    [TestCase("plain send", SdpDirection.SendOnly)]
    [TestCase("plain recv", SdpDirection.ReceiveOnly)]
    public void RidWithoutParameters_PreservesDirectionAndEmptyLists(string value, SdpDirection direction)
    {
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(ValueAttribute("rid", value));
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.RidAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.RidAttribute!.Direction, Is.EqualTo(direction));
            Assert.That(decoded.RidAttribute.PayloadTypes, Is.Empty);
            Assert.That(decoded.RidAttribute.Restrictions, Is.Empty);
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.Value, Is.EqualTo(value));
        });
    }

    [Test]
    public void RestrictionOnlyRid_SeparatesFirstRestrictionWithSpace()
    {
        string value = "low recv max-height=720;x-flag";
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeDecodeResult decoded = codec.DecodeAttribute(ValueAttribute("rid", value));
        SdpAttributeEncodeResult encoded = codec.EncodeAttribute(decoded.RidAttribute!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.RidAttribute!.PayloadTypes, Is.Empty);
            Assert.That(decoded.RidAttribute.Restrictions.Select(item => item.Name), Is.EqualTo(new[] { "max-height", "x-flag" }));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.Value, Is.EqualTo(value));
        });
    }

    [TestCase(SdpAttributeKind.Property, "f send")]
    [TestCase(SdpAttributeKind.Value, "")]
    [TestCase(SdpAttributeKind.Value, "f")]
    [TestCase(SdpAttributeKind.Value, "bad.id send")]
    [TestCase(SdpAttributeKind.Value, "f SEND")]
    [TestCase(SdpAttributeKind.Value, "f  send")]
    [TestCase(SdpAttributeKind.Value, "f send ")]
    [TestCase(SdpAttributeKind.Value, "f send pt=")]
    [TestCase(SdpAttributeKind.Value, "f send pt=96,")]
    [TestCase(SdpAttributeKind.Value, "f send pt=096")]
    [TestCase(SdpAttributeKind.Value, "f send pt=128")]
    [TestCase(SdpAttributeKind.Value, "f send pt=96;")]
    [TestCase(SdpAttributeKind.Value, "f send pt=96;pt=97")]
    [TestCase(SdpAttributeKind.Value, "f send max_width=1")]
    [TestCase(SdpAttributeKind.Value, "f send x=one;;y=two")]
    [TestCase(SdpAttributeKind.Value, "f\tsend")]
    [TestCase(SdpAttributeKind.Value, "f send x=bad\tvalue")]
    [TestCase(SdpAttributeKind.Value, "f send x=bad\nvalue")]
    public void InvalidRidSyntax_FailsAndRetainsOpaqueSource(SdpAttributeKind kind, string value)
    {
        SdpAttribute source = new SdpAttribute
        {
            Kind = kind,
            Name = "rid",
            Value = value,
            RawText = "opaque-rid"
        };
        source.Location.LineNumber = 52;

        SdpAttributeDecodeResult result = new SdpAttributeCodec().DecodeAttribute(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Recognized, Is.True);
            Assert.That(result.SourceAttribute, Is.SameAs(source));
            Assert.That(result.SourceAttribute!.RawText, Is.EqualTo("opaque-rid"));
            Assert.That(result.TypedAttribute, Is.Null);
            Assert.That(result.RidAttribute, Is.Null);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].AttributeName, Is.EqualTo("rid"));
            Assert.That(result.Diagnostics[0].Location.LineNumber, Is.EqualTo(52));
        });
    }

    [Test]
    public void CallerConstructedRid_EncodesPayloadAndRestrictionFields()
    {
        SdpRidAttribute rid = new SdpRidAttribute { Id = "camera_2", Direction = SdpDirection.ReceiveOnly };
        rid.PayloadTypes.Add(98);
        rid.PayloadTypes.Add(99);
        rid.Restrictions.Add(new SdpRidRestriction { Name = "depend", HasValue = true, Value = "base,mid", RawText = "stale" });
        rid.Restrictions.Add(new SdpRidRestriction { Name = "future-option", HasValue = false, RawText = "ignored=value" });

        SdpAttributeEncodeResult result = new SdpAttributeCodec().EncodeAttribute(rid);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.TypedAttribute, Is.SameAs(rid));
            Assert.That(result.Attribute!.RawText, Is.EqualTo("rid:camera_2 recv pt=98,99;depend=base,mid;future-option"));
            Assert.That(rid.Restrictions[0].RawText, Is.EqualTo("stale"));
            Assert.That(rid.Restrictions[1].RawText, Is.EqualTo("ignored=value"));
        });
    }

    [Test]
    public void InvalidCallerData_ReturnsOneDiagnosticWithoutAnAttribute()
    {
        SdpRidAttribute invalidId = Rid("bad.id", SdpDirection.SendOnly);
        SdpRidAttribute invalidDirection = Rid("good", SdpDirection.SendReceive);
        SdpRidAttribute invalidPayload = Rid("good", SdpDirection.SendOnly);
        invalidPayload.PayloadTypes.Add(128);
        SdpRidAttribute invalidName = Rid("good", SdpDirection.SendOnly);
        invalidName.Restrictions.Add(new SdpRidRestriction { Name = "bad_name" });
        SdpRidAttribute reservedName = Rid("good", SdpDirection.SendOnly);
        reservedName.Restrictions.Add(new SdpRidRestriction { Name = "pt", HasValue = true, Value = "96" });
        SdpRidAttribute hiddenValue = Rid("good", SdpDirection.SendOnly);
        hiddenValue.Restrictions.Add(new SdpRidRestriction { Name = "flag", Value = "hidden" });
        SdpRidAttribute unsafeValue = Rid("good", SdpDirection.SendOnly);
        unsafeValue.Restrictions.Add(new SdpRidRestriction { Name = "extension", HasValue = true, Value = "one;two" });
        ISdpAttributeCodec codec = new SdpAttributeCodec();

        SdpAttributeEncodeResult[] results =
        {
            codec.EncodeAttribute(invalidId),
            codec.EncodeAttribute(invalidDirection),
            codec.EncodeAttribute(invalidPayload),
            codec.EncodeAttribute(invalidName),
            codec.EncodeAttribute(reservedName),
            codec.EncodeAttribute(hiddenValue),
            codec.EncodeAttribute(unsafeValue)
        };

        Assert.That(results, Has.All.Matches<SdpAttributeEncodeResult>(result =>
            !result.Success && result.Attribute == null && result.Diagnostics.Count == 1));
    }

    static SdpRidAttribute Rid(string identifier, SdpDirection direction)
    {
        return new SdpRidAttribute { Id = identifier, Direction = direction };
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }
}
