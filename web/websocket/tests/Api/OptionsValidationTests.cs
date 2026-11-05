using websocket;

namespace websocket.Tests;

public class OptionsValidationTests
{
    [Test]
    public void DefaultConnectionServerAndCompressionOptionsAreValid()
    {
        var connection = new WebSocketConnectionOptions();
        var server = new WebSocketServerHandshakeOptions();
        var compression = new PerMessageDeflateOptions();

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => WebSocketOptionValidator.ValidateConnectionOptions(connection));
            Assert.DoesNotThrow(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(server));
            Assert.DoesNotThrow(() => WebSocketOptionValidator.ValidatePerMessageDeflateOptions(compression));
        });
    }

    [Test]
    public void ConfiguredDefaultClientOptionsAreValid()
    {
        var options = new WebSocketClientHandshakeOptions();
        options.Uri = "wss://example.com/chat";

        Assert.DoesNotThrow(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(options));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void HandshakesRejectEveryNonPositiveHeaderLimit(int value)
    {
        var clientBytes = ClientOptions();
        clientBytes.MaximumHeaderBytes = value;
        var clientCount = ClientOptions();
        clientCount.MaximumHeaderCount = value;
        var clientLine = ClientOptions();
        clientLine.MaximumLineLength = value;
        var serverBytes = new WebSocketServerHandshakeOptions();
        serverBytes.MaximumHeaderBytes = value;
        var serverCount = new WebSocketServerHandshakeOptions();
        serverCount.MaximumHeaderCount = value;
        var serverLine = new WebSocketServerHandshakeOptions();
        serverLine.MaximumLineLength = value;

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(clientBytes));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(clientCount));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(clientLine));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(serverBytes));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(serverCount));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(serverLine));
        });
    }

    [TestCase(1)]
    [TestCase(int.MaxValue)]
    public void HandshakesAcceptExactAndMaximumHeaderLimits(int value)
    {
        var client = ClientOptions();
        client.MaximumHeaderBytes = value;
        client.MaximumHeaderCount = value;
        client.MaximumLineLength = value;
        var server = new WebSocketServerHandshakeOptions();
        server.MaximumHeaderBytes = value;
        server.MaximumHeaderCount = value;
        server.MaximumLineLength = value;

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(client));
            Assert.DoesNotThrow(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(server));
        });
    }

    [Test]
    public void HandshakeOptionsRejectMixedRawAndGenericExtensionConfiguration()
    {
        var client = new WebSocketClientHandshakeOptions();
        client.Uri = "wss://example.com/chat";
        client.Extensions = [new WebSocketExtensionOffer { Name = "x-test" }];
        client.ExtensionHandlers = [new DataTestExtension()];
        var server = new WebSocketServerHandshakeOptions();
        server.SupportedExtensions = [new WebSocketExtensionOffer { Name = "x-test" }];
        server.ExtensionHandlers = [new DataTestExtension()];

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketHandshakeException>(() =>
                WebSocketOptionValidator.ValidateClientHandshakeOptions(client));
            Assert.Throws<WebSocketHandshakeException>(() =>
                WebSocketOptionValidator.ValidateServerHandshakeOptions(server));
        });
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void ConnectionRejectsEveryNonPositiveFiniteLimit(int value)
    {
        var frame = new WebSocketConnectionOptions();
        frame.MaximumFramePayloadLength = value;
        var message = new WebSocketConnectionOptions();
        message.MaximumMessagePayloadLength = value;
        var decompressed = new WebSocketConnectionOptions();
        decompressed.MaximumDecompressedMessagePayloadLength = value;
        var fragments = new WebSocketConnectionOptions();
        fragments.MaximumFragmentCount = value;
        var input = new WebSocketConnectionOptions();
        input.MaximumBufferedInputBytes = value;
        var output = new WebSocketConnectionOptions();
        output.MaximumBufferedOutputBytes = value;
        var events = new WebSocketConnectionOptions();
        events.MaximumQueuedEvents = value;
        var automaticFrame = new WebSocketConnectionOptions();
        automaticFrame.AutomaticFramePayloadLength = value;
        var closeTimeout = new WebSocketConnectionOptions();
        closeTimeout.CloseTimeoutMilliseconds = value;
        var heartbeatIdle = new WebSocketConnectionOptions();
        heartbeatIdle.Heartbeat.IdleIntervalMilliseconds = value;
        var heartbeatPong = new WebSocketConnectionOptions();
        heartbeatPong.Heartbeat.PongTimeoutMilliseconds = value;
        var heartbeatToken = new WebSocketConnectionOptions();
        heartbeatToken.Heartbeat.TokenLength = value;

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(frame));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(message));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(decompressed));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(fragments));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(input));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(output));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(events));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(automaticFrame));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(closeTimeout));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(heartbeatIdle));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(heartbeatPong));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(heartbeatToken));
        });
    }

    [Test]
    public void ConnectionAcceptsEveryExactLowerBoundaryAndTightenedLimit()
    {
        var options = new WebSocketConnectionOptions();
        options.MaximumFramePayloadLength = 1;
        options.MaximumMessagePayloadLength = 1;
        options.MaximumDecompressedMessagePayloadLength = 1;
        options.MaximumFragmentCount = 1;
        options.MaximumBufferedInputBytes =
            WebSocketProtocolConstants.MaximumFrameHeaderLength;
        options.MaximumBufferedOutputBytes =
            WebSocketProtocolConstants.MaximumFrameHeaderLength + 1;
        options.MaximumQueuedEvents = 1;
        options.AutomaticFramePayloadLength = 1;
        options.CloseTimeoutMilliseconds = 1;
        options.Heartbeat.IdleIntervalMilliseconds = 1;
        options.Heartbeat.PongTimeoutMilliseconds = 1;
        options.Heartbeat.TokenLength = 1;

        Assert.DoesNotThrow(() =>
            WebSocketOptionValidator.ValidateConnectionOptions(options));
    }

    [Test]
    public void ConnectionRejectsUnsafeLimitRelationships()
    {
        var frameExceedsMessage = new WebSocketConnectionOptions();
        frameExceedsMessage.MaximumFramePayloadLength = frameExceedsMessage.MaximumMessagePayloadLength + 1;
        var automaticExceedsFrame = new WebSocketConnectionOptions();
        automaticExceedsFrame.AutomaticFramePayloadLength = automaticExceedsFrame.MaximumFramePayloadLength + 1;
        var inputCannotHoldHeader = new WebSocketConnectionOptions();
        inputCannotHoldHeader.MaximumBufferedInputBytes = WebSocketProtocolConstants.MaximumFrameHeaderLength - 1;
        var outputCannotHoldFrame = new WebSocketConnectionOptions();
        outputCannotHoldFrame.MaximumBufferedOutputBytes = WebSocketProtocolConstants.MaximumFrameHeaderLength;
        outputCannotHoldFrame.AutomaticFramePayloadLength = 1;
        var fragmentsCannotRepresentMessage = new WebSocketConnectionOptions();
        fragmentsCannotRepresentMessage.MaximumFragmentCount = WebSocketDefaults.MaximumFragmentCount - 1;
        var heartbeatTokenTooLarge = new WebSocketConnectionOptions();
        heartbeatTokenTooLarge.Heartbeat.TokenLength = WebSocketProtocolConstants.ControlPayloadLengthMaximum + 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(frameExceedsMessage));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(automaticExceedsFrame));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(inputCannotHoldHeader));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(outputCannotHoldFrame));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(fragmentsCannotRepresentMessage));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(heartbeatTokenTooLarge));
        });
    }

    [Test]
    public void ConnectionLimitRelationshipsRemainOverflowSafeNearIntMaximum()
    {
        var valid = new WebSocketConnectionOptions();
        valid.MaximumFramePayloadLength = int.MaxValue;
        valid.MaximumMessagePayloadLength = int.MaxValue;
        valid.MaximumDecompressedMessagePayloadLength = int.MaxValue;
        valid.MaximumFragmentCount = 2;
        valid.MaximumBufferedInputBytes = WebSocketProtocolConstants.MaximumFrameHeaderLength;
        valid.MaximumBufferedOutputBytes = int.MaxValue;
        valid.AutomaticFramePayloadLength = int.MaxValue - WebSocketProtocolConstants.MaximumFrameHeaderLength;
        var encodedSizeWouldOverflow = new WebSocketConnectionOptions();
        encodedSizeWouldOverflow.MaximumFramePayloadLength = int.MaxValue;
        encodedSizeWouldOverflow.MaximumMessagePayloadLength = int.MaxValue;
        encodedSizeWouldOverflow.MaximumDecompressedMessagePayloadLength = int.MaxValue;
        encodedSizeWouldOverflow.MaximumFragmentCount = 1;
        encodedSizeWouldOverflow.MaximumBufferedOutputBytes = int.MaxValue;
        encodedSizeWouldOverflow.AutomaticFramePayloadLength = int.MaxValue;

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => WebSocketOptionValidator.ValidateConnectionOptions(valid));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateConnectionOptions(encodedSizeWouldOverflow));
        });
    }

    [Test]
    public void DecompressedLimitCanBeTightenedIndependently()
    {
        var options = new WebSocketConnectionOptions();
        options.MaximumDecompressedMessagePayloadLength = 1;

        Assert.DoesNotThrow(() => WebSocketOptionValidator.ValidateConnectionOptions(options));
    }

    [Test]
    public void ConnectionAcceptsExactUpperRelationshipsAndOptionalCollaborators()
    {
        var handler = new DataTestExtension();
        var options = ConnectionExtensions(
            [handler],
            [Negotiation("x-test")]);
        options.HasSelectedSubprotocol = true;
        options.SelectedSubprotocol = "chat";
        options.HasPerMessageDeflate = true;
        options.MaximumFramePayloadLength = 125;
        options.MaximumMessagePayloadLength = 125;
        options.MaximumFragmentCount = 1;
        options.MaximumBufferedInputBytes =
            WebSocketProtocolConstants.MaximumFrameHeaderLength;
        options.MaximumBufferedOutputBytes =
            WebSocketProtocolConstants.MaximumFrameHeaderLength + 125;
        options.AutomaticFramePayloadLength = 125;
        options.Heartbeat.TokenLength =
            WebSocketProtocolConstants.ControlPayloadLengthMaximum;
        options.HasMaskKeySource = true;
        options.MaskKeySource = new DataTestMaskKeySource();

        Assert.DoesNotThrow(() =>
            WebSocketOptionValidator.ValidateConnectionOptions(options));
    }

    [Test]
    public void ConnectionRejectsInvalidVocabularyAndNestedOptionObjects()
    {
        var invalidRole = new WebSocketConnectionOptions();
        invalidRole.Role = (WebSocketRole)2;
        var invalidTransport = new WebSocketConnectionOptions();
        invalidTransport.Transport = (WebSocketHandshakeTransport)3;
        var invalidDelivery = new WebSocketConnectionOptions();
        invalidDelivery.DeliveryMode = (WebSocketMessageDeliveryMode)2;
        var nullProtocol = new WebSocketConnectionOptions();
        nullProtocol.SelectedSubprotocol = null!;
        var nullExtensions = new WebSocketConnectionOptions();
        nullExtensions.Extensions = null!;
        var nullNegotiations = new WebSocketConnectionOptions();
        nullNegotiations.NegotiatedExtensions = null!;
        var nullCompression = new WebSocketConnectionOptions();
        nullCompression.PerMessageDeflate = null!;
        var activeInvalidCompression = new WebSocketConnectionOptions();
        activeInvalidCompression.HasPerMessageDeflate = true;
        activeInvalidCompression.PerMessageDeflate.CompressionThreshold = -1;

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(null));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(invalidRole));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(invalidTransport));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(invalidDelivery));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(nullProtocol));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(nullExtensions));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(nullNegotiations));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(nullCompression));
            Assert.Throws<WebSocketExtensionException>(() => WebSocketOptionValidator.ValidateConnectionOptions(activeInvalidCompression));
        });
    }

    [Test]
    public void ConnectionRejectsMalformedOrConflictingNegotiatedExtensions()
    {
        var missingNegotiation = ConnectionExtensions(
            [new DataTestExtension()],
            new WebSocketExtensionNegotiation[] { null! });
        var wrongStatus = ConnectionExtensions(
            [new DataTestExtension()],
            [Negotiation("x-test")]);
        wrongStatus.NegotiatedExtensions[0].Status =
            WebSocketExtensionStatus.Offered;
        var missingConfiguration = ConnectionExtensions(
            [new DataTestExtension()],
            [Negotiation("x-test")]);
        missingConfiguration.NegotiatedExtensions[0]
            .HasAcceptedConfiguration = false;
        var failure = ConnectionExtensions(
            [new DataTestExtension()],
            [Negotiation("x-test")]);
        failure.NegotiatedExtensions[0].HasError = true;
        var invalidName = ConnectionExtensions(
            [new DataTestExtension()],
            [Negotiation("bad name")]);
        var mismatchedConfiguration = ConnectionExtensions(
            [new DataTestExtension()],
            [Negotiation("x-test")]);
        mismatchedConfiguration.NegotiatedExtensions[0]
            .AcceptedConfiguration.Name = "x-other";
        var invalidParameter = ConnectionExtensions(
            [new DataTestExtension()],
            [Negotiation("x-test")]);
        invalidParameter.NegotiatedExtensions[0]
            .AcceptedConfiguration.Parameters =
        [
            new WebSocketExtensionParameter
            {
                Name = "mode",
                Value = "unflagged"
            }
        ];
        var sharedHandler = new DataTestExtension();
        var duplicateHandler = ConnectionExtensions(
            [sharedHandler, sharedHandler],
            [Negotiation("x-first"), Negotiation("x-second")]);
        var duplicateName = ConnectionExtensions(
            [new DataTestExtension(), new DataTestExtension()],
            [Negotiation("x-test"), Negotiation("x-test")]);
        var conflictingClaim = ConnectionExtensions(
            [new DataTestExtension(), new DataTestExtension()],
            [Negotiation("x-first"), Negotiation("x-second")]);
        conflictingClaim.NegotiatedExtensions[0].ClaimsRsv2 = true;
        conflictingClaim.NegotiatedExtensions[1].ClaimsRsv2 = true;

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(missingNegotiation));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(wrongStatus));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(missingConfiguration));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(failure));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(invalidName));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(mismatchedConfiguration));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(invalidParameter));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(duplicateHandler));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(duplicateName));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(conflictingClaim));
        });
    }

    [Test]
    public void ConnectionRejectsContradictoryPresenceFlagsAndCollaborators()
    {
        var protocolWithoutFlag = new WebSocketConnectionOptions();
        protocolWithoutFlag.SelectedSubprotocol = "chat";
        var emptySelectedProtocol = new WebSocketConnectionOptions();
        emptySelectedProtocol.HasSelectedSubprotocol = true;
        var sourceWithoutFlag = new WebSocketConnectionOptions();
        sourceWithoutFlag.MaskKeySource = new DataTestMaskKeySource();
        var serverMaskSource = new WebSocketConnectionOptions();
        serverMaskSource.Role = WebSocketRole.Server;
        serverMaskSource.HasMaskKeySource = true;
        serverMaskSource.MaskKeySource = new DataTestMaskKeySource();
        var missingHeartbeat = new WebSocketConnectionOptions();
        missingHeartbeat.Heartbeat = null!;
        var nullExtension = new WebSocketConnectionOptions();
        nullExtension.Extensions = new IWebSocketExtension[] { null! };
        var unmatchedNegotiation = new WebSocketConnectionOptions();
        unmatchedNegotiation.Extensions = [new DataTestExtension()];

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(protocolWithoutFlag));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(emptySelectedProtocol));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(sourceWithoutFlag));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(serverMaskSource));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(missingHeartbeat));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(nullExtension));
            Assert.Throws<WebSocketStateException>(() => WebSocketOptionValidator.ValidateConnectionOptions(unmatchedNegotiation));
        });
    }

    [Test]
    public void PerMessageDeflateAcceptsBoundaryWindowBitsAndZeroThreshold()
    {
        var minimum = new PerMessageDeflateOptions();
        minimum.OfferServerMaxWindowBits = true;
        minimum.HasServerMaxWindowBits = true;
        minimum.ServerMaxWindowBits = 8;
        minimum.OfferClientMaxWindowBits = true;
        minimum.HasClientMaxWindowBits = true;
        minimum.ClientMaxWindowBits = 8;
        minimum.OfferServerNoContextTakeover = true;
        minimum.RequireServerNoContextTakeover = true;
        minimum.OfferClientNoContextTakeover = true;
        minimum.RequireClientNoContextTakeover = true;
        minimum.CompressionThreshold = 0;
        minimum.MaximumDecompressedMessagePayloadLength = 1;
        var maximum = new PerMessageDeflateOptions();
        maximum.OfferServerMaxWindowBits = true;
        maximum.HasServerMaxWindowBits = true;
        maximum.ServerMaxWindowBits = 15;
        maximum.OfferClientMaxWindowBits = true;
        maximum.HasClientMaxWindowBits = true;
        maximum.ClientMaxWindowBits = 15;
        maximum.CompressionThreshold = int.MaxValue;
        maximum.MaximumDecompressedMessagePayloadLength = int.MaxValue;

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => WebSocketOptionValidator.ValidatePerMessageDeflateOptions(minimum));
            Assert.DoesNotThrow(() => WebSocketOptionValidator.ValidatePerMessageDeflateOptions(maximum));
        });
    }

    [Test]
    public void PerMessageDeflateRejectsContradictoryOrUnsafeSettings()
    {
        var requiredButNotOffered = new PerMessageDeflateOptions();
        requiredButNotOffered.RequireClientNoContextTakeover = true;
        var serverRequiredButNotOffered = new PerMessageDeflateOptions();
        serverRequiredButNotOffered.RequireServerNoContextTakeover = true;
        var valuedButNotOffered = new PerMessageDeflateOptions();
        valuedButNotOffered.HasServerMaxWindowBits = true;
        var clientValuedButNotOffered = new PerMessageDeflateOptions();
        clientValuedButNotOffered.HasClientMaxWindowBits = true;
        var clientWindowTooSmall = new PerMessageDeflateOptions();
        clientWindowTooSmall.OfferClientMaxWindowBits = true;
        clientWindowTooSmall.HasClientMaxWindowBits = true;
        clientWindowTooSmall.ClientMaxWindowBits = 7;
        var clientWindowTooLarge = new PerMessageDeflateOptions();
        clientWindowTooLarge.OfferClientMaxWindowBits = true;
        clientWindowTooLarge.HasClientMaxWindowBits = true;
        clientWindowTooLarge.ClientMaxWindowBits = 16;
        var serverWindowTooSmall = new PerMessageDeflateOptions();
        serverWindowTooSmall.OfferServerMaxWindowBits = true;
        serverWindowTooSmall.HasServerMaxWindowBits = true;
        serverWindowTooSmall.ServerMaxWindowBits = 7;
        var serverWindowTooLarge = new PerMessageDeflateOptions();
        serverWindowTooLarge.OfferServerMaxWindowBits = true;
        serverWindowTooLarge.HasServerMaxWindowBits = true;
        serverWindowTooLarge.ServerMaxWindowBits = 16;
        var negativeThreshold = new PerMessageDeflateOptions();
        negativeThreshold.CompressionThreshold = -1;
        var zeroDecompressedLimit = new PerMessageDeflateOptions();
        zeroDecompressedLimit.MaximumDecompressedMessagePayloadLength = 0;
        var negativeDecompressedLimit = new PerMessageDeflateOptions();
        negativeDecompressedLimit.MaximumDecompressedMessagePayloadLength = -1;

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketExtensionException>(() => WebSocketOptionValidator.ValidatePerMessageDeflateOptions(null));
            Assert.Throws<WebSocketExtensionException>(() => WebSocketOptionValidator.ValidatePerMessageDeflateOptions(requiredButNotOffered));
            Assert.Throws<WebSocketExtensionException>(() => WebSocketOptionValidator.ValidatePerMessageDeflateOptions(serverRequiredButNotOffered));
            Assert.Throws<WebSocketExtensionException>(() => WebSocketOptionValidator.ValidatePerMessageDeflateOptions(valuedButNotOffered));
            Assert.Throws<WebSocketExtensionException>(() => WebSocketOptionValidator.ValidatePerMessageDeflateOptions(clientValuedButNotOffered));
            Assert.Throws<WebSocketExtensionException>(() => WebSocketOptionValidator.ValidatePerMessageDeflateOptions(clientWindowTooSmall));
            Assert.Throws<WebSocketExtensionException>(() => WebSocketOptionValidator.ValidatePerMessageDeflateOptions(clientWindowTooLarge));
            Assert.Throws<WebSocketExtensionException>(() => WebSocketOptionValidator.ValidatePerMessageDeflateOptions(serverWindowTooSmall));
            Assert.Throws<WebSocketExtensionException>(() => WebSocketOptionValidator.ValidatePerMessageDeflateOptions(serverWindowTooLarge));
            Assert.Throws<WebSocketExtensionException>(() => WebSocketOptionValidator.ValidatePerMessageDeflateOptions(negativeThreshold));
            Assert.Throws<WebSocketExtensionException>(() => WebSocketOptionValidator.ValidatePerMessageDeflateOptions(zeroDecompressedLimit));
            Assert.Throws<WebSocketExtensionException>(() => WebSocketOptionValidator.ValidatePerMessageDeflateOptions(negativeDecompressedLimit));
        });
    }

    [Test]
    public void ClientExtendedConnectRequiresPeerSetting()
    {
        var options = new WebSocketClientHandshakeOptions();
        options.Uri = "wss://example.com/chat";
        options.Transport = WebSocketHandshakeTransport.Http2ExtendedConnect;

        Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(options));

        options.PeerEnabledExtendedConnect = true;

        Assert.DoesNotThrow(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(options));
    }

    [Test]
    public void ClientRejectsContradictoryOptionalTextAndInvalidHeaderLimits()
    {
        var originWithoutFlag = new WebSocketClientHandshakeOptions();
        originWithoutFlag.Uri = "ws://example.com";
        originWithoutFlag.Origin = "https://example.com";
        var emptyOrigin = new WebSocketClientHandshakeOptions();
        emptyOrigin.Uri = "ws://example.com";
        emptyOrigin.HasOrigin = true;
        var lineExceedsTotal = new WebSocketClientHandshakeOptions();
        lineExceedsTotal.Uri = "ws://example.com";
        lineExceedsTotal.MaximumLineLength = lineExceedsTotal.MaximumHeaderBytes + 1;
        var nullProtocol = new WebSocketClientHandshakeOptions();
        nullProtocol.Uri = "ws://example.com";
        nullProtocol.Subprotocols = new string[] { null! };

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(originWithoutFlag));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(emptyOrigin));
            Assert.Throws<WebSocketLimitException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(lineExceedsTotal));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(nullProtocol));
        });
    }

    [Test]
    public void ClientRejectsInvalidVocabularyCollaboratorsAndCollections()
    {
        var nullUri = ClientOptions();
        nullUri.Uri = null!;
        var emptyUri = ClientOptions();
        emptyUri.Uri = "";
        var invalidTransport = ClientOptions();
        invalidTransport.Transport = (WebSocketHandshakeTransport)3;
        var nullOrigin = ClientOptions();
        nullOrigin.Origin = null!;
        var invalidOrigin = ClientOptions();
        invalidOrigin.HasOrigin = true;
        invalidOrigin.Origin = "not-an-origin";
        var emptyUserAgent = ClientOptions();
        emptyUserAgent.HasUserAgent = true;
        var unflaggedUserAgent = ClientOptions();
        unflaggedUserAgent.UserAgent = "agent";
        var nullProtocols = ClientOptions();
        nullProtocols.Subprotocols = null!;
        var nullOffers = ClientOptions();
        nullOffers.Extensions = null!;
        var nullHandlers = ClientOptions();
        nullHandlers.ExtensionHandlers = null!;
        var nullHandler = ClientOptions();
        nullHandler.ExtensionHandlers = new IWebSocketExtension[] { null! };
        var invalidParameter = ClientOptions();
        invalidParameter.Extensions =
        [
            new WebSocketExtensionOffer
            {
                Name = "x-test",
                Parameters =
                [
                    new WebSocketExtensionParameter
                    {
                        Name = "mode",
                        Value = "unflagged"
                    }
                ]
            }
        ];
        var nullFields = ClientOptions();
        nullFields.ExtraFields = null!;
        var unsafeField = ClientOptions();
        unsafeField.ExtraFields =
        [
            new WebSocketHeaderField
            {
                Name = "X-Test",
                Value = " leading"
            }
        ];

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(null));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(nullUri));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(emptyUri));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(invalidTransport));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(nullOrigin));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(invalidOrigin));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(emptyUserAgent));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(unflaggedUserAgent));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(nullProtocols));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(nullOffers));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(nullHandlers));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(nullHandler));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(invalidParameter));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(nullFields));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateClientHandshakeOptions(unsafeField));
        });
    }

    [Test]
    public void ServerExtendedConnectRequiresEnablementAndUniqueTransports()
    {
        var disabled = new WebSocketServerHandshakeOptions();
        disabled.SupportedTransports = [WebSocketHandshakeTransport.Http2ExtendedConnect];
        var duplicate = new WebSocketServerHandshakeOptions();
        duplicate.SupportedTransports =
        [
            WebSocketHandshakeTransport.Http1Upgrade,
            WebSocketHandshakeTransport.Http1Upgrade
        ];

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(disabled));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(duplicate));
        });

        disabled.ExtendedConnectEnabled = true;

        Assert.DoesNotThrow(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(disabled));
    }

    [Test]
    public void ServerRejectsContradictoryPoliciesAndRejectionMetadata()
    {
        var missingPolicy = new WebSocketServerHandshakeOptions();
        missingPolicy.HasOriginPolicy = true;
        var unflaggedPolicy = new WebSocketServerHandshakeOptions();
        unflaggedPolicy.OriginPolicy = new TestOriginPolicy();
        var missingSelector = new WebSocketServerHandshakeOptions();
        missingSelector.HasSubprotocolSelector = true;
        var unflaggedSelector = new WebSocketServerHandshakeOptions();
        unflaggedSelector.SubprotocolSelector = new TestSubprotocolSelector();
        var statusTooSmall = new WebSocketServerHandshakeOptions();
        statusTooSmall.RejectionStatusCode = 399;
        var statusTooLarge = new WebSocketServerHandshakeOptions();
        statusTooLarge.RejectionStatusCode = 600;
        var nullBody = new WebSocketServerHandshakeOptions();
        nullBody.RejectionBody = null!;
        var unflaggedBody = new WebSocketServerHandshakeOptions();
        unflaggedBody.RejectionBody = new byte[] { 1 };

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(missingPolicy));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(unflaggedPolicy));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(missingSelector));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(unflaggedSelector));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(statusTooSmall));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(statusTooLarge));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(nullBody));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(unflaggedBody));
        });
    }

    [TestCase(400)]
    [TestCase(599)]
    public void ServerAcceptsExactRejectionStatusAndOptionalBoundaries(
        int statusCode)
    {
        var options = new WebSocketServerHandshakeOptions();
        options.RejectionStatusCode = statusCode;
        options.HasOriginPolicy = true;
        options.OriginPolicy = new TestOriginPolicy();
        options.HasSubprotocolSelector = true;
        options.SubprotocolSelector = new TestSubprotocolSelector();
        options.HasRejectionBody = true;
        options.RejectionBody = [];

        Assert.DoesNotThrow(() =>
            WebSocketOptionValidator.ValidateServerHandshakeOptions(options));
    }

    [Test]
    public void ServerRejectsInvalidVocabularyAndCollections()
    {
        var nullTransports = new WebSocketServerHandshakeOptions();
        nullTransports.SupportedTransports = null!;
        var emptyTransports = new WebSocketServerHandshakeOptions();
        emptyTransports.SupportedTransports = [];
        var invalidTransport = new WebSocketServerHandshakeOptions();
        invalidTransport.SupportedTransports =
            [(WebSocketHandshakeTransport)3];
        var nullProtocols = new WebSocketServerHandshakeOptions();
        nullProtocols.SupportedSubprotocols = null!;
        var nullOffers = new WebSocketServerHandshakeOptions();
        nullOffers.SupportedExtensions = null!;
        var nullHandlers = new WebSocketServerHandshakeOptions();
        nullHandlers.ExtensionHandlers = null!;
        var nullHandler = new WebSocketServerHandshakeOptions();
        nullHandler.ExtensionHandlers =
            new IWebSocketExtension[] { null! };
        var nullExtraFields = new WebSocketServerHandshakeOptions();
        nullExtraFields.ExtraFields = null!;
        var nullRejectionFields = new WebSocketServerHandshakeOptions();
        nullRejectionFields.RejectionFields = null!;
        var invalidOffer = new WebSocketServerHandshakeOptions();
        invalidOffer.SupportedExtensions =
            [new WebSocketExtensionOffer { Name = "bad name" }];
        var unsafeField = new WebSocketServerHandshakeOptions();
        unsafeField.RejectionFields =
        [
            new WebSocketHeaderField
            {
                Name = "Bad Name",
                Value = "value"
            }
        ];

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(null));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(nullTransports));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(emptyTransports));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(invalidTransport));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(nullProtocols));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(nullOffers));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(nullHandlers));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(nullHandler));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(nullExtraFields));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(nullRejectionFields));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(invalidOffer));
            Assert.Throws<WebSocketHandshakeException>(() => WebSocketOptionValidator.ValidateServerHandshakeOptions(unsafeField));
        });
    }

    static WebSocketConnectionOptions ConnectionExtensions(
        IWebSocketExtension[] handlers,
        WebSocketExtensionNegotiation[] negotiations)
    {
        var options = new WebSocketConnectionOptions();
        options.Extensions = handlers;
        options.NegotiatedExtensions = negotiations;
        return options;
    }

    static WebSocketClientHandshakeOptions ClientOptions()
    {
        return new WebSocketClientHandshakeOptions
        {
            Uri = "wss://example.com/chat"
        };
    }

    static WebSocketExtensionNegotiation Negotiation(string name)
    {
        return new WebSocketExtensionNegotiation
        {
            Name = name,
            Status = WebSocketExtensionStatus.Negotiated,
            HasAcceptedConfiguration = true,
            AcceptedConfiguration = new WebSocketExtensionOffer
            {
                Name = name
            }
        };
    }
}
