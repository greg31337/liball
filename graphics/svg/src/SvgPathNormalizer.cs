namespace svg;

internal class SvgPathNormalizerState
{
    SvgPathData source;
    SvgPathNormalizeOptions options;
    SvgGeneratedPathBuilder builder;
    bool hasCurrent;
    double currentX;
    double currentY;
    double subpathX;
    double subpathY;
    bool hasCubicControl;
    double cubicControlX;
    double cubicControlY;
    bool hasQuadraticControl;
    double quadraticControlX;
    double quadraticControlY;

    internal SvgPathNormalizerState(SvgPathData path, SvgPathNormalizeOptions normalizeOptions)
    {
        source = path;
        options = normalizeOptions;
        builder = new SvgGeneratedPathBuilder();
        hasCurrent = false;
        currentX = 0;
        currentY = 0;
        subpathX = 0;
        subpathY = 0;
        hasCubicControl = false;
        cubicControlX = 0;
        cubicControlY = 0;
        hasQuadraticControl = false;
        quadraticControlX = 0;
        quadraticControlY = 0;
    }

    internal SvgPathNormalizeResult Run()
    {
        int index = 0;
        while (index < source.Commands.Count)
        {
            SvgPathCommand command = source.Commands[index];
            SvgPathNormalizeError validation = ValidateCommand(command, index);
            if (validation != SvgPathNormalizeError.None) return new SvgPathNormalizer().Failure(validation);
            SvgPathNormalizeError processed = Process(command);
            if (processed != SvgPathNormalizeError.None) return new SvgPathNormalizer().Failure(processed);
            index = index + 1;
        }
        return new SvgPathNormalizeResult(true, SvgPathNormalizeError.None, builder.Build());
    }

    SvgPathNormalizeError ValidateCommand(SvgPathCommand command, int index)
    {
        if (command == null || !command.IsValid || !SvgPathCommandRules.IsKnown(command.Kind) || command.Values == null) return SvgPathNormalizeError.InvalidPath;
        if (command.Values.Length != SvgPathCommandRules.ParameterCount(command.Kind)) return SvgPathNormalizeError.InvalidPath;
        if (index == 0 && command.Kind != SvgPathCommandKind.MoveTo) return SvgPathNormalizeError.InvalidPath;
        if (command.Kind != SvgPathCommandKind.MoveTo && !hasCurrent) return SvgPathNormalizeError.InvalidPath;
        int valueIndex = 0;
        while (valueIndex < command.Values.Length)
        {
            if (!SvgFiniteMath.IsFinite(command.Values[valueIndex])) return SvgPathNormalizeError.InvalidPath;
            valueIndex = valueIndex + 1;
        }
        if (command.Kind == SvgPathCommandKind.ArcTo)
        {
            if (command.Values[0] < 0 || command.Values[1] < 0) return SvgPathNormalizeError.InvalidPath;
            if (command.Values[3] != 0 && command.Values[3] != 1 || command.Values[4] != 0 && command.Values[4] != 1) return SvgPathNormalizeError.InvalidPath;
        }
        return SvgPathNormalizeError.None;
    }

    SvgPathNormalizeError Process(SvgPathCommand command)
    {
        if (command.Kind == SvgPathCommandKind.MoveTo) return Move(command);
        if (command.Kind == SvgPathCommandKind.ClosePath) return Close();
        if (command.Kind == SvgPathCommandKind.LineTo) return Line(command);
        if (command.Kind == SvgPathCommandKind.HorizontalLineTo) return Horizontal(command);
        if (command.Kind == SvgPathCommandKind.VerticalLineTo) return Vertical(command);
        if (command.Kind == SvgPathCommandKind.CubicCurveTo) return Cubic(command);
        if (command.Kind == SvgPathCommandKind.SmoothCubicCurveTo) return SmoothCubic(command);
        if (command.Kind == SvgPathCommandKind.QuadraticCurveTo) return Quadratic(command);
        if (command.Kind == SvgPathCommandKind.SmoothQuadraticCurveTo) return SmoothQuadratic(command);
        if (command.Kind == SvgPathCommandKind.ArcTo) return Arc(command);
        return SvgPathNormalizeError.InvalidPath;
    }

    SvgPathNormalizeError Move(SvgPathCommand command)
    {
        SvgFiniteResult x = Resolve(command.Values[0], currentX, command.Relative);
        SvgFiniteResult y = Resolve(command.Values[1], currentY, command.Relative);
        if (!x.Success || !y.Success) return SvgPathNormalizeError.NonFiniteResult;
        builder.MoveTo(x.Value, y.Value);
        currentX = x.Value;
        currentY = y.Value;
        subpathX = x.Value;
        subpathY = y.Value;
        hasCurrent = true;
        ResetControls();
        return SvgPathNormalizeError.None;
    }

    SvgPathNormalizeError Close()
    {
        builder.Close();
        currentX = subpathX;
        currentY = subpathY;
        ResetControls();
        return SvgPathNormalizeError.None;
    }

    SvgPathNormalizeError Line(SvgPathCommand command)
    {
        SvgFiniteResult x = Resolve(command.Values[0], currentX, command.Relative);
        SvgFiniteResult y = Resolve(command.Values[1], currentY, command.Relative);
        if (!x.Success || !y.Success) return SvgPathNormalizeError.NonFiniteResult;
        AddLine(x.Value, y.Value);
        return SvgPathNormalizeError.None;
    }

    SvgPathNormalizeError Horizontal(SvgPathCommand command)
    {
        SvgFiniteResult x = Resolve(command.Values[0], currentX, command.Relative);
        if (!x.Success) return SvgPathNormalizeError.NonFiniteResult;
        AddLine(x.Value, currentY);
        return SvgPathNormalizeError.None;
    }

    SvgPathNormalizeError Vertical(SvgPathCommand command)
    {
        SvgFiniteResult y = Resolve(command.Values[0], currentY, command.Relative);
        if (!y.Success) return SvgPathNormalizeError.NonFiniteResult;
        AddLine(currentX, y.Value);
        return SvgPathNormalizeError.None;
    }

    void AddLine(double x, double y)
    {
        builder.LineTo(x, y);
        currentX = x;
        currentY = y;
        ResetControls();
    }

    SvgPathNormalizeError Cubic(SvgPathCommand command)
    {
        SvgFiniteResult control1X = Resolve(command.Values[0], currentX, command.Relative);
        SvgFiniteResult control1Y = Resolve(command.Values[1], currentY, command.Relative);
        SvgFiniteResult control2X = Resolve(command.Values[2], currentX, command.Relative);
        SvgFiniteResult control2Y = Resolve(command.Values[3], currentY, command.Relative);
        SvgFiniteResult x = Resolve(command.Values[4], currentX, command.Relative);
        SvgFiniteResult y = Resolve(command.Values[5], currentY, command.Relative);
        if (!control1X.Success || !control1Y.Success || !control2X.Success || !control2Y.Success || !x.Success || !y.Success) return SvgPathNormalizeError.NonFiniteResult;
        AddCubic(control1X.Value, control1Y.Value, control2X.Value, control2Y.Value, x.Value, y.Value);
        return SvgPathNormalizeError.None;
    }

    SvgPathNormalizeError SmoothCubic(SvgPathCommand command)
    {
        SvgFiniteResult control1X = hasCubicControl ? Reflect(currentX, cubicControlX) : Finite(currentX);
        SvgFiniteResult control1Y = hasCubicControl ? Reflect(currentY, cubicControlY) : Finite(currentY);
        SvgFiniteResult control2X = Resolve(command.Values[0], currentX, command.Relative);
        SvgFiniteResult control2Y = Resolve(command.Values[1], currentY, command.Relative);
        SvgFiniteResult x = Resolve(command.Values[2], currentX, command.Relative);
        SvgFiniteResult y = Resolve(command.Values[3], currentY, command.Relative);
        if (!control1X.Success || !control1Y.Success || !control2X.Success || !control2Y.Success || !x.Success || !y.Success) return SvgPathNormalizeError.NonFiniteResult;
        AddCubic(control1X.Value, control1Y.Value, control2X.Value, control2Y.Value, x.Value, y.Value);
        return SvgPathNormalizeError.None;
    }

    void AddCubic(double control1X, double control1Y, double control2X, double control2Y, double x, double y)
    {
        builder.CubicTo(control1X, control1Y, control2X, control2Y, x, y);
        currentX = x;
        currentY = y;
        hasCubicControl = true;
        cubicControlX = control2X;
        cubicControlY = control2Y;
        hasQuadraticControl = false;
    }

    SvgPathNormalizeError Quadratic(SvgPathCommand command)
    {
        SvgFiniteResult controlX = Resolve(command.Values[0], currentX, command.Relative);
        SvgFiniteResult controlY = Resolve(command.Values[1], currentY, command.Relative);
        SvgFiniteResult x = Resolve(command.Values[2], currentX, command.Relative);
        SvgFiniteResult y = Resolve(command.Values[3], currentY, command.Relative);
        if (!controlX.Success || !controlY.Success || !x.Success || !y.Success) return SvgPathNormalizeError.NonFiniteResult;
        return AddQuadratic(controlX.Value, controlY.Value, x.Value, y.Value);
    }

    SvgPathNormalizeError SmoothQuadratic(SvgPathCommand command)
    {
        SvgFiniteResult controlX = hasQuadraticControl ? Reflect(currentX, quadraticControlX) : Finite(currentX);
        SvgFiniteResult controlY = hasQuadraticControl ? Reflect(currentY, quadraticControlY) : Finite(currentY);
        SvgFiniteResult x = Resolve(command.Values[0], currentX, command.Relative);
        SvgFiniteResult y = Resolve(command.Values[1], currentY, command.Relative);
        if (!controlX.Success || !controlY.Success || !x.Success || !y.Success) return SvgPathNormalizeError.NonFiniteResult;
        return AddQuadratic(controlX.Value, controlY.Value, x.Value, y.Value);
    }

    SvgPathNormalizeError AddQuadratic(double controlX, double controlY, double x, double y)
    {
        if (options.ConvertQuadraticsToCubics)
        {
            SvgFiniteResult control1X = CubicControl(currentX, controlX);
            SvgFiniteResult control1Y = CubicControl(currentY, controlY);
            SvgFiniteResult control2X = CubicControl(x, controlX);
            SvgFiniteResult control2Y = CubicControl(y, controlY);
            if (!control1X.Success || !control1Y.Success || !control2X.Success || !control2Y.Success) return SvgPathNormalizeError.NonFiniteResult;
            builder.CubicTo(control1X.Value, control1Y.Value, control2X.Value, control2Y.Value, x, y);
        }
        else
        {
            builder.QuadraticTo(controlX, controlY, x, y);
        }
        currentX = x;
        currentY = y;
        hasQuadraticControl = true;
        quadraticControlX = controlX;
        quadraticControlY = controlY;
        hasCubicControl = false;
        return SvgPathNormalizeError.None;
    }

    SvgPathNormalizeError Arc(SvgPathCommand command)
    {
        SvgFiniteResult x = Resolve(command.Values[5], currentX, command.Relative);
        SvgFiniteResult y = Resolve(command.Values[6], currentY, command.Relative);
        if (!x.Success || !y.Success) return SvgPathNormalizeError.NonFiniteResult;
        builder.ArcToWithParameters(command.Values[0], command.Values[1], command.Values[2], command.Values[3] == 1, command.Values[4] == 1, x.Value, y.Value);
        currentX = x.Value;
        currentY = y.Value;
        ResetControls();
        return SvgPathNormalizeError.None;
    }

    void ResetControls()
    {
        hasCubicControl = false;
        hasQuadraticControl = false;
    }

    static SvgFiniteResult Resolve(double value, double origin, bool relative)
    {
        if (relative) return SvgFiniteMath.Add(origin, value);
        return Finite(value);
    }

    static SvgFiniteResult Reflect(double point, double control)
    {
        SvgFiniteResult difference = SvgFiniteMath.Subtract(point, control);
        if (!difference.Success) return difference;
        return SvgFiniteMath.Add(point, difference.Value);
    }

    static SvgFiniteResult CubicControl(double endpoint, double quadraticControl)
    {
        if (endpoint == quadraticControl) return Finite(endpoint);
        double endpointThird = endpoint / 3;
        double controlThird = quadraticControl / 3;
        SvgFiniteResult first = SvgFiniteMath.Add(endpointThird, controlThird);
        if (!first.Success) return first;
        return SvgFiniteMath.Add(first.Value, controlThird);
    }

    static SvgFiniteResult Finite(double value)
    {
        if (!SvgFiniteMath.IsFinite(value)) return new SvgFiniteResult(false, 0, "Path coordinate is not finite.");
        return new SvgFiniteResult(true, SvgFiniteMath.NormalizeZero(value), "");
    }
}
