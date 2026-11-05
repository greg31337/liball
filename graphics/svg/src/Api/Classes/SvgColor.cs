namespace svg;

/// <summary>Parses bounded CSS named, hexadecimal, legacy, and modern color values.</summary>
public class SvgColorParser : ISvgColorParser
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgColorParser()
    {
    }

    /// <summary>Parses one complete CSS color value.</summary>
    /// <param name="value">The color text.</param>
    /// <param name="maxTokenCharacters">The maximum characters in one name, number, or hexadecimal token.</param>
    /// <returns>A parsed color or a stable failure.</returns>
    public SvgColorParseResult Parse(string? value, int maxTokenCharacters)
    {
        if (value == null || maxTokenCharacters < 1)
        {
            return new SvgColorParser().Failure(SvgColorParseError.InvalidArgument, 0);
        }
        SvgSpanReader reader = new SvgSpanReader(value, maxTokenCharacters);
        reader.SkipCssWhitespace();
        if (reader.IsEnd())
        {
            return new SvgColorParser().Failure(SvgColorParseError.ExpectedColor, reader.GetPosition());
        }
        SvgColorParseResult result;
        if (reader.PeekCharacter().Value == '#')
        {
            result = new SvgColorParser().ParseHex(reader, maxTokenCharacters);
        }
        else
        {
            result = new SvgColorParser().ParseNameOrFunction(reader);
        }
        if (!result.Success)
        {
            return result;
        }
        reader.SkipCssWhitespace();
        if (!reader.IsEnd())
        {
            return new SvgColorParser().Failure(SvgColorParseError.TrailingCharacters, reader.GetPosition());
        }
        (result.Color ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Source = value;
        return result;
    }
SvgColorParseResult ParseNameOrFunction(SvgSpanReader reader)
    {
        int start = reader.GetPosition();
        SvgSpanTokenResult token = reader.ReadToken(IsAsciiLetter, true);
        if (!token.Success)
        {
            SvgColorParseError error = token.Error == SvgSpanReadError.TokenTooLong ? SvgColorParseError.TokenTooLong : SvgColorParseError.InvalidArgument;
            return new SvgColorParser().Failure(error, token.ErrorOffset);
        }
        if (token.RawText.Length == 0)
        {
            return new SvgColorParser().Failure(SvgColorParseError.ExpectedColor, start);
        }
        string name = token.GetValue().ToLowerInvariant();
        if (reader.Consume('('))
        {
            if (name == "rgb" || name == "rgba")
            {
                return new SvgColorParser().ParseRgb(reader);
            }
            if (name == "hsl" || name == "hsla")
            {
                return new SvgColorParser().ParseHsl(reader);
            }
            if (name == "hwb")
            {
                return new SvgColorParser().ParseHwb(reader);
            }
            if (name == "lab")
            {
                return new SvgColorParser().ParseLabLike(reader, SvgColorSpace.Lab);
            }
            if (name == "lch")
            {
                return new SvgColorParser().ParseLabLike(reader, SvgColorSpace.Lch);
            }
            if (name == "oklab")
            {
                return new SvgColorParser().ParseLabLike(reader, SvgColorSpace.Oklab);
            }
            if (name == "oklch")
            {
                return new SvgColorParser().ParseLabLike(reader, SvgColorSpace.Oklch);
            }
            if (name == "color")
            {
                return new SvgColorParser().ParseColor(reader);
            }
            return new SvgColorParser().Failure(SvgColorParseError.UnknownFunction, start);
        }
        if (name == "currentcolor")
        {
            return new SvgColorParser().Success(SvgColor.CurrentColor());
        }
        if (name == "transparent")
        {
            return new SvgColorParser().Success(new SvgColor(SvgColorSpace.Srgb, 0, 0, 0, 0));
        }
        int packed = SvgNamedColorTable.Find(name);
        if (packed < 0)
        {
            return new SvgColorParser().Failure(SvgColorParseError.UnknownColor, start);
        }
        return new SvgColorParser().Success(new SvgColorParser().ColorFromPacked(packed));
    }
SvgColorParseResult ParseHex(SvgSpanReader reader, int maximum)
    {
        reader.Consume('#');
        int start = reader.GetPosition();
        System.ReadOnlySpan<char> input = reader.GetInput().Span;
        int position = start;
        while (position < input.Length && SvgSpanCharacters.HexValue(input[position]) >= 0)
        {
            if (position - start >= maximum)
            {
                return new SvgColorParser().Failure(SvgColorParseError.TokenTooLong, position);
            }
            position = position + 1;
        }
        int count = position - start;
        if (count != 3 && count != 4 && count != 6 && count != 8)
        {
            return new SvgColorParser().Failure(SvgColorParseError.InvalidHex, position);
        }
        if (position < input.Length && !SvgSpanCharacters.IsCssWhitespace(input[position]))
        {
            return new SvgColorParser().Failure(SvgColorParseError.InvalidHex, position);
        }
        int red;
        int green;
        int blue;
        int alpha = 255;
        if (count == 3 || count == 4)
        {
            red = SvgSpanCharacters.HexValue(input[start]) * 17;
            green = SvgSpanCharacters.HexValue(input[start + 1]) * 17;
            blue = SvgSpanCharacters.HexValue(input[start + 2]) * 17;
            if (count == 4) alpha = SvgSpanCharacters.HexValue(input[start + 3]) * 17;
        }
        else
        {
            red = SvgSpanCharacters.HexValue(input[start]) * 16 + SvgSpanCharacters.HexValue(input[start + 1]);
            green = SvgSpanCharacters.HexValue(input[start + 2]) * 16 + SvgSpanCharacters.HexValue(input[start + 3]);
            blue = SvgSpanCharacters.HexValue(input[start + 4]) * 16 + SvgSpanCharacters.HexValue(input[start + 5]);
            if (count == 8) alpha = SvgSpanCharacters.HexValue(input[start + 6]) * 16 + SvgSpanCharacters.HexValue(input[start + 7]);
        }
        reader.SetPosition(position);
        return new SvgColorParser().Success(new SvgColor(SvgColorSpace.Srgb, red / 255.0, green / 255.0, blue / 255.0, alpha / 255.0));
    }
SvgColorParseResult ParseRgb(SvgSpanReader reader)
    {
        reader.SkipCssWhitespace();
        SvgColorComponent first = new SvgColorParser().ReadComponent(reader, true);
        if (!first.Success) return new SvgColorParser().ComponentFailure(first);
        int whitespace = reader.SkipCssWhitespace();
        if (reader.Consume(','))
        {
            return new SvgColorParser().ParseLegacyRgb(reader, first);
        }
        return new SvgColorParser().ParseModernRgb(reader, first, whitespace);
    }
SvgColorParseResult ParseLegacyRgb(SvgSpanReader reader, SvgColorComponent first)
    {
        if (first.Missing) return new SvgColorParser().Failure(SvgColorParseError.ExpectedComponent, first.Start);
        reader.SkipCssWhitespace();
        SvgColorComponent second = new SvgColorParser().ReadComponent(reader, false);
        if (!second.Success) return new SvgColorParser().ComponentFailure(second);
        if (!new SvgColorParser().ReadComma(reader)) return new SvgColorParser().Failure(SvgColorParseError.InvalidSeparator, reader.GetPosition());
        SvgColorComponent third = new SvgColorParser().ReadComponent(reader, false);
        if (!third.Success) return new SvgColorParser().ComponentFailure(third);
        if (first.Percentage != second.Percentage || first.Percentage != third.Percentage)
        {
            int offset = first.Percentage != second.Percentage ? second.Start : third.Start;
            return new SvgColorParser().Failure(SvgColorParseError.MixedLegacyUnits, offset);
        }
        double alpha = 1;
        reader.SkipCssWhitespace();
        if (reader.Consume(','))
        {
            reader.SkipCssWhitespace();
            SvgColorComponent alphaComponent = new SvgColorParser().ReadComponent(reader, false);
            if (!alphaComponent.Success) return new SvgColorParser().ComponentFailure(alphaComponent);
            alpha = new SvgColorParser().NormalizeAlpha(alphaComponent);
            reader.SkipCssWhitespace();
        }
        if (!reader.Consume(')')) return new SvgColorParser().Failure(SvgColorParseError.ExpectedCloseParenthesis, reader.GetPosition());
        return new SvgColorParser().Success(new SvgColorParser().BuildRgb(first, second, third, alpha, false));
    }
SvgColorParseResult ParseModernRgb(SvgSpanReader reader, SvgColorComponent first, int firstWhitespace)
    {
        if (firstWhitespace == 0) return new SvgColorParser().Failure(SvgColorParseError.InvalidSeparator, reader.GetPosition());
        SvgColorComponent second = new SvgColorParser().ReadComponent(reader, true);
        if (!second.Success) return new SvgColorParser().ComponentFailure(second);
        if (reader.SkipCssWhitespace() == 0) return new SvgColorParser().Failure(SvgColorParseError.InvalidSeparator, reader.GetPosition());
        SvgColorComponent third = new SvgColorParser().ReadComponent(reader, true);
        if (!third.Success) return new SvgColorParser().ComponentFailure(third);
        reader.SkipCssWhitespace();
        SvgColorComponent? alpha = null;
        if (reader.Consume('/'))
        {
            reader.SkipCssWhitespace();
            alpha = new SvgColorParser().ReadComponent(reader, true);
            if (!alpha.Success) return new SvgColorParser().ComponentFailure(alpha);
            reader.SkipCssWhitespace();
        }
        if (!reader.Consume(')'))
        {
            SvgColorParseError error = reader.PeekCharacter().Value == ',' ? SvgColorParseError.InvalidSeparator : SvgColorParseError.ExpectedCloseParenthesis;
            return new SvgColorParser().Failure(error, reader.GetPosition());
        }
        double alphaValue = alpha == null || alpha.Missing ? alpha == null ? 1 : 0 : new SvgColorParser().NormalizeAlpha(alpha);
        SvgColor color = new SvgColorParser().BuildRgb(first, second, third, alphaValue, true);
        if (alpha != null) color.AlphaMissing = alpha.Missing;
        return new SvgColorParser().Success(color);
    }
SvgColorParseResult ParseHsl(SvgSpanReader reader)
    {
        reader.SkipCssWhitespace();
        SvgColorComponent hue = new SvgColorParser().ReadHue(reader, true);
        if (!hue.Success) return new SvgColorParser().ComponentFailure(hue);
        int whitespace = reader.SkipCssWhitespace();
        if (reader.Consume(','))
        {
            return new SvgColorParser().ParseLegacyHsl(reader, hue);
        }
        return new SvgColorParser().ParseModernHsl(reader, hue, whitespace);
    }
SvgColorParseResult ParseLegacyHsl(SvgSpanReader reader, SvgColorComponent hue)
    {
        if (hue.Missing) return new SvgColorParser().Failure(SvgColorParseError.ExpectedComponent, hue.Start);
        reader.SkipCssWhitespace();
        SvgColorComponent saturation = new SvgColorParser().ReadComponent(reader, false);
        if (!saturation.Success) return new SvgColorParser().ComponentFailure(saturation);
        if (!saturation.Percentage) return new SvgColorParser().Failure(SvgColorParseError.InvalidUnit, saturation.Start);
        if (!new SvgColorParser().ReadComma(reader)) return new SvgColorParser().Failure(SvgColorParseError.InvalidSeparator, reader.GetPosition());
        SvgColorComponent lightness = new SvgColorParser().ReadComponent(reader, false);
        if (!lightness.Success) return new SvgColorParser().ComponentFailure(lightness);
        if (!lightness.Percentage) return new SvgColorParser().Failure(SvgColorParseError.InvalidUnit, lightness.Start);
        double alpha = 1;
        reader.SkipCssWhitespace();
        if (reader.Consume(','))
        {
            reader.SkipCssWhitespace();
            SvgColorComponent alphaComponent = new SvgColorParser().ReadComponent(reader, false);
            if (!alphaComponent.Success) return new SvgColorParser().ComponentFailure(alphaComponent);
            alpha = new SvgColorParser().NormalizeAlpha(alphaComponent);
            reader.SkipCssWhitespace();
        }
        if (!reader.Consume(')')) return new SvgColorParser().Failure(SvgColorParseError.ExpectedCloseParenthesis, reader.GetPosition());
        return new SvgColorParser().BuildHsl(hue, saturation, lightness, alpha, null);
    }
SvgColorParseResult ParseModernHsl(SvgSpanReader reader, SvgColorComponent hue, int firstWhitespace)
    {
        if (firstWhitespace == 0) return new SvgColorParser().Failure(SvgColorParseError.InvalidSeparator, reader.GetPosition());
        SvgColorComponent saturation = new SvgColorParser().ReadComponent(reader, true);
        if (!saturation.Success) return new SvgColorParser().ComponentFailure(saturation);
        if (reader.SkipCssWhitespace() == 0) return new SvgColorParser().Failure(SvgColorParseError.InvalidSeparator, reader.GetPosition());
        SvgColorComponent lightness = new SvgColorParser().ReadComponent(reader, true);
        if (!lightness.Success) return new SvgColorParser().ComponentFailure(lightness);
        reader.SkipCssWhitespace();
        SvgColorComponent? alpha = null;
        if (reader.Consume('/'))
        {
            reader.SkipCssWhitespace();
            alpha = new SvgColorParser().ReadComponent(reader, true);
            if (!alpha.Success) return new SvgColorParser().ComponentFailure(alpha);
            reader.SkipCssWhitespace();
        }
        if (!reader.Consume(')'))
        {
            SvgColorParseError error = reader.PeekCharacter().Value == ',' ? SvgColorParseError.InvalidSeparator : SvgColorParseError.ExpectedCloseParenthesis;
            return new SvgColorParser().Failure(error, reader.GetPosition());
        }
        double alphaValue = alpha == null || alpha.Missing ? alpha == null ? 1 : 0 : new SvgColorParser().NormalizeAlpha(alpha);
        return new SvgColorParser().BuildHsl(hue, saturation, lightness, alphaValue, alpha);
    }
SvgColorParseResult BuildHsl(SvgColorComponent hue, SvgColorComponent saturation, SvgColorComponent lightness, double alpha, SvgColorComponent? alphaComponent)
    {
        double hueValue = hue.Missing ? 0 : hue.Value;
        double saturationValue = saturation.Missing ? 0 : saturation.Value / 100;
        double lightnessValue = lightness.Missing ? 0 : lightness.Value / 100;
        if (saturationValue < 0) saturationValue = 0;
        double hueSixth = hueValue / 30;
        double a = saturationValue * System.Math.Min(lightnessValue, 1 - lightnessValue);
        double red = new SvgColorParser().HslChannel(0, hueSixth, lightnessValue, a);
        double green = new SvgColorParser().HslChannel(8, hueSixth, lightnessValue, a);
        double blue = new SvgColorParser().HslChannel(4, hueSixth, lightnessValue, a);
        if (!SvgFiniteMath.IsFinite(red) || !SvgFiniteMath.IsFinite(green) || !SvgFiniteMath.IsFinite(blue))
        {
            return new SvgColorParser().Failure(SvgColorParseError.NonFiniteResult, hue.Start);
        }
        SvgColor color = new SvgColor(SvgColorSpace.Srgb, red, green, blue, alpha);
        color.FirstMissing = hue.Missing;
        color.SecondMissing = saturation.Missing;
        color.ThirdMissing = lightness.Missing;
        if (alphaComponent != null) color.AlphaMissing = alphaComponent.Missing;
        return new SvgColorParser().Success(color);
    }
SvgColorParseResult ParseHwb(SvgSpanReader reader)
    {
        reader.SkipCssWhitespace();
        SvgColorComponent hue = new SvgColorParser().ReadHue(reader, true);
        if (!hue.Success) return new SvgColorParser().ComponentFailure(hue);
        if (reader.SkipCssWhitespace() == 0) return new SvgColorParser().Failure(SvgColorParseError.InvalidSeparator, reader.GetPosition());
        SvgColorComponent white = new SvgColorParser().ReadComponent(reader, true);
        if (!white.Success) return new SvgColorParser().ComponentFailure(white);
        if (reader.SkipCssWhitespace() == 0) return new SvgColorParser().Failure(SvgColorParseError.InvalidSeparator, reader.GetPosition());
        SvgColorComponent black = new SvgColorParser().ReadComponent(reader, true);
        if (!black.Success) return new SvgColorParser().ComponentFailure(black);
        SvgColorComponent? alpha = new SvgColorParser().ReadModernAlpha(reader);
        if (alpha != null && !alpha.Success) return new SvgColorParser().ComponentFailure(alpha);
        if (!reader.Consume(')')) return new SvgColorParser().CloseFailure(reader);
        double whiteValue = white.Missing ? 0 : new SvgColorParser().Clamp(white.Value, 0, 100);
        double blackValue = black.Missing ? 0 : new SvgColorParser().Clamp(black.Value, 0, 100);
        double alphaValue = alpha == null ? 1 : alpha.Missing ? 0 : new SvgColorParser().NormalizeAlpha(alpha);
        SvgColor color = new SvgColor(SvgColorSpace.Hwb, hue.Missing ? 0 : hue.Value, whiteValue, blackValue, alphaValue);
        color.FirstMissing = hue.Missing;
        color.SecondMissing = white.Missing;
        color.ThirdMissing = black.Missing;
        if (alpha != null) color.AlphaMissing = alpha.Missing;
        return new SvgColorParser().Success(color);
    }
SvgColorParseResult ParseLabLike(SvgSpanReader reader, SvgColorSpace space)
    {
        reader.SkipCssWhitespace();
        SvgColorComponent first = new SvgColorParser().ReadComponent(reader, true);
        if (!first.Success) return new SvgColorParser().ComponentFailure(first);
        if (reader.SkipCssWhitespace() == 0) return new SvgColorParser().Failure(SvgColorParseError.InvalidSeparator, reader.GetPosition());
        SvgColorComponent second = new SvgColorParser().ReadComponent(reader, true);
        if (!second.Success) return new SvgColorParser().ComponentFailure(second);
        if (reader.SkipCssWhitespace() == 0) return new SvgColorParser().Failure(SvgColorParseError.InvalidSeparator, reader.GetPosition());
        bool polar = space == SvgColorSpace.Lch || space == SvgColorSpace.Oklch;
        SvgColorComponent third = polar ? new SvgColorParser().ReadHue(reader, true) : new SvgColorParser().ReadComponent(reader, true);
        if (!third.Success) return new SvgColorParser().ComponentFailure(third);
        SvgColorComponent? alpha = new SvgColorParser().ReadModernAlpha(reader);
        if (alpha != null && !alpha.Success) return new SvgColorParser().ComponentFailure(alpha);
        if (!reader.Consume(')')) return new SvgColorParser().CloseFailure(reader);
        double firstValue = first.Missing ? 0 : first.Value;
        if (space == SvgColorSpace.Oklab || space == SvgColorSpace.Oklch)
        {
            if (first.Percentage) firstValue = firstValue / 100;
            firstValue = new SvgColorParser().Clamp(firstValue, 0, 1);
        }
        else
        {
            firstValue = new SvgColorParser().Clamp(firstValue, 0, 100);
        }
        double secondValue = second.Missing ? 0 : second.Value;
        if (second.Percentage) secondValue = secondValue * (space == SvgColorSpace.Lab ? 1.25 : space == SvgColorSpace.Lch ? 1.5 : 0.004);
        if (polar && secondValue < 0) secondValue = 0;
        double thirdValue = third.Missing ? 0 : third.Value;
        if (!polar && third.Percentage) thirdValue = thirdValue * (space == SvgColorSpace.Lab ? 1.25 : 0.004);
        double alphaValue = alpha == null ? 1 : alpha.Missing ? 0 : new SvgColorParser().NormalizeAlpha(alpha);
        SvgColor color = new SvgColor(space, firstValue, secondValue, thirdValue, alphaValue);
        color.FirstMissing = first.Missing;
        color.SecondMissing = second.Missing;
        color.ThirdMissing = third.Missing;
        if (alpha != null) color.AlphaMissing = alpha.Missing;
        return new SvgColorParser().Success(color);
    }
SvgColorParseResult ParseColor(SvgSpanReader reader)
    {
        reader.SkipCssWhitespace();
        int start = reader.GetPosition();
        SvgSpanTokenResult token = reader.ReadToken(IsColorSpaceCharacter, true);
        if (!token.Success) return new SvgColorParser().Failure(SvgColorParseError.TokenTooLong, token.ErrorOffset);
        string name = token.GetValue().ToLowerInvariant();
        SvgColorSpace space;
        if (name == "srgb") space = SvgColorSpace.Srgb;
        else if (name == "srgb-linear") space = SvgColorSpace.SrgbLinear;
        else return new SvgColorParser().Failure(SvgColorParseError.InvalidUnit, start);
        if (reader.SkipCssWhitespace() == 0) return new SvgColorParser().Failure(SvgColorParseError.InvalidSeparator, reader.GetPosition());
        SvgColorComponent first = new SvgColorParser().ReadComponent(reader, true);
        if (!first.Success) return new SvgColorParser().ComponentFailure(first);
        if (reader.SkipCssWhitespace() == 0) return new SvgColorParser().Failure(SvgColorParseError.InvalidSeparator, reader.GetPosition());
        SvgColorComponent second = new SvgColorParser().ReadComponent(reader, true);
        if (!second.Success) return new SvgColorParser().ComponentFailure(second);
        if (reader.SkipCssWhitespace() == 0) return new SvgColorParser().Failure(SvgColorParseError.InvalidSeparator, reader.GetPosition());
        SvgColorComponent third = new SvgColorParser().ReadComponent(reader, true);
        if (!third.Success) return new SvgColorParser().ComponentFailure(third);
        SvgColorComponent? alpha = new SvgColorParser().ReadModernAlpha(reader);
        if (alpha != null && !alpha.Success) return new SvgColorParser().ComponentFailure(alpha);
        if (!reader.Consume(')')) return new SvgColorParser().CloseFailure(reader);
        double alphaValue = alpha == null ? 1 : alpha.Missing ? 0 : new SvgColorParser().NormalizeAlpha(alpha);
        SvgColor color = new SvgColor(space, new SvgColorParser().NormalizeColorComponent(first), new SvgColorParser().NormalizeColorComponent(second), new SvgColorParser().NormalizeColorComponent(third), alphaValue);
        color.FirstMissing = first.Missing;
        color.SecondMissing = second.Missing;
        color.ThirdMissing = third.Missing;
        if (alpha != null) color.AlphaMissing = alpha.Missing;
        return new SvgColorParser().Success(color);
    }
SvgColorComponent? ReadModernAlpha(SvgSpanReader reader)
    {
        reader.SkipCssWhitespace();
        if (!reader.Consume('/')) return null;
        reader.SkipCssWhitespace();
        SvgColorComponent alpha = new SvgColorParser().ReadComponent(reader, true);
        reader.SkipCssWhitespace();
        return alpha;
    }
SvgColorParseResult CloseFailure(SvgSpanReader reader)
    {
        SvgColorParseError error = reader.PeekCharacter().Value == ',' ? SvgColorParseError.InvalidSeparator : SvgColorParseError.ExpectedCloseParenthesis;
        return new SvgColorParser().Failure(error, reader.GetPosition());
    }
double NormalizeColorComponent(SvgColorComponent component)
    {
        if (component.Missing) return 0;
        return component.Percentage ? component.Value / 100 : component.Value;
    }
double HslChannel(double offset, double hue, double lightness, double a)
    {
        double k = (offset + hue) % 12;
        if (k < 0) k = k + 12;
        double middle = System.Math.Min(k - 3, 9 - k);
        middle = System.Math.Min(middle, 1);
        middle = System.Math.Max(-1, middle);
        return lightness - a * middle;
    }
SvgColor BuildRgb(SvgColorComponent first, SvgColorComponent second, SvgColorComponent third, double alpha, bool preserveMissing)
    {
        SvgColor color = new SvgColor(SvgColorSpace.Srgb, new SvgColorParser().NormalizeRgb(first), new SvgColorParser().NormalizeRgb(second), new SvgColorParser().NormalizeRgb(third), alpha);
        if (preserveMissing)
        {
            color.FirstMissing = first.Missing;
            color.SecondMissing = second.Missing;
            color.ThirdMissing = third.Missing;
        }
        return color;
    }
double NormalizeRgb(SvgColorComponent component)
    {
        if (component.Missing) return 0;
        double value = component.Percentage ? component.Value / 100 : component.Value / 255;
        return new SvgColorParser().Clamp(value, 0, 1);
    }
double NormalizeAlpha(SvgColorComponent component)
    {
        double value = component.Percentage ? component.Value / 100 : component.Value;
        return new SvgColorParser().Clamp(value, 0, 1);
    }
double Clamp(double value, double minimum, double maximum)
    {
        if (value < minimum) return minimum;
        if (value > maximum) return maximum;
        return SvgFiniteMath.NormalizeZero(value);
    }
bool ReadComma(SvgSpanReader reader)
    {
        reader.SkipCssWhitespace();
        if (!reader.Consume(',')) return false;
        reader.SkipCssWhitespace();
        return true;
    }
SvgColorComponent ReadComponent(SvgSpanReader reader, bool allowMissing)
    {
        int start = reader.GetPosition();
        if (allowMissing && new SvgColorParser().ConsumeNone(reader)) return new SvgColorComponent(true, 0, false, true, SvgColorParseError.None, start, -1);
        SvgNumberParseResult number = SvgNumberParser.ReadCssNumber(reader);
        if (!number.Success)
        {
            SvgColorParseError error = number.Error == SvgNumberParseError.TokenTooLong ? SvgColorParseError.TokenTooLong : number.Error == SvgNumberParseError.ExpectedNumber ? SvgColorParseError.ExpectedComponent : SvgColorParseError.InvalidNumber;
            return new SvgColorComponent(false, 0, false, false, error, start, number.ErrorOffset);
        }
        bool percentage = reader.Consume('%');
        return new SvgColorComponent(true, number.Value, percentage, false, SvgColorParseError.None, start, -1);
    }
SvgColorComponent ReadHue(SvgSpanReader reader, bool allowMissing)
    {
        int start = reader.GetPosition();
        if (allowMissing && new SvgColorParser().ConsumeNone(reader)) return new SvgColorComponent(true, 0, false, true, SvgColorParseError.None, start, -1);
        SvgNumberParseResult number = SvgNumberParser.ReadCssNumber(reader);
        if (!number.Success)
        {
            SvgColorParseError error = number.Error == SvgNumberParseError.TokenTooLong ? SvgColorParseError.TokenTooLong : number.Error == SvgNumberParseError.ExpectedNumber ? SvgColorParseError.ExpectedComponent : SvgColorParseError.InvalidNumber;
            return new SvgColorComponent(false, 0, false, false, error, start, number.ErrorOffset);
        }
        SvgSpanTokenResult unitToken = reader.ReadToken(IsAsciiLetter, false);
        if (!unitToken.Success) return new SvgColorComponent(false, 0, false, false, SvgColorParseError.TokenTooLong, start, unitToken.ErrorOffset);
        string unit = unitToken.GetValue().ToLowerInvariant();
        double degrees;
        if (unit == "grad") degrees = (number.Value % 400) * 0.9;
        else if (unit == "rad") degrees = (number.Value % (System.Math.PI * 2)) * 180 / System.Math.PI;
        else if (unit == "turn") degrees = (number.Value % 1) * 360;
        else if (unit == "" || unit == "deg") degrees = number.Value % 360;
        else return new SvgColorComponent(false, 0, false, false, SvgColorParseError.InvalidUnit, start, unitToken.Start);
        if (degrees < 0) degrees = degrees + 360;
        return new SvgColorComponent(true, SvgFiniteMath.NormalizeZero(degrees), false, false, SvgColorParseError.None, start, -1);
    }
bool ConsumeNone(SvgSpanReader reader)
    {
        int start = reader.GetPosition();
        System.ReadOnlySpan<char> input = reader.GetInput().Span;
        if (start + 4 > input.Length) return false;
        if (!new SvgColorParser().EqualAscii(input[start], 'n') || !new SvgColorParser().EqualAscii(input[start + 1], 'o') || !new SvgColorParser().EqualAscii(input[start + 2], 'n') || !new SvgColorParser().EqualAscii(input[start + 3], 'e')) return false;
        if (start + 4 < input.Length && new SvgColorParser().IsAsciiLetter(input[start + 4])) return false;
        reader.SetPosition(start + 4);
        return true;
    }
bool EqualAscii(char value, char lower)
    {
        return value == lower || value == lower - 32;
    }
bool IsAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }
bool IsColorSpaceCharacter(char value)
    {
        return new SvgColorParser().IsAsciiLetter(value) || value == '-';
    }
SvgColorParseResult ComponentFailure(SvgColorComponent component)
    {
        return new SvgColorParser().Failure(component.Error, component.ErrorOffset);
    }
SvgColor ColorFromPacked(int packed)
    {
        double red = (packed >> 16 & 255) / 255.0;
        double green = (packed >> 8 & 255) / 255.0;
        double blue = (packed & 255) / 255.0;
        return new SvgColor(SvgColorSpace.Srgb, red, green, blue, 1);
    }
SvgColorParseResult Success(SvgColor color)
    {
        return new SvgColorParseResult(true, SvgColorParseError.None, color, -1);
    }
SvgColorParseResult Failure(SvgColorParseError error, int offset)
    {
        return new SvgColorParseResult(false, error, null, offset);
    }
}
