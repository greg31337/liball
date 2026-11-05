using System.Text;

namespace http.Tests;

public class RangeFieldCodecTests
{
    [Test]
    public void ByteRangeParserHandlesClosedOpenAndSuffixFormsInSourceOrder()
    {
        byte[] input = "Bytes= 0-499, 500-, , -0500,"u8.ToArray();

        RangeValueParseResult result = FieldValueParser.ParseRangeValue(input, 96, 8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(input.Length));
            Assert.That(result.Range.Unit, Is.EqualTo("Bytes"));
            Assert.That(result.IsExtensionUnit, Is.False);
            Assert.That(result.EmptyElements, Is.EqualTo(2));
            Assert.That(result.Range.Ranges, Has.Count.EqualTo(3));
            Assert.That(result.Range.Ranges[0].HasStart, Is.True);
            Assert.That(result.Range.Ranges[0].Start, Is.Zero);
            Assert.That(result.Range.Ranges[0].HasEnd, Is.True);
            Assert.That(result.Range.Ranges[0].End, Is.EqualTo(499));
            Assert.That(result.Range.Ranges[1].Start, Is.EqualTo(500));
            Assert.That(result.Range.Ranges[1].HasEnd, Is.False);
            Assert.That(result.Range.Ranges[2].IsSuffix, Is.True);
            Assert.That(result.Range.Ranges[2].SuffixLength, Is.EqualTo(500));
        });
    }

    [Test]
    public void ByteRangeParserAcceptsZeroSuffixAndMaximumSignedOffsets()
    {
        RangeValueParseResult result = FieldValueParser.ParseRangeValue(
            "bytes=9223372036854775807-,-0"u8.ToArray(),
            64,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Range.Ranges[0].Start, Is.EqualTo(long.MaxValue));
            Assert.That(result.Range.Ranges[0].HasEnd, Is.False);
            Assert.That(result.Range.Ranges[1].IsSuffix, Is.True);
            Assert.That(result.Range.Ranges[1].SuffixLength, Is.Zero);
        });
    }

    [Test]
    public void ByteRangeParserRejectsInvalidUndefinedAndOverflowingMembers()
    {
        string[] malformed =
        [
            "bytes=",
            "bytes=,,",
            "bytes=500-499",
            "bytes=-",
            "bytes=1.2-4.3",
            "bytes=0 - 1",
            "bytes=+1-2",
            "bytes=9223372036854775808-",
            "bytes=-9223372036854775808",
            "bytes=0-9223372036854775808"
        ];

        foreach (string input in malformed)
        {
            RangeValueParseResult result = FieldValueParser.ParseRangeValue(
                Encoding.ASCII.GetBytes(input),
                96,
                8);
            Assert.That(result.IsMalformed, Is.True, input);
        }
    }

    [Test]
    public void ExtensionRangeParserPreservesUnitAndSpecifierOctets()
    {
        byte[] input = "Example-Unit=1.2-4.3,chapter@7,token=value"u8.ToArray();

        RangeValueParseResult parsed = FieldValueParser.ParseRangeValue(input, 96, 8);
        FieldValueWriteResult written = FieldValueWriter.SerializeRangeValue(parsed.Range, 96, 8);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.IsExtensionUnit, Is.True);
            Assert.That(parsed.Range.Unit, Is.EqualTo("Example-Unit"));
            Assert.That(parsed.Range.Ranges, Is.Empty);
            Assert.That(parsed.Range.OtherRanges, Has.Count.EqualTo(3));
            Assert.That(parsed.Range.OtherRanges[0], Is.EqualTo("1.2-4.3"u8.ToArray()));
            Assert.That(parsed.Range.OtherRanges[1], Is.EqualTo("chapter@7"u8.ToArray()));
            Assert.That(parsed.Range.OtherRanges[2], Is.EqualTo("token=value"u8.ToArray()));
            Assert.That(Encoding.ASCII.GetString(written.Bytes),
                Is.EqualTo("Example-Unit=1.2-4.3, chapter@7, token=value"));
        });
    }

    [Test]
    public void RangeSerializerProducesCanonicalDecimalAndListForms()
    {
        HttpRangeRequest request = new HttpRangeRequest();
        request.Unit = "bytes";
        HttpByteRange closed = new HttpByteRange();
        closed.HasStart = true;
        closed.Start = 0;
        closed.HasEnd = true;
        closed.End = 499;
        HttpByteRange open = new HttpByteRange();
        open.HasStart = true;
        open.Start = long.MaxValue;
        HttpByteRange suffix = new HttpByteRange();
        suffix.IsSuffix = true;
        suffix.SuffixLength = 25;
        request.Ranges.Add(closed);
        request.Ranges.Add(open);
        request.Ranges.Add(suffix);

        FieldValueWriteResult written = FieldValueWriter.SerializeRangeValue(request, 96, 4);
        RangeValueParseResult parsed = FieldValueParser.ParseRangeValue(written.Bytes, 96, 4);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(written.Bytes),
                Is.EqualTo("bytes=0-499, 9223372036854775807-, -25"));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Range.Ranges[1].Start, Is.EqualTo(long.MaxValue));
        });
    }

    [Test]
    public void RangeSerializerRejectsInconsistentStructuredAndExtensionState()
    {
        HttpRangeRequest mixed = new HttpRangeRequest();
        mixed.Unit = "bytes";
        mixed.OtherRanges.Add("0-1"u8.ToArray());

        HttpRangeRequest reversed = new HttpRangeRequest();
        reversed.Unit = "bytes";
        HttpByteRange invalid = new HttpByteRange();
        invalid.HasStart = true;
        invalid.Start = 10;
        invalid.HasEnd = true;
        invalid.End = 9;
        reversed.Ranges.Add(invalid);

        HttpRangeRequest extension = new HttpRangeRequest();
        extension.Unit = "custom";
        extension.OtherRanges.Add("has space"u8.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(FieldValueWriter.SerializeRangeValue(mixed, 32, 4).IsMalformed, Is.True);
            Assert.That(FieldValueWriter.SerializeRangeValue(reversed, 32, 4).IsMalformed, Is.True);
            Assert.That(FieldValueWriter.SerializeRangeValue(extension, 32, 4).IsMalformed, Is.True);
        });
    }

    [Test]
    public void ContentRangeParserHandlesKnownUnknownAndUnsatisfiedLengths()
    {
        RangeValueParseResult known = FieldValueParser.ParseContentRange(
            "bytes 42-1233/1234"u8.ToArray(),
            64);
        RangeValueParseResult unknown = FieldValueParser.ParseContentRange(
            "Bytes 42-1233/*"u8.ToArray(),
            64);
        RangeValueParseResult unsatisfied = FieldValueParser.ParseContentRange(
            "custom */0"u8.ToArray(),
            64);

        Assert.Multiple(() =>
        {
            Assert.That(known.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(known.ContentRange.Start, Is.EqualTo(42));
            Assert.That(known.ContentRange.End, Is.EqualTo(1233));
            Assert.That(known.ContentRange.HasCompleteLength, Is.True);
            Assert.That(known.ContentRange.CompleteLength, Is.EqualTo(1234));
            Assert.That(unknown.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(unknown.ContentRange.HasCompleteLength, Is.False);
            Assert.That(unsatisfied.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(unsatisfied.ContentRange.IsUnsatisfied, Is.True);
            Assert.That(unsatisfied.ContentRange.CompleteLength, Is.Zero);
            Assert.That(unsatisfied.IsExtensionUnit, Is.True);
        });
    }

    [Test]
    public void ContentRangeParserRejectsBadSpacingOrderingLengthsAndOverflow()
    {
        string[] malformed =
        [
            "bytes 1-0/2",
            "bytes 0-1/1",
            "bytes */*",
            "bytes  0-1/2",
            "bytes\t0-1/2",
            " bytes 0-1/2",
            "bytes 0-/2",
            "bytes 0-1/",
            "bytes 0-1/2 ",
            "bytes 9223372036854775808-9223372036854775808/*",
            "bytes 0-1/9223372036854775808"
        ];

        foreach (string input in malformed)
        {
            RangeValueParseResult result = FieldValueParser.ParseContentRange(
                Encoding.ASCII.GetBytes(input),
                96);
            Assert.That(result.IsMalformed, Is.True, input);
        }
    }

    [Test]
    public void ContentRangeSerializerCanonicalizesAllValidForms()
    {
        HttpContentRange known = new HttpContentRange();
        known.Unit = "bytes";
        known.HasStart = true;
        known.Start = 42;
        known.HasEnd = true;
        known.End = 1233;
        known.HasCompleteLength = true;
        known.CompleteLength = 1234;

        HttpContentRange unknown = new HttpContentRange();
        unknown.Unit = "bytes";
        unknown.HasStart = true;
        unknown.Start = 42;
        unknown.HasEnd = true;
        unknown.End = 1233;

        HttpContentRange unsatisfied = new HttpContentRange();
        unsatisfied.Unit = "bytes";
        unsatisfied.IsUnsatisfied = true;
        unsatisfied.HasCompleteLength = true;
        unsatisfied.CompleteLength = long.MaxValue;

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(
                FieldValueWriter.SerializeContentRange(known, 64).Bytes),
                Is.EqualTo("bytes 42-1233/1234"));
            Assert.That(Encoding.ASCII.GetString(
                FieldValueWriter.SerializeContentRange(unknown, 64).Bytes),
                Is.EqualTo("bytes 42-1233/*"));
            Assert.That(Encoding.ASCII.GetString(
                FieldValueWriter.SerializeContentRange(unsatisfied, 64).Bytes),
                Is.EqualTo("bytes */9223372036854775807"));
        });
    }

    [Test]
    public void ContentRangeSerializerRejectsInconsistentOrImpossibleState()
    {
        HttpContentRange reversed = new HttpContentRange();
        reversed.Unit = "bytes";
        reversed.HasStart = true;
        reversed.Start = 2;
        reversed.HasEnd = true;
        reversed.End = 1;

        HttpContentRange shortComplete = new HttpContentRange();
        shortComplete.Unit = "bytes";
        shortComplete.HasStart = true;
        shortComplete.Start = 0;
        shortComplete.HasEnd = true;
        shortComplete.End = 9;
        shortComplete.HasCompleteLength = true;
        shortComplete.CompleteLength = 9;

        HttpContentRange unsatisfiedWithRange = new HttpContentRange();
        unsatisfiedWithRange.Unit = "bytes";
        unsatisfiedWithRange.IsUnsatisfied = true;
        unsatisfiedWithRange.HasStart = true;
        unsatisfiedWithRange.HasCompleteLength = true;
        unsatisfiedWithRange.CompleteLength = 10;

        Assert.Multiple(() =>
        {
            Assert.That(FieldValueWriter.SerializeContentRange(reversed, 64).IsMalformed, Is.True);
            Assert.That(FieldValueWriter.SerializeContentRange(shortComplete, 64).IsMalformed, Is.True);
            Assert.That(FieldValueWriter.SerializeContentRange(unsatisfiedWithRange, 64).IsMalformed, Is.True);
        });
    }

    [Test]
    public void AcceptRangesParserPreservesUnitsAndIgnoresReasonableEmptyMembers()
    {
        RangeValueParseResult parsed = FieldValueParser.ParseAcceptRanges(
            ", Bytes, custom-unit, none,"u8.ToArray(),
            64,
            8);
        FieldValueWriteResult written = FieldValueWriter.SerializeAcceptRanges(
            parsed.Units,
            64,
            8);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.EmptyElements, Is.EqualTo(2));
            Assert.That(parsed.Units, Is.EqualTo(new[] { "Bytes", "custom-unit", "none" }));
            Assert.That(Encoding.ASCII.GetString(written.Bytes),
                Is.EqualTo("Bytes, custom-unit, none"));
        });
    }

    [Test]
    public void RangeUnitAndAcceptRangesRequireTokenSyntaxAndAtLeastOneUnit()
    {
        FieldValueParseResult parsed = FieldValueParser.ParseRangeUnit("custom.unit"u8.ToArray(), 16);
        FieldValueWriteResult written = FieldValueWriter.SerializeRangeUnit("Custom-Unit", 16);
        RangeValueParseResult empty = FieldValueParser.ParseAcceptRanges(",,"u8.ToArray(), 16, 4);
        RangeValueParseResult invalid = FieldValueParser.ParseAcceptRanges("bytes, bad/unit"u8.ToArray(), 32, 4);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(Encoding.ASCII.GetString(written.Bytes), Is.EqualTo("Custom-Unit"));
            Assert.That(empty.IsMalformed, Is.True);
            Assert.That(invalid.IsMalformed, Is.True);
            Assert.That(FieldValueWriter.SerializeAcceptRanges(new List<string>(), 16, 4).IsMalformed, Is.True);
            Assert.That(FieldValueWriter.SerializeRangeUnit("bad unit", 16).IsMalformed, Is.True);
        });
    }

    [Test]
    public void RangeOperationsDistinguishConfiguredLimitsFromMalformedInput()
    {
        RangeValueParseResult size = FieldValueParser.ParseRangeValue("bytes=0-1"u8.ToArray(), 8, 4);
        RangeValueParseResult ranges = FieldValueParser.ParseRangeValue(
            "bytes=0-1,2-3"u8.ToArray(),
            32,
            1);
        RangeValueParseResult units = FieldValueParser.ParseAcceptRanges("bytes,items"u8.ToArray(), 32, 1);

        HttpRangeRequest request = new HttpRangeRequest();
        request.Unit = "bytes";
        HttpByteRange range = new HttpByteRange();
        range.HasStart = true;
        request.Ranges.Add(range);
        FieldValueWriteResult output = FieldValueWriter.SerializeRangeValue(request, 7, 2);

        Assert.Multiple(() =>
        {
            Assert.That(size.IsLimitExceeded, Is.True);
            Assert.That(size.IsMalformed, Is.False);
            Assert.That(ranges.IsLimitExceeded, Is.True);
            Assert.That(units.IsLimitExceeded, Is.True);
            Assert.That(output.IsLimitExceeded, Is.True);
        });
    }
}
