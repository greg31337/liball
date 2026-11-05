using asn1;

namespace asn1.Tests;

public class BerConstructedCodecTests
{
    [Test]
    public void SequenceAndSetHonorRequiredOptionalDefaultAndExtensionFields()
    {
        BerConstructedCodec codec = CreateCodec();
        BerComponentValue required = MakeComponent("required", true, false, false, false, false, 1);
        BerComponentValue optional = MakeComponent("optional", false, true, false, false, false, 2);
        BerComponentValue defaulted = MakeComponent("defaulted", true, false, true, false, true, 3);
        BerComponentValue extension = MakeComponent("extension", true, false, false, true, false, 4);

        BerConstructedResult ber = codec.EncodeSequenceValue(
            [required, optional, defaulted, extension],
            AsnEncodingRules.Ber,
            false);
        BerConstructedResult der = codec.EncodeSetValue(
            [required, optional, defaulted, extension],
            AsnEncodingRules.Der,
            false);
        BerConstructedResult missing = codec.EncodeSequenceValue(
            [MakeComponent("required", false, false, false, false, false, 1)],
            AsnEncodingRules.Ber,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(ber.Success, Is.True);
            Assert.That(ber.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(16)));
            Assert.That(ber.Tlv?.Children.Select(item => item.Content[0]),
                Is.EqualTo(new byte[] { 1, 3, 4 }));
            Assert.That(der.Success, Is.True);
            Assert.That(der.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(17)));
            Assert.That(der.Tlv?.Children.Select(item => item.Content[0]),
                Is.EqualTo(new byte[] { 1, 4 }));
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.ErrorCode, Is.EqualTo("ASNE0062"));
        });
    }

    [Test]
    public void ChoiceAndCollectionFormsValidateTheirMembers()
    {
        BerConstructedCodec codec = CreateCodec();
        BerComponentValue absent = MakeComponent("a", false, true, false, false, false, 1);
        BerComponentValue selected = MakeComponent("b", true, true, false, false, false, 2);
        BerConstructedResult choice = codec.EncodeChoiceValue([absent, selected]);
        BerConstructedResult none = codec.EncodeChoiceValue([absent]);
        BerConstructedResult multiple = codec.EncodeChoiceValue([selected, selected]);
        BerConstructedResult sequenceOf = codec.EncodeSequenceOfValue(
            [MakePrimitive(7), MakePrimitive(8)],
            AsnEncodingRules.Ber,
            true);
        BerConstructedResult setOf = codec.EncodeSetOfValue(
            [MakePrimitive(9)],
            AsnEncodingRules.Der,
            false);
        BerConstructedResult readSequenceOf = codec.ReadSequenceOfValue(
            sequenceOf.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Ber);
        BerConstructedResult readSetOf = codec.ReadSetOfValue(
            setOf.Tlv ?? throw new InvalidOperationException(),
            AsnEncodingRules.Der);

        Assert.Multiple(() =>
        {
            Assert.That(choice.Tlv?.Content, Is.EqualTo(new byte[] { 2 }));
            Assert.That(none.Success, Is.False);
            Assert.That(multiple.Success, Is.False);
            Assert.That(sequenceOf.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(16)));
            Assert.That(sequenceOf.Tlv?.IndefiniteLength, Is.True);
            Assert.That(setOf.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(17)));
            Assert.That(readSequenceOf.Children.Length, Is.EqualTo(2));
            Assert.That(readSetOf.Children.Length, Is.EqualTo(1));
        });
    }

    [Test]
    public void DerSetOrdersComponentsByTag()
    {
        BerConstructedCodec codec = CreateCodec();
        BerComponentValue contextThree = MakeComponent(
            "context-three", true, false, false, false, false, 3);
        contextThree.Tlv = MakeTaggedPrimitive(
            AsnTagClass.ContextSpecific, 3, 3);
        BerComponentValue contextOneFirst = MakeComponent(
            "context-one-first", true, false, false, false, false, 1);
        contextOneFirst.Tlv = MakeTaggedPrimitive(
            AsnTagClass.ContextSpecific, 1, 1);
        BerComponentValue contextTwo = MakeComponent(
            "context-two", true, false, false, false, false, 2);
        contextTwo.Tlv = MakeTaggedPrimitive(
            AsnTagClass.ContextSpecific, 2, 2);
        BerComponentValue application = MakeComponent(
            "application", true, false, false, false, false, 9);
        application.Tlv = MakeTaggedPrimitive(AsnTagClass.Application, 9, 9);
        BerComponentValue universal = MakeComponent(
            "universal", true, false, false, false, false, 20);
        universal.Tlv = MakeTaggedPrimitive(AsnTagClass.Universal, 20, 20);
        BerComponentValue[] components =
        [
            contextThree,
            contextOneFirst,
            contextTwo,
            application,
            universal
        ];

        BerConstructedResult ber = codec.EncodeSetValue(
            components,
            AsnEncodingRules.Ber,
            false);
        BerConstructedResult der = codec.EncodeSetValue(
            components,
            AsnEncodingRules.Der,
            false);
        BerConstructedResult cer = codec.EncodeSetValue(
            components,
            AsnEncodingRules.Cer,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(ber.Tlv?.Children.Select(child => child.Content[0]),
                Is.EqualTo(new byte[] { 3, 1, 2, 9, 20 }));
            Assert.That(der.Tlv?.Children.Select(child => child.Content[0]),
                Is.EqualTo(new byte[] { 20, 9, 1, 2, 3 }));
            Assert.That(cer.Tlv?.Children.Select(child => child.Content[0]),
                Is.EqualTo(new byte[] { 20, 9, 1, 2, 3 }));
            Assert.That(cer.Tlv?.IndefiniteLength, Is.True);
        });
    }

    [Test]
    public void DerSetOfOrdersByCompleteEncodingAndLeavesBerOrderUntouched()
    {
        BerConstructedCodec codec = CreateCodec();
        BerTlv context = MakeTaggedPrimitive(
            AsnTagClass.ContextSpecific, 0, 0);
        BerTlv integerHigh = MakeTaggedPrimitive(
            AsnTagClass.Universal, 2, 255);
        BerTlv integerLow = MakeTaggedPrimitive(
            AsnTagClass.Universal, 2, 1);
        BerTlv[] elements = [context, integerHigh, integerLow];

        BerConstructedResult ber = codec.EncodeSetOfValue(
            elements,
            AsnEncodingRules.Ber,
            false);
        BerConstructedResult der = codec.EncodeSetOfValue(
            elements,
            AsnEncodingRules.Der,
            false);
        BerConstructedResult cer = codec.EncodeSetOfValue(
            elements,
            AsnEncodingRules.Cer,
            false);
        BerWriteResult written = new BerTlvWriterImplementation(
            new AsnResourceLimits()).Write(new BerWriteRequest
            {
                Tlv = der.Tlv,
                EncodingRules = AsnEncodingRules.Der
            });

        Assert.Multiple(() =>
        {
            Assert.That(ber.Tlv?.Children.Select(child => child.Content[0]),
                Is.EqualTo(new byte[] { 0, 255, 1 }));
            Assert.That(der.Tlv?.Children.Select(child => child.Content[0]),
                Is.EqualTo(new byte[] { 1, 255, 0 }));
            Assert.That(cer.Tlv?.Children.Select(child => child.Content[0]),
                Is.EqualTo(new byte[] { 1, 255, 0 }));
            Assert.That(cer.Tlv?.IndefiniteLength, Is.True);
            Assert.That(written.Data, Is.EqualTo(new byte[]
            {
                0x31, 0x09,
                0x02, 0x01, 0x01,
                0x02, 0x01, 0xff,
                0x80, 0x01, 0x00
            }));
            Assert.That(elements[0], Is.SameAs(context));
        });
    }

    [Test]
    public void DerSetOfSortingIsBoundedAndReportsTheFirstEncodingFailure()
    {
        BerConstructedCodec limited = new BerConstructedCodec(
            new AsnResourceLimits { MaximumTotalAllocationBytes = 21 });
        BerConstructedResult exhausted = limited.EncodeSetOfValue(
            [MakePrimitive(1), MakePrimitive(2)],
            AsnEncodingRules.Der,
            false);
        BerTlv indefinite = new BerTlv
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.Universal,
                Number = AsnUniversalTags.Sequence
            },
            Constructed = true,
            IndefiniteLength = true
        };
        BerConstructedResult invalid = CreateCodec().EncodeSetOfValue(
            [MakePrimitive(1), indefinite],
            AsnEncodingRules.Der,
            false);

        Assert.Multiple(() =>
        {
            Assert.That(exhausted.Success, Is.False);
            Assert.That(exhausted.LimitExceeded, Is.True);
            Assert.That(exhausted.ErrorCode, Is.EqualTo("ASNR0005"));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.ErrorCode, Is.EqualTo("ASNE0020"));
            Assert.That(invalid.ErrorMessage,
                Does.StartWith("Canonical SET OF element 1 "));
        });
    }

    [Test]
    public void ExplicitAndImplicitTagChainsRoundTripWithoutMutatingTheValue()
    {
        BerConstructedCodec codec = CreateCodec();
        BerTlv original = MakePrimitive(5);
        AsnTagDescriptor[] tags =
        [
            new AsnTagDescriptor
            {
                Class = AsnTagClass.Application,
                Number = 7,
                Mode = AsnTaggingMode.Explicit
            },
            new AsnTagDescriptor
            {
                Class = AsnTagClass.ContextSpecific,
                Number = 3,
                Mode = AsnTaggingMode.Implicit
            },
            new AsnTagDescriptor
            {
                Class = AsnTagClass.Universal,
                Number = 2,
                Mode = AsnTaggingMode.Implicit
            }
        ];

        BerConstructedResult applied = codec.ApplyTagChain(original, tags);
        BerConstructedResult removed = codec.RemoveTagChain(
            applied.Tlv ?? throw new InvalidOperationException(),
            tags);
        BerConstructedResult wrong = codec.RemoveTagChain(original, tags);

        Assert.Multiple(() =>
        {
            Assert.That(applied.Success, Is.True);
            Assert.That(applied.Tlv?.Tag.Class, Is.EqualTo(AsnTagClass.Application));
            Assert.That(applied.Tlv?.Constructed, Is.True);
            Assert.That(applied.Tlv?.Children.Single().Tag.Class,
                Is.EqualTo(AsnTagClass.ContextSpecific));
            Assert.That(removed.Tlv?.Tag.Class, Is.EqualTo(AsnTagClass.Universal));
            Assert.That(removed.Tlv?.Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(2)));
            Assert.That(removed.Tlv?.Content, Is.EqualTo(new byte[] { 5 }));
            Assert.That(original.Tag.Class, Is.EqualTo(AsnTagClass.Universal));
            Assert.That(wrong.ErrorCode, Is.EqualTo("ASND0088"));
        });
    }

    [Test]
    public void RecursiveIndefiniteConversionMarksConstructedNodesAndWritesEoc()
    {
        BerConstructedCodec codec = CreateCodec();
        BerTlv nested = new BerTlv
        {
            Tag = new AsnTag { Number = 16 },
            Constructed = true,
            Children =
            [
                new BerTlv
                {
                    Tag = new AsnTag { Number = 17 },
                    Constructed = true,
                    Children = [MakePrimitive(5)]
                }
            ]
        };

        BerConstructedResult converted = codec.MakeIndefiniteRecursively(
            nested,
            AsnEncodingRules.Ber);
        BerWriteResult written = new BerTlvWriterImplementation(
            new AsnResourceLimits()).Write(new BerWriteRequest
            {
                Tlv = converted.Tlv,
                EncodingRules = AsnEncodingRules.Ber
            });
        BerConstructedResult der = codec.MakeIndefiniteRecursively(
            nested,
            AsnEncodingRules.Der);

        Assert.Multiple(() =>
        {
            Assert.That(converted.Tlv?.IndefiniteLength, Is.True);
            Assert.That(converted.Tlv?.Children[0].IndefiniteLength, Is.True);
            Assert.That(converted.Tlv?.Children[0].Children[0].IndefiniteLength,
                Is.False);
            Assert.That(written.Data, Is.EqualTo(new byte[]
            {
                0x30, 0x80, 0x31, 0x80, 0x02, 0x01, 0x05,
                0x00, 0x00, 0x00, 0x00
            }));
            Assert.That(der.Success, Is.False);
            Assert.That(der.ErrorCode, Is.EqualTo("ASNE0058"));
            Assert.That(nested.IndefiniteLength, Is.False);
        });
    }

    private static BerConstructedCodec CreateCodec()
    {
        return new BerConstructedCodec(new AsnResourceLimits());
    }

    private static BerComponentValue MakeComponent(
        string name,
        bool present,
        bool optional,
        bool hasDefault,
        bool extension,
        bool equalsDefault,
        byte value)
    {
        return new BerComponentValue
        {
            Field = new AsnFieldDescriptor
            {
                Name = name,
                Optional = optional,
                HasDefaultValue = hasDefault,
                Extension = extension
            },
            Present = present,
            EqualsDefault = equalsDefault,
            Tlv = MakePrimitive(value)
        };
    }

    private static BerTlv MakePrimitive(byte value)
    {
        return MakeTaggedPrimitive(AsnTagClass.Universal, 2, value);
    }

    private static BerTlv MakeTaggedPrimitive(
        AsnTagClass tagClass,
        int tagNumber,
        byte value)
    {
        return new BerTlv
        {
            Tag = new AsnTag
            {
                Class = tagClass,
                Number = tagNumber
            },
            Content = [value]
        };
    }
}
