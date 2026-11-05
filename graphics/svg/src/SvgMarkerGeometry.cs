namespace svg;

internal class SvgMarkerInstanceResult { internal SvgMarkerGeometryError Error; internal SvgMarkerInstance? Instance; internal SvgMarkerInstanceResult(SvgMarkerGeometryError error, SvgMarkerInstance? instance) { Error = error; Instance = instance; } }

internal class SvgMarkerSegment { internal SvgPoint Start; internal SvgPoint End; internal SvgVector? StartTangent; internal SvgVector? EndTangent; internal SvgMarkerSegment(SvgPoint start, SvgPoint end, SvgVector? startTangent, SvgVector? endTangent) { Start = start; End = end; StartTangent = startTangent; EndTangent = endTangent; } }

internal class SvgMarkerTangentResult { internal bool Success; internal SvgVector? Tangent; internal SvgMarkerTangentResult(bool success, SvgVector? tangent) { Success = success; Tangent = tangent; } }

internal class SvgMarkerExtractState
{
    internal System.Collections.Generic.List<SvgMarkerVertex> Vertices; SvgPathData? path; int maximum; System.Collections.Generic.List<SvgMarkerSegment> segments; bool closed; int subpathIndex;
    internal SvgMarkerExtractState(SvgPathData? normalized, int limit) { path = normalized; maximum = limit; Vertices = new System.Collections.Generic.List<SvgMarkerVertex>(); segments = new System.Collections.Generic.List<SvgMarkerSegment>(); closed = false; subpathIndex = 0; }
    internal SvgMarkerGeometryError Run()
    {
        int index = 0; bool active = false;
        while (index < (path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count)
        {
            SvgPathCommand command = path.Commands[index];
            if (command.Kind == SvgPathCommandKind.MoveTo) { if (active) { SvgMarkerGeometryError finish = Finish(); if (finish != SvgMarkerGeometryError.None) return finish; } active = true; subpathIndex = command.SubpathIndex; }
            else { if (!active) { active = true; subpathIndex = command.SubpathIndex; } SvgMarkerSegment? segment = Segment(command); if (segment == null) return SvgMarkerGeometryError.NonFiniteResult; segments.Add(segment); if (command.Kind == SvgPathCommandKind.ClosePath) { closed = true; SvgMarkerGeometryError finish = Finish(); if (finish != SvgMarkerGeometryError.None) return finish; active = false; } }
            index = index + 1;
        }
        return active ? Finish() : SvgMarkerGeometryError.None;
    }

    SvgMarkerGeometryError Finish()
    {
        if (segments.Count == 0) { Reset(); return SvgMarkerGeometryError.None; }
        int required = closed ? segments.Count + 1 : segments.Count + 1; if (Vertices.Count > maximum - required) return SvgMarkerGeometryError.VertexLimitExceeded;
        SvgPoint start = segments[0].Start; SvgVector? startIncoming = closed ? SearchIncoming(segments.Count - 1) : Zero(); SvgVector? startOutgoing = SearchOutgoing(0); Vertices.Add(Vertex(start, startIncoming, startOutgoing, SvgMarkerPlacement.Start, 0));
        int index = 1; while (index < segments.Count) { SvgVector? incoming = SearchIncoming(index - 1); SvgVector? outgoing = SearchOutgoing(index); Vertices.Add(Vertex(segments[index - 1].End, incoming, outgoing, SvgMarkerPlacement.Mid, index)); index = index + 1; }
        SvgPoint end = closed ? start : segments[segments.Count - 1].End; SvgVector? endIncoming = SearchIncoming(segments.Count - 1); SvgVector? endOutgoing = closed ? SearchOutgoing(0) : Zero(); Vertices.Add(Vertex(end, endIncoming, endOutgoing, SvgMarkerPlacement.End, closed ? 0 : segments.Count)); Reset(); return SvgMarkerGeometryError.None;
    }

    SvgMarkerVertex Vertex(SvgPoint point, SvgVector? incoming, SvgVector? outgoing, SvgMarkerPlacement placement, int vertexIndex)
    {
        SvgVector? direction = placement == SvgMarkerPlacement.Start ? Prefer(outgoing, incoming) : placement == SvgMarkerPlacement.End ? Prefer(incoming, outgoing) : Bisect((incoming ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), (outgoing ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); double angle = (direction ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X == 0 && direction.Y == 0 ? 0 : System.Math.Atan2(direction.Y, direction.X) * 180 / System.Math.PI;
        return new SvgMarkerVertex(new SvgPoint(point.X, point.Y), (incoming ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), (outgoing ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")), SvgFiniteMath.NormalizeZero(angle), placement, subpathIndex, vertexIndex);
    }

    SvgVector? SearchIncoming(int start) { int count = 0; int index = start; while (count < segments.Count) { SvgVector? value = segments[index].EndTangent; if (!Zero(value)) return value; index = index - 1; if (index < 0) { if (!closed) return Zero(); index = segments.Count - 1; } count = count + 1; } return Zero(); }
    SvgVector? SearchOutgoing(int start) { int count = 0; int index = start; while (count < segments.Count) { SvgVector? value = segments[index].StartTangent; if (!Zero(value)) return value; index = index + 1; if (index >= segments.Count) { if (!closed) return Zero(); index = 0; } count = count + 1; } return Zero(); }
    SvgMarkerSegment? Segment(SvgPathCommand command)
    {
        SvgPoint start = new SvgPoint(command.StartX, command.StartY); SvgPoint end = new SvgPoint(command.EndX, command.EndY); SvgVector first; SvgVector last;
        if (command.Kind == SvgPathCommandKind.LineTo || command.Kind == SvgPathCommandKind.ClosePath) { SvgMarkerTangentResult? value = Between(end, start); if (!value.Success) return null; first = (value.Tangent ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")); last = (first ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")); }
        else if (command.Kind == SvgPathCommandKind.QuadraticCurveTo)
        {
            SvgPoint c = new SvgPoint(command.Values[0], command.Values[1]); SvgMarkerTangentResult firstControl = Between(c, start); SvgMarkerTangentResult chord = Between(end, start); SvgMarkerTangentResult? lastControl = Between(end, c); if (!firstControl.Success || !chord.Success || !lastControl.Success) return null; first = (Prefer(firstControl.Tangent, chord.Tangent) ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")); last = (Prefer(lastControl.Tangent, chord.Tangent) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        }
        else if (command.Kind == SvgPathCommandKind.CubicCurveTo)
        {
            SvgPoint c1 = new SvgPoint(command.Values[0], command.Values[1]); SvgPoint c2 = new SvgPoint(command.Values[2], command.Values[3]); SvgMarkerTangentResult firstControl = Between(c1, start); SvgMarkerTangentResult secondControl = Between(c2, start); SvgMarkerTangentResult chord = Between(end, start); SvgMarkerTangentResult lastControl = Between(end, c2); SvgMarkerTangentResult? penultimate = Between(end, c1); if (!firstControl.Success || !secondControl.Success || !chord.Success || !lastControl.Success || !penultimate.Success) return null; first = (Prefer(firstControl.Tangent, Prefer(secondControl.Tangent, chord.Tangent)) ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")); last = (Prefer(lastControl.Tangent, Prefer(penultimate.Tangent, chord.Tangent)) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        }
        else { SvgArcOperationResult arc = new SvgArcGeometry().FromCommand(command); if (!arc.Success) return null; SvgVectorOperationResult startDerivative = (arc.Arc ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Derivative(0); SvgVectorOperationResult endDerivative = arc.Arc.Derivative(1); if (!startDerivative.Success || !endDerivative.Success) return null; first = Unit((startDerivative.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, startDerivative.Vector.Y); last = Unit((endDerivative.Vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, endDerivative.Vector.Y); }
        return new SvgMarkerSegment(start, end, first, last);
    }

    void Reset() { segments = new System.Collections.Generic.List<SvgMarkerSegment>(); closed = false; }
    static SvgVector Bisect(SvgVector incoming, SvgVector outgoing) { if (Zero(incoming)) return outgoing; if (Zero(outgoing)) return incoming; double x = incoming.X + outgoing.X; double y = incoming.Y + outgoing.Y; if (x == 0 && y == 0) { double first = System.Math.Atan2(incoming.Y, incoming.X); double second = System.Math.Atan2(outgoing.Y, outgoing.X); double difference = second - first; while (difference <= -System.Math.PI) difference = difference + 2 * System.Math.PI; while (difference > System.Math.PI) difference = difference - 2 * System.Math.PI; return Unit(System.Math.Cos(first + difference / 2), System.Math.Sin(first + difference / 2)); } return Unit(x, y); }
    static SvgVector? Prefer(SvgVector? first, SvgVector? second) { return Zero(first) ? second : first; }
    static SvgMarkerTangentResult Between(SvgPoint first, SvgPoint second) { SvgFiniteResult x = SvgFiniteMath.Subtract(first.X, second.X); SvgFiniteResult y = SvgFiniteMath.Subtract(first.Y, second.Y); return x.Success && y.Success ? new SvgMarkerTangentResult(true, Unit(x.Value, y.Value)) : new SvgMarkerTangentResult(false, null); }
    static SvgVector Unit(double x, double y) { double maximum = System.Math.Max(System.Math.Abs(x), System.Math.Abs(y)); if (!SvgFiniteMath.IsFinite(maximum) || maximum == 0) return Zero(); double ratio = System.Math.Min(System.Math.Abs(x), System.Math.Abs(y)) / maximum; double length = maximum * System.Math.Sqrt(1 + ratio * ratio); return SvgFiniteMath.IsFinite(length) && length > 0 ? new SvgVector(x / length, y / length) : Zero(); }
    static bool Zero(SvgVector? vector) { return (vector ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X == 0 && vector.Y == 0; }
    static SvgVector Zero() { return new SvgVector(0, 0); }
}
