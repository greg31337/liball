namespace svg;

internal class SvgStrokePolyline
{
    internal System.Collections.Generic.List<SvgPoint> Points;
    internal bool Closed;
    internal bool HasDrawable;
    internal int SubpathIndex;
    internal SvgStrokePolyline(int index) { Points = new System.Collections.Generic.List<SvgPoint>(); Closed = false; HasDrawable = false; SubpathIndex = index; }
}

internal class SvgStrokePiece
{
    internal System.Collections.Generic.List<SvgPoint> Points;
    internal bool Closed;
    internal int SubpathIndex;
    internal double StartDistance;
    internal double EndDistance;
    internal SvgStrokePiece(int index, double start) { Points = new System.Collections.Generic.List<SvgPoint>(); Closed = false; SubpathIndex = index; StartDistance = start; EndDistance = start; }
}

internal class SvgStrokeFlattenState
{
    internal System.Collections.Generic.List<SvgStrokePolyline> Polylines;
    SvgPathData? path;
    SvgStrokeStyle style;
    SvgStrokePolyline? current;
    int segmentCount;

    internal SvgStrokeFlattenState(SvgPathData? normalized, SvgStrokeStyle strokeStyle) { path = normalized; style = strokeStyle; Polylines = new System.Collections.Generic.List<SvgStrokePolyline>(); current = null; segmentCount = 0; }

    internal SvgStrokeError Run()
    {
        int index = 0;
        while (index < (path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count)
        {
            SvgPathCommand command = path.Commands[index];
            if (command.Kind == SvgPathCommandKind.MoveTo) Start(command);
            else
            {
                if (current == null) StartAt(command);
                SvgFlattenResult flattened = Flatten(command);
                if (!flattened.Success) return Map(flattened.Error);
                segmentCount = segmentCount + flattened.SegmentCount;
                if (segmentCount > style.MaximumFlattenedSegments) return SvgStrokeError.SegmentLimitExceeded;
                int pointIndex = 1;
                while (pointIndex < (flattened.Points ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count) { (current ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Points.Add(new SvgPoint(flattened.Points[pointIndex].Point.X, flattened.Points[pointIndex].Point.Y)); pointIndex = pointIndex + 1; }
                (current ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).HasDrawable = true;
                if (command.Kind == SvgPathCommandKind.ClosePath) { current.Closed = true; Finish(); }
            }
            index = index + 1;
        }
        Finish();
        return SvgStrokeError.None;
    }

    void Start(SvgPathCommand command) { Finish(); current = new SvgStrokePolyline(command.SubpathIndex); current.Points.Add(new SvgPoint(command.EndX, command.EndY)); }
    void StartAt(SvgPathCommand command) { current = new SvgStrokePolyline(command.SubpathIndex); current.Points.Add(new SvgPoint(command.StartX, command.StartY)); }
    void Finish() { if (current != null) Polylines.Add(current); current = null; }

    SvgFlattenResult Flatten(SvgPathCommand command)
    {
        SvgFlattenOptions options = new SvgFlattenOptions(); options.Tolerance = style.Tolerance; options.MaximumSubdivisionDepth = style.MaximumSubdivisionDepth; options.MaximumSegments = style.MaximumFlattenedSegments;
        SvgPoint start = new SvgPoint(command.StartX, command.StartY); SvgPoint end = new SvgPoint(command.EndX, command.EndY);
        if (command.Kind == SvgPathCommandKind.LineTo || command.Kind == SvgPathCommandKind.ClosePath) return new SvgLineSegment(start, end).Flatten(options);
        if (command.Kind == SvgPathCommandKind.QuadraticCurveTo) return new SvgQuadraticBezier(start, new SvgPoint(command.Values[0], command.Values[1]), end).Flatten(options);
        if (command.Kind == SvgPathCommandKind.CubicCurveTo) return new SvgCubicBezier(start, new SvgPoint(command.Values[0], command.Values[1]), new SvgPoint(command.Values[2], command.Values[3]), end).Flatten(options);
        SvgArcOperationResult arc = new SvgArcGeometry().FromCommand(command); if (!arc.Success) return new SvgFlattenResult(false, SvgFlattenError.NonFiniteResult, null, 0); return (arc.Arc ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Flatten(options);
    }

    static SvgStrokeError Map(SvgFlattenError error) { if (error == SvgFlattenError.SegmentLimitExceeded) return SvgStrokeError.SegmentLimitExceeded; if (error == SvgFlattenError.SubdivisionLimitExceeded) return SvgStrokeError.SubdivisionLimitExceeded; if (error == SvgFlattenError.InvalidArgument) return SvgStrokeError.InvalidArgument; return SvgStrokeError.NonFiniteResult; }
}

internal class SvgStrokePatternCursor
{
    internal int Index;
    internal double Remaining;
    internal bool On;
    SvgStrokeDashPatternResult pattern;

    internal SvgStrokePatternCursor(SvgStrokeDashPatternResult dashPattern)
    {
        pattern = dashPattern; Index = 0; Remaining = pattern.IsSolid ? double.MaxValue : (pattern.Pattern ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[0]; On = true;
        if (!pattern.IsSolid) { double offset = pattern.Offset; SkipZero(); while (offset > 0) { if (offset < Remaining) { Remaining = Remaining - offset; offset = 0; } else { offset = offset - Remaining; Advance(); } } }
    }

    internal void Advance() { Index = (Index + 1) % (pattern.Pattern ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count; Remaining = pattern.Pattern[Index]; SkipZero(); On = Index % 2 == 0; }
    void SkipZero() { while (!pattern.IsSolid && Remaining == 0) { Index = (Index + 1) % (pattern.Pattern ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Count; Remaining = pattern.Pattern[Index]; } On = Index % 2 == 0; }
}

internal class SvgStrokeDashState
{
    internal System.Collections.Generic.List<SvgStrokePiece> Pieces;
    internal System.Collections.Generic.List<SvgStrokeDashPlacement> Placements;
    System.Collections.Generic.List<SvgStrokePolyline> polylines;
    SvgStrokeDashPatternResult pattern;
    int maximum;

    internal SvgStrokeDashState(System.Collections.Generic.List<SvgStrokePolyline> lines, SvgStrokeDashPatternResult dashPattern, int maxFragments) { polylines = lines; pattern = dashPattern; maximum = maxFragments; Pieces = new System.Collections.Generic.List<SvgStrokePiece>(); Placements = new System.Collections.Generic.List<SvgStrokeDashPlacement>(); }

    internal SvgStrokeError Run() { int index = 0; while (index < polylines.Count) { SvgStrokeError error = Place(polylines[index]); if (error != SvgStrokeError.None) return error; index = index + 1; } return SvgStrokeError.None; }

    SvgStrokeError Place(SvgStrokePolyline line)
    {
        if (!line.HasDrawable || line.Points.Count == 0) return SvgStrokeError.None;
        double total = Length(line.Points);
        if (!SvgFiniteMath.IsFinite(total)) return SvgStrokeError.NonFiniteResult;
        if (total == 0)
        {
            SvgStrokePatternCursor zeroCursor = new SvgStrokePatternCursor(pattern);
            if (zeroCursor.On) { SvgStrokePiece zero = new SvgStrokePiece(line.SubpathIndex, 0); zero.Points.Add(Copy(line.Points[0])); return Add(zero); }
            return SvgStrokeError.None;
        }
        if (pattern.IsSolid)
        {
            SvgStrokePiece solid = new SvgStrokePiece(line.SubpathIndex, 0); CopyPoints(line.Points, solid.Points); solid.EndDistance = total; solid.Closed = line.Closed; return Add(solid);
        }
        System.Collections.Generic.List<SvgStrokePiece> local = new System.Collections.Generic.List<SvgStrokePiece>();
        SvgStrokePatternCursor cursor = new SvgStrokePatternCursor(pattern);
        SvgStrokePiece? current = null;
        double distance = 0;
        int transitions = 0;
        int edgeIndex = 1;
        while (edgeIndex < line.Points.Count)
        {
            SvgPoint start = line.Points[edgeIndex - 1]; SvgPoint end = line.Points[edgeIndex]; double edgeLength = Distance(start, end);
            if (!SvgFiniteMath.IsFinite(edgeLength)) return SvgStrokeError.NonFiniteResult;
            double edgeDistance = 0;
            while (edgeDistance < edgeLength)
            {
                double available = edgeLength - edgeDistance; double step = System.Math.Min(available, cursor.Remaining);
                if (step <= 0 || edgeDistance + step == edgeDistance) return SvgStrokeError.DashLimitExceeded;
                SvgPoint? first = Interpolate(start, end, edgeDistance / edgeLength); SvgPoint? second = Interpolate(start, end, (edgeDistance + step) / edgeLength);
                if (first == null || second == null) return SvgStrokeError.NonFiniteResult;
                if (cursor.On)
                {
                    if (current == null) { current = new SvgStrokePiece(line.SubpathIndex, distance); current.Points.Add(first); }
                    AddPoint(current.Points, second);
                }
                edgeDistance = edgeDistance + step; distance = distance + step; cursor.Remaining = cursor.Remaining - step;
                if (cursor.Remaining == 0)
                {
                    transitions = transitions + 1; if (transitions > maximum) return SvgStrokeError.DashLimitExceeded;
                    bool wasOn = cursor.On; cursor.Advance();
                    if (wasOn && !cursor.On && current != null) { if (Pieces.Count + local.Count >= maximum) return SvgStrokeError.DashLimitExceeded; current.EndDistance = distance; local.Add(current); current = null; }
                }
            }
            edgeIndex = edgeIndex + 1;
        }
        if (current != null) { current.EndDistance = total; local.Add(current); }
        MergeClosedSeam(line, local, total);
        int localIndex = 0; while (localIndex < local.Count) { SvgStrokeError error = Add(local[localIndex]); if (error != SvgStrokeError.None) return error; localIndex = localIndex + 1; }
        return SvgStrokeError.None;
    }

    void MergeClosedSeam(SvgStrokePolyline line, System.Collections.Generic.List<SvgStrokePiece> local, double total)
    {
        if (!line.Closed || local.Count == 0) return;
        SvgStrokePiece first = local[0]; SvgStrokePiece last = local[local.Count - 1]; SvgPoint seam = line.Points[0];
        bool begins = first.StartDistance == 0 && Same(first.Points[0], seam); bool ends = last.EndDistance == total && Same(last.Points[last.Points.Count - 1], seam);
        if (local.Count == 1 && begins && ends) { first.Closed = true; return; }
        if (!begins || !ends) return;
        SvgStrokePiece merged = new SvgStrokePiece(line.SubpathIndex, last.StartDistance); CopyPoints(last.Points, merged.Points); int index = 1; while (index < first.Points.Count) { AddPoint(merged.Points, first.Points[index]); index = index + 1; } merged.EndDistance = total + first.EndDistance;
        local.RemoveAt(local.Count - 1); local.RemoveAt(0); local.Add(merged);
    }

    SvgStrokeError Add(SvgStrokePiece piece) { if (Pieces.Count >= maximum) return SvgStrokeError.DashLimitExceeded; Pieces.Add(piece); Placements.Add(new SvgStrokeDashPlacement(piece.SubpathIndex, piece.StartDistance, piece.EndDistance, piece.Closed)); return SvgStrokeError.None; }
    static double Length(System.Collections.Generic.List<SvgPoint> points) { double total = 0; int i = 1; while (i < points.Count) { total = total + Distance(points[i - 1], points[i]); if (!SvgFiniteMath.IsFinite(total)) return double.PositiveInfinity; i = i + 1; } return total; }
    static double Distance(SvgPoint a, SvgPoint b) { double x = b.X - a.X; double y = b.Y - a.Y; double maximum = System.Math.Max(System.Math.Abs(x), System.Math.Abs(y)); if (!SvgFiniteMath.IsFinite(maximum) || maximum == 0) return maximum; double ratio = System.Math.Min(System.Math.Abs(x), System.Math.Abs(y)) / maximum; return maximum * System.Math.Sqrt(1 + ratio * ratio); }
    static SvgPoint? Interpolate(SvgPoint a, SvgPoint b, double amount) { SvgFiniteResult x = SvgFiniteMath.Lerp(a.X, b.X, amount); SvgFiniteResult y = SvgFiniteMath.Lerp(a.Y, b.Y, amount); return x.Success && y.Success ? new SvgPoint(x.Value, y.Value) : null; }
    static SvgPoint Copy(SvgPoint p) { return new SvgPoint(p.X, p.Y); }
    static void CopyPoints(System.Collections.Generic.List<SvgPoint> source, System.Collections.Generic.List<SvgPoint> target) { int i = 0; while (i < source.Count) { target.Add(Copy(source[i])); i = i + 1; } }
    static void AddPoint(System.Collections.Generic.List<SvgPoint> points, SvgPoint point) { if (points.Count == 0 || !Same(points[points.Count - 1], point)) points.Add(point); }
    static bool Same(SvgPoint a, SvgPoint b) { return a.X == b.X && a.Y == b.Y; }
}

internal class SvgStrokeDirection
{
    internal double X; internal double Y; internal SvgStrokeDirection(double x, double y) { X = x; Y = y; }
}

internal class SvgStrokeOutlineState
{
    internal SvgGeneratedPathBuilder Builder;
    System.Collections.Generic.List<SvgStrokePiece> pieces;
    SvgStrokeStyle style;
    double radius;

    internal SvgStrokeOutlineState(System.Collections.Generic.List<SvgStrokePiece> strokePieces, SvgStrokeStyle strokeStyle) { pieces = strokePieces; style = strokeStyle; radius = style.Width / 2; Builder = new SvgGeneratedPathBuilder(); }
    internal SvgStrokeError Run() { if (radius == 0) return SvgStrokeError.None; int i = 0; while (i < pieces.Count) { SvgStrokeError error = Outline(pieces[i]); if (error != SvgStrokeError.None) return error; i = i + 1; } return SvgStrokeError.None; }

    SvgStrokeError Outline(SvgStrokePiece piece)
    {
        System.Collections.Generic.List<SvgPoint> points = Clean(piece.Points, piece.Closed);
        if (points.Count < 2) return Zero(points.Count == 0 ? null : points[0]);
        int edgeCount = piece.Closed ? points.Count : points.Count - 1;
        int i = 0;
        while (i < edgeCount)
        {
            SvgPoint a = points[i]; SvgPoint b = points[(i + 1) % points.Count]; SvgStrokeDirection? d = Direction(a, b); if (d == null) return SvgStrokeError.NonFiniteResult;
            SvgStrokeError rectangle = Rectangle(a, b, d); if (rectangle != SvgStrokeError.None) return rectangle; i = i + 1;
        }
        int joinStart = piece.Closed ? 0 : 1; int joinEnd = piece.Closed ? points.Count : points.Count - 1; i = joinStart;
        while (i < joinEnd)
        {
            SvgPoint before = points[(i - 1 + points.Count) % points.Count]; SvgPoint vertex = points[i]; SvgPoint after = points[(i + 1) % points.Count]; SvgStrokeError join = Join(before, vertex, after); if (join != SvgStrokeError.None) return join; i = i + 1;
        }
        if (!piece.Closed)
        {
            SvgStrokeDirection? first = Direction(points[0], points[1]); SvgStrokeDirection? last = Direction(points[points.Count - 2], points[points.Count - 1]);
            SvgStrokeError startCap = Cap(points[0], first, true); if (startCap != SvgStrokeError.None) return startCap; SvgStrokeError endCap = Cap(points[points.Count - 1], last, false); if (endCap != SvgStrokeError.None) return endCap;
        }
        return SvgStrokeError.None;
    }

    SvgStrokeError Zero(SvgPoint? point)
    {
        if (point == null || style.LineCap == SvgStrokeLineCap.Butt) return SvgStrokeError.None;
        if (style.LineCap == SvgStrokeLineCap.Round) return Circle(point);
        System.Collections.Generic.List<SvgPoint> square = new System.Collections.Generic.List<SvgPoint>(); square.Add((Offset(point, -radius, -radius) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); square.Add((Offset(point, radius, -radius) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); square.Add((Offset(point, radius, radius) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); square.Add((Offset(point, -radius, radius) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); return Polygon(square);
    }

    SvgStrokeError Rectangle(SvgPoint a, SvgPoint b, SvgStrokeDirection d)
    {
        double nx = -d.Y * radius; double ny = d.X * radius; System.Collections.Generic.List<SvgPoint> polygon = new System.Collections.Generic.List<SvgPoint>(); polygon.Add((Offset(a, nx, ny) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); polygon.Add((Offset(a, -nx, -ny) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); polygon.Add((Offset(b, -nx, -ny) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); polygon.Add((Offset(b, nx, ny) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); return Polygon(polygon);
    }

    SvgStrokeError Cap(SvgPoint point, SvgStrokeDirection? direction, bool start)
    {
        if (style.LineCap == SvgStrokeLineCap.Butt) return SvgStrokeError.None;
        if (style.LineCap == SvgStrokeLineCap.Square)
        {
            double sign = start ? -1 : 1; double ex = (direction ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X * radius * sign; double ey = direction.Y * radius * sign; double nx = -direction.Y * radius; double ny = direction.X * radius;
            System.Collections.Generic.List<SvgPoint> polygon = new System.Collections.Generic.List<SvgPoint>(); polygon.Add((Offset(point, nx, ny) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); polygon.Add((Offset(point, -nx, -ny) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); polygon.Add((Offset(point, -nx + ex, -ny + ey) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); polygon.Add((Offset(point, nx + ex, ny + ey) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); return Polygon(polygon);
        }
        double normalAngle = System.Math.Atan2((direction ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X, -direction.Y); double startAngle = start ? normalAngle : normalAngle + System.Math.PI; return Fan(point, startAngle, System.Math.PI);
    }

    SvgStrokeError Join(SvgPoint before, SvgPoint vertex, SvgPoint after)
    {
        SvgStrokeDirection? first = Direction(before, vertex); SvgStrokeDirection? second = Direction(vertex, after); if (first == null || second == null) return SvgStrokeError.NonFiniteResult;
        double cross = first.X * second.Y - first.Y * second.X; double dot = first.X * second.X + first.Y * second.Y; if (cross == 0 && dot > 0) return SvgStrokeError.None;
        if (cross == 0) return style.LineJoin == SvgStrokeLineJoin.Round || style.LineJoin == SvgStrokeLineJoin.Arcs ? Circle(vertex) : ZeroSquare(vertex);
        double side = cross > 0 ? -1 : 1; SvgPoint? outerFirst = Offset(vertex, -first.Y * radius * side, first.X * radius * side); SvgPoint? outerSecond = Offset(vertex, -second.Y * radius * side, second.X * radius * side);
        if (outerFirst == null || outerSecond == null) return SvgStrokeError.NonFiniteResult;
        if (style.LineJoin == SvgStrokeLineJoin.Round)
        {
            double firstAngle = System.Math.Atan2(outerFirst.Y - vertex.Y, outerFirst.X - vertex.X); double secondAngle = System.Math.Atan2(outerSecond.Y - vertex.Y, outerSecond.X - vertex.X); double sweep = PositiveSweep(firstAngle, secondAngle);
            if (cross < 0) { double temporary = firstAngle; firstAngle = secondAngle; secondAngle = temporary; sweep = PositiveSweep(firstAngle, secondAngle); }
            return Fan(vertex, firstAngle, sweep);
        }
        if (style.LineJoin == SvgStrokeLineJoin.Bevel) return JoinPolygon(vertex, outerFirst, null, null, outerSecond, cross);
        SvgPoint? intersection = Intersection(outerFirst, first, outerSecond, second); if (intersection == null) return JoinPolygon(vertex, outerFirst, null, null, outerSecond, cross);
        double ratio = Distance(vertex, intersection) / radius;
        if (ratio <= style.MiterLimit) return JoinPolygon(vertex, outerFirst, intersection, null, outerSecond, cross);
        if (style.LineJoin == SvgStrokeLineJoin.Miter) return JoinPolygon(vertex, outerFirst, null, null, outerSecond, cross);
        SvgPoint? firstClip = Clip(vertex, outerFirst, first, intersection); SvgPoint? secondClip = Clip(vertex, outerSecond, second, intersection); if (firstClip == null || secondClip == null) return SvgStrokeError.NonFiniteResult;
        return JoinPolygon(vertex, outerFirst, firstClip, secondClip, outerSecond, cross);
    }

    SvgStrokeError JoinPolygon(SvgPoint center, SvgPoint first, SvgPoint? middle1, SvgPoint? middle2, SvgPoint second, double cross)
    {
        System.Collections.Generic.List<SvgPoint> polygon = new System.Collections.Generic.List<SvgPoint>(); polygon.Add(center);
        if (cross > 0) { polygon.Add(first); if (middle1 != null) polygon.Add(middle1); if (middle2 != null) polygon.Add(middle2); polygon.Add(second); }
        else { polygon.Add(second); if (middle2 != null) polygon.Add(middle2); if (middle1 != null) polygon.Add(middle1); polygon.Add(first); }
        return Polygon(polygon);
    }

    SvgPoint? Clip(SvgPoint vertex, SvgPoint outer, SvgStrokeDirection direction, SvgPoint miter)
    {
        SvgStrokeDirection? axis = Direction(vertex, miter); if (axis == null) return null; double denominator = direction.X * axis.X + direction.Y * axis.Y; if (denominator == 0) return outer;
        double ox = outer.X - vertex.X; double oy = outer.Y - vertex.Y; double amount = (style.MiterLimit * radius - ox * axis.X - oy * axis.Y) / denominator; return Offset(outer, direction.X * amount, direction.Y * amount);
    }

    SvgStrokeError Circle(SvgPoint center) { return Fan(center, 0, System.Math.PI * 2); }
    SvgStrokeError ZeroSquare(SvgPoint point) { System.Collections.Generic.List<SvgPoint> p = new System.Collections.Generic.List<SvgPoint>(); p.Add((Offset(point, -radius, -radius) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); p.Add((Offset(point, radius, -radius) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); p.Add((Offset(point, radius, radius) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); p.Add((Offset(point, -radius, radius) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); return Polygon(p); }

    SvgStrokeError Fan(SvgPoint center, double startAngle, double sweep)
    {
        int count = ArcCount(System.Math.Abs(sweep)); if (count < 0) return SvgStrokeError.OutputLimitExceeded; if (count < 1) count = 1; System.Collections.Generic.List<SvgPoint> polygon = new System.Collections.Generic.List<SvgPoint>(); polygon.Add(center);
        double previous = startAngle; polygon.Add((Offset(center, System.Math.Cos(previous) * radius, System.Math.Sin(previous) * radius) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))); int i = 1;
        while (i <= count)
        {
            double angle = startAngle + sweep * i / count;
            double quadrantStep = System.Math.PI / 2;
            int quadrant = (int)System.Math.Floor(previous / quadrantStep) + 1;
            double quadrantAngle = quadrant * quadrantStep;
            while (quadrantAngle < angle)
            {
                polygon.Add((Offset(center, System.Math.Cos(quadrantAngle) * radius, System.Math.Sin(quadrantAngle) * radius) ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
                quadrant = quadrant + 1;
                quadrantAngle = quadrant * quadrantStep;
            }
            polygon.Add((Offset(center, System.Math.Cos(angle) * radius, System.Math.Sin(angle) * radius) ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            previous = angle;
            i = i + 1;
        }
        return Polygon(polygon);
    }

    int ArcCount(double sweep)
    {
        double count;
        if (style.Tolerance >= radius) count = System.Math.Ceiling(sweep / (System.Math.PI / 2));
        else
        {
            double ratio = style.Tolerance / radius;
            if (1 - ratio == 1) return -1;
            double step = 2 * System.Math.Acos(1 - ratio);
            if (!SvgFiniteMath.IsFinite(step) || step <= 0) return -1;
            count = System.Math.Ceiling(sweep / step);
        }
        if (!SvgFiniteMath.IsFinite(count) || count > style.MaximumOutputCommands) return -1;
        return (int)count;
    }

    SvgStrokeError Polygon(System.Collections.Generic.List<SvgPoint> points)
    {
        int i = 0; while (i < points.Count) { if (points[i] == null) return SvgStrokeError.NonFiniteResult; i = i + 1; }
        if (points.Count < 3) return SvgStrokeError.None; if (Builder.Commands.Count >= style.MaximumOutputCommands || points.Count > style.MaximumOutputCommands - Builder.Commands.Count - 1) return SvgStrokeError.OutputLimitExceeded;
        Builder.MoveTo(points[0].X, points[0].Y); i = 1; while (i < points.Count) { Builder.LineTo(points[i].X, points[i].Y); i = i + 1; } Builder.Close(); return SvgStrokeError.None;
    }

    static System.Collections.Generic.List<SvgPoint> Clean(System.Collections.Generic.List<SvgPoint> source, bool closed) { System.Collections.Generic.List<SvgPoint> result = new System.Collections.Generic.List<SvgPoint>(); int i = 0; while (i < source.Count) { if (result.Count == 0 || !Same(result[result.Count - 1], source[i])) result.Add(new SvgPoint(source[i].X, source[i].Y)); i = i + 1; } if (closed && result.Count > 1 && Same(result[0], result[result.Count - 1])) result.RemoveAt(result.Count - 1); return result; }
    static SvgStrokeDirection? Direction(SvgPoint a, SvgPoint b) { double x = b.X - a.X; double y = b.Y - a.Y; double length = Distance(a, b); return length > 0 && SvgFiniteMath.IsFinite(length) ? new SvgStrokeDirection(x / length, y / length) : null; }
    static double Distance(SvgPoint a, SvgPoint b) { double x = b.X - a.X; double y = b.Y - a.Y; double maximum = System.Math.Max(System.Math.Abs(x), System.Math.Abs(y)); if (!SvgFiniteMath.IsFinite(maximum) || maximum == 0) return maximum; double ratio = System.Math.Min(System.Math.Abs(x), System.Math.Abs(y)) / maximum; return maximum * System.Math.Sqrt(1 + ratio * ratio); }
    static SvgPoint? Offset(SvgPoint point, double x, double y) { SvgFiniteResult px = SvgFiniteMath.Add(point.X, x); SvgFiniteResult py = SvgFiniteMath.Add(point.Y, y); return px.Success && py.Success ? new SvgPoint(px.Value, py.Value) : null; }
    static SvgPoint? Intersection(SvgPoint first, SvgStrokeDirection firstDirection, SvgPoint second, SvgStrokeDirection secondDirection) { double denominator = firstDirection.X * secondDirection.Y - firstDirection.Y * secondDirection.X; if (denominator == 0) return null; double x = second.X - first.X; double y = second.Y - first.Y; double amount = (x * secondDirection.Y - y * secondDirection.X) / denominator; return Offset(first, firstDirection.X * amount, firstDirection.Y * amount); }
    static double PositiveSweep(double first, double second) { double sweep = second - first; while (sweep < 0) sweep = sweep + System.Math.PI * 2; while (sweep > System.Math.PI * 2) sweep = sweep - System.Math.PI * 2; return sweep; }
    static bool Same(SvgPoint a, SvgPoint b) { return a.X == b.X && a.Y == b.Y; }
}
