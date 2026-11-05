using asn1;

namespace asn1.Tests;

public class BerUsefulTypeCodecTests
{
    [Test]
    public void TimeAndUsefulTimeTypesUseTheirRequiredWireText()
    {
        AsnTypeKind[] kinds =
        [
            AsnTypeKind.UtcTime,
            AsnTypeKind.GeneralizedTime,
            AsnTypeKind.Time,
            AsnTypeKind.Date,
            AsnTypeKind.TimeOfDay,
            AsnTypeKind.DateTime,
            AsnTypeKind.Duration
        ];
        AsnTimeKind[] timeKinds =
        [
            AsnTimeKind.UtcTime,
            AsnTimeKind.GeneralizedTime,
            AsnTimeKind.Time,
            AsnTimeKind.Date,
            AsnTimeKind.TimeOfDay,
            AsnTimeKind.DateTime,
            AsnTimeKind.Duration
        ];
        string[] values =
        [
            "260828153045Z",
            "20260828153045.123-0700",
            "15:30:45Z",
            "2026-08-28",
            "15:30:45.25+07:00",
            "2026-08-28T15:30:45Z",
            "P2DT3H"
        ];
        string[] wireValues =
        [
            "260828153045Z",
            "20260828153045.123-0700",
            "15:30:45Z",
            "20260828",
            "153045.25+0700",
            "20260828153045Z",
            "2DT3H"
        ];
        string[] decodedValues =
        [
            "260828153045Z",
            "20260828153045.123-0700",
            "15:30:45Z",
            "2026-08-28",
            "15:30:45.25+0700",
            "2026-08-28T15:30:45Z",
            "P2DT3H"
        ];
        int[] tags = [23, 24, 14, 31, 32, 33, 34];
        BerUsefulTypeCodec codec = CreateCodec();

        int index = 0;
        while (index < kinds.Length)
        {
            BerPrimitiveEncodeResult encoded = codec.EncodeUsefulType(
                kinds[index],
                new AsnRuntimeValue
                {
                    Kind = AsnRuntimeValueKind.Time,
                    TimeValue = new AsnTimeValue
                    {
                        Kind = timeKinds[index],
                        Text = values[index]
                    }
                },
                AsnEncodingRules.Ber);
            BerPrimitiveDecodeResult decoded = codec.DecodeUsefulType(
                kinds[index],
                encoded.Tlv ?? throw new InvalidOperationException(),
                AsnEncodingRules.Ber);

            Assert.That(encoded.Success, Is.True, Format(encoded.Diagnostics));
            Assert.That(encoded.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(tags[index])));
            Assert.That(encoded.Tlv?.Content,
                Is.EqualTo(System.Text.Encoding.UTF8.GetBytes(wireValues[index])));
            Assert.That(decoded.Success, Is.True, Format(decoded.Diagnostics));
            Assert.That(decoded.Value?.TimeValue?.Text,
                Is.EqualTo(decodedValues[index]));
            index++;
        }
    }

    [Test]
    public void DecodedTimeRestoresOffsetAndFractionMetadata()
    {
        BerPrimitiveDecodeResult decoded = CreateCodec().DecodeUsefulType(
            AsnTypeKind.TimeOfDay,
            UniversalPrimitive(
                AsnUniversalTags.TimeOfDay,
                System.Text.Encoding.ASCII.GetBytes("153045.125-0730")),
            AsnEncodingRules.Ber);
        AsnTimeValue value = decoded.Value?.TimeValue
            ?? throw new InvalidOperationException();

        Assert.Multiple(() =>
        {
            Assert.That(value.Text, Is.EqualTo("15:30:45.125-0730"));
            Assert.That(value.HasFractionalDigits, Is.True);
            Assert.That(value.FractionalDigits, Is.EqualTo(3));
            Assert.That(value.HasUtcOffsetMinutes, Is.True);
            Assert.That(value.UtcOffsetMinutes, Is.EqualTo(-450));
        });
    }

    [Test]
    public void DerNormalizesUtcAndGeneralizedTime()
    {
        BerUsefulTypeCodec codec = CreateCodec();
        BerPrimitiveEncodeResult utc = codec.EncodeUsefulType(
            AsnTypeKind.UtcTime,
            TimeValue(AsnTimeKind.UtcTime, "9912311900-0500"),
            AsnEncodingRules.Der);
        BerPrimitiveEncodeResult generalized = codec.EncodeUsefulType(
            AsnTypeKind.GeneralizedTime,
            TimeValue(
                AsnTimeKind.GeneralizedTime,
                "20260828153045.1200-0700"),
            AsnEncodingRules.Der);
        BerPrimitiveEncodeResult generalizedCer = codec.EncodeUsefulType(
            AsnTypeKind.GeneralizedTime,
            TimeValue(
                AsnTimeKind.GeneralizedTime,
                "20260828153045.1200-0700"),
            AsnEncodingRules.Cer);
        BerPrimitiveEncodeResult fractionalHour = codec.EncodeUsefulType(
            AsnTypeKind.GeneralizedTime,
            TimeValue(AsnTimeKind.GeneralizedTime, "2026082815.5+0100"),
            AsnEncodingRules.Der);
        BerPrimitiveEncodeResult local = codec.EncodeUsefulType(
            AsnTypeKind.GeneralizedTime,
            TimeValue(AsnTimeKind.GeneralizedTime, "20260828153045"),
            AsnEncodingRules.Der);

        Assert.Multiple(() =>
        {
            Assert.That(utc.Tlv?.Content,
                Is.EqualTo(System.Text.Encoding.ASCII.GetBytes(
                    "000101000000Z")));
            Assert.That(generalized.Tlv?.Content,
                Is.EqualTo(System.Text.Encoding.ASCII.GetBytes(
                    "20260828223045.12Z")));
            Assert.That(generalizedCer.Tlv?.Content,
                Is.EqualTo(System.Text.Encoding.ASCII.GetBytes(
                    "20260828223045.12Z")));
            Assert.That(fractionalHour.Tlv?.Content,
                Is.EqualTo(System.Text.Encoding.ASCII.GetBytes(
                    "20260828143000Z")));
            Assert.That(local.Success, Is.False);
            Assert.That(local.Diagnostics.Single().Code, Is.EqualTo("ASNE0037"));
        });
    }

    [Test]
    public void DerCanonicalizesNewTimeTypeLexicalForms()
    {
        BerUsefulTypeCodec codec = CreateCodec();
        BerPrimitiveEncodeResult timeOfDay = codec.EncodeUsefulType(
            AsnTypeKind.TimeOfDay,
            TimeValue(AsnTimeKind.TimeOfDay, "15:30:45,250+07:00"),
            AsnEncodingRules.Der);
        BerPrimitiveDecodeResult timeOfDayDecoded = codec.DecodeUsefulType(
            AsnTypeKind.TimeOfDay,
            timeOfDay.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Der);
        BerPrimitiveEncodeResult dateTime = codec.EncodeUsefulType(
            AsnTypeKind.DateTime,
            TimeValue(AsnTimeKind.DateTime, "2026-08-28T15:30:45-07:30"),
            AsnEncodingRules.Der);
        BerPrimitiveEncodeResult duration = codec.EncodeUsefulType(
            AsnTypeKind.Duration,
            TimeValue(AsnTimeKind.Duration, "P0Y2M0DT0H3M0S"),
            AsnEncodingRules.Der);

        Assert.Multiple(() =>
        {
            Assert.That(timeOfDay.Tlv?.Content,
                Is.EqualTo(System.Text.Encoding.ASCII.GetBytes(
                    "153045.250+07")));
            Assert.That(timeOfDayDecoded.Value?.TimeValue?.Text,
                Is.EqualTo("15:30:45.250+0700"));
            Assert.That(dateTime.Tlv?.Content,
                Is.EqualTo(System.Text.Encoding.ASCII.GetBytes(
                    "20260828153045-0730")));
            Assert.That(duration.Tlv?.Content,
                Is.EqualTo(System.Text.Encoding.ASCII.GetBytes(
                    "2MT3M0S")));
        });
    }

    [Test]
    public void ObjectDescriptorUsesPrimitiveUniversalTagSeven()
    {
        BerUsefulTypeCodec codec = CreateCodec();
        BerPrimitiveEncodeResult encoded = codec.EncodeUsefulType(
            AsnTypeKind.ObjectDescriptor,
            TextValue("Human readable"),
            AsnEncodingRules.Der);
        BerPrimitiveDecodeResult decoded = codec.DecodeUsefulType(
            AsnTypeKind.ObjectDescriptor,
            encoded.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Der);
        BerPrimitiveEncodeResult invalid = codec.EncodeUsefulType(
            AsnTypeKind.ObjectDescriptor,
            TextValue("line\nfeed"),
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(7)));
            Assert.That(encoded.Tlv?.Constructed, Is.False);
            Assert.That(encoded.Tlv?.Content,
                Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("Human readable")));
            Assert.That(decoded.Value?.TextValue, Is.EqualTo("Human readable"));
            Assert.That(invalid.Success, Is.False);
        });
    }

    [Test]
    public void CerObjectDescriptorUsesCanonicalConstructedFragments()
    {
        string text = new string('A', 1001);
        BerUsefulTypeCodec codec = CreateCodec();
        BerPrimitiveEncodeResult encoded = codec.EncodeUsefulType(
            AsnTypeKind.ObjectDescriptor,
            TextValue(text),
            AsnEncodingRules.Cer);
        BerPrimitiveDecodeResult decoded = codec.DecodeUsefulType(
            AsnTypeKind.ObjectDescriptor,
            encoded.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Cer);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Tlv?.Constructed, Is.True);
            Assert.That(encoded.Tlv?.IndefiniteLength, Is.True);
            Assert.That(encoded.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(7)));
            Assert.That(encoded.Tlv?.Children.Select(child => child.Tag.Number),
                Is.EqualTo(new System.Numerics.BigInteger[] { 7, 7 }));
            Assert.That(encoded.Tlv?.Children.Select(child => child.Content.Length),
                Is.EqualTo(new int[] { 1000, 1 }));
            Assert.That(decoded.Value?.TextValue, Is.EqualTo(text));
        });
    }

    [Test]
    public void EmbeddedPdvUsesAutomaticOuterTagsAndConstrainedComponents()
    {
        BerUsefulTypeCodec codec = CreateCodec();
        AsnRuntimeValue value = AssociatedValue(
            AsnRuntimeValueKind.EmbeddedPdv,
            "data-value",
            Identification("syntax", ObjectIdentifier(1, 2, 3)),
            [0xde, 0xad],
            null);
        BerPrimitiveEncodeResult encoded = codec.EncodeUsefulType(
            AsnTypeKind.EmbeddedPdv,
            value,
            AsnEncodingRules.Ber);
        BerPrimitiveDecodeResult decoded = codec.DecodeUsefulType(
            AsnTypeKind.EmbeddedPdv,
            encoded.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Ber);
        byte[] bytes = Write(encoded.Tlv ?? throw new InvalidOperationException());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(11)));
            Assert.That(encoded.Tlv?.Constructed, Is.True);
            Assert.That(encoded.Tlv?.Children.Select(child =>
                    new object[] { child.Tag.Class, child.Tag.Number }),
                Is.EqualTo(new[]
                {
                    new object[] { AsnTagClass.ContextSpecific,
                        System.Numerics.BigInteger.Zero },
                    new object[] { AsnTagClass.ContextSpecific,
                        new System.Numerics.BigInteger(2) }
                }));
            Assert.That(bytes,
                Is.EqualTo(new byte[]
                {
                    0x2b, 0x0a,
                    0xa0, 0x04, 0x81, 0x02, 0x2a, 0x03,
                    0x82, 0x02, 0xde, 0xad
                }));
            Assert.That(decoded.Value?.Kind,
                Is.EqualTo(AsnRuntimeValueKind.EmbeddedPdv));
            Assert.That(Field(decoded.Value, "data-value").Octets,
                Is.EqualTo(new byte[] { 0xde, 0xad }));
            Assert.That(SelectedName(Field(decoded.Value, "identification")),
                Is.EqualTo("syntax"));
        });
    }

    [Test]
    public void CerUsefulTypeImplicitStringsUseConstructedCanonicalForm()
    {
        byte[] data = new byte[1001];
        data[1000] = 0x7f;
        BerUsefulTypeCodec codec = CreateCodec();
        BerPrimitiveEncodeResult encoded = codec.EncodeUsefulType(
            AsnTypeKind.EmbeddedPdv,
            AssociatedValue(
                AsnRuntimeValueKind.EmbeddedPdv,
                "data-value",
                Identification("syntax", ObjectIdentifier(1, 2, 3)),
                data,
                null),
            AsnEncodingRules.Cer);
        BerPrimitiveDecodeResult decoded = codec.DecodeUsefulType(
            AsnTypeKind.EmbeddedPdv,
            encoded.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Cer);
        BerTlv encodedData = encoded.Tlv?.Children[1]
            ?? throw new InvalidOperationException();

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Tlv?.IndefiniteLength, Is.True);
            Assert.That(encodedData.Tag.Class,
                Is.EqualTo(AsnTagClass.ContextSpecific));
            Assert.That(encodedData.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(2)));
            Assert.That(encodedData.Constructed, Is.True);
            Assert.That(encodedData.IndefiniteLength, Is.True);
            Assert.That(encodedData.Children.Select(child => child.Tag.Number),
                Is.EqualTo(new System.Numerics.BigInteger[] { 4, 4 }));
            Assert.That(encodedData.Children.Select(child => child.Content.Length),
                Is.EqualTo(new int[] { 1000, 1 }));
            Assert.That(Field(decoded.Value, "data-value").Octets,
                Is.EqualTo(data));
        });
    }

    [Test]
    public void EveryIdentificationAlternativeUsesItsAutomaticContextTag()
    {
        string[] names =
        [
            "syntaxes",
            "syntax",
            "presentation-context-id",
            "context-negotiation",
            "transfer-syntax",
            "fixed"
        ];
        AsnRuntimeValue[] values =
        [
            SequenceValue(
                FieldValue("abstract", ObjectIdentifier(1, 2, 3)),
                FieldValue("transfer", ObjectIdentifier(1, 2, 4))),
            ObjectIdentifier(1, 2, 3),
            IntegerValue(7),
            SequenceValue(
                FieldValue("presentation-context-id", IntegerValue(9)),
                FieldValue("transfer-syntax", ObjectIdentifier(1, 3, 6, 1))),
            ObjectIdentifier(2, 5, 4, 3),
            new AsnRuntimeValue { Kind = AsnRuntimeValueKind.Null }
        ];
        BerUsefulTypeCodec codec = CreateCodec();

        int index = 0;
        while (index < names.Length)
        {
            AsnRuntimeValue source = AssociatedValue(
                AsnRuntimeValueKind.CharacterString,
                "string-value",
                Identification(names[index], values[index]),
                [0x41],
                null);
            BerPrimitiveEncodeResult encoded = codec.EncodeUsefulType(
                AsnTypeKind.CharacterString,
                source,
                AsnEncodingRules.Ber);
            BerPrimitiveDecodeResult decoded = codec.DecodeUsefulType(
                AsnTypeKind.CharacterString,
                encoded.Tlv ?? throw new InvalidOperationException(),
                AsnEncodingRules.Ber);

            Assert.That(encoded.Success, Is.True, Format(encoded.Diagnostics));
            Assert.That(encoded.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(29)));
            Assert.That(encoded.Tlv?.Children[0].Tag.Class,
                Is.EqualTo(AsnTagClass.ContextSpecific));
            Assert.That(encoded.Tlv?.Children[0].Tag.Number,
                Is.EqualTo(System.Numerics.BigInteger.Zero));
            Assert.That(encoded.Tlv?.Children[0].Constructed, Is.True);
            Assert.That(encoded.Tlv?.Children[0].Children[0].Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(index)));
            Assert.That(encoded.Tlv?.Children[0].Children[0].Constructed,
                Is.EqualTo(index == 0 || index == 3));
            if (index == 0 || index == 3)
            {
                Assert.That(encoded.Tlv?.Children[0].Children[0].Children
                        .Select(child => child.Tag.Class),
                    Is.EqualTo(new[]
                    {
                        AsnTagClass.ContextSpecific,
                        AsnTagClass.ContextSpecific
                    }));
                Assert.That(encoded.Tlv?.Children[0].Children[0].Children
                        .Select(child => child.Tag.Number),
                    Is.EqualTo(new System.Numerics.BigInteger[] { 0, 1 }));
            }

            Assert.That(encoded.Tlv?.Children[1].Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(2)));
            Assert.That(SelectedName(Field(decoded.Value, "identification")),
                Is.EqualTo(names[index]));
            index++;
        }
    }

    [Test]
    public void AssociatedUsefulTypesRejectTheConstrainedAbsentDescriptor()
    {
        AsnRuntimeValue value = AssociatedValue(
            AsnRuntimeValueKind.EmbeddedPdv,
            "data-value",
            Identification("syntax", ObjectIdentifier(1, 2, 3)),
            [0x01],
            "not permitted");
        BerPrimitiveEncodeResult encoded = CreateCodec().EncodeUsefulType(
            AsnTypeKind.EmbeddedPdv,
            value,
            AsnEncodingRules.Ber);

        Assert.That(encoded.Success, Is.False);
    }

    [Test]
    public void ExternalUsesHistoricalComponentAndEncodingChoiceTags()
    {
        BerUsefulTypeCodec codec = CreateCodec();
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.External,
            Fields =
            [
                FieldValue("direct-reference", ObjectIdentifier(1, 2, 3)),
                FieldValue("indirect-reference", IntegerValue(5)),
                FieldValue("data-value-descriptor", TextValue("data")),
                FieldValue("octet-aligned", OctetValue(0xaa, 0xbb))
            ]
        };
        BerPrimitiveEncodeResult encoded = codec.EncodeUsefulType(
            AsnTypeKind.External,
            value,
            AsnEncodingRules.Ber);
        BerPrimitiveDecodeResult decoded = codec.DecodeUsefulType(
            AsnTypeKind.External,
            encoded.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True, Format(encoded.Diagnostics));
            Assert.That(encoded.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(8)));
            Assert.That(encoded.Tlv?.Children.Select(child => child.Tag.Number),
                Is.EqualTo(new System.Numerics.BigInteger[] { 6, 2, 7, 1 }));
            Assert.That(encoded.Tlv?.Children[3].Tag.Class,
                Is.EqualTo(AsnTagClass.ContextSpecific));
            Assert.That(decoded.Value?.Kind,
                Is.EqualTo(AsnRuntimeValueKind.External));
            Assert.That(Field(decoded.Value, "octet-aligned").Octets,
                Is.EqualTo(new byte[] { 0xaa, 0xbb }));
        });
    }

    [Test]
    public void ExternalSupportsExplicitSingleValueAndArbitraryBitEncodings()
    {
        BerUsefulTypeCodec codec = CreateCodec();
        AsnRuntimeValue single = ExternalWithEncoding(
            "single-ASN1-type",
            OpenValue([0x02, 0x01, 0x09]));
        AsnRuntimeValue arbitrary = ExternalWithEncoding(
            "arbitrary",
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.BitString,
                BitStringValue = new AsnBitStringValue
                {
                    Bytes = [0xa0],
                    BitLength = 3
                }
            });
        BerPrimitiveEncodeResult singleEncoded = codec.EncodeUsefulType(
            AsnTypeKind.External,
            single,
            AsnEncodingRules.Ber);
        BerPrimitiveEncodeResult arbitraryEncoded = codec.EncodeUsefulType(
            AsnTypeKind.External,
            arbitrary,
            AsnEncodingRules.Ber);
        BerPrimitiveDecodeResult singleDecoded = codec.DecodeUsefulType(
            AsnTypeKind.External,
            singleEncoded.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(singleEncoded.Tlv?.Children[1].Tag.Number,
                Is.EqualTo(System.Numerics.BigInteger.Zero));
            Assert.That(singleEncoded.Tlv?.Children[1].Constructed, Is.True);
            Assert.That(singleEncoded.Tlv?.Children[1].Children[0].Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(2)));
            Assert.That(arbitraryEncoded.Tlv?.Children[1].Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(2)));
            Assert.That(arbitraryEncoded.Tlv?.Children[1].Content,
                Is.EqualTo(new byte[] { 5, 0xa0 }));
            Assert.That(Field(singleDecoded.Value, "single-ASN1-type")
                    .OpenTypeValue?.OriginalData,
                Is.EqualTo(new byte[] { 0x02, 0x01, 0x09 }));
        });
    }

    [Test]
    public void InstanceOfWrapsOneCompleteOpenValueExplicitly()
    {
        BerUsefulTypeCodec codec = CreateCodec();
        AsnRuntimeValue instance = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.InstanceOf,
            Fields =
            [
                FieldValue("type-id", ObjectIdentifier(1, 2, 3)),
                FieldValue("value", OpenValue([0x0c, 0x02, 0x68, 0x69]))
            ]
        };
        BerPrimitiveEncodeResult encoded = codec.EncodeUsefulType(
            AsnTypeKind.InstanceOf,
            instance,
            AsnEncodingRules.Ber);
        BerPrimitiveDecodeResult decoded = codec.DecodeUsefulType(
            AsnTypeKind.InstanceOf,
            encoded.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(8)));
            Assert.That(encoded.Tlv?.Children[0].Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(6)));
            Assert.That(encoded.Tlv?.Children[1].Tag.Class,
                Is.EqualTo(AsnTagClass.ContextSpecific));
            Assert.That(encoded.Tlv?.Children[1].Constructed, Is.True);
            Assert.That(encoded.Tlv?.Children[1].Children, Has.Length.EqualTo(1));
            Assert.That(Field(decoded.Value, "value").OpenTypeValue?.OriginalData,
                Is.EqualTo(new byte[] { 0x0c, 0x02, 0x68, 0x69 }));
        });
    }

    [Test]
    public void OpenTypeContentIsOneCompletePreservedEncoding()
    {
        BerUsefulTypeCodec codec = CreateCodec();
        AsnRuntimeValue source = OpenValue([0x02, 0x01, 0x7f]);
        BerPrimitiveEncodeResult encoded = codec.EncodeUsefulType(
            AsnTypeKind.OpenType,
            source,
            AsnEncodingRules.Ber);
        BerPrimitiveDecodeResult decoded = codec.DecodeUsefulType(
            AsnTypeKind.OpenType,
            encoded.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Ber);
        BerPrimitiveEncodeResult trailing = codec.EncodeUsefulType(
            AsnTypeKind.OpenType,
            OpenValue([0x02, 0x01, 0x01, 0x05, 0x00]),
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(2)));
            Assert.That(decoded.Value?.OpenTypeValue?.Unresolved, Is.True);
            Assert.That(decoded.Value?.OpenTypeValue?.OriginalData,
                Is.EqualTo(new byte[] { 0x02, 0x01, 0x7f }));
            Assert.That(trailing.Success, Is.False);
        });
    }

    [Test]
    public void OpenTypeContentHonorsTheConfiguredByteLimit()
    {
        BerUsefulTypeCodec codec = new BerUsefulTypeCodec(
            new AsnResourceLimits { MaximumOpenTypeBytes = 2 });
        BerPrimitiveEncodeResult encoded = codec.EncodeUsefulType(
            AsnTypeKind.OpenType,
            OpenValue([0x02, 0x01, 0x01]),
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.False);
            Assert.That(encoded.Diagnostics.Single().Category,
                Is.EqualTo(AsnDiagnosticCategory.Resource));
        });
    }

    [Test]
    public void UsefulTypeDecoderRejectsWrongOrderAndInvalidWrappers()
    {
        BerUsefulTypeCodec codec = CreateCodec();
        BerTlv wrongOrder = UniversalConstructed(
            AsnUniversalTags.EmbeddedPdv,
            UniversalPrimitive(AsnUniversalTags.OctetString, [1]),
            ContextPrimitive(1, [0x2a, 0x03]));
        BerTlv instance = UniversalConstructed(
            AsnUniversalTags.InstanceOf,
            UniversalPrimitive(AsnUniversalTags.ObjectIdentifier, [0x2a, 0x03]),
            ContextPrimitive(0, [0x02, 0x01, 0x01]));
        BerPrimitiveDecodeResult associated = codec.DecodeUsefulType(
            AsnTypeKind.EmbeddedPdv,
            wrongOrder,
            AsnEncodingRules.Ber);
        BerPrimitiveDecodeResult explicitValue = codec.DecodeUsefulType(
            AsnTypeKind.InstanceOf,
            instance,
            AsnEncodingRules.Ber);
        BerTlv invalidTree = UniversalConstructed(
            AsnUniversalTags.EmbeddedPdv);
        invalidTree.Children = new BerTlv[1];
        BerPrimitiveDecodeResult invalid = codec.DecodeUsefulType(
            AsnTypeKind.EmbeddedPdv,
            invalidTree,
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(associated.Success, Is.False);
            Assert.That(explicitValue.Success, Is.False);
            Assert.That(invalid.Diagnostics.Single().Code,
                Is.EqualTo("ASND0076"));
        });
    }

    private static BerUsefulTypeCodec CreateCodec()
    {
        return new BerUsefulTypeCodec(new AsnResourceLimits());
    }

    private static AsnRuntimeValue AssociatedValue(
        AsnRuntimeValueKind kind,
        string dataName,
        AsnRuntimeValue identification,
        byte[] data,
        string? descriptor)
    {
        List<AsnRuntimeFieldValue> fields =
        [FieldValue("identification", identification)];
        if (descriptor != null)
        {
            fields.Add(FieldValue("data-value-descriptor", TextValue(descriptor)));
        }

        fields.Add(FieldValue(dataName, OctetValue(data)));
        return new AsnRuntimeValue
        {
            Kind = kind,
            Fields = fields.ToArray()
        };
    }

    private static AsnRuntimeValue Identification(
        string name,
        AsnRuntimeValue value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Choice,
            Fields = [FieldValue(name, value)]
        };
    }

    private static AsnRuntimeValue SequenceValue(
        AsnRuntimeFieldValue first,
        AsnRuntimeFieldValue second)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields = [first, second]
        };
    }

    private static AsnRuntimeValue ExternalWithEncoding(
        string name,
        AsnRuntimeValue encoding)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.External,
            Fields =
            [
                FieldValue("direct-reference", ObjectIdentifier(1, 2, 3)),
                FieldValue(name, encoding)
            ]
        };
    }

    private static AsnRuntimeValue ObjectIdentifier(params int[] arcs)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.ObjectIdentifier,
            ObjectIdentifierValue = new AsnObjectIdentifierValue
            {
                Arcs = arcs.Select(arc =>
                    new System.Numerics.BigInteger(arc)).ToArray()
            }
        };
    }

    private static AsnRuntimeValue IntegerValue(int value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = value
        };
    }

    private static AsnRuntimeValue TextValue(string value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.CharacterString,
            TextValue = value
        };
    }

    private static AsnRuntimeValue TimeValue(
        AsnTimeKind kind,
        string text)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Time,
            TimeValue = new AsnTimeValue
            {
                Kind = kind,
                Text = text
            }
        };
    }

    private static AsnRuntimeValue OctetValue(params byte[] value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OctetString,
            Octets = value
        };
    }

    private static AsnRuntimeValue OpenValue(byte[] data)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.OpenType,
            OpenTypeValue = new AsnOpenTypeValue
            {
                Unresolved = true,
                HasEncodingRules = true,
                EncodingRules = AsnEncodingRules.Ber,
                HasOriginalData = true,
                OriginalData = data
            }
        };
    }

    private static AsnRuntimeFieldValue FieldValue(
        string name,
        AsnRuntimeValue value)
    {
        return new AsnRuntimeFieldValue
        {
            Name = name,
            Present = true,
            Value = value
        };
    }

    private static AsnRuntimeValue Field(AsnRuntimeValue? value, string name)
    {
        return value?.Fields.Single(field => field.Name == name).Value
            ?? throw new InvalidOperationException();
    }

    private static string SelectedName(AsnRuntimeValue choice)
    {
        return choice.Fields.Single(field => field.Present).Name;
    }

    private static BerTlv UniversalPrimitive(int tag, byte[] content)
    {
        return new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = tag
            },
            Content = content
        };
    }

    private static BerTlv ContextPrimitive(int tag, byte[] content)
    {
        return new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.ContextSpecific,
                Number = tag
            },
            Content = content
        };
    }

    private static BerTlv UniversalConstructed(
        int tag,
        params BerTlv[] children)
    {
        return new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = tag
            },
            Constructed = true,
            Children = children
        };
    }

    private static byte[] Write(BerTlv tlv)
    {
        BerWriteResult written = new BerTlvWriterImplementation(
            new AsnResourceLimits()).Write(new BerWriteRequest
            {
                Tlv = tlv,
                EncodingRules = AsnEncodingRules.Ber
            });
        Assert.That(written.Success, Is.True, Format(written.Diagnostics));
        return written.Data;
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(diagnostic =>
            diagnostic.Code + ":" + diagnostic.Message));
    }
}
