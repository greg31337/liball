using asn1;

namespace asn1.Tests;

public class OerCollectionCodecTests
{
    [Test]
    public void RootPreamblePlacesExtensionAndPresenceBitsMostSignificantFirst()
    {
        OerPreambleCodec codec = new OerPreambleCodec(
            new AsnResourceLimits());

        OerPreambleResult empty = codec.EncodeRootPreamble(
            false,
            false,
            []);
        OerPreambleResult encoded = codec.EncodeRootPreamble(
            true,
            true,
            [false, true, false]);
        OerPreambleResult decoded = codec.DecodeRootPreamble(
            encoded.Data,
            0,
            encoded.Data.Length,
            true,
            3);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Data, Is.Empty);
            Assert.That(encoded.Data, Is.EqualTo(new byte[] { 0xa0 }));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.ExtensionPresent, Is.True);
            Assert.That(decoded.Presence,
                Is.EqualTo(new bool[] { false, true, false }));
        });
    }

    [Test]
    public void ExtensionBitmapCarriesUnusedCountAndOpenTypesCarryLengths()
    {
        AsnResourceLimits limits = new AsnResourceLimits();
        OerPreambleCodec preambles = new OerPreambleCodec(limits);
        OerOpenTypeCodec openTypes = new OerOpenTypeCodec(limits);

        OerPreambleResult bitmap = preambles.EncodeExtensionBitmap(
            [true, false, true]);
        OerPreambleResult decoded = preambles.DecodeExtensionBitmap(
            bitmap.Data,
            0,
            bitmap.Data.Length,
            false);
        OerContentResult open = openTypes.EncodeOpenType([0x01, 0x02]);
        OerContentResult empty = openTypes.EncodeOpenType([]);

        Assert.Multiple(() =>
        {
            Assert.That(bitmap.Data,
                Is.EqualTo(new byte[] { 0x02, 0x05, 0xa0 }));
            Assert.That(decoded.Presence,
                Is.EqualTo(new bool[] { true, false, true }));
            Assert.That(open.Data,
                Is.EqualTo(new byte[] { 0x02, 0x01, 0x02 }));
            Assert.That(empty.Data, Is.EqualTo(new byte[] { 0x00 }));
        });
    }

    [Test]
    public void SequenceUsesOptionalAndDefaultBitmapForRootComponents()
    {
        AsnFieldDescriptor required = Field("required", 0, BooleanType());
        AsnFieldDescriptor optional = Field("optional", 1, BooleanType());
        optional.Optional = true;
        AsnFieldDescriptor defaulted = Field("defaulted", 2, BooleanType());
        defaulted.HasDefaultValue = true;
        AsnTypeDescriptor sequence = Collection(
            AsnTypeKind.Sequence,
            [required, optional, defaulted],
            [],
            false);
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("required", Boolean(false)),
                Present("optional", Boolean(true))
            ]
        };

        OerCollectionResult encoded = Codec().EncodeCollection(
            sequence,
            value,
            false);
        OerCollectionResult decoded = Codec().DecodeCollection(
            sequence,
            encoded.Data,
            0,
            encoded.Data.Length,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x80, 0x00, 0xff }));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Value?.Fields.Single(
                field => field.Name == "optional").Value?.BooleanValue,
                Is.True);
            Assert.That(decoded.Value?.Fields.Single(
                field => field.Name == "defaulted").Present,
                Is.False);
            Assert.That(decoded.ConsumedCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void SetRootComponentsAndPreambleBitsUseCanonicalTagOrder()
    {
        AsnFieldDescriptor later = Field("later", 0, BooleanType());
        later.Optional = true;
        later.Tags = [Tag(2)];
        AsnFieldDescriptor earlier = Field("earlier", 1, BooleanType());
        earlier.Optional = true;
        earlier.Tags = [Tag(1)];
        AsnTypeDescriptor set = Collection(
            AsnTypeKind.Set,
            [later, earlier],
            [],
            false);
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Set,
            Fields = [Present("earlier", Boolean(true))]
        };

        OerCollectionResult encoded = Codec().EncodeCollection(
            set,
            value,
            false);
        OerCollectionResult decoded = Codec().DecodeCollection(
            set,
            encoded.Data,
            0,
            encoded.Data.Length,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x80, 0xff }));
            Assert.That(decoded.Value?.Fields[0].Name, Is.EqualTo("earlier"));
            Assert.That(decoded.Value?.Fields[0].Present, Is.True);
            Assert.That(decoded.Value?.Fields[1].Name, Is.EqualTo("later"));
            Assert.That(decoded.Value?.Fields[1].Present, Is.False);
        });
    }

    [Test]
    public void ExtensionAdditionGroupUsesOneBitmapBitAndOneOpenType()
    {
        AsnFieldDescriptor root = Field("root", 0, BooleanType());
        AsnFieldDescriptor single = Field("single", 1, BooleanType());
        single.Extension = true;
        AsnFieldDescriptor first = Field("first", 2, BooleanType());
        first.Extension = true;
        first.ExtensionGroupIndex = 0;
        first.Optional = true;
        AsnFieldDescriptor second = Field("second", 3, BooleanType());
        second.Extension = true;
        second.ExtensionGroupIndex = 0;
        second.Optional = true;
        AsnTypeDescriptor sequence = Collection(
            AsnTypeKind.Sequence,
            [root],
            [single, first, second],
            true);
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                Present("root", Boolean(false)),
                Present("second", Boolean(true))
            ]
        };

        OerCollectionResult encoded = Codec().EncodeCollection(
            sequence,
            value,
            false);
        OerCollectionResult decoded = Codec().DecodeCollection(
            sequence,
            encoded.Data,
            0,
            encoded.Data.Length,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data, Is.EqualTo(new byte[]
            {
                0x80, 0x00,
                0x02, 0x06, 0x40,
                0x02, 0x40, 0xff
            }));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Value?.Fields.Single(
                field => field.Name == "first").Present,
                Is.False);
            Assert.That(decoded.Value?.Fields.Single(
                field => field.Name == "second").Value?.BooleanValue,
                Is.True);
        });
    }

    [Test]
    public void InvalidPaddingAndTruncatedOpenTypeAreRejected()
    {
        OerPreambleResult padding = new OerPreambleCodec(
            new AsnResourceLimits()).DecodeRootPreamble(
                [0x81],
                0,
                1,
                false,
                1);
        AsnFieldDescriptor root = Field("root", 0, BooleanType());
        AsnFieldDescriptor addition = Field("addition", 1, BooleanType());
        addition.Extension = true;
        AsnTypeDescriptor sequence = Collection(
            AsnTypeKind.Sequence,
            [root],
            [addition],
            true);
        OerCollectionResult truncated = Codec().DecodeCollection(
            sequence,
            [0x80, 0x00, 0x02, 0x07, 0x80, 0x01],
            0,
            6,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(padding.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(padding.ErrorCode, Is.EqualTo("ASNO0051"));
            Assert.That(truncated.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
        });
    }

    [Test]
    public void PresentOptionalComponentRequiresAValue()
    {
        AsnFieldDescriptor optional = Field(
            "optional",
            0,
            BooleanType());
        optional.Optional = true;
        AsnTypeDescriptor sequence = Collection(
            AsnTypeKind.Sequence,
            [optional],
            [],
            false);

        OerCollectionResult result = Codec().EncodeCollection(
            sequence,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Sequence,
                Fields =
                [
                    new AsnRuntimeFieldValue
                    {
                        Name = "optional",
                        Present = true
                    }
                ]
            },
            false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(result.ErrorCode, Is.EqualTo("ASNO0062"));
        });
    }

    private static OerCollectionCodec Codec()
    {
        return new OerCollectionCodec(new AsnResourceLimits());
    }

    private static AsnTypeDescriptor BooleanType()
    {
        return new AsnTypeDescriptor { Kind = AsnTypeKind.Boolean };
    }

    private static AsnRuntimeValue Boolean(bool value)
    {
        return new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Boolean,
            HasBooleanValue = true,
            BooleanValue = value
        };
    }

    private static AsnRuntimeFieldValue Present(
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

    private static AsnFieldDescriptor Field(
        string name,
        int index,
        AsnTypeDescriptor type)
    {
        return new AsnFieldDescriptor
        {
            Name = name,
            Index = index,
            Type = type
        };
    }

    private static AsnTypeDescriptor Collection(
        AsnTypeKind kind,
        AsnFieldDescriptor[] roots,
        AsnFieldDescriptor[] extensions,
        bool extensible)
    {
        AsnFieldDescriptor[] fields = new AsnFieldDescriptor[
            roots.Length + extensions.Length];
        System.Array.Copy(roots, 0, fields, 0, roots.Length);
        System.Array.Copy(
            extensions,
            0,
            fields,
            roots.Length,
            extensions.Length);
        return new AsnTypeDescriptor
        {
            Kind = kind,
            Fields = fields,
            RootFields = roots,
            ExtensionFields = extensions,
            Extensible = extensible
        };
    }

    private static AsnTagDescriptor Tag(int number)
    {
        return new AsnTagDescriptor
        {
            Class = AsnTagClass.ContextSpecific,
            Number = number
        };
    }
}
