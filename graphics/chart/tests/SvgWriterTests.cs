namespace chart.Tests;

[TestFixture]
public class SvgWriterTests
{
    [Test]
    public void ToSvgString_WritesRootMetadataAndClosesDocument()
    {
        SvgWriter writer = new SvgWriter(100, 50);

        writer.AddTitleDescription("A&B <Chart>", "Quote \" stays");
        writer.AddMetadata("raw <meta> & data");

        string svg = writer.ToSvgString();

        Assert.That(svg, Is.EqualTo("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"50\" viewBox=\"0 0 100 50\"><title>A&amp;B &lt;Chart&gt;</title><desc>Quote \" stays</desc><metadata>raw &lt;meta&gt; &amp; data</metadata></svg>"));
        AssertValidXml(svg);
    }

    [Test]
    public void EscapeAttributeAndText_EscapeExpectedXmlCharacters()
    {
        Assert.That(SvgWriter.EscapeText("<A&B> \"quote\""), Is.EqualTo("&lt;A&amp;B&gt; \"quote\""));
        Assert.That(SvgWriter.EscapeAttribute("<A&B> \"quote\" 'single'"), Is.EqualTo("&lt;A&amp;B&gt; &quot;quote&quot; &apos;single&apos;"));
    }

    [Test]
    public void CreateId_ReturnsDeterministicSanitizedIds()
    {
        SvgWriter writer = new SvgWriter(10, 10);

        Assert.That(writer.CreateId("clip path"), Is.EqualTo("clip-path-1"));
        Assert.That(writer.CreateId("clip path"), Is.EqualTo("clip-path-2"));
        Assert.That(writer.CreateId(""), Is.EqualTo("id-3"));
    }

    [Test]
    public void BeginGroup_WritesTransformStyleAndNestedMarkup()
    {
        SvgWriter writer = new SvgWriter(20, 20);
        SvgStyle style = new SvgStyle();

        style.SetFill(new ChartColor(255, 0, 0, 128));
        style.SetStroke(new ChartColor(0, 0, 255, 255), 2.5);
        style.SetOpacity(0.75);
        style.SetFont("Arial & Sans", 12);

        writer.BeginGroup("series-1", SvgWriter.Translate(2.5, 4.0), style);
        writer.AppendRawElement("<path d=\"M0 0\"/>");
        writer.EndGroup();

        string svg = writer.ToSvgString();

        Assert.That(svg, Does.Contain("<g id=\"series-1\" transform=\"translate(2.5 4)\" style=\"fill:#FF0000;fill-opacity:0.502;stroke:#0000FF;stroke-width:2.5;opacity:0.75;font-family:Arial &amp; Sans;font-size:12\">"));
        Assert.That(svg, Does.Contain("<path d=\"M0 0\"/></g></svg>"));
        AssertValidXml(svg);
    }

    [Test]
    public void SvgStyle_SerializesNoFillAndNoStroke()
    {
        SvgStyle style = new SvgStyle();

        style.SetNoFill();
        style.SetNoStroke();

        Assert.That(style.ToStyleString(), Is.EqualTo("fill:none;stroke:none"));
    }

    [Test]
    public void TransformHelpers_UseInvariantCompactNumbers()
    {
        Assert.That(SvgWriter.Translate(1.25, 2.0), Is.EqualTo("translate(1.25 2)"));
        Assert.That(SvgWriter.Scale(2.0, 3.5), Is.EqualTo("scale(2 3.5)"));
        Assert.That(SvgWriter.Rotate(45.0, 10.0, 20.0), Is.EqualTo("rotate(45 10 20)"));
        Assert.That(SvgWriter.Matrix(new ChartMatrix(1.0, 2.0, 3.0, 4.0, 5.0, 6.0)), Is.EqualTo("matrix(1 2 3 4 5 6)"));
        Assert.That(SvgWriter.FormatDouble(-0.00000001), Is.EqualTo("0"));
    }

    [Test]
    public void ToSvgString_AutoClosesOpenGroups()
    {
        SvgWriter writer = new SvgWriter(10, 10);

        writer.BeginGroup("outer", "", null!);
        writer.BeginGroup("inner", "", null!);

        Assert.That(writer.ToSvgString(), Does.EndWith("</g></g></svg>"));
        Assert.That(writer.OpenGroups, Is.EqualTo(0));
    }

    [Test]
    public void PrimitiveWriters_OutputShapeElements()
    {
        SvgWriter writer = new SvgWriter(40, 30);
        SvgStyle style = new SvgStyle();
        ChartPoint[] points = new ChartPoint[]
        {
            new ChartPoint(1.0, 2.0),
            new ChartPoint(3.0, 4.5),
            new ChartPoint(5.0, 6.0)
        };

        style.SetNoFill();
        style.SetStroke(ChartColor.Black(), 1.5);

        writer.WriteLine(1.0, 2.0, 3.0, 4.0, style, "line-1", "");
        writer.WritePolyline(points, style, "polyline-1", "");
        writer.WritePolygon(points, style, "polygon-1", "");
        writer.WriteRect(1.0, 2.0, 10.0, 8.0, 2.0, 3.0, style, "rect-1", SvgWriter.Translate(1.0, 2.0));
        writer.WriteCircle(5.0, 6.0, 3.0, style, "circle-1", "");
        writer.WriteEllipse(7.0, 8.0, 4.0, 2.0, style, "ellipse-1", "");
        writer.WriteText("A&B", 9.0, 10.0, style, "text-1", "");

        string svg = writer.ToSvgString();

        Assert.That(svg, Does.Contain("<line id=\"line-1\" x1=\"1\" y1=\"2\" x2=\"3\" y2=\"4\" style=\"fill:none;stroke:#000000;stroke-width:1.5\"/>"));
        Assert.That(svg, Does.Contain("<polyline id=\"polyline-1\" points=\"1,2 3,4.5 5,6\" style=\"fill:none;stroke:#000000;stroke-width:1.5\"/>"));
        Assert.That(svg, Does.Contain("<polygon id=\"polygon-1\" points=\"1,2 3,4.5 5,6\" style=\"fill:none;stroke:#000000;stroke-width:1.5\"/>"));
        Assert.That(svg, Does.Contain("<rect id=\"rect-1\" x=\"1\" y=\"2\" width=\"10\" height=\"8\" rx=\"2\" ry=\"3\" transform=\"translate(1 2)\" style=\"fill:none;stroke:#000000;stroke-width:1.5\"/>"));
        Assert.That(svg, Does.Contain("<circle id=\"circle-1\" cx=\"5\" cy=\"6\" r=\"3\" style=\"fill:none;stroke:#000000;stroke-width:1.5\"/>"));
        Assert.That(svg, Does.Contain("<ellipse id=\"ellipse-1\" cx=\"7\" cy=\"8\" rx=\"4\" ry=\"2\" style=\"fill:none;stroke:#000000;stroke-width:1.5\"/>"));
        Assert.That(svg, Does.Contain("<text id=\"text-1\" x=\"9\" y=\"10\" style=\"fill:none;stroke:#000000;stroke-width:1.5\">A&amp;B</text>"));
        AssertValidXml(svg);
    }

    [Test]
    public void WritePath_OutputsPathDataForAllCommandKinds()
    {
        SvgWriter writer = new SvgWriter(20, 20);
        ChartPath path = new ChartPath();

        path.MoveTo(1.0, 2.0);
        path.LineTo(3.0, 4.0);
        path.QuadraticBezierTo(5.0, 6.0, 7.0, 8.0);
        path.CubicBezierTo(9.0, 10.0, 11.0, 12.0, 13.0, 14.0);
        path.Close();

        writer.WritePath(path, null!, "path-1", "");

        string svg = writer.ToSvgString();

        Assert.That(SvgWriter.PathData(path), Is.EqualTo("M 1 2 L 3 4 Q 5 6 7 8 C 9 10 11 12 13 14 Z"));
        Assert.That(svg, Does.Contain("<path id=\"path-1\" d=\"M 1 2 L 3 4 Q 5 6 7 8 C 9 10 11 12 13 14 Z\"/>"));
        AssertValidXml(svg);
    }

    [Test]
    public void DefinitionWriters_OutputClipPathGradientAndPattern()
    {
        SvgWriter writer = new SvgWriter(30, 30);
        SvgStyle fill = new SvgStyle();
        fill.SetFill(ChartColor.White());

        writer.WriteClipPathRect("clip-1", 1.0, 2.0, 3.0, 4.0);
        writer.WriteLinearGradient(
            "grad-1",
            0.0,
            0.0,
            10.0,
            0.0,
            new ChartColor[] { ChartColor.Black(), new ChartColor(255, 0, 0, 128) },
            new double[] { 0.0, 1.0 });
        writer.BeginPattern("pattern-1", 4.0, 4.0);
        writer.WriteRect(0.0, 0.0, 2.0, 2.0, 0.0, 0.0, fill, "", "");
        writer.EndPattern();

        string svg = writer.ToSvgString();

        Assert.That(svg, Does.Contain("<clipPath id=\"clip-1\"><rect x=\"1\" y=\"2\" width=\"3\" height=\"4\"/></clipPath>"));
        Assert.That(svg, Does.Contain("<linearGradient id=\"grad-1\" x1=\"0\" y1=\"0\" x2=\"10\" y2=\"0\"><stop offset=\"0%\" stop-color=\"#000000\"/><stop offset=\"100%\" stop-color=\"#FF0000\" stop-opacity=\"0.502\"/></linearGradient>"));
        Assert.That(svg, Does.Contain("<pattern id=\"pattern-1\" patternUnits=\"userSpaceOnUse\" width=\"4\" height=\"4\"><rect x=\"0\" y=\"0\" width=\"2\" height=\"2\" style=\"fill:#FFFFFF\"/></pattern>"));
        AssertValidXml(svg);
    }

    [Test]
    public void ClipPathDefinitions_CanUseDeterministicIdsInValidClipReferences()
    {
        SvgWriter writer = new SvgWriter(30, 30);
        SvgWriter second = new SvgWriter(30, 30);
        SvgStyle fill = new SvgStyle();
        string clipId = writer.CreateId("plot clip");

        fill.SetFill(ChartColor.Black());
        writer.WriteClipPathRect(clipId, 2.0, 3.0, 10.0, 8.0);
        writer.AppendRawElement("<g clip-path=\"url(#" + clipId + ")\">");
        writer.WriteRect(0.0, 0.0, 20.0, 20.0, 0.0, 0.0, fill, "clipped-rect", "");
        writer.AppendRawElement("</g>");

        string svg = writer.ToSvgString();

        Assert.That(second.CreateId("plot clip"), Is.EqualTo(clipId));
        Assert.That(clipId, Is.EqualTo("plot-clip-1"));
        Assert.That(svg, Does.Contain("<clipPath id=\"plot-clip-1\"><rect x=\"2\" y=\"3\" width=\"10\" height=\"8\"/></clipPath>"));
        Assert.That(svg, Does.Contain("<g clip-path=\"url(#plot-clip-1)\"><rect id=\"clipped-rect\" x=\"0\" y=\"0\" width=\"20\" height=\"20\" style=\"fill:#000000\"/></g>"));
        AssertValidXml(svg);
    }

    [Test]
    public void RenderSceneSvg_OutputIsValidXmlWithEscapedText()
    {
        ChartRenderScene scene = new ChartRenderScene(30, 20);
        ChartRenderStyle style = ChartRenderStyle.CreateFill(ChartColor.Black());
        scene.AddText("label", "A&B <C>", 2.0, 10.0, style);
        scene.AddRect("box", 1.0, 2.0, 8.0, 6.0, 0.0, 0.0, ChartRenderStyle.CreateFill(ChartColor.White()));

        string svg = scene.ToSvgString();

        Assert.That(svg, Does.Contain("A&amp;B &lt;C&gt;"));
        AssertValidXml(svg);
    }

    [Test]
    public void PrimitiveHelpers_ValidateInputs()
    {
        SvgWriter writer = new SvgWriter(10, 10);

        Assert.Throws<System.ArgumentNullException>(() => SvgWriter.PointsData(null!));
        Assert.Throws<System.ArgumentException>(() => writer.WritePolyline(new ChartPoint[] { new ChartPoint(1.0, 1.0) }, null!, "", ""));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => writer.WriteRect(0.0, 0.0, 0.0, 1.0, 0.0, 0.0, null!, "", ""));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => writer.WriteCircle(1.0, 1.0, 0.0, null!, "", ""));
        Assert.Throws<System.ArgumentNullException>(() => writer.WriteText(null!, 0.0, 0.0, null!, "", ""));
        Assert.Throws<System.ArgumentNullException>(() => writer.WritePath(null!, null!, "", ""));
        Assert.Throws<System.ArgumentException>(() => writer.WriteLinearGradient("bad", 0.0, 0.0, 1.0, 0.0, new ChartColor[] { ChartColor.Black() }, new double[] { 0.0, 1.0 }));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => writer.BeginPattern("bad", 0.0, 1.0));
    }

    [Test]
    public void SvgWriter_ValidatesInputs()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new SvgWriter(0, 10));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new SvgWriter(10, 0));
        Assert.Throws<System.ArgumentNullException>(() => SvgWriter.EscapeText(null!));
        Assert.Throws<System.ArgumentNullException>(() => SvgWriter.EscapeAttribute(null!));
        Assert.Throws<System.ArgumentNullException>(() => new SvgWriter(10, 10).AddMetadata(null!));
        Assert.Throws<System.InvalidOperationException>(() => new SvgWriter(10, 10).EndGroup());
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new SvgStyle().SetOpacity(1.5));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new SvgStyle().SetStroke(ChartColor.Black(), -1.0));
    }

    static void AssertValidXml(string svg)
    {
        System.Xml.XmlDocument document = new System.Xml.XmlDocument();
        document.LoadXml(svg);
        Assert.That(document.DocumentElement!.Name, Is.EqualTo("svg"));
    }
}
