using rtp;

namespace rtp.Tests.Wire;

public class RtpOneByteHeaderExtensionBoundaryTests
{
    [Test]
    public void EveryIdentifierAndLengthCombinationRoundTrips()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        int identifier =
            RtpHeaderExtensionProfiles.OneByteIdentifierMinimum;
        while (identifier <=
            RtpHeaderExtensionProfiles.OneByteIdentifierMaximum)
        {
            int dataLength =
                RtpHeaderExtensionProfiles.OneByteDataLengthMinimum;
            while (dataLength <=
                RtpHeaderExtensionProfiles.OneByteDataLengthMaximum)
            {
                byte[] elementData = CreateData(dataLength);
                RtpHeaderExtension extension = CreateExtension(
                    identifier,
                    elementData);
                int unalignedDataLength = 1 + dataLength;
                int paddingLength =
                    (4 - (unalignedDataLength & 3)) & 3;
                int expectedDataLength =
                    unalignedDataLength + paddingLength;
                int expectedSize = 4 + expectedDataLength;

                int size = codec.GetRtpHeaderExtensionEncodedSize(
                    extension);
                byte[] encoded = new byte[size];
                int written = codec.EncodeRtpHeaderExtension(
                    extension,
                    encoded);
                RtpHeaderExtension decoded =
                    codec.DecodeRtpHeaderExtension(encoded);

                Assert.Multiple(() =>
                {
                    Assert.That(size, Is.EqualTo(expectedSize));
                    Assert.That(written, Is.EqualTo(expectedSize));
                    Assert.That(encoded[0..4], Is.EqualTo(new byte[]
                    {
                        0xBE,
                        0xDE,
                        (byte)(expectedDataLength / 4 >> 8),
                        (byte)(expectedDataLength / 4)
                    }));
                    Assert.That(encoded[4], Is.EqualTo((byte)(
                        identifier << 4 | dataLength - 1)));
                    Assert.That(encoded[5..(5 + dataLength)],
                        Is.EqualTo(elementData));
                    Assert.That(encoded[(5 + dataLength)..],
                        Is.All.Zero);
                    Assert.That(decoded.Format,
                        Is.EqualTo(RtpHeaderExtensionFormat.OneByte));
                    Assert.That(decoded.Profile,
                        Is.EqualTo(RtpHeaderExtensionProfiles.OneByte));
                    Assert.That(decoded.Elements, Has.Length.EqualTo(1));
                    Assert.That(decoded.Elements[0].Identifier,
                        Is.EqualTo(identifier));
                    Assert.That(decoded.Elements[0].Data,
                        Is.EqualTo(elementData));
                    Assert.That(codec.EncodeRtpHeaderExtension(
                        decoded,
                        new byte[expectedSize]),
                        Is.EqualTo(expectedSize));
                    Assert.That(EncodeExtensionToArray(codec, decoded),
                        Is.EqualTo(encoded));
                });
                dataLength = dataLength + 1;
            }

            identifier = identifier + 1;
        }
    }

    [Test]
    public void EncoderAlignmentProducesZeroToThreeTrailingPaddingBytes()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        int[] dataLengths = new int[] { 3, 2, 1, 4 };
        int[] expectedPaddingLengths = new int[] { 0, 1, 2, 3 };
        int caseIndex = 0;
        while (caseIndex < dataLengths.Length)
        {
            int dataLength = dataLengths[caseIndex];
            int paddingLength = expectedPaddingLengths[caseIndex];
            RtpHeaderExtension extension = CreateExtension(
                1,
                CreateData(dataLength));
            int expectedSize = 4 + 1 + dataLength + paddingLength;
            byte[] destination = new byte[expectedSize];

            int written = codec.EncodeRtpHeaderExtension(
                extension,
                destination);

            Assert.Multiple(() =>
            {
                Assert.That(written, Is.EqualTo(expectedSize));
                Assert.That(destination[2], Is.Zero);
                Assert.That(destination[3],
                    Is.EqualTo((byte)((expectedSize - 4) / 4)));
                Assert.That(destination[(5 + dataLength)..],
                    Is.All.Zero);
            });
            caseIndex = caseIndex + 1;
        }
    }

    [Test]
    public void DecoderAcceptsPaddingBeforeBetweenAndAfterElements()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        byte[] source = new byte[]
        {
            0xBE, 0xDE, 0, 3,
            0, 0,
            0x10, 0xAA,
            0,
            0x21, 0xBB, 0xCC,
            0, 0, 0, 0
        };

        RtpCodecResult result = codec.TryDecodeRtpHeaderExtension(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ConsumedCount, Is.EqualTo(source.Length));
            Assert.That(result.HeaderExtension?.Elements,
                Has.Length.EqualTo(2));
            Assert.That(result.HeaderExtension?.Elements[0].Identifier,
                Is.EqualTo(1));
            Assert.That(result.HeaderExtension?.Elements[0].Data,
                Is.EqualTo(new byte[] { 0xAA }));
            Assert.That(result.HeaderExtension?.Elements[1].Identifier,
                Is.EqualTo(2));
            Assert.That(result.HeaderExtension?.Elements[1].Data,
                Is.EqualTo(new byte[] { 0xBB, 0xCC }));
            Assert.That(result.HeaderExtension?.Data,
                Is.EqualTo(source[4..]));
        });
    }

    [Test]
    public void EveryReservedIdentifierHeaderTerminatesParsing()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        int lengthBits = 0;
        while (lengthBits <= 15)
        {
            byte[] source = new byte[]
            {
                0xBE, 0xDE, 0, 1,
                (byte)(0xF0 | lengthBits),
                0x01,
                0x1F,
                0xEE
            };

            RtpCodecResult result = codec.TryDecodeRtpHeaderExtension(
                source);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.ConsumedCount,
                    Is.EqualTo(source.Length));
                Assert.That(result.HeaderExtension?.Elements, Is.Empty);
                Assert.That(result.HeaderExtension?.Data,
                    Is.EqualTo(source[4..]));
            });
            lengthBits = lengthBits + 1;
        }
    }

    [Test]
    public void ReservedIdentifierAfterAnElementPreservesOnlyEarlierElements()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        byte[] source = new byte[]
        {
            0xBE, 0xDE, 0, 2,
            0x10, 0xAA,
            0xFF,
            0x20,
            0xBB, 0xCC,
            0x01, 0
        };

        RtpHeaderExtension decoded =
            codec.DecodeRtpHeaderExtension(source);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Elements, Has.Length.EqualTo(1));
            Assert.That(decoded.Elements[0].Identifier, Is.EqualTo(1));
            Assert.That(decoded.Elements[0].Data,
                Is.EqualTo(new byte[] { 0xAA }));
            Assert.That(decoded.Data, Is.EqualTo(source[4..]));
        });
    }

    [Test]
    public void EveryNonzeroPaddingHeaderIsRejected()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        int lengthBits = 1;
        while (lengthBits <= 15)
        {
            byte[] source = new byte[]
            {
                0xBE, 0xDE, 0, 1,
                (byte)lengthBits, 0, 0, 0
            };

            RtpCodecResult result = codec.TryDecodeRtpHeaderExtension(
                source);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ConsumedCount, Is.Zero);
                Assert.That(result.HeaderExtension, Is.Null);
                Assert.That(result.ErrorMessage, Does.Contain("padding"));
            });
            lengthBits = lengthBits + 1;
        }
    }

    [Test]
    public void EveryElementLengthCodeRejectsAHeaderAtTheDataTail()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        int lengthCode = 0;
        while (lengthCode <= 15)
        {
            byte[] source = new byte[]
            {
                0xBE, 0xDE, 0, 1,
                0, 0, 0,
                (byte)(0x10 | lengthCode)
            };

            RtpCodecResult result = codec.TryDecodeRtpHeaderExtension(
                source);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ConsumedCount, Is.Zero);
                Assert.That(result.HeaderExtension, Is.Null);
                Assert.That(result.ErrorMessage,
                    Does.Contain("truncated data"));
                Assert.Throws<RtpPacketFormatException>(() =>
                    codec.DecodeRtpHeaderExtension(source));
            });
            lengthCode = lengthCode + 1;
        }
    }

    [Test]
    public void EncoderRejectsEveryIdentifierJustOutsideTheOneByteRange()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        int[] identifiers = new int[]
        {
            -1,
            RtpHeaderExtensionProfiles.PaddingIdentifier,
            RtpHeaderExtensionProfiles.OneByteReservedIdentifier,
            RtpHeaderExtensionProfiles.OneByteReservedIdentifier + 1,
            int.MaxValue
        };
        int identifierIndex = 0;
        while (identifierIndex < identifiers.Length)
        {
            RtpHeaderExtension extension = CreateExtension(
                identifiers[identifierIndex],
                new byte[] { 1 });
            RtpCodecResult result = codec.TryEncodeRtpHeaderExtension(
                extension,
                new byte[8]);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.WrittenCount, Is.Zero);
                Assert.That(result.ErrorMessage,
                    Does.Contain("between 1 and 14"));
            });
            identifierIndex = identifierIndex + 1;
        }
    }

    [Test]
    public void EncoderRejectsLengthsImmediatelyOutsideTheOneByteRange()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        int[] dataLengths = new int[] { 0, 17 };
        int lengthIndex = 0;
        while (lengthIndex < dataLengths.Length)
        {
            RtpHeaderExtension extension = CreateExtension(
                1,
                new byte[dataLengths[lengthIndex]]);
            byte[] destination = new byte[24];
            Array.Fill(destination, (byte)0xA5);
            byte[] original = (byte[])destination.Clone();

            RtpCodecResult result = codec.TryEncodeRtpHeaderExtension(
                extension,
                destination);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.WrittenCount, Is.Zero);
                Assert.That(result.ErrorMessage,
                    Does.Contain("1 to 16"));
                Assert.That(destination, Is.EqualTo(original));
            });
            lengthIndex = lengthIndex + 1;
        }
    }

    [Test]
    public void EmptyOneByteBlockHasZeroWordsAndRoundTrips()
    {
        IRtpHeaderExtensionCodec codec = new RtpHeaderExtensionCodec();
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Profile = RtpHeaderExtensionProfiles.OneByte;
        extension.Format = RtpHeaderExtensionFormat.OneByte;
        byte[] expected = new byte[] { 0xBE, 0xDE, 0, 0 };
        byte[] destination = new byte[expected.Length];

        int written = codec.EncodeRtpHeaderExtension(
            extension,
            destination);
        RtpHeaderExtension decoded =
            codec.DecodeRtpHeaderExtension(destination);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(expected.Length));
            Assert.That(destination, Is.EqualTo(expected));
            Assert.That(decoded.Elements, Is.Empty);
            Assert.That(decoded.Data, Is.Empty);
            Assert.That(codec.GetRtpHeaderExtensionEncodedSize(decoded),
                Is.EqualTo(expected.Length));
        });
    }

    static byte[] CreateData(int length)
    {
        byte[] data = new byte[length];
        int byteIndex = 0;
        while (byteIndex < data.Length)
        {
            data[byteIndex] = (byte)(byteIndex + 1);
            byteIndex = byteIndex + 1;
        }

        return data;
    }

    static RtpHeaderExtension CreateExtension(
        int identifier,
        byte[] data)
    {
        RtpHeaderExtensionElement element =
            new RtpHeaderExtensionElement();
        element.Identifier = identifier;
        element.Data = data;
        RtpHeaderExtension extension = new RtpHeaderExtension();
        extension.Profile = RtpHeaderExtensionProfiles.OneByte;
        extension.Format = RtpHeaderExtensionFormat.OneByte;
        extension.Elements = new RtpHeaderExtensionElement[] { element };
        return extension;
    }

    static byte[] EncodeExtensionToArray(
        IRtpHeaderExtensionCodec codec,
        RtpHeaderExtension extension)
    {
        byte[] destination = new byte[
            codec.GetRtpHeaderExtensionEncodedSize(extension)];
        codec.EncodeRtpHeaderExtension(extension, destination);
        return destination;
    }
}
