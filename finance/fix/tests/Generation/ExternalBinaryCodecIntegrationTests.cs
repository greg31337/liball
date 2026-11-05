using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class ExternalBinaryCodecIntegrationTests
{
    [TestCase(FixEncodingTypes.SbeVersion20BigEndian, false)]
    [TestCase(FixEncodingTypes.SbeVersion20LittleEndian, true)]
    public void ExternalCodecRoundTripsCanonicalMessageThroughSofh(ushort encodingIdentifier, bool littleEndian)
    {
        FakeExternalBinaryCodec codec = new FakeExternalBinaryCodec();
        FixDictionaryIdentity identity = new FixDictionaryIdentity
        {
            Name = "Venue",
            BeginString = FixBeginStrings.Fix44,
            ApplicationVersion = FixApplicationVersions.FixLatest,
            Scenario = "base"
        };
        BinaryMessageContext requested = Context(encodingIdentifier, 42, 101, 7, identity);
        FixMessage original = new FixMessage
        {
            BeginString = FixBeginStrings.Fix44,
            MsgType = "D",
            BodyFields = [Field(11, "ORDER-1"), Field(54, "1")]
        };

        byte[] frameBytes = EncodeFrame(codec, original, requested, littleEndian);
        FrameResult frame = new SofhMessageFramer().ReadFrame(frameBytes, new FrameOptions());
        BinaryMessageContext framedContext = new BinaryMessageContext
        {
            EncodingType = BinaryEncodingType.Sbe,
            EncodingIdentifier = frame.EncodingType,
            DictionaryIdentity = identity
        };
        byte[] payload = frameBytes.AsSpan(frame.FrameOffset + FixDefaults.SofhHeaderSize, frame.FrameLength - FixDefaults.SofhHeaderSize).ToArray();
        BinaryDecodeResult decoded = codec.DecodeBinary(payload, framedContext);
        byte[] reencodedPayload = EncodePayload(codec, decoded.Message!, decoded.Context);

        Assert.Multiple(() =>
        {
            Assert.That(frame.Status, Is.EqualTo(FrameStatus.Complete), frame.ErrorCode);
            Assert.That(frame.IsSofh, Is.True);
            Assert.That(frame.EncodingType, Is.EqualTo(encodingIdentifier));
            Assert.That(frame.FrameLength, Is.EqualTo(frameBytes.Length));
            Assert.That(frame.Consumed, Is.EqualTo(frameBytes.Length));
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), decoded.ErrorCode);
            Assert.That(decoded.Consumed, Is.EqualTo(payload.Length));
            Assert.That(decoded.Required, Is.EqualTo(payload.Length));
            Assert.That(decoded.Context.EncodingType, Is.EqualTo(BinaryEncodingType.Sbe));
            Assert.That(decoded.Context.EncodingIdentifier, Is.EqualTo(encodingIdentifier));
            Assert.That(decoded.Context.SchemaId, Is.EqualTo(42));
            Assert.That(decoded.Context.TemplateId, Is.EqualTo(101));
            Assert.That(decoded.Context.Version, Is.EqualTo(7));
            Assert.That(decoded.Context.MsgType, Is.EqualTo("D"));
            Assert.That(decoded.Context.DictionaryIdentity, Is.SameAs(identity));
            Assert.That(decoded.Message!.BeginString, Is.EqualTo(FixBeginStrings.Fix44));
            Assert.That(decoded.Message.MsgType, Is.EqualTo("D"));
            Assert.That(decoded.Message.BodyFields.Select(item => item.Tag), Is.EqualTo(new[] { 11, 54 }));
            Assert.That(Text(decoded.Message.BodyFields[0]), Is.EqualTo("ORDER-1"));
            Assert.That(Text(decoded.Message.BodyFields[1]), Is.EqualTo("1"));
            Assert.That(decoded.Message.BodyFields[0].Value, Is.Not.SameAs(original.BodyFields[0].Value));
            Assert.That(reencodedPayload, Is.EqualTo(payload));
            Assert.That(requested.MsgType, Is.Empty);
            Assert.That(requested.DictionaryIdentity, Is.SameAs(identity));
        });
    }

    [Test]
    public void ConcatenatedSofhFramesMapToIndependentCanonicalMessages()
    {
        FakeExternalBinaryCodec codec = new FakeExternalBinaryCodec();
        FixDictionaryIdentity orderIdentity = new FixDictionaryIdentity { Name = "Orders", BeginString = FixBeginStrings.Fix44 };
        FixDictionaryIdentity reportIdentity = new FixDictionaryIdentity { Name = "Reports", ApplicationVersion = FixApplicationVersions.FixLatest };
        byte[] orderFrame = EncodeFrame(
            codec,
            new FixMessage { BeginString = FixBeginStrings.Fix44, MsgType = "D", BodyFields = [Field(11, "ORDER-2")] },
            Context(FixEncodingTypes.SbeVersion20BigEndian, 42, 101, 7, orderIdentity),
            false);
        byte[] reportFrame = EncodeFrame(
            codec,
            new FixMessage { MsgType = "8", BodyFields = [Field(17, "EXEC-1")] },
            Context(FixEncodingTypes.SbeVersion20LittleEndian, 43, 102, 8, reportIdentity),
            true);
        byte[] source = Combine(orderFrame, reportFrame);
        SofhMessageFramer framer = new SofhMessageFramer();

        FrameResult firstFrame = framer.ReadFrame(source, new FrameOptions());
        BinaryDecodeResult first = DecodeFrame(codec, source, firstFrame, orderIdentity);
        ReadOnlySpan<byte> remaining = source.AsSpan(firstFrame.Consumed);
        FrameResult secondFrame = framer.ReadFrame(remaining, new FrameOptions());
        BinaryDecodeResult second = DecodeFrame(codec, remaining, secondFrame, reportIdentity);

        Assert.Multiple(() =>
        {
            Assert.That(firstFrame.Status, Is.EqualTo(FrameStatus.Complete));
            Assert.That(firstFrame.Consumed, Is.EqualTo(orderFrame.Length));
            Assert.That(first.Context.EncodingIdentifier, Is.EqualTo(FixEncodingTypes.SbeVersion20BigEndian));
            Assert.That(first.Context.SchemaId, Is.EqualTo(42));
            Assert.That(first.Context.TemplateId, Is.EqualTo(101));
            Assert.That(first.Context.DictionaryIdentity, Is.SameAs(orderIdentity));
            Assert.That(first.Message!.MsgType, Is.EqualTo("D"));
            Assert.That(Text(first.Message.BodyFields.Single()), Is.EqualTo("ORDER-2"));
            Assert.That(secondFrame.Status, Is.EqualTo(FrameStatus.Complete));
            Assert.That(secondFrame.Consumed, Is.EqualTo(reportFrame.Length));
            Assert.That(second.Context.EncodingIdentifier, Is.EqualTo(FixEncodingTypes.SbeVersion20LittleEndian));
            Assert.That(second.Context.SchemaId, Is.EqualTo(43));
            Assert.That(second.Context.TemplateId, Is.EqualTo(102));
            Assert.That(second.Context.DictionaryIdentity, Is.SameAs(reportIdentity));
            Assert.That(second.Message!.MsgType, Is.EqualTo("8"));
            Assert.That(Text(second.Message.BodyFields.Single()), Is.EqualTo("EXEC-1"));
            Assert.That(firstFrame.Consumed + secondFrame.Consumed, Is.EqualTo(source.Length));
            Assert.That(codec.DecodeCalls, Is.EqualTo(2));
        });
    }

    [Test]
    public void IncompleteFrameAndPayloadReportExactBoundariesWithoutMutation()
    {
        FakeExternalBinaryCodec codec = new FakeExternalBinaryCodec();
        FixDictionaryIdentity identity = new FixDictionaryIdentity { Name = "Venue" };
        BinaryMessageContext context = Context(FixEncodingTypes.SbeVersion20LittleEndian, 42, 101, 7, identity);
        FixMessage message = new FixMessage { MsgType = "D", BodyFields = [Field(11, "ORDER-3")] };
        byte[] frameBytes = EncodeFrame(codec, message, context, true);
        byte[] truncatedFrame = frameBytes.AsSpan(0, frameBytes.Length - 1).ToArray();

        FrameResult frame = new SofhMessageFramer().ReadFrame(truncatedFrame, new FrameOptions());
        ReadOnlySpan<byte> truncatedPayload = truncatedFrame.AsSpan(FixDefaults.SofhHeaderSize);
        BinaryDecodeResult decoded = codec.DecodeBinary(truncatedPayload, new BinaryMessageContext
        {
            EncodingType = BinaryEncodingType.Sbe,
            EncodingIdentifier = FixEncodingTypes.SbeVersion20LittleEndian,
            DictionaryIdentity = identity
        });
        byte[] tooSmall = new byte[decoded.Required - 1];
        Array.Fill(tooSmall, (byte)88);
        BinaryEncodeResult encoded = codec.EncodeBinary(message, context, tooSmall);

        Assert.Multiple(() =>
        {
            Assert.That(frame.Status, Is.EqualTo(FrameStatus.Incomplete));
            Assert.That(frame.Consumed, Is.Zero);
            Assert.That(frame.Required, Is.EqualTo(frameBytes.Length));
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Incomplete));
            Assert.That(decoded.Consumed, Is.Zero);
            Assert.That(decoded.Required, Is.EqualTo(frameBytes.Length - FixDefaults.SofhHeaderSize));
            Assert.That(decoded.Message, Is.Null);
            Assert.That(encoded.Status, Is.EqualTo(EncodeStatus.DestinationTooSmall));
            Assert.That(encoded.BytesWritten, Is.Zero);
            Assert.That(encoded.RequiredSize, Is.EqualTo(decoded.Required));
            Assert.That(tooSmall, Is.All.EqualTo(88));
            Assert.That(context.MsgType, Is.Empty);
            Assert.That(context.DictionaryIdentity, Is.SameAs(identity));
        });
    }

    private static BinaryDecodeResult DecodeFrame(FakeExternalBinaryCodec codec, ReadOnlySpan<byte> source, FrameResult frame, FixDictionaryIdentity identity)
    {
        BinaryMessageContext context = new BinaryMessageContext
        {
            EncodingType = BinaryEncodingType.Sbe,
            EncodingIdentifier = frame.EncodingType,
            DictionaryIdentity = identity
        };
        return codec.DecodeBinary(source.Slice(frame.FrameOffset + FixDefaults.SofhHeaderSize, frame.FrameLength - FixDefaults.SofhHeaderSize), context);
    }

    private static byte[] EncodeFrame(FakeExternalBinaryCodec codec, FixMessage message, BinaryMessageContext context, bool littleEndian)
    {
        byte[] payload = EncodePayload(codec, message, context);
        byte[] header = new byte[FixDefaults.SofhHeaderSize];
        WriteSofhHeader(header, payload.Length + header.Length, context.EncodingIdentifier, littleEndian);
        return Combine(header, payload);
    }

    private static byte[] EncodePayload(FakeExternalBinaryCodec codec, FixMessage message, BinaryMessageContext context)
    {
        BinaryEncodeResult measured = codec.EncodeBinary(message, context, Span<byte>.Empty);
        Assert.That(measured.Status, Is.EqualTo(EncodeStatus.DestinationTooSmall), measured.ErrorCode);
        byte[] payload = new byte[measured.RequiredSize];
        BinaryEncodeResult encoded = codec.EncodeBinary(message, context, payload);
        Assert.That(encoded.Status, Is.EqualTo(EncodeStatus.Success), encoded.ErrorCode);
        Assert.That(encoded.BytesWritten, Is.EqualTo(payload.Length));
        return payload;
    }

    private static void WriteSofhHeader(Span<byte> destination, int length, ushort encodingIdentifier, bool littleEndian)
    {
        if (littleEndian)
        {
            destination[0] = (byte)length;
            destination[1] = (byte)(length >> 8);
            destination[2] = (byte)(length >> 16);
            destination[3] = (byte)(length >> 24);
            destination[4] = (byte)encodingIdentifier;
            destination[5] = (byte)(encodingIdentifier >> 8);
        }
        else
        {
            destination[0] = (byte)(length >> 24);
            destination[1] = (byte)(length >> 16);
            destination[2] = (byte)(length >> 8);
            destination[3] = (byte)length;
            destination[4] = (byte)(encodingIdentifier >> 8);
            destination[5] = (byte)encodingIdentifier;
        }
    }

    private static BinaryMessageContext Context(ushort encodingIdentifier, int schemaId, int templateId, int version, FixDictionaryIdentity identity)
    {
        return new BinaryMessageContext
        {
            EncodingType = BinaryEncodingType.Sbe,
            EncodingIdentifier = encodingIdentifier,
            SchemaId = schemaId,
            TemplateId = templateId,
            Version = version,
            DictionaryIdentity = identity
        };
    }

    private static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.ASCII.GetBytes(value) };
    }

    private static string Text(FixField field)
    {
        return Encoding.ASCII.GetString(field.Value);
    }

    private static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] combined = new byte[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }

    private class FakeExternalBinaryCodec : IBinaryMessageCodec
    {
        public int DecodeCalls;

        public BinaryDecodeResult DecodeBinary(ReadOnlySpan<byte> source, BinaryMessageContext context)
        {
            DecodeCalls++;
            BinaryMessageContext effective = CopyContext(context);
            effective.EncodingType = BinaryEncodingType.Sbe;
            bool littleEndian = IsLittleEndian(effective.EncodingIdentifier);
            if (source.Length < 8) return IncompleteDecode(effective, 8);

            effective.SchemaId = ReadUInt16(source, 0, littleEndian);
            effective.TemplateId = ReadUInt16(source, 2, littleEndian);
            effective.Version = ReadUInt16(source, 4, littleEndian);
            effective.MsgType = Encoding.ASCII.GetString(source.Slice(6, 1));
            int fieldCount = source[7];
            int offset = 8;
            List<FixField> fields = [];
            for (int index = 0; index < fieldCount; index++)
            {
                if (source.Length < offset + 3) return IncompleteDecode(effective, offset + 3);
                int tag = ReadUInt16(source, offset, littleEndian);
                int length = source[offset + 2];
                if (source.Length < offset + 3 + length) return IncompleteDecode(effective, offset + 3 + length);
                fields.Add(new FixField
                {
                    Tag = tag,
                    Value = source.Slice(offset + 3, length).ToArray(),
                    WireIndex = index
                });
                offset += 3 + length;
            }

            FixMessage message = new FixMessage
            {
                BeginString = effective.DictionaryIdentity.BeginString,
                MsgType = effective.MsgType,
                BodyFields = fields,
                OriginalBytes = source.Slice(0, offset).ToArray()
            };
            return new BinaryDecodeResult
            {
                Status = DecodeStatus.Success,
                Message = message,
                Context = effective,
                Consumed = offset,
                Required = offset
            };
        }

        public BinaryEncodeResult EncodeBinary(FixMessage message, BinaryMessageContext context, Span<byte> destination)
        {
            BinaryMessageContext effective = CopyContext(context);
            effective.EncodingType = BinaryEncodingType.Sbe;
            effective.MsgType = message.MsgType;
            int required = RequiredSize(message);
            if (!ValidMessage(message) || !ValidMetadata(effective))
            {
                return new BinaryEncodeResult
                {
                    Status = EncodeStatus.Invalid,
                    Context = effective,
                    RequiredSize = required,
                    ErrorCode = "invalid-fake-binary-message"
                };
            }
            if (destination.Length < required)
            {
                return new BinaryEncodeResult
                {
                    Status = EncodeStatus.DestinationTooSmall,
                    Context = effective,
                    RequiredSize = required,
                    ErrorCode = "destination-too-small"
                };
            }

            bool littleEndian = IsLittleEndian(effective.EncodingIdentifier);
            WriteUInt16(destination, 0, effective.SchemaId, littleEndian);
            WriteUInt16(destination, 2, effective.TemplateId, littleEndian);
            WriteUInt16(destination, 4, effective.Version, littleEndian);
            destination[6] = Encoding.ASCII.GetBytes(message.MsgType)[0];
            destination[7] = (byte)message.BodyFields.Count;
            int offset = 8;
            for (int index = 0; index < message.BodyFields.Count; index++)
            {
                FixField field = message.BodyFields[index];
                WriteUInt16(destination, offset, field.Tag, littleEndian);
                destination[offset + 2] = (byte)field.Value.Length;
                field.Value.CopyTo(destination.Slice(offset + 3));
                offset += 3 + field.Value.Length;
            }
            return new BinaryEncodeResult
            {
                Status = EncodeStatus.Success,
                Context = effective,
                BytesWritten = required,
                RequiredSize = required
            };
        }

        private static BinaryDecodeResult IncompleteDecode(BinaryMessageContext context, int required)
        {
            return new BinaryDecodeResult
            {
                Status = DecodeStatus.Incomplete,
                Context = context,
                Required = required,
                ErrorCode = "incomplete"
            };
        }

        private static int RequiredSize(FixMessage message)
        {
            int required = 8;
            for (int index = 0; index < message.BodyFields.Count; index++) required += 3 + message.BodyFields[index].Value.Length;
            return required;
        }

        private static bool ValidMessage(FixMessage message)
        {
            if (message.MsgType.Length != 1 || message.BodyFields.Count > byte.MaxValue) return false;
            for (int index = 0; index < message.BodyFields.Count; index++)
            {
                if (message.BodyFields[index].Tag <= 0 || message.BodyFields[index].Tag > ushort.MaxValue || message.BodyFields[index].Value.Length > byte.MaxValue) return false;
            }
            return true;
        }

        private static bool ValidMetadata(BinaryMessageContext context)
        {
            bool knownEncoding = context.EncodingIdentifier == FixEncodingTypes.SbeVersion20BigEndian || context.EncodingIdentifier == FixEncodingTypes.SbeVersion20LittleEndian;
            return knownEncoding && context.SchemaId >= 0 && context.SchemaId <= ushort.MaxValue && context.TemplateId >= 0 && context.TemplateId <= ushort.MaxValue && context.Version >= 0 && context.Version <= ushort.MaxValue;
        }

        private static bool IsLittleEndian(ushort encodingIdentifier)
        {
            return encodingIdentifier == FixEncodingTypes.SbeVersion20LittleEndian;
        }

        private static int ReadUInt16(ReadOnlySpan<byte> source, int offset, bool littleEndian)
        {
            return littleEndian ? source[offset] | source[offset + 1] << 8 : source[offset] << 8 | source[offset + 1];
        }

        private static void WriteUInt16(Span<byte> destination, int offset, int value, bool littleEndian)
        {
            if (littleEndian)
            {
                destination[offset] = (byte)value;
                destination[offset + 1] = (byte)(value >> 8);
            }
            else
            {
                destination[offset] = (byte)(value >> 8);
                destination[offset + 1] = (byte)value;
            }
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
