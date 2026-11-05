namespace svg.Tests;

public class SvgListParserTests
{
    [Test]
    public void CoordinateListsAcceptMixedCommaAndWhitespaceSeparators()
    {
        SvgNumberListResult result = SvgListParser.ParseCoordinateList(" 1, 2 3,\n-4 .5 ", 10, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgListParseError.None));
            Assert.That(result.NumberError, Is.EqualTo(SvgNumberParseError.None));
            Assert.That(result.Values, Is.EqualTo(new[] { 1.0, 2.0, 3.0, -4.0, 0.5 }));
            Assert.That(result.Start, Is.EqualTo(1));
            Assert.That(result.NextOffset, Is.EqualTo(15));
            Assert.That(result.ErrorOffset, Is.EqualTo(-1));
        });
    }

    [Test]
    public void ExplicitNumberListSeparatorModesAcceptTheirOwnGrammar()
    {
        SvgNumberListResult comma = SvgListParser.ParseCommaNumberList("1, 2 ,3", 5, 32, false);
        SvgNumberListResult whitespace = SvgListParser.ParseWhitespaceNumberList("1 2\t3", 5, 32, false);
        SvgNumberListResult semicolon = SvgListParser.ParseSemicolonNumberList("1 ; 2;-3", 5, 32, false);

        Assert.Multiple(() =>
        {
            Assert.That(comma.Success, Is.True);
            Assert.That(comma.Values, Is.EqualTo(new[] { 1.0, 2.0, 3.0 }));
            Assert.That(whitespace.Success, Is.True);
            Assert.That(whitespace.Values, Is.EqualTo(new[] { 1.0, 2.0, 3.0 }));
            Assert.That(semicolon.Success, Is.True);
            Assert.That(semicolon.Values, Is.EqualTo(new[] { 1.0, 2.0, -3.0 }));
        });
    }

    [Test]
    public void ExplicitSeparatorModesRejectOtherSeparatorsAtExactOffsets()
    {
        SvgNumberListResult comma = SvgListParser.ParseCommaNumberList("1 2", 5, 32, false);
        SvgNumberListResult whitespace = SvgListParser.ParseWhitespaceNumberList("1,2", 5, 32, false);
        SvgNumberListResult semicolon = SvgListParser.ParseSemicolonNumberList("1,2", 5, 32, false);

        Assert.Multiple(() =>
        {
            Assert.That(comma.Success, Is.False);
            Assert.That(comma.Error, Is.EqualTo(SvgListParseError.MissingSeparator));
            Assert.That(comma.ErrorOffset, Is.EqualTo(2));
            Assert.That(whitespace.Success, Is.False);
            Assert.That(whitespace.Error, Is.EqualTo(SvgListParseError.MissingSeparator));
            Assert.That(whitespace.ErrorOffset, Is.EqualTo(1));
            Assert.That(semicolon.Success, Is.False);
            Assert.That(semicolon.Error, Is.EqualTo(SvgListParseError.MissingSeparator));
            Assert.That(semicolon.ErrorOffset, Is.EqualTo(1));
        });
    }

    [Test]
    public void IntegerListsUseTheSameSeparatorPoliciesAndIntegerRangeChecks()
    {
        SvgIntegerListResult comma = SvgListParser.ParseIntegerList("+1, 2,-3", SvgListSeparatorMode.Comma, 5, 32, false);
        SvgIntegerListResult whitespace = SvgListParser.ParseIntegerList("1 2\n3", SvgListSeparatorMode.Whitespace, 5, 32, false);
        SvgIntegerListResult semicolon = SvgListParser.ParseIntegerList("1;2 ; -3", SvgListSeparatorMode.Semicolon, 5, 32, false);
        SvgIntegerListResult overflow = SvgListParser.ParseIntegerList("1 2147483648", SvgListSeparatorMode.Whitespace, 5, 32, false);

        Assert.Multiple(() =>
        {
            Assert.That(comma.Success, Is.True);
            Assert.That(comma.Values, Is.EqualTo(new[] { 1, 2, -3 }));
            Assert.That(whitespace.Success, Is.True);
            Assert.That(whitespace.Values, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(semicolon.Success, Is.True);
            Assert.That(semicolon.Values, Is.EqualTo(new[] { 1, 2, -3 }));
            Assert.That(overflow.Success, Is.False);
            Assert.That(overflow.Error, Is.EqualTo(SvgListParseError.InvalidItem));
            Assert.That(overflow.NumberError, Is.EqualTo(SvgNumberParseError.Overflow));
            Assert.That(overflow.ErrorOffset, Is.EqualTo(11));
            Assert.That(overflow.Values, Is.EqualTo(new[] { 1 }));
        });
    }

    [Test]
    public void EmptyListPolicyIsExplicitAndTrailingWhitespaceIsNotASeparator()
    {
        SvgNumberListResult allowed = SvgListParser.ParseNumberList(" \n", SvgListSeparatorMode.CommaOrWhitespace, 5, 32, true);
        SvgNumberListResult required = SvgListParser.ParseNumberList(" \n", SvgListSeparatorMode.CommaOrWhitespace, 5, 32, false);
        SvgNumberListResult trailingWhitespace = SvgListParser.ParseCommaNumberList("1,2 \n", 5, 32, false);

        Assert.Multiple(() =>
        {
            Assert.That(allowed.Success, Is.True);
            Assert.That(allowed.Values, Is.Empty);
            Assert.That(allowed.Start, Is.EqualTo(2));
            Assert.That(allowed.NextOffset, Is.EqualTo(2));
            Assert.That(required.Success, Is.False);
            Assert.That(required.Error, Is.EqualTo(SvgListParseError.ExpectedItem));
            Assert.That(required.ErrorOffset, Is.EqualTo(2));
            Assert.That(trailingWhitespace.Success, Is.True);
            Assert.That(trailingWhitespace.Values, Is.EqualTo(new[] { 1.0, 2.0 }));
        });
    }

    [Test]
    public void MalformedItemsAndSeparatorsRetainTheSuccessfullyParsedPrefix()
    {
        SvgNumberListResult leading = SvgListParser.ParseCoordinateList(",1", 5, 32);
        SvgNumberListResult repeated = SvgListParser.ParseCoordinateList("1,,2", 5, 32);
        SvgNumberListResult trailing = SvgListParser.ParseCoordinateList("1, ", 5, 32);
        SvgNumberListResult exponent = SvgListParser.ParseCoordinateList("1 2e+ x", 5, 32);
        SvgNumberListResult compact = SvgListParser.ParseCoordinateList("1-2", 5, 32);

        Assert.Multiple(() =>
        {
            Assert.That(leading.Error, Is.EqualTo(SvgListParseError.ExpectedItem));
            Assert.That(leading.ErrorOffset, Is.Zero);
            Assert.That(leading.Values, Is.Empty);
            Assert.That(repeated.Error, Is.EqualTo(SvgListParseError.ExpectedItem));
            Assert.That(repeated.ErrorOffset, Is.EqualTo(2));
            Assert.That(repeated.Values, Is.EqualTo(new[] { 1.0 }));
            Assert.That(trailing.Error, Is.EqualTo(SvgListParseError.TrailingSeparator));
            Assert.That(trailing.ErrorOffset, Is.EqualTo(3));
            Assert.That(trailing.Values, Is.EqualTo(new[] { 1.0 }));
            Assert.That(exponent.Error, Is.EqualTo(SvgListParseError.InvalidItem));
            Assert.That(exponent.NumberError, Is.EqualTo(SvgNumberParseError.InvalidExponent));
            Assert.That(exponent.ErrorOffset, Is.EqualTo(5));
            Assert.That(exponent.Values, Is.EqualTo(new[] { 1.0 }));
            Assert.That(compact.Error, Is.EqualTo(SvgListParseError.MissingSeparator));
            Assert.That(compact.ErrorOffset, Is.EqualTo(1));
        });
    }

    [Test]
    public void ItemAndTokenLimitsFailBeforeAddingTheExcessItem()
    {
        SvgNumberListResult itemLimit = SvgListParser.ParseCoordinateList("1 2 3", 2, 32);
        SvgNumberListResult tokenLimit = SvgListParser.ParseCoordinateList("123 4", 5, 2);

        Assert.Multiple(() =>
        {
            Assert.That(itemLimit.Success, Is.False);
            Assert.That(itemLimit.Error, Is.EqualTo(SvgListParseError.ItemLimitExceeded));
            Assert.That(itemLimit.ErrorOffset, Is.EqualTo(4));
            Assert.That(itemLimit.NextOffset, Is.EqualTo(4));
            Assert.That(itemLimit.Values, Is.EqualTo(new[] { 1.0, 2.0 }));
            Assert.That(tokenLimit.Success, Is.False);
            Assert.That(tokenLimit.Error, Is.EqualTo(SvgListParseError.InvalidItem));
            Assert.That(tokenLimit.NumberError, Is.EqualTo(SvgNumberParseError.TokenTooLong));
            Assert.That(tokenLimit.ErrorOffset, Is.EqualTo(2));
            Assert.That(tokenLimit.Values, Is.Empty);
        });
    }

    [Test]
    public void CoordinateGrammarDelegatesToTheFiniteSvgNumberParser()
    {
        SvgNumberParseResult coordinate = SvgListParser.ParseCoordinate(" -.5e2 ", 16);
        SvgNumberParseResult overflow = SvgListParser.ParseCoordinate("1e309", 16);

        Assert.Multiple(() =>
        {
            Assert.That(coordinate.Success, Is.True);
            Assert.That(coordinate.Value, Is.EqualTo(-50));
            Assert.That(overflow.Success, Is.False);
            Assert.That(overflow.Error, Is.EqualTo(SvgNumberParseError.Overflow));
        });
    }

    [Test]
    public void PointsUseDenseAlternatingCoordinatesAndAllowAnEmptyList()
    {
        SvgPointListResult points = SvgListParser.ParsePoints("0,0 10,20 -5.5,6", 10, 32);
        SvgPointListResult empty = SvgListParser.ParsePoints(" \n", 10, 32);

        Assert.Multiple(() =>
        {
            Assert.That(points.Success, Is.True);
            Assert.That(points.Error, Is.EqualTo(SvgListParseError.None));
            Assert.That(points.Coordinates, Is.EqualTo(new[] { 0.0, 0.0, 10.0, 20.0, -5.5, 6.0 }));
            Assert.That(points.PointCount, Is.EqualTo(3));
            Assert.That(points.ErrorOffset, Is.EqualTo(-1));
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Coordinates, Is.Empty);
            Assert.That(empty.PointCount, Is.Zero);
        });
    }

    [Test]
    public void PointsRejectOddCoordinatesAndReportCompletePrefixPoints()
    {
        SvgPointListResult result = SvgListParser.ParsePoints("0,0 1", 10, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgListParseError.OddCoordinateCount));
            Assert.That(result.NumberError, Is.EqualTo(SvgNumberParseError.None));
            Assert.That(result.Coordinates, Is.EqualTo(new[] { 0.0, 0.0, 1.0 }));
            Assert.That(result.PointCount, Is.EqualTo(1));
            Assert.That(result.ErrorOffset, Is.EqualTo(5));
        });
    }

    [Test]
    public void PointsHonorExplicitAndConfiguredPointLimits()
    {
        SvgPointListResult explicitLimit = SvgListParser.ParsePoints("0,0 1,1 2,2", 2, 32);
        SvgGeometryLimits limits = new SvgGeometryLimits();
        limits.MaxPoints = 1;
        SvgPointListResult configuredLimit = SvgListParser.ParsePoints("0 0 1 1", limits, 32);

        Assert.Multiple(() =>
        {
            Assert.That(explicitLimit.Success, Is.False);
            Assert.That(explicitLimit.Error, Is.EqualTo(SvgListParseError.ItemLimitExceeded));
            Assert.That(explicitLimit.ErrorOffset, Is.EqualTo(8));
            Assert.That(explicitLimit.PointCount, Is.EqualTo(2));
            Assert.That(configuredLimit.Success, Is.False);
            Assert.That(configuredLimit.Error, Is.EqualTo(SvgListParseError.ItemLimitExceeded));
            Assert.That(configuredLimit.ErrorOffset, Is.EqualTo(4));
            Assert.That(configuredLimit.PointCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void OptionalPairAcceptsOneOrTwoValuesAndReportsPresence()
    {
        SvgOptionalNumberPairResult one = SvgListParser.ParseOptionalNumberPair(" 2 ", 32);
        SvgOptionalNumberPairResult comma = SvgListParser.ParseOptionalNumberPair("2, 3", 32);
        SvgOptionalNumberPairResult whitespace = SvgListParser.ParseOptionalNumberPair("2 3", 32);

        Assert.Multiple(() =>
        {
            Assert.That(one.Success, Is.True);
            Assert.That(one.First, Is.EqualTo(2));
            Assert.That(one.Second, Is.Zero);
            Assert.That(one.HasSecond, Is.False);
            Assert.That(comma.Success, Is.True);
            Assert.That(comma.First, Is.EqualTo(2));
            Assert.That(comma.Second, Is.EqualTo(3));
            Assert.That(comma.HasSecond, Is.True);
            Assert.That(whitespace.Success, Is.True);
            Assert.That(whitespace.Second, Is.EqualTo(3));
        });
    }

    [Test]
    public void OptionalPairRejectsMissingTrailingAndExcessValues()
    {
        SvgOptionalNumberPairResult missing = SvgListParser.ParseOptionalNumberPair("", 32);
        SvgOptionalNumberPairResult trailing = SvgListParser.ParseOptionalNumberPair("2,", 32);
        SvgOptionalNumberPairResult excess = SvgListParser.ParseOptionalNumberPair("2,3,4", 32);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Error, Is.EqualTo(SvgListParseError.ExpectedItem));
            Assert.That(missing.ErrorOffset, Is.Zero);
            Assert.That(trailing.Success, Is.False);
            Assert.That(trailing.Error, Is.EqualTo(SvgListParseError.TrailingSeparator));
            Assert.That(trailing.First, Is.EqualTo(2));
            Assert.That(trailing.HasSecond, Is.False);
            Assert.That(excess.Success, Is.False);
            Assert.That(excess.Error, Is.EqualTo(SvgListParseError.ItemLimitExceeded));
            Assert.That(excess.First, Is.EqualTo(2));
            Assert.That(excess.Second, Is.EqualTo(3));
            Assert.That(excess.HasSecond, Is.True);
            Assert.That(excess.ErrorOffset, Is.EqualTo(4));
        });
    }

    [Test]
    public void InvalidArgumentsReturnBoundedFailuresWithoutThrowing()
    {
        SvgNumberListResult nullList = SvgListParser.ParseCoordinateList(null, 5, 32);
        SvgNumberListResult itemLimit = SvgListParser.ParseCoordinateList("1", 0, 32);
        SvgIntegerListResult tokenLimit = SvgListParser.ParseIntegerList("1", SvgListSeparatorMode.Comma, 5, 0, false);
        SvgNumberListResult mode = SvgListParser.ParseNumberList("1", (SvgListSeparatorMode)999, 5, 32, false);
        SvgPointListResult pointLimit = SvgListParser.ParsePoints("", 0, 32);
        SvgPointListResult nullGeometry = SvgListParser.ParsePoints("", (SvgGeometryLimits)null, 32);

        Assert.Multiple(() =>
        {
            Assert.That(nullList.Error, Is.EqualTo(SvgListParseError.InvalidArgument));
            Assert.That(itemLimit.Error, Is.EqualTo(SvgListParseError.InvalidArgument));
            Assert.That(tokenLimit.Error, Is.EqualTo(SvgListParseError.InvalidArgument));
            Assert.That(mode.Error, Is.EqualTo(SvgListParseError.InvalidArgument));
            Assert.That(pointLimit.Error, Is.EqualTo(SvgListParseError.InvalidArgument));
            Assert.That(nullGeometry.Error, Is.EqualTo(SvgListParseError.InvalidArgument));
        });
    }
}
