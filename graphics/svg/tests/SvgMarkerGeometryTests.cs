namespace svg.Tests;

public class SvgMarkerGeometryTests
{
    [Test]
    public void OpenPolylineProducesStartMidAndEndTangents()
    {
        SvgMarkerVertexResult result = Parse("M0 0 L10 0 L10 10").GetMarkerVertices();

        Assert.Multiple(() =>
        {
            AssertVertices(result, 3);
            AssertVertex(result.Vertices[0], SvgMarkerPlacement.Start, 0, 0, 0, 0, 1, 0, 0);
            AssertVertex(result.Vertices[1], SvgMarkerPlacement.Mid, 10, 0, 1, 0, 0, 1, 45);
            AssertVertex(result.Vertices[2], SvgMarkerPlacement.End, 10, 10, 0, 1, 0, 0, 90);
        });
    }

    [Test]
    public void CurveEndpointTangentsUseFirstNoncoincidentControls()
    {
        SvgMarkerVertexResult quadratic = Parse("M0 0 Q0 10 10 10").GetMarkerVertices();
        SvgMarkerVertexResult cubic = Parse("M0 0 C0 0 0 10 10 10").GetMarkerVertices();
        SvgMarkerVertexResult arc = Parse("M1 0 A1 1 0 0 1 0 1").GetMarkerVertices();

        Assert.Multiple(() =>
        {
            AssertVertices(quadratic, 2);
            Assert.That(quadratic.Vertices[0].AutoAngle, Is.EqualTo(90).Within(1e-10));
            Assert.That(quadratic.Vertices[1].AutoAngle, Is.EqualTo(0).Within(1e-10));
            AssertVertices(cubic, 2);
            Assert.That(cubic.Vertices[0].AutoAngle, Is.EqualTo(90).Within(1e-10));
            Assert.That(cubic.Vertices[1].AutoAngle, Is.EqualTo(0).Within(1e-10));
            AssertVertices(arc, 2);
            Assert.That(arc.Vertices[0].AutoAngle, Is.EqualTo(90).Within(1e-10));
            Assert.That(arc.Vertices[1].AutoAngle, Is.EqualTo(180).Within(1e-10));
        });
    }

    [Test]
    public void ZeroLengthSegmentsSearchForNeighboringTangents()
    {
        SvgMarkerVertexResult result = Parse("M0 0 L0 0 L10 0 L10 0").GetMarkerVertices();

        Assert.Multiple(() =>
        {
            AssertVertices(result, 4);
            Assert.That(result.Vertices[0].OutgoingTangent.X, Is.EqualTo(1));
            Assert.That(result.Vertices[0].AutoAngle, Is.EqualTo(0));
            Assert.That(result.Vertices[1].OutgoingTangent.X, Is.EqualTo(1));
            Assert.That(result.Vertices[2].IncomingTangent.X, Is.EqualTo(1));
            Assert.That(result.Vertices[3].IncomingTangent.X, Is.EqualTo(1));
            Assert.That(result.Vertices.All(vertex => vertex.AutoAngle == 0), Is.True);
        });
    }

    [Test]
    public void ClosedSubpathsUseClosingTangentsAndCoincidentStartEndMarkers()
    {
        SvgMarkerVertexResult result = Parse("M0 0 L10 0 L10 10 L0 10 Z").GetMarkerVertices();

        Assert.Multiple(() =>
        {
            AssertVertices(result, 5);
            Assert.That(result.Vertices[0].Placement, Is.EqualTo(SvgMarkerPlacement.Start));
            Assert.That(result.Vertices[0].Position.ValueEquals(result.Vertices[^1].Position), Is.True);
            Assert.That(result.Vertices[0].IncomingTangent.Y, Is.EqualTo(-1));
            Assert.That(result.Vertices[0].AutoAngle, Is.EqualTo(0));
            Assert.That(result.Vertices[1].AutoAngle, Is.EqualTo(45).Within(1e-10));
            Assert.That(result.Vertices[2].AutoAngle, Is.EqualTo(135).Within(1e-10));
            Assert.That(result.Vertices[3].AutoAngle, Is.EqualTo(-135).Within(1e-10));
            Assert.That(result.Vertices[4].Placement, Is.EqualTo(SvgMarkerPlacement.End));
            Assert.That(result.Vertices[4].AutoAngle, Is.EqualTo(-90).Within(1e-10));
        });
    }

    [Test]
    public void AutoStartReverseAndAutoEndApplyPlacementSpecificAngles()
    {
        SvgMarkerGeometryOptions options = new SvgMarkerGeometryOptions();
        options.StartMarker = Definition(SvgRect.Create(0, -1, 2, 2), SvgMarkerOrientKind.AutoStartReverse);
        options.EndMarker = Definition(SvgRect.Create(0, -1, 2, 2), SvgMarkerOrientKind.Auto);

        SvgMarkerGeometryResult result = Parse("M0 0 L10 0").GetMarkerGeometry(options);

        Assert.Multiple(() =>
        {
            AssertGeometry(result, 2, 2);
            Assert.That(result.Instances[0].AppliedAngle, Is.EqualTo(180));
            AssertRect(result.Instances[0].Bounds, -2, -1, 2, 2);
            Assert.That(result.Instances[1].AppliedAngle, Is.EqualTo(0));
            AssertRect(result.Instances[1].Bounds, 10, -1, 2, 2);
            AssertRect(result.Bounds, -2, -1, 14, 2);
        });
    }

    [Test]
    public void FixedAnglesReferencePointsAndContentTransformsComposeInOrder()
    {
        SvgMarkerGeometryOptions options = new SvgMarkerGeometryOptions();
        SvgMarkerDefinition end = Definition(SvgRect.Create(0, 0, 2, 1), SvgMarkerOrientKind.Angle);
        end.Angle = 90;
        end.RefX = 1;
        end.ContentTransform = new SvgMatrix(2, 0, 0, 1, 0, 0);
        options.EndMarker = end;

        SvgMarkerGeometryResult result = Parse("M0 0 L10 0").GetMarkerGeometry(options);

        Assert.Multiple(() =>
        {
            AssertGeometry(result, 2, 1);
            Assert.That(result.Instances[0].AppliedAngle, Is.EqualTo(90));
            AssertRect(result.Instances[0].Bounds, 9, -1, 1, 4, 1e-10);
        });
    }

    [Test]
    public void StrokeWidthUnitsScaleMarkerGeometryAndUserUnitsDoNot()
    {
        SvgMarkerGeometryOptions options = new SvgMarkerGeometryOptions();
        options.StrokeWidth = 3;
        SvgMarkerDefinition start = Definition(SvgRect.Create(0, 0, 2, 1), SvgMarkerOrientKind.Angle);
        start.Units = SvgMarkerUnits.StrokeWidth;
        options.StartMarker = start;
        SvgMarkerDefinition end = Definition(SvgRect.Create(0, 0, 2, 1), SvgMarkerOrientKind.Angle);
        end.Units = SvgMarkerUnits.UserSpaceOnUse;
        options.EndMarker = end;

        SvgMarkerGeometryResult result = Parse("M0 0 L10 0").GetMarkerGeometry(options);

        Assert.Multiple(() =>
        {
            AssertGeometry(result, 2, 2);
            Assert.That(result.Instances[0].Scale, Is.EqualTo(3));
            AssertRect(result.Instances[0].Bounds, 0, 0, 6, 3);
            Assert.That(result.Instances[1].Scale, Is.EqualTo(1));
            AssertRect(result.Instances[1].Bounds, 10, 0, 2, 1);
            AssertRect(result.Bounds, 0, 0, 12, 3);
        });
    }

    [Test]
    public void ZeroStrokeWidthSuppressesStrokeUnitMarkersOnly()
    {
        SvgMarkerGeometryOptions options = new SvgMarkerGeometryOptions();
        options.StrokeWidth = 0;
        SvgMarkerDefinition start = Definition(SvgRect.Create(0, 0, 2, 1), SvgMarkerOrientKind.Angle);
        start.Units = SvgMarkerUnits.StrokeWidth;
        options.StartMarker = start;
        options.EndMarker = Definition(SvgRect.Create(0, 0, 2, 1), SvgMarkerOrientKind.Angle);

        SvgMarkerGeometryResult result = Parse("M0 0 L10 0").GetMarkerGeometry(options);

        Assert.Multiple(() =>
        {
            AssertGeometry(result, 2, 1);
            Assert.That(result.Instances[0].Vertex.Placement, Is.EqualTo(SvgMarkerPlacement.End));
            AssertRect(result.Bounds, 10, 0, 2, 1);
        });
    }

    [Test]
    public void DistinctSubpathsAndEnabledPlacementRolesRemainOrdered()
    {
        SvgMarkerGeometryOptions options = new SvgMarkerGeometryOptions();
        options.MidMarker = Definition(SvgRect.Create(-1, -1, 2, 2), SvgMarkerOrientKind.Auto);
        options.EndMarker = Definition(SvgRect.Create(-1, -1, 2, 2), SvgMarkerOrientKind.Auto);

        SvgMarkerGeometryResult result = Parse("M0 0 L2 0 L2 2 M10 0 L12 0").GetMarkerGeometry(options);

        Assert.Multiple(() =>
        {
            AssertGeometry(result, 5, 3);
            Assert.That(result.Vertices.Select(vertex => vertex.SubpathIndex), Is.EqualTo(new[] { 0, 0, 0, 1, 1 }));
            Assert.That(result.Instances.Select(instance => instance.Vertex.Placement), Is.EqualTo(new[] { SvgMarkerPlacement.Mid, SvgMarkerPlacement.End, SvgMarkerPlacement.End }));
            AssertRect(result.Bounds, 2 - System.Math.Sqrt(2), -System.Math.Sqrt(2), 11 + System.Math.Sqrt(2), 3 + System.Math.Sqrt(2));
        });
    }

    [Test]
    public void ReversingTangentsUseDeterministicHalfAngleBisectors()
    {
        SvgMarkerVertexResult result = Parse("M0 0 L10 0 L0 0").GetMarkerVertices();

        Assert.Multiple(() =>
        {
            AssertVertices(result, 3);
            Assert.That(result.Vertices[1].IncomingTangent.X, Is.EqualTo(1));
            Assert.That(result.Vertices[1].OutgoingTangent.X, Is.EqualTo(-1));
            Assert.That(result.Vertices[1].AutoAngle, Is.EqualTo(90).Within(1e-10));
        });
    }

    [Test]
    public void InvalidMutationDefinitionsLimitsAndOverflowReturnStableFailures()
    {
        SvgPathData line = Parse("M0 0 L1 0");
        SvgMarkerGeometryOptions invalidUnits = OptionsWithEnd();
        invalidUnits.EndMarker.Units = (SvgMarkerUnits)99;
        SvgMarkerGeometryOptions invalidBounds = OptionsWithEnd();
        invalidBounds.EndMarker.Bounds.Width = -1;
        SvgMarkerGeometryOptions invalidTransform = OptionsWithEnd();
        invalidTransform.EndMarker.ContentTransform = new SvgMatrix(1, 0, 0, 1, 0, 0);
        invalidTransform.EndMarker.ContentTransform.A = double.NaN;
        SvgMarkerGeometryOptions invalidStroke = OptionsWithEnd();
        invalidStroke.StrokeWidth = -1;
        SvgMarkerGeometryOptions limited = OptionsWithEnd();
        limited.MaximumVertices = 2;
        SvgPathData invalidPath = Parse("M0 0 L1 0");
        invalidPath.Commands[1].Values[0] = double.NaN;
        SvgMarkerGeometryOptions overflow = OptionsWithEnd();
        overflow.EndMarker.Bounds = SvgRect.Create(double.MaxValue, 0, double.MaxValue, 1);

        Assert.Multiple(() =>
        {
            AssertVertexFailure(new SvgMarkerGeometry().Extract(null, 10), SvgMarkerGeometryError.InvalidArgument);
            AssertVertexFailure(new SvgMarkerGeometry().Extract(line, 0), SvgMarkerGeometryError.InvalidArgument);
            AssertGeometryFailure(line.GetMarkerGeometry(null), SvgMarkerGeometryError.InvalidArgument);
            AssertGeometryFailure(line.GetMarkerGeometry(invalidUnits), SvgMarkerGeometryError.InvalidArgument);
            AssertGeometryFailure(line.GetMarkerGeometry(invalidBounds), SvgMarkerGeometryError.InvalidArgument);
            AssertGeometryFailure(line.GetMarkerGeometry(invalidTransform), SvgMarkerGeometryError.InvalidArgument);
            AssertGeometryFailure(line.GetMarkerGeometry(invalidStroke), SvgMarkerGeometryError.InvalidArgument);
            AssertGeometryFailure(Parse("M0 0 L1 0 L2 0").GetMarkerGeometry(limited), SvgMarkerGeometryError.VertexLimitExceeded);
            AssertGeometryFailure(invalidPath.GetMarkerGeometry(OptionsWithEnd()), SvgMarkerGeometryError.InvalidPath);
            AssertGeometryFailure(line.GetMarkerGeometry(overflow), SvgMarkerGeometryError.NonFiniteResult);
        });
    }

    static SvgMarkerGeometryOptions OptionsWithEnd()
    {
        SvgMarkerGeometryOptions options = new SvgMarkerGeometryOptions();
        options.EndMarker = Definition(SvgRect.Create(0, 0, 2, 1), SvgMarkerOrientKind.Angle);
        return options;
    }

    static SvgMarkerDefinition Definition(SvgRect bounds, SvgMarkerOrientKind orient)
    {
        SvgMarkerDefinition definition = new SvgMarkerDefinition();
        definition.Bounds = bounds;
        definition.OrientKind = orient;
        return definition;
    }

    static SvgPathData Parse(string source)
    {
        SvgPathParseResult result = new SvgPathParser().Parse(source, 128, 128);
        Assert.That(result.Success, Is.True);
        return result.PathData;
    }

    static void AssertVertices(SvgMarkerVertexResult result, int count)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgMarkerGeometryError.None));
        Assert.That(result.Vertices, Has.Count.EqualTo(count));
    }

    static void AssertVertex(SvgMarkerVertex vertex, SvgMarkerPlacement placement, double x, double y, double incomingX, double incomingY, double outgoingX, double outgoingY, double angle)
    {
        Assert.That(vertex.Placement, Is.EqualTo(placement));
        Assert.That(vertex.Position.X, Is.EqualTo(x).Within(1e-10));
        Assert.That(vertex.Position.Y, Is.EqualTo(y).Within(1e-10));
        Assert.That(vertex.IncomingTangent.X, Is.EqualTo(incomingX).Within(1e-10));
        Assert.That(vertex.IncomingTangent.Y, Is.EqualTo(incomingY).Within(1e-10));
        Assert.That(vertex.OutgoingTangent.X, Is.EqualTo(outgoingX).Within(1e-10));
        Assert.That(vertex.OutgoingTangent.Y, Is.EqualTo(outgoingY).Within(1e-10));
        Assert.That(vertex.AutoAngle, Is.EqualTo(angle).Within(1e-10));
    }

    static void AssertGeometry(SvgMarkerGeometryResult result, int vertices, int instances)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.EqualTo(SvgMarkerGeometryError.None));
        Assert.That(result.Vertices, Has.Count.EqualTo(vertices));
        Assert.That(result.Instances, Has.Count.EqualTo(instances));
        Assert.That(result.Bounds, Is.Not.Null);
    }

    static void AssertRect(SvgRect rect, double x, double y, double width, double height, double tolerance = 1e-10)
    {
        Assert.That(rect.IsEmpty, Is.False);
        Assert.That(rect.X, Is.EqualTo(x).Within(tolerance));
        Assert.That(rect.Y, Is.EqualTo(y).Within(tolerance));
        Assert.That(rect.Width, Is.EqualTo(width).Within(tolerance));
        Assert.That(rect.Height, Is.EqualTo(height).Within(tolerance));
    }

    static void AssertVertexFailure(SvgMarkerVertexResult result, SvgMarkerGeometryError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Vertices, Is.Null);
    }

    static void AssertGeometryFailure(SvgMarkerGeometryResult result, SvgMarkerGeometryError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Vertices, Is.Null);
        Assert.That(result.Instances, Is.Null);
        Assert.That(result.Bounds, Is.Null);
    }
}
