using System.Text;
using websocket;

namespace websocket.Tests;

public class FrameCodecTests
{
    [TestCase(WebSocketOpcode.Continuation, 0)]
    [TestCase(WebSocketOpcode.Text, 1)]
    [TestCase(WebSocketOpcode.Binary, 2)]
    [TestCase(WebSocketOpcode.ReservedData3, 3)]
    [TestCase(WebSocketOpcode.ReservedData4, 4)]
    [TestCase(WebSocketOpcode.ReservedData5, 5)]
    [TestCase(WebSocketOpcode.ReservedData6, 6)]
    [TestCase(WebSocketOpcode.ReservedData7, 7)]
    [TestCase(WebSocketOpcode.Close, 8)]
    [TestCase(WebSocketOpcode.Ping, 9)]
    [TestCase(WebSocketOpcode.Pong, 10)]
    [TestCase(WebSocketOpcode.ReservedControl11, 11)]
    [TestCase(WebSocketOpcode.ReservedControl12, 12)]
    [TestCase(WebSocketOpcode.ReservedControl13, 13)]
    [TestCase(WebSocketOpcode.ReservedControl14, 14)]
    [TestCase(WebSocketOpcode.ReservedControl15, 15)]
    public void EveryRegisteredOpcodeValueDecodesFromItsWireNibble(
        WebSocketOpcode opcode,
        int wireValue)
    {
        WebSocketExtensionNegotiation[] extensions = [];
        if (wireValue == 3 || wireValue == 4 || wireValue == 5 ||
            wireValue == 6 || wireValue == 7 || wireValue > 10)
        {
            extensions =
            [
                new WebSocketExtensionNegotiation
                {
                    Name = "x-opcode",
                    Status = WebSocketExtensionStatus.Negotiated,
                    HasAcceptedConfiguration = true,
                    AcceptedConfiguration = new WebSocketExtensionOffer
                    {
                        Name = "x-opcode"
                    },
                    ClaimedOpcodes = [opcode]
                }
            ];
        }

        var codec = new WebSocketFrameCodec(
            WebSocketRole.Client,
            WebSocketProtocolConstants.BasicPayloadLengthMaximum,
            extensions);

        WebSocketFrameReadResult result = codec.ReadFrame(
            [(byte)(0x80 | wireValue), 0]);

        Assert.Multiple(() =>
        {
            Assert.That(wireValue,
                Is.InRange(
                    WebSocketProtocolConstants.MinimumOpcode,
                    WebSocketProtocolConstants.MaximumOpcode));
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.HasHeader, Is.True);
            Assert.That(result.Header.Opcode, Is.EqualTo(opcode));
            Assert.That((int)result.Header.Opcode, Is.EqualTo(wireValue));
        });
    }

    [Test]
    public void EveryOpcodeAndWireFlagCombinationUsesNegotiatedClaims()
    {
        WebSocketExtensionNegotiation[] claims = [AllFrameClaims()];

        for (int opcode = 0; opcode <= 15; opcode++)
        {
            for (int finalValue = 0; finalValue <= 1; finalValue++)
            {
                for (int reservedBits = 0;
                    reservedBits <= 7;
                    reservedBits++)
                {
                    for (int maskedValue = 0;
                        maskedValue <= 1;
                        maskedValue++)
                    {
                        bool final = finalValue == 1;
                        bool masked = maskedValue == 1;
                        WebSocketRole role = WebSocketRole.Client;
                        byte[] source =
                            [(byte)((finalValue << 7) |
                                (reservedBits << 4) | opcode), 0];
                        if (masked)
                        {
                            role = WebSocketRole.Server;
                            source =
                            [
                                source[0], 0x80,
                                0x11, 0x22, 0x33, 0x44
                            ];
                        }

                        WebSocketFrameCodec codec =
                            new WebSocketFrameCodec(role, 125, claims);
                        WebSocketFrameReadResult result =
                            codec.ReadFrame(source);
                        bool fragmentedControl = opcode >= 8 && !final;
                        string description =
                            $"opcode={opcode}, final={final}, " +
                            $"reserved={reservedBits}, masked={masked}";

                        if (fragmentedControl)
                        {
                            Assert.That(
                                result.Status,
                                Is.EqualTo(
                                    WebSocketParseStatus.ProtocolFailure),
                                description);
                            Assert.That(
                                result.Error.Identifier,
                                Is.EqualTo("frame-control-fragmented"),
                                description);
                        }
                        else
                        {
                            Assert.That(
                                result.Status,
                                Is.EqualTo(WebSocketParseStatus.Complete),
                                description);
                            Assert.That(result.HasHeader, Is.True, description);
                            Assert.That(
                                (int)result.Header.Opcode,
                                Is.EqualTo(opcode),
                                description);
                            Assert.That(
                                result.Header.Final,
                                Is.EqualTo(final),
                                description);
                            Assert.That(
                                result.Header.Rsv1,
                                Is.EqualTo((reservedBits & 4) != 0),
                                description);
                            Assert.That(
                                result.Header.Rsv2,
                                Is.EqualTo((reservedBits & 2) != 0),
                                description);
                            Assert.That(
                                result.Header.Rsv3,
                                Is.EqualTo((reservedBits & 1) != 0),
                                description);
                            Assert.That(
                                result.Header.Masked,
                                Is.EqualTo(masked),
                                description);
                        }
                    }
                }
            }
        }
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(125)]
    [TestCase(126)]
    [TestCase(65535)]
    [TestCase(65536)]
    public void EveryPayloadLengthBoundaryConsumesOnlyOneCompleteFrame(
        int payloadLength)
    {
        WebSocketFrameCodec codec =
            new WebSocketFrameCodec(WebSocketRole.Client, 65536, []);
        byte[] source = CreateUnmaskedFrameWithTrailingBytes(payloadLength);
        int headerLength = HeaderLength(payloadLength);

        WebSocketFrameReadResult result = codec.ReadFrame(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount,
                Is.EqualTo(headerLength + payloadLength));
            Assert.That(result.HasHeader, Is.True);
            Assert.That(result.Header.PayloadLength,
                Is.EqualTo(payloadLength));
            Assert.That(result.PayloadCount, Is.EqualTo(payloadLength));
            Assert.That(result.Payload,
                Is.EqualTo(source.AsSpan(
                    headerLength,
                    payloadLength).ToArray()));
            Assert.That(result.EndOfFrame, Is.True);
            Assert.That(
                source.Length - result.ConsumedCount,
                Is.EqualTo(2));
        });
    }

    [TestCase("827e007d", 4)]
    [TestCase("827f000000000000ffff", 10)]
    [TestCase("827f8000000000000000", 10)]
    public void NonMinimalAndSignedLengthsFailAtThePublicReadBoundary(
        string hexadecimalSource,
        int expectedConsumedCount)
    {
        WebSocketFrameCodec codec =
            new WebSocketFrameCodec(WebSocketRole.Client, int.MaxValue, []);

        WebSocketFrameReadResult result =
            codec.ReadFrame(Convert.FromHexString(hexadecimalSource));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.ConsumedCount,
                Is.EqualTo(expectedConsumedCount));
            Assert.That(result.HasHeader, Is.False);
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Identifier,
                Is.EqualTo("frame-header-invalid"));
            Assert.That(result.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.ProtocolError));
        });
    }

    [Test]
    public void EveryReservedBitAndOpcodeRequiresANegotiatedClaim()
    {
        string[] reservedBitFrames = ["c100", "a100", "9100"];
        string[] reservedBitIdentifiers =
        [
            "frame-rsv1-unclaimed",
            "frame-rsv2-unclaimed",
            "frame-rsv3-unclaimed"
        ];
        for (int index = 0; index < reservedBitFrames.Length; index++)
        {
            WebSocketFrameCodec codec =
                new WebSocketFrameCodec(WebSocketRole.Client, 125, []);
            WebSocketFrameReadResult result = codec.ReadFrame(
                Convert.FromHexString(reservedBitFrames[index]));

            Assert.That(result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Error.Identifier,
                Is.EqualTo(reservedBitIdentifiers[index]));
        }

        int[] reservedOpcodes = [3, 4, 5, 6, 7, 11, 12, 13, 14, 15];
        foreach (int opcode in reservedOpcodes)
        {
            WebSocketFrameCodec codec =
                new WebSocketFrameCodec(WebSocketRole.Client, 125, []);
            WebSocketFrameReadResult result = codec.ReadFrame(
                [(byte)(0x80 | opcode), 0]);

            Assert.That(
                result.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure),
                $"Opcode {opcode}");
            Assert.That(
                result.Error.Identifier,
                Is.EqualTo("frame-opcode-unclaimed"),
                $"Opcode {opcode}");
        }
    }

    [Test]
    public void ControlPayloadAccepts125AndRejects126BeforePayloadInput()
    {
        WebSocketFrameCodec maximumCodec =
            new WebSocketFrameCodec(WebSocketRole.Client, 126, []);
        byte[] maximumFrame = new byte[127];
        maximumFrame[0] = 0x89;
        maximumFrame[1] = 125;
        WebSocketFrameReadResult maximum =
            maximumCodec.ReadFrame(maximumFrame);
        WebSocketFrameCodec oversizedCodec =
            new WebSocketFrameCodec(WebSocketRole.Client, 126, []);
        WebSocketFrameReadResult oversized = oversizedCodec.ReadFrame(
            [0x89, 126, 0, 126, 0xaa]);

        Assert.Multiple(() =>
        {
            Assert.That(maximum.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(maximum.ConsumedCount, Is.EqualTo(127));
            Assert.That(maximum.Header.Opcode,
                Is.EqualTo(WebSocketOpcode.Ping));
            Assert.That(maximum.PayloadCount, Is.EqualTo(125));
            Assert.That(oversized.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(oversized.ConsumedCount, Is.EqualTo(4));
            Assert.That(oversized.Error.Identifier,
                Is.EqualTo("frame-control-too-large"));
        });
    }

    [Test]
    public void ConsecutiveFramesAndTrailingBytesRemainCallerControlled()
    {
        WebSocketFrameCodec codec =
            new WebSocketFrameCodec(WebSocketRole.Client, 125, []);
        byte[] source = [0x82, 0x01, 0xaa, 0x81, 0x00, 0xcc, 0xdd];

        WebSocketFrameReadResult first = codec.ReadFrame(source);
        codec.ResetFrameRead();
        WebSocketFrameReadResult second = codec.ReadFrame(
            source.AsSpan(first.ConsumedCount));
        int consumedCount = first.ConsumedCount + second.ConsumedCount;

        Assert.Multiple(() =>
        {
            Assert.That(first.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(first.ConsumedCount, Is.EqualTo(3));
            Assert.That(first.Header.Opcode,
                Is.EqualTo(WebSocketOpcode.Binary));
            Assert.That(first.Payload, Is.EqualTo(new byte[] { 0xaa }));
            Assert.That(second.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(second.ConsumedCount, Is.EqualTo(2));
            Assert.That(second.Header.Opcode,
                Is.EqualTo(WebSocketOpcode.Text));
            Assert.That(second.Payload, Is.Empty);
            Assert.That(consumedCount, Is.EqualTo(5));
            Assert.That(source.AsSpan(consumedCount).ToArray(),
                Is.EqualTo(new byte[] { 0xcc, 0xdd }));
        });
    }

    [Test]
    public void ConstructorRejectsInvalidRoleLimitAndExtensionMetadata()
    {
        var invalidExtensionArray = new WebSocketExtensionNegotiation[1];
        var firstClaim = new WebSocketExtensionNegotiation
        {
            Status = WebSocketExtensionStatus.Negotiated,
            ClaimsRsv3 = true
        };
        var secondClaim = new WebSocketExtensionNegotiation
        {
            Status = WebSocketExtensionStatus.Negotiated,
            ClaimsRsv3 = true
        };

        Assert.Throws<WebSocketStateException>(() => new WebSocketFrameCodec((WebSocketRole)99, 1, []));
        Assert.Throws<WebSocketLimitException>(() => new WebSocketFrameCodec(WebSocketRole.Client, 0, []));
        Assert.Throws<WebSocketStateException>(() => new WebSocketFrameCodec(WebSocketRole.Client, 1, invalidExtensionArray));
        Assert.Throws<WebSocketStateException>(() => new WebSocketFrameCodec(
            WebSocketRole.Client,
            1,
            [firstClaim, secondClaim]));
    }

    [Test]
    public void WholeUnmaskedFrameReturnsIndependentHeaderAndPayload()
    {
        var codec = new WebSocketFrameCodec(WebSocketRole.Client, 100, []);
        byte[] source = [0x81, 0x05, 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0xaa];

        var result = codec.ReadFrame(source);
        result.Header.Opcode = WebSocketOpcode.Binary;
        result.Payload[0] = 0;
        codec.ResetFrameRead();
        var repeated = codec.ReadFrame(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(7));
            Assert.That(result.HasHeader, Is.True);
            Assert.That(result.EndOfFrame, Is.True);
            Assert.That(repeated.Header.Opcode, Is.EqualTo(WebSocketOpcode.Text));
            Assert.That(Encoding.UTF8.GetString(repeated.Payload), Is.EqualTo("Hello"));
            Assert.That(source[2], Is.EqualTo(0x48));
        });
    }

    [Test]
    public void MaskedFrameReadsAcrossHeaderAndPayloadBoundaries()
    {
        var codec = new WebSocketFrameCodec(WebSocketRole.Server, 100, []);

        var first = codec.ReadFrame(Convert.FromHexString("818537"));
        var second = codec.ReadFrame(Convert.FromHexString("fa213d7f9f"));
        var third = codec.ReadFrame(Convert.FromHexString("4d5158aabb"));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(first.ConsumedCount, Is.EqualTo(3));
            Assert.That(first.HasHeader, Is.False);
            Assert.That(second.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(second.ConsumedCount, Is.EqualTo(5));
            Assert.That(second.HasHeader, Is.True);
            Assert.That(second.Header.MaskingKey, Is.EqualTo(0x37fa213dU));
            Assert.That(Encoding.UTF8.GetString(second.Payload), Is.EqualTo("He"));
            Assert.That(third.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(third.ConsumedCount, Is.EqualTo(3));
            Assert.That(third.HasHeader, Is.False);
            Assert.That(Encoding.UTF8.GetString(third.Payload), Is.EqualTo("llo"));
            Assert.That(third.EndOfFrame, Is.True);
        });
    }

    [Test]
    public void RoleIncorrectMaskingFailsBeforePayloadConsumption()
    {
        var client = new WebSocketFrameCodec(WebSocketRole.Client, 100, []);
        var server = new WebSocketFrameCodec(WebSocketRole.Server, 100, []);

        var masked = client.ReadFrame(Convert.FromHexString("818537fa213d7f9f4d5158"));
        var unmasked = server.ReadFrame(Convert.FromHexString("810548656c6c6f"));

        Assert.Multiple(() =>
        {
            Assert.That(masked.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(masked.ConsumedCount, Is.EqualTo(6));
            Assert.That(masked.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Protocol));
            Assert.That(masked.Error.Identifier, Is.EqualTo("frame-mask-forbidden"));
            Assert.That(masked.Error.HasCloseCode, Is.True);
            Assert.That(masked.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.ProtocolError));
            Assert.That(masked.Error.KnownCloseCode,
                Is.EqualTo(WebSocketCloseCode.ProtocolError));
            Assert.That(unmasked.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(unmasked.ConsumedCount, Is.EqualTo(2));
            Assert.That(unmasked.Error.Kind,
                Is.EqualTo(WebSocketErrorKind.Protocol));
            Assert.That(unmasked.Error.Identifier, Is.EqualTo("frame-mask-required"));
            Assert.That(unmasked.Error.HasCloseCode, Is.True);
            Assert.That(unmasked.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.ProtocolError));
            Assert.That(unmasked.Error.KnownCloseCode,
                Is.EqualTo(WebSocketCloseCode.ProtocolError));
        });
    }

    [Test]
    public void DeclaredLimitFailureDoesNotConsumeOrAllocatePayload()
    {
        var codec = new WebSocketFrameCodec(WebSocketRole.Client, 125, []);

        var result = codec.ReadFrame(Convert.FromHexString("827e007e01"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(result.ConsumedCount, Is.EqualTo(4));
            Assert.That(result.Payload, Is.Empty);
            Assert.That(result.PayloadCount, Is.Zero);
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Limit));
            Assert.That(result.Error.Identifier, Is.EqualTo("frame-payload-too-large"));
            Assert.That(result.Error.HasCloseCode, Is.True);
            Assert.That(result.Error.CloseCode,
                Is.EqualTo(WebSocketCloseCodes.MessageTooBig));
            Assert.That(result.Error.KnownCloseCode,
                Is.EqualTo(WebSocketCloseCode.MessageTooBig));
        });
    }

    [Test]
    public void InvalidLengthFailurePersistsWithIndependentErrorsUntilReset()
    {
        var codec = new WebSocketFrameCodec(WebSocketRole.Client, 1000, []);

        var first = codec.ReadFrame(Convert.FromHexString("827e007d"));
        first.Error.Identifier = "changed";
        var repeated = codec.ReadFrame(new byte[] { 0xaa });
        codec.ResetFrameRead();
        var valid = codec.ReadFrame(Convert.FromHexString("8100"));

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(first.ConsumedCount, Is.EqualTo(4));
            Assert.That(repeated.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(repeated.ConsumedCount, Is.Zero);
            Assert.That(repeated.Error.Identifier, Is.EqualTo("frame-header-invalid"));
            Assert.That(valid.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(valid.Header.Opcode, Is.EqualTo(WebSocketOpcode.Text));
        });
    }

    [Test]
    public void ResetDiscardsPartialHeaderState()
    {
        var codec = new WebSocketFrameCodec(WebSocketRole.Client, 100, []);
        codec.ReadFrame(new byte[] { 0x82 });

        codec.ResetFrameRead();
        var result = codec.ReadFrame(Convert.FromHexString("8100"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(2));
            Assert.That(result.Header.Opcode, Is.EqualTo(WebSocketOpcode.Text));
        });
    }

    [Test]
    public void ExtensionClaimsAreSnapshottedForReading()
    {
        var extension = new WebSocketExtensionNegotiation();
        extension.Status = WebSocketExtensionStatus.Negotiated;
        extension.ClaimsRsv1 = true;
        extension.ClaimedOpcodes = new[] { WebSocketOpcode.ReservedData3 };
        var codec = new WebSocketFrameCodec(WebSocketRole.Client, 100, new[] { extension });
        extension.Status = WebSocketExtensionStatus.Declined;
        extension.ClaimsRsv1 = false;
        extension.ClaimedOpcodes[0] = WebSocketOpcode.Binary;

        var result = codec.ReadFrame(Convert.FromHexString("c300"));

        Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
    }

    [Test]
    public void ExtensionClaimsAreSnapshottedForWriting()
    {
        var extension = new WebSocketExtensionNegotiation();
        extension.Status = WebSocketExtensionStatus.Negotiated;
        extension.ClaimsRsv1 = true;
        extension.ClaimedOpcodes = new[] { WebSocketOpcode.ReservedData3 };
        var codec = new WebSocketFrameCodec(WebSocketRole.Client, 100, new[] { extension });
        extension.Status = WebSocketExtensionStatus.Declined;
        extension.ClaimsRsv1 = false;
        extension.ClaimedOpcodes[0] = WebSocketOpcode.Binary;
        var source = new FrameCodecMaskKeySource();
        var request = CreateRequest(0, true, [], 0, 0, 0, source);
        request.Header.Rsv1 = true;
        request.Header.Opcode = WebSocketOpcode.ReservedData3;
        var destination = new byte[6];

        var produced = codec.WriteFrame(request, destination);

        Assert.Multiple(() =>
        {
            Assert.That(produced, Is.EqualTo(6));
            Assert.That(destination[0], Is.EqualTo(0xc3));
        });
    }

    [Test]
    public void MeasurementValidatesOutboundRoleAndDoesNotAcquireMask()
    {
        var client = new WebSocketFrameCodec(WebSocketRole.Client, 100, []);
        var server = new WebSocketFrameCodec(WebSocketRole.Server, 100, []);
        var source = new FrameCodecMaskKeySource();
        var masked = CreateRequest(5, true, new byte[] { 1, 2 }, 0, 2, 0, source);
        var unmasked = CreateRequest(5, false, new byte[] { 1, 2 }, 0, 2, 0, source);

        var clientLength = client.MeasureFrame(masked);
        var serverLength = server.MeasureFrame(unmasked);

        Assert.Multiple(() =>
        {
            Assert.That(clientLength, Is.EqualTo(11));
            Assert.That(serverLength, Is.EqualTo(7));
            Assert.That(source.CallCount, Is.Zero);
            Assert.Throws<WebSocketProtocolException>(() => client.MeasureFrame(unmasked));
            Assert.Throws<WebSocketProtocolException>(() => server.MeasureFrame(masked));
        });
    }

    [Test]
    public void PartialPublicWritesAdvanceOnlyConsumedPayloadRange()
    {
        var codec = new WebSocketFrameCodec(WebSocketRole.Client, 100, []);
        var source = new FrameCodecMaskKeySource();
        source.NextKey = 0x37fa213dU;
        byte[] payload = [0x48, 0x65, 0x6c, 0x6c, 0x6f];
        var request = CreateRequest(5, true, payload, 0, 5, 0, source);
        request.Header.Opcode = WebSocketOpcode.Text;
        var first = new byte[3];
        var second = new byte[5];
        var third = new byte[3];

        var firstProduced = codec.WriteFrame(request, first);
        var firstOffset = request.PayloadOffset;
        var firstCount = request.PayloadCount;
        var secondProduced = codec.WriteFrame(request, second);
        var secondOffset = request.PayloadOffset;
        var secondCount = request.PayloadCount;
        var thirdProduced = codec.WriteFrame(request, third);
        var combined = first.Concat(second).Concat(third).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(firstProduced, Is.EqualTo(3));
            Assert.That(firstOffset, Is.Zero);
            Assert.That(firstCount, Is.EqualTo(5));
            Assert.That(secondProduced, Is.EqualTo(5));
            Assert.That(secondOffset, Is.EqualTo(2));
            Assert.That(secondCount, Is.EqualTo(3));
            Assert.That(thirdProduced, Is.EqualTo(3));
            Assert.That(request.PayloadOffset, Is.EqualTo(5));
            Assert.That(request.PayloadCount, Is.Zero);
            Assert.That(request.MaskingOffset, Is.EqualTo(5));
            Assert.That(combined, Is.EqualTo(Convert.FromHexString("818537fa213d7f9f4d5158")));
            Assert.That(source.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void WriterAndReaderRoundTripMaskedPayload()
    {
        var writer = new WebSocketFrameCodec(WebSocketRole.Client, 100, []);
        var reader = new WebSocketFrameCodec(WebSocketRole.Server, 100, []);
        var source = new FrameCodecMaskKeySource();
        byte[] payload = [0xaa, 10, 20, 30, 0xbb];
        var request = CreateRequest(3, true, payload, 1, 3, 0, source);
        var encoded = new byte[9];

        var produced = writer.WriteFrame(request, encoded);
        var decoded = reader.ReadFrame(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(produced, Is.EqualTo(9));
            Assert.That(decoded.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(decoded.ConsumedCount, Is.EqualTo(9));
            Assert.That(decoded.Header.Masked, Is.True);
            Assert.That(decoded.Payload, Is.EqualTo(new byte[] { 10, 20, 30 }));
        });
    }

    [Test]
    public void ClientAndServerWritesRoundTripWithRoleCorrectMasking()
    {
        WebSocketFrameCodec clientWriter =
            new WebSocketFrameCodec(WebSocketRole.Client, 100, []);
        WebSocketFrameCodec serverReader =
            new WebSocketFrameCodec(WebSocketRole.Server, 100, []);
        WebSocketFrameCodec serverWriter =
            new WebSocketFrameCodec(WebSocketRole.Server, 100, []);
        WebSocketFrameCodec clientReader =
            new WebSocketFrameCodec(WebSocketRole.Client, 100, []);
        FrameCodecMaskKeySource source = new FrameCodecMaskKeySource();
        source.NextKey = 0x37fa213dU;
        byte[] clientPayload = [0x48, 0x65, 0x6c, 0x6c, 0x6f];
        WebSocketFrameWriteRequest clientRequest = CreateRequest(
            clientPayload.Length,
            true,
            clientPayload,
            0,
            clientPayload.Length,
            0,
            source);
        clientRequest.Header.Opcode = WebSocketOpcode.Text;
        byte[] serverPayload = [0x57, 0x6f, 0x72, 0x6c, 0x64];
        WebSocketFrameWriteRequest serverRequest = CreateRequest(
            serverPayload.Length,
            false,
            serverPayload,
            0,
            serverPayload.Length,
            0,
            source);
        serverRequest.Header.Opcode = WebSocketOpcode.Text;
        byte[] clientWire = new byte[11];
        byte[] serverWire = new byte[7];

        int clientProduced =
            clientWriter.WriteFrame(clientRequest, clientWire);
        int serverProduced =
            serverWriter.WriteFrame(serverRequest, serverWire);
        WebSocketFrameReadResult serverResult =
            serverReader.ReadFrame(clientWire);
        WebSocketFrameReadResult clientResult =
            clientReader.ReadFrame(serverWire);

        Assert.Multiple(() =>
        {
            Assert.That(clientProduced, Is.EqualTo(11));
            Assert.That(clientWire,
                Is.EqualTo(Convert.FromHexString(
                    "818537fa213d7f9f4d5158")));
            Assert.That(serverResult.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(serverResult.Header.Masked, Is.True);
            Assert.That(serverResult.Payload, Is.EqualTo(clientPayload));
            Assert.That(serverProduced, Is.EqualTo(7));
            Assert.That(serverWire,
                Is.EqualTo(Convert.FromHexString("8105576f726c64")));
            Assert.That(clientResult.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(clientResult.Header.Masked, Is.False);
            Assert.That(clientResult.Payload, Is.EqualTo(serverPayload));
            Assert.That(source.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void EveryHeaderFlagAndOpcodeCombinationMeasuresAndWrites()
    {
        WebSocketExtensionNegotiation[] claims = [AllFrameClaims()];

        for (int opcode = 0; opcode <= 15; opcode++)
        {
            for (int finalValue = 0; finalValue <= 1; finalValue++)
            {
                for (int reservedBits = 0;
                    reservedBits <= 7;
                    reservedBits++)
                {
                    for (int maskedValue = 0;
                        maskedValue <= 1;
                        maskedValue++)
                    {
                        bool final = finalValue == 1;
                        bool masked = maskedValue == 1;
                        WebSocketRole writerRole = WebSocketRole.Server;
                        WebSocketRole readerRole = WebSocketRole.Client;
                        if (masked)
                        {
                            writerRole = WebSocketRole.Client;
                            readerRole = WebSocketRole.Server;
                        }

                        FrameCodecMaskKeySource source =
                            new FrameCodecMaskKeySource();
                        WebSocketFrameWriteRequest request = CreateRequest(
                            0,
                            masked,
                            [],
                            0,
                            0,
                            0,
                            source);
                        request.Header.Final = final;
                        request.Header.Rsv1 =
                            (reservedBits & 4) != 0;
                        request.Header.Rsv2 =
                            (reservedBits & 2) != 0;
                        request.Header.Rsv3 =
                            (reservedBits & 1) != 0;
                        request.Header.Opcode = (WebSocketOpcode)opcode;
                        WebSocketFrameCodec writer =
                            new WebSocketFrameCodec(
                                writerRole,
                                125,
                                claims);
                        int expectedLength = masked ? 6 : 2;
                        byte[] destination =
                            Enumerable.Repeat(
                                (byte)0xaa,
                                expectedLength).ToArray();
                        bool fragmentedControl =
                            opcode >= 8 && !final;
                        string description =
                            $"opcode={opcode}, final={final}, " +
                            $"reserved={reservedBits}, masked={masked}";

                        if (fragmentedControl)
                        {
                            Assert.Throws<WebSocketProtocolException>(
                                () => writer.MeasureFrame(request),
                                description);
                            Assert.Throws<WebSocketProtocolException>(
                                () => writer.WriteFrame(
                                    request,
                                    destination),
                                description);
                            Assert.That(
                                destination,
                                Is.EqualTo(Enumerable.Repeat(
                                    (byte)0xaa,
                                    expectedLength)),
                                description);
                            Assert.That(
                                source.CallCount,
                                Is.Zero,
                                description);
                        }
                        else
                        {
                            int measured = writer.MeasureFrame(request);
                            Assert.That(
                                source.CallCount,
                                Is.Zero,
                                description);
                            int written = writer.WriteFrame(
                                request,
                                destination);
                            WebSocketFrameCodec reader =
                                new WebSocketFrameCodec(
                                    readerRole,
                                    125,
                                    claims);
                            WebSocketFrameReadResult read =
                                reader.ReadFrame(destination);

                            Assert.That(
                                measured,
                                Is.EqualTo(expectedLength),
                                description);
                            Assert.That(
                                written,
                                Is.EqualTo(expectedLength),
                                description);
                            Assert.That(
                                destination[0],
                                Is.EqualTo((byte)((finalValue << 7) |
                                    (reservedBits << 4) | opcode)),
                                description);
                            Assert.That(
                                destination[1],
                                Is.EqualTo(masked ? 0x80 : 0x00),
                                description);
                            Assert.That(
                                source.CallCount,
                                Is.EqualTo(masked ? 1 : 0),
                                description);
                            Assert.That(
                                read.Status,
                                Is.EqualTo(WebSocketParseStatus.Complete),
                                description);
                            Assert.That(
                                read.Header.Opcode,
                                Is.EqualTo((WebSocketOpcode)opcode),
                                description);
                        }
                    }
                }
            }
        }
    }

    [TestCase(0, false)]
    [TestCase(1, false)]
    [TestCase(125, false)]
    [TestCase(126, false)]
    [TestCase(65535, false)]
    [TestCase(65536, false)]
    [TestCase(0, true)]
    [TestCase(1, true)]
    [TestCase(125, true)]
    [TestCase(126, true)]
    [TestCase(65535, true)]
    [TestCase(65536, true)]
    public void EveryHeaderLengthFormMeasuresWritesAndReadsBack(
        int payloadLength,
        bool masked)
    {
        WebSocketRole writerRole = WebSocketRole.Server;
        WebSocketRole readerRole = WebSocketRole.Client;
        if (masked)
        {
            writerRole = WebSocketRole.Client;
            readerRole = WebSocketRole.Server;
        }

        WebSocketFrameCodec writer =
            new WebSocketFrameCodec(writerRole, 65536, []);
        WebSocketFrameCodec reader =
            new WebSocketFrameCodec(readerRole, 65536, []);
        FrameCodecMaskKeySource source = new FrameCodecMaskKeySource();
        source.NextKey = 0xa1b2c3d4U;
        byte[] payload = new byte[payloadLength];
        for (int index = 0; index < payload.Length; index++)
        {
            payload[index] = (byte)(index * 31);
        }

        WebSocketFrameWriteRequest request = CreateRequest(
            payloadLength,
            masked,
            payload,
            0,
            payloadLength,
            0,
            source);
        int expectedHeaderLength = HeaderLength(payloadLength);
        if (masked)
        {
            expectedHeaderLength = expectedHeaderLength +
                WebSocketProtocolConstants.MaskingKeyLength;
        }

        int measured = writer.MeasureFrame(request);
        byte[] encoded = new byte[measured];
        int written = writer.WriteFrame(request, encoded);
        WebSocketFrameReadResult read = reader.ReadFrame(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(measured,
                Is.EqualTo(expectedHeaderLength + payloadLength));
            Assert.That(written, Is.EqualTo(measured));
            Assert.That(request.PayloadOffset, Is.EqualTo(payloadLength));
            Assert.That(request.PayloadCount, Is.Zero);
            Assert.That(request.MaskingOffset, Is.EqualTo(payloadLength));
            Assert.That(source.CallCount, Is.EqualTo(masked ? 1 : 0));
            Assert.That(read.Status,
                Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(read.ConsumedCount, Is.EqualTo(encoded.Length));
            Assert.That(read.Header.PayloadLength,
                Is.EqualTo(payloadLength));
            Assert.That(read.Header.Masked, Is.EqualTo(masked));
            Assert.That(read.Payload, Is.EqualTo(payload));
        });
    }

    [Test]
    public void ZeroCapacityWriteRetainsOneDeterministicFrameStart()
    {
        WebSocketFrameCodec codec =
            new WebSocketFrameCodec(WebSocketRole.Client, 125, []);
        FrameCodecMaskKeySource source = new FrameCodecMaskKeySource();
        source.NextKey = 0x37fa213dU;
        byte[] payload = [0x48, 0x65, 0x6c, 0x6c, 0x6f];
        WebSocketFrameWriteRequest request = CreateRequest(
            payload.Length,
            true,
            payload,
            0,
            payload.Length,
            0,
            source);
        request.Header.Opcode = WebSocketOpcode.Text;

        int emptyWritten = codec.WriteFrame(request, []);
        int offsetAfterEmpty = request.PayloadOffset;
        int countAfterEmpty = request.PayloadCount;
        int maskingOffsetAfterEmpty = request.MaskingOffset;
        byte[] destination = new byte[11];
        int written = codec.WriteFrame(request, destination);

        Assert.Multiple(() =>
        {
            Assert.That(emptyWritten, Is.Zero);
            Assert.That(offsetAfterEmpty, Is.Zero);
            Assert.That(countAfterEmpty, Is.EqualTo(payload.Length));
            Assert.That(maskingOffsetAfterEmpty, Is.Zero);
            Assert.That(written, Is.EqualTo(11));
            Assert.That(destination,
                Is.EqualTo(Convert.FromHexString(
                    "818537fa213d7f9f4d5158")));
            Assert.That(source.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void SeparatePublicPayloadChunksContinueOneMaskCycle()
    {
        WebSocketFrameCodec codec =
            new WebSocketFrameCodec(WebSocketRole.Client, 125, []);
        FrameCodecMaskKeySource source = new FrameCodecMaskKeySource();
        source.NextKey = 0x37fa213dU;
        byte[] payload = [0x48, 0x65, 0x6c, 0x6c, 0x6f];
        WebSocketFrameWriteRequest firstRequest = CreateRequest(
            payload.Length,
            true,
            payload,
            0,
            2,
            0,
            source);
        firstRequest.Header.Opcode = WebSocketOpcode.Text;
        byte[] firstDestination = new byte[8];

        int firstWritten = codec.WriteFrame(
            firstRequest,
            firstDestination);
        WebSocketFrameWriteRequest secondRequest = CreateRequest(
            payload.Length,
            true,
            payload,
            2,
            3,
            2,
            source);
        secondRequest.Header.Opcode = WebSocketOpcode.Text;
        byte[] secondDestination = new byte[3];
        int secondWritten = codec.WriteFrame(
            secondRequest,
            secondDestination);
        byte[] combined = firstDestination
            .Concat(secondDestination).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(firstWritten, Is.EqualTo(8));
            Assert.That(firstRequest.PayloadOffset, Is.EqualTo(2));
            Assert.That(firstRequest.PayloadCount, Is.Zero);
            Assert.That(firstRequest.MaskingOffset, Is.EqualTo(2));
            Assert.That(secondWritten, Is.EqualTo(3));
            Assert.That(secondRequest.PayloadOffset, Is.EqualTo(5));
            Assert.That(secondRequest.PayloadCount, Is.Zero);
            Assert.That(secondRequest.MaskingOffset, Is.EqualTo(5));
            Assert.That(combined,
                Is.EqualTo(Convert.FromHexString(
                    "818537fa213d7f9f4d5158")));
            Assert.That(source.CallCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ConsecutiveClientFramesAcquireDifferentKeys()
    {
        var codec = new WebSocketFrameCodec(WebSocketRole.Client, 100, []);
        var source = new FrameCodecMaskKeySource();
        var firstRequest = CreateRequest(0, true, [], 0, 0, 0, source);
        var secondRequest = CreateRequest(0, true, [], 0, 0, 0, source);
        var first = new byte[6];
        var second = new byte[6];

        codec.WriteFrame(firstRequest, first);
        codec.WriteFrame(secondRequest, second);

        Assert.Multiple(() =>
        {
            Assert.That(source.CallCount, Is.EqualTo(2));
            Assert.That(first.AsSpan(2, 4).ToArray(), Is.Not.EqualTo(second.AsSpan(2, 4).ToArray()));
        });
    }

    [Test]
    public void PublicWriteMapsRequestAndLimitFailuresToExceptionsWithoutOutput()
    {
        var codec = new WebSocketFrameCodec(WebSocketRole.Server, 10, []);
        var source = new FrameCodecMaskKeySource();
        var invalidRange = CreateRequest(1, false, [], 0, 1, 0, source);
        var oversized = CreateRequest(11, false, [], 0, 0, 0, source);
        var destination = Enumerable.Repeat((byte)0xaa, 16).ToArray();

        Assert.Throws<WebSocketStateException>(() => codec.WriteFrame(invalidRange, destination));
        Assert.Throws<WebSocketLimitException>(() => codec.WriteFrame(oversized, destination));
        Assert.That(destination, Is.EqualTo(Enumerable.Repeat((byte)0xaa, 16)));
    }

    static WebSocketFrameWriteRequest CreateRequest(
        long payloadLength,
        bool masked,
        byte[] payload,
        int payloadOffset,
        int payloadCount,
        int maskingOffset,
        FrameCodecMaskKeySource source)
    {
        var request = new WebSocketFrameWriteRequest();
        request.Header.Final = true;
        request.Header.Opcode = WebSocketOpcode.Binary;
        request.Header.Masked = masked;
        request.Header.PayloadLength = payloadLength;
        request.Payload = payload;
        request.PayloadOffset = payloadOffset;
        request.PayloadCount = payloadCount;
        request.MaskingOffset = maskingOffset;
        if (masked)
        {
            request.HasMaskKeySource = true;
            request.MaskKeySource = source;
        }

        return request;
    }

    static WebSocketExtensionNegotiation AllFrameClaims()
    {
        return new WebSocketExtensionNegotiation
        {
            Name = "x-all-frame-claims",
            Status = WebSocketExtensionStatus.Negotiated,
            HasAcceptedConfiguration = true,
            AcceptedConfiguration = new WebSocketExtensionOffer
            {
                Name = "x-all-frame-claims"
            },
            ClaimsRsv1 = true,
            ClaimsRsv2 = true,
            ClaimsRsv3 = true,
            ClaimedOpcodes =
            [
                WebSocketOpcode.ReservedData3,
                WebSocketOpcode.ReservedData4,
                WebSocketOpcode.ReservedData5,
                WebSocketOpcode.ReservedData6,
                WebSocketOpcode.ReservedData7,
                WebSocketOpcode.ReservedControl11,
                WebSocketOpcode.ReservedControl12,
                WebSocketOpcode.ReservedControl13,
                WebSocketOpcode.ReservedControl14,
                WebSocketOpcode.ReservedControl15
            ]
        };
    }

    static byte[] CreateUnmaskedFrameWithTrailingBytes(int payloadLength)
    {
        int headerLength = HeaderLength(payloadLength);
        byte[] frame = new byte[headerLength + payloadLength + 2];
        frame[0] = 0x82;
        if (payloadLength <= 125)
        {
            frame[1] = (byte)payloadLength;
        }
        else if (payloadLength <= 65535)
        {
            frame[1] = 126;
            frame[2] = (byte)(payloadLength >> 8);
            frame[3] = (byte)payloadLength;
        }
        else
        {
            frame[1] = 127;
            frame[6] = (byte)(payloadLength >> 24);
            frame[7] = (byte)(payloadLength >> 16);
            frame[8] = (byte)(payloadLength >> 8);
            frame[9] = (byte)payloadLength;
        }

        for (int index = 0; index < payloadLength; index++)
        {
            frame[headerLength + index] = (byte)index;
        }

        frame[headerLength + payloadLength] = 0xaa;
        frame[headerLength + payloadLength + 1] = 0xbb;
        return frame;
    }

    static int HeaderLength(int payloadLength)
    {
        if (payloadLength <= 125)
        {
            return 2;
        }

        if (payloadLength <= 65535)
        {
            return 4;
        }

        return 10;
    }
}

internal class FrameCodecMaskKeySource : IWebSocketMaskKeySource
{
    internal int CallCount;
    internal uint NextKey = 0x11223344U;

    public uint CreateMaskingKey()
    {
        uint result = NextKey;
        NextKey = NextKey + 0x01010101U;
        CallCount = CallCount + 1;
        return result;
    }
}
