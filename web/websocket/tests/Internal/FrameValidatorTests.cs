using websocket;

namespace websocket.Tests;

public class FrameValidatorTests
{
    [TestCase(WebSocketOpcode.Continuation)]
    [TestCase(WebSocketOpcode.Text)]
    [TestCase(WebSocketOpcode.Binary)]
    [TestCase(WebSocketOpcode.Close)]
    [TestCase(WebSocketOpcode.Ping)]
    [TestCase(WebSocketOpcode.Pong)]
    public void BaseOpcodesAreValidWithoutExtensionClaims(WebSocketOpcode opcode)
    {
        var header = CreateHeader(opcode, false, true, 0);

        var result = FrameValidator.ValidateHeader(header, WebSocketRole.Client, 1024, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
        });
    }

    [TestCase(WebSocketRole.Server, false, "frame-mask-required")]
    [TestCase(WebSocketRole.Client, true, "frame-mask-forbidden")]
    public void RoleIncorrectMaskingIsRejected(
        WebSocketRole localRole,
        bool masked,
        string expectedIdentifier)
    {
        var header = CreateHeader(WebSocketOpcode.Binary, masked, true, 0);

        var result = FrameValidator.ValidateHeader(header, localRole, 1024, []);

        AssertProtocolFailure(result, expectedIdentifier);
    }

    [Test]
    public void RoleCorrectMaskingIsAcceptedInBothDirections()
    {
        var masked = CreateHeader(WebSocketOpcode.Binary, true, true, 0);
        var unmasked = CreateHeader(WebSocketOpcode.Binary, false, true, 0);

        var server = FrameValidator.ValidateHeader(masked, WebSocketRole.Server, 1024, []);
        var client = FrameValidator.ValidateHeader(unmasked, WebSocketRole.Client, 1024, []);

        Assert.Multiple(() =>
        {
            Assert.That(server.Valid, Is.True);
            Assert.That(client.Valid, Is.True);
        });
    }

    [TestCase(1, "frame-rsv1-unclaimed")]
    [TestCase(2, "frame-rsv2-unclaimed")]
    [TestCase(3, "frame-rsv3-unclaimed")]
    public void UnclaimedReservedBitsAreRejected(int reservedBit, string expectedIdentifier)
    {
        var header = CreateHeader(WebSocketOpcode.Binary, false, true, 0);
        if (reservedBit == 1)
        {
            header.Rsv1 = true;
        }
        else if (reservedBit == 2)
        {
            header.Rsv2 = true;
        }
        else
        {
            header.Rsv3 = true;
        }

        var result = FrameValidator.ValidateHeader(header, WebSocketRole.Client, 1024, []);

        AssertProtocolFailure(result, expectedIdentifier);
    }

    [Test]
    public void NegotiatedClaimsAllowReservedBitsAndOpcode()
    {
        var header = CreateHeader(WebSocketOpcode.ReservedControl15, false, true, 125);
        header.Rsv1 = true;
        header.Rsv2 = true;
        header.Rsv3 = true;
        var extension = new WebSocketExtensionNegotiation();
        extension.Status = WebSocketExtensionStatus.Negotiated;
        extension.ClaimsRsv1 = true;
        extension.ClaimsRsv2 = true;
        extension.ClaimsRsv3 = true;
        extension.ClaimedOpcodes = new[] { WebSocketOpcode.ReservedControl15 };

        var result = FrameValidator.ValidateHeader(header, WebSocketRole.Client, 1024, new[] { extension });

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.True);
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
        });
    }

    [TestCase(WebSocketExtensionStatus.Offered)]
    [TestCase(WebSocketExtensionStatus.Declined)]
    [TestCase(WebSocketExtensionStatus.Invalid)]
    public void ClaimsFromNonNegotiatedEntriesAreIgnored(WebSocketExtensionStatus status)
    {
        var header = CreateHeader(WebSocketOpcode.Binary, false, true, 0);
        header.Rsv1 = true;
        var extension = new WebSocketExtensionNegotiation();
        extension.Status = status;
        extension.ClaimsRsv1 = true;

        var result = FrameValidator.ValidateHeader(header, WebSocketRole.Client, 1024, new[] { extension });

        AssertProtocolFailure(result, "frame-rsv1-unclaimed");
    }

    [Test]
    public void NullExtensionCollectionBehavesAsNoClaims()
    {
        var header = CreateHeader(WebSocketOpcode.Binary, false, true, 0);

        var result = FrameValidator.ValidateHeader(header, WebSocketRole.Client, 1024, null);

        Assert.That(result.Valid, Is.True);
    }

    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(11)]
    [TestCase(12)]
    [TestCase(13)]
    [TestCase(14)]
    [TestCase(15)]
    public void ReservedOpcodesRequireANegotiatedClaim(int opcode)
    {
        var header = CreateHeader((WebSocketOpcode)opcode, false, true, 0);

        var result = FrameValidator.ValidateHeader(header, WebSocketRole.Client, 1024, []);

        AssertProtocolFailure(result, "frame-opcode-unclaimed");
    }

    [Test]
    public void EveryReservedOpcodeCanBeClaimed()
    {
        int[] opcodes = [3, 4, 5, 6, 7, 11, 12, 13, 14, 15];

        foreach (var opcode in opcodes)
        {
            var header = CreateHeader((WebSocketOpcode)opcode, false, true, 0);
            var extension = new WebSocketExtensionNegotiation();
            extension.Status = WebSocketExtensionStatus.Negotiated;
            extension.ClaimedOpcodes = new[] { (WebSocketOpcode)opcode };

            var result = FrameValidator.ValidateHeader(header, WebSocketRole.Client, 1024, new[] { extension });

            Assert.That(result.Valid, Is.True, $"Opcode {opcode}");
        }
    }

    [Test]
    public void OutOfRangeClaimsDoNotAuthorizeAReservedOpcode()
    {
        var header = CreateHeader(WebSocketOpcode.ReservedData3, false, true, 0);
        var extension = new WebSocketExtensionNegotiation();
        extension.Status = WebSocketExtensionStatus.Negotiated;
        extension.ClaimedOpcodes = new[] { (WebSocketOpcode)(-1), (WebSocketOpcode)16 };

        var result = FrameValidator.ValidateHeader(header, WebSocketRole.Client, 1024, new[] { extension });

        AssertProtocolFailure(result, "frame-opcode-unclaimed");
    }

    [TestCase(-1)]
    [TestCase(16)]
    [TestCase(int.MaxValue)]
    public void ValuesOutsideTheFourBitOpcodeRangeAreRejected(int opcode)
    {
        var header = CreateHeader((WebSocketOpcode)opcode, false, true, 0);

        var result = FrameValidator.ValidateHeader(header, WebSocketRole.Client, 1024, []);

        AssertProtocolFailure(result, "frame-opcode-out-of-range");
    }

    [TestCase(WebSocketOpcode.Close)]
    [TestCase(WebSocketOpcode.Ping)]
    [TestCase(WebSocketOpcode.Pong)]
    [TestCase(WebSocketOpcode.ReservedControl11)]
    public void ControlFramesCannotBeFragmented(WebSocketOpcode opcode)
    {
        var header = CreateHeader(opcode, false, false, 0);
        WebSocketExtensionNegotiation[] extensions = [];
        if (opcode == WebSocketOpcode.ReservedControl11)
        {
            var extension = new WebSocketExtensionNegotiation();
            extension.Status = WebSocketExtensionStatus.Negotiated;
            extension.ClaimedOpcodes = new[] { opcode };
            extensions = new[] { extension };
        }

        var result = FrameValidator.ValidateHeader(header, WebSocketRole.Client, 1024, extensions);

        AssertProtocolFailure(result, "frame-control-fragmented");
    }

    [Test]
    public void ControlPayloadBoundaryIsEnforced()
    {
        var maximum = CreateHeader(WebSocketOpcode.Ping, false, true, 125);
        var oversized = CreateHeader(WebSocketOpcode.Ping, false, true, 126);

        var valid = FrameValidator.ValidateHeader(maximum, WebSocketRole.Client, 1024, []);
        var invalid = FrameValidator.ValidateHeader(oversized, WebSocketRole.Client, 1024, []);

        Assert.That(valid.Valid, Is.True);
        AssertProtocolFailure(invalid, "frame-control-too-large");
    }

    [Test]
    public void FragmentedDataFrameCanExceedTheControlBoundary()
    {
        var header = CreateHeader(WebSocketOpcode.Binary, false, false, 126);

        var result = FrameValidator.ValidateHeader(header, WebSocketRole.Client, 1024, []);

        Assert.That(result.Valid, Is.True);
    }

    [Test]
    public void NegativePayloadLengthIsRejected()
    {
        var header = CreateHeader(WebSocketOpcode.Binary, false, true, -1);

        var result = FrameValidator.ValidateHeader(header, WebSocketRole.Client, 1024, []);

        AssertProtocolFailure(result, "frame-payload-negative");
    }

    [Test]
    public void ConfiguredPayloadLimitAcceptsEqualityAndRejectsLargerFrames()
    {
        var maximum = CreateHeader(WebSocketOpcode.Binary, false, true, 100);
        var oversized = CreateHeader(WebSocketOpcode.Binary, false, true, 101);

        var valid = FrameValidator.ValidateHeader(maximum, WebSocketRole.Client, 100, []);
        var invalid = FrameValidator.ValidateHeader(oversized, WebSocketRole.Client, 100, []);

        Assert.That(valid.Valid, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(invalid.Valid, Is.False);
            Assert.That(invalid.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(invalid.Error.Kind, Is.EqualTo(WebSocketErrorKind.Limit));
            Assert.That(invalid.Error.CloseCode, Is.EqualTo(WebSocketCloseCodes.MessageTooBig));
            Assert.That(invalid.Error.KnownCloseCode, Is.EqualTo(WebSocketCloseCode.MessageTooBig));
            Assert.That(invalid.Error.Identifier, Is.EqualTo("frame-payload-too-large"));
        });
    }

    [Test]
    public void InvalidLocalRoleIsRejectedAsStateFailure()
    {
        var header = CreateHeader(WebSocketOpcode.Binary, false, true, 0);

        var result = FrameValidator.ValidateHeader(header, (WebSocketRole)99, 1024, null);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.State));
            Assert.That(result.Error.Identifier, Is.EqualTo("frame-role-invalid"));
        });
    }

    [Test]
    public void NonpositiveConfiguredPayloadLimitIsRejected()
    {
        var header = CreateHeader(WebSocketOpcode.Binary, false, true, 0);

        var result = FrameValidator.ValidateHeader(header, WebSocketRole.Client, 0, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.State));
            Assert.That(result.Error.Identifier, Is.EqualTo("frame-limit-invalid"));
        });
    }

    static WebSocketFrameHeader CreateHeader(
        WebSocketOpcode opcode,
        bool masked,
        bool final,
        long payloadLength)
    {
        var header = new WebSocketFrameHeader();
        header.Opcode = opcode;
        header.Masked = masked;
        header.Final = final;
        header.PayloadLength = payloadLength;
        return header;
    }

    static void AssertProtocolFailure(FrameValidationResult result, string identifier)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Valid, Is.False);
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Error.Kind, Is.EqualTo(WebSocketErrorKind.Protocol));
            Assert.That(result.Error.HasCloseCode, Is.True);
            Assert.That(result.Error.CloseCode, Is.EqualTo(WebSocketCloseCodes.ProtocolError));
            Assert.That(result.Error.HasKnownCloseCode, Is.True);
            Assert.That(result.Error.KnownCloseCode, Is.EqualTo(WebSocketCloseCode.ProtocolError));
            Assert.That(result.Error.Identifier, Is.EqualTo(identifier));
        });
    }
}
