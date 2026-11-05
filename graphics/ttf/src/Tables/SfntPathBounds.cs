namespace ttf;

internal enum SfntBoundsPolicy
{
    KeepStored = 0,
    UseCalculated = 1,
    RequireMatch = 2
}

internal class SfntPathBounds
{
    bool empty;
    double xMinimum;
    double yMinimum;
    double xMaximum;
    double yMaximum;

    internal SfntPathBounds(
        bool empty,
        double xMinimum,
        double yMinimum,
        double xMaximum,
        double yMaximum)
    {
        this.empty = empty;
        this.xMinimum = xMinimum;
        this.yMinimum = yMinimum;
        this.xMaximum = xMaximum;
        this.yMaximum = yMaximum;
    }

    internal bool IsEmpty()
    {
        return empty;
    }

    internal double GetXMinimum()
    {
        EnsureNotEmpty();
        return xMinimum;
    }

    internal double GetYMinimum()
    {
        EnsureNotEmpty();
        return yMinimum;
    }

    internal double GetXMaximum()
    {
        EnsureNotEmpty();
        return xMaximum;
    }

    internal double GetYMaximum()
    {
        EnsureNotEmpty();
        return yMaximum;
    }

    void EnsureNotEmpty()
    {
        if (empty)
        {
            throw new System.InvalidOperationException(
                "An empty path has no coordinate bounds.");
        }
    }
}

internal class SfntBoundsComparison
{
    SfntPathBounds storedBounds;
    SfntPathBounds calculatedBounds;
    SfntPathBounds effectiveBounds;
    bool match;

    internal SfntBoundsComparison(
        SfntPathBounds storedBounds,
        SfntPathBounds calculatedBounds,
        SfntPathBounds effectiveBounds,
        bool match)
    {
        this.storedBounds = storedBounds;
        this.calculatedBounds = calculatedBounds;
        this.effectiveBounds = effectiveBounds;
        this.match = match;
    }

    internal SfntPathBounds GetStoredBounds()
    {
        return storedBounds;
    }

    internal SfntPathBounds GetCalculatedBounds()
    {
        return calculatedBounds;
    }

    internal SfntPathBounds GetEffectiveBounds()
    {
        return effectiveBounds;
    }

    internal bool IsMatch()
    {
        return match;
    }
}

internal class SfntBoundsAccumulator
{
    bool empty;
    double xMinimum;
    double yMinimum;
    double xMaximum;
    double yMaximum;

    internal SfntBoundsAccumulator()
    {
        empty = true;
    }

    internal void AddPoint(double x, double y)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y))
        {
            throw FontFormatException.Create(
                "A path contains a non-finite coordinate.");
        }

        if (empty)
        {
            xMinimum = x;
            yMinimum = y;
            xMaximum = x;
            yMaximum = y;
            empty = false;
            return;
        }

        if (x < xMinimum)
        {
            xMinimum = x;
        }

        if (y < yMinimum)
        {
            yMinimum = y;
        }

        if (x > xMaximum)
        {
            xMaximum = x;
        }

        if (y > yMaximum)
        {
            yMaximum = y;
        }
    }

    internal void AddBounds(SfntPathBounds bounds)
    {
        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        if (!bounds.IsEmpty())
        {
            AddPoint(bounds.GetXMinimum(), bounds.GetYMinimum());
            AddPoint(bounds.GetXMaximum(), bounds.GetYMaximum());
        }
    }

    internal SfntPathBounds CreateBounds()
    {
        return new SfntPathBounds(
            empty,
            xMinimum,
            yMinimum,
            xMaximum,
            yMaximum);
    }
}

internal static class SfntPathBoundsCalculator
{
    const double ComparisonTolerance = 0.000000001;

    internal static SfntPathBounds Calculate(SfntGlyphOutline outline)
    {
        if (outline == null)
        {
            throw new System.ArgumentNullException("outline");
        }

        SfntBoundsAccumulator bounds = new SfntBoundsAccumulator();
        bool hasCurrentPoint = false;
        double currentX = 0;
        double currentY = 0;
        double startX = 0;
        double startY = 0;
        int commandIndex = 0;

        while (commandIndex < outline.GetCommandCount())
        {
            SfntPathCommand command = outline.GetCommand(commandIndex);
            SfntPathCommandKind kind = command.GetKind();

            if (kind == SfntPathCommandKind.Move)
            {
                currentX = command.GetEndX();
                currentY = command.GetEndY();
                startX = currentX;
                startY = currentY;
                bounds.AddPoint(currentX, currentY);
                hasCurrentPoint = true;
            }
            else
            {
                if (!hasCurrentPoint)
                {
                    throw FontFormatException.Create(
                        "A drawing path command requires a current point.");
                }

                if (kind == SfntPathCommandKind.Line)
                {
                    currentX = command.GetEndX();
                    currentY = command.GetEndY();
                    bounds.AddPoint(currentX, currentY);
                }
                else if (kind == SfntPathCommandKind.Quadratic)
                {
                    AddQuadraticBounds(
                        bounds,
                        currentX,
                        currentY,
                        command.GetControlX(),
                        command.GetControlY(),
                        command.GetEndX(),
                        command.GetEndY());
                    currentX = command.GetEndX();
                    currentY = command.GetEndY();
                }
                else if (kind == SfntPathCommandKind.Cubic)
                {
                    AddCubicBounds(
                        bounds,
                        currentX,
                        currentY,
                        command.GetControlX(),
                        command.GetControlY(),
                        command.GetSecondControlX(),
                        command.GetSecondControlY(),
                        command.GetEndX(),
                        command.GetEndY());
                    currentX = command.GetEndX();
                    currentY = command.GetEndY();
                }
                else if (kind == SfntPathCommandKind.Close)
                {
                    bounds.AddPoint(startX, startY);
                    currentX = startX;
                    currentY = startY;
                    hasCurrentPoint = false;
                }
                else
                {
                    throw FontFormatException.Create(
                        "A path contains an unsupported command kind.");
                }
            }

            commandIndex += 1;
        }

        return bounds.CreateBounds();
    }

    internal static SfntBoundsComparison CompareGlyph(
        SfntResolvedGlyph glyph,
        SfntGlyphHeader? header,
        SfntBoundsPolicy policy)
    {
        if (glyph == null)
        {
            throw new System.ArgumentNullException("glyph");
        }

        SfntPathBounds stored = header == null
            ? CreateZeroBounds()
            : CreateHeaderBounds(header);
        return Compare(stored, glyph.GetOutline().GetBounds(), policy);
    }

    internal static SfntBoundsComparison CompareFont(
        SfntLocaTable table,
        SfntHeadTable head,
        SfntBoundsPolicy policy)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (head == null)
        {
            throw new System.ArgumentNullException("head");
        }

        SfntBoundsAccumulator calculated = new SfntBoundsAccumulator();
        int glyphId = 0;

        while (glyphId < table.GetGlyphCount())
        {
            calculated.AddBounds(
                table.GetResolvedGlyph(glyphId).GetOutline().GetBounds());
            glyphId += 1;
        }

        SfntPathBounds stored = new SfntPathBounds(
            false,
            head.GetXMin(),
            head.GetYMin(),
            head.GetXMax(),
            head.GetYMax());
        return Compare(stored, calculated.CreateBounds(), policy);
    }

    internal static SfntBoundsComparison Compare(
        SfntPathBounds stored,
        SfntPathBounds calculated,
        SfntBoundsPolicy policy)
    {
        if (stored == null)
        {
            throw new System.ArgumentNullException("stored");
        }

        if (calculated == null)
        {
            throw new System.ArgumentNullException("calculated");
        }

        ValidatePolicy(policy);
        bool match = BoundsMatch(stored, calculated);

        if (!match && policy == SfntBoundsPolicy.RequireMatch)
        {
            throw FontFormatException.Create(
                "Stored bounds differ from exact calculated path bounds.");
        }

        SfntPathBounds effective = policy == SfntBoundsPolicy.UseCalculated
            ? calculated
            : stored;
        return new SfntBoundsComparison(
            stored,
            calculated,
            effective,
            match);
    }

    static void AddQuadraticBounds(
        SfntBoundsAccumulator bounds,
        double startX,
        double startY,
        double controlX,
        double controlY,
        double endX,
        double endY)
    {
        ValidateFinitePoint(controlX, controlY);
        bounds.AddPoint(startX, startY);
        bounds.AddPoint(endX, endY);
        AddQuadraticExtremum(
            bounds,
            startX,
            controlX,
            endX,
            startX,
            startY,
            controlX,
            controlY,
            endX,
            endY);
        AddQuadraticExtremum(
            bounds,
            startY,
            controlY,
            endY,
            startX,
            startY,
            controlX,
            controlY,
            endX,
            endY);
    }

    static void AddQuadraticExtremum(
        SfntBoundsAccumulator bounds,
        double start,
        double control,
        double end,
        double startX,
        double startY,
        double controlX,
        double controlY,
        double endX,
        double endY)
    {
        double denominator = start - (2 * control) + end;

        if (denominator == 0)
        {
            return;
        }

        double time = (start - control) / denominator;

        if (time > 0 && time < 1)
        {
            bounds.AddPoint(
                EvaluateQuadratic(startX, controlX, endX, time),
                EvaluateQuadratic(startY, controlY, endY, time));
        }
    }

    static void AddCubicBounds(
        SfntBoundsAccumulator bounds,
        double startX,
        double startY,
        double firstControlX,
        double firstControlY,
        double secondControlX,
        double secondControlY,
        double endX,
        double endY)
    {
        ValidateFinitePoint(firstControlX, firstControlY);
        ValidateFinitePoint(secondControlX, secondControlY);
        bounds.AddPoint(startX, startY);
        bounds.AddPoint(endX, endY);
        AddCubicExtrema(
            bounds,
            startX,
            firstControlX,
            secondControlX,
            endX,
            startX,
            startY,
            firstControlX,
            firstControlY,
            secondControlX,
            secondControlY,
            endX,
            endY);
        AddCubicExtrema(
            bounds,
            startY,
            firstControlY,
            secondControlY,
            endY,
            startX,
            startY,
            firstControlX,
            firstControlY,
            secondControlX,
            secondControlY,
            endX,
            endY);
    }

    static void AddCubicExtrema(
        SfntBoundsAccumulator bounds,
        double start,
        double firstControl,
        double secondControl,
        double end,
        double startX,
        double startY,
        double firstControlX,
        double firstControlY,
        double secondControlX,
        double secondControlY,
        double endX,
        double endY)
    {
        double quadratic = -start + (3 * firstControl) -
            (3 * secondControl) + end;
        double linear = 2 * (start - (2 * firstControl) + secondControl);
        double constant = firstControl - start;

        if (quadratic == 0)
        {
            if (linear != 0)
            {
                AddCubicExtremum(
                    bounds,
                    -constant / linear,
                    startX,
                    startY,
                    firstControlX,
                    firstControlY,
                    secondControlX,
                    secondControlY,
                    endX,
                    endY);
            }

            return;
        }

        double discriminant = (linear * linear) -
            (4 * quadratic * constant);

        if (discriminant < 0)
        {
            return;
        }

        double squareRoot = System.Math.Sqrt(discriminant);
        AddCubicExtremum(
            bounds,
            (-linear + squareRoot) / (2 * quadratic),
            startX,
            startY,
            firstControlX,
            firstControlY,
            secondControlX,
            secondControlY,
            endX,
            endY);
        AddCubicExtremum(
            bounds,
            (-linear - squareRoot) / (2 * quadratic),
            startX,
            startY,
            firstControlX,
            firstControlY,
            secondControlX,
            secondControlY,
            endX,
            endY);
    }

    static void AddCubicExtremum(
        SfntBoundsAccumulator bounds,
        double time,
        double startX,
        double startY,
        double firstControlX,
        double firstControlY,
        double secondControlX,
        double secondControlY,
        double endX,
        double endY)
    {
        if (time > 0 && time < 1)
        {
            bounds.AddPoint(
                EvaluateCubic(
                    startX,
                    firstControlX,
                    secondControlX,
                    endX,
                    time),
                EvaluateCubic(
                    startY,
                    firstControlY,
                    secondControlY,
                    endY,
                    time));
        }
    }

    static double EvaluateQuadratic(
        double start,
        double control,
        double end,
        double time)
    {
        double inverse = 1 - time;
        return (inverse * inverse * start) +
            (2 * inverse * time * control) +
            (time * time * end);
    }

    static double EvaluateCubic(
        double start,
        double firstControl,
        double secondControl,
        double end,
        double time)
    {
        double inverse = 1 - time;
        return (inverse * inverse * inverse * start) +
            (3 * inverse * inverse * time * firstControl) +
            (3 * inverse * time * time * secondControl) +
            (time * time * time * end);
    }

    static SfntPathBounds CreateHeaderBounds(SfntGlyphHeader header)
    {
        return new SfntPathBounds(
            false,
            header.GetXMinimum(),
            header.GetYMinimum(),
            header.GetXMaximum(),
            header.GetYMaximum());
    }

    static SfntPathBounds CreateZeroBounds()
    {
        return new SfntPathBounds(false, 0, 0, 0, 0);
    }

    static bool BoundsMatch(
        SfntPathBounds first,
        SfntPathBounds second)
    {
        if (first.IsEmpty() || second.IsEmpty())
        {
            if (first.IsEmpty() && second.IsEmpty())
            {
                return true;
            }

            SfntPathBounds nonempty = first.IsEmpty() ? second : first;
            return NearlyEqual(nonempty.GetXMinimum(), 0) &&
                NearlyEqual(nonempty.GetYMinimum(), 0) &&
                NearlyEqual(nonempty.GetXMaximum(), 0) &&
                NearlyEqual(nonempty.GetYMaximum(), 0);
        }

        return NearlyEqual(first.GetXMinimum(), second.GetXMinimum()) &&
            NearlyEqual(first.GetYMinimum(), second.GetYMinimum()) &&
            NearlyEqual(first.GetXMaximum(), second.GetXMaximum()) &&
            NearlyEqual(first.GetYMaximum(), second.GetYMaximum());
    }

    static bool NearlyEqual(double first, double second)
    {
        return System.Math.Abs(first - second) <= ComparisonTolerance;
    }

    static void ValidateFinitePoint(double x, double y)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y))
        {
            throw FontFormatException.Create(
                "A path contains a non-finite coordinate.");
        }
    }

    static void ValidatePolicy(SfntBoundsPolicy policy)
    {
        if (policy != SfntBoundsPolicy.KeepStored &&
            policy != SfntBoundsPolicy.UseCalculated &&
            policy != SfntBoundsPolicy.RequireMatch)
        {
            throw new System.ArgumentOutOfRangeException("policy");
        }
    }
}
