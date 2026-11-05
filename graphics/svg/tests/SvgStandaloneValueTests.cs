namespace svg.Tests;

public class SvgStandaloneValueTests
{
    [Test]
    public void NumberAndCoordinateInputsHaveStringSpanParityAndExactOffsets()
    {
        SvgValueService service = new SvgValueService();
        SvgValueParseOptions options = new SvgValueParseOptions();
        SvgValueNumberResult fromString = service.ParseNumberWithString("  -.5e2 ", options);
        SvgValueNumberResult fromSpan = service.ParseNumberWithReadOnlySpanOfChar("  -.5e2 ".AsSpan(), options);
        SvgValueNumberResult coordinate = service.ParseCoordinateWithString("12  px", options);

        Assert.Multiple(() =>
        {
            Assert.That(fromString.Success, Is.True);
            Assert.That(fromString.Value, Is.EqualTo(-50));
            Assert.That(fromString.StartOffset, Is.EqualTo(2));
            Assert.That(fromString.NextOffset, Is.EqualTo(8));
            Assert.That(fromString.ErrorOffset, Is.EqualTo(-1));
            Assert.That(fromSpan.Success, Is.EqualTo(fromString.Success));
            Assert.That(fromSpan.Value, Is.EqualTo(fromString.Value));
            Assert.That(fromSpan.StartOffset, Is.EqualTo(fromString.StartOffset));
            Assert.That(fromSpan.NextOffset, Is.EqualTo(fromString.NextOffset));
            Assert.That(coordinate.Success, Is.False);
            Assert.That(coordinate.Error, Is.EqualTo(SvgValueParseError.TrailingCharacters));
            Assert.That(coordinate.ErrorOffset, Is.EqualTo(4));
            Assert.That(coordinate.Value, Is.Zero);
            Assert.That(coordinate.Recovered, Is.False);
        });
    }

    [Test]
    public void RecoveryControlsWhetherAValidScalarOrPairPrefixIsRetained()
    {
        SvgValueService service = new SvgValueService();
        SvgValueParseOptions strict = new SvgValueParseOptions();
        SvgValueParseOptions compatible = new SvgValueParseOptions();
        compatible.RecoveryMode = SvgValueRecoveryMode.KeepValidPrefix;

        SvgValueNumberResult strictNumber = service.ParseNumberWithString("12px", strict);
        SvgValueNumberResult recoveredNumber = service.ParseNumberWithString("12px", compatible);
        SvgValueOptionalPairResult strictPair = service.ParseOptionalPairWithString("2, nope", strict);
        SvgValueOptionalPairResult recoveredPair = service.ParseOptionalPairWithReadOnlySpanOfChar("2, nope".AsSpan(), compatible);

        Assert.Multiple(() =>
        {
            Assert.That(strictNumber.Value, Is.Zero);
            Assert.That(strictNumber.Recovered, Is.False);
            Assert.That(recoveredNumber.Success, Is.False);
            Assert.That(recoveredNumber.Error, Is.EqualTo(SvgValueParseError.TrailingCharacters));
            Assert.That(recoveredNumber.ErrorOffset, Is.EqualTo(2));
            Assert.That(recoveredNumber.Value, Is.EqualTo(12));
            Assert.That(recoveredNumber.NextOffset, Is.EqualTo(2));
            Assert.That(recoveredNumber.Recovered, Is.True);
            Assert.That(strictPair.First, Is.Zero);
            Assert.That(recoveredPair.Success, Is.False);
            Assert.That(recoveredPair.Error, Is.EqualTo(SvgValueParseError.ExpectedNumber));
            Assert.That(recoveredPair.ErrorOffset, Is.EqualTo(3));
            Assert.That(recoveredPair.First, Is.EqualTo(2));
            Assert.That(recoveredPair.HasSecond, Is.False);
            Assert.That(recoveredPair.Recovered, Is.True);
        });
    }

    [Test]
    public void IntegerParsingReportsRangeAndTokenFailures()
    {
        SvgValueService service = new SvgValueService();
        SvgValueParseOptions options = new SvgValueParseOptions();
        SvgValueIntegerResult minimum = service.ParseIntegerWithString("-2147483648", options);
        SvgValueIntegerResult overflow = service.ParseIntegerWithReadOnlySpanOfChar("2147483648".AsSpan(), options);
        options.MaxTokenCharacters = 2;
        SvgValueIntegerResult tooLong = service.ParseIntegerWithString("123", options);

        Assert.Multiple(() =>
        {
            Assert.That(minimum.Success, Is.True);
            Assert.That(minimum.Value, Is.EqualTo(int.MinValue));
            Assert.That(overflow.Success, Is.False);
            Assert.That(overflow.Error, Is.EqualTo(SvgValueParseError.Overflow));
            Assert.That(overflow.ErrorOffset, Is.EqualTo(9));
            Assert.That(tooLong.Error, Is.EqualTo(SvgValueParseError.TokenTooLong));
            Assert.That(tooLong.ErrorOffset, Is.EqualTo(2));
        });
    }

    [Test]
    public void BoundedNumberAndIntegerListsHonorSeparatorAndEmptyPolicies()
    {
        SvgValueService service = new SvgValueService();
        SvgValueParseOptions options = new SvgValueParseOptions();
        options.MaxItems = 3;
        SvgValueNumberListResult numbers = service.ParseNumberListWithString("1, 2,-3", SvgValueListSeparator.Comma, options);
        SvgValueIntegerListResult integers = service.ParseIntegerListWithReadOnlySpanOfChar("1; 2;-3".AsSpan(), SvgValueListSeparator.Semicolon, options);
        SvgValueNumberListResult emptyRejected = service.ParseNumberListWithString(" \n", SvgValueListSeparator.CommaOrWhitespace, options);
        options.AllowEmpty = true;
        SvgValueNumberListResult emptyAllowed = service.ParseNumberListWithString(" \n", SvgValueListSeparator.CommaOrWhitespace, options);

        Assert.Multiple(() =>
        {
            Assert.That(numbers.Success, Is.True);
            Assert.That(numbers.Values, Is.EqualTo(new[] { 1.0, 2.0, -3.0 }));
            Assert.That(integers.Success, Is.True);
            Assert.That(integers.Values, Is.EqualTo(new[] { 1, 2, -3 }));
            Assert.That(emptyRejected.Error, Is.EqualTo(SvgValueParseError.ExpectedNumber));
            Assert.That(emptyRejected.ErrorOffset, Is.EqualTo(2));
            Assert.That(emptyAllowed.Success, Is.True);
            Assert.That(emptyAllowed.Values, Is.Empty);
        });
    }

    [Test]
    public void ListRecoveryRetainsOnlyTheValidBoundedPrefix()
    {
        SvgValueService service = new SvgValueService();
        SvgValueParseOptions strict = new SvgValueParseOptions();
        strict.MaxItems = 2;
        SvgValueParseOptions compatible = new SvgValueParseOptions();
        compatible.MaxItems = 2;
        compatible.RecoveryMode = SvgValueRecoveryMode.KeepValidPrefix;

        SvgValueNumberListResult strictResult = service.ParseNumberListWithString("1 2 3", SvgValueListSeparator.Whitespace, strict);
        SvgValueNumberListResult recovered = service.ParseNumberListWithString("1 2 3", SvgValueListSeparator.Whitespace, compatible);
        SvgValueNumberListResult malformed = service.ParseNumberListWithString("1 2e+ x", SvgValueListSeparator.Whitespace, compatible);

        Assert.Multiple(() =>
        {
            Assert.That(strictResult.Error, Is.EqualTo(SvgValueParseError.ItemLimitExceeded));
            Assert.That(strictResult.ErrorOffset, Is.EqualTo(4));
            Assert.That(strictResult.Values, Is.Empty);
            Assert.That(recovered.Error, Is.EqualTo(SvgValueParseError.ItemLimitExceeded));
            Assert.That(recovered.Values, Is.EqualTo(new[] { 1.0, 2.0 }));
            Assert.That(recovered.Recovered, Is.True);
            Assert.That(malformed.Error, Is.EqualTo(SvgValueParseError.InvalidExponent));
            Assert.That(malformed.ErrorOffset, Is.EqualTo(5));
            Assert.That(malformed.Values, Is.EqualTo(new[] { 1.0 }));
        });
    }

    [Test]
    public void PointListsReturnFinitePointsAndRecoverOnlyCompletePairs()
    {
        SvgValueService service = new SvgValueService();
        SvgValueParseOptions options = new SvgValueParseOptions();
        options.MaxItems = 3;
        SvgValuePointListResult points = service.ParsePointListWithString("0,0 10,20 -5.5,6", options);
        options.RecoveryMode = SvgValueRecoveryMode.KeepValidPrefix;
        SvgValuePointListResult odd = service.ParsePointListWithReadOnlySpanOfChar("0,0 1".AsSpan(), options);
        options.MaxItems = 1;
        SvgValuePointListResult bounded = service.ParsePointListWithString("0 0 1 1", options);

        Assert.Multiple(() =>
        {
            Assert.That(points.Success, Is.True);
            Assert.That(points.Points.Count, Is.EqualTo(3));
            Assert.That(points.Points[1].X, Is.EqualTo(10));
            Assert.That(points.Points[1].Y, Is.EqualTo(20));
            Assert.That(odd.Success, Is.False);
            Assert.That(odd.Error, Is.EqualTo(SvgValueParseError.OddCoordinateCount));
            Assert.That(odd.ErrorOffset, Is.EqualTo(5));
            Assert.That(odd.Points.Count, Is.EqualTo(1));
            Assert.That(odd.Recovered, Is.True);
            Assert.That(bounded.Error, Is.EqualTo(SvgValueParseError.ItemLimitExceeded));
            Assert.That(bounded.ErrorOffset, Is.EqualTo(4));
            Assert.That(bounded.Points.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void FormattingProvidesShortestCanonicalAndCompactOutput()
    {
        ISvgValueService service = new SvgValueService();
        SvgValueFormatResult shortest = service.FormatNumber(0.5, SvgStandaloneValueFormatMode.ShortestRoundTrip);
        SvgValueFormatResult canonical = service.FormatNumber(1e20, SvgStandaloneValueFormatMode.Canonical);
        SvgValueFormatResult compact = service.FormatNumber(0.5, SvgStandaloneValueFormatMode.Compact);
        SvgValueFormatResult pair = service.FormatOptionalPair(0.5, -0.5, true, SvgStandaloneValueFormatMode.Compact);
        SvgValueFormatResult numbers = service.FormatNumberList(new[] { 0.5, 2.0 }, SvgValueListSeparator.Comma, SvgStandaloneValueFormatMode.Canonical);
        SvgValueFormatResult integers = service.FormatIntegerList(new[] { -2, 3 }, SvgValueListSeparator.Semicolon);
        SvgValueFormatResult points = service.FormatPointList(new List<SvgPoint> { new SvgPoint(1, 2), new SvgPoint(3, 4) }, SvgStandaloneValueFormatMode.Canonical);

        Assert.Multiple(() =>
        {
            Assert.That(shortest.Text, Is.EqualTo("0.5"));
            Assert.That(canonical.Text, Is.EqualTo("100000000000000000000"));
            Assert.That(compact.Text, Is.EqualTo(".5"));
            Assert.That(pair.Text, Is.EqualTo(".5,-.5"));
            Assert.That(numbers.Text, Is.EqualTo("0.5, 2"));
            Assert.That(integers.Text, Is.EqualTo("-2; 3"));
            Assert.That(points.Text, Is.EqualTo("1,2 3,4"));
        });
    }

    [Test]
    public void SpanFormattingIsAtomicAndReportsRequiredCapacity()
    {
        SvgValueService service = new SvgValueService();
        char[] tooSmall = new[] { 'x', 'x' };
        SvgValueFormatResult failed = service.FormatNumberToSpan(123.5, SvgStandaloneValueFormatMode.Canonical, tooSmall.AsSpan());
        char[] destination = new char[5];
        SvgValueFormatResult copied = service.FormatNumberToSpan(123.5, SvgStandaloneValueFormatMode.Canonical, destination.AsSpan());
        char[] listDestination = new char[3];
        SvgValueFormatResult listCopied = service.FormatNumberListToSpan(new[] { 1.0, 2.0 }, SvgValueListSeparator.Whitespace, SvgStandaloneValueFormatMode.Canonical, listDestination.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.Error, Is.EqualTo(SvgValueFormatError.DestinationTooSmall));
            Assert.That(failed.RequiredLength, Is.EqualTo(5));
            Assert.That(failed.WrittenLength, Is.Zero);
            Assert.That(tooSmall, Is.EqualTo(new[] { 'x', 'x' }));
            Assert.That(copied.Success, Is.True);
            Assert.That(copied.WrittenLength, Is.EqualTo(5));
            Assert.That(new string(destination), Is.EqualTo("123.5"));
            Assert.That(listCopied.Success, Is.True);
            Assert.That(new string(listDestination), Is.EqualTo("1 2"));
        });
    }

    [Test]
    public void InvalidInputsReturnStableFailuresWithoutThrowing()
    {
        SvgValueService service = new SvgValueService();
        SvgValueParseOptions invalidOptions = new SvgValueParseOptions();
        invalidOptions.MaxItems = 0;
        SvgValueNumberResult nullInput = service.ParseNumberWithString(null, new SvgValueParseOptions());
        SvgValueIntegerListResult invalidList = service.ParseIntegerListWithString("1", (SvgValueListSeparator)999, invalidOptions);
        SvgValueFormatResult nonFinite = service.FormatNumber(double.NaN, SvgStandaloneValueFormatMode.Canonical);
        SvgValueFormatResult invalidMode = service.FormatNumber(1, (SvgStandaloneValueFormatMode)999);
        SvgValueFormatResult invalidPoints = service.FormatPointList(null, SvgStandaloneValueFormatMode.Canonical);

        Assert.Multiple(() =>
        {
            Assert.That(nullInput.Error, Is.EqualTo(SvgValueParseError.InvalidArgument));
            Assert.That(invalidList.Error, Is.EqualTo(SvgValueParseError.InvalidArgument));
            Assert.That(nonFinite.Error, Is.EqualTo(SvgValueFormatError.NonFinite));
            Assert.That(invalidMode.Error, Is.EqualTo(SvgValueFormatError.InvalidArgument));
            Assert.That(invalidPoints.Error, Is.EqualTo(SvgValueFormatError.InvalidArgument));
        });
    }
}
