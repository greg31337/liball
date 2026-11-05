using System.Text;

namespace http.Tests;

public class FieldValueParserTests
{
    [Test]
    public void TokenParsingRequiresOneOrMoreTokenCharactersAndFullConsumption()
    {
        FieldValueParseResult valid = FieldValueParser.ParseToken(
            Encoding.ASCII.GetBytes("AZaz09!#$%&'*+-.^_`|~"),
            64);
        FieldValueParseResult empty = FieldValueParser.ParseToken(Array.Empty<byte>(), 64);
        FieldValueParseResult delimiter = FieldValueParser.ParseToken("token/value"u8.ToArray(), 64);
        FieldValueParseResult obsText = FieldValueParser.ParseToken(new byte[] { 0x61, 0x80 }, 64);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(valid.Value, Is.EqualTo(Encoding.ASCII.GetBytes("AZaz09!#$%&'*+-.^_`|~")));
            Assert.That(valid.Consumed, Is.EqualTo(21));
            Assert.That(empty.IsMalformed, Is.True);
            Assert.That(delimiter.IsMalformed, Is.True);
            Assert.That(delimiter.ErrorPosition, Is.EqualTo(5));
            Assert.That(obsText.IsMalformed, Is.True);
            Assert.That(obsText.ErrorPosition, Is.EqualTo(1));
        });
    }

    [Test]
    public void PrimitiveParsersReportConfiguredSizeLimitsSeparately()
    {
        FieldValueParseResult token = FieldValueParser.ParseToken("abcd"u8.ToArray(), 3);
        FieldValueParseResult quoted = FieldValueParser.ParseQuotedString("\"ab\""u8.ToArray(), 3);
        FieldValueParseResult whitespace = FieldValueParser.ParseOptionalWhitespace(" \t "u8.ToArray(), 2);

        Assert.Multiple(() =>
        {
            Assert.That(token.IsLimitExceeded, Is.True);
            Assert.That(token.IsMalformed, Is.False);
            Assert.That(quoted.IsLimitExceeded, Is.True);
            Assert.That(whitespace.IsLimitExceeded, Is.True);
        });
    }

    [Test]
    public void QuotedStringParsingDecodesEscapesAndPreservesObsText()
    {
        byte[] input =
        [
            0x22,
            0x61,
            0x5c, 0x22,
            0x62,
            0x5c, 0x5c,
            0x80,
            0x22
        ];

        FieldValueParseResult result = FieldValueParser.ParseQuotedString(input, 32);
        FieldValueParseResult empty = FieldValueParser.ParseQuotedString("\"\""u8.ToArray(), 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Value, Is.EqualTo(new byte[] { 0x61, 0x22, 0x62, 0x5c, 0x80 }));
            Assert.That(result.Consumed, Is.EqualTo(input.Length));
            Assert.That(empty.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(empty.Value, Is.Empty);
        });
    }

    [Test]
    public void QuotedStringParsingRejectsIncompleteInvalidAndTrailingData()
    {
        FieldValueParseResult incomplete = FieldValueParser.ParseQuotedString("\"abc"u8.ToArray(), 32);
        FieldValueParseResult badEscape = FieldValueParser.ParseQuotedString(
            new byte[] { 0x22, 0x5c, 0x0a, 0x22 },
            32);
        FieldValueParseResult trailing = FieldValueParser.ParseQuotedString("\"abc\"x"u8.ToArray(), 32);

        Assert.Multiple(() =>
        {
            Assert.That(incomplete.IsMalformed, Is.True);
            Assert.That(incomplete.ErrorPosition, Is.EqualTo(4));
            Assert.That(badEscape.IsMalformed, Is.True);
            Assert.That(badEscape.ErrorPosition, Is.EqualTo(1));
            Assert.That(trailing.IsMalformed, Is.True);
            Assert.That(trailing.ErrorPosition, Is.EqualTo(5));
        });
    }

    [Test]
    public void QuotedPairParsingRequiresExactlyOnePermittedEscapedOctet()
    {
        FieldValueParseResult tab = FieldValueParser.ParseQuotedPair(new byte[] { 0x5c, 0x09 }, 2);
        FieldValueParseResult obsText = FieldValueParser.ParseQuotedPair(new byte[] { 0x5c, 0xff }, 2);
        FieldValueParseResult control = FieldValueParser.ParseQuotedPair(new byte[] { 0x5c, 0x1f }, 2);
        FieldValueParseResult tooLong = FieldValueParser.ParseQuotedPair("\\ab"u8.ToArray(), 3);

        Assert.Multiple(() =>
        {
            Assert.That(tab.Value, Is.EqualTo(new byte[] { 0x09 }));
            Assert.That(obsText.Value, Is.EqualTo(new byte[] { 0xff }));
            Assert.That(control.IsMalformed, Is.True);
            Assert.That(tooLong.IsMalformed, Is.True);
        });
    }

    [Test]
    public void OptionalWhitespaceAcceptsOnlySpaceAndHorizontalTab()
    {
        FieldValueParseResult empty = FieldValueParser.ParseOptionalWhitespace(Array.Empty<byte>(), 8);
        FieldValueParseResult valid = FieldValueParser.ParseOptionalWhitespace(" \t  "u8.ToArray(), 8);
        FieldValueParseResult invalid = FieldValueParser.ParseOptionalWhitespace(" \r"u8.ToArray(), 8);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(valid.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(valid.Value, Is.EqualTo(" \t  "u8.ToArray()));
            Assert.That(invalid.IsMalformed, Is.True);
            Assert.That(invalid.ErrorPosition, Is.EqualTo(1));
        });
    }

    [Test]
    public void CommaListSplitsOnlyAtTopLevelAndTrimsOptionalWhitespace()
    {
        byte[] input = Encoding.ASCII.GetBytes("one, \"two,three\" , four(a,b)");

        FieldValueParseResult result = FieldValueParser.ParseCommaList(input, 128, 8, 4);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Values, Has.Count.EqualTo(3));
            Assert.That(Encoding.ASCII.GetString(result.Values[0]), Is.EqualTo("one"));
            Assert.That(Encoding.ASCII.GetString(result.Values[1]), Is.EqualTo("\"two,three\""));
            Assert.That(Encoding.ASCII.GetString(result.Values[2]), Is.EqualTo("four(a,b)"));
            Assert.That(result.Consumed, Is.EqualTo(input.Length));
        });
    }

    [Test]
    public void CommaListRejectsEmptyMembersAndUnbalancedConstructs()
    {
        FieldValueParseResult leading = FieldValueParser.ParseCommaList(",one"u8.ToArray(), 32, 8, 4);
        FieldValueParseResult middle = FieldValueParser.ParseCommaList("one,,two"u8.ToArray(), 32, 8, 4);
        FieldValueParseResult quote = FieldValueParser.ParseCommaList("one,\"two"u8.ToArray(), 32, 8, 4);
        FieldValueParseResult comment = FieldValueParser.ParseCommaList("one,(two"u8.ToArray(), 32, 8, 4);

        Assert.Multiple(() =>
        {
            Assert.That(leading.IsMalformed, Is.True);
            Assert.That(middle.IsMalformed, Is.True);
            Assert.That(quote.IsMalformed, Is.True);
            Assert.That(comment.IsMalformed, Is.True);
        });
    }

    [Test]
    public void HashListIgnoresEmptyMembersAndPreservesSourceOrder()
    {
        FieldValueParseResult result = FieldValueParser.ParseHashList(
            " , one, ,two, "u8.ToArray(),
            64,
            8,
            4);
        FieldValueParseResult empty = FieldValueParser.ParseHashList(Array.Empty<byte>(), 64, 8, 4);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Values, Has.Count.EqualTo(2));
            Assert.That(result.Values[0], Is.EqualTo("one"u8.ToArray()));
            Assert.That(result.Values[1], Is.EqualTo("two"u8.ToArray()));
            Assert.That(result.EmptyElements, Is.EqualTo(3));
            Assert.That(empty.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(empty.Values, Is.Empty);
        });
    }

    [Test]
    public void ListParsingEnforcesMemberEmptyElementAndDepthLimits()
    {
        FieldValueParseResult members = FieldValueParser.ParseHashList("a,b,c"u8.ToArray(), 32, 2, 4);
        FieldValueParseResult empties = FieldValueParser.ParseHashList(",,,a"u8.ToArray(), 32, 2, 4);
        FieldValueParseResult depth = FieldValueParser.ParseHashList("a((b))"u8.ToArray(), 32, 4, 1);

        Assert.Multiple(() =>
        {
            Assert.That(members.IsLimitExceeded, Is.True);
            Assert.That(members.IsMalformed, Is.False);
            Assert.That(empties.IsLimitExceeded, Is.True);
            Assert.That(depth.IsLimitExceeded, Is.True);
        });
    }

    [Test]
    public void CommentParsingHandlesNestingEscapesAndObsText()
    {
        byte[] input =
        [
            0x28,
            0x61,
            0x28, 0x62, 0x29,
            0x5c, 0x28,
            0x5c, 0x29,
            0x80,
            0x29
        ];

        FieldValueParseResult result = FieldValueParser.ParseComment(input, 32, 3);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Value, Is.EqualTo(new byte[]
            {
                0x61,
                0x28, 0x62, 0x29,
                0x28, 0x29,
                0x80
            }));
            Assert.That(result.Consumed, Is.EqualTo(input.Length));
        });
    }

    [Test]
    public void CommentParsingRejectsMalformedAndOverlyDeepInput()
    {
        FieldValueParseResult incomplete = FieldValueParser.ParseComment("(a(b)"u8.ToArray(), 32, 3);
        FieldValueParseResult trailing = FieldValueParser.ParseComment("(a)x"u8.ToArray(), 32, 3);
        FieldValueParseResult badEscape = FieldValueParser.ParseComment(
            new byte[] { 0x28, 0x5c, 0x0a, 0x29 },
            32,
            3);
        FieldValueParseResult deep = FieldValueParser.ParseComment("((a))"u8.ToArray(), 32, 1);

        Assert.Multiple(() =>
        {
            Assert.That(incomplete.IsMalformed, Is.True);
            Assert.That(trailing.IsMalformed, Is.True);
            Assert.That(trailing.ErrorPosition, Is.EqualTo(3));
            Assert.That(badEscape.IsMalformed, Is.True);
            Assert.That(deep.IsLimitExceeded, Is.True);
            Assert.That(deep.IsMalformed, Is.False);
        });
    }

    [Test]
    public void ParameterParsingPreservesOrderNamesAndDecodedValueForms()
    {
        byte[] input = Encoding.ASCII.GetBytes(" ; Charset=utf-8 ; title=\"a\\\"b\";flag=yes");

        FieldValueParseResult result = FieldValueParser.ParseParameters(input, 128, 8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Parameters, Has.Count.EqualTo(3));
            Assert.That(result.Parameters[0].Name, Is.EqualTo("Charset"));
            Assert.That(result.Parameters[0].Value, Is.EqualTo("utf-8"u8.ToArray()));
            Assert.That(result.Parameters[0].IsQuoted, Is.False);
            Assert.That(result.Parameters[1].Name, Is.EqualTo("title"));
            Assert.That(result.Parameters[1].Value, Is.EqualTo("a\"b"u8.ToArray()));
            Assert.That(result.Parameters[1].IsQuoted, Is.True);
            Assert.That(result.Parameters[2].Name, Is.EqualTo("flag"));
            Assert.That(result.Parameters[2].HasValue, Is.True);
        });
    }

    [Test]
    public void ParameterParsingAcceptsEmptySlotsAndOptionalWhitespaceAroundSemicolons()
    {
        FieldValueParseResult result = FieldValueParser.ParseParameters(
            " ; ; a=b ;\t; c=\"\" ; "u8.ToArray(),
            64,
            8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Parameters, Has.Count.EqualTo(2));
            Assert.That(result.Parameters[0].Name, Is.EqualTo("a"));
            Assert.That(result.Parameters[1].Name, Is.EqualTo("c"));
            Assert.That(result.Parameters[1].Value, Is.Empty);
            Assert.That(result.Parameters[1].IsQuoted, Is.True);
            Assert.That(result.EmptyElements, Is.EqualTo(3));
        });
    }

    [Test]
    public void ParameterParsingRejectsBadEqualsWhitespaceAndIncompleteValues()
    {
        FieldValueParseResult beforeEquals = FieldValueParser.ParseParameters(";a =b"u8.ToArray(), 32, 8);
        FieldValueParseResult afterEquals = FieldValueParser.ParseParameters(";a= b"u8.ToArray(), 32, 8);
        FieldValueParseResult missing = FieldValueParser.ParseParameters(";a="u8.ToArray(), 32, 8);
        FieldValueParseResult trailing = FieldValueParser.ParseParameters(";a=b x"u8.ToArray(), 32, 8);

        Assert.Multiple(() =>
        {
            Assert.That(beforeEquals.IsMalformed, Is.True);
            Assert.That(afterEquals.IsMalformed, Is.True);
            Assert.That(missing.IsMalformed, Is.True);
            Assert.That(trailing.IsMalformed, Is.True);
        });
    }

    [Test]
    public void ParameterParsingEnforcesInputAndParameterLimits()
    {
        FieldValueParseResult size = FieldValueParser.ParseParameters(";a=b"u8.ToArray(), 3, 8);
        FieldValueParseResult count = FieldValueParser.ParseParameters(";a=b;c=d"u8.ToArray(), 32, 1);
        FieldValueParseResult emptySlots = FieldValueParser.ParseParameters(";;;a=b"u8.ToArray(), 32, 2);

        Assert.Multiple(() =>
        {
            Assert.That(size.IsLimitExceeded, Is.True);
            Assert.That(count.IsLimitExceeded, Is.True);
            Assert.That(emptySlots.IsLimitExceeded, Is.True);
        });
    }
}
