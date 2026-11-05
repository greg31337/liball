using System.Text;

namespace http.Tests;

public class EntityTagCodecTests
{
    [Test]
    public void EntityTagParserAcceptsStrongWeakEmptyAndOpaqueOctets()
    {
        EntityTagParseResult empty = FieldValueParser.ParseEntityTag("\"\""u8.ToArray(), 16);
        EntityTagParseResult weak = FieldValueParser.ParseEntityTag(
            "W/\"abc\\def\""u8.ToArray(),
            32);
        EntityTagParseResult obsText = FieldValueParser.ParseEntityTag(
            new byte[] { 0x22, 0x21, 0x23, 0x5c, 0x7e, 0x80, 0xff, 0x22 },
            16);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(empty.Value.OpaqueTag, Is.Empty);
            Assert.That(empty.Value.IsWeak, Is.False);
            Assert.That(weak.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(weak.Value.IsWeak, Is.True);
            Assert.That(weak.Value.OpaqueTag, Is.EqualTo("abc\\def"u8.ToArray()));
            Assert.That(obsText.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(obsText.Value.OpaqueTag,
                Is.EqualTo(new byte[] { 0x21, 0x23, 0x5c, 0x7e, 0x80, 0xff }));
        });
    }

    [Test]
    public void EntityTagParserTreatsBackslashAsOpaqueInsteadOfAnEscape()
    {
        byte[] input = new byte[] { 0x22, 0x74, 0x61, 0x67, 0x5c, 0x22 };

        EntityTagParseResult result = FieldValueParser.ParseEntityTag(input, 16);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(input.Length));
            Assert.That(result.Value.OpaqueTag, Is.EqualTo(new byte[] { 0x74, 0x61, 0x67, 0x5c }));
        });
    }

    [Test]
    public void EntityTagParserRejectsWrongWeaknessCaseAndInvalidOpaqueSyntax()
    {
        byte[][] malformed =
        [
            "w/\"tag\""u8.ToArray(),
            "W/ \"tag\""u8.ToArray(),
            "\"has space\""u8.ToArray(),
            "\"has\ttab\""u8.ToArray(),
            "\"unterminated"u8.ToArray(),
            "\"a\"b\""u8.ToArray(),
            new byte[] { 0x22, 0x61, 0x7f, 0x22 },
            Array.Empty<byte>()
        ];

        foreach (byte[] input in malformed)
        {
            EntityTagParseResult result = FieldValueParser.ParseEntityTag(input, 64);
            Assert.That(result.IsMalformed, Is.True, Encoding.Latin1.GetString(input));
        }
    }

    [Test]
    public void EntityTagListParserHandlesOpaqueCommasBackslashesAndEmptyMembers()
    {
        byte[] input = " , W/\"one,two\", , \"trail\\\", "u8.ToArray();

        EntityTagParseResult result = FieldValueParser.ParseEntityTags(input, 96, 8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(input.Length));
            Assert.That(result.Values, Has.Count.EqualTo(2));
            Assert.That(result.EmptyElements, Is.EqualTo(3));
            Assert.That(result.Values[0].IsWeak, Is.True);
            Assert.That(result.Values[0].OpaqueTag, Is.EqualTo("one,two"u8.ToArray()));
            Assert.That(result.Values[1].OpaqueTag, Is.EqualTo("trail\\"u8.ToArray()));
        });
    }

    [Test]
    public void EntityTagListParserRepresentsOnlyAStandaloneWildcard()
    {
        EntityTagParseResult wildcard = FieldValueParser.ParseEntityTags(" \t* "u8.ToArray(), 16, 2);
        EntityTagParseResult mixedAfter = FieldValueParser.ParseEntityTags("*, \"tag\""u8.ToArray(), 32, 4);
        EntityTagParseResult mixedBefore = FieldValueParser.ParseEntityTags("\"tag\", *"u8.ToArray(), 32, 4);

        Assert.Multiple(() =>
        {
            Assert.That(wildcard.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(wildcard.IsWildcard, Is.True);
            Assert.That(wildcard.Values, Has.Count.EqualTo(1));
            Assert.That(wildcard.Values[0].IsWildcard, Is.True);
            Assert.That(mixedAfter.IsMalformed, Is.True);
            Assert.That(mixedBefore.IsMalformed, Is.True);
        });
    }

    [Test]
    public void EntityTagListParserRejectsMalformedMemberBoundaries()
    {
        string[] malformed =
        [
            "\"one\" \"two\"",
            "tag",
            "W/\"one\", w/\"two\"",
            "\"one",
            "\"one\", W/"
        ];

        foreach (string input in malformed)
        {
            EntityTagParseResult result = FieldValueParser.ParseEntityTags(
                Encoding.ASCII.GetBytes(input),
                64,
                8);
            Assert.That(result.IsMalformed, Is.True, input);
        }
    }

    [Test]
    public void EntityTagSerializationIsCanonicalAndRoundTripsExactOctets()
    {
        HttpEntityTag weak = new HttpEntityTag();
        weak.IsWeak = true;
        weak.OpaqueTag = "one,two"u8.ToArray();
        HttpEntityTag strong = new HttpEntityTag();
        strong.OpaqueTag = new byte[] { 0x74, 0x61, 0x67, 0x5c, 0x80 };

        FieldValueWriteResult written = FieldValueWriter.SerializeEntityTags(
            new List<HttpEntityTag> { weak, strong },
            64,
            4);
        EntityTagParseResult parsed = FieldValueParser.ParseEntityTags(written.Bytes, 64, 4);

        byte[] expected = new byte[]
        {
            0x57, 0x2f, 0x22,
            0x6f, 0x6e, 0x65, 0x2c, 0x74, 0x77, 0x6f,
            0x22, 0x2c, 0x20, 0x22,
            0x74, 0x61, 0x67, 0x5c, 0x80,
            0x22
        };

        Assert.Multiple(() =>
        {
            Assert.That(written.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(written.Bytes, Is.EqualTo(expected));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Values[0].OpaqueTag, Is.EqualTo(weak.OpaqueTag));
            Assert.That(parsed.Values[1].OpaqueTag, Is.EqualTo(strong.OpaqueTag));
        });
    }

    [Test]
    public void EntityTagListSerializerEnforcesWildcardState()
    {
        HttpEntityTag wildcard = new HttpEntityTag();
        wildcard.IsWildcard = true;
        HttpEntityTag ordinary = new HttpEntityTag();
        ordinary.OpaqueTag = "tag"u8.ToArray();

        FieldValueWriteResult valid = FieldValueWriter.SerializeEntityTags(
            new List<HttpEntityTag> { wildcard },
            1,
            1);
        FieldValueWriteResult mixed = FieldValueWriter.SerializeEntityTags(
            new List<HttpEntityTag> { ordinary, wildcard },
            32,
            2);

        wildcard.IsWeak = true;
        FieldValueWriteResult inconsistent = FieldValueWriter.SerializeEntityTags(
            new List<HttpEntityTag> { wildcard },
            8,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Bytes, Is.EqualTo("*"u8.ToArray()));
            Assert.That(mixed.IsMalformed, Is.True);
            Assert.That(inconsistent.IsMalformed, Is.True);
            Assert.That(FieldValueWriter.SerializeEntityTag(wildcard, 8).IsMalformed, Is.True);
        });
    }

    [Test]
    public void EntityTagOperationsReportConfiguredLimitsSeparately()
    {
        EntityTagParseResult size = FieldValueParser.ParseEntityTag("\"tag\""u8.ToArray(), 4);
        EntityTagParseResult members = FieldValueParser.ParseEntityTags(
            "\"one\", \"two\""u8.ToArray(),
            32,
            1);
        EntityTagParseResult empties = FieldValueParser.ParseEntityTags(
            ",,,\"tag\""u8.ToArray(),
            32,
            2);

        HttpEntityTag tag = new HttpEntityTag();
        tag.OpaqueTag = "tag"u8.ToArray();
        FieldValueWriteResult writeSize = FieldValueWriter.SerializeEntityTag(tag, 4);

        Assert.Multiple(() =>
        {
            Assert.That(size.IsLimitExceeded, Is.True);
            Assert.That(size.IsMalformed, Is.False);
            Assert.That(members.IsLimitExceeded, Is.True);
            Assert.That(empties.IsLimitExceeded, Is.True);
            Assert.That(writeSize.IsLimitExceeded, Is.True);
        });
    }

    [Test]
    public void StrongAndWeakComparisonFollowRfcRulesAndUseExactOctets()
    {
        HttpEntityTag weakOne = new HttpEntityTag();
        weakOne.IsWeak = true;
        weakOne.OpaqueTag = "1"u8.ToArray();
        HttpEntityTag weakTwo = new HttpEntityTag();
        weakTwo.IsWeak = true;
        weakTwo.OpaqueTag = "2"u8.ToArray();
        HttpEntityTag strongOne = new HttpEntityTag();
        strongOne.OpaqueTag = "1"u8.ToArray();
        HttpEntityTag strongOneCopy = new HttpEntityTag();
        strongOneCopy.OpaqueTag = "1"u8.ToArray();
        HttpEntityTag strongUpper = new HttpEntityTag();
        strongUpper.OpaqueTag = "ONE"u8.ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(EntityTagComparer.CompareStrong(weakOne, weakOne), Is.False);
            Assert.That(EntityTagComparer.CompareWeak(weakOne, weakOne), Is.True);
            Assert.That(EntityTagComparer.CompareWeak(weakOne, weakTwo), Is.False);
            Assert.That(EntityTagComparer.CompareStrong(weakOne, strongOne), Is.False);
            Assert.That(EntityTagComparer.CompareWeak(weakOne, strongOne), Is.True);
            Assert.That(EntityTagComparer.CompareStrong(strongOne, strongOneCopy), Is.True);
            Assert.That(EntityTagComparer.CompareWeak(strongOne, strongOneCopy), Is.True);
            Assert.That(EntityTagComparer.CompareWeak(strongOne, strongUpper), Is.False);
        });
    }

    [Test]
    public void ComparisonNeverTreatsWildcardOrInvalidStateAsAnEntityTag()
    {
        HttpEntityTag wildcard = new HttpEntityTag();
        wildcard.IsWildcard = true;
        HttpEntityTag ordinary = new HttpEntityTag();
        ordinary.OpaqueTag = "tag"u8.ToArray();
        HttpEntityTag invalid = new HttpEntityTag();
        invalid.OpaqueTag = null!;
        HttpEntityTag invalidSyntax = new HttpEntityTag();
        invalidSyntax.OpaqueTag = "not valid"u8.ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(EntityTagComparer.CompareStrong(wildcard, ordinary), Is.False);
            Assert.That(EntityTagComparer.CompareWeak(ordinary, wildcard), Is.False);
            Assert.That(EntityTagComparer.CompareStrong(invalid, ordinary), Is.False);
            Assert.That(EntityTagComparer.CompareWeak(ordinary, invalid), Is.False);
            Assert.That(EntityTagComparer.CompareWeak(invalidSyntax, invalidSyntax), Is.False);
            Assert.That(EntityTagComparer.CompareWeak(null!, ordinary), Is.False);
        });
    }
}
