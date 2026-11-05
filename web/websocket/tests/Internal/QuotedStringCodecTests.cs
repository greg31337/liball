using websocket;

namespace websocket.Tests;

public class QuotedStringCodecTests
{
    [TestCase("\"\"", "", 2)]
    [TestCase("\"plain text\"", "plain text", 12)]
    [TestCase("\"a,b;c=d\"", "a,b;c=d", 9)]
    [TestCase("\"say \\\"yes\\\" and \\\\ no\"", "say \"yes\" and \\ no", 23)]
    [TestCase("xx\"value\"tail", "value", 7, 2)]
    public void ReadsAndUnescapesQuotedStrings(
        string source,
        string expectedValue,
        int expectedConsumed,
        int offset = 0)
    {
        var result = QuotedStringCodec.Read(source, offset);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ConsumedCount, Is.EqualTo(expectedConsumed));
            Assert.That(result.Value, Is.EqualTo(expectedValue));
        });
    }

    [Test]
    public void ReadsHorizontalTabAndOpaqueOctets()
    {
        var source = new string(new[] { '"', '\t', (char)128, (char)255, '"' });

        var result = QuotedStringCodec.Read(source, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.Value.ToCharArray(), Is.EqualTo(new[] { '\t', (char)128, (char)255 }));
        });
    }

    [TestCase("")]
    [TestCase("\"")]
    [TestCase("\"unfinished")]
    [TestCase("\"unfinished\\")]
    public void ReportsIncompleteQuotedStrings(string source)
    {
        var result = QuotedStringCodec.Read(source, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.NeedInput));
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.Value, Is.Empty);
        });
    }

    [TestCase("plain")]
    [TestCase("\"bad\rvalue\"")]
    [TestCase("\"bad\nvalue\"")]
    [TestCase("\"bad\u007fvalue\"")]
    [TestCase("\"bad\u0100value\"")]
    [TestCase("\"bad\\\u0001value\"")]
    public void RejectsMalformedQuotedStrings(string source)
    {
        var result = QuotedStringCodec.Read(source, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.Value, Is.Empty);
        });
    }

    [Test]
    public void RejectsInvalidOffsets()
    {
        Assert.Multiple(() =>
        {
            Assert.That(QuotedStringCodec.Read("\"value\"", -1).Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(QuotedStringCodec.Read("\"value\"", 8).Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
        });
    }

    [TestCase("", "\"\"")]
    [TestCase("plain text", "\"plain text\"")]
    [TestCase("say \"yes\" \\ no", "\"say \\\"yes\\\" \\\\ no\"")]
    public void WritesCanonicalQuotedStrings(string value, string expected)
    {
        var destination = Enumerable.Repeat('?', expected.Length + 1).ToArray();

        var result = QuotedStringCodec.Write(value, destination);

        Assert.Multiple(() =>
        {
            Assert.That(QuotedStringCodec.MeasureEncodedLength(value), Is.EqualTo(expected.Length));
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(result.ProducedCount, Is.EqualTo(expected.Length));
            Assert.That(new string(destination, 0, result.ProducedCount), Is.EqualTo(expected));
            Assert.That(destination[expected.Length], Is.EqualTo('?'));
        });
    }

    [Test]
    public void WriteReportsCapacityWithoutPartiallyWriting()
    {
        var destination = new[] { '?', '?', '?' };

        var result = QuotedStringCodec.Write("needs space", destination);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(WebSocketParseStatus.NeedOutputCapacity));
            Assert.That(result.ProducedCount, Is.Zero);
            Assert.That(destination, Is.EqualTo(new[] { '?', '?', '?' }));
        });
    }

    [Test]
    public void WriteRejectsUnsafeDecodedCharactersWithoutWriting()
    {
        var destination = Enumerable.Repeat('?', 10).ToArray();

        var control = QuotedStringCodec.Write("bad\rvalue", destination);
        var nonOctetLength = QuotedStringCodec.MeasureEncodedLength("\u0100");

        Assert.Multiple(() =>
        {
            Assert.That(control.Status, Is.EqualTo(WebSocketParseStatus.ProtocolFailure));
            Assert.That(control.ProducedCount, Is.Zero);
            Assert.That(nonOctetLength, Is.EqualTo(-1));
            Assert.That(destination, Is.EqualTo(Enumerable.Repeat('?', 10)));
        });
    }

    [Test]
    public void WrittenValueRoundTripsThroughReader()
    {
        const string value = "a, b; quote=\" slash=\\";
        var destination = new char[QuotedStringCodec.MeasureEncodedLength(value)];

        var write = QuotedStringCodec.Write(value, destination);
        var read = QuotedStringCodec.Read(destination, 0);

        Assert.Multiple(() =>
        {
            Assert.That(write.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(read.Status, Is.EqualTo(WebSocketParseStatus.Complete));
            Assert.That(read.ConsumedCount, Is.EqualTo(destination.Length));
            Assert.That(read.Value, Is.EqualTo(value));
        });
    }

    [Test]
    public void ReaderClassifiesEveryQuotedTextAndQuotedPairOctet()
    {
        for (int value = 0; value <= 255; value++)
        {
            char character = (char)value;
            if (IsQuotedTextCharacter(character))
            {
                var textResult = QuotedStringCodec.Read(
                    new string(new[] { '"', character, '"' }),
                    0);

                Assert.Multiple(() =>
                {
                    Assert.That(textResult.Status, Is.EqualTo(WebSocketParseStatus.Complete),
                        $"quoted text octet {value}");
                    Assert.That(textResult.Value, Is.EqualTo(character.ToString()),
                        $"quoted text octet {value}");
                });
            }

            var pairResult = QuotedStringCodec.Read(
                new string(new[] { '"', '\\', character, '"' }),
                0);
            WebSocketParseStatus expected = IsQuotedPairCharacter(character)
                ? WebSocketParseStatus.Complete
                : WebSocketParseStatus.ProtocolFailure;

            Assert.That(pairResult.Status, Is.EqualTo(expected), $"quoted pair octet {value}");
            if (expected == WebSocketParseStatus.Complete)
            {
                Assert.That(pairResult.Value, Is.EqualTo(character.ToString()),
                    $"quoted pair octet {value}");
            }
        }
    }

    static bool IsQuotedTextCharacter(char character)
    {
        return character == '\t' ||
            character == ' ' ||
            character == '!' ||
            (character >= '#' && character <= '[') ||
            (character >= ']' && character <= '~') ||
            character >= (char)128;
    }

    static bool IsQuotedPairCharacter(char character)
    {
        return character == '\t' ||
            character == ' ' ||
            (character >= '!' && character <= '~') ||
            character >= (char)128;
    }
}
