namespace chart.Tests;

[TestFixture]
public class ChartMemoryAllocationTests
{
    const long LargeBitmapAllocationBudget = 64L * 1024L * 1024L;
    const long LargeDataRenderAllocationBudget = 128L * 1024L * 1024L;

    [Test]
    public void LargeRawBitmapExportPath_StaysWithinAllocationBudget()
    {
        long allocated = AllocatedBytesFor(() =>
        {
            RawBitmap bitmap = new RawBitmap(512, 512, ChartPixelFormat.Rgba32);
            ChartRasterCanvas canvas = new ChartRasterCanvas(bitmap);

            canvas.Clear(ChartColor.White());
            canvas.FillRectangle(new ChartRect(32.0, 32.0, 448.0, 448.0), ChartColor.FromRgb(40, 120, 200), ChartBlendMode.Source);
            RawBitmap copy = ChartExportOperations.ToRawBitmap(bitmap);
            byte[] raw = ChartExportOperations.ToRawBytes(bitmap);
            byte[] bmp = ChartExportOperations.ToBmpBytes(bitmap, 32, true);
            byte[] png = ChartExportOperations.ToPngBytes(bitmap);

            Assert.That(copy.Buffer.Length, Is.EqualTo(bitmap.Buffer.Length));
            Assert.That(raw.Length, Is.EqualTo(512 * 512 * 4));
            Assert.That(bmp[0], Is.EqualTo((byte)'B'));
            Assert.That(bmp[1], Is.EqualTo((byte)'M'));
            Assert.That(new byte[] { png[0], png[1], png[2], png[3], png[4], png[5], png[6], png[7] }, Is.EqualTo(PngEncoder.Signature()));
        });

        Assert.That(allocated, Is.LessThan(LargeBitmapAllocationBudget));
    }

    [Test]
    public void LargeDataRenderingPath_StaysWithinAllocationBudget()
    {
        double[] xValues = Sequence(2048);
        double[] yValues = Wave(2048);

        long allocated = AllocatedBytesFor(() =>
        {
            ChartCartesianTransform transform = new ChartCartesianTransform(new ChartRect(0.0, 0.0, 160.0, 100.0), new ChartRange(0.0, 2047.0), new ChartRange(-1.0, 1.0));
            ChartCartesianRenderer renderer = new ChartCartesianRenderer(transform);
            ChartCartesianSeries line = ChartLineAreaChartsOperations.CreateLineSeries("large", xValues, yValues, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));
            ChartRenderScene scene = new ChartRenderScene(160, 100);

            scene.AddClear(ChartColor.White());
            renderer.AddSeries(line);
            renderer.RenderToScene(scene);
            string svg = scene.ToSvgString();
            RawBitmap bitmap = scene.ToRawBitmap();

            Assert.That(scene.CommandCount, Is.EqualTo(2048));
            Assert.That(svg, Does.Contain("id=\"large-line-segment-2046\""));
            Assert.That(bitmap.Width, Is.EqualTo(160));
            Assert.That(bitmap.Height, Is.EqualTo(100));
        });

        Assert.That(allocated, Is.LessThan(LargeDataRenderAllocationBudget));
    }

    static long AllocatedBytesFor(System.Action action)
    {
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();

        long before = System.GC.GetAllocatedBytesForCurrentThread();
        action();
        long after = System.GC.GetAllocatedBytesForCurrentThread();
        return after - before;
    }

    static double[] Sequence(int count)
    {
        double[] values = new double[count];
        int index = 0;
        while (index < count)
        {
            values[index] = index;
            index = index + 1;
        }

        return values;
    }

    static double[] Wave(int count)
    {
        double[] values = new double[count];
        int index = 0;
        while (index < count)
        {
            values[index] = System.Math.Sin(index * 0.05);
            index = index + 1;
        }

        return values;
    }
}
