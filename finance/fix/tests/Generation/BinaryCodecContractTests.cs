using System;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class BinaryCodecContractTests
{
    [Test]
    public void ExternalCodecCanReturnCanonicalMessageWithDiscoveredMetadata()
    {
        IBinaryMessageCodec codec = new ExampleBinaryCodec();
        BinaryMessageContext supplied = new BinaryMessageContext
        {
            EncodingType = BinaryEncodingType.Sbe,
            EncodingIdentifier = FixEncodingTypes.SbeVersion20LittleEndian,
            DictionaryIdentity = new FixDictionaryIdentity { Name = "Venue", Scenario = "base" }
        };

        BinaryDecodeResult result = codec.DecodeBinary([42, 0, 101, 0, 7, 0, (byte)'D'], supplied);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(result.Consumed, Is.EqualTo(7));
            Assert.That(result.Message, Is.Not.Null);
            Assert.That(result.Message!.MsgType, Is.EqualTo("D"));
            Assert.That(result.Context, Is.Not.SameAs(supplied));
            Assert.That(result.Context.EncodingType, Is.EqualTo(BinaryEncodingType.Sbe));
            Assert.That(result.Context.EncodingIdentifier, Is.EqualTo(FixEncodingTypes.SbeVersion20LittleEndian));
            Assert.That(result.Context.SchemaId, Is.EqualTo(42));
            Assert.That(result.Context.TemplateId, Is.EqualTo(101));
            Assert.That(result.Context.Version, Is.EqualTo(7));
            Assert.That(result.Context.MsgType, Is.EqualTo("D"));
            Assert.That(result.Context.DictionaryIdentity, Is.SameAs(supplied.DictionaryIdentity));
        });
    }

    [Test]
    public void DecodedMetadataCanDriveCanonicalReencodingWithoutMutatingInputContext()
    {
        IBinaryMessageCodec codec = new ExampleBinaryCodec();
        BinaryMessageContext supplied = new BinaryMessageContext
        {
            EncodingType = BinaryEncodingType.Sbe,
            EncodingIdentifier = FixEncodingTypes.SbeVersion20LittleEndian,
            DictionaryIdentity = new FixDictionaryIdentity { Name = "Venue", ApplicationVersion = "FIX.Latest", Scenario = "base" }
        };
        BinaryDecodeResult decoded = codec.DecodeBinary([42, 0, 101, 0, 7, 0, (byte)'D'], supplied);
        byte[] destination = new byte[7];

        BinaryEncodeResult encoded = codec.EncodeBinary(decoded.Message!, decoded.Context, destination);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Status, Is.EqualTo(EncodeStatus.Success));
            Assert.That(encoded.BytesWritten, Is.EqualTo(7));
            Assert.That(encoded.RequiredSize, Is.EqualTo(7));
            Assert.That(destination, Is.EqualTo(new byte[] { 42, 0, 101, 0, 7, 0, (byte)'D' }));
            Assert.That(encoded.Context, Is.Not.SameAs(decoded.Context));
            Assert.That(encoded.Context.SchemaId, Is.EqualTo(42));
            Assert.That(encoded.Context.TemplateId, Is.EqualTo(101));
            Assert.That(encoded.Context.Version, Is.EqualTo(7));
            Assert.That(encoded.Context.EncodingIdentifier, Is.EqualTo(FixEncodingTypes.SbeVersion20LittleEndian));
            Assert.That(encoded.Context.MsgType, Is.EqualTo("D"));
            Assert.That(supplied.SchemaId, Is.Zero);
            Assert.That(supplied.TemplateId, Is.Zero);
            Assert.That(supplied.Version, Is.Zero);
            Assert.That(supplied.MsgType, Is.Empty);
        });
    }

    [Test]
    public void ExternalCodecReportsRequiredSizesAndRetainsKnownMetadataOnExpectedFailure()
    {
        IBinaryMessageCodec codec = new ExampleBinaryCodec();
        BinaryMessageContext context = new BinaryMessageContext
        {
            EncodingType = BinaryEncodingType.Proprietary,
            EncodingIdentifier = 12,
            SchemaId = 9,
            TemplateId = 8,
            Version = 7,
            MsgType = "Z"
        };
        BinaryDecodeResult incomplete = codec.DecodeBinary([1, 2], context);
        BinaryEncodeResult tooSmall = codec.EncodeBinary(new FixMessage { MsgType = "Z" }, context, new byte[2]);

        Assert.Multiple(() =>
        {
            Assert.That(incomplete.Status, Is.EqualTo(DecodeStatus.Incomplete));
            Assert.That(incomplete.Consumed, Is.Zero);
            Assert.That(incomplete.Required, Is.EqualTo(7));
            Assert.That(incomplete.Context.EncodingType, Is.EqualTo(BinaryEncodingType.Proprietary));
            Assert.That(incomplete.Context.EncodingIdentifier, Is.EqualTo(12));
            Assert.That(incomplete.Context.TemplateId, Is.EqualTo(8));
            Assert.That(tooSmall.Status, Is.EqualTo(EncodeStatus.DestinationTooSmall));
            Assert.That(tooSmall.BytesWritten, Is.Zero);
            Assert.That(tooSmall.RequiredSize, Is.EqualTo(7));
            Assert.That(tooSmall.Context.SchemaId, Is.EqualTo(9));
            Assert.That(tooSmall.Context.TemplateId, Is.EqualTo(8));
            Assert.That(tooSmall.Context.Version, Is.EqualTo(7));
            Assert.That(tooSmall.Context.MsgType, Is.EqualTo("Z"));
        });
    }

    private class ExampleBinaryCodec : IBinaryMessageCodec
    {
        public BinaryDecodeResult DecodeBinary(ReadOnlySpan<byte> source, BinaryMessageContext context)
        {
            BinaryMessageContext effective = CopyContext(context);
            if (source.Length < 7)
            {
                return new BinaryDecodeResult { Status = DecodeStatus.Incomplete, Required = 7, Context = effective };
            }

            effective.SchemaId = source[0] | source[1] << 8;
            effective.TemplateId = source[2] | source[3] << 8;
            effective.Version = source[4] | source[5] << 8;
            effective.MsgType = Encoding.ASCII.GetString(source.Slice(6, 1));
            return new BinaryDecodeResult
            {
                Status = DecodeStatus.Success,
                Message = new FixMessage { MsgType = effective.MsgType },
                Context = effective,
                Consumed = 7,
                Required = 7
            };
        }

        public BinaryEncodeResult EncodeBinary(FixMessage message, BinaryMessageContext context, Span<byte> destination)
        {
            BinaryMessageContext effective = CopyContext(context);
            effective.MsgType = message.MsgType;
            if (destination.Length < 7)
            {
                return new BinaryEncodeResult { Status = EncodeStatus.DestinationTooSmall, RequiredSize = 7, Context = effective };
            }

            destination[0] = (byte)effective.SchemaId;
            destination[1] = (byte)(effective.SchemaId >> 8);
            destination[2] = (byte)effective.TemplateId;
            destination[3] = (byte)(effective.TemplateId >> 8);
            destination[4] = (byte)effective.Version;
            destination[5] = (byte)(effective.Version >> 8);
            destination[6] = Encoding.ASCII.GetBytes(effective.MsgType)[0];
            return new BinaryEncodeResult
            {
                Status = EncodeStatus.Success,
                Context = effective,
                BytesWritten = 7,
                RequiredSize = 7
            };
        }

        private static BinaryMessageContext CopyContext(BinaryMessageContext source)
        {
            return new BinaryMessageContext
            {
                EncodingType = source.EncodingType,
                EncodingIdentifier = source.EncodingIdentifier,
                TemplateId = source.TemplateId,
                SchemaId = source.SchemaId,
                Version = source.Version,
                MsgType = source.MsgType,
                DictionaryIdentity = source.DictionaryIdentity
            };
        }
    }
}
