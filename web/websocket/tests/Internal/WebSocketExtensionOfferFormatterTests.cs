using websocket;

namespace websocket.Tests;

public class WebSocketExtensionOfferFormatterTests
{
    [Test]
    public void FormatsOrderedOffersAndParametersCanonically()
    {
        var offers = new[]
        {
            Offer(
                "permessage-deflate",
                Parameter("client_no_context_takeover", null),
                Parameter("client_max_window_bits", "12")),
            Offer(
                "x-websocket-test",
                Parameter("mode", "fast"),
                Parameter("flag", null))
        };

        var result = WebSocketExtensionOfferFormatter.FormatOffers(offers);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Value, Is.EqualTo(
                "permessage-deflate; client_no_context_takeover; client_max_window_bits=12, " +
                "x-websocket-test; mode=fast; flag"));
        });
    }

    [Test]
    public void EmptyOffersProduceAnEmptyCompleteValue()
    {
        var result = WebSocketExtensionOfferFormatter.FormatOffers([]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Value, Is.Empty);
        });
    }

    [Test]
    public void PreservesAlternativeAndDuplicateParameterOrder()
    {
        var result = WebSocketExtensionOfferFormatter.FormatOffers(
        [
            Offer("x-test", Parameter("mode", "one"), Parameter("mode", "two")),
            Offer("x-test", Parameter("mode", "three"))
        ]);

        Assert.That(result.Value,
            Is.EqualTo("x-test; mode=one; mode=two, x-test; mode=three"));
    }

    [Test]
    public void RejectsInvalidOfferAndParameterTokens()
    {
        var invalidName = WebSocketExtensionOfferFormatter.FormatOffers(
            [Offer("bad name")]);
        var invalidParameter = WebSocketExtensionOfferFormatter.FormatOffers(
            [Offer("x-test", Parameter("bad name", null))]);
        var invalidValue = WebSocketExtensionOfferFormatter.FormatOffers(
            [Offer("x-test", Parameter("mode", "bad value"))]);
        var inconsistentValue = new WebSocketExtensionParameter();
        inconsistentValue.Name = "mode";
        inconsistentValue.Value = "fast";
        var inconsistent = WebSocketExtensionOfferFormatter.FormatOffers(
            [Offer("x-test", inconsistentValue)]);
        var missing = WebSocketExtensionOfferFormatter.FormatOffers(null);

        Assert.Multiple(() =>
        {
            Assert.That(invalidName.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(invalidParameter.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(invalidValue.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(inconsistent.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(missing.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
        });
    }

    static WebSocketExtensionOffer Offer(
        string name,
        params WebSocketExtensionParameter[] parameters)
    {
        return new WebSocketExtensionOffer
        {
            Name = name,
            Parameters = parameters
        };
    }

    static WebSocketExtensionParameter Parameter(string name, string? value)
    {
        return new WebSocketExtensionParameter
        {
            Name = name,
            HasValue = value != null,
            Value = value ?? ""
        };
    }
}
