using websocket;

namespace websocket.Tests;

public class WebSocketExtensionResponseParserTests
{
    [Test]
    public void ParsesOrderedExtensionsTokenParametersAndQuotedTokens()
    {
        var result = WebSocketExtensionResponseParser.ParseResponse(
            "permessage-deflate; client_no_context_takeover; client_max_window_bits=12, " +
            "x-test; mode=\"fast\"",
            2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Extensions.Select(extension => extension.Name),
                Is.EqualTo(new[] { "permessage-deflate", "x-test" }));
            Assert.That(result.Extensions[0].Parameters.Select(parameter => parameter.Name),
                Is.EqualTo(new[] { "client_no_context_takeover", "client_max_window_bits" }));
            Assert.That(result.Extensions[0].Parameters[0].HasValue, Is.False);
            Assert.That(result.Extensions[0].Parameters[1].HasValue, Is.True);
            Assert.That(result.Extensions[0].Parameters[1].Value, Is.EqualTo("12"));
            Assert.That(result.Extensions[1].Parameters[0].Value, Is.EqualTo("fast"));
        });
    }

    [Test]
    public void PreservesRepeatedNamesAndParametersForNegotiationValidation()
    {
        var result = WebSocketExtensionResponseParser.ParseResponse(
            "x-test; mode=one; mode=two, x-test; mode=three",
            2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Extensions, Has.Length.EqualTo(2));
            Assert.That(result.Extensions[0].Parameters.Select(parameter => parameter.Value),
                Is.EqualTo(new[] { "one", "two" }));
            Assert.That(result.Extensions[1].Parameters[0].Value, Is.EqualTo("three"));
        });
    }

    [Test]
    public void ParsesRepeatedFieldsAlternativesWhitespaceAndEscapedQuotedTokens()
    {
        var fields = new[]
        {
            Field("one ; mode = \"fa\\st\" , two; flag"),
            Field("three; level = 7")
        };

        var result = WebSocketExtensionResponseParser.ParseFields(
            fields,
            2,
            3,
            3,
            16,
            64);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Extensions.Select(extension => extension.Name),
                Is.EqualTo(new[] { "one", "two", "three" }));
            Assert.That(result.Extensions[0].Parameters[0].Value, Is.EqualTo("fast"));
            Assert.That(result.Extensions[1].Parameters[0].HasValue, Is.False);
            Assert.That(result.Extensions[2].Parameters[0].Value, Is.EqualTo("7"));
        });
    }

    [Test]
    public void IncrementalStateAcceptsSplitsInsideNamesQuotesEscapesAndFields()
    {
        var parser = new WebSocketExtensionParserState(2, 3, 3, 16, 64);

        var first = parser.ParseChunk("x-te", false, false);
        var second = parser.ParseChunk("st; mode=\"fa\\", false, false);
        var third = parser.ParseChunk("st\", second", true, false);
        var completed = parser.ParseChunk("third; flag", true, true);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(first.ConsumedCount, Is.EqualTo(4));
            Assert.That(second.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(third.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(completed.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(completed.Extensions.Select(extension => extension.Name),
                Is.EqualTo(new[] { "x-test", "second", "third" }));
            Assert.That(completed.Extensions[0].Parameters[0].Value, Is.EqualTo("fast"));
            Assert.That(completed.Extensions[2].Parameters[0].HasValue, Is.False);
        });
    }

    [TestCase("")]
    [TestCase(" , , ")]
    [TestCase("bad name")]
    [TestCase("x-test;")]
    [TestCase("x-test; bad name")]
    [TestCase("x-test; mode=")]
    [TestCase("x-test; mode=bad value")]
    [TestCase("x-test; mode=\"bad value\"")]
    [TestCase("x-test; mode=\"fa\\ st\"")]
    [TestCase("x-test; mode=\"unterminated")]
    [TestCase("x-test / other")]
    public void RejectsEmptyOrMalformedResponses(string value)
    {
        var result = WebSocketExtensionResponseParser.ParseResponse(value, 8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.Extensions, Is.Empty);
        });
    }

    [Test]
    public void AppliesExtensionCountLimitWithoutReturningPartialResults()
    {
        var exceeded = WebSocketExtensionResponseParser.ParseResponse("one, two", 1);
        var invalid = WebSocketExtensionResponseParser.ParseResponse("one", -1);
        var missing = WebSocketExtensionResponseParser.ParseResponse(null!, 1);

        Assert.Multiple(() =>
        {
            Assert.That(exceeded.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(exceeded.Extensions, Is.Empty);
            Assert.That(invalid.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(missing.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
        });
    }

    [TestCase("field")]
    [TestCase("extension")]
    [TestCase("parameter")]
    [TestCase("token")]
    [TestCase("total")]
    public void AppliesEveryConfiguredCountAndLengthLimit(string limit)
    {
        WebSocketExtensionResponseParseResult result;
        if (limit == "field")
        {
            result = WebSocketExtensionResponseParser.ParseFields(
                [Field("one"), Field("two")],
                1,
                2,
                2,
                8,
                16);
        }
        else if (limit == "extension")
        {
            result = WebSocketExtensionResponseParser.ParseFields(
                [Field("one, two")],
                1,
                1,
                2,
                8,
                16);
        }
        else if (limit == "parameter")
        {
            result = WebSocketExtensionResponseParser.ParseFields(
                [Field("one; a=1; b=2")],
                1,
                2,
                1,
                8,
                32);
        }
        else if (limit == "token")
        {
            result = WebSocketExtensionResponseParser.ParseFields(
                [Field("long-name")],
                1,
                2,
                2,
                4,
                16);
        }
        else
        {
            result = WebSocketExtensionResponseParser.ParseFields(
                [Field("one; mode=fast")],
                1,
                2,
                2,
                8,
                8);
        }

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(result.Extensions, Is.Empty);
        });
    }

    [Test]
    public void AcceptsExactCountAndLengthBoundaries()
    {
        const string value = "abcd; mode=fast";

        var result = WebSocketExtensionResponseParser.ParseFields(
            [Field(value)],
            1,
            1,
            1,
            4,
            value.Length);

        Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
    }

    [Test]
    public void RejectsNegativeLimitsEvenWithoutFields()
    {
        var result = WebSocketExtensionResponseParser.ParseFields(
            [],
            -1,
            0,
            0,
            0,
            0);

        Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
    }

    [Test]
    public void LimitFailureIsTerminalAndDoesNotReturnPartialOffers()
    {
        var parser = new WebSocketExtensionParserState(1, 1, 1, 8, 8);
        var rejected = parser.ParseChunk("one, two", true, true);
        var repeated = parser.ParseChunk("three", true, true);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(rejected.Extensions, Is.Empty);
            Assert.That(repeated.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(repeated.Extensions, Is.Empty);
        });
    }

    [Test]
    public void RequiresAFieldBoundaryWhenInputEnds()
    {
        var parser = new WebSocketExtensionParserState(1, 1, 1, 8, 8);

        var result = parser.ParseChunk("one", false, true);

        Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
    }

    static WebSocketHeaderField Field(string value)
    {
        return new WebSocketHeaderField
        {
            Name = "Sec-WebSocket-Extensions",
            Value = value
        };
    }
}
