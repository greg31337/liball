using websocket;

namespace websocket.Tests;

public class ExtensionHeaderCodecTests
{
    [TestCase(0, 1, 1, 1, 1)]
    [TestCase(-1, 1, 1, 1, 1)]
    [TestCase(1, 0, 1, 1, 1)]
    [TestCase(1, 1, 0, 1, 1)]
    [TestCase(1, 1, 1, 0, 1)]
    [TestCase(1, 1, 1, 1, 0)]
    [TestCase(1, 1, 1, 2, 1)]
    public void ConstructorRejectsInvalidLimits(
        int fields,
        int extensions,
        int parameters,
        int tokenLength,
        int totalLength)
    {
        Assert.Throws<WebSocketLimitException>(() =>
            new WebSocketExtensionHeaderCodec(
                fields,
                extensions,
                parameters,
                tokenLength,
                totalLength));
    }

    [Test]
    public void ParsesRepeatedFieldsAndPreservesAlternativeAndParameterOrder()
    {
        IWebSocketExtensionHeaderCodec codec = CreateCodec();
        var fields = new[]
        {
            Field("X-Ignored", "value"),
            Field("Sec-WebSocket-Extensions", "x-test; mode=one; mode=two, y-test"),
            Field("sec-websocket-extensions", "x-test; mode=three")
        };

        var offers = codec.ParseExtensionFields(fields);

        Assert.Multiple(() =>
        {
            Assert.That(offers.Select(offer => offer.Name),
                Is.EqualTo(new[] { "x-test", "y-test", "x-test" }));
            Assert.That(offers[0].Parameters.Select(parameter => parameter.Value),
                Is.EqualTo(new[] { "one", "two" }));
            Assert.That(offers[2].Parameters[0].Value, Is.EqualTo("three"));
        });
    }

    [Test]
    public void ParsesWhitespaceQuotedTokensAndQuotedPairEscapes()
    {
        IWebSocketExtensionHeaderCodec codec = CreateCodec();

        var offers = codec.ParseExtensionFields(
        [
            Field("Sec-WebSocket-Extensions", " one ; mode = \"fa\\st\" ; flag ")
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(offers, Has.Length.EqualTo(1));
            Assert.That(offers[0].Name, Is.EqualTo("one"));
            Assert.That(offers[0].Parameters[0].HasValue, Is.True);
            Assert.That(offers[0].Parameters[0].Value, Is.EqualTo("fast"));
            Assert.That(offers[0].Parameters[1].HasValue, Is.False);
        });
    }

    [Test]
    public void NoExtensionFieldsProduceAnEmptyResult()
    {
        IWebSocketExtensionHeaderCodec codec = CreateCodec();

        var offers = codec.ParseExtensionFields([Field("X-Test", "value")]);

        Assert.That(offers, Is.Empty);
    }

    [TestCase("bad name")]
    [TestCase("x-test / other")]
    [TestCase("x-test;")]
    [TestCase("x-test; bad name")]
    [TestCase("x-test; mode=")]
    [TestCase("x-test; mode=bad value")]
    [TestCase("x-test; mode=\"bad value\"")]
    [TestCase("x-test; mode=\"fa\\ st\"")]
    [TestCase("x-test; mode=\"fast\"extra")]
    [TestCase("x-test; mode=\"unterminated")]
    public void MalformedExtensionFieldsThrowExtensionException(string value)
    {
        IWebSocketExtensionHeaderCodec codec = CreateCodec();

        Assert.Throws<WebSocketExtensionException>(() =>
            codec.ParseExtensionFields(
                [Field("Sec-WebSocket-Extensions", value)]));
    }

    [Test]
    public void ParsingMapsEveryConfiguredBoundToLimitException()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketLimitException>(() =>
                new WebSocketExtensionHeaderCodec(1, 2, 2, 16, 64)
                    .ParseExtensionFields(
                    [
                        Field("Sec-WebSocket-Extensions", "one"),
                        Field("Sec-WebSocket-Extensions", "two")
                    ]));
            Assert.Throws<WebSocketLimitException>(() =>
                new WebSocketExtensionHeaderCodec(2, 1, 2, 16, 64)
                    .ParseExtensionFields(
                        [Field("Sec-WebSocket-Extensions", "one, two")]));
            Assert.Throws<WebSocketLimitException>(() =>
                new WebSocketExtensionHeaderCodec(2, 2, 1, 16, 64)
                    .ParseExtensionFields(
                        [Field("Sec-WebSocket-Extensions", "one; a=1; b=2")]));
            Assert.Throws<WebSocketLimitException>(() =>
                new WebSocketExtensionHeaderCodec(2, 2, 2, 4, 64)
                    .ParseExtensionFields(
                        [Field("Sec-WebSocket-Extensions", "long-name")]));
            Assert.Throws<WebSocketLimitException>(() =>
                new WebSocketExtensionHeaderCodec(2, 2, 2, 8, 8)
                    .ParseExtensionFields(
                        [Field("Sec-WebSocket-Extensions", "one; mode=fast")]));
        });
    }

    [Test]
    public void RepeatedFieldsAreEquivalentToOneCombinedField()
    {
        IWebSocketExtensionHeaderCodec codec = CreateCodec();
        var repeated = codec.ParseExtensionFields(
        [
            Field("Sec-WebSocket-Extensions", "one; mode=fast, two; flag"),
            Field("Sec-WebSocket-Extensions", "three; level=7")
        ]);
        var combined = codec.ParseExtensionFields(
        [
            Field(
                "Sec-WebSocket-Extensions",
                "one; mode=fast, two; flag, three; level=7")
        ]);
        char[] repeatedValue = new char[codec.MeasureExtensionFields(repeated)];
        char[] combinedValue = new char[codec.MeasureExtensionFields(combined)];

        int repeatedLength = codec.WriteExtensionFields(repeated, repeatedValue);
        int combinedLength = codec.WriteExtensionFields(combined, combinedValue);

        Assert.Multiple(() =>
        {
            Assert.That(repeated.Select(offer => offer.Name),
                Is.EqualTo(new[] { "one", "two", "three" }));
            Assert.That(repeated[1].Parameters[0].HasValue, Is.False);
            Assert.That(repeatedLength, Is.EqualTo(combinedLength));
            Assert.That(repeatedValue, Is.EqualTo(combinedValue));
        });
    }

    [Test]
    public void ParsingAndWritingAcceptEveryExactConfiguredBoundary()
    {
        const string value = "abcd; mode=fast";
        IWebSocketExtensionHeaderCodec codec =
            new WebSocketExtensionHeaderCodec(1, 1, 1, 4, value.Length);

        var offers = codec.ParseExtensionFields(
            [Field("Sec-WebSocket-Extensions", value)]);
        char[] destination = new char[value.Length];
        int measured = codec.MeasureExtensionFields(offers);
        int written = codec.WriteExtensionFields(offers, destination);

        Assert.Multiple(() =>
        {
            Assert.That(offers, Has.Length.EqualTo(1));
            Assert.That(offers[0].Parameters, Has.Length.EqualTo(1));
            Assert.That(measured, Is.EqualTo(value.Length));
            Assert.That(written, Is.EqualTo(value.Length));
            Assert.That(new string(destination), Is.EqualTo(value));
        });
    }

    [Test]
    public void MeasurementAndWritingProduceTheSameCanonicalValue()
    {
        IWebSocketExtensionHeaderCodec codec = CreateCodec();
        var offers = new[]
        {
            Offer("x-test", Parameter("mode", "fast"), Parameter("flag", null)),
            Offer("x-test", Parameter("mode", "slow"))
        };
        const string expected = "x-test; mode=fast; flag, x-test; mode=slow";
        var destination = Enumerable.Repeat('?', expected.Length + 2).ToArray();

        var measured = codec.MeasureExtensionFields(offers);
        var written = codec.WriteExtensionFields(offers, destination);

        Assert.Multiple(() =>
        {
            Assert.That(measured, Is.EqualTo(expected.Length));
            Assert.That(written, Is.EqualTo(measured));
            Assert.That(new string(destination, 0, written), Is.EqualTo(expected));
            Assert.That(destination[written], Is.EqualTo('?'));
            Assert.That(destination[written + 1], Is.EqualTo('?'));
        });
    }

    [Test]
    public void QuotedInputRoundTripsToCanonicalUnquotedOutput()
    {
        IWebSocketExtensionHeaderCodec codec = CreateCodec();
        var offers = codec.ParseExtensionFields(
        [
            Field(
                "Sec-WebSocket-Extensions",
                "x; mode = \"fa\\st\"; mode=slow, y; flag")
        ]);
        var destination = new char[codec.MeasureExtensionFields(offers)];

        var written = codec.WriteExtensionFields(offers, destination);
        var reparsed = codec.ParseExtensionFields(
        [
            Field("Sec-WebSocket-Extensions", new string(destination))
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(new string(destination),
                Is.EqualTo("x; mode=fast; mode=slow, y; flag"));
            Assert.That(written, Is.EqualTo(destination.Length));
            Assert.That(reparsed.Select(offer => offer.Name),
                Is.EqualTo(new[] { "x", "y" }));
            Assert.That(reparsed[0].Parameters.Select(parameter => parameter.Value),
                Is.EqualTo(new[] { "fast", "slow" }));
        });
    }

    [Test]
    public void EmptyOffersMeasureAndWriteWithoutChangingDestination()
    {
        IWebSocketExtensionHeaderCodec codec = CreateCodec();
        char[] destination = ['?'];

        var measured = codec.MeasureExtensionFields([]);
        var written = codec.WriteExtensionFields([], destination);

        Assert.Multiple(() =>
        {
            Assert.That(measured, Is.Zero);
            Assert.That(written, Is.Zero);
            Assert.That(destination, Is.EqualTo(new[] { '?' }));
        });
    }

    [Test]
    public void InsufficientDestinationThrowsWithoutWritingPartialOutput()
    {
        IWebSocketExtensionHeaderCodec codec = CreateCodec();
        var offers = new[] { Offer("x-test", Parameter("mode", "fast")) };
        var destination = Enumerable.Repeat('?', 4).ToArray();

        Assert.Throws<WebSocketLimitException>(() =>
            codec.WriteExtensionFields(offers, destination));
        Assert.That(destination, Is.EqualTo(Enumerable.Repeat('?', 4)));
    }

    [Test]
    public void InvalidOfferMetadataThrowsWithoutWriting()
    {
        IWebSocketExtensionHeaderCodec codec = CreateCodec();
        var badName = Offer("bad name");
        var badParameter = Parameter("mode", null);
        badParameter.Value = "contradiction";
        var destination = Enumerable.Repeat('?', 64).ToArray();

        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketExtensionException>(() =>
                codec.MeasureExtensionFields([badName]));
            Assert.Throws<WebSocketExtensionException>(() =>
                codec.WriteExtensionFields(
                    [Offer("x-test", badParameter)],
                    destination));
            Assert.That(destination, Is.EqualTo(Enumerable.Repeat('?', 64)));
        });
    }

    [Test]
    public void SerializationEnforcesExtensionParameterTokenAndTotalLimits()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<WebSocketLimitException>(() =>
                new WebSocketExtensionHeaderCodec(1, 1, 2, 16, 64)
                    .MeasureExtensionFields([Offer("one"), Offer("two")]));
            Assert.Throws<WebSocketLimitException>(() =>
                new WebSocketExtensionHeaderCodec(1, 2, 1, 16, 64)
                    .MeasureExtensionFields(
                    [
                        Offer("one", Parameter("a", "1"), Parameter("b", "2"))
                    ]));
            Assert.Throws<WebSocketLimitException>(() =>
                new WebSocketExtensionHeaderCodec(1, 2, 2, 4, 64)
                    .MeasureExtensionFields([Offer("long-name")]));
            Assert.Throws<WebSocketLimitException>(() =>
                new WebSocketExtensionHeaderCodec(1, 2, 2, 8, 8)
                    .MeasureExtensionFields(
                        [Offer("one", Parameter("mode", "fast"))]));
        });
    }

    static IWebSocketExtensionHeaderCodec CreateCodec()
    {
        return new WebSocketExtensionHeaderCodec(4, 8, 16, 64, 256);
    }

    static WebSocketHeaderField Field(string name, string value)
    {
        return new WebSocketHeaderField
        {
            Name = name,
            Value = value
        };
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
