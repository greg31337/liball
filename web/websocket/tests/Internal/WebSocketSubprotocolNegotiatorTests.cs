using websocket;

namespace websocket.Tests;

public class WebSocketSubprotocolNegotiatorTests
{
    [Test]
    public void ParsesRepeatedOfferFieldsInWireOrder()
    {
        var fields = new[]
        {
            Field("Sec-WebSocket-Protocol", "chat, superchat"),
            Field("X-Other", "ignored"),
            Field("sec-websocket-protocol", "binary.v2, mqtt")
        };

        var result = WebSocketSubprotocolNegotiator.ParseClientOffers(fields, 4);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.FieldCount, Is.EqualTo(2));
            Assert.That(result.Protocols,
                Is.EqualTo(new[] { "chat", "superchat", "binary.v2", "mqtt" }));
        });
    }

    [Test]
    public void AbsentOfferFieldProducesAnEmptyValidOffer()
    {
        var result = WebSocketSubprotocolNegotiator.ParseClientOffers(
            [Field("Host", "example.com")],
            8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.FieldCount, Is.Zero);
            Assert.That(result.Protocols, Is.Empty);
        });
    }

    [TestCase("")]
    [TestCase(" \t")]
    [TestCase(",, \t,")]
    [TestCase("chat; superchat")]
    [TestCase("chat / superchat")]
    [TestCase("chat, caf\u00e9")]
    public void RejectsPresentEmptyOrMalformedOfferFields(string value)
    {
        var result = WebSocketSubprotocolNegotiator.ParseClientOffers(
            [Field("Sec-WebSocket-Protocol", value)],
            8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.FieldCount, Is.EqualTo(1));
            Assert.That(result.Protocols, Is.Empty);
        });
    }

    [Test]
    public void IgnoresEmptyListMembersAroundValidOffers()
    {
        var result = WebSocketSubprotocolNegotiator.ParseClientOffers(
            [Field("Sec-WebSocket-Protocol", ", chat, , superchat,")],
            8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Protocols, Is.EqualTo(new[] { "chat", "superchat" }));
        });
    }

    [Test]
    public void RejectsExactDuplicateOffersAcrossRepeatedFields()
    {
        var duplicate = WebSocketSubprotocolNegotiator.ParseClientOffers(
            [
                Field("Sec-WebSocket-Protocol", "chat"),
                Field("Sec-WebSocket-Protocol", "superchat, chat")
            ],
            8);
        var differentCase = WebSocketSubprotocolNegotiator.ParseClientOffers(
            [Field("Sec-WebSocket-Protocol", "Chat, chat")],
            8);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(duplicate.Protocols, Is.Empty);
            Assert.That(differentCase.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(differentCase.Protocols, Is.EqualTo(new[] { "Chat", "chat" }));
        });
    }

    [Test]
    public void AppliesOneOfferLimitAcrossRepeatedFields()
    {
        var result = WebSocketSubprotocolNegotiator.ParseClientOffers(
            [
                Field("Sec-WebSocket-Protocol", "one, two"),
                Field("Sec-WebSocket-Protocol", "three")
            ],
            2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(result.FieldCount, Is.EqualTo(2));
            Assert.That(result.Protocols, Is.Empty);
        });
    }

    [Test]
    public void NegotiationAcceptsASelectorResultOfferedAndSupportedExactly()
    {
        var selector = new FixedSubprotocolSelector("chat");

        var result = WebSocketSubprotocolNegotiator.Choose(
            ["chat", "superchat"],
            ["superchat", "chat"],
            selector);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.HasSelection, Is.True);
            Assert.That(result.SelectedProtocol, Is.EqualTo("chat"));
        });
    }

    [TestCase("unoffered")]
    [TestCase("Chat")]
    [TestCase("bad value")]
    public void NegotiationRejectsUnofferedCaseMismatchedOrInvalidSelections(string selected)
    {
        var result = WebSocketSubprotocolNegotiator.Choose(
            ["chat"],
            ["chat", "unoffered"],
            new FixedSubprotocolSelector(selected));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.HasSelection, Is.False);
            Assert.That(result.SelectedProtocol, Is.Empty);
        });
    }

    [Test]
    public void NegotiationAllowsAnExplicitNoSelectionResult()
    {
        var result = WebSocketSubprotocolNegotiator.Choose(
            ["chat"],
            ["chat"],
            new FixedSubprotocolSelector(""));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.HasSelection, Is.False);
            Assert.That(result.SelectedProtocol, Is.Empty);
        });
    }

    [Test]
    public void DefaultNegotiationTreatsNoOverlapAsAValidNoSelection()
    {
        var clientOrder = WebSocketSubprotocolNegotiator.Choose(
            ["chat", "superchat"],
            ["mqtt", "binary"],
            new DefaultWebSocketSubprotocolSelector(false));
        var serverOrder = WebSocketSubprotocolNegotiator.Choose(
            ["chat", "superchat"],
            ["mqtt", "binary"],
            new DefaultWebSocketSubprotocolSelector(true));

        Assert.Multiple(() =>
        {
            Assert.That(clientOrder.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(clientOrder.HasSelection, Is.False);
            Assert.That(serverOrder.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(serverOrder.HasSelection, Is.False);
        });
    }

    [Test]
    public void CustomSelectorReceivesCopiesAndCannotMutateNegotiationInputs()
    {
        var clientProtocols = new[] { "chat", "superchat" };
        var serverProtocols = new[] { "superchat", "chat" };
        var selector = new MutatingSubprotocolSelector("chat");

        var result = WebSocketSubprotocolNegotiator.Choose(
            clientProtocols,
            serverProtocols,
            selector);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.SelectedProtocol, Is.EqualTo("chat"));
            Assert.That(clientProtocols, Is.EqualTo(new[] { "chat", "superchat" }));
            Assert.That(serverProtocols, Is.EqualTo(new[] { "superchat", "chat" }));
            Assert.That(selector.ClientProtocols, Is.Not.SameAs(clientProtocols));
            Assert.That(selector.ServerProtocols, Is.Not.SameAs(serverProtocols));
        });
    }

    [Test]
    public void NullSelectorOrNullSelectionCannotSatisfyRequiredSelection()
    {
        var missingSelector = WebSocketSubprotocolNegotiator.Choose(
            ["chat"],
            ["chat"],
            null);
        var nullSelection = WebSocketSubprotocolNegotiator.Choose(
            ["chat"],
            ["chat"],
            new NullSubprotocolSelector());

        Assert.Multiple(() =>
        {
            Assert.That(missingSelector.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(missingSelector.HasSelection, Is.False);
            Assert.That(nullSelection.Status,
                Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(nullSelection.HasSelection, Is.False);
        });
    }

    [Test]
    public void ConfiguredProtocolArraysRequireTokensWithoutExactDuplicates()
    {
        var invalidClient = new WebSocketClientHandshakeOptions();
        invalidClient.Uri = "ws://example.com/";
        invalidClient.Subprotocols = ["chat", "chat"];
        var invalidServer = new WebSocketServerHandshakeOptions();
        invalidServer.SupportedSubprotocols = ["bad value"];
        var caseDistinct = new WebSocketServerHandshakeOptions();
        caseDistinct.SupportedSubprotocols = ["Chat", "chat"];

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketHandshakeException>(() =>
                WebSocketOptionValidator.ValidateClientHandshakeOptions(invalidClient));
            Assert.Throws<WebSocketHandshakeException>(() =>
                WebSocketOptionValidator.ValidateServerHandshakeOptions(invalidServer));
            Assert.DoesNotThrow(() =>
                WebSocketOptionValidator.ValidateServerHandshakeOptions(caseDistinct));
        });
    }

    static WebSocketHeaderField Field(string name, string value)
    {
        return new WebSocketHeaderField
        {
            Name = name,
            Value = value
        };
    }
}

internal class FixedSubprotocolSelector : IWebSocketSubprotocolSelector
{
    internal string Selected;

    internal FixedSubprotocolSelector(string selected)
    {
        Selected = selected;
    }

    public string SelectSubprotocol(string[] clientProtocols, string[] serverProtocols)
    {
        return Selected;
    }
}

internal class MutatingSubprotocolSelector : IWebSocketSubprotocolSelector
{
    readonly string selection;

    internal string[] ClientProtocols = [];
    internal string[] ServerProtocols = [];

    internal MutatingSubprotocolSelector(string selected)
    {
        selection = selected;
    }

    public string SelectSubprotocol(string[] clientProtocols, string[] serverProtocols)
    {
        ClientProtocols = clientProtocols;
        ServerProtocols = serverProtocols;
        clientProtocols[0] = "mutated-client";
        serverProtocols[0] = "mutated-server";
        return selection;
    }
}

internal class NullSubprotocolSelector : IWebSocketSubprotocolSelector
{
    public string SelectSubprotocol(string[] clientProtocols, string[] serverProtocols)
    {
        return null!;
    }
}
