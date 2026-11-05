using System.Text;

namespace http.Tests;

public class MediaTypeCodecTests
{
    [Test]
    public void QualityParserAcceptsTheCompleteThousandthPrecisionGrammar()
    {
        string[] inputs = ["0", "0.", "0.1", "0.01", "0.001", "0.999", "1", "1.", "1.0", "1.00", "1.000"];
        int[] expected = [0, 0, 100, 10, 1, 999, 1000, 1000, 1000, 1000, 1000];

        for (int index = 0; index < inputs.Length; index++)
        {
            QualityValueParseResult result = FieldValueParser.ParseQualityValue(
                Encoding.ASCII.GetBytes(inputs[index]),
                8);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done), inputs[index]);
                Assert.That(result.Quality, Is.EqualTo(expected[index]), inputs[index]);
                Assert.That(result.Consumed, Is.EqualTo(inputs[index].Length), inputs[index]);
            });
        }
    }

    [Test]
    public void QualityParserRejectsOutOfRangeAndOverPreciseForms()
    {
        string[] inputs = ["", ".5", "00", "01", "2", "-1", "0.0000", "0.12x", "1.001", "1.0000", " 0.5"];

        foreach (string input in inputs)
        {
            QualityValueParseResult result = FieldValueParser.ParseQualityValue(
                Encoding.ASCII.GetBytes(input),
                8);
            Assert.That(result.IsMalformed, Is.True, input);
        }

        QualityValueParseResult limited = FieldValueParser.ParseQualityValue("0.5"u8.ToArray(), 2);
        Assert.Multiple(() =>
        {
            Assert.That(limited.IsLimitExceeded, Is.True);
            Assert.That(limited.IsMalformed, Is.False);
        });
    }

    [Test]
    public void QualitySerializerUsesTheShortestExactRepresentation()
    {
        int[] values = [0, 1, 10, 50, 100, 500, 999, 1000];
        string[] expected = ["0", "0.001", "0.01", "0.05", "0.1", "0.5", "0.999", "1"];

        for (int index = 0; index < values.Length; index++)
        {
            FieldValueWriteResult written = FieldValueWriter.SerializeQualityValue(values[index], 5);
            QualityValueParseResult parsed = FieldValueParser.ParseQualityValue(written.Bytes, 5);

            Assert.Multiple(() =>
            {
                Assert.That(Encoding.ASCII.GetString(written.Bytes), Is.EqualTo(expected[index]));
                Assert.That(parsed.Quality, Is.EqualTo(values[index]));
            });
        }

        Assert.That(FieldValueWriter.SerializeQualityValue(-1, 5).IsMalformed, Is.True);
        Assert.That(FieldValueWriter.SerializeQualityValue(1001, 5).IsMalformed, Is.True);
        Assert.That(FieldValueWriter.SerializeQualityValue(1, 4).IsLimitExceeded, Is.True);
    }

    [Test]
    public void MediaTypeParserPreservesCaseAndDecodesQuotedParameters()
    {
        byte[] input = "Text/Plain ; Charset=\"utf-8\"; note=\"a\\\"b\\\\c\"; mode=flowed"u8.ToArray();
        MediaTypeParseResult result = FieldValueParser.ParseMediaType(input, 128, 8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(input.Length));
            Assert.That(result.Value.Type, Is.EqualTo("Text"));
            Assert.That(result.Value.Subtype, Is.EqualTo("Plain"));
            Assert.That(result.Value.IsTypeWildcard, Is.False);
            Assert.That(result.Value.IsSubtypeWildcard, Is.False);
            Assert.That(result.Value.Parameters, Has.Count.EqualTo(3));
            Assert.That(result.Value.Parameters[0].Name, Is.EqualTo("Charset"));
            Assert.That(result.Value.Parameters[0].IsQuoted, Is.True);
            Assert.That(result.Value.Parameters[0].Value, Is.EqualTo("utf-8"u8.ToArray()));
            Assert.That(result.Value.Parameters[1].Value, Is.EqualTo("a\"b\\c"u8.ToArray()));
            Assert.That(result.Value.Parameters[2].IsQuoted, Is.False);
        });
    }

    [Test]
    public void MediaTypeAndRangeParsersApplyWildcardAndWhitespaceRules()
    {
        MediaTypeParseResult all = FieldValueParser.ParseMediaRange("*/*"u8.ToArray(), 16, 2);
        MediaTypeParseResult subtype = FieldValueParser.ParseMediaRange("image/*"u8.ToArray(), 16, 2);
        MediaTypeParseResult exact = FieldValueParser.ParseMediaRange("image/svg+xml"u8.ToArray(), 32, 2);
        MediaTypeParseResult contentTypeWildcard = FieldValueParser.ParseMediaType("image/*"u8.ToArray(), 16, 2);
        MediaTypeParseResult partialWildcard = FieldValueParser.ParseMediaRange("*/json"u8.ToArray(), 16, 2);
        MediaTypeParseResult spacedSlash = FieldValueParser.ParseMediaRange("image /png"u8.ToArray(), 16, 2);
        MediaTypeParseResult trailingWhitespace = FieldValueParser.ParseMediaType("image/png "u8.ToArray(), 16, 2);

        Assert.Multiple(() =>
        {
            Assert.That(all.Value.IsTypeWildcard, Is.True);
            Assert.That(all.Value.IsSubtypeWildcard, Is.True);
            Assert.That(subtype.Value.Type, Is.EqualTo("image"));
            Assert.That(subtype.Value.IsSubtypeWildcard, Is.True);
            Assert.That(exact.Value.Subtype, Is.EqualTo("svg+xml"));
            Assert.That(contentTypeWildcard.IsMalformed, Is.True);
            Assert.That(partialWildcard.IsMalformed, Is.True);
            Assert.That(spacedSlash.IsMalformed, Is.True);
            Assert.That(trailingWhitespace.IsMalformed, Is.True);
        });
    }

    [Test]
    public void WeightedMediaRangesExtractQualityAndDeterministicSpecificity()
    {
        byte[] input = " , */*;q=0.1, text/*;level=1;q=0.2, Text/Plain;format=\"flowed,yes\";Q=0.750, "u8.ToArray();

        MediaTypeParseResult result = FieldValueParser.ParseWeightedMediaRanges(input, 128, 8, 8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.EmptyElements, Is.EqualTo(2));
            Assert.That(result.Values, Has.Count.EqualTo(3));
            Assert.That(result.Values[0].Value, Is.EqualTo("*/*"));
            Assert.That(result.Values[0].Quality, Is.EqualTo(100));
            Assert.That(result.Values[0].Specificity, Is.EqualTo(0));
            Assert.That(result.Values[0].SourceOrder, Is.EqualTo(0));
            Assert.That(result.Values[1].Value, Is.EqualTo("text/*"));
            Assert.That(result.Values[1].Quality, Is.EqualTo(200));
            Assert.That(result.Values[1].Specificity, Is.EqualTo(1001));
            Assert.That(result.Values[2].Value, Is.EqualTo("Text/Plain"));
            Assert.That(result.Values[2].Quality, Is.EqualTo(750));
            Assert.That(result.Values[2].Specificity, Is.EqualTo(2001));
            Assert.That(result.Values[2].Parameters[0].Value, Is.EqualTo("flowed,yes"u8.ToArray()));
        });
    }

    [Test]
    public void WeightedParsingRejectsDuplicateConflictingAndInvalidQualityParameters()
    {
        MediaTypeParseResult duplicate = FieldValueParser.ParseWeightedMediaRanges(
            "text/plain;q=0.5;Q=0.500"u8.ToArray(),
            64,
            4,
            4);
        MediaTypeParseResult conflict = FieldValueParser.ParseWeightedMediaRanges(
            "text/plain;q=0.5;q=0.4"u8.ToArray(),
            64,
            4,
            4);
        MediaTypeParseResult quoted = FieldValueParser.ParseWeightedMediaRanges(
            "text/plain;q=\"0.5\""u8.ToArray(),
            64,
            4,
            4);
        MediaTypeParseResult precision = FieldValueParser.ParseWeightedMediaRanges(
            "text/plain;q=0.1234"u8.ToArray(),
            64,
            4,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.IsMalformed, Is.True);
            Assert.That(duplicate.IsDuplicateQuality, Is.True);
            Assert.That(duplicate.IsConflictingQuality, Is.False);
            Assert.That(conflict.IsDuplicateQuality, Is.True);
            Assert.That(conflict.IsConflictingQuality, Is.True);
            Assert.That(quoted.IsMalformed, Is.True);
            Assert.That(precision.IsMalformed, Is.True);
        });
    }

    [Test]
    public void MediaSerializationCanonicalizesSeparatorsAndRoundTripsParameters()
    {
        HttpMediaType value = new HttpMediaType();
        value.Type = "Application";
        value.Subtype = "Example+Json";
        value.Parameters.Add(new HttpParameter
        {
            Name = "profile",
            HasValue = true,
            Value = "a b\"c"u8.ToArray(),
            IsQuoted = true
        });
        value.Parameters.Add(new HttpParameter
        {
            Name = "version",
            HasValue = true,
            Value = "2"u8.ToArray()
        });

        FieldValueWriteResult written = FieldValueWriter.SerializeMediaType(value, 128, 4);
        MediaTypeParseResult parsed = FieldValueParser.ParseMediaType(written.Bytes, 128, 4);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(written.Bytes),
                Is.EqualTo("Application/Example+Json; profile=\"a b\\\"c\"; version=2"));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Value.Type, Is.EqualTo("Application"));
            Assert.That(parsed.Value.Parameters[0].Value, Is.EqualTo("a b\"c"u8.ToArray()));
        });
    }

    [Test]
    public void WeightedMediaSerializationPlacesQualityLastAndOmitsItsDefault()
    {
        HttpWeightedValue wildcard = new HttpWeightedValue();
        wildcard.Value = "image/*";
        wildcard.Quality = 250;
        wildcard.Parameters.Add(new HttpParameter
        {
            Name = "level",
            HasValue = true,
            Value = "1"u8.ToArray()
        });

        HttpWeightedValue exact = new HttpWeightedValue();
        exact.Value = "image/png";

        FieldValueWriteResult written = FieldValueWriter.SerializeWeightedMediaRanges(
            new List<HttpWeightedValue> { wildcard, exact },
            128,
            4,
            4);
        MediaTypeParseResult parsed = FieldValueParser.ParseWeightedMediaRanges(
            written.Bytes,
            128,
            4,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(written.Bytes),
                Is.EqualTo("image/*; level=1; q=0.25, image/png"));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Values[0].Quality, Is.EqualTo(250));
            Assert.That(parsed.Values[1].Quality, Is.EqualTo(1000));
        });
    }

    [Test]
    public void GenericWeightedTokensSupportWildcardsQuotedParametersAndRoundTrip()
    {
        MediaTypeParseResult parsed = FieldValueParser.ParseWeightedValues(
            "gzip; note=\"fast, usually\";q=1, *;q=0"u8.ToArray(),
            96,
            4,
            4);
        FieldValueWriteResult written = FieldValueWriter.SerializeWeightedValues(
            parsed.Values,
            96,
            4,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Values[0].Specificity, Is.EqualTo(1001));
            Assert.That(parsed.Values[1].Specificity, Is.EqualTo(0));
            Assert.That(Encoding.ASCII.GetString(written.Bytes),
                Is.EqualTo("gzip; note=\"fast, usually\", *; q=0"));
        });
    }

    [Test]
    public void MediaOperationsDistinguishMalformedStateFromConfiguredLimits()
    {
        MediaTypeParseResult size = FieldValueParser.ParseMediaType("text/plain"u8.ToArray(), 9, 4);
        MediaTypeParseResult parameters = FieldValueParser.ParseMediaType(
            "text/plain;a=1;b=2"u8.ToArray(),
            64,
            1);
        MediaTypeParseResult members = FieldValueParser.ParseWeightedMediaRanges(
            "text/plain,image/png"u8.ToArray(),
            64,
            1,
            2);

        HttpMediaType invalidRange = new HttpMediaType();
        invalidRange.IsTypeWildcard = true;
        invalidRange.Subtype = "json";
        FieldValueWriteResult malformed = FieldValueWriter.SerializeMediaRange(invalidRange, 64, 2);

        HttpWeightedValue reserved = new HttpWeightedValue();
        reserved.Value = "text/plain";
        reserved.Parameters.Add(new HttpParameter
        {
            Name = "Q",
            HasValue = true,
            Value = "0.5"u8.ToArray()
        });
        FieldValueWriteResult duplicateQuality = FieldValueWriter.SerializeWeightedMediaRanges(
            new List<HttpWeightedValue> { reserved },
            64,
            2,
            2);

        Assert.Multiple(() =>
        {
            Assert.That(size.IsLimitExceeded, Is.True);
            Assert.That(parameters.IsLimitExceeded, Is.True);
            Assert.That(members.IsLimitExceeded, Is.True);
            Assert.That(malformed.IsMalformed, Is.True);
            Assert.That(duplicateQuality.IsMalformed, Is.True);
        });
    }
}
