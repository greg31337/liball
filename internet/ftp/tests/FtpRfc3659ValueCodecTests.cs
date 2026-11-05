namespace ftp.Tests;

public class FtpRfc3659ValueCodecTests
{
    [Test]
    public void Timestamp_parser_uses_UTC_and_preserves_exact_fractional_precision()
    {
        FtpTimestamp shortFraction = FtpTimestampCodec.ParseTimestampValue(
            "20240229123456.1");
        FtpTimestamp longFraction = FtpTimestampCodec.ParseTimestampValue(
            "20240229123456.1234567890123");

        Assert.Multiple(() =>
        {
            Assert.That(shortFraction.HasValue, Is.True);
            Assert.That(shortFraction.ValueUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(shortFraction.ValueUtc, Is.EqualTo(
                new DateTime(2024, 2, 29, 12, 34, 56, DateTimeKind.Utc).AddTicks(1000000)));
            Assert.That(shortFraction.FractionalSeconds, Is.EqualTo("1"));
            Assert.That(shortFraction.RawValue, Is.EqualTo("20240229123456.1"));
            Assert.That(shortFraction.LeapSecond, Is.False);
            Assert.That(longFraction.ValueUtc.Ticks % TimeSpan.TicksPerSecond, Is.EqualTo(1234567));
            Assert.That(longFraction.FractionalSeconds, Is.EqualTo("1234567890123"));
            Assert.That(longFraction.RawValue, Is.EqualTo("20240229123456.1234567890123"));
        });
    }

    [Test]
    public void Timestamp_formatter_round_trips_exact_wire_precision_without_adding_digits()
    {
        FtpTimestamp wholeSecond = FtpTimestampCodec.ParseTimestampValue("19980705132316");
        FtpTimestamp fractional = FtpTimestampCodec.ParseTimestampValue(
            "19980615100045.0140000009");

        Assert.Multiple(() =>
        {
            Assert.That(FtpTimestampCodec.FormatTimestampValue(wholeSecond),
                Is.EqualTo("19980705132316"));
            Assert.That(FtpTimestampCodec.FormatTimestampValue(fractional),
                Is.EqualTo("19980615100045.0140000009"));
        });
    }

    [Test]
    public void Timestamp_codec_supports_RFC_leap_second_60_explicitly()
    {
        FtpTimestamp leap = FtpTimestampCodec.ParseTimestampValue("20161231235960.5");
        FtpTimestamp upperBoundary = FtpTimestampCodec.ParseTimestampValue("99991231235960");

        Assert.Multiple(() =>
        {
            Assert.That(leap.LeapSecond, Is.True);
            Assert.That(leap.ValueUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(leap.ValueUtc.Second, Is.EqualTo(59));
            Assert.That(leap.ValueUtc.Ticks % TimeSpan.TicksPerSecond, Is.EqualTo(5000000));
            Assert.That(FtpTimestampCodec.FormatTimestampValue(leap),
                Is.EqualTo("20161231235960.5"));
            Assert.That(upperBoundary.LeapSecond, Is.True);
            Assert.That(FtpTimestampCodec.FormatTimestampValue(upperBoundary),
                Is.EqualTo("99991231235960"));
        });
    }

    [Test]
    public void Timestamp_parser_enforces_RFC_year_calendar_and_clock_ranges()
    {
        FtpTimestamp lowerBoundary = FtpTimestampCodec.ParseTimestampValue("10000101000000");

        Assert.Multiple(() =>
        {
            Assert.That(lowerBoundary.ValueUtc.Year, Is.EqualTo(1000));
            Assert.Throws<FtpProtocolException>(() =>
                FtpTimestampCodec.ParseTimestampValue("09991231235959"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpTimestampCodec.ParseTimestampValue("20230229000000"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpTimestampCodec.ParseTimestampValue("20241301000000"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpTimestampCodec.ParseTimestampValue("20240101240000"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpTimestampCodec.ParseTimestampValue("20240101006000"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpTimestampCodec.ParseTimestampValue("20240101000061"));
        });
    }

    [Test]
    public void Timestamp_parser_rejects_non_digits_offsets_and_empty_fractions()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                FtpTimestampCodec.ParseTimestampValue("2024010100000X"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpTimestampCodec.ParseTimestampValue("20240101000000."));
            Assert.Throws<FtpProtocolException>(() =>
                FtpTimestampCodec.ParseTimestampValue("20240101000000.12Z"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpTimestampCodec.ParseTimestampValue("20240101000000Z"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpTimestampCodec.ParseTimestampValue(" 20240101000000"));
        });
    }

    [Test]
    public void Null_timestamp_is_unavailable_and_cannot_be_formatted()
    {
        FtpTimestamp unavailable = FtpTimestampCodec.ParseTimestampValue(null);

        Assert.Multiple(() =>
        {
            Assert.That(unavailable.HasValue, Is.False);
            Assert.That(unavailable.ValueUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(unavailable.FractionalSeconds, Is.Empty);
            Assert.That(unavailable.RawValue, Is.Empty);
            Assert.That(unavailable.LeapSecond, Is.False);
            Assert.Throws<FtpProtocolException>(() =>
                FtpTimestampCodec.FormatTimestampValue(unavailable));
        });
    }

    [Test]
    public void Timestamp_formatter_rejects_non_UTC_and_inconsistent_precision()
    {
        FtpTimestamp local = CreateTimestamp(
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Local), "");
        FtpTimestamp missingFraction = CreateTimestamp(
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(1), "");
        FtpTimestamp mismatchedFraction = CreateTimestamp(
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(1200000), "13");
        FtpTimestamp invalidLeap = CreateTimestamp(
            new DateTime(2024, 1, 1, 0, 0, 58, DateTimeKind.Utc), "");
        invalidLeap.LeapSecond = true;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                FtpTimestampCodec.FormatTimestampValue(local));
            Assert.Throws<FtpProtocolException>(() =>
                FtpTimestampCodec.FormatTimestampValue(missingFraction));
            Assert.Throws<FtpProtocolException>(() =>
                FtpTimestampCodec.FormatTimestampValue(mismatchedFraction));
            Assert.Throws<FtpProtocolException>(() =>
                FtpTimestampCodec.FormatTimestampValue(invalidLeap));
        });
    }

    [Test]
    public void Size_parser_preserves_availability_raw_digits_and_int64_boundaries()
    {
        FtpSizeValue unavailable = FtpSizeCodec.ParseSizeValue(null);
        FtpSizeValue leadingZeros = FtpSizeCodec.ParseSizeValue("00042");
        FtpSizeValue maximum = FtpSizeCodec.ParseSizeValue("9223372036854775807");

        Assert.Multiple(() =>
        {
            Assert.That(unavailable.HasValue, Is.False);
            Assert.That(unavailable.Value, Is.Zero);
            Assert.That(unavailable.RawValue, Is.Empty);
            Assert.That(leadingZeros.HasValue, Is.True);
            Assert.That(leadingZeros.Value, Is.EqualTo(42));
            Assert.That(leadingZeros.RawValue, Is.EqualTo("00042"));
            Assert.That(maximum.Value, Is.EqualTo(long.MaxValue));
            Assert.That(FtpSizeCodec.FormatSizeValue(leadingZeros), Is.EqualTo("42"));
        });
    }

    [Test]
    public void Size_codec_rejects_empty_signed_spaced_overflow_and_negative_values()
    {
        FtpSizeValue negative = new FtpSizeValue();
        negative.HasValue = true;
        negative.Value = -1;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() => FtpSizeCodec.ParseSizeValue(""));
            Assert.Throws<FtpProtocolException>(() => FtpSizeCodec.ParseSizeValue("+1"));
            Assert.Throws<FtpProtocolException>(() => FtpSizeCodec.ParseSizeValue("-1"));
            Assert.Throws<FtpProtocolException>(() => FtpSizeCodec.ParseSizeValue(" 1"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpSizeCodec.ParseSizeValue("9223372036854775808"));
            Assert.Throws<FtpProtocolException>(() =>
                FtpSizeCodec.FormatSizeValue(new FtpSizeValue()));
            Assert.Throws<FtpProtocolException>(() => FtpSizeCodec.FormatSizeValue(negative));
            Assert.Throws<FtpProtocolException>(() => FtpSizeCodec.CreateSizeReply(-1));
        });
    }

    [Test]
    public void Codecs_generate_single_line_213_payloads_consumed_by_typed_extractors()
    {
        FtpTimestamp timestamp = FtpTimestampCodec.ParseTimestampValue("20240506070809.25");
        FtpReply sizeReply = FtpSizeCodec.CreateSizeReply(12345);
        FtpReply timestampReply = FtpTimestampCodec.CreateTimestampReply(timestamp);

        Assert.Multiple(() =>
        {
            Assert.That(sizeReply.Code, Is.EqualTo(213));
            Assert.That(sizeReply.Text, Is.EqualTo("12345"));
            Assert.That(sizeReply.Multiline, Is.False);
            Assert.That(FtpReplyExtractor.ExtractSize(sizeReply), Is.EqualTo(12345));
            Assert.That(timestampReply.Code, Is.EqualTo(213));
            Assert.That(timestampReply.Text, Is.EqualTo("20240506070809.25"));
            Assert.That(FtpReplyExtractor.ExtractModificationTime(timestampReply).ValueUtc,
                Is.EqualTo(timestamp.ValueUtc));
        });
    }

    private static FtpTimestamp CreateTimestamp(DateTime valueUtc, string fraction)
    {
        FtpTimestamp timestamp = new FtpTimestamp();
        timestamp.HasValue = true;
        timestamp.ValueUtc = valueUtc;
        timestamp.FractionalSeconds = fraction;
        return timestamp;
    }
}
