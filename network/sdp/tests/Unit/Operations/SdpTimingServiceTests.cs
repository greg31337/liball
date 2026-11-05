using System;

namespace sdp.Tests.Unit.Operations;

public class SdpTimingServiceTests
{
    [Test]
    public void ConvertNtpToDateTimeOffset_ConvertsEpochUnixEpochAndPostEraValue()
    {
        ISdpTimingService service = new SdpTimingService();

        SdpNtpConversionResult epoch = service.ConvertNtpToDateTimeOffset("0");
        SdpNtpConversionResult unixEpoch = service.ConvertNtpToDateTimeOffset("2208988800");
        SdpNtpConversionResult postEra = service.ConvertNtpToDateTimeOffset("4294967296");

        Assert.Multiple(() =>
        {
            Assert.That(epoch.Success, Is.True);
            Assert.That(epoch.NtpValue, Is.EqualTo("0"));
            Assert.That(epoch.DateTimeValue, Is.EqualTo(new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(epoch.DiagnosticCode, Is.Empty);
            Assert.That(epoch.Detail, Is.Empty);

            Assert.That(unixEpoch.Success, Is.True);
            Assert.That(unixEpoch.DateTimeValue, Is.EqualTo(new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)));

            Assert.That(postEra.Success, Is.True);
            Assert.That(postEra.DateTimeValue, Is.EqualTo(new DateTimeOffset(2036, 2, 7, 6, 28, 16, TimeSpan.Zero)));
        });
    }

    [Test]
    public void ConvertDateTimeOffsetToNtp_NormalizesOffsetAndRoundTrips()
    {
        ISdpTimingService service = new SdpTimingService();
        DateTimeOffset localUnixEpoch = new DateTimeOffset(1970, 1, 1, 1, 0, 0, TimeSpan.FromHours(1));

        SdpNtpConversionResult encoded = service.ConvertDateTimeOffsetToNtp(localUnixEpoch);
        SdpNtpConversionResult decoded = service.ConvertNtpToDateTimeOffset(encoded.NtpValue);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.NtpValue, Is.EqualTo("2208988800"));
            Assert.That(encoded.DateTimeValue, Is.EqualTo(new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)));
            Assert.That(encoded.DateTimeValue.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.DateTimeValue, Is.EqualTo(encoded.DateTimeValue));
        });
    }

    [Test]
    public void Conversions_AcceptLatestWholeSecondRepresentableByDateTimeOffset()
    {
        ISdpTimingService service = new SdpTimingService();
        DateTimeOffset maximumWholeSecond = new DateTimeOffset(9999, 12, 31, 23, 59, 59, TimeSpan.Zero);

        SdpNtpConversionResult encoded = service.ConvertDateTimeOffsetToNtp(maximumWholeSecond);
        SdpNtpConversionResult decoded = service.ConvertNtpToDateTimeOffset("255611289599");

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.NtpValue, Is.EqualTo("255611289599"));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.DateTimeValue, Is.EqualTo(maximumWholeSecond));
        });
    }

    [TestCase("255611289600")]
    [TestCase("18446744073709551615")]
    public void ConvertNtpToDateTimeOffset_ReportsValuesBeyondDateTimeOffset(string value)
    {
        ISdpTimingService service = new SdpTimingService();

        SdpNtpConversionResult result = service.ConvertNtpToDateTimeOffset(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.NtpValue, Is.Empty);
            Assert.That(result.DateTimeValue, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(result.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.TimingUnrepresentable));
            Assert.That(result.Detail, Is.Not.Empty);
        });
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("-1")]
    [TestCase("+1")]
    [TestCase("1.5")]
    [TestCase("seconds")]
    public void ConvertNtpToDateTimeOffset_ReportsInvalidDecimalText(string value)
    {
        ISdpTimingService service = new SdpTimingService();

        SdpNtpConversionResult result = service.ConvertNtpToDateTimeOffset(value);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            Assert.That(result.Detail, Is.Not.Empty);
        });
    }

    [Test]
    public void ConvertNtpToDateTimeOffset_ReportsUnsignedIntegerOverflow()
    {
        ISdpTimingService service = new SdpTimingService();

        SdpNtpConversionResult result = service.ConvertNtpToDateTimeOffset("18446744073709551616");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));
            Assert.That(result.Detail, Is.Not.Empty);
        });
    }

    [Test]
    public void ConvertDateTimeOffsetToNtp_ReportsPreEpochAndFractionalInstants()
    {
        ISdpTimingService service = new SdpTimingService();
        DateTimeOffset epoch = new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);

        SdpNtpConversionResult preEpoch = service.ConvertDateTimeOffsetToNtp(epoch.AddTicks(-1));
        SdpNtpConversionResult fractional = service.ConvertDateTimeOffsetToNtp(epoch.AddTicks(1));

        Assert.Multiple(() =>
        {
            Assert.That(preEpoch.Success, Is.False);
            Assert.That(preEpoch.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.TimingUnrepresentable));
            Assert.That(preEpoch.Detail, Does.Contain("earlier"));
            Assert.That(fractional.Success, Is.False);
            Assert.That(fractional.DiagnosticCode, Is.EqualTo(SdpDiagnosticCodes.TimingUnrepresentable));
            Assert.That(fractional.Detail, Does.Contain("fractional"));
        });
    }

    [Test]
    public void ConvertNtpToDateTimeOffset_RejectsNullInput()
    {
        ISdpTimingService service = new SdpTimingService();

        Assert.That(() => service.ConvertNtpToDateTimeOffset(null!), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("ntpValue"));
    }
}
