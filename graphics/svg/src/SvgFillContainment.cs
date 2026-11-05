namespace svg;

internal class SvgFillEdge
{
    internal double StartX;
    internal double StartY;
    internal double EndX;
    internal double EndY;

    internal SvgFillEdge(SvgPoint? start, SvgPoint? end)
    {
        StartX = (start ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X;
        StartY = start.Y;
        EndX = (end ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X;
        EndY = end.Y;
    }
}

internal class SvgFillContainmentState
{
    SvgPathData? path;
    SvgPoint query;
    SvgFillRule fillRule;
    SvgFillContainmentOptions options;
    System.Collections.Generic.List<SvgFillEdge> edges;
    SvgPoint? subpathStart;
    SvgPoint? subpathEnd;
    bool hasSubpath;
    bool hasDrawable;

    internal SvgFillContainmentState(SvgPathData? normalizedPath, SvgPoint point, SvgFillRule rule, SvgFillContainmentOptions containmentOptions)
    {
        path = normalizedPath;
        query = point;
        fillRule = rule;
        options = containmentOptions;
        edges = new System.Collections.Generic.List<SvgFillEdge>();
        subpathStart = null;
        subpathEnd = null;
        hasSubpath = false;
        hasDrawable = false;
    }

    internal SvgFillContainmentResult Run()
    {
        int index = 0;
        while (index < (path ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Commands.Count)
        {
            SvgFillContainmentError error = AddCommand(path.Commands[index]);
            if (error != SvgFillContainmentError.None) return new SvgFillContainment().Failure(error);
            index = index + 1;
        }
        SvgFillContainmentError closingError = CloseOpenSubpath();
        if (closingError != SvgFillContainmentError.None) return new SvgFillContainment().Failure(closingError);
        index = 0;
        while (index < edges.Count)
        {
            if (OnBoundary(edges[index])) return new SvgFillContainmentResult(true, SvgFillContainmentError.None, SvgFillContainmentKind.Boundary, true, 0, 0);
            index = index + 1;
        }
        return ClassifyCrossings();
    }

    SvgFillContainmentError AddCommand(SvgPathCommand command)
    {
        if (command.Kind == SvgPathCommandKind.MoveTo)
        {
            SvgFillContainmentError closingError = CloseOpenSubpath();
            if (closingError != SvgFillContainmentError.None) return closingError;
            SvgPointTransformResult move = Transform(new SvgPoint(command.EndX, command.EndY));
            if (!move.Success) return Map(move.Error);
            subpathStart = move.Point;
            subpathEnd = move.Point;
            hasSubpath = true;
            hasDrawable = false;
            return SvgFillContainmentError.None;
        }
        SvgFlattenResult flattened = Flatten(command);
        if (!flattened.Success) return Map(flattened.Error);
        SvgPointTransformResult first = Transform((flattened.Points ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[0].Point);
        if (!first.Success) return Map(first.Error);
        int pointIndex = 1;
        while (pointIndex < flattened.Points.Count)
        {
            SvgPointTransformResult next = Transform(flattened.Points[pointIndex].Point);
            if (!next.Success) return Map(next.Error);
            SvgFillContainmentError edgeError = AddEdge(first.Point, next.Point);
            if (edgeError != SvgFillContainmentError.None) return edgeError;
            first = next;
            pointIndex = pointIndex + 1;
        }
        subpathEnd = first.Point;
        hasDrawable = true;
        return SvgFillContainmentError.None;
    }

    SvgFlattenResult Flatten(SvgPathCommand command)
    {
        SvgFlattenOptions flattenOptions = new SvgFlattenOptions();
        flattenOptions.Tolerance = options.Tolerance;
        flattenOptions.DeviceTransform = options.Transform;
        flattenOptions.MaximumSubdivisionDepth = options.MaximumSubdivisionDepth;
        int remaining = options.MaximumSegments - edges.Count;
        flattenOptions.MaximumSegments = remaining > 0 ? remaining : 1;
        SvgPoint start = new SvgPoint(command.StartX, command.StartY);
        SvgPoint end = new SvgPoint(command.EndX, command.EndY);
        if (command.Kind == SvgPathCommandKind.LineTo || command.Kind == SvgPathCommandKind.ClosePath) return new SvgLineSegment(start, end).Flatten(flattenOptions);
        if (command.Kind == SvgPathCommandKind.QuadraticCurveTo) return new SvgQuadraticBezier(start, new SvgPoint(command.Values[0], command.Values[1]), end).Flatten(flattenOptions);
        if (command.Kind == SvgPathCommandKind.CubicCurveTo) return new SvgCubicBezier(start, new SvgPoint(command.Values[0], command.Values[1]), new SvgPoint(command.Values[2], command.Values[3]), end).Flatten(flattenOptions);
        if (command.Kind == SvgPathCommandKind.ArcTo)
        {
            SvgArcOperationResult arc = new SvgArcGeometry().FromCommand(command);
            if (!arc.Success) return new SvgFlattenResult(false, SvgFlattenError.NonFiniteResult, null, 0);
            return (arc.Arc ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Flatten(flattenOptions);
        }
        return new SvgFlattenResult(false, SvgFlattenError.InvalidArgument, null, 0);
    }

    SvgFillContainmentError CloseOpenSubpath()
    {
        if (!hasSubpath || !hasDrawable) return SvgFillContainmentError.None;
        hasDrawable = false;
        if ((subpathEnd ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X == (subpathStart ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).X && subpathEnd.Y == subpathStart.Y) return SvgFillContainmentError.None;
        SvgFillContainmentError error = AddEdge(subpathEnd, subpathStart);
        if (error == SvgFillContainmentError.None) subpathEnd = subpathStart;
        return error;
    }

    SvgFillContainmentError AddEdge(SvgPoint? start, SvgPoint? end)
    {
        if (edges.Count >= options.MaximumSegments) return SvgFillContainmentError.SegmentLimitExceeded;
        edges.Add(new SvgFillEdge(start, end));
        return SvgFillContainmentError.None;
    }

    SvgPointTransformResult Transform(SvgPoint point)
    {
        if (options.Transform == null) return new SvgPointTransformResult(true, SvgGeometryError.None, point);
        return options.Transform.TransformPoint(point);
    }

    bool OnBoundary(SvgFillEdge edge)
    {
        double scale = Scale(edge);
        if (scale == 0) return true;
        double startX = edge.StartX / scale;
        double startY = edge.StartY / scale;
        double endX = edge.EndX / scale;
        double endY = edge.EndY / scale;
        double pointX = query.X / scale;
        double pointY = query.Y / scale;
        double dx = endX - startX;
        double dy = endY - startY;
        double px = pointX - startX;
        double py = pointY - startY;
        double lengthSquared = dx * dx + dy * dy;
        double amount = lengthSquared == 0 ? 0 : (px * dx + py * dy) / lengthSquared;
        if (amount < 0) amount = 0;
        else if (amount > 1) amount = 1;
        double closestX = startX + amount * dx;
        double closestY = startY + amount * dy;
        double distance = Hypotenuse(pointX - closestX, pointY - closestY);
        return distance <= options.Tolerance / scale;
    }

    SvgFillContainmentResult ClassifyCrossings()
    {
        int winding = 0;
        int crossings = 0;
        int index = 0;
        while (index < edges.Count)
        {
            SvgFillEdge edge = edges[index];
            double scale = Scale(edge);
            if (scale > 0)
            {
                double startX = edge.StartX / scale;
                double startY = edge.StartY / scale;
                double endX = edge.EndX / scale;
                double endY = edge.EndY / scale;
                double pointX = query.X / scale;
                double pointY = query.Y / scale;
                double side = (endX - startX) * (pointY - startY) - (pointX - startX) * (endY - startY);
                if (startY <= pointY && endY > pointY && side > 0)
                {
                    winding = winding + 1;
                    crossings = crossings + 1;
                }
                else if (endY <= pointY && startY > pointY && side < 0)
                {
                    winding = winding - 1;
                    crossings = crossings + 1;
                }
            }
            index = index + 1;
        }
        bool inside = fillRule == SvgFillRule.Nonzero ? winding != 0 : crossings % 2 != 0;
        SvgFillContainmentKind kind = inside ? SvgFillContainmentKind.Inside : SvgFillContainmentKind.Outside;
        return new SvgFillContainmentResult(true, SvgFillContainmentError.None, kind, inside, winding, crossings);
    }

    double Scale(SvgFillEdge edge)
    {
        double scale = System.Math.Max(System.Math.Abs(edge.StartX), System.Math.Abs(edge.StartY));
        scale = System.Math.Max(scale, System.Math.Abs(edge.EndX));
        scale = System.Math.Max(scale, System.Math.Abs(edge.EndY));
        scale = System.Math.Max(scale, System.Math.Abs(query.X));
        return System.Math.Max(scale, System.Math.Abs(query.Y));
    }

    static double Hypotenuse(double x, double y)
    {
        double absoluteX = System.Math.Abs(x);
        double absoluteY = System.Math.Abs(y);
        double maximum = System.Math.Max(absoluteX, absoluteY);
        if (maximum == 0) return 0;
        double ratio = System.Math.Min(absoluteX, absoluteY) / maximum;
        return maximum * System.Math.Sqrt(1 + ratio * ratio);
    }

    static SvgFillContainmentError Map(SvgFlattenError error)
    {
        if (error == SvgFlattenError.SegmentLimitExceeded) return SvgFillContainmentError.SegmentLimitExceeded;
        if (error == SvgFlattenError.SubdivisionLimitExceeded) return SvgFillContainmentError.SubdivisionLimitExceeded;
        if (error == SvgFlattenError.InvalidArgument) return SvgFillContainmentError.InvalidArgument;
        return SvgFillContainmentError.NonFiniteResult;
    }

    static SvgFillContainmentError Map(SvgGeometryError error)
    {
        return error == SvgGeometryError.InvalidArgument ? SvgFillContainmentError.InvalidArgument : SvgFillContainmentError.NonFiniteResult;
    }
}
