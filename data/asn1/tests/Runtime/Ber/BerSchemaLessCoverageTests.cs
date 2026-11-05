using asn1;

namespace asn1.Tests;

public class BerSchemaLessCoverageTests
{
    [Test]
    public void ReaderAcceptsEveryIdentifierClassAndTagNumberForm()
    {
        byte[][] encodings =
        [
            [0x02, 0x00],
            [0x61, 0x00],
            [0x82, 0x00],
            [0xe3, 0x00],
            [0x1f, 0x1f, 0x00],
            [0x5f, 0x81, 0x00, 0x00],
            [0xff, 0x81, 0x80, 0x00, 0x00]
        ];
        AsnTagClass[] classes =
        [
            AsnTagClass.Universal,
            AsnTagClass.Application,
            AsnTagClass.ContextSpecific,
            AsnTagClass.Private,
            AsnTagClass.Universal,
            AsnTagClass.Application,
            AsnTagClass.Private
        ];
        int[] numbers = [2, 1, 2, 3, 31, 128, 16384];
        bool[] constructed = [false, true, false, true, false, false, true];

        int index = 0;
        while (index < encodings.Length)
        {
            BerReadResult result = Read(encodings[index]);
            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Done),
                    "case " + index.ToString());
                Assert.That(result.ConsumedCount,
                    Is.EqualTo(encodings[index].Length));
                Assert.That(result.Tlv?.Tag.Class, Is.EqualTo(classes[index]));
                Assert.That(result.Tlv?.Tag.Number,
                    Is.EqualTo(new System.Numerics.BigInteger(numbers[index])));
                Assert.That(result.Tlv?.Constructed,
                    Is.EqualTo(constructed[index]));
            });
            index = index + 1;
        }
    }

    [Test]
    public void ReaderAcceptsShortAndLongDefiniteLengthForms()
    {
        int[] lengths = [0, 127, 128, 256];
        byte[][] headers =
        [
            [0x04, 0x00],
            [0x04, 0x7f],
            [0x04, 0x81, 0x80],
            [0x04, 0x82, 0x01, 0x00]
        ];

        int index = 0;
        while (index < lengths.Length)
        {
            byte[] encoded = new byte[headers[index].Length + lengths[index]];
            System.Array.Copy(
                headers[index],
                encoded,
                headers[index].Length);
            int contentIndex = headers[index].Length;
            while (contentIndex < encoded.Length)
            {
                encoded[contentIndex] = (byte)(contentIndex & 255);
                contentIndex = contentIndex + 1;
            }

            BerReadResult result = Read(encoded);
            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Done));
                Assert.That(result.Tlv?.Content, Has.Length.EqualTo(lengths[index]));
                Assert.That(result.Events[0].Length, Is.EqualTo(lengths[index]));
                Assert.That(result.ConsumedCount, Is.EqualTo(encoded.Length));
            });
            index = index + 1;
        }

        BerReadResult nonMinimalBer = Read([0x04, 0x81, 0x01, 0xaa]);
        BerReadResult nonMinimalDer = new BerTlvReaderImplementation(
            new AsnResourceLimits()).ReadSpan(
                [0x04, 0x81, 0x01, 0xaa],
                AsnEncodingRules.Der);
        Assert.Multiple(() =>
        {
            Assert.That(nonMinimalBer.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(nonMinimalDer.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(nonMinimalDer.Diagnostics.Single().Code,
                Is.EqualTo("ASND0004"));
        });
    }

    [Test]
    public void ConstructedStringsSupportTreeEventSkipAndExactSliceViews()
    {
        byte[] region =
        [
            0x24, 0x80,
            0x04, 0x02, 0x01, 0x02,
            0x24, 0x03, 0x04, 0x01, 0x03,
            0x00, 0x00
        ];
        byte[] input = [0xaa, .. region, 0xbb, 0xcc];
        BerTlvReaderImplementation reader = new BerTlvReaderImplementation(
            new AsnResourceLimits());
        BerReadResult result = reader.Read(new BerReadRequest
        {
            Data = input,
            Offset = 1,
            HasCount = true,
            Count = region.Length,
            EncodingRules = AsnEncodingRules.Ber,
            PreserveEncodedData = true
        });
        BerTlvSkipResult skipped = new BerTlvNavigator(reader).SkipValue(
            input.AsSpan(1),
            AsnEncodingRules.Ber);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(result.ConsumedCount, Is.EqualTo(region.Length));
            Assert.That(result.Tlv?.EncodedData, Is.EqualTo(region));
            Assert.That(result.Tlv?.IndefiniteLength, Is.True);
            Assert.That(result.Tlv?.Children, Has.Length.EqualTo(2));
            Assert.That(result.Tlv?.Children[0].Content,
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(result.Tlv?.Children[1].Constructed, Is.True);
            Assert.That(result.Tlv?.Children[1].EncodedData,
                Is.EqualTo(new byte[] { 0x24, 0x03, 0x04, 0x01, 0x03 }));
            Assert.That(result.Events.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    AsnTlvEventKind.StartConstructed,
                    AsnTlvEventKind.Primitive,
                    AsnTlvEventKind.StartConstructed,
                    AsnTlvEventKind.Primitive,
                    AsnTlvEventKind.EndConstructed,
                    AsnTlvEventKind.EndConstructed,
                    AsnTlvEventKind.EndOfInput
                }));
            Assert.That(result.Events.Select(item => item.Offset),
                Is.EqualTo(new[] { 1, 3, 7, 9, 12, 14, 14 }));
            Assert.That(skipped.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(skipped.SkippedCount, Is.EqualTo(region.Length));
        });
    }

    [Test]
    public void ReaderDistinguishesEveryMalformedOrTruncatedHeaderBoundary()
    {
        AssertNeedMore([]);
        AssertNeedMore([0x1f, 0x81]);
        AssertNeedMore([0x04]);
        AssertNeedMore([0x04, 0x82, 0x01]);
        AssertNeedMore([0x04, 0x03, 0x01, 0x02]);
        AssertError([0x04, 0xff], "ASND0002");
        AssertError(
            [0x04, 0x88, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],
            "ASNR0003");
        AssertError([0x30, 0x80, 0x00, 0x01], "ASND0005");
        AssertError([0x00, 0x00], "ASND0017");
        AssertError([0x04, 0x80], "ASND0018");

        BerTlvReaderImplementation reader = new BerTlvReaderImplementation(
            new AsnResourceLimits());
        BerReadResult nonMinimalTag = reader.ReadSpan(
            [0x1f, 0x1e, 0x00],
            AsnEncodingRules.Der);
        BerReadResult leadingZeroTag = reader.ReadSpan(
            [0x1f, 0x80, 0x1f, 0x00],
            AsnEncodingRules.Der);
        Assert.Multiple(() =>
        {
            Assert.That(nonMinimalTag.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(nonMinimalTag.Diagnostics.Single().Code,
                Is.EqualTo("ASND0001"));
            Assert.That(leadingZeroTag.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(leadingZeroTag.Diagnostics.Single().Code,
                Is.EqualTo("ASND0001"));
        });
    }

    [Test]
    public void ReaderEnforcesTagLengthIntegerContentAndAllocationLimits()
    {
        BerReadResult tag = ReadWithLimits(
            [0x1f, 0x81, 0x00, 0x00],
            new AsnResourceLimits { MaximumTagOctets = 2 });
        BerReadResult length = ReadWithLimits(
            [0x04, 0x82, 0x00, 0x01, 0xaa],
            new AsnResourceLimits { MaximumLengthOctets = 2 });
        BerReadResult integer = ReadWithLimits(
            [0x1f, 0x81, 0x00, 0x00],
            new AsnResourceLimits { MaximumIntegerBits = 7 });
        BerReadResult content = ReadWithLimits(
            [0x04, 0x03, 0x01, 0x02, 0x03],
            new AsnResourceLimits { MaximumOpenTypeBytes = 2 });
        BerReadResult allocation = ReadWithLimits(
            [0x04, 0x00],
            new AsnResourceLimits { MaximumTotalAllocationBytes = 63 });

        Assert.Multiple(() =>
        {
            AssertResourceError(tag, "ASNR0001");
            AssertResourceError(length, "ASNR0003");
            AssertResourceError(integer, "ASNR0002");
            AssertResourceError(content, "ASNR0006");
            AssertResourceError(allocation, "ASNR0005");
        });
    }

    [Test]
    public void IncrementalReaderResumesAtEveryBerByteBoundary()
    {
        byte[] encoded = ComplexBerValue();
        int split = 0;
        while (split <= encoded.Length)
        {
            BerIncrementalTlvDecoder decoder = new BerIncrementalTlvDecoder(
                AsnEncodingRules.Ber,
                true,
                new AsnResourceLimits());
            BerReadResult first = decoder.ConsumeChunk(new AsnIncrementalInput
            {
                Data = encoded,
                HasCount = true,
                Count = split
            });
            if (split == encoded.Length)
            {
                AssertCompleted(first, encoded, split);
            }
            else
            {
                Assert.That(first.Status,
                    Is.EqualTo(AsnDecodeStatus.NeedMoreData),
                    "split " + split.ToString());
                Assert.That(first.ConsumedCount, Is.EqualTo(split));
                BerReadResult second = decoder.ConsumeChunk(
                    new AsnIncrementalInput
                    {
                        Data = encoded,
                        Offset = split,
                        HasCount = true,
                        Count = encoded.Length - split,
                        IsFinalBlock = true
                    });
                AssertCompleted(second, encoded, encoded.Length - split);
            }

            split = split + 1;
        }
    }

    private static byte[] ComplexBerValue()
    {
        byte[] content = new byte[128];
        int index = 0;
        while (index < content.Length)
        {
            content[index] = (byte)index;
            index = index + 1;
        }

        return
        [
            0x30, 0x80,
            0x9f, 0x81, 0x00, 0x81, 0x80, .. content,
            0x24, 0x80,
            0x04, 0x02, 0xaa, 0xbb,
            0x04, 0x01, 0xcc,
            0x00, 0x00,
            0x00, 0x00
        ];
    }

    private static void AssertCompleted(
        BerReadResult result,
        byte[] encoded,
        int consumed)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(result.ConsumedCount, Is.EqualTo(consumed));
            Assert.That(result.Tlv?.EncodedData, Is.EqualTo(encoded));
            Assert.That(result.Tlv?.Children, Has.Length.EqualTo(2));
            Assert.That(result.Tlv?.Children[0].Tag.Number,
                Is.EqualTo(new System.Numerics.BigInteger(128)));
            Assert.That(result.Tlv?.Children[1].Children, Has.Length.EqualTo(2));
        });
    }

    private static BerReadResult Read(byte[] encoded)
    {
        return new BerTlvReaderImplementation(new AsnResourceLimits()).ReadSpan(
            encoded,
            AsnEncodingRules.Ber);
    }

    private static BerReadResult ReadWithLimits(
        byte[] encoded,
        AsnResourceLimits limits)
    {
        return new BerTlvReaderImplementation(limits).ReadSpan(
            encoded,
            AsnEncodingRules.Ber);
    }

    private static void AssertNeedMore(byte[] encoded)
    {
        BerReadResult result = Read(encoded);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    private static void AssertError(byte[] encoded, string code)
    {
        BerReadResult result = Read(encoded);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(code));
        });
    }

    private static void AssertResourceError(
        BerReadResult result,
        string code)
    {
        Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
        Assert.That(result.Diagnostics.Single().Category,
            Is.EqualTo(AsnDiagnosticCategory.Resource));
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(code));
    }
}
