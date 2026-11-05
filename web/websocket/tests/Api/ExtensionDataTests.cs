using websocket;

namespace websocket.Tests;

public class ExtensionDataTests
{
    [Test]
    public void OfferAndParameterDefaultsUseExplicitValuePresence()
    {
        var offer = new WebSocketExtensionOffer();
        var parameter = new WebSocketExtensionParameter();

        Assert.Multiple(() =>
        {
            Assert.That(offer.Name, Is.Empty);
            Assert.That(offer.Parameters, Is.Empty);
            Assert.That(parameter.Name, Is.Empty);
            Assert.That(parameter.HasValue, Is.False);
            Assert.That(parameter.Value, Is.Empty);
        });

        parameter.Name = "mode";
        parameter.HasValue = true;
        parameter.Value = "fast";
        offer.Name = "x-test";
        offer.Parameters = [parameter];

        Assert.Multiple(() =>
        {
            Assert.That(offer.Name, Is.EqualTo("x-test"));
            Assert.That(offer.Parameters[0], Is.SameAs(parameter));
            Assert.That(parameter.Name, Is.EqualTo("mode"));
            Assert.That(parameter.HasValue, Is.True);
            Assert.That(parameter.Value, Is.EqualTo("fast"));
        });
    }

    [Test]
    public void NegotiationDefaultsContainNoAcceptedConfigurationClaimsOrError()
    {
        var first = new WebSocketExtensionNegotiation();
        var second = new WebSocketExtensionNegotiation();

        Assert.Multiple(() =>
        {
            Assert.That(first.Name, Is.Empty);
            Assert.That(first.Status, Is.EqualTo(WebSocketExtensionStatus.Offered));
            Assert.That(first.HasAcceptedConfiguration, Is.False);
            Assert.That(first.ClaimsRsv1, Is.False);
            Assert.That(first.ClaimsRsv2, Is.False);
            Assert.That(first.ClaimsRsv3, Is.False);
            Assert.That(first.ClaimedOpcodes, Is.Empty);
            Assert.That(first.HasError, Is.False);
            Assert.That(first.AcceptedConfiguration, Is.Not.SameAs(second.AcceptedConfiguration));
            Assert.That(first.Error, Is.Not.SameAs(second.Error));
        });
    }

    [Test]
    public void NegotiationCarriesConfigurationClaimsAndFailureMetadata()
    {
        var configuration = new WebSocketExtensionOffer();
        configuration.Name = "permessage-deflate";
        var opcodes = new[] { WebSocketOpcode.ReservedData3, WebSocketOpcode.ReservedControl11 };
        var error = new WebSocketError();
        error.Kind = WebSocketErrorKind.Compression;
        var negotiation = new WebSocketExtensionNegotiation();
        negotiation.Name = "permessage-deflate";
        negotiation.Status = WebSocketExtensionStatus.Invalid;
        negotiation.HasAcceptedConfiguration = true;
        negotiation.AcceptedConfiguration = configuration;
        negotiation.ClaimsRsv1 = true;
        negotiation.ClaimsRsv2 = false;
        negotiation.ClaimsRsv3 = true;
        negotiation.ClaimedOpcodes = opcodes;
        negotiation.HasError = true;
        negotiation.Error = error;

        Assert.Multiple(() =>
        {
            Assert.That(negotiation.Name, Is.EqualTo("permessage-deflate"));
            Assert.That(negotiation.Status, Is.EqualTo(WebSocketExtensionStatus.Invalid));
            Assert.That(negotiation.HasAcceptedConfiguration, Is.True);
            Assert.That(negotiation.AcceptedConfiguration, Is.SameAs(configuration));
            Assert.That(negotiation.ClaimsRsv1, Is.True);
            Assert.That(negotiation.ClaimsRsv2, Is.False);
            Assert.That(negotiation.ClaimsRsv3, Is.True);
            Assert.That(negotiation.ClaimedOpcodes, Is.SameAs(opcodes));
            Assert.That(negotiation.HasError, Is.True);
            Assert.That(negotiation.Error, Is.SameAs(error));
        });
    }

    [Test]
    public void ExtensionContextCarriesLifecycleAndExplicitLimits()
    {
        var empty = new WebSocketExtensionContext();
        var context = new WebSocketExtensionContext();
        context.Role = WebSocketRole.Server;
        context.Outbound = true;
        context.RequestCompression = true;
        context.Rsv1 = true;
        context.Rsv2 = false;
        context.Rsv3 = true;
        context.HasOpcode = true;
        context.Opcode = WebSocketOpcode.ReservedData5;
        context.MessageType = WebSocketMessageType.Binary;
        context.FirstChunk = true;
        context.FinalChunk = false;
        context.FirstFragment = true;
        context.FinalFragment = false;
        context.MaximumFramePayloadLength = 100;
        context.MaximumMessagePayloadLength = 200;
        context.MaximumDecompressedMessagePayloadLength = 300;
        context.MaximumBufferedOutputBytes = 400;

        Assert.Multiple(() =>
        {
            Assert.That(empty.Role, Is.EqualTo(WebSocketRole.Client));
            Assert.That(empty.Outbound, Is.False);
            Assert.That(empty.RequestCompression, Is.False);
            Assert.That(empty.Rsv1, Is.False);
            Assert.That(empty.Rsv2, Is.False);
            Assert.That(empty.Rsv3, Is.False);
            Assert.That(empty.HasOpcode, Is.False);
            Assert.That(empty.Opcode, Is.EqualTo(WebSocketOpcode.Continuation));
            Assert.That(empty.MessageType, Is.EqualTo(WebSocketMessageType.Text));
            Assert.That(empty.FirstChunk, Is.False);
            Assert.That(empty.FinalChunk, Is.False);
            Assert.That(empty.FirstFragment, Is.False);
            Assert.That(empty.FinalFragment, Is.False);
            Assert.That(empty.MaximumFramePayloadLength,
                Is.EqualTo(WebSocketDefaults.MaximumFramePayloadLength));
            Assert.That(empty.MaximumMessagePayloadLength,
                Is.EqualTo(WebSocketDefaults.MaximumMessagePayloadLength));
            Assert.That(empty.MaximumDecompressedMessagePayloadLength,
                Is.EqualTo(WebSocketDefaults.MaximumDecompressedMessagePayloadLength));
            Assert.That(empty.MaximumBufferedOutputBytes,
                Is.EqualTo(WebSocketDefaults.MaximumBufferedOutputBytes));
            Assert.That(context.Role, Is.EqualTo(WebSocketRole.Server));
            Assert.That(context.Outbound, Is.True);
            Assert.That(context.RequestCompression, Is.True);
            Assert.That(context.Rsv1, Is.True);
            Assert.That(context.Rsv2, Is.False);
            Assert.That(context.Rsv3, Is.True);
            Assert.That(context.HasOpcode, Is.True);
            Assert.That(context.Opcode,
                Is.EqualTo(WebSocketOpcode.ReservedData5));
            Assert.That(context.MessageType, Is.EqualTo(WebSocketMessageType.Binary));
            Assert.That(context.FirstChunk, Is.True);
            Assert.That(context.FinalChunk, Is.False);
            Assert.That(context.FirstFragment, Is.True);
            Assert.That(context.FinalFragment, Is.False);
            Assert.That(context.MaximumFramePayloadLength, Is.EqualTo(100));
            Assert.That(context.MaximumMessagePayloadLength, Is.EqualTo(200));
            Assert.That(context.MaximumDecompressedMessagePayloadLength, Is.EqualTo(300));
            Assert.That(context.MaximumBufferedOutputBytes, Is.EqualTo(400));
        });
    }

    [Test]
    public void TransformResultCarriesConsumedProducedAndRsvMetadata()
    {
        var empty = new WebSocketTransformResult();
        var output = new byte[] { 1, 2, 3, 99 };
        var error = new WebSocketError
        {
            Kind = WebSocketErrorKind.Compression
        };
        var result = new WebSocketTransformResult();
        result.Status = WebSocketParseStatus.Complete;
        result.ConsumedCount = 5;
        result.ProducedCount = 3;
        result.Output = output;
        result.SetRsv1 = true;
        result.SetRsv2 = true;
        result.SetRsv3 = true;
        result.HasError = true;
        result.Error = error;

        Assert.Multiple(() =>
        {
            Assert.That(empty.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(empty.ConsumedCount, Is.Zero);
            Assert.That(empty.ProducedCount, Is.Zero);
            Assert.That(empty.Output, Is.Empty);
            Assert.That(empty.SetRsv1, Is.False);
            Assert.That(empty.SetRsv2, Is.False);
            Assert.That(empty.SetRsv3, Is.False);
            Assert.That(empty.HasError, Is.False);
            Assert.That(empty.Error, Is.Not.SameAs(result.Error));
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(5));
            Assert.That(result.ProducedCount, Is.EqualTo(3));
            Assert.That(result.Output, Is.SameAs(output));
            Assert.That(result.Output.Take(result.ProducedCount), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.SetRsv1, Is.True);
            Assert.That(result.SetRsv2, Is.True);
            Assert.That(result.SetRsv3, Is.True);
            Assert.That(result.HasError, Is.True);
            Assert.That(result.Error, Is.SameAs(error));
        });
    }

    [Test]
    public void PerMessageDeflateDefaultsAreBoundedAndWindowValuesAreExplicit()
    {
        var options = new PerMessageDeflateOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.OfferServerNoContextTakeover, Is.False);
            Assert.That(options.RequireServerNoContextTakeover, Is.False);
            Assert.That(options.OfferClientNoContextTakeover, Is.False);
            Assert.That(options.RequireClientNoContextTakeover, Is.False);
            Assert.That(options.OfferServerMaxWindowBits, Is.False);
            Assert.That(options.HasServerMaxWindowBits, Is.False);
            Assert.That(options.ServerMaxWindowBits, Is.EqualTo(15));
            Assert.That(options.OfferClientMaxWindowBits, Is.False);
            Assert.That(options.HasClientMaxWindowBits, Is.False);
            Assert.That(options.ClientMaxWindowBits, Is.EqualTo(15));
            Assert.That(options.CompressionThreshold, Is.EqualTo(1024));
            Assert.That(options.MaximumDecompressedMessagePayloadLength, Is.EqualTo(WebSocketDefaults.MaximumDecompressedMessagePayloadLength));
        });

        options.OfferServerMaxWindowBits = true;
        options.OfferServerNoContextTakeover = true;
        options.RequireServerNoContextTakeover = true;
        options.OfferClientNoContextTakeover = true;
        options.RequireClientNoContextTakeover = true;
        options.HasServerMaxWindowBits = true;
        options.ServerMaxWindowBits = 12;
        options.OfferClientMaxWindowBits = true;
        options.HasClientMaxWindowBits = true;
        options.ClientMaxWindowBits = 10;
        options.CompressionThreshold = 64;
        options.MaximumDecompressedMessagePayloadLength = 2048;

        Assert.Multiple(() =>
        {
            Assert.That(options.OfferServerNoContextTakeover, Is.True);
            Assert.That(options.RequireServerNoContextTakeover, Is.True);
            Assert.That(options.OfferClientNoContextTakeover, Is.True);
            Assert.That(options.RequireClientNoContextTakeover, Is.True);
            Assert.That(options.OfferServerMaxWindowBits, Is.True);
            Assert.That(options.HasServerMaxWindowBits, Is.True);
            Assert.That(options.ServerMaxWindowBits, Is.EqualTo(12));
            Assert.That(options.HasClientMaxWindowBits, Is.True);
            Assert.That(options.ClientMaxWindowBits, Is.EqualTo(10));
            Assert.That(options.CompressionThreshold, Is.EqualTo(64));
            Assert.That(options.MaximumDecompressedMessagePayloadLength, Is.EqualTo(2048));
        });
    }
}
