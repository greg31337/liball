namespace svg;

internal class SvgGeneratedPathBuilder
{
    internal System.Collections.Generic.List<SvgPathCommand> Commands;
    double currentX;
    double currentY;
    double subpathX;
    double subpathY;
    int subpathIndex;

    internal SvgGeneratedPathBuilder()
    {
        Commands = new System.Collections.Generic.List<SvgPathCommand>();
        currentX = 0;
        currentY = 0;
        subpathX = 0;
        subpathY = 0;
        subpathIndex = -1;
    }

    internal void MoveTo(double x, double y)
    {
        x = SvgFiniteMath.NormalizeZero(x);
        y = SvgFiniteMath.NormalizeZero(y);
        int sourceOffset = Commands.Count;
        subpathIndex = subpathIndex + 1;
        SvgPathCommand command = new SvgPathCommand(SvgPathCommandKind.MoveTo, false, new double[] { x, y }, sourceOffset, sourceOffset + 1, true, currentX, currentY, x, y, x, y, subpathIndex);
        Commands.Add(command);
        currentX = x;
        currentY = y;
        subpathX = x;
        subpathY = y;
    }

    internal void LineTo(double x, double y)
    {
        x = SvgFiniteMath.NormalizeZero(x);
        y = SvgFiniteMath.NormalizeZero(y);
        int sourceOffset = Commands.Count;
        SvgPathCommand command = new SvgPathCommand(SvgPathCommandKind.LineTo, false, new double[] { x, y }, sourceOffset, sourceOffset + 1, true, currentX, currentY, x, y, subpathX, subpathY, subpathIndex);
        Commands.Add(command);
        currentX = x;
        currentY = y;
    }

    internal void ArcTo(double rx, double ry, double x, double y)
    {
        ArcToWithParameters(rx, ry, 0, false, true, x, y);
    }

    internal void ArcToWithParameters(double rx, double ry, double rotation, bool largeArc, bool sweep, double x, double y)
    {
        rx = SvgFiniteMath.NormalizeZero(rx);
        ry = SvgFiniteMath.NormalizeZero(ry);
        rotation = SvgFiniteMath.NormalizeZero(rotation);
        x = SvgFiniteMath.NormalizeZero(x);
        y = SvgFiniteMath.NormalizeZero(y);
        int sourceOffset = Commands.Count;
        double largeArcValue = largeArc ? 1 : 0;
        double sweepValue = sweep ? 1 : 0;
        double[] values = new double[] { rx, ry, rotation, largeArcValue, sweepValue, x, y };
        SvgPathCommand command = new SvgPathCommand(SvgPathCommandKind.ArcTo, false, values, sourceOffset, sourceOffset + 1, true, currentX, currentY, x, y, subpathX, subpathY, subpathIndex);
        Commands.Add(command);
        currentX = x;
        currentY = y;
    }

    internal void CubicTo(double control1X, double control1Y, double control2X, double control2Y, double x, double y)
    {
        control1X = SvgFiniteMath.NormalizeZero(control1X);
        control1Y = SvgFiniteMath.NormalizeZero(control1Y);
        control2X = SvgFiniteMath.NormalizeZero(control2X);
        control2Y = SvgFiniteMath.NormalizeZero(control2Y);
        x = SvgFiniteMath.NormalizeZero(x);
        y = SvgFiniteMath.NormalizeZero(y);
        int sourceOffset = Commands.Count;
        double[] values = new double[] { control1X, control1Y, control2X, control2Y, x, y };
        SvgPathCommand command = new SvgPathCommand(SvgPathCommandKind.CubicCurveTo, false, values, sourceOffset, sourceOffset + 1, true, currentX, currentY, x, y, subpathX, subpathY, subpathIndex);
        Commands.Add(command);
        currentX = x;
        currentY = y;
    }

    internal void QuadraticTo(double controlX, double controlY, double x, double y)
    {
        controlX = SvgFiniteMath.NormalizeZero(controlX);
        controlY = SvgFiniteMath.NormalizeZero(controlY);
        x = SvgFiniteMath.NormalizeZero(x);
        y = SvgFiniteMath.NormalizeZero(y);
        int sourceOffset = Commands.Count;
        double[] values = new double[] { controlX, controlY, x, y };
        SvgPathCommand command = new SvgPathCommand(SvgPathCommandKind.QuadraticCurveTo, false, values, sourceOffset, sourceOffset + 1, true, currentX, currentY, x, y, subpathX, subpathY, subpathIndex);
        Commands.Add(command);
        currentX = x;
        currentY = y;
    }

    internal void Close()
    {
        int sourceOffset = Commands.Count;
        SvgPathCommand command = new SvgPathCommand(SvgPathCommandKind.ClosePath, false, new double[0], sourceOffset, sourceOffset + 1, true, currentX, currentY, subpathX, subpathY, subpathX, subpathY, subpathIndex);
        Commands.Add(command);
        currentX = subpathX;
        currentY = subpathY;
    }

    internal SvgPathData Build()
    {
        return new SvgPathData(Commands, "", new System.Collections.Generic.List<SvgPathParseIssue>());
    }
}
