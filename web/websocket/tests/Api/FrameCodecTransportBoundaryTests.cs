using websocket;

namespace websocket.Tests;

public class FrameCodecTransportBoundaryTests
{
    [Test]
    public void RepresentativeFramesMatchUnsplitDecodingAtEveryBoundary()
    {
        FrameBoundaryCase[] cases = Cases();

        foreach (FrameBoundaryCase frameCase in cases)
        {
            FrameBoundarySnapshot expected = Decode(frameCase, -1);

            for (int split = 0;
                split <= frameCase.Source.Length;
                split++)
            {
                FrameBoundarySnapshot actual = Decode(frameCase, split);

                AssertSnapshot(
                    actual,
                    expected,
                    $"{frameCase.Name} split {split}");
            }
        }
    }

    static FrameBoundaryCase[] Cases()
    {
        WebSocketExtensionNegotiation[] claims = [AllClaims()];
        return
        [
            new FrameBoundaryCase
            {
                Name = "empty-unmasked",
                Role = WebSocketRole.Client,
                MaximumPayloadLength = 65536,
                Source = [0x81, 0x00]
            },
            new FrameBoundaryCase
            {
                Name = "basic-unmasked",
                Role = WebSocketRole.Client,
                MaximumPayloadLength = 65536,
                Source = CreateFrame(0x82, 17, false, 0U)
            },
            new FrameBoundaryCase
            {
                Name = "rfc-masked",
                Role = WebSocketRole.Server,
                MaximumPayloadLength = 65536,
                Source = Convert.FromHexString(
                    "818537fa213d7f9f4d5158")
            },
            new FrameBoundaryCase
            {
                Name = "maximum-control",
                Role = WebSocketRole.Client,
                MaximumPayloadLength = 65536,
                Source = CreateFrame(0x89, 125, false, 0U)
            },
            new FrameBoundaryCase
            {
                Name = "extended-16-unmasked",
                Role = WebSocketRole.Client,
                MaximumPayloadLength = 65536,
                Source = CreateFrame(0x82, 126, false, 0U)
            },
            new FrameBoundaryCase
            {
                Name = "extended-16-masked",
                Role = WebSocketRole.Server,
                MaximumPayloadLength = 65536,
                Source = CreateFrame(0x82, 126, true, 0xa1b2c3d4U)
            },
            new FrameBoundaryCase
            {
                Name = "claimed-reserved-flags-opcode",
                Role = WebSocketRole.Client,
                MaximumPayloadLength = 65536,
                Extensions = claims,
                Source = CreateFrame(0xf3, 7, false, 0U)
            },
            new FrameBoundaryCase
            {
                Name = "non-minimal-16-bit",
                Role = WebSocketRole.Client,
                MaximumPayloadLength = 65536,
                Source = Convert.FromHexString("827e007d")
            },
            new FrameBoundaryCase
            {
                Name = "signed-63-bit",
                Role = WebSocketRole.Client,
                MaximumPayloadLength = 65536,
                Source = Convert.FromHexString(
                    "827f8000000000000000")
            },
            new FrameBoundaryCase
            {
                Name = "wrong-role-mask",
                Role = WebSocketRole.Client,
                MaximumPayloadLength = 65536,
                Source = Convert.FromHexString("818011223344")
            },
            new FrameBoundaryCase
            {
                Name = "extended-63-limit",
                Role = WebSocketRole.Client,
                MaximumPayloadLength = 125,
                Source = Convert.FromHexString(
                    "827f0000000000010000")
            }
        ];
    }

    static FrameBoundarySnapshot Decode(
        FrameBoundaryCase frameCase,
        int split)
    {
        WebSocketFrameCodec codec = new WebSocketFrameCodec(
            frameCase.Role,
            frameCase.MaximumPayloadLength,
            frameCase.Extensions);
        FrameBoundarySnapshot snapshot = new FrameBoundarySnapshot();
        if (split < 0)
        {
            Accumulate(snapshot, codec.ReadFrame(frameCase.Source));
            return snapshot;
        }

        Accumulate(
            snapshot,
            codec.ReadFrame(frameCase.Source.AsSpan(0, split)));
        Accumulate(
            snapshot,
            codec.ReadFrame(frameCase.Source.AsSpan(split)));
        return snapshot;
    }

    static void Accumulate(
        FrameBoundarySnapshot snapshot,
        WebSocketFrameReadResult result)
    {
        snapshot.Status = result.Status;
        snapshot.ConsumedCount =
            snapshot.ConsumedCount + result.ConsumedCount;
        if (result.HasHeader)
        {
            snapshot.HasHeader = true;
            snapshot.Header = CopyHeader(result.Header);
        }

        for (int index = 0; index < result.PayloadCount; index++)
        {
            snapshot.Payload.Add(
                result.Payload[result.PayloadOffset + index]);
        }

        snapshot.EndOfFrame = snapshot.EndOfFrame || result.EndOfFrame;
        if (result.HasError)
        {
            snapshot.HasError = true;
            snapshot.Error = CopyError(result.Error);
        }
    }

    static void AssertSnapshot(
        FrameBoundarySnapshot actual,
        FrameBoundarySnapshot expected,
        string description)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Status,
                Is.EqualTo(expected.Status), description);
            Assert.That(actual.ConsumedCount,
                Is.EqualTo(expected.ConsumedCount), description);
            Assert.That(actual.HasHeader,
                Is.EqualTo(expected.HasHeader), description);
            Assert.That(actual.Header.Final,
                Is.EqualTo(expected.Header.Final), description);
            Assert.That(actual.Header.Rsv1,
                Is.EqualTo(expected.Header.Rsv1), description);
            Assert.That(actual.Header.Rsv2,
                Is.EqualTo(expected.Header.Rsv2), description);
            Assert.That(actual.Header.Rsv3,
                Is.EqualTo(expected.Header.Rsv3), description);
            Assert.That(actual.Header.Opcode,
                Is.EqualTo(expected.Header.Opcode), description);
            Assert.That(actual.Header.Masked,
                Is.EqualTo(expected.Header.Masked), description);
            Assert.That(actual.Header.MaskingKey,
                Is.EqualTo(expected.Header.MaskingKey), description);
            Assert.That(actual.Header.PayloadLength,
                Is.EqualTo(expected.Header.PayloadLength), description);
            Assert.That(actual.Payload,
                Is.EqualTo(expected.Payload), description);
            Assert.That(actual.EndOfFrame,
                Is.EqualTo(expected.EndOfFrame), description);
            Assert.That(actual.HasError,
                Is.EqualTo(expected.HasError), description);
            Assert.That(actual.Error.Kind,
                Is.EqualTo(expected.Error.Kind), description);
            Assert.That(actual.Error.HasCloseCode,
                Is.EqualTo(expected.Error.HasCloseCode), description);
            Assert.That(actual.Error.CloseCode,
                Is.EqualTo(expected.Error.CloseCode), description);
            Assert.That(actual.Error.HasKnownCloseCode,
                Is.EqualTo(expected.Error.HasKnownCloseCode), description);
            Assert.That(actual.Error.KnownCloseCode,
                Is.EqualTo(expected.Error.KnownCloseCode), description);
            Assert.That(actual.Error.Identifier,
                Is.EqualTo(expected.Error.Identifier), description);
            Assert.That(actual.Error.Diagnostic,
                Is.EqualTo(expected.Error.Diagnostic), description);
        });
    }

    static byte[] CreateFrame(
        byte firstByte,
        int payloadLength,
        bool masked,
        uint maskingKey)
    {
        int headerLength = 2;
        if (payloadLength > 65535)
        {
            headerLength = 10;
        }
        else if (payloadLength > 125)
        {
            headerLength = 4;
        }

        if (masked)
        {
            headerLength = headerLength + 4;
        }

        byte[] frame = new byte[headerLength + payloadLength];
        frame[0] = firstByte;
        int maskBit = masked ? 128 : 0;
        int headerIndex = 2;
        if (payloadLength <= 125)
        {
            frame[1] = (byte)(maskBit | payloadLength);
        }
        else if (payloadLength <= 65535)
        {
            frame[1] = (byte)(maskBit | 126);
            frame[2] = (byte)(payloadLength >> 8);
            frame[3] = (byte)payloadLength;
            headerIndex = 4;
        }
        else
        {
            frame[1] = (byte)(maskBit | 127);
            frame[6] = (byte)(payloadLength >> 24);
            frame[7] = (byte)(payloadLength >> 16);
            frame[8] = (byte)(payloadLength >> 8);
            frame[9] = (byte)payloadLength;
            headerIndex = 10;
        }

        if (masked)
        {
            frame[headerIndex] = (byte)(maskingKey >> 24);
            frame[headerIndex + 1] = (byte)(maskingKey >> 16);
            frame[headerIndex + 2] = (byte)(maskingKey >> 8);
            frame[headerIndex + 3] = (byte)maskingKey;
            headerIndex = headerIndex + 4;
        }

        for (int index = 0; index < payloadLength; index++)
        {
            frame[headerIndex + index] = (byte)(index * 37 + 11);
        }

        if (masked)
        {
            WebSocketMasker masker = new WebSocketMasker();
            masker.ApplyMask(
                frame.AsSpan(headerIndex, payloadLength),
                maskingKey,
                0);
        }

        return frame;
    }

    static WebSocketExtensionNegotiation AllClaims()
    {
        return new WebSocketExtensionNegotiation
        {
            Name = "x-boundary-claims",
            Status = WebSocketExtensionStatus.Negotiated,
            HasAcceptedConfiguration = true,
            AcceptedConfiguration = new WebSocketExtensionOffer
            {
                Name = "x-boundary-claims"
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

    static WebSocketFrameHeader CopyHeader(WebSocketFrameHeader source)
    {
        return new WebSocketFrameHeader
        {
            Final = source.Final,
            Rsv1 = source.Rsv1,
            Rsv2 = source.Rsv2,
            Rsv3 = source.Rsv3,
            Opcode = source.Opcode,
            Masked = source.Masked,
            MaskingKey = source.MaskingKey,
            PayloadLength = source.PayloadLength
        };
    }

    static WebSocketError CopyError(WebSocketError source)
    {
        return new WebSocketError
        {
            Kind = source.Kind,
            HasCloseCode = source.HasCloseCode,
            CloseCode = source.CloseCode,
            HasKnownCloseCode = source.HasKnownCloseCode,
            KnownCloseCode = source.KnownCloseCode,
            HasHttpStatusCode = source.HasHttpStatusCode,
            HttpStatusCode = source.HttpStatusCode,
            Identifier = source.Identifier,
            Diagnostic = source.Diagnostic
        };
    }
}

internal class FrameBoundaryCase
{
    internal string Name = "";
    internal WebSocketRole Role;
    internal int MaximumPayloadLength;
    internal WebSocketExtensionNegotiation[] Extensions = [];
    internal byte[] Source = [];
}

internal class FrameBoundarySnapshot
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.NeedInput;
    internal int ConsumedCount;
    internal bool HasHeader;
    internal WebSocketFrameHeader Header = new WebSocketFrameHeader();
    internal List<byte> Payload = [];
    internal bool EndOfFrame;
    internal bool HasError;
    internal WebSocketError Error = new WebSocketError();
}
