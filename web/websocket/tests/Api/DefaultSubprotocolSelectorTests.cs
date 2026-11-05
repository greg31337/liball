using websocket;

namespace websocket.Tests;

public class DefaultSubprotocolSelectorTests
{
    [Test]
    public void ClientOrderChoosesFirstClientPreferenceSupportedByServer()
    {
        var selector = new DefaultWebSocketSubprotocolSelector(false);

        var selected = selector.SelectSubprotocol(
            ["chat", "superchat", "binary"],
            ["superchat", "chat"]);

        Assert.That(selected, Is.EqualTo("chat"));
    }

    [Test]
    public void ServerOrderChoosesFirstServerPreferenceOfferedByClient()
    {
        var selector = new DefaultWebSocketSubprotocolSelector(true);

        var selected = selector.SelectSubprotocol(
            ["chat", "superchat", "binary"],
            ["superchat", "chat"]);

        Assert.That(selected, Is.EqualTo("superchat"));
    }

    [Test]
    public void MatchingIsCaseSensitive()
    {
        var clientOrder = new DefaultWebSocketSubprotocolSelector(false);
        var serverOrder = new DefaultWebSocketSubprotocolSelector(true);

        var clientSelected = clientOrder.SelectSubprotocol(["Chat", "chat"], ["chat"]);
        var serverSelected = serverOrder.SelectSubprotocol(["Chat"], ["chat"]);

        Assert.Multiple(() =>
        {
            Assert.That(clientSelected, Is.EqualTo("chat"));
            Assert.That(serverSelected, Is.Empty);
        });
    }

    [Test]
    public void NoCommonProtocolReturnsNoSelection()
    {
        var selector = new DefaultWebSocketSubprotocolSelector(false);

        var selected = selector.SelectSubprotocol(["chat"], ["binary"]);

        Assert.That(selected, Is.Empty);
    }

    [Test]
    public void InvalidOrDuplicateInputsCannotProduceASelection()
    {
        var selector = new DefaultWebSocketSubprotocolSelector(false);

        Assert.Multiple(() =>
        {
            Assert.That(selector.SelectSubprotocol(["chat", "chat"], ["chat"]), Is.Empty);
            Assert.That(selector.SelectSubprotocol(["bad value"], ["bad value"]), Is.Empty);
            Assert.That(selector.SelectSubprotocol(null!, ["chat"]), Is.Empty);
            Assert.That(selector.SelectSubprotocol(["chat"], null!), Is.Empty);
        });
    }

    [Test]
    public void EveryAllowedTokenCharacterCanParticipateInAProtocolName()
    {
        const string token =
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!#$%&'*+-.^_`|~";
        var clientOrder = new DefaultWebSocketSubprotocolSelector(false);
        var serverOrder = new DefaultWebSocketSubprotocolSelector(true);

        Assert.Multiple(() =>
        {
            Assert.That(clientOrder.SelectSubprotocol([token, "other"], ["other", token]),
                Is.EqualTo(token));
            Assert.That(serverOrder.SelectSubprotocol([token, "other"], ["other", token]),
                Is.EqualTo("other"));
        });
    }

    [Test]
    public void EmptySidesAndCaseOnlyMatchesProduceNoSelection()
    {
        var clientOrder = new DefaultWebSocketSubprotocolSelector(false);
        var serverOrder = new DefaultWebSocketSubprotocolSelector(true);

        Assert.Multiple(() =>
        {
            Assert.That(clientOrder.SelectSubprotocol([], ["chat"]), Is.Empty);
            Assert.That(clientOrder.SelectSubprotocol(["chat"], []), Is.Empty);
            Assert.That(serverOrder.SelectSubprotocol([], []), Is.Empty);
            Assert.That(clientOrder.SelectSubprotocol(["CHAT"], ["chat"]), Is.Empty);
        });
    }
}
