using System.Text;
using rtp;

namespace rtp.Tests.Wire;

public class RtpKnownHeaderExtensionBoundaryTests
{
    [Test]
    public void EveryClientAudioLevelByteRoundTripsExactly()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        int encodedValue = 0;
        while (encodedValue <= byte.MaxValue)
        {
            byte[] source = new byte[] { (byte)encodedValue };
            RtpAudioLevelExtension decoded = codec.DecodeAudioLevelExtension(source);
            byte[] destination = new byte[1];
            int written = codec.EncodeAudioLevelExtension(decoded, destination);

            Assert.Multiple(() =>
            {
                Assert.That(written, Is.EqualTo(1));
                Assert.That(decoded.VoiceActivity, Is.EqualTo(encodedValue >= 128));
                Assert.That(decoded.Level, Is.EqualTo(encodedValue & 127));
                Assert.That(destination, Is.EqualTo(source));
            });
            encodedValue = encodedValue + 1;
        }
    }

    [Test]
    public void ClientAudioLevelRejectsEveryOutOfRangeModelValue()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        int level = 128;
        while (level <= byte.MaxValue)
        {
            RtpAudioLevelExtension value = new RtpAudioLevelExtension();
            value.Level = (byte)level;
            byte[] destination = new byte[] { 0xA5 };

            RtpCodecResult result = codec.TryEncodeAudioLevelExtension(
                value,
                destination);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.WrittenCount, Is.Zero);
                Assert.That(result.ErrorMessage, Does.Contain("0 and 127"));
                Assert.That(destination, Is.EqualTo(new byte[] { 0xA5 }));
            });
            level = level + 1;
        }
    }

    [Test]
    public void ClientAudioLevelRequiresOneSourceAndDestinationByte()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        RtpAudioLevelExtension value = new RtpAudioLevelExtension();

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryDecodeAudioLevelExtension([]).Success, Is.False);
            Assert.That(codec.TryDecodeAudioLevelExtension(new byte[2]).Success, Is.False);
            Assert.That(codec.TryEncodeAudioLevelExtension(value, []).Success, Is.False);
            Assert.Throws<RtpBufferCapacityException>(() =>
                codec.EncodeAudioLevelExtension(value, []));
            Assert.Throws<RtpPacketFormatException>(() =>
                codec.GetAudioLevelExtensionEncodedSize(null!));
        });
    }

    [Test]
    public void MixerAudioLevelRoundTripsEveryLegalCountAndBoundaryValue()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        int count = 1;
        while (count <= 15)
        {
            byte[] levels = new byte[count];
            int levelIndex = 0;
            while (levelIndex < levels.Length)
            {
                levels[levelIndex] = (byte)(levelIndex * 127 / (count - 1 == 0 ? 1 : count - 1));
                levelIndex = levelIndex + 1;
            }

            levels[0] = 0;
            levels[levels.Length - 1] = 127;
            RtpMixerAudioLevelExtension value = new RtpMixerAudioLevelExtension();
            value.Levels = levels;
            byte[] destination = new byte[count];

            int written = codec.EncodeMixerAudioLevelExtension(value, destination);
            RtpCodecResult decoded = codec.TryDecodeMixerAudioLevelExtension(destination);
            destination[0] = 99;

            Assert.Multiple(() =>
            {
                Assert.That(codec.GetMixerAudioLevelExtensionEncodedSize(value),
                    Is.EqualTo(count));
                Assert.That(written, Is.EqualTo(count));
                Assert.That(decoded.Success, Is.True);
                Assert.That(decoded.ConsumedCount, Is.EqualTo(count));
                Assert.That(decoded.MixerAudioLevelExtension?.Levels,
                    Is.EqualTo(levels));
            });
            count = count + 1;
        }
    }

    [Test]
    public void MixerAudioLevelAcceptsAllSevenBitValuesAndRejectsReservedBits()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        int encodedValue = 0;
        while (encodedValue <= byte.MaxValue)
        {
            byte[] source = new byte[] { (byte)encodedValue };
            RtpCodecResult decodeResult =
                codec.TryDecodeMixerAudioLevelExtension(source);
            RtpMixerAudioLevelExtension model = new RtpMixerAudioLevelExtension();
            model.Levels = source;
            byte[] destination = new byte[] { 0xA5 };
            RtpCodecResult encodeResult = codec.TryEncodeMixerAudioLevelExtension(
                model,
                destination);
            bool valid = encodedValue <= 127;

            Assert.Multiple(() =>
            {
                Assert.That(decodeResult.Success, Is.EqualTo(valid));
                Assert.That(encodeResult.Success, Is.EqualTo(valid));
                if (valid)
                {
                    Assert.That(decodeResult.MixerAudioLevelExtension?.Levels,
                        Is.EqualTo(source));
                    Assert.That(destination, Is.EqualTo(source));
                }
                else
                {
                    Assert.That(decodeResult.ErrorMessage, Does.Contain("reserved bit"));
                    Assert.That(encodeResult.ErrorMessage, Does.Contain("0 and 127"));
                    Assert.That(destination, Is.EqualTo(new byte[] { 0xA5 }));
                }
            });
            encodedValue = encodedValue + 1;
        }
    }

    [Test]
    public void MixerAudioLevelRejectsInvalidCountsAndShortDestinations()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        RtpMixerAudioLevelExtension empty = new RtpMixerAudioLevelExtension();
        RtpMixerAudioLevelExtension oversized = new RtpMixerAudioLevelExtension();
        oversized.Levels = new byte[16];
        RtpMixerAudioLevelExtension valid = new RtpMixerAudioLevelExtension();
        valid.Levels = new byte[] { 1, 2 };
        byte[] shortDestination = new byte[] { 0xA5 };

        RtpCodecResult shortResult = codec.TryEncodeMixerAudioLevelExtension(
            valid,
            shortDestination);

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryEncodeMixerAudioLevelExtension(empty, []).Success,
                Is.False);
            Assert.That(codec.TryEncodeMixerAudioLevelExtension(oversized, new byte[16]).Success,
                Is.False);
            Assert.That(codec.TryDecodeMixerAudioLevelExtension([]).Success, Is.False);
            Assert.That(codec.TryDecodeMixerAudioLevelExtension(new byte[16]).Success,
                Is.False);
            Assert.That(shortResult.Success, Is.False);
            Assert.That(shortDestination, Is.EqualTo(new byte[] { 0xA5 }));
            Assert.Throws<RtpBufferCapacityException>(() =>
                codec.EncodeMixerAudioLevelExtension(valid, shortDestination));
        });
    }

    [Test]
    public void Ntp64BoundaryValuesUseNetworkByteOrder()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        ulong[] timestamps = new ulong[]
        {
            0,
            1,
            0x00000000FFFFFFFFUL,
            0xFFFFFFFF00000000UL,
            ulong.MaxValue
        };
        int timestampIndex = 0;
        while (timestampIndex < timestamps.Length)
        {
            ulong timestamp = timestamps[timestampIndex];
            RtpNtp64Extension value = new RtpNtp64Extension();
            value.Timestamp = timestamp;
            byte[] destination = new byte[8];

            int written = codec.EncodeNtp64Extension(value, destination);
            RtpNtp64Extension decoded = codec.DecodeNtp64Extension(destination);

            Assert.That(written, Is.EqualTo(8));
            Assert.That(decoded.Timestamp, Is.EqualTo(timestamp));
            int byteIndex = 0;
            while (byteIndex < destination.Length)
            {
                int shift = (7 - byteIndex) * 8;
                Assert.That(destination[byteIndex],
                    Is.EqualTo((byte)(timestamp >> shift)),
                    $"timestamp {timestamp:X16}, byte {byteIndex}");
                byteIndex = byteIndex + 1;
            }

            timestampIndex = timestampIndex + 1;
        }
    }

    [Test]
    public void Ntp64RejectsEveryNonEightBytePayloadAndShortDestination()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        int length = 0;
        while (length <= 16)
        {
            if (length != 8)
            {
                RtpCodecResult result = codec.TryDecodeNtp64Extension(new byte[length]);
                Assert.Multiple(() =>
                {
                    Assert.That(result.Success, Is.False);
                    Assert.That(result.ConsumedCount, Is.Zero);
                    Assert.That(result.ErrorMessage, Does.Contain("exactly eight"));
                });
            }

            length = length + 1;
        }

        length = 0;
        while (length < 8)
        {
            byte[] destination = new byte[length];
            Array.Fill(destination, (byte)0xA5);
            byte[] original = (byte[])destination.Clone();
            RtpCodecResult result = codec.TryEncodeNtp64Extension(
                new RtpNtp64Extension(),
                destination);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.WrittenCount, Is.Zero);
                Assert.That(destination, Is.EqualTo(original));
            });
            length = length + 1;
        }
    }

    [Test]
    public void MidRoundTripsUtf8AtOneAndTwoByteFormatLengthBoundaries()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        string[] values = new string[]
        {
            "A",
            new string('a', 16),
            new string('b', 17),
            string.Concat(Enumerable.Repeat("界", 85))
        };
        int valueIndex = 0;
        while (valueIndex < values.Length)
        {
            string valueText = values[valueIndex];
            RtpStreamIdentifierExtension value = CreateStreamIdentifier(
                RtpHeaderExtensionUris.MediaIdentifier,
                valueText);
            int expectedSize = Encoding.UTF8.GetByteCount(valueText);
            byte[] destination = new byte[expectedSize];

            int written = codec.EncodeStreamIdentifierExtension(value, destination);
            RtpStreamIdentifierExtension decoded =
                codec.DecodeStreamIdentifierExtension(
                    destination,
                    RtpHeaderExtensionUris.MediaIdentifier);

            Assert.Multiple(() =>
            {
                Assert.That(expectedSize, Is.LessThanOrEqualTo(255));
                Assert.That(codec.GetStreamIdentifierExtensionEncodedSize(value),
                    Is.EqualTo(expectedSize));
                Assert.That(written, Is.EqualTo(expectedSize));
                Assert.That(destination, Is.EqualTo(Encoding.UTF8.GetBytes(valueText)));
                Assert.That(decoded.Uri, Is.EqualTo(RtpHeaderExtensionUris.MediaIdentifier));
                Assert.That(decoded.Value, Is.EqualTo(valueText));
            });
            valueIndex = valueIndex + 1;
        }
    }

    [Test]
    public void RidAndRepairedRidRoundTripEveryAllowedCharacterAndMaximumLength()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        string allowed = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        string[] uris = new string[]
        {
            RtpHeaderExtensionUris.RtpStreamIdentifier,
            RtpHeaderExtensionUris.RepairedRtpStreamIdentifier
        };
        int uriIndex = 0;
        while (uriIndex < uris.Length)
        {
            string uri = uris[uriIndex];
            string[] values = new string[]
            {
                "0",
                allowed,
                new string('z', 255)
            };
            int valueIndex = 0;
            while (valueIndex < values.Length)
            {
                RtpStreamIdentifierExtension value = CreateStreamIdentifier(
                    uri,
                    values[valueIndex]);
                byte[] destination = new byte[values[valueIndex].Length];

                int written = codec.EncodeStreamIdentifierExtension(value, destination);
                RtpStreamIdentifierExtension decoded =
                    codec.DecodeStreamIdentifierExtension(destination, uri);

                Assert.Multiple(() =>
                {
                    Assert.That(written, Is.EqualTo(values[valueIndex].Length));
                    Assert.That(destination,
                        Is.EqualTo(Encoding.ASCII.GetBytes(values[valueIndex])));
                    Assert.That(decoded.Uri, Is.EqualTo(uri));
                    Assert.That(decoded.Value, Is.EqualTo(values[valueIndex]));
                });
                valueIndex = valueIndex + 1;
            }

            uriIndex = uriIndex + 1;
        }
    }

    [Test]
    public void MidRejectsMalformedUtf8InvalidUtf16AndLengthViolations()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        byte[][] malformedUtf8 = new byte[][]
        {
            new byte[] { 0x80 },
            new byte[] { 0xC0, 0x80 },
            new byte[] { 0xC2 },
            new byte[] { 0xE0, 0x9F, 0x80 },
            new byte[] { 0xED, 0xA0, 0x80 },
            new byte[] { 0xF0, 0x8F, 0x80, 0x80 },
            new byte[] { 0xF4, 0x90, 0x80, 0x80 },
            new byte[] { 0xF5, 0x80, 0x80, 0x80 },
            new byte[] { 0xFF }
        };
        int payloadIndex = 0;
        while (payloadIndex < malformedUtf8.Length)
        {
            RtpCodecResult result = codec.TryDecodeStreamIdentifierExtension(
                malformedUtf8[payloadIndex],
                RtpHeaderExtensionUris.MediaIdentifier);
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("UTF-8"));
            });
            payloadIndex = payloadIndex + 1;
        }

        RtpStreamIdentifierExtension highSurrogate = CreateStreamIdentifier(
            RtpHeaderExtensionUris.MediaIdentifier,
            "\uD800");
        RtpStreamIdentifierExtension lowSurrogate = CreateStreamIdentifier(
            RtpHeaderExtensionUris.MediaIdentifier,
            "\uDC00");
        RtpStreamIdentifierExtension oversized = CreateStreamIdentifier(
            RtpHeaderExtensionUris.MediaIdentifier,
            new string('a', 256));

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryDecodeStreamIdentifierExtension(
                [],
                RtpHeaderExtensionUris.MediaIdentifier).Success, Is.False);
            Assert.That(codec.TryDecodeStreamIdentifierExtension(
                new byte[256],
                RtpHeaderExtensionUris.MediaIdentifier).Success, Is.False);
            Assert.That(codec.TryEncodeStreamIdentifierExtension(
                highSurrogate,
                new byte[4]).Success, Is.False);
            Assert.That(codec.TryEncodeStreamIdentifierExtension(
                lowSurrogate,
                new byte[4]).Success, Is.False);
            Assert.That(codec.TryEncodeStreamIdentifierExtension(
                oversized,
                new byte[256]).Success, Is.False);
        });
    }

    [Test]
    public void RidAndRepairedRidRejectEveryNonAlphanumericByte()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        string[] uris = new string[]
        {
            RtpHeaderExtensionUris.RtpStreamIdentifier,
            RtpHeaderExtensionUris.RepairedRtpStreamIdentifier
        };
        int uriIndex = 0;
        while (uriIndex < uris.Length)
        {
            int encodedValue = 0;
            while (encodedValue <= byte.MaxValue)
            {
                bool valid = IsAsciiAlphaNumeric(encodedValue);
                RtpCodecResult result = codec.TryDecodeStreamIdentifierExtension(
                    new byte[] { (byte)encodedValue },
                    uris[uriIndex]);
                Assert.That(result.Success,
                    Is.EqualTo(valid),
                    $"URI {uris[uriIndex]}, byte {encodedValue}");
                encodedValue = encodedValue + 1;
            }

            uriIndex = uriIndex + 1;
        }
    }

    [Test]
    public void StreamIdentifierEncodingRejectsInvalidUrisValuesAndCapacity()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        RtpStreamIdentifierExtension empty = CreateStreamIdentifier(
            RtpHeaderExtensionUris.MediaIdentifier,
            "");
        RtpStreamIdentifierExtension invalidRid = CreateStreamIdentifier(
            RtpHeaderExtensionUris.RtpStreamIdentifier,
            "not-valid");
        RtpStreamIdentifierExtension unknown = CreateStreamIdentifier(
            "urn:example:unknown",
            "value");
        RtpStreamIdentifierExtension valid = CreateStreamIdentifier(
            RtpHeaderExtensionUris.MediaIdentifier,
            "AB");
        byte[] shortDestination = new byte[] { 0xA5 };

        RtpCodecResult capacityResult = codec.TryEncodeStreamIdentifierExtension(
            valid,
            shortDestination);

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryEncodeStreamIdentifierExtension(empty, []).Success,
                Is.False);
            Assert.That(codec.TryEncodeStreamIdentifierExtension(
                invalidRid,
                new byte[9]).Success, Is.False);
            Assert.That(codec.TryEncodeStreamIdentifierExtension(
                unknown,
                new byte[5]).Success, Is.False);
            Assert.That(codec.TryDecodeStreamIdentifierExtension(
                new byte[] { 1 },
                "").Success, Is.False);
            Assert.That(capacityResult.Success, Is.False);
            Assert.That(shortDestination, Is.EqualTo(new byte[] { 0xA5 }));
            Assert.Throws<RtpBufferCapacityException>(() =>
                codec.EncodeStreamIdentifierExtension(valid, shortDestination));
        });
    }

    [Test]
    public void EveryStreamIdentifierUriEnforcesOneTo255EncodedBytes()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        string[] uris = new string[]
        {
            RtpHeaderExtensionUris.MediaIdentifier,
            RtpHeaderExtensionUris.RtpStreamIdentifier,
            RtpHeaderExtensionUris.RepairedRtpStreamIdentifier
        };
        int uriIndex = 0;
        while (uriIndex < uris.Length)
        {
            RtpStreamIdentifierExtension empty = CreateStreamIdentifier(
                uris[uriIndex],
                "");
            RtpStreamIdentifierExtension maximum = CreateStreamIdentifier(
                uris[uriIndex],
                new string('a', 255));
            RtpStreamIdentifierExtension oversized = CreateStreamIdentifier(
                uris[uriIndex],
                new string('a', 256));

            Assert.Multiple(() =>
            {
                Assert.That(codec.TryEncodeStreamIdentifierExtension(
                    empty,
                    []).Success, Is.False);
                Assert.That(codec.TryEncodeStreamIdentifierExtension(
                    maximum,
                    new byte[255]).Success, Is.True);
                Assert.That(codec.TryEncodeStreamIdentifierExtension(
                    oversized,
                    new byte[256]).Success, Is.False);
                Assert.That(codec.TryDecodeStreamIdentifierExtension(
                    [],
                    uris[uriIndex]).Success, Is.False);
                Assert.That(codec.TryDecodeStreamIdentifierExtension(
                    new byte[256],
                    uris[uriIndex]).Success, Is.False);
            });
            uriIndex = uriIndex + 1;
        }
    }

    [Test]
    public void KnownPayloadModelsRejectNullValuesAndCollections()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        RtpMixerAudioLevelExtension mixer = new RtpMixerAudioLevelExtension();
        mixer.Levels = null!;
        RtpStreamIdentifierExtension nullUri = CreateStreamIdentifier(
            RtpHeaderExtensionUris.MediaIdentifier,
            "value");
        nullUri.Uri = null!;
        RtpStreamIdentifierExtension nullValue = CreateStreamIdentifier(
            RtpHeaderExtensionUris.MediaIdentifier,
            "value");
        nullValue.Value = null!;

        Assert.Multiple(() =>
        {
            Assert.That(codec.TryEncodeAudioLevelExtension(null!, new byte[1]).Success,
                Is.False);
            Assert.That(codec.TryEncodeMixerAudioLevelExtension(null!, new byte[1]).Success,
                Is.False);
            Assert.That(codec.TryEncodeMixerAudioLevelExtension(mixer, new byte[1]).Success,
                Is.False);
            Assert.That(codec.TryEncodeNtp64Extension(null!, new byte[8]).Success,
                Is.False);
            Assert.That(codec.TryEncodeStreamIdentifierExtension(
                null!,
                new byte[1]).Success, Is.False);
            Assert.That(codec.TryEncodeStreamIdentifierExtension(
                nullUri,
                new byte[5]).Success, Is.False);
            Assert.That(codec.TryEncodeStreamIdentifierExtension(
                nullValue,
                new byte[5]).Success, Is.False);
        });
    }

    [Test]
    public void UnknownUrisPreserveOwnedRawPayloadsAtAllLengthBoundaries()
    {
        IRtpKnownHeaderExtensionCodec codec = new RtpKnownHeaderExtensionCodec();
        int[] lengths = new int[] { 0, 1, 255, 256, 1024 };
        int lengthIndex = 0;
        while (lengthIndex < lengths.Length)
        {
            byte[] source = CreateData(lengths[lengthIndex]);
            byte[] expected = (byte[])source.Clone();

            RtpCodecResult result = codec.TryDecodeStreamIdentifierExtension(
                source,
                "urn:example:future-extension");
            if (source.Length > 0)
            {
                source[0] = (byte)(source[0] + 1);
            }

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.ConsumedCount, Is.EqualTo(expected.Length));
                Assert.That(result.StreamIdentifierExtension, Is.Null);
                Assert.That(result.Data, Is.EqualTo(expected));
                Assert.That(result.Data, Is.Not.SameAs(source));
            });
            lengthIndex = lengthIndex + 1;
        }
    }

    [Test]
    public void UnknownProfilesRoundTripAsOwnedOpaqueRtpExtensions()
    {
        IRtpPacketCodec codec = new RtpPacketCodec(new RtpSessionConfiguration());
        ushort[] profiles = new ushort[]
        {
            0x0000,
            0x0FFF,
            0x1010,
            0xBEDF,
            0xFFFF
        };
        int profileIndex = 0;
        while (profileIndex < profiles.Length)
        {
            ushort profile = profiles[profileIndex];
            byte[] source = new byte[]
            {
                0x90, 0x60, 0x12, 0x34,
                0x01, 0x23, 0x45, 0x67,
                0x89, 0xAB, 0xCD, 0xEF,
                (byte)(profile >> 8), (byte)profile, 0, 1,
                0xAA, 0xBB, 0xCC, 0xDD,
                0xEE
            };
            byte[] expected = (byte[])source.Clone();

            RtpPacket decoded = codec.DecodeRtpPacket(source);
            source[16] = 0;
            source[20] = 0;
            byte[] reencoded = codec.EncodeRtpPacketToArray(decoded);

            Assert.Multiple(() =>
            {
                Assert.That(decoded.HeaderExtension?.Profile, Is.EqualTo(profile));
                Assert.That(decoded.HeaderExtension?.Format,
                    Is.EqualTo(RtpHeaderExtensionFormat.Raw));
                Assert.That(decoded.HeaderExtension?.Elements, Is.Empty);
                Assert.That(decoded.HeaderExtension?.Data,
                    Is.EqualTo(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }));
                Assert.That(decoded.Payload, Is.EqualTo(new byte[] { 0xEE }));
                Assert.That(reencoded, Is.EqualTo(expected));
            });
            profileIndex = profileIndex + 1;
        }
    }

    static bool IsAsciiAlphaNumeric(int value)
    {
        return (value >= '0' && value <= '9') ||
            (value >= 'A' && value <= 'Z') ||
            (value >= 'a' && value <= 'z');
    }

    static byte[] CreateData(int length)
    {
        byte[] data = new byte[length];
        int byteIndex = 0;
        while (byteIndex < data.Length)
        {
            data[byteIndex] = (byte)(byteIndex * 37 + 11);
            byteIndex = byteIndex + 1;
        }

        return data;
    }

    static RtpStreamIdentifierExtension CreateStreamIdentifier(string uri, string value)
    {
        RtpStreamIdentifierExtension identifier = new RtpStreamIdentifierExtension();
        identifier.Uri = uri;
        identifier.Value = value;
        return identifier;
    }
}
