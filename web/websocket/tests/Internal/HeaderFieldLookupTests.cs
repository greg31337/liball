using websocket;

namespace websocket.Tests;

public class HeaderFieldLookupTests
{
    [Test]
    public void FindsDuplicateFieldNamesCaseInsensitivelyInOriginalOrder()
    {
        var first = Field("Upgrade", "websocket");
        var unrelated = Field("X-Trace", "one");
        var second = Field("upgrade", "h2c");
        var third = Field("UPGRADE", "WebSocket");
        var fields = new[] { first, unrelated, second, third };

        var result = HeaderFieldLookup.FindFields(fields, "uPgRaDe");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Matches, Has.Length.EqualTo(3));
            Assert.That(result.Matches[0], Is.SameAs(first));
            Assert.That(result.Matches[1], Is.SameAs(second));
            Assert.That(result.Matches[2], Is.SameAs(third));
            Assert.That(result.CombinedValue, Is.Empty);
            Assert.That(fields, Is.EqualTo(new[] { first, unrelated, second, third }));
        });
    }

    [Test]
    public void MissingOrNullCollectionsProduceAnEmptySuccessfulLookup()
    {
        var missing = HeaderFieldLookup.FindFields(
            new[] { Field("Connection", "Upgrade") },
            "Host");
        var nullCollection = HeaderFieldLookup.FindFields(null, "Host");

        Assert.Multiple(() =>
        {
            Assert.That(missing.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(missing.Matches, Is.Empty);
            Assert.That(nullCollection.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(nullCollection.Matches, Is.Empty);
        });
    }

    [Test]
    public void ExplicitListCombinationUsesCommaSpaceAndPreservesEveryValue()
    {
        var fields = new[]
        {
            Field("Connection", "keep-alive, Upgrade"),
            Field("Host", "example.test"),
            Field("connection", "close"),
            Field("CONNECTION", "")
        };

        var result = HeaderFieldLookup.CombineListFields(fields, "Connection");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Matches, Has.Length.EqualTo(3));
            Assert.That(result.CombinedValue, Is.EqualTo("keep-alive, Upgrade, close, "));
            Assert.That(fields[0].Value, Is.EqualTo("keep-alive, Upgrade"));
            Assert.That(fields[2].Value, Is.EqualTo("close"));
            Assert.That(fields[3].Value, Is.Empty);
        });
    }

    [Test]
    public void CombinesOneValueExactlyAndLeavesMissingValueEmpty()
    {
        var single = HeaderFieldLookup.CombineListFields(
            new[] { Field("Upgrade", "websocket") },
            "Upgrade");
        var missing = HeaderFieldLookup.CombineListFields(
            new[] { Field("Host", "example.test") },
            "Upgrade");

        Assert.Multiple(() =>
        {
            Assert.That(single.CombinedValue, Is.EqualTo("websocket"));
            Assert.That(missing.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(missing.CombinedValue, Is.Empty);
        });
    }

    [Test]
    public void LookupRejectsInvalidRequestedOrStoredFields()
    {
        var invalidRequestedName = HeaderFieldLookup.FindFields(
            new[] { Field("Host", "example.test") },
            "bad name");
        var emptyRequestedName = HeaderFieldLookup.FindFields(
            new[] { Field("Host", "example.test") },
            "");
        var nullEntry = HeaderFieldLookup.FindFields(
            new WebSocketHeaderField[] { null! },
            "Host");
        var nullNameField = Field("Host", "value");
        nullNameField.Name = null!;
        var nullName = HeaderFieldLookup.FindFields(
            new[] { nullNameField },
            "Host");
        var nullValueField = Field("Host", "value");
        nullValueField.Value = null!;
        var nullValue = HeaderFieldLookup.FindFields(
            new[] { nullValueField },
            "Host");
        var invalidStoredName = HeaderFieldLookup.FindFields(
            new[] { Field("bad name", "value") },
            "Host");
        var injectedValue = HeaderFieldLookup.FindFields(
            new[] { Field("Host", "safe\r\nInjected: yes") },
            "Host");
        var nonOctetValue = HeaderFieldLookup.FindFields(
            new[] { Field("Host", "\u0100") },
            "Host");

        Assert.Multiple(() =>
        {
            Assert.That(invalidRequestedName.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(emptyRequestedName.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(nullEntry.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(nullName.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(nullValue.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(invalidStoredName.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(injectedValue.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(nonOctetValue.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
        });
    }

    [Test]
    public void ReadsTokensAcrossRepeatedFieldsWithoutRemovingDuplicates()
    {
        var fields = new[]
        {
            Field("Connection", "keep-alive, Upgrade"),
            Field("connection", ", Upgrade,, close,")
        };

        var result = HeaderFieldLookup.ReadTokenValues(fields, "CONNECTION", 4);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.FieldCount, Is.EqualTo(2));
            Assert.That(result.Tokens, Is.EqualTo(new[] { "keep-alive", "Upgrade", "Upgrade", "close" }));
        });
    }

    [Test]
    public void HttpTokenSearchIsAsciiCaseInsensitive()
    {
        var fields = new[]
        {
            Field("Connection", "keep-alive"),
            Field("connection", "UPGRADE")
        };

        var found = HeaderFieldLookup.ContainsHttpToken(fields, "CONNECTION", "upgrade", 4);
        var missing = HeaderFieldLookup.ContainsHttpToken(fields, "connection", "close", 4);

        Assert.Multiple(() =>
        {
            Assert.That(found.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(found.FieldCount, Is.EqualTo(2));
            Assert.That(found.TokenCount, Is.EqualTo(2));
            Assert.That(found.Found, Is.True);
            Assert.That(missing.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(missing.Found, Is.False);
        });
    }

    [Test]
    public void SubprotocolSearchIsCaseSensitiveWhileFieldNameIsNot()
    {
        var fields = new[]
        {
            Field("Sec-WebSocket-Protocol", "Chat, superchat"),
            Field("sec-websocket-protocol", "chat.v2")
        };

        var exact = HeaderFieldLookup.ContainsSubprotocol(
            fields,
            "SEC-WEBSOCKET-PROTOCOL",
            "Chat",
            4);
        var wrongCase = HeaderFieldLookup.ContainsSubprotocol(
            fields,
            "sec-websocket-protocol",
            "chat",
            4);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(exact.Found, Is.True);
            Assert.That(wrongCase.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(wrongCase.Found, Is.False);
        });
    }

    [Test]
    public void TokenReadingAppliesOneLimitAcrossAllRepeatedFields()
    {
        var fields = new[]
        {
            Field("Connection", "one, two"),
            Field("connection", "three")
        };

        var result = HeaderFieldLookup.ReadTokenValues(fields, "Connection", 2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(result.FieldCount, Is.EqualTo(2));
            Assert.That(result.Tokens, Is.Empty);
        });
    }

    [Test]
    public void TokenSearchRejectsMalformedListsCandidatesAndLimits()
    {
        var malformed = HeaderFieldLookup.ContainsHttpToken(
            new[] { Field("Connection", "Upgrade; close") },
            "Connection",
            "Upgrade",
            4);
        var invalidCandidate = HeaderFieldLookup.ContainsHttpToken(
            new[] { Field("Connection", "Upgrade") },
            "Connection",
            "bad value",
            4);
        var invalidLimit = HeaderFieldLookup.ContainsSubprotocol(
            null,
            "Sec-WebSocket-Protocol",
            "chat",
            -1);

        Assert.Multiple(() =>
        {
            Assert.That(malformed.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(malformed.Found, Is.False);
            Assert.That(invalidCandidate.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(invalidCandidate.Found, Is.False);
            Assert.That(invalidLimit.Status, Is.EqualTo(WebSocketParseStatus.LimitRejected));
            Assert.That(invalidLimit.Found, Is.False);
        });
    }

    [Test]
    public void SearchOfAbsentFieldCompletesWithoutAMatch()
    {
        var result = HeaderFieldLookup.ContainsHttpToken(
            new[] { Field("Host", "example.test") },
            "Upgrade",
            "websocket",
            1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.FieldCount, Is.Zero);
            Assert.That(result.TokenCount, Is.Zero);
            Assert.That(result.Found, Is.False);
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
