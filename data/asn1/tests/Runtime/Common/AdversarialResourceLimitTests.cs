using asn1;

namespace asn1.Tests;

public class AdversarialResourceLimitTests
{
    [Test]
    public void BerRejectsHighTagsHugeLengthsAndDeepIndefiniteValues()
    {
        BerIdentifierReadResult tag = new BerIdentifierReader(
            new AsnResourceLimits { MaximumTagOctets = 2 })
            .ReadIdentifier([0x1f, 0x81, 0x80, 0x00], false);
        BerLengthReadResult length = new BerLengthReader(
            new AsnResourceLimits { MaximumLengthOctets = 2 })
            .ReadLength(
                [0x88, 0x7f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff],
                false);
        BerReadResult nesting = new BerTlvReaderImplementation(
            new AsnResourceLimits { MaximumNestingDepth = 1 })
            .ReadSpan(
                [0x30, 0x80, 0x30, 0x80, 0x04, 0x00,
                    0x00, 0x00, 0x00, 0x00],
                AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(tag.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(tag.LimitExceeded, Is.True);
            Assert.That(tag.ErrorCode, Is.EqualTo("ASNR0001"));
            Assert.That(length.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(length.LimitExceeded, Is.True);
            Assert.That(length.ErrorCode, Is.EqualTo("ASNR0003"));
            Assert.That(nesting.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(nesting.Diagnostics.Single().Code,
                Is.EqualTo("ASNR0004"));
        });
    }

    [Test]
    public void BerRejectsGiantIntegersAndObjectIdentifierArcs()
    {
        AsnResourceLimits limits = new AsnResourceLimits
        {
            MaximumIntegerBits = 8
        };
        BerPrimitiveValueResult integer = new BerIntegerCodec(limits)
            .DecodeInteger([0x01, 0x00]);
        BerPrimitiveValueResult oid = new BerObjectIdentifierCodec(limits)
            .DecodeObjectIdentifier([0x2a, 0x81, 0x80, 0x00], false);

        Assert.Multiple(() =>
        {
            Assert.That(integer.Success, Is.False);
            Assert.That(integer.LimitExceeded, Is.True);
            Assert.That(integer.ErrorCode, Is.EqualTo("ASNR0010"));
            Assert.That(oid.Success, Is.False);
            Assert.That(oid.LimitExceeded, Is.True);
            Assert.That(oid.ErrorCode, Is.EqualTo("ASNR0010"));
        });
    }

    [Test]
    public void PerRejectsOversizedFragmentBeforeReadingItsPayload()
    {
        AsnResourceLimits limits = new AsnResourceLimits
        {
            MaximumOpenTypeBytes = 1024
        };
        PerBitReader reader = new PerBitReader(limits);
        reader.AppendInputBits([0xc1], 0, 8, true);

        PerPrimitiveResult result = new PerPrimitiveCodec(limits)
            .ReadBitString(reader, 0, false, 0, false);

        byte[] fragments = new byte[10243];
        fragments[0] = 0xc4;
        fragments[8193] = 0xc1;
        fragments[10242] = 0x00;
        PerBitReader cumulativeReader = new PerBitReader(
            new AsnResourceLimits());
        cumulativeReader.AppendInputBits(
            fragments,
            0,
            fragments.Length * 8,
            true);
        PerPrimitiveResult cumulative = new PerPrimitiveCodec(
            new AsnResourceLimits { MaximumOpenTypeBytes = 9000 })
            .ReadBitString(cumulativeReader, 0, false, 0, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(result.LimitExceeded, Is.True);
            Assert.That(result.ErrorCode, Is.EqualTo("ASNR0006"));
            Assert.That(reader.Position, Is.EqualTo(8));
            Assert.That(cumulative.Success, Is.False);
            Assert.That(cumulative.Status,
                Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(cumulative.LimitExceeded, Is.True);
            Assert.That(cumulative.ErrorCode, Is.EqualTo("ASNR0006"));
        });
    }

    [Test]
    public void PerAndOerRejectOversizedExtensionBitmaps()
    {
        AsnResourceLimits unlimited = new AsnResourceLimits();
        PerBitWriter writer = new PerBitWriter(unlimited);
        PerCollectionResult perEncoded = new PerCollectionCodec(unlimited)
            .WriteExtensionBitmap(writer, [true, false, true], false);
        PerBitWriteResult perData = writer.GetOutput();
        AsnResourceLimits limited = new AsnResourceLimits
        {
            MaximumCollectionElements = 2
        };
        PerBitReader reader = new PerBitReader(limited);
        reader.AppendInputBits(
            perData.Data,
            0,
            perData.WrittenBitCount,
            true);
        PerCollectionResult per = new PerCollectionCodec(limited)
            .ReadExtensionBitmap(reader, false);

        OerPreambleResult oerData = new OerPreambleCodec(unlimited)
            .EncodeExtensionBitmap([true, false, true]);
        OerPreambleResult oer = new OerPreambleCodec(limited)
            .DecodeExtensionBitmap(
                oerData.Data,
                0,
                oerData.Data.Length,
                false);

        Assert.Multiple(() =>
        {
            Assert.That(perEncoded.Success, Is.True);
            Assert.That(per.Success, Is.False);
            Assert.That(per.LimitExceeded, Is.True);
            Assert.That(per.ErrorCode, Is.EqualTo("ASNR0011"));
            Assert.That(oerData.Success, Is.True);
            Assert.That(oer.Success, Is.False);
            Assert.That(oer.LimitExceeded, Is.True);
            Assert.That(oer.ErrorCode, Is.EqualTo("ASNR0011"));
        });
    }

    [Test]
    public void XerRejectsEntitiesNamespaceFloodsAndOversizedText()
    {
        XerXmlReadResult entity = ReadXml(
            "<a>&expansion;</a>",
            new AsnResourceLimits());
        XerXmlReadResult namespaces = ReadXml(
            "<a xmlns:x=\"urn:x\" xmlns:y=\"urn:y\" xmlns:z=\"urn:z\"/>",
            new AsnResourceLimits { MaximumCollectionElements = 2 });
        XerXmlReadResult text = ReadXml(
            "<a>0123456789</a>",
            new AsnResourceLimits { MaximumTextCharacters = 8 });

        Assert.Multiple(() =>
        {
            Assert.That(entity.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(entity.ErrorCode, Is.EqualTo("ASNXER0023"));
            Assert.That(namespaces.Status,
                Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(namespaces.LimitExceeded, Is.True);
            Assert.That(namespaces.ErrorCode, Is.EqualTo("ASNR0011"));
            Assert.That(text.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(text.LimitExceeded, Is.True);
            Assert.That(text.ErrorCode, Is.EqualTo("ASNR0009"));
        });
    }

    [Test]
    public void EcnRejectsRepetitionCountBeforeIterating()
    {
        AsnResourceLimits limits = new AsnResourceLimits
        {
            MaximumCollectionElements = 2
        };
        EcnExecutablePlan plan = new EcnExecutablePlan
        {
            Root = new EcnExecutablePlanStep
            {
                Kind = EcnPlanStepKind.Repetition
            },
            Valid = true,
            Decodable = true
        };
        EcnPlanExecutionBinding binding = new EcnPlanExecutionBinding
        {
            Path = "$item",
            RepetitionCount = int.MaxValue
        };

        EcnPlanExecutionResult result = new EcnPlanExecutor(limits)
            .DecodeRepetitionValue(
                plan,
                [binding],
                new EcnPlanBitCursor { FinalBlock = true },
                0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(result.LimitExceeded, Is.True);
            Assert.That(result.ErrorCode, Is.EqualTo("ASNR0002"));
            Assert.That(result.Value, Is.Null);
        });
    }

    [Test]
    public void ValidationStopsAtTheConfiguredDiagnosticLimit()
    {
        AsnTypeDescriptor constrained = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Integer,
            Constraints = [ZeroOnlyConstraint()]
        };
        AsnTypeDescriptor sequence = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Sequence,
            Fields =
            [
                Field("first", 0, constrained),
                Field("second", 1, constrained),
                Field("third", 2, constrained)
            ]
        };
        AsnRuntimeValue value = new AsnRuntimeValue
        {
            Kind = AsnRuntimeValueKind.Sequence,
            Fields =
            [
                RuntimeField("first"),
                RuntimeField("second"),
                RuntimeField("third")
            ]
        };

        AsnDiagnostic[] diagnostics = new RuntimeConstraintEvaluator()
            .ValidateRuntimeValue(
                value,
                sequence,
                new AsnResourceLimits { MaximumDiagnostics = 2 });

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Has.Length.EqualTo(2));
            Assert.That(diagnostics.Select(item => item.Code),
                Is.EqualTo(new[] { "ASNC0031", "ASNC0031" }));
            Assert.That(diagnostics.Select(item => item.ValuePath),
                Is.EqualTo(new[] { "$.first", "$.second" }));
        });
    }

    [Test]
    public void OerRejectsDeclaredContentLimitBeforeWaitingForPayload()
    {
        AsnResourceLimits limits = new AsnResourceLimits
        {
            MaximumOpenTypeBytes = 8
        };

        OerContentResult result = new OerContentCodec(limits)
            .DecodeVariableContent([0x82, 0x01, 0x00], 0, 3, false);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(result.LimitExceeded, Is.True);
            Assert.That(result.ErrorCode, Is.EqualTo("ASNR0020"));
            Assert.That(result.Content, Is.Empty);
        });
    }

    private static XerXmlReadResult ReadXml(
        string xml,
        AsnResourceLimits limits)
    {
        return new XerXmlTokenReader(limits).ConsumeXml(
            new AsnIncrementalInput
            {
                Data = System.Text.Encoding.UTF8.GetBytes(xml),
                IsFinalBlock = true
            });
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

    private static AsnRuntimeFieldValue RuntimeField(string name)
    {
        return new AsnRuntimeFieldValue
        {
            Name = name,
            Present = true,
            Value = new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = 1
            }
        };
    }

    private static AsnConstraintDescriptor ZeroOnlyConstraint()
    {
        return new AsnConstraintDescriptor
        {
            Kind = AsnConstraintKind.ValueRange,
            DomainKind = AsnTypeKind.Integer,
            RootIntervals =
            [
                new AsnIntervalDescriptor
                {
                    HasLowerBound = true,
                    LowerBound = System.Numerics.BigInteger.Zero,
                    HasUpperBound = true,
                    UpperBound = System.Numerics.BigInteger.Zero
                }
            ]
        };
    }
}
