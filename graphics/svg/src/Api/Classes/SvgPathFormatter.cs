namespace svg;

/// <summary>Writes canonical and minified SVG path data.</summary>
public class SvgPathFormatter : ISvgPathFormatter
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgPathFormatter()
    {
    }

    /// <summary>Formats a path using explicit options.</summary>
    /// <param name="path">The valid path data.</param>
    /// <param name="options">The formatting options.</param>
    /// <returns>Formatted text or a stable failure.</returns>
    public SvgPathFormatResult Format(SvgPathData path, SvgPathFormatOptions options)
    {
        SvgPathFormatError validation = new SvgPathFormatter().Validate(path, options);
        if (validation != SvgPathFormatError.None) return new SvgPathFormatter().Failure(validation);
        if (options.Mode == SvgPathFormatMode.Canonical) return new SvgPathFormatter().WriteCanonical(path, options);
        if (options.Mode == SvgPathFormatMode.Minified) return new SvgPathFormatter().WriteMinified(path, options);
        return new SvgPathFormatter().Failure(SvgPathFormatError.InvalidMode);
    }

    /// <summary>Formats a path canonically with exact shortest-round-trip values.</summary>
    /// <param name="path">The valid path data.</param>
    /// <returns>Canonical path text or a stable failure.</returns>
    public SvgPathFormatResult FormatCanonicalWithPath(SvgPathData path)
    {
        return new SvgPathFormatter().Format(path, SvgPathFormatOptions.Create(SvgPathFormatMode.Canonical, false, 0));
    }

    /// <summary>Formats a path canonically with a maximum fractional precision.</summary>
    /// <param name="path">The valid path data.</param>
    /// <param name="fractionalDigits">The maximum fractional digit count.</param>
    /// <returns>Canonical rounded path text or a stable failure.</returns>
    public SvgPathFormatResult FormatCanonicalWithPathAndFractionalDigits(SvgPathData path, int fractionalDigits)
    {
        return new SvgPathFormatter().Format(path, SvgPathFormatOptions.Create(SvgPathFormatMode.Canonical, true, fractionalDigits));
    }

    /// <summary>Formats a path with compact exact numbers and separators.</summary>
    /// <param name="path">The valid path data.</param>
    /// <returns>Minified path text or a stable failure.</returns>
    public SvgPathFormatResult FormatMinifiedWithPath(SvgPathData path)
    {
        return new SvgPathFormatter().Format(path, SvgPathFormatOptions.Create(SvgPathFormatMode.Minified, false, 0));
    }

    /// <summary>Formats a path compactly with a maximum fractional precision.</summary>
    /// <param name="path">The valid path data.</param>
    /// <param name="fractionalDigits">The maximum fractional digit count.</param>
    /// <returns>Minified rounded path text or a stable failure.</returns>
    public SvgPathFormatResult FormatMinifiedWithPathAndFractionalDigits(SvgPathData path, int fractionalDigits)
    {
        return new SvgPathFormatter().Format(path, SvgPathFormatOptions.Create(SvgPathFormatMode.Minified, true, fractionalDigits));
    }
SvgPathFormatResult WriteCanonical(SvgPathData? path, SvgPathFormatOptions options)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int commandIndex = 0;
        while (commandIndex < (path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count)
        {
            SvgPathCommand command = path.Commands[commandIndex];
            if (commandIndex > 0) builder.Append(' ');
            builder.Append(new SvgPathFormatter().CommandLetter(command));
            int parameterIndex = 0;
            while (parameterIndex < command.Values.Length)
            {
                string? number = new SvgPathFormatter().Number(command, parameterIndex, options, false);
                if (number == null) return new SvgPathFormatter().Failure(SvgPathFormatError.InvalidNumber);
                builder.Append(' ');
                builder.Append(number);
                parameterIndex = parameterIndex + 1;
            }
            commandIndex = commandIndex + 1;
        }
        return new SvgPathFormatter().Success(builder.ToString());
    }
SvgPathFormatResult WriteMinified(SvgPathData? path, SvgPathFormatOptions options)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        SvgPathCommandKind activeKind = SvgPathCommandKind.Unknown;
        bool activeRelative = false;
        string? previousNumber = null;
        int commandIndex = 0;
        while (commandIndex < (path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count)
        {
            SvgPathCommand command = path.Commands[commandIndex];
            bool emitLetter = command.Kind == SvgPathCommandKind.MoveTo || command.Kind == SvgPathCommandKind.ClosePath || command.Kind != activeKind || command.Relative != activeRelative;
            if (emitLetter)
            {
                builder.Append(new SvgPathFormatter().CommandLetter(command));
                previousNumber = null;
            }
            int parameterIndex = 0;
            while (parameterIndex < command.Values.Length)
            {
                string? number = new SvgPathFormatter().Number(command, parameterIndex, options, true);
                if (number == null) return new SvgPathFormatter().Failure(SvgPathFormatError.InvalidNumber);
                bool arcCompact = command.Kind == SvgPathCommandKind.ArcTo && (parameterIndex == 4 || parameterIndex == 5);
                new SvgPathFormatter().AppendMinifiedNumber(builder, previousNumber, number, arcCompact);
                previousNumber = number;
                parameterIndex = parameterIndex + 1;
            }
            if (command.Kind == SvgPathCommandKind.MoveTo)
            {
                activeKind = SvgPathCommandKind.LineTo;
                activeRelative = command.Relative;
            }
            else if (command.Kind == SvgPathCommandKind.ClosePath)
            {
                activeKind = SvgPathCommandKind.Unknown;
                previousNumber = null;
            }
            else
            {
                activeKind = command.Kind;
                activeRelative = command.Relative;
            }
            commandIndex = commandIndex + 1;
        }
        return new SvgPathFormatter().Success(builder.ToString());
    }
void AppendMinifiedNumber(System.Text.StringBuilder builder, string? previous, string current, bool arcCompact)
    {
        if (previous != null && !arcCompact && new SvgPathFormatter().NeedsSeparator(previous, current)) builder.Append(' ');
        builder.Append(current);
    }
bool NeedsSeparator(string previous, string current)
    {
        char first = current[0];
        if (first == '+' || first == '-') return false;
        if (first == '.') return previous.IndexOf('.') < 0 && previous.IndexOf('e') < 0;
        return true;
    }
string? Number(SvgPathCommand command, int parameterIndex, SvgPathFormatOptions options, bool compact)
    {
        if (command.Kind == SvgPathCommandKind.ArcTo && (parameterIndex == 3 || parameterIndex == 4)) return command.Values[parameterIndex] == 0 ? "0" : "1";
        SvgNumberFormatResult result;
        if (options.UseFractionalPrecision)
        {
            result = SvgNumberFormatter.FormatFixedTrimmed(command.Values[parameterIndex], options.FractionalDigits);
            if (result.Success && compact) result.Text = new SvgPathFormatter().RemoveFractionLeadingZero(result.Text);
        }
        else if (compact)
        {
            result = SvgNumberFormatter.FormatCompact(command.Values[parameterIndex]);
        }
        else
        {
            result = SvgNumberFormatter.FormatCanonical(command.Values[parameterIndex]);
        }
        return result.Success ? result.Text : null;
    }
string RemoveFractionLeadingZero(string text)
    {
        if (text.Length > 1 && text[0] == '0' && text[1] == '.') return text.Substring(1);
        if (text.Length > 2 && text[0] == '-' && text[1] == '0' && text[2] == '.') return "-" + text.Substring(2);
        return text;
    }
char CommandLetter(SvgPathCommand command)
    {
        char letter = 'M';
        if (command.Kind == SvgPathCommandKind.ClosePath) letter = 'Z';
        else if (command.Kind == SvgPathCommandKind.LineTo) letter = 'L';
        else if (command.Kind == SvgPathCommandKind.HorizontalLineTo) letter = 'H';
        else if (command.Kind == SvgPathCommandKind.VerticalLineTo) letter = 'V';
        else if (command.Kind == SvgPathCommandKind.CubicCurveTo) letter = 'C';
        else if (command.Kind == SvgPathCommandKind.SmoothCubicCurveTo) letter = 'S';
        else if (command.Kind == SvgPathCommandKind.QuadraticCurveTo) letter = 'Q';
        else if (command.Kind == SvgPathCommandKind.SmoothQuadraticCurveTo) letter = 'T';
        else if (command.Kind == SvgPathCommandKind.ArcTo) letter = 'A';
        if (command.Relative) return (char)(letter - 'A' + 'a');
        return letter;
    }
SvgPathFormatError Validate(SvgPathData? path, SvgPathFormatOptions? options)
    {
        if (path == null || options == null) return SvgPathFormatError.InvalidArgument;
        if (options.Mode != SvgPathFormatMode.Canonical && options.Mode != SvgPathFormatMode.Minified) return SvgPathFormatError.InvalidMode;
        if (options.UseFractionalPrecision && (options.FractionalDigits < 0 || options.FractionalDigits > SvgNumberFormatter.MaximumFixedPrecision)) return SvgPathFormatError.InvalidPrecision;
        if (!path.IsValid || path.Commands == null) return SvgPathFormatError.InvalidPath;
        int commandIndex = 0;
        while (commandIndex < path.Commands.Count)
        {
            SvgPathCommand command = path.Commands[commandIndex];
            if (command == null || !command.IsValid || !SvgPathCommandRules.IsKnown(command.Kind) || command.Values == null || command.Values.Length != SvgPathCommandRules.ParameterCount(command.Kind)) return SvgPathFormatError.InvalidPath;
            if (commandIndex == 0 && command.Kind != SvgPathCommandKind.MoveTo) return SvgPathFormatError.InvalidPath;
            int parameterIndex = 0;
            while (parameterIndex < command.Values.Length)
            {
                if (!SvgFiniteMath.IsFinite(command.Values[parameterIndex])) return SvgPathFormatError.InvalidPath;
                parameterIndex = parameterIndex + 1;
            }
            if (command.Kind == SvgPathCommandKind.ArcTo)
            {
                if (command.Values[0] < 0 || command.Values[1] < 0) return SvgPathFormatError.InvalidPath;
                if (command.Values[3] != 0 && command.Values[3] != 1 || command.Values[4] != 0 && command.Values[4] != 1) return SvgPathFormatError.InvalidPath;
            }
            commandIndex = commandIndex + 1;
        }
        return SvgPathFormatError.None;
    }
SvgPathFormatResult Success(string text)
    {
        return new SvgPathFormatResult(true, SvgPathFormatError.None, text);
    }
SvgPathFormatResult Failure(SvgPathFormatError error)
    {
        return new SvgPathFormatResult(false, error, "");
    }
}
