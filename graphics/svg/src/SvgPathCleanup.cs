namespace svg;

internal class SvgCleanupSubpath
{
    internal double StartX; internal double StartY; internal System.Collections.Generic.List<SvgPathCommand> Parts;
    internal SvgCleanupSubpath(double startX, double startY) { StartX = startX; StartY = startY; Parts = new System.Collections.Generic.List<SvgPathCommand>(); }
}

internal class SvgPathCleanupState
{
    SvgPathData? path; SvgPathCleanupOptions options; System.Collections.Generic.List<SvgCleanupSubpath> subpaths; SvgCleanupSubpath? current; int compacted;
    internal SvgPathCleanupState(SvgPathData? normalizedPath, SvgPathCleanupOptions cleanupOptions) { path = normalizedPath; options = cleanupOptions; subpaths = new System.Collections.Generic.List<SvgCleanupSubpath>(); current = null; compacted = 0; }
    internal SvgPathCleanupResult Run()
    {
        int index = 0; while (index < (path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count) { SvgPathCleanupError error = Process(path.Commands[index]); if (error != SvgPathCleanupError.None) return new SvgPathCleanup().Failure(error); index = index + 1; }
        SvgGeneratedPathBuilder builder = new SvgGeneratedPathBuilder(); index = 0; while (index < subpaths.Count) { SvgCleanupSubpath subpath = subpaths[index]; if (subpath.Parts.Count != 0) AddSubpath(builder, subpath); index = index + 1; } SvgPathData output = builder.Build(); if (output.Commands.Count > options.MaximumCommands) return new SvgPathCleanup().Failure(SvgPathCleanupError.CommandLimitExceeded); int removed = path.Commands.Count - output.Commands.Count; return new SvgPathCleanupResult(true, SvgPathCleanupError.None, output, path.Commands.Count, output.Commands.Count, removed, compacted);
    }
    SvgPathCleanupError Process(SvgPathCommand command)
    {
        if (command.Kind == SvgPathCommandKind.MoveTo) { current = new SvgCleanupSubpath(command.EndX, command.EndY); subpaths.Add(current); return SvgPathCleanupError.None; }
        if (current == null) return SvgPathCleanupError.InvalidPath; if (command.Kind == SvgPathCommandKind.LineTo) { AddLine(command); return SvgPathCleanupError.None; } if (command.Kind == SvgPathCommandKind.QuadraticCurveTo) { AddQuadratic(command); return SvgPathCleanupError.None; } if (command.Kind == SvgPathCommandKind.CubicCurveTo) { AddCubic(command); return SvgPathCleanupError.None; } if (command.Kind == SvgPathCommandKind.ArcTo) return AddArc(command); if (command.Kind == SvgPathCommandKind.ClosePath) { AddClose(command); return SvgPathCleanupError.None; } return SvgPathCleanupError.InvalidPath;
    }
    void AddLine(SvgPathCommand command)
    {
        if (Same(command.StartX, command.StartY, command.EndX, command.EndY)) return; SvgPathCommand? previous = Tail(); if (previous != null && previous.Kind == SvgPathCommandKind.LineTo && Forward(previous.StartX, previous.StartY, previous.EndX, previous.EndY, command.EndX, command.EndY)) { previous.EndX = command.EndX; previous.EndY = command.EndY; previous.Values[0] = command.EndX; previous.Values[1] = command.EndY; compacted = compacted + 1; return; } (current ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Parts.Add(command);
    }
    void AddQuadratic(SvgPathCommand command)
    {
        double controlX = command.Values[0]; double controlY = command.Values[1]; if (Same(command.StartX, command.StartY, command.EndX, command.EndY) && Same(command.StartX, command.StartY, controlX, controlY)) return; if (Forward(command.StartX, command.StartY, controlX, controlY, command.EndX, command.EndY)) { AddLine(Line(command)); compacted = compacted + 1; return; } (current ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Parts.Add(command);
    }
    void AddCubic(SvgPathCommand command)
    {
        double firstX = command.Values[0]; double firstY = command.Values[1]; double secondX = command.Values[2]; double secondY = command.Values[3]; if (Same(command.StartX, command.StartY, command.EndX, command.EndY) && Same(command.StartX, command.StartY, firstX, firstY) && Same(command.StartX, command.StartY, secondX, secondY)) return; if (Forward(command.StartX, command.StartY, firstX, firstY, command.EndX, command.EndY) && Forward(command.StartX, command.StartY, secondX, secondY, command.EndX, command.EndY) && Ordered(command.StartX, command.StartY, firstX, firstY, secondX, secondY, command.EndX, command.EndY)) { AddLine(Line(command)); compacted = compacted + 1; return; } (current ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Parts.Add(command);
    }
    SvgPathCleanupError AddArc(SvgPathCommand command)
    {
        SvgArcOperationResult result = new SvgArcGeometry().FromCommand(command); if (!result.Success) return result.Error == SvgGeometryError.InvalidArgument ? SvgPathCleanupError.InvalidPath : SvgPathCleanupError.NonFiniteResult; if ((result.Arc ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).IsPoint) return SvgPathCleanupError.None; if (result.Arc.IsLine) { AddLine(Line(command)); compacted = compacted + 1; return SvgPathCleanupError.None; } (current ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Parts.Add(command); return SvgPathCleanupError.None;
    }
    void AddClose(SvgPathCommand command)
    {
        SvgPathCommand? tail = Tail(); if (tail == null || tail.Kind == SvgPathCommandKind.ClosePath) return; if (tail.Kind == SvgPathCommandKind.LineTo && (Same(tail.EndX, tail.EndY, (current ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).StartX, current.StartY) || Forward(tail.StartX, tail.StartY, tail.EndX, tail.EndY, current.StartX, current.StartY))) { current.Parts.RemoveAt(current.Parts.Count - 1); compacted = compacted + 1; tail = Tail(); }
        if (tail != null) (current ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Parts.Add(command);
    }
    SvgPathCommand? Tail() { return (current ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Parts.Count == 0 ? null : current.Parts[current.Parts.Count - 1]; }
    static SvgPathCommand Line(SvgPathCommand command) { return new SvgPathCommand(SvgPathCommandKind.LineTo, false, new double[] { command.EndX, command.EndY }, command.SourceOffset, command.EndOffset, command.Explicit, command.StartX, command.StartY, command.EndX, command.EndY, command.SubpathStartX, command.SubpathStartY, command.SubpathIndex); }
    static void AddSubpath(SvgGeneratedPathBuilder builder, SvgCleanupSubpath subpath) { builder.MoveTo(subpath.StartX, subpath.StartY); int index = 0; while (index < subpath.Parts.Count) { SvgPathCommand command = subpath.Parts[index]; if (command.Kind == SvgPathCommandKind.LineTo) builder.LineTo(command.EndX, command.EndY); else if (command.Kind == SvgPathCommandKind.QuadraticCurveTo) builder.QuadraticTo(command.Values[0], command.Values[1], command.EndX, command.EndY); else if (command.Kind == SvgPathCommandKind.CubicCurveTo) builder.CubicTo(command.Values[0], command.Values[1], command.Values[2], command.Values[3], command.EndX, command.EndY); else if (command.Kind == SvgPathCommandKind.ArcTo) builder.ArcToWithParameters(command.Values[0], command.Values[1], command.Values[2], command.Values[3] == 1, command.Values[4] == 1, command.EndX, command.EndY); else builder.Close(); index = index + 1; } }
    static bool Same(double firstX, double firstY, double secondX, double secondY) { return firstX == secondX && firstY == secondY; }
    static bool Forward(double startX, double startY, double middleX, double middleY, double endX, double endY) { if (!Collinear(startX, startY, middleX, middleY, endX, endY)) return false; if (System.Math.Abs(endX - startX) >= System.Math.Abs(endY - startY)) return Between(startX, middleX, endX); return Between(startY, middleY, endY); }
    static bool Ordered(double startX, double startY, double firstX, double firstY, double secondX, double secondY, double endX, double endY) { if (System.Math.Abs(endX - startX) >= System.Math.Abs(endY - startY)) return Directional(startX, firstX, secondX, endX); return Directional(startY, firstY, secondY, endY); }
    static bool Between(double start, double middle, double end) { return start <= end ? middle >= start && middle <= end : middle <= start && middle >= end; }
    static bool Directional(double start, double first, double second, double end) { return start <= end ? first <= second : first >= second; }
    static bool Collinear(double startX, double startY, double middleX, double middleY, double endX, double endY) { double firstX = middleX - startX; double firstY = middleY - startY; double secondX = endX - startX; double secondY = endY - startY; if (!SvgFiniteMath.IsFinite(firstX) || !SvgFiniteMath.IsFinite(firstY) || !SvgFiniteMath.IsFinite(secondX) || !SvgFiniteMath.IsFinite(secondY)) return false; double a = firstX * secondY; double b = firstY * secondX; if (!SvgFiniteMath.IsFinite(a) || !SvgFiniteMath.IsFinite(b) || a == 0 && firstX != 0 && secondY != 0 || b == 0 && firstY != 0 && secondX != 0) return false; return a == b; }
}
