namespace barcode.Tests;

public class SharedDataTests
{
    [Test]
    public void GeometryHoldsCoordinatesCornersAndBounds()
    {
        BarcodePoint topLeft = new BarcodePoint();
        topLeft.X = 1.25;
        topLeft.Y = 2.5;
        BarcodePoint topRight = new BarcodePoint();
        topRight.X = 11.75;
        topRight.Y = 2.75;
        BarcodePoint bottomRight = new BarcodePoint();
        bottomRight.X = 12.0;
        bottomRight.Y = 8.5;
        BarcodePoint bottomLeft = new BarcodePoint();
        bottomLeft.X = 1.0;
        bottomLeft.Y = 8.25;
        BarcodeRectangle bounds = new BarcodeRectangle();
        bounds.X = 1.0;
        bounds.Y = 2.5;
        bounds.Width = 11.0;
        bounds.Height = 6.0;
        BarcodeQuadrilateral region = new BarcodeQuadrilateral();
        region.TopLeft = topLeft;
        region.TopRight = topRight;
        region.BottomRight = bottomRight;
        region.BottomLeft = bottomLeft;
        region.Bounds = bounds;

        Assert.Multiple(() =>
        {
            Assert.That(topLeft.X, Is.EqualTo(1.25));
            Assert.That(topLeft.Y, Is.EqualTo(2.5));
            Assert.That(topRight.X, Is.EqualTo(11.75));
            Assert.That(topRight.Y, Is.EqualTo(2.75));
            Assert.That(bottomRight.X, Is.EqualTo(12.0));
            Assert.That(bottomRight.Y, Is.EqualTo(8.5));
            Assert.That(bottomLeft.X, Is.EqualTo(1.0));
            Assert.That(bottomLeft.Y, Is.EqualTo(8.25));
            Assert.That(bounds.X, Is.EqualTo(1.0));
            Assert.That(bounds.Y, Is.EqualTo(2.5));
            Assert.That(bounds.Width, Is.EqualTo(11.0));
            Assert.That(bounds.Height, Is.EqualTo(6.0));
            Assert.That(region.TopLeft, Is.SameAs(topLeft));
            Assert.That(region.TopRight, Is.SameAs(topRight));
            Assert.That(region.BottomRight, Is.SameAs(bottomRight));
            Assert.That(region.BottomLeft, Is.SameAs(bottomLeft));
            Assert.That(region.Bounds, Is.SameAs(bounds));
        });
    }

    [Test]
    public void PayloadHoldsRawDataEciAndStructuredAppendMetadata()
    {
        byte[] rawBytes = new byte[] { 65, 66, 67 };
        EciSegment segment = new EciSegment();
        segment.AssignmentNumber = 26;
        segment.ByteOffset = 1;
        segment.ByteCount = 2;
        EciSegment[] segments = new EciSegment[] { segment };
        StructuredAppendInfo append = new StructuredAppendInfo();
        append.Format = BarcodeFormat.DataMatrix;
        append.Index = 2;
        append.Count = 4;
        append.FileIdentifier = "FILE-7";
        append.IsLastSegment = false;
        BarcodePayload payload = new BarcodePayload();
        payload.Text = "ABC";
        payload.RawBytes = rawBytes;
        payload.CharacterEncoding = "UTF-8";
        payload.EciSegments = segments;
        payload.IsGs1 = true;
        payload.StructuredAppend = append;

        rawBytes[1] = 90;
        segment.ByteCount = 1;

        Assert.Multiple(() =>
        {
            Assert.That(payload.Text, Is.EqualTo("ABC"));
            Assert.That(payload.RawBytes, Is.SameAs(rawBytes));
            Assert.That(payload.RawBytes?[1], Is.EqualTo(90));
            Assert.That(payload.CharacterEncoding, Is.EqualTo("UTF-8"));
            Assert.That(payload.EciSegments, Is.SameAs(segments));
            Assert.That(payload.EciSegments?[0].AssignmentNumber, Is.EqualTo(26));
            Assert.That(payload.EciSegments?[0].ByteOffset, Is.EqualTo(1));
            Assert.That(payload.EciSegments?[0].ByteCount, Is.EqualTo(1));
            Assert.That(payload.IsGs1, Is.True);
            Assert.That(payload.StructuredAppend, Is.SameAs(append));
            Assert.That(append.Format, Is.EqualTo(BarcodeFormat.DataMatrix));
            Assert.That(append.Index, Is.EqualTo(2));
            Assert.That(append.Count, Is.EqualTo(4));
            Assert.That(append.FileIdentifier, Is.EqualTo("FILE-7"));
            Assert.That(append.IsLastSegment, Is.False);
        });
    }

    [Test]
    public void SizingHoldsRequestedResolvedAndQuietZoneDimensions()
    {
        BarcodeQuietZone quietZone = new BarcodeQuietZone();
        quietZone.LeftModules = 10;
        quietZone.TopModules = 2;
        quietZone.RightModules = 11;
        quietZone.BottomModules = 3;
        quietZone.LeftPixels = 20;
        quietZone.TopPixels = 4;
        quietZone.RightPixels = 22;
        quietZone.BottomPixels = 6;
        BarcodeSizing sizing = new BarcodeSizing();
        sizing.RequestedWidthModules = 95;
        sizing.RequestedHeightModules = 1;
        sizing.RequestedWidthPixels = 380;
        sizing.RequestedHeightPixels = 120;
        sizing.RequestedWidthMillimetres = 50.5;
        sizing.RequestedHeightMillimetres = 16.0;
        sizing.DotsPerInch = 300.0;
        sizing.ModuleSizeMillimetres = 0.5;
        sizing.BarHeightMillimetres = 12.0;
        sizing.TargetAspectRatio = 3.0;
        sizing.QuietZone = quietZone;
        BarcodeSize size = new BarcodeSize();
        size.WidthModules = 116;
        size.HeightModules = 4;
        size.WidthPixels = 464;
        size.HeightPixels = 128;
        size.WidthMillimetres = 52.0;
        size.HeightMillimetres = 17.0;
        size.DotsPerInch = 300.0;
        size.ModuleSizeMillimetres = 0.45;
        size.QuietZone = quietZone;

        Assert.Multiple(() =>
        {
            Assert.That(quietZone.LeftModules, Is.EqualTo(10));
            Assert.That(quietZone.TopModules, Is.EqualTo(2));
            Assert.That(quietZone.RightModules, Is.EqualTo(11));
            Assert.That(quietZone.BottomModules, Is.EqualTo(3));
            Assert.That(quietZone.LeftPixels, Is.EqualTo(20));
            Assert.That(quietZone.TopPixels, Is.EqualTo(4));
            Assert.That(quietZone.RightPixels, Is.EqualTo(22));
            Assert.That(quietZone.BottomPixels, Is.EqualTo(6));
            Assert.That(sizing.RequestedWidthModules, Is.EqualTo(95));
            Assert.That(sizing.RequestedHeightModules, Is.EqualTo(1));
            Assert.That(sizing.RequestedWidthPixels, Is.EqualTo(380));
            Assert.That(sizing.RequestedHeightPixels, Is.EqualTo(120));
            Assert.That(sizing.RequestedWidthMillimetres, Is.EqualTo(50.5));
            Assert.That(sizing.RequestedHeightMillimetres, Is.EqualTo(16.0));
            Assert.That(sizing.DotsPerInch, Is.EqualTo(300.0));
            Assert.That(sizing.ModuleSizeMillimetres, Is.EqualTo(0.5));
            Assert.That(sizing.BarHeightMillimetres, Is.EqualTo(12.0));
            Assert.That(sizing.TargetAspectRatio, Is.EqualTo(3.0));
            Assert.That(sizing.QuietZone, Is.SameAs(quietZone));
            Assert.That(size.WidthModules, Is.EqualTo(116));
            Assert.That(size.HeightModules, Is.EqualTo(4));
            Assert.That(size.WidthPixels, Is.EqualTo(464));
            Assert.That(size.HeightPixels, Is.EqualTo(128));
            Assert.That(size.WidthMillimetres, Is.EqualTo(52.0));
            Assert.That(size.HeightMillimetres, Is.EqualTo(17.0));
            Assert.That(size.DotsPerInch, Is.EqualTo(300.0));
            Assert.That(size.ModuleSizeMillimetres, Is.EqualTo(0.45));
            Assert.That(size.QuietZone, Is.SameAs(quietZone));
        });
    }

    [Test]
    public void TextLayoutHoldsRunsBoundsAndResolvedSize()
    {
        BarcodeTextRun run = new BarcodeTextRun();
        run.Text = "(01) 09501101530003";
        run.X = 4.0;
        run.BaselineY = 92.0;
        run.Width = 140.0;
        run.Height = 12.0;
        BarcodeTextRun[] runs = new BarcodeTextRun[] { run };
        BarcodeRectangle bounds = new BarcodeRectangle();
        bounds.X = 0.0;
        bounds.Y = 80.0;
        bounds.Width = 148.0;
        bounds.Height = 16.0;
        BarcodeSize size = new BarcodeSize();
        size.WidthPixels = 148;
        size.HeightPixels = 96;
        BarcodeTextLayout layout = new BarcodeTextLayout();
        layout.Runs = runs;
        layout.ReservedHeight = 16.0;
        layout.Bounds = bounds;
        layout.Size = size;

        Assert.Multiple(() =>
        {
            Assert.That(run.Text, Is.EqualTo("(01) 09501101530003"));
            Assert.That(run.X, Is.EqualTo(4.0));
            Assert.That(run.BaselineY, Is.EqualTo(92.0));
            Assert.That(run.Width, Is.EqualTo(140.0));
            Assert.That(run.Height, Is.EqualTo(12.0));
            Assert.That(layout.Runs, Is.SameAs(runs));
            Assert.That(layout.ReservedHeight, Is.EqualTo(16.0));
            Assert.That(layout.Bounds, Is.SameAs(bounds));
            Assert.That(layout.Size, Is.SameAs(size));
        });
    }

    [Test]
    public void ConfigurationHoldsSharedDataObjects()
    {
        BarcodePayload payload = new BarcodePayload();
        BarcodeSizing sizing = new BarcodeSizing();
        BarcodeSize size = new BarcodeSize();
        BarcodeTextLayout textLayout = new BarcodeTextLayout();
        BarcodeQuadrilateral region = new BarcodeQuadrilateral();
        BarcodeConfiguration configuration = new BarcodeConfiguration();
        configuration.Payload = payload;
        configuration.Sizing = sizing;
        configuration.ResolvedSize = size;
        configuration.TextLayout = textLayout;
        configuration.Region = region;

        Assert.Multiple(() =>
        {
            Assert.That(configuration.Payload, Is.SameAs(payload));
            Assert.That(configuration.Sizing, Is.SameAs(sizing));
            Assert.That(configuration.ResolvedSize, Is.SameAs(size));
            Assert.That(configuration.TextLayout, Is.SameAs(textLayout));
            Assert.That(configuration.Region, Is.SameAs(region));
        });
    }
}
