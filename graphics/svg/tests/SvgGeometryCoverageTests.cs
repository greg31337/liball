namespace svg.Tests;

public class SvgGeometryCoverageTests
{
    [Test]
    public void SeededPathNormalizationIsIdempotentAndRoundTrips()
    {
        Random random = new Random(105086);
        int caseIndex = 0;
        while (caseIndex < 64)
        {
            string source = RandomPath(random);
            SvgPathParseResult parsed = new SvgPathParser().Parse(source, 512, 128);
            Assert.That(parsed.Success, Is.True, source);
            SvgPathNormalizeResult normalized = parsed.PathData.Normalize();
            Assert.That(normalized.Success, Is.True, source);
            SvgPathNormalizeResult repeated = normalized.PathData.Normalize();
            Assert.That(repeated.Success, Is.True, source);
            string canonical = normalized.PathData.ToText();
            Assert.That(repeated.PathData.ToText(), Is.EqualTo(canonical), source);
            SvgPathParseResult reparsed = new SvgPathParser().Parse(canonical, 512, 128);
            Assert.That(reparsed.Success, Is.True, canonical);
            Assert.That(reparsed.PathData.Normalize().PathData.ToText(), Is.EqualTo(canonical));
            AssertNormalized(normalized.PathData);
            caseIndex = caseIndex + 1;
        }
    }

    [Test]
    public void SeededBezierSplitsPreserveTheParametricLocus()
    {
        Random random = new Random(105090);
        int caseIndex = 0;
        while (caseIndex < 100)
        {
            SvgQuadraticBezier quadratic = new SvgQuadraticBezier(RandomPoint(random), RandomPoint(random), RandomPoint(random));
            SvgCubicBezier cubic = new SvgCubicBezier(RandomPoint(random), RandomPoint(random), RandomPoint(random), RandomPoint(random));
            double splitAmount = 0.05 + random.NextDouble() * 0.9;
            SvgQuadraticSplitResult quadraticSplit = quadratic.Split(splitAmount);
            SvgCubicSplitResult cubicSplit = cubic.Split(splitAmount);
            Assert.That(quadraticSplit.Success, Is.True);
            Assert.That(cubicSplit.Success, Is.True);
            AssertPoint(quadraticSplit.First.End, quadratic.Evaluate(splitAmount).Point, 0.0000000001);
            AssertPoint(quadraticSplit.First.End, quadraticSplit.Second.Start, 0);
            AssertPoint(cubicSplit.First.End, cubic.Evaluate(splitAmount).Point, 0.0000000001);
            AssertPoint(cubicSplit.First.End, cubicSplit.Second.Start, 0);
            double local = random.NextDouble();
            AssertPoint(quadraticSplit.First.Evaluate(local).Point, quadratic.Evaluate(local * splitAmount).Point, 0.000000001);
            AssertPoint(quadraticSplit.Second.Evaluate(local).Point, quadratic.Evaluate(splitAmount + local * (1 - splitAmount)).Point, 0.000000001);
            AssertPoint(cubicSplit.First.Evaluate(local).Point, cubic.Evaluate(local * splitAmount).Point, 0.000000001);
            AssertPoint(cubicSplit.Second.Evaluate(local).Point, cubic.Evaluate(splitAmount + local * (1 - splitAmount)).Point, 0.000000001);
            caseIndex = caseIndex + 1;
        }
    }

    [Test]
    public void EveryCircularArcQuadrantHasTightBoundsAndTangents()
    {
        double[,] cases = new double[,]
        {
            { 10, 0, 0, 10, 0, 0, 10, 10, 90, 180 },
            { 0, 10, -10, 0, -10, 0, 10, 10, 180, -90 },
            { -10, 0, 0, -10, -10, -10, 10, 10, -90, 0 },
            { 0, -10, 10, 0, 0, -10, 10, 10, 0, 90 }
        };
        int index = 0;
        while (index < cases.GetLength(0))
        {
            SvgPoint start = new SvgPoint(cases[index, 0], cases[index, 1]);
            SvgPoint end = new SvgPoint(cases[index, 2], cases[index, 3]);
            SvgArcOperationResult arc = new SvgArcGeometry().FromEndpoint(start, end, 10, 10, 0, false, true);
            Assert.That(arc.Success, Is.True);
            SvgRectOperationResult bounds = arc.Arc.GetBounds();
            Assert.That(bounds.Success, Is.True);
            AssertRect(bounds.Rectangle, cases[index, 4], cases[index, 5], cases[index, 6], cases[index, 7], 0.000000001);
            SvgVectorOperationResult startTangent = arc.Arc.Derivative(0);
            SvgVectorOperationResult endTangent = arc.Arc.Derivative(1);
            Assert.That(Angle(startTangent.Vector), Is.EqualTo(cases[index, 8]).Within(0.000000001));
            Assert.That(Angle(endTangent.Vector), Is.EqualTo(cases[index, 9]).Within(0.000000001));
            index = index + 1;
        }
    }

    [Test]
    public void FillBoundsCaptureCubicExtremaAcrossMultipleSubpaths()
    {
        SvgPathData path = Parse("M0 0 C0 9 9 9 9 0 M-5 -2 Q-3 -8 -1 -2 M20 20");

        SvgDrawingBoundsResult result = path.GetFillBounds();

        Assert.That(result.Success, Is.True);
        AssertRect(result.Bounds, -5, -5, 14, 11.75, 0.000000001);
    }

    [Test]
    public void FlattenedCurvesMeetSampledToleranceAndRefineMonotonically()
    {
        SvgQuadraticBezier quadratic = new SvgQuadraticBezier(new SvgPoint(0, 0), new SvgPoint(50, 100), new SvgPoint(100, 0));
        SvgCubicBezier cubic = new SvgCubicBezier(new SvgPoint(0, 0), new SvgPoint(0, 100), new SvgPoint(100, -100), new SvgPoint(100, 0));
        SvgArc arc = new SvgArcGeometry().FromEndpoint(new SvgPoint(10, 0), new SvgPoint(-10, 0), 10, 6, 25, false, true).Arc;
        double[] tolerances = new[] { 4.0, 1.0, 0.25, 0.0625 };
        int previousQuadratic = 0;
        int previousCubic = 0;
        int previousArc = 0;
        int index = 0;
        while (index < tolerances.Length)
        {
            SvgFlattenOptions options = new SvgFlattenOptions();
            options.Tolerance = tolerances[index];
            SvgFlattenResult quadraticFlat = quadratic.Flatten(options);
            SvgFlattenResult cubicFlat = cubic.Flatten(options);
            SvgFlattenResult arcFlat = arc.Flatten(options);
            Assert.That(quadraticFlat.Success, Is.True);
            Assert.That(cubicFlat.Success, Is.True);
            Assert.That(arcFlat.Success, Is.True);
            Assert.That(quadraticFlat.SegmentCount, Is.GreaterThanOrEqualTo(previousQuadratic));
            Assert.That(cubicFlat.SegmentCount, Is.GreaterThanOrEqualTo(previousCubic));
            Assert.That(arcFlat.SegmentCount, Is.GreaterThanOrEqualTo(previousArc));
            AssertDeviation(quadratic, quadraticFlat, tolerances[index]);
            AssertDeviation(cubic, cubicFlat, tolerances[index]);
            AssertDeviation(arc, arcFlat, tolerances[index]);
            previousQuadratic = quadraticFlat.SegmentCount;
            previousCubic = cubicFlat.SegmentCount;
            previousArc = arcFlat.SegmentCount;
            index = index + 1;
        }
    }

    [Test]
    public void OddDashPatternsKeepBoundaryAndBoundsSemantics()
    {
        SvgPathData line = Parse("M0 0 L20 0");
        SvgStrokeGeometryOptions options = new SvgStrokeGeometryOptions();
        options.Style.Width = 2;
        options.Style.DashArray.Add(3);
        options.Style.DashArray.Add(2);
        options.Style.DashArray.Add(1);

        SvgStrokeBoundsResult bounds = line.GetStrokeBounds(options);

        Assert.Multiple(() =>
        {
            AssertStroke(line, options, 2.9, true);
            AssertStroke(line, options, 3.1, false);
            AssertStroke(line, options, 5.1, true);
            AssertStroke(line, options, 6.1, false);
            AssertStroke(line, options, 9.1, true);
            AssertStroke(line, options, 11.1, false);
            AssertStroke(line, options, 12.1, true);
            Assert.That(bounds.Success, Is.True);
            AssertRect(bounds.Bounds, 0, -1, 18, 2, 0.000000001);
        });
    }

    [Test]
    public void ArcMarkersRetainQuadrantTangentsAcrossSubpaths()
    {
        SvgMarkerVertexResult result = Parse("M10 0 A10 10 0 0 1 0 10 M0 -10 A10 10 0 0 1 10 0").GetMarkerVertices();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Vertices, Has.Count.EqualTo(4));
            Assert.That(result.Vertices.Select(vertex => vertex.Placement), Is.EqualTo(new[] { SvgMarkerPlacement.Start, SvgMarkerPlacement.End, SvgMarkerPlacement.Start, SvgMarkerPlacement.End }));
            Assert.That(result.Vertices.Select(vertex => vertex.SubpathIndex), Is.EqualTo(new[] { 0, 0, 1, 1 }));
            Assert.That(result.Vertices[0].AutoAngle, Is.EqualTo(90).Within(0.000000001));
            Assert.That(System.Math.Abs(result.Vertices[1].AutoAngle), Is.EqualTo(180).Within(0.000000001));
            Assert.That(result.Vertices[2].AutoAngle, Is.EqualTo(0).Within(0.000000001));
            Assert.That(result.Vertices[3].AutoAngle, Is.EqualTo(90).Within(0.000000001));
        });
    }

    [Test]
    public void SeededRectangleBooleansMatchPointwiseTruthTables()
    {
        Random random = new Random(105100);
        SvgPathBooleanOperation[] operations = new[] { SvgPathBooleanOperation.Union, SvgPathBooleanOperation.Intersection, SvgPathBooleanOperation.Difference, SvgPathBooleanOperation.Xor };
        int caseIndex = 0;
        while (caseIndex < 24)
        {
            double firstX = random.Next(-10, 8);
            double firstY = random.Next(-10, 8);
            double firstWidth = random.Next(1, 9);
            double firstHeight = random.Next(1, 9);
            double secondX = random.Next(-10, 8);
            double secondY = random.Next(-10, 8);
            double secondWidth = random.Next(1, 9);
            double secondHeight = random.Next(1, 9);
            SvgPathData first = Rect(firstX, firstY, firstWidth, firstHeight);
            SvgPathData second = Rect(secondX, secondY, secondWidth, secondHeight);
            int operationIndex = 0;
            while (operationIndex < operations.Length)
            {
                SvgPathBooleanResult result = new SvgPathBoolean().ApplyWithFirstAndSecondAndOperation(first, second, operations[operationIndex]);
                Assert.That(result.Success, Is.True, "case " + caseIndex + " operation " + operations[operationIndex]);
                int sampleIndex = 0;
                while (sampleIndex < 20)
                {
                    double x = random.Next(-24, 25) / 2.0 + 0.125;
                    double y = random.Next(-24, 25) / 2.0 + 0.125;
                    bool inFirst = Inside(x, y, firstX, firstY, firstWidth, firstHeight);
                    bool inSecond = Inside(x, y, secondX, secondY, secondWidth, secondHeight);
                    bool expected = Boolean(inFirst, inSecond, operations[operationIndex]);
                    SvgFillContainmentResult actual = result.PathData.ClassifyFill(new SvgPoint(x, y), SvgFillRule.Nonzero);
                    Assert.That(actual.Success, Is.True);
                    Assert.That(actual.Contains, Is.EqualTo(expected), "case " + caseIndex + " operation " + operations[operationIndex] + " point " + x + "," + y);
                    sampleIndex = sampleIndex + 1;
                }
                operationIndex = operationIndex + 1;
            }
            caseIndex = caseIndex + 1;
        }
    }

    [Test]
    public void SeededTransformedHitTestsMatchRectangleOracle()
    {
        Random random = new Random(105104);
        int caseIndex = 0;
        while (caseIndex < 100)
        {
            double width = random.Next(1, 20);
            double height = random.Next(1, 20);
            double scaleX = random.Next(1, 6) / 2.0;
            double scaleY = random.Next(1, 6) / 2.0;
            double translateX = random.Next(-20, 21);
            double translateY = random.Next(-20, 21);
            bool expected = caseIndex % 2 == 0;
            double localX = expected ? 0.1 + random.NextDouble() * (width - 0.2) : width + 0.5;
            double localY = expected ? 0.1 + random.NextDouble() * (height - 0.2) : height / 2;
            SvgPoint query = new SvgPoint(localX * scaleX + translateX, localY * scaleY + translateY);
            SvgGeometryHitTestOptions options = new SvgGeometryHitTestOptions();
            options.PointerEvents = SvgPointerEventsMode.Fill;
            options.FillPainted = false;
            options.Transform = new SvgMatrix(scaleX, 0, 0, scaleY, translateX, translateY);
            SvgGeometryHitTestResult hit = Rect(0, 0, width, height).HitTest(query, options);
            Assert.That(hit.Success, Is.True);
            Assert.That(hit.Hit, Is.EqualTo(expected), "case " + caseIndex);
            caseIndex = caseIndex + 1;
        }
    }

    static string RandomPath(Random random)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.Append("M ");
        AppendNumber(text, random.Next(-20, 21));
        AppendNumber(text, random.Next(-20, 21));
        int index = 0;
        while (index < 18)
        {
            int kind = random.Next(8);
            if (kind == 0) AppendCommand(text, "l", random, 2);
            else if (kind == 1) AppendCommand(text, "H", random, 1);
            else if (kind == 2) AppendCommand(text, "v", random, 1);
            else if (kind == 3) AppendCommand(text, "q", random, 4);
            else if (kind == 4) AppendCommand(text, "t", random, 2);
            else if (kind == 5) AppendCommand(text, "c", random, 6);
            else if (kind == 6) AppendCommand(text, "s", random, 4);
            else
            {
                text.Append(" a ");
                AppendNumber(text, random.Next(1, 21));
                AppendNumber(text, random.Next(1, 21));
                AppendNumber(text, random.Next(-180, 181));
                AppendNumber(text, random.Next(2));
                AppendNumber(text, random.Next(2));
                AppendNumber(text, random.Next(-20, 21));
                AppendNumber(text, random.Next(-20, 21));
            }
            index = index + 1;
        }
        text.Append(" z m 3 -4 l 5 6");
        return text.ToString();
    }

    static void AppendCommand(System.Text.StringBuilder text, string command, Random random, int count)
    {
        text.Append(' ');
        text.Append(command);
        text.Append(' ');
        int index = 0;
        while (index < count)
        {
            AppendNumber(text, random.Next(-20, 21));
            index = index + 1;
        }
    }

    static void AppendNumber(System.Text.StringBuilder text, double value)
    {
        text.Append(value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        text.Append(' ');
    }

    static void AssertNormalized(SvgPathData path)
    {
        int index = 0;
        while (index < path.Commands.Count)
        {
            SvgPathCommand command = path.Commands[index];
            bool kind = command.Kind == SvgPathCommandKind.MoveTo || command.Kind == SvgPathCommandKind.LineTo || command.Kind == SvgPathCommandKind.QuadraticCurveTo || command.Kind == SvgPathCommandKind.CubicCurveTo || command.Kind == SvgPathCommandKind.ArcTo || command.Kind == SvgPathCommandKind.ClosePath;
            Assert.That(kind, Is.True);
            Assert.That(command.Relative, Is.False);
            Assert.That(double.IsFinite(command.StartX), Is.True);
            Assert.That(double.IsFinite(command.StartY), Is.True);
            Assert.That(double.IsFinite(command.EndX), Is.True);
            Assert.That(double.IsFinite(command.EndY), Is.True);
            index = index + 1;
        }
    }

    static SvgPoint RandomPoint(Random random)
    {
        return new SvgPoint(random.Next(-100, 101), random.Next(-100, 101));
    }

    static void AssertDeviation(SvgQuadraticBezier curve, SvgFlattenResult flattened, double tolerance)
    {
        int index = 1;
        while (index < flattened.Points.Count)
        {
            double amount = (flattened.Points[index - 1].Amount + flattened.Points[index].Amount) / 2;
            SvgPoint point = curve.Evaluate(amount).Point;
            double deviation = Distance(point, flattened.Points[index - 1].Point, flattened.Points[index].Point);
            Assert.That(deviation, Is.LessThanOrEqualTo(tolerance + 0.000000001));
            index = index + 1;
        }
    }

    static void AssertDeviation(SvgCubicBezier curve, SvgFlattenResult flattened, double tolerance)
    {
        int index = 1;
        while (index < flattened.Points.Count)
        {
            double amount = (flattened.Points[index - 1].Amount + flattened.Points[index].Amount) / 2;
            SvgPoint point = curve.Evaluate(amount).Point;
            double deviation = Distance(point, flattened.Points[index - 1].Point, flattened.Points[index].Point);
            Assert.That(deviation, Is.LessThanOrEqualTo(tolerance + 0.000000001));
            index = index + 1;
        }
    }

    static void AssertDeviation(SvgArc curve, SvgFlattenResult flattened, double tolerance)
    {
        int index = 1;
        while (index < flattened.Points.Count)
        {
            double amount = (flattened.Points[index - 1].Amount + flattened.Points[index].Amount) / 2;
            SvgPoint point = curve.Evaluate(amount).Point;
            double deviation = Distance(point, flattened.Points[index - 1].Point, flattened.Points[index].Point);
            Assert.That(deviation, Is.LessThanOrEqualTo(tolerance + 0.000000001));
            index = index + 1;
        }
    }

    static double Distance(SvgPoint point, SvgPoint start, SvgPoint end)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double lengthSquared = dx * dx + dy * dy;
        if (lengthSquared == 0) return System.Math.Sqrt((point.X - start.X) * (point.X - start.X) + (point.Y - start.Y) * (point.Y - start.Y));
        double amount = ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / lengthSquared;
        if (amount < 0) amount = 0;
        if (amount > 1) amount = 1;
        double x = start.X + amount * dx;
        double y = start.Y + amount * dy;
        return System.Math.Sqrt((point.X - x) * (point.X - x) + (point.Y - y) * (point.Y - y));
    }

    static double Angle(SvgVector vector)
    {
        double value = System.Math.Atan2(vector.Y, vector.X) * 180 / System.Math.PI;
        if (value == -180) return 180;
        return value;
    }

    static void AssertStroke(SvgPathData path, SvgStrokeGeometryOptions options, double x, bool expected)
    {
        SvgStrokeContainmentResult result = path.ContainsStroke(new SvgPoint(x, 0), options);
        Assert.That(result.Success, Is.True);
        Assert.That(result.Contains, Is.EqualTo(expected));
    }

    static bool Boolean(bool first, bool second, SvgPathBooleanOperation operation)
    {
        if (operation == SvgPathBooleanOperation.Union) return first || second;
        if (operation == SvgPathBooleanOperation.Intersection) return first && second;
        if (operation == SvgPathBooleanOperation.Difference) return first && !second;
        return first != second;
    }

    static bool Inside(double x, double y, double left, double top, double width, double height)
    {
        return x > left && x < left + width && y > top && y < top + height;
    }

    static SvgPathData Rect(double x, double y, double width, double height)
    {
        string text = "M" + Number(x) + " " + Number(y) + " H" + Number(x + width) + " V" + Number(y + height) + " H" + Number(x) + " Z";
        return Parse(text);
    }

    static string Number(double value)
    {
        return value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    }

    static SvgPathData Parse(string source)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(source, 1024, 128);
        Assert.That(result.Success, Is.True, source);
        return result.PathData;
    }

    static void AssertPoint(SvgPoint actual, SvgPoint expected, double tolerance)
    {
        Assert.That(actual.X, Is.EqualTo(expected.X).Within(tolerance));
        Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(tolerance));
    }

    static void AssertRect(SvgRect actual, double x, double y, double width, double height, double tolerance)
    {
        Assert.That(actual.IsEmpty, Is.False);
        Assert.That(actual.X, Is.EqualTo(x).Within(tolerance));
        Assert.That(actual.Y, Is.EqualTo(y).Within(tolerance));
        Assert.That(actual.Width, Is.EqualTo(width).Within(tolerance));
        Assert.That(actual.Height, Is.EqualTo(height).Within(tolerance));
    }
}
