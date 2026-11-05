using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class SegmentValidatorTests
{
    [Test]
    public void AcceptsEmptyInputAndEveryValidModel2DataMode()
    {
        SegmentValidator.ValidateSegments(
            new QrSegment[0],
            QrSymbolKind.Model2,
            1,
            10,
            100,
            false);

        QrSegment[] segments = new QrSegment[]
        {
            new QrSegment { Mode = QrEncodingMode.Numeric, CharacterCount = 3, Text = "123" },
            new QrSegment { Mode = QrEncodingMode.Alphanumeric, CharacterCount = 3, Text = "ABC" },
            new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 3, Data = new byte[] { 0, 1, 2 } },
            new QrSegment { Mode = QrEncodingMode.Kanji, CharacterCount = 1, Text = "漢" },
            new QrSegment
            {
                Mode = QrEncodingMode.Hanzi,
                CharacterCount = 1,
                Text = "中",
                IsHanziCompatibilityMode = true
            }
        };

        Assert.That(() => SegmentValidator.ValidateSegments(
            segments,
            QrSymbolKind.Model2,
            1,
            10,
            100,
            true), Throws.Nothing);
    }

    [Test]
    public void AcceptsLegalControlOrderAndEciSwitchesBetweenDataSegments()
    {
        QrSegment structuredAppend = new QrSegment
        {
            Mode = QrEncodingMode.StructuredAppend,
            StructuredAppend = new QrStructuredAppendInfo { Index = 0, Total = 2, Parity = 0xA5 }
        };
        QrSegment[] segments = new QrSegment[]
        {
            structuredAppend,
            Fnc1Encoder.CreateFirstPositionControlSegment(),
            EciEncoder.CreateControlSegment(QrEciAssignments.Utf8),
            new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 1, Data = new byte[] { 0x41 } },
            EciEncoder.CreateControlSegment(QrEciAssignments.Iso8859_1),
            new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 1, Data = new byte[] { 0xE9 } }
        };

        Assert.That(() => SegmentValidator.ValidateSegments(
            segments,
            QrSymbolKind.Model2,
            1,
            10,
            100,
            false), Throws.Nothing);
    }

    [Test]
    public void RejectsIllegalControlOrderingAndDuplicates()
    {
        QrSegment data = new QrSegment
        {
            Mode = QrEncodingMode.Byte,
            CharacterCount = 1,
            Data = new byte[] { 1 }
        };
        QrSegment append = new QrSegment
        {
            Mode = QrEncodingMode.StructuredAppend,
            StructuredAppend = new QrStructuredAppendInfo { Index = 0, Total = 2, Parity = 1 }
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => ValidateModel2(new QrSegment[] { data, append }), Throws.TypeOf<QrInputException>());
            Assert.That(() => ValidateModel2(new QrSegment[] { append, append }), Throws.TypeOf<QrInputException>());
            Assert.That(() => ValidateModel2(new QrSegment[]
            {
                EciEncoder.CreateControlSegment(26),
                Fnc1Encoder.CreateFirstPositionControlSegment()
            }), Throws.TypeOf<QrInputException>());
            Assert.That(() => ValidateModel2(new QrSegment[]
            {
                Fnc1Encoder.CreateFirstPositionControlSegment(),
                Fnc1Encoder.CreateSecondPositionControlSegment(1)
            }), Throws.TypeOf<QrInputException>());
            Assert.That(() => ValidateModel2(new QrSegment[]
            {
                data,
                Fnc1Encoder.CreateFirstPositionControlSegment()
            }), Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void EnforcesFamilyAndMicroVersionModeRestrictions()
    {
        QrSegment numeric = new QrSegment { Mode = QrEncodingMode.Numeric, CharacterCount = 1, Text = "1" };
        QrSegment alphanumeric = new QrSegment { Mode = QrEncodingMode.Alphanumeric, CharacterCount = 1, Text = "A" };
        QrSegment bytes = new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 1, Data = new byte[] { 1 } };
        QrSegment kanji = new QrSegment { Mode = QrEncodingMode.Kanji, CharacterCount = 1, Text = "漢" };
        QrSegment eci = EciEncoder.CreateControlSegment(26);
        QrSegment fnc1 = Fnc1Encoder.CreateFirstPositionControlSegment();

        Assert.Multiple(() =>
        {
            Assert.That(() => Validate(new QrSegment[] { numeric }, QrSymbolKind.Micro, 1), Throws.Nothing);
            Assert.That(() => Validate(new QrSegment[] { alphanumeric }, QrSymbolKind.Micro, 1), Throws.TypeOf<QrInputException>());
            Assert.That(() => Validate(new QrSegment[] { alphanumeric }, QrSymbolKind.Micro, 2), Throws.Nothing);
            Assert.That(() => Validate(new QrSegment[] { bytes }, QrSymbolKind.Micro, 2), Throws.TypeOf<QrInputException>());
            Assert.That(() => Validate(new QrSegment[] { bytes, kanji }, QrSymbolKind.Micro, 3), Throws.Nothing);
            Assert.That(() => Validate(new QrSegment[] { eci }, QrSymbolKind.Micro, 4), Throws.TypeOf<QrInputException>());
            Assert.That(() => Validate(new QrSegment[] { eci, fnc1 }, QrSymbolKind.rMQR, 0), Throws.TypeOf<QrInputException>());
            Assert.That(() => Validate(new QrSegment[] { fnc1, eci, bytes }, QrSymbolKind.rMQR, 0), Throws.Nothing);
            Assert.That(() => Validate(new QrSegment[] { eci }, QrSymbolKind.LegacyModel1, 1), Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void RequiresExplicitHanziOptInAndMetadata()
    {
        QrSegment marked = new QrSegment
        {
            Mode = QrEncodingMode.Hanzi,
            CharacterCount = 1,
            Text = "中",
            IsHanziCompatibilityMode = true
        };
        QrSegment unmarked = new QrSegment
        {
            Mode = QrEncodingMode.Hanzi,
            CharacterCount = 1,
            Text = "中"
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => SegmentValidator.ValidateSegments(
                new QrSegment[] { marked }, QrSymbolKind.Model2, 1, 10, 100, true), Throws.Nothing);
            Assert.That(() => SegmentValidator.ValidateSegments(
                new QrSegment[] { marked }, QrSymbolKind.Model2, 1, 10, 100, false), Throws.TypeOf<QrInputException>());
            Assert.That(() => SegmentValidator.ValidateSegments(
                new QrSegment[] { unmarked }, QrSymbolKind.Model2, 1, 10, 100, true), Throws.TypeOf<QrInputException>());
            Assert.That(() => SegmentValidator.ValidateSegments(
                new QrSegment[] { marked }, QrSymbolKind.rMQR, 0, 10, 100, true), Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void ValidatesDataModePayloadShapeAndMetadata()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => ValidateModel2(new QrSegment[]
            {
                new QrSegment { Mode = QrEncodingMode.Numeric, CharacterCount = 2, Text = "12A" }
            }), Throws.TypeOf<QrInputException>());
            Assert.That(() => ValidateModel2(new QrSegment[]
            {
                new QrSegment { Mode = QrEncodingMode.Alphanumeric, CharacterCount = 3, Text = "ABc" }
            }), Throws.TypeOf<QrInputException>());
            Assert.That(() => ValidateModel2(new QrSegment[]
            {
                new QrSegment { Mode = QrEncodingMode.Byte, CharacterCount = 2, Data = new byte[] { 1 } }
            }), Throws.TypeOf<QrInputException>());
            Assert.That(() => ValidateModel2(new QrSegment[]
            {
                new QrSegment { Mode = QrEncodingMode.Kanji, CharacterCount = 1, Text = "A" }
            }), Throws.TypeOf<QrInputException>());
            Assert.That(() => ValidateModel2(new QrSegment[]
            {
                new QrSegment { Mode = QrEncodingMode.Byte, Data = null! }
            }), Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void ValidatesControlMetadataAndStructuredAppendBounds()
    {
        QrSegment malformedEci = EciEncoder.CreateControlSegment(26);
        malformedEci.Data[0] = 25;
        QrSegment malformedFnc1 = Fnc1Encoder.CreateSecondPositionControlSegment(42);
        malformedFnc1.Data[0] = 41;
        QrSegment malformedAppend = new QrSegment
        {
            Mode = QrEncodingMode.StructuredAppend,
            StructuredAppend = new QrStructuredAppendInfo { Index = 2, Total = 2, Parity = 256 }
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => ValidateModel2(new QrSegment[] { malformedEci }), Throws.TypeOf<QrInputException>());
            Assert.That(() => ValidateModel2(new QrSegment[] { malformedFnc1 }), Throws.TypeOf<QrInputException>());
            Assert.That(() => ValidateModel2(new QrSegment[] { malformedAppend }), Throws.TypeOf<QrInputException>());
            Assert.That(() => ValidateModel2(new QrSegment[]
            {
                new QrSegment { Mode = QrEncodingMode.StructuredAppend }
            }), Throws.TypeOf<QrInputException>());
            Assert.That(() => ValidateModel2(new QrSegment[]
            {
                new QrSegment { Mode = QrEncodingMode.Terminator }
            }), Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void EnforcesSegmentInputAndCharacterCountLimits()
    {
        QrSegment bytes = new QrSegment
        {
            Mode = QrEncodingMode.Byte,
            CharacterCount = 3,
            Data = new byte[] { 1, 2, 3 }
        };
        QrSegment tooManyNumericCharacters = new QrSegment
        {
            Mode = QrEncodingMode.Numeric,
            CharacterCount = 8,
            Text = "12345678"
        };

        Assert.Multiple(() =>
        {
            Assert.That(() => SegmentValidator.ValidateSegments(
                new QrSegment[] { bytes }, QrSymbolKind.Model2, 1, 0, 10, false),
                Throws.TypeOf<QrInputException>());
            Assert.That(() => SegmentValidator.ValidateSegments(
                new QrSegment[] { bytes, bytes }, QrSymbolKind.Model2, 1, 1, 10, false),
                Throws.TypeOf<QrResourceLimitException>());
            Assert.That(() => SegmentValidator.ValidateSegments(
                new QrSegment[] { bytes }, QrSymbolKind.Model2, 1, 1, 2, false),
                Throws.TypeOf<QrResourceLimitException>());
            Assert.That(() => SegmentValidator.ValidateSegments(
                new QrSegment[] { tooManyNumericCharacters }, QrSymbolKind.Micro, 1, 1, 20, false),
                Throws.TypeOf<QrInputException>());
        });
    }

    [Test]
    public void RejectsNullUndefinedAndOutOfRangeInputs()
    {
        QrSegment[] oneNull = new QrSegment[] { null! };

        Assert.Multiple(() =>
        {
            Assert.That(() => SegmentValidator.ValidateSegments(
                null!, QrSymbolKind.Model2, 1, 1, 1, false), Throws.TypeOf<QrInputException>());
            Assert.That(() => SegmentValidator.ValidateSegments(
                oneNull, QrSymbolKind.Model2, 1, 1, 1, false), Throws.TypeOf<QrInputException>());
            Assert.That(() => SegmentValidator.ValidateSegments(
                new QrSegment[0], (QrSymbolKind)99, 1, 1, 1, false), Throws.TypeOf<QrInputException>());
            Assert.That(() => SegmentValidator.ValidateSegments(
                new QrSegment[0], QrSymbolKind.Model2, 0, 1, 1, false), Throws.TypeOf<QrInputException>());
            Assert.That(() => SegmentValidator.ValidateSegments(
                new QrSegment[] { new QrSegment { Mode = (QrEncodingMode)6 } },
                QrSymbolKind.Model2, 1, 1, 1, false), Throws.TypeOf<QrInputException>());
            Assert.That(() => SegmentValidator.ValidateSegments(
                new QrSegment[0], QrSymbolKind.Model2, 1, 1, -1, false), Throws.TypeOf<QrInputException>());
        });
    }

    private static void ValidateModel2(QrSegment[] segments)
    {
        SegmentValidator.ValidateSegments(segments, QrSymbolKind.Model2, 1, 20, 1000, true);
    }

    private static void Validate(QrSegment[] segments, QrSymbolKind symbolKind, int version)
    {
        SegmentValidator.ValidateSegments(segments, symbolKind, version, 20, 1000, false);
    }
}
