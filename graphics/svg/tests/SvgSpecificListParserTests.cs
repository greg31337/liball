namespace svg.Tests;

public class SvgSpecificListParserTests
{
    [Test]
    public void DashArraysParseTypedLengthsPercentagesCommasAndWhitespace()
    {
        SvgLengthListParseResult result = new SvgLengthListParser().ParseDashArray(" 5, 2px 10% 0.5em ", 8, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Value.Keyword, Is.EqualTo(SvgListKeyword.Values));
            Assert.That(result.Value.Items, Has.Count.EqualTo(4));
            Assert.That(result.Value.Items[0].Length.Unit, Is.EqualTo(SvgLengthUnit.Number));
            Assert.That(result.Value.Items[1].Length.Unit, Is.EqualTo(SvgLengthUnit.Px));
            Assert.That(result.Value.Items[2].Length.Unit, Is.EqualTo(SvgLengthUnit.Percentage));
            Assert.That(result.Value.Items[3].Length.Unit, Is.EqualTo(SvgLengthUnit.Em));
            Assert.That(result.Value.Items[0].StartOffset, Is.EqualTo(1));
            Assert.That(result.Value.Items[1].StartOffset, Is.EqualTo(4));
            Assert.That(result.Value.ToText(), Is.EqualTo("5 2px 10% 0.5em"));
        });
    }

    [Test]
    public void DashArraysHandleNoneCssWideKeywordsAllZeroAndNegativeFailures()
    {
        SvgLengthListParseResult none = new SvgLengthListParser().ParseDashArray(" NONE ", 4, 16);
        SvgLengthListParseResult inherit = new SvgLengthListParser().ParseDashArray("inherit", 4, 16);
        SvgLengthListParseResult escapedInherit = new SvgLengthListParser().ParseDashArray("in\\68 erit", 4, 16);
        SvgLengthListParseResult zero = new SvgLengthListParser().ParseDashArray("0 0px 0%", 4, 16);
        SvgLengthListParseResult negative = new SvgLengthListParser().ParseDashArray("5 -2 3", 4, 16);
        SvgLengthListParseResult normal = new SvgLengthListParser().ParseDashArray("normal", 4, 16);

        Assert.Multiple(() =>
        {
            Assert.That(none.Success, Is.True);
            Assert.That(none.Value.Keyword, Is.EqualTo(SvgListKeyword.None));
            Assert.That(none.Value.ToText(), Is.EqualTo("none"));
            Assert.That(inherit.Value.Keyword, Is.EqualTo(SvgListKeyword.Inherit));
            Assert.That(escapedInherit.Value.Keyword, Is.EqualTo(SvgListKeyword.Inherit));
            Assert.That(zero.Value.Keyword, Is.EqualTo(SvgListKeyword.None));
            Assert.That(negative.Error, Is.EqualTo(SvgSpecificListParseError.NegativeDash));
            Assert.That(negative.ErrorOffset, Is.EqualTo(2));
            Assert.That(negative.Value.Items, Has.Count.EqualTo(1));
            Assert.That(normal.Error, Is.EqualTo(SvgSpecificListParseError.InvalidKeyword));
        });
    }

    [Test]
    public void TextPositionListsSupportEveryLengthUnitAndNegativeOffsets()
    {
        SvgLengthListParseResult x = new SvgLengthListParser().ParseX("10 25% -2em,3px", 8, 32);
        SvgLengthListParseResult y = new SvgLengthListParser().ParseY("1cm", 8, 32);
        SvgLengthListParseResult dx = new SvgLengthListParser().ParseDx("-.5 2vw", 8, 32);
        SvgLengthListParseResult dy = new SvgLengthListParser().ParseDy("4rem", 8, 32);

        Assert.Multiple(() =>
        {
            Assert.That(x.Success, Is.True);
            Assert.That(x.Value.Items.Select(item => item.Length.Unit), Is.EqualTo(new[] { SvgLengthUnit.Number, SvgLengthUnit.Percentage, SvgLengthUnit.Em, SvgLengthUnit.Px }));
            Assert.That(x.Value.Items[2].Length.Value, Is.EqualTo(-2));
            Assert.That(y.Value.Items[0].Length.Unit, Is.EqualTo(SvgLengthUnit.Cm));
            Assert.That(dx.Value.Items[0].Length.Value, Is.EqualTo(-0.5));
            Assert.That(dx.Value.Items[1].Length.Unit, Is.EqualTo(SvgLengthUnit.Vw));
            Assert.That(dy.Value.Items[0].Length.Unit, Is.EqualTo(SvgLengthUnit.Rem));
            Assert.That(x.Value.ToText(), Is.EqualTo("10 25% -2em 3px"));
        });
    }

    [Test]
    public void LengthListsReportSeparatorsMalformedItemsAndBoundsWithValidPrefixes()
    {
        SvgLengthListParseResult empty = new SvgLengthListParser().ParseTextPosition(" ", 4, 16);
        SvgLengthListParseResult trailing = new SvgLengthListParser().ParseTextPosition("1, ", 4, 16);
        SvgLengthListParseResult repeated = new SvgLengthListParser().ParseTextPosition("1,,2", 4, 16);
        SvgLengthListParseResult badUnit = new SvgLengthListParser().ParseTextPosition("1px 2bad", 4, 16);
        SvgLengthListParseResult itemLimit = new SvgLengthListParser().ParseTextPosition("1 2 3", 2, 16);
        SvgLengthListParseResult tokenLimit = new SvgLengthListParser().ParseTextPosition("12345", 4, 4);
        SvgLengthListParseResult none = new SvgLengthListParser().ParseTextPosition("none", 4, 16);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Error, Is.EqualTo(SvgSpecificListParseError.ExpectedValue));
            Assert.That(trailing.Error, Is.EqualTo(SvgSpecificListParseError.InvalidSeparator));
            Assert.That(repeated.Error, Is.EqualTo(SvgSpecificListParseError.InvalidItem));
            Assert.That(badUnit.Error, Is.EqualTo(SvgSpecificListParseError.InvalidItem));
            Assert.That(badUnit.Value.Items, Has.Count.EqualTo(1));
            Assert.That(itemLimit.Error, Is.EqualTo(SvgSpecificListParseError.ItemLimitExceeded));
            Assert.That(itemLimit.Value.Items, Has.Count.EqualTo(2));
            Assert.That(tokenLimit.Error, Is.EqualTo(SvgSpecificListParseError.TokenTooLong));
            Assert.That(none.Error, Is.EqualTo(SvgSpecificListParseError.InvalidKeyword));
        });
    }

    [Test]
    public void RotateListsParseUnitlessDegreesKeywordsAndExactSourceOffsets()
    {
        SvgRotateListParseResult result = new SvgRotateListParser().Parse(" 0, 45 -90.5 1e2 ", 8, 32);
        SvgRotateListParseResult inherit = new SvgRotateListParser().Parse("INHERIT", 8, 32);
        SvgRotateListParseResult unit = new SvgRotateListParser().Parse("10deg", 8, 32);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Items.Select(item => item.Angle.Value), Is.EqualTo(new[] { 0.0, 45.0, -90.5, 100.0 }));
            Assert.That(result.Items.All(item => item.Angle.Unit == SvgAngleUnit.Number), Is.True);
            Assert.That(result.Items[0].StartOffset, Is.EqualTo(1));
            Assert.That(result.Items[1].StartOffset, Is.EqualTo(4));
            Assert.That(inherit.Success, Is.True);
            Assert.That(inherit.Keyword, Is.EqualTo(SvgListKeyword.Inherit));
            Assert.That(unit.Error, Is.EqualTo(SvgSpecificListParseError.InvalidItem));
        });
    }

    [Test]
    public void RotateListsRetainPrefixesOnSyntaxAndLimitFailures()
    {
        SvgRotateListParseResult trailing = new SvgRotateListParser().Parse("0,", 4, 16);
        SvgRotateListParseResult malformed = new SvgRotateListParser().Parse("0 nope", 4, 16);
        SvgRotateListParseResult limit = new SvgRotateListParser().Parse("0 1 2", 2, 16);
        SvgRotateListParseResult token = new SvgRotateListParser().Parse("12345", 2, 4);

        Assert.Multiple(() =>
        {
            Assert.That(trailing.Error, Is.EqualTo(SvgSpecificListParseError.InvalidSeparator));
            Assert.That(trailing.Items, Has.Count.EqualTo(1));
            Assert.That(malformed.Error, Is.EqualTo(SvgSpecificListParseError.InvalidItem));
            Assert.That(malformed.Items, Has.Count.EqualTo(1));
            Assert.That(limit.Error, Is.EqualTo(SvgSpecificListParseError.ItemLimitExceeded));
            Assert.That(limit.Items, Has.Count.EqualTo(2));
            Assert.That(token.Error, Is.EqualTo(SvgSpecificListParseError.TokenTooLong));
        });
    }

    [Test]
    public void FontFamiliesParseStringsIdentifierSequencesEscapesAndGenericKeywords()
    {
        string input = " '21st Century', New Century Schoolbook, sans-serif, \"sans-serif\", A\\26 B ";
        SvgFontFamilyParseResult result = new SvgFontFamilyParser().Parse(input, 8, 64);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Families.Select(family => family.Name), Is.EqualTo(new[] { "21st Century", "New Century Schoolbook", "sans-serif", "sans-serif", "A&B" }));
            Assert.That(result.Families[0].WasQuoted, Is.True);
            Assert.That(result.Families[1].WasQuoted, Is.False);
            Assert.That(result.Families[2].IsGeneric, Is.True);
            Assert.That(result.Families[3].IsGeneric, Is.False);
            Assert.That(result.Families[4].StartOffset, Is.EqualTo(input.IndexOf("A\\26", StringComparison.Ordinal)));
        });
    }

    [Test]
    public void FontFamiliesHandleCssWideKeywordsAndRejectMalformedOrBoundedLists()
    {
        SvgFontFamilyParseResult inherit = new SvgFontFamilyParser().Parse(" inherit ", 4, 32);
        SvgFontFamilyParseResult emptyQuoted = new SvgFontFamilyParser().Parse("\"\"", 4, 32);
        SvgFontFamilyParseResult trailing = new SvgFontFamilyParser().Parse("Arial,", 4, 32);
        SvgFontFamilyParseResult punctuation = new SvgFontFamilyParser().Parse("Red/Black, serif", 4, 32);
        SvgFontFamilyParseResult digitLeading = new SvgFontFamilyParser().Parse("21st Century", 4, 32);
        SvgFontFamilyParseResult mixedQuote = new SvgFontFamilyParser().Parse("\"Lucida\" Grande", 4, 32);
        SvgFontFamilyParseResult itemLimit = new SvgFontFamilyParser().Parse("Arial, serif", 1, 32);
        SvgFontFamilyParseResult tokenLimit = new SvgFontFamilyParser().Parse("Long Family Name", 4, 5);

        Assert.Multiple(() =>
        {
            Assert.That(inherit.Success, Is.True);
            Assert.That(inherit.Keyword, Is.EqualTo(SvgListKeyword.Inherit));
            Assert.That(emptyQuoted.Success, Is.True);
            Assert.That(emptyQuoted.Families[0].Name, Is.Empty);
            Assert.That(trailing.Error, Is.EqualTo(SvgSpecificListParseError.InvalidSeparator));
            Assert.That(punctuation.Error, Is.EqualTo(SvgSpecificListParseError.InvalidFontFamily));
            Assert.That(digitLeading.Error, Is.EqualTo(SvgSpecificListParseError.InvalidFontFamily));
            Assert.That(mixedQuote.Error, Is.EqualTo(SvgSpecificListParseError.InvalidFontFamily));
            Assert.That(itemLimit.Error, Is.EqualTo(SvgSpecificListParseError.ItemLimitExceeded));
            Assert.That(itemLimit.Families, Has.Count.EqualTo(1));
            Assert.That(tokenLimit.Error, Is.EqualTo(SvgSpecificListParseError.TokenTooLong));
        });
    }

    [Test]
    public void PaintOrderParsesNormalCssWideAndUniqueComponentLists()
    {
        SvgPaintOrderParseResult normal = new SvgPaintOrderParser().Parse(" normal ");
        SvgPaintOrderParseResult inherit = new SvgPaintOrderParser().Parse("inherit");
        SvgPaintOrderParseResult order = new SvgPaintOrderParser().Parse("stroke markers fill");

        Assert.Multiple(() =>
        {
            Assert.That(normal.Success, Is.True);
            Assert.That(normal.Keyword, Is.EqualTo(SvgListKeyword.Normal));
            Assert.That(inherit.Keyword, Is.EqualTo(SvgListKeyword.Inherit));
            Assert.That(order.Success, Is.True);
            Assert.That(order.Items, Is.EqualTo(new[] { SvgPaintOrderComponent.Stroke, SvgPaintOrderComponent.Markers, SvgPaintOrderComponent.Fill }));
        });
    }

    [Test]
    public void PaintOrderRejectsDuplicatesUnknownComponentsAndCommaSeparators()
    {
        SvgPaintOrderParseResult duplicate = new SvgPaintOrderParser().Parse("fill stroke fill");
        SvgPaintOrderParseResult unknown = new SvgPaintOrderParser().Parse("fill background");
        SvgPaintOrderParseResult comma = new SvgPaintOrderParser().Parse("fill,stroke");
        SvgPaintOrderParseResult empty = new SvgPaintOrderParser().Parse(" ");

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Error, Is.EqualTo(SvgSpecificListParseError.DuplicateItem));
            Assert.That(duplicate.Items, Has.Count.EqualTo(2));
            Assert.That(unknown.Error, Is.EqualTo(SvgSpecificListParseError.InvalidKeyword));
            Assert.That(comma.Error, Is.EqualTo(SvgSpecificListParseError.InvalidKeyword));
            Assert.That(empty.Error, Is.EqualTo(SvgSpecificListParseError.ExpectedValue));
        });
    }
}
