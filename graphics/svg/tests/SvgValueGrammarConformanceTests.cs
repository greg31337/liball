namespace svg.Tests;

public class SvgValueGrammarConformanceTests
{
    [TestCase("number", "-.5e2")]
    [TestCase("css-number", "+.5")]
    [TestCase("integer", "-42")]
    [TestCase("coordinate", "3.5")]
    [TestCase("number-list", "1,-2 .5")]
    [TestCase("integer-list", "1, -2 3")]
    [TestCase("comma-list", "1, 2,3")]
    [TestCase("whitespace-list", "1 2\t3")]
    [TestCase("semicolon-list", "0;.5;1")]
    [TestCase("points", "0,0 10,20")]
    [TestCase("optional-pair", "5,10")]
    [TestCase("svg-length", "10cm")]
    [TestCase("css-length", "2.5rem")]
    [TestCase("angle", ".25turn")]
    [TestCase("time", "250ms")]
    [TestCase("frequency", "2kHz")]
    [TestCase("resolution", "96dpi")]
    [TestCase("ratio", "16 / 9")]
    [TestCase("path-length", "100")]
    [TestCase("viewBox", "0 0 100 200")]
    [TestCase("preserveAspectRatio", "xMidYMid meet")]
    [TestCase("svg-transform", "translate(10 20) rotate(45)")]
    [TestCase("css-transform", "translate(10%, 2em) rotate(.25turn)")]
    [TestCase("transform-origin", "bottom right 2em")]
    [TestCase("transform-box", "view-box")]
    [TestCase("color", "hsl(120 100% 25% / .5)")]
    [TestCase("paint", "url(#paint) rgb(255 0 0)")]
    [TestCase("function", "paint(foo, nested(bar))")]
    [TestCase("iri", "images/art.svg#icon")]
    [TestCase("fragment", "#icon")]
    [TestCase("url", "url('#icon')")]
    [TestCase("media-type", "image/svg+xml; charset=utf-8")]
    [TestCase("languages", "en-US, fr")]
    [TestCase("features", "http://www.w3.org/TR/SVG11/feature#Shape http://example.test/ext")]
    [TestCase("dash-array", "5 3 2")]
    [TestCase("text-position", "1em, 2%")]
    [TestCase("x", "10 20%")]
    [TestCase("y", "1cm")]
    [TestCase("dx", "-.5 2vw")]
    [TestCase("dy", "4rem")]
    [TestCase("rotate-list", "0 45 -90")]
    [TestCase("font-family", "'Open Sans', serif")]
    [TestCase("paint-order", "stroke markers fill")]
    [TestCase("path", "M0 0 L10 10z")]
    [TestCase("clock", "01:02:03.5")]
    [TestCase("timing", "button.click+1s")]
    [TestCase("begin-list", "0s; button.click+1s; indefinite")]
    [TestCase("end-list", "0s;indefinite")]
    [TestCase("keyTimes", "0;.5;1")]
    [TestCase("keyPoints", "0;.8;.4;1")]
    [TestCase("keySplines", "0 0 .5 1; .25 .1 .75 .9")]
    [TestCase("restart", "whenNotActive")]
    [TestCase("fill", "freeze")]
    [TestCase("additive", "sum")]
    [TestCase("accumulate", "none")]
    [TestCase("calcMode", "spline")]
    [TestCase("attributeType", "CSS")]
    [TestCase("custom-property", "calc(10px + 2%)")]
    [TestCase("variable", "var(--size, 5px)")]
    [TestCase("calc", "clamp(1px, calc(2px + 3%), 10%)")]
    public void SvgAndW3cExamplesCoverEveryPhaseOneValueGrammar(string grammar, string input)
    {
        Assert.That(ParseSucceeds(grammar, input), Is.True, grammar + " rejected " + input);
    }

    [TestCase("number", "1e+", 3)]
    [TestCase("number-list", "1,,2", 2)]
    [TestCase("svg-length", "2px!", 3)]
    [TestCase("angle", "2deg!", 4)]
    [TestCase("ratio", "16/", 3)]
    [TestCase("viewBox", "0,,0 10 20", 2)]
    [TestCase("preserveAspectRatio", "center meet", 0)]
    [TestCase("svg-transform", "scale(1e+)", 9)]
    [TestCase("css-transform", "translate(10px", 14)]
    [TestCase("transform-origin", "left right", 5)]
    [TestCase("transform-box", "padding-box", 0)]
    [TestCase("color", " ", 1)]
    [TestCase("dash-array", "5 -2 3", 2)]
    [TestCase("path", "M0 0 A-1 2 0 0 0 3 4", 6)]
    [TestCase("calc", "calc(1px + 1s)", 9)]
    public void FuzzDerivedMalformedValuesReportExactOffsets(string grammar, string input, int expectedOffset)
    {
        Assert.That(FailureOffset(grammar, input), Is.EqualTo(expectedOffset), grammar + " did not locate " + input);
    }

    [TestCase("number", "1234.5e-2")]
    [TestCase("number-list", "1.5,-2.25 .5")]
    [TestCase("svg-length", "12.5cm")]
    [TestCase("angle", ".25turn")]
    [TestCase("time", "1250.5ms")]
    [TestCase("frequency", "2.5kHz")]
    [TestCase("resolution", "96.5dpi")]
    [TestCase("ratio", "16.5 / 9.25")]
    [TestCase("viewBox", ".5 1.25 100.5 200.75")]
    [TestCase("svg-transform", "translate(10.5 20.25) rotate(45.5)")]
    [TestCase("css-transform", "translate(10.5%, 2.25em) rotate(.25turn)")]
    [TestCase("color", "rgb(12.5% 25% 50% / .75)")]
    [TestCase("path", "M.5 1.25L10.5 20.75")]
    [TestCase("clock", "01:02:03.5")]
    [TestCase("calc", "calc(1.5px + 2.25%)")]
    public void NumericValueGrammarsAreInvariantUnderCommaDecimalCultures(string grammar, string input)
    {
        System.Globalization.CultureInfo originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo originalUiCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            System.Globalization.CultureInfo.CurrentCulture = culture;
            System.Globalization.CultureInfo.CurrentUICulture = culture;
            Assert.That(ParseSucceeds(grammar, input), Is.True, grammar + " depended on the current culture");
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
            System.Globalization.CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [TestCase("number", "-1.25e2")]
    [TestCase("svg-length", "2.500CM")]
    [TestCase("angle", "100grad")]
    [TestCase("time", "2500ms")]
    [TestCase("frequency", "2.5KHZ")]
    [TestCase("resolution", "2x")]
    [TestCase("ratio", "16 / 9")]
    [TestCase("path-length", "12.500")]
    [TestCase("viewBox", "-1,2 30.500 40")]
    [TestCase("preserveAspectRatio", "defer xMaxYMin slice")]
    [TestCase("svg-transform", "translate(10,-20) rotate(45 1 2)")]
    [TestCase("css-transform", "TRANSLATE(10px,25%) ROTATE(.5turn)")]
    [TestCase("transform-origin", "bottom right 2em")]
    [TestCase("color", "rgba(255, 0, 0, .5)")]
    [TestCase("paint", "url(#p) rgba(255, 0, 0, .5)")]
    [TestCase("function", "f\\6f o(calc(1 + fn(2)))")]
    [TestCase("iri", "images/a.svg#icon")]
    [TestCase("media-type", "Application/SVG+XML ; Charset = utf-8")]
    [TestCase("dash-array", "5, 2PX 10%")]
    [TestCase("text-position", "10 25% -2em,3px")]
    [TestCase("path", "M0 0L10.500 20z")]
    public void CanonicalWritersProduceParseFormatParseFixedPoints(string grammar, string input)
    {
        string first = Canonicalize(grammar, input);
        string second = Canonicalize(grammar, first);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Empty);
            Assert.That(second, Is.EqualTo(first));
        });
    }

    static bool ParseSucceeds(string grammar, string input)
    {
        if (grammar == "number") return SvgNumberParser.ParseNumber(input, 128).Success;
        if (grammar == "css-number") return SvgNumberParser.ParseCssNumber(input, 128).Success;
        if (grammar == "integer") return SvgNumberParser.ParseInteger(input, 128).Success;
        if (grammar == "coordinate") return SvgListParser.ParseCoordinate(input, 128).Success;
        if (grammar == "number-list") return SvgListParser.ParseNumberList(input, SvgListSeparatorMode.CommaOrWhitespace, 16, 128, false).Success;
        if (grammar == "integer-list") return SvgListParser.ParseIntegerList(input, SvgListSeparatorMode.CommaOrWhitespace, 16, 128, false).Success;
        if (grammar == "comma-list") return SvgListParser.ParseCommaNumberList(input, 16, 128, false).Success;
        if (grammar == "whitespace-list") return SvgListParser.ParseWhitespaceNumberList(input, 16, 128, false).Success;
        if (grammar == "semicolon-list") return SvgListParser.ParseSemicolonNumberList(input, 16, 128, false).Success;
        if (grammar == "points") return SvgListParser.ParsePoints(input, 16, 128).Success;
        if (grammar == "optional-pair") return SvgListParser.ParseOptionalNumberPair(input, 128).Success;
        if (grammar == "svg-length") return SvgLengthParser.ParseLength(input, true, true, 128).Success;
        if (grammar == "css-length") return SvgLengthParser.ParseCssLength(input, true, false, 128).Success;
        if (grammar == "angle") return SvgScalarUnitParser.ParseAngle(input, false, 128).Success;
        if (grammar == "time") return SvgScalarUnitParser.ParseTime(input, false, 128).Success;
        if (grammar == "frequency") return SvgScalarUnitParser.ParseFrequency(input, 128).Success;
        if (grammar == "resolution") return SvgScalarUnitParser.ParseResolution(input, 128).Success;
        if (grammar == "ratio") return SvgScalarUnitParser.ParseRatio(input, 128).Success;
        if (grammar == "path-length") return SvgScalarUnitParser.ParsePathLength(input, 128).Success;
        if (grammar == "viewBox") return SvgViewBoxParser.ParseViewBox(input, 128).Success;
        if (grammar == "preserveAspectRatio") return SvgViewBoxParser.ParseAspectRatio(input, 128).Success;
        if (grammar == "svg-transform") return SvgTransformParser.Parse(input, 16, 128).Success;
        if (grammar == "css-transform") return SvgCssTransformParser.ParseTransform(input, 16, 128, false).Success;
        if (grammar == "transform-origin") return SvgCssTransformParser.ParseOrigin(input, 128).Success;
        if (grammar == "transform-box") return SvgCssTransformParser.ParseTransformBox(input, 128).Success;
        if (grammar == "color") return new SvgColorParser().Parse(input, 128).Success;
        if (grammar == "paint") return new SvgPaintParser().Parse(input, 8, 128, false).Success;
        if (grammar == "function") return new SvgFunctionValueParser().Parse(input, 128, 256).Success;
        if (grammar == "iri") return new SvgIriParser().ParseIri(input, 256).Success;
        if (grammar == "fragment") return new SvgIriParser().ParseFragment(input, 256).Success;
        if (grammar == "url") return new SvgIriParser().ParseUrl(input, 256).Success;
        if (grammar == "media-type") return new SvgMediaTypeParser().Parse(input, 8, 128).Success;
        if (grammar == "languages") return new SvgLanguageListParser().Parse(input, 8, 128).Success;
        if (grammar == "features") return new SvgFeatureStringParser().Parse(input, 8, 256).Success;
        if (grammar == "dash-array") return new SvgLengthListParser().ParseDashArray(input, 16, 128).Success;
        if (grammar == "text-position") return new SvgLengthListParser().ParseTextPosition(input, 16, 128).Success;
        if (grammar == "x") return new SvgLengthListParser().ParseX(input, 16, 128).Success;
        if (grammar == "y") return new SvgLengthListParser().ParseY(input, 16, 128).Success;
        if (grammar == "dx") return new SvgLengthListParser().ParseDx(input, 16, 128).Success;
        if (grammar == "dy") return new SvgLengthListParser().ParseDy(input, 16, 128).Success;
        if (grammar == "rotate-list") return new SvgRotateListParser().Parse(input, 16, 128).Success;
        if (grammar == "font-family") return new SvgFontFamilyParser().Parse(input, 16, 128).Success;
        if (grammar == "paint-order") return new SvgPaintOrderParser().Parse(input).Success;
        if (grammar == "path") return new SvgPathParser().Parse(input, 64, 128).Success;
        if (grammar == "clock") return new SvgTimingParser().ParseClock(input, 128).Success;
        if (grammar == "timing") return new SvgTimingParser().ParseTiming(input, 256).Success;
        if (grammar == "begin-list") return new SvgTimingParser().ParseBeginList(input, 16, 256).Success;
        if (grammar == "end-list") return new SvgTimingParser().ParseEndList(input, 16, 256).Success;
        if (grammar == "keyTimes") return new SvgAnimationValueParser().ParseKeyTimes(input, 16, 128).Success;
        if (grammar == "keyPoints") return new SvgAnimationValueParser().ParseKeyPoints(input, 16, 128).Success;
        if (grammar == "keySplines") return new SvgAnimationValueParser().ParseKeySplines(input, 16, 128).Success;
        if (grammar == "restart") return new SvgAnimationKeywordParser().ParseRestart(input).Success;
        if (grammar == "fill") return new SvgAnimationKeywordParser().ParseFill(input).Success;
        if (grammar == "additive") return new SvgAnimationKeywordParser().ParseAdditive(input).Success;
        if (grammar == "accumulate") return new SvgAnimationKeywordParser().ParseAccumulate(input).Success;
        if (grammar == "calcMode") return new SvgAnimationKeywordParser().ParseCalcMode(input).Success;
        if (grammar == "attributeType") return new SvgAnimationKeywordParser().ParseAttributeType(input).Success;
        if (grammar == "custom-property") return new SvgCustomPropertySet().Set("--value", input, 64, 128).Success;
        if (grammar == "variable")
        {
            SvgCustomPropertySet properties = new SvgCustomPropertySet();
            properties.Set("--size", "10px", 8, 128);
            return new SvgCssVariableResolver().Resolve(properties, input, 16, 64, 128).Success;
        }
        if (grammar == "calc") return new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters(input, SvgCalcDimension.LengthPercentage, 16, 128, 128).Success;
        throw new System.ArgumentOutOfRangeException("grammar");
    }

    static int FailureOffset(string grammar, string input)
    {
        if (grammar == "number") return SvgNumberParser.ParseNumber(input, 128).ErrorOffset;
        if (grammar == "number-list") return SvgListParser.ParseNumberList(input, SvgListSeparatorMode.CommaOrWhitespace, 16, 128, false).ErrorOffset;
        if (grammar == "svg-length") return SvgLengthParser.ParseLength(input, true, true, 128).ErrorOffset;
        if (grammar == "angle") return SvgScalarUnitParser.ParseAngle(input, false, 128).ErrorOffset;
        if (grammar == "ratio") return SvgScalarUnitParser.ParseRatio(input, 128).ErrorOffset;
        if (grammar == "viewBox") return SvgViewBoxParser.ParseViewBox(input, 128).ErrorOffset;
        if (grammar == "preserveAspectRatio") return SvgViewBoxParser.ParseAspectRatio(input, 128).ErrorOffset;
        if (grammar == "svg-transform") return SvgTransformParser.Parse(input, 16, 128).ErrorOffset;
        if (grammar == "css-transform") return SvgCssTransformParser.ParseTransform(input, 16, 128, false).ErrorOffset;
        if (grammar == "transform-origin") return SvgCssTransformParser.ParseOrigin(input, 128).ErrorOffset;
        if (grammar == "transform-box") return SvgCssTransformParser.ParseTransformBox(input, 128).ErrorOffset;
        if (grammar == "color") return new SvgColorParser().Parse(input, 128).ErrorOffset;
        if (grammar == "dash-array") return new SvgLengthListParser().ParseDashArray(input, 16, 128).ErrorOffset;
        if (grammar == "path") return new SvgPathParser().Parse(input, 64, 128).ErrorOffset;
        if (grammar == "calc") return new SvgCalcParser().ParseWithValueAndExpectedDimensionAndMaxDepthAndMaxNodesAndMaxTokenCharacters(input, SvgCalcDimension.Any, 16, 128, 128).ErrorOffset;
        throw new System.ArgumentOutOfRangeException("grammar");
    }

    static string Canonicalize(string grammar, string input)
    {
        if (grammar == "number")
        {
            SvgNumberParseResult parsed = SvgNumberParser.ParseNumber(input, 128);
            Assert.That(parsed.Success, Is.True);
            return SvgNumberFormatter.FormatShortest(parsed.Value).Text;
        }
        if (grammar == "svg-length") return SvgLengthParser.ParseLength(input, true, true, 128).Length.ToText();
        if (grammar == "angle") return SvgScalarUnitParser.ParseAngle(input, false, 128).Angle.ToText();
        if (grammar == "time") return SvgScalarUnitParser.ParseTime(input, false, 128).Time.ToText();
        if (grammar == "frequency") return SvgScalarUnitParser.ParseFrequency(input, 128).Frequency.ToText();
        if (grammar == "resolution") return SvgScalarUnitParser.ParseResolution(input, 128).Resolution.ToText();
        if (grammar == "ratio") return SvgScalarUnitParser.ParseRatio(input, 128).Ratio.ToText();
        if (grammar == "path-length") return SvgScalarUnitParser.ParsePathLength(input, 128).PathLength.ToText();
        if (grammar == "viewBox") return SvgViewBoxParser.ParseViewBox(input, 128).ViewBox.ToText();
        if (grammar == "preserveAspectRatio") return SvgViewBoxParser.ParseAspectRatio(input, 128).AspectRatio.ToText();
        if (grammar == "svg-transform") return SvgTransformParser.Parse(input, 16, 128).TransformList.ToText();
        if (grammar == "css-transform") return SvgCssTransformParser.ParseTransform(input, 16, 128, false).TransformList.ToText();
        if (grammar == "transform-origin") return SvgCssTransformParser.ParseOrigin(input, 128).Origin.ToText();
        if (grammar == "color") return new SvgColorParser().Parse(input, 128).Color.ToText();
        if (grammar == "paint") return new SvgPaintParser().Parse(input, 8, 128, false).Paint.ToText();
        if (grammar == "function") return new SvgFunctionValueParser().Parse(input, 128, 256).Function.ToText();
        if (grammar == "iri") return new SvgIriParser().ParseIri(input, 256).Reference.ToText();
        if (grammar == "media-type") return new SvgMediaTypeParser().Parse(input, 8, 128).MediaType.ToText();
        if (grammar == "dash-array") return new SvgLengthListParser().ParseDashArray(input, 16, 128).Value.ToText();
        if (grammar == "text-position") return new SvgLengthListParser().ParseTextPosition(input, 16, 128).Value.ToText();
        if (grammar == "path") return new SvgPathParser().Parse(input, 64, 128).PathData.ToText();
        throw new System.ArgumentOutOfRangeException("grammar");
    }
}
