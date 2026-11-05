using asn1;

namespace asn1.Tests;

public class BerTlvDataTests
{
    [Test]
    public void TlvRejectsAnInvalidNestedTagClass()
    {
        BerTlv tlv = new BerTlv
        {
            Tag = new AsnTag
            {
                Class = (AsnTagClass)99,
                Number = 1
            }
        };

        Assert.That(tlv.IsValid(), Is.False);
    }

    [Test]
    public void TlvTreeEnforcesPrimitiveAndConstructedShape()
    {
        BerTlv primitive = new BerTlv
        {
            Tag = new AsnTag { Class = AsnTagClass.Universal, Number = 2 },
            Content = [0x01],
            EncodedData = [0x02, 0x01, 0x01]
        };
        BerTlv constructed = new BerTlv
        {
            Tag = new AsnTag { Class = AsnTagClass.Universal, Number = 16 },
            Constructed = true,
            IndefiniteLength = true,
            Children = [primitive]
        };
        BerTlv indefinitePrimitive = new BerTlv
        {
            Tag = new AsnTag { Number = 4 },
            IndefiniteLength = true,
            Content = [0x00]
        };
        BerTlv constructedWithRawContent = new BerTlv
        {
            Tag = new AsnTag { Number = 16 },
            Constructed = true,
            Content = [0x00]
        };
        BerTlv negativeTag = new BerTlv { Tag = new AsnTag { Number = -1 } };

        Assert.That(primitive.IsValid(), Is.True);
        Assert.That(constructed.IsValid(), Is.True);
        Assert.That(indefinitePrimitive.IsValid(), Is.False);
        Assert.That(constructedWithRawContent.IsValid(), Is.False);
        Assert.That(negativeTag.IsValid(), Is.False);
    }

    [Test]
    public void TlvRequestsResultsAndEventsRetainExactProgress()
    {
        BerTlv tlv = new BerTlv
        {
            Tag = new AsnTag { Class = AsnTagClass.ContextSpecific, Number = 128 },
            Content = [0xaa]
        };
        BerTlvEvent tlvEvent = new BerTlvEvent
        {
            Kind = AsnTlvEventKind.Primitive,
            Tag = tlv.Tag,
            Offset = 3,
            HasLength = true,
            Length = 1,
            Data = [0xaa]
        };
        BerReadRequest readRequest = new BerReadRequest
        {
            Data = [0x9f, 0x81, 0x00, 0x01, 0xaa],
            Offset = 0,
            HasCount = true,
            Count = 5,
            EncodingRules = AsnEncodingRules.Ber,
            PreserveEncodedData = true
        };
        BerReadResult readResult = new BerReadResult
        {
            Status = AsnDecodeStatus.Done,
            ConsumedCount = 5,
            Tlv = tlv,
            Events = [tlvEvent]
        };
        BerWriteRequest writeRequest = new BerWriteRequest
        {
            Tlv = tlv,
            Events = [tlvEvent],
            EncodingRules = AsnEncodingRules.Der
        };
        BerWriteResult writeResult = new BerWriteResult
        {
            Success = true,
            Data = readRequest.Data,
            WrittenCount = 5
        };

        Assert.That(readRequest.HasCount, Is.True);
        Assert.That(readResult.ConsumedCount, Is.EqualTo(5));
        Assert.That(readResult.Events[0].Length, Is.EqualTo(1));
        Assert.That(readResult.Tlv, Is.SameAs(tlv));
        Assert.That(writeRequest.Events[0].Tag, Is.SameAs(tlv.Tag));
        Assert.That(writeResult.Data, Is.SameAs(readRequest.Data));
        Assert.That(writeResult.WrittenCount, Is.EqualTo(5));
    }

    [Test]
    public void TlvContractsReturnDedicatedReadAndWriteResults()
    {
        ResultTlvReader reader = new ResultTlvReader();
        ResultTlvWriter writer = new ResultTlvWriter();

        BerReadResult readResult = reader.Read(new BerReadRequest());
        BerWriteResult writeResult = writer.Write(new BerWriteRequest());

        Assert.That(readResult, Is.SameAs(reader.Result));
        Assert.That(writeResult, Is.SameAs(writer.Result));
    }

    private class ResultTlvReader : IBerTlvReader
    {
        public BerReadResult Result = new BerReadResult();

        public BerReadResult Read(BerReadRequest request)
        {
            return this.Result;
        }

        public BerReadResult ReadSpan(ReadOnlySpan<byte> input, AsnEncodingRules rules)
        {
            return this.Result;
        }

        public BerReadResult ReadStream(Stream input, AsnEncodingRules rules)
        {
            return this.Result;
        }
    }

    private class ResultTlvWriter : IBerTlvWriter
    {
        public BerWriteResult Result = new BerWriteResult();

        public BerWriteResult Write(BerWriteRequest request)
        {
            return this.Result;
        }

        public BerWriteResult WriteSpan(BerWriteRequest request, Span<byte> destination)
        {
            return this.Result;
        }

        public BerWriteResult WriteStream(BerWriteRequest request, Stream output)
        {
            return this.Result;
        }
    }
}
