using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class FillAuthoringCompletionTests
{
    [Test]
    public void MultiStopGradient_RoundTripsStopsTransparencyScalingAndRotation()
    {
        PptxColorOperations colors = new PptxColorOperations();
        PptxFillOperations fills = new PptxFillOperations();
        List<PptxGradientStop> stops = new List<PptxGradientStop>();
        stops.Add(fills.CreateGradientStop(0, colors.CreateRgb("FF0000")));
        stops.Add(fills.CreateGradientStop(35000, colors.CreateScheme("accent2")));
        stops.Add(fills.CreateGradientStop(70000, colors.CreatePreset("gold")));
        stops.Add(fills.CreateGradientStop(100000, colors.CreateHsl(240.0, 100000, 50000)));
        PptxFill fill = fills.CreateMultiStopGradient(stops, 90.0);
        fill.GradientScaled = false;
        fills.SetGradientRotation(fill, 135.0, false);
        fills.SetTransparency(fill, 25000);

        string xml = fills.ToXml(fill);
        PptxFill reopened = fills.ParseXml(xml);

        Assert.That(reopened.Type, Is.EqualTo(PptxFill.Gradient));
        Assert.That(reopened.GradientStops.Count, Is.EqualTo(4));
        Assert.That(reopened.GradientStops[1].Position, Is.EqualTo(35000));
        Assert.That(reopened.GradientStops[1].Color.Type, Is.EqualTo(PptxColor.Scheme));
        Assert.That(reopened.GradientStops[2].Color.Type, Is.EqualTo(PptxColor.Preset));
        Assert.That(reopened.GradientStops[3].Color.Type, Is.EqualTo(PptxColor.Hsl));
        Assert.That(reopened.GradientStops[0].Color.Alpha, Is.EqualTo(75000));
        Assert.That(reopened.Transparency, Is.EqualTo(25000));
        Assert.That(reopened.GradientAngleUnits, Is.EqualTo(8100000));
        Assert.That(reopened.GradientScaled, Is.False);
        Assert.That(reopened.RotateWithShape, Is.False);
        Assert.That(xml, Does.Contain("pos=\"70000\""));
    }

    [Test]
    public void PictureFill_RoundTripsCropTileTransparencyAndRotationThenSwitchesToStretch()
    {
        PptxFillOperations fills = new PptxFillOperations();
        PptxFill picture = fills.CreatePicture("rIdImage7");
        picture.PictureIsLinked = true;
        PptxImageCrop crop = new PptxImageCrop();
        crop.Left = 1000;
        crop.Top = 2000;
        crop.Right = 3000;
        crop.Bottom = 4000;
        fills.SetPictureCrop(picture, crop);
        PptxFillTile tile = new PptxFillTile();
        tile.OffsetX = 12700L;
        tile.OffsetY = -25400L;
        tile.ScaleX = 75000;
        tile.ScaleY = 50000;
        tile.Alignment = "ctr";
        tile.Flip = "xy";
        fills.SetPictureTile(picture, tile);
        fills.SetPictureRotation(picture, false);
        fills.SetTransparency(picture, 40000);

        string tileXml = fills.ToXml(picture);
        PptxFill tiled = fills.ParseXml(tileXml);

        Assert.That(tiled.PictureIsLinked, Is.True);
        Assert.That(tiled.RelationshipId, Is.EqualTo("rIdImage7"));
        Assert.That(tiled.Transparency, Is.EqualTo(40000));
        Assert.That(tiled.RotateWithShape, Is.False);
        Assert.That(tiled.Crop.Left, Is.EqualTo(1000));
        Assert.That(tiled.Crop.Bottom, Is.EqualTo(4000));
        Assert.That(tiled.UseTile, Is.True);
        Assert.That(tiled.UseStretch, Is.False);
        Assert.That(tiled.Tile.OffsetX, Is.EqualTo(12700L));
        Assert.That(tiled.Tile.OffsetY, Is.EqualTo(-25400L));
        Assert.That(tiled.Tile.ScaleX, Is.EqualTo(75000));
        Assert.That(tiled.Tile.ScaleY, Is.EqualTo(50000));
        Assert.That(tiled.Tile.Alignment, Is.EqualTo("ctr"));
        Assert.That(tiled.Tile.Flip, Is.EqualTo("xy"));
        Assert.That(tileXml, Does.Contain("alphaModFix amt=\"60000\""));
        Assert.That(tileXml, Does.Not.Contain("stretch"));

        PptxFillStretch stretch = new PptxFillStretch();
        stretch.Left = 5000;
        stretch.Top = 6000;
        stretch.Right = 7000;
        stretch.Bottom = 8000;
        fills.SetPictureStretch(tiled, stretch);
        string stretchXml = fills.ToXml(tiled);
        PptxFill stretched = fills.ParseXml(stretchXml);

        Assert.That(stretched.UseStretch, Is.True);
        Assert.That(stretched.UseTile, Is.False);
        Assert.That(stretched.Stretch.Left, Is.EqualTo(5000));
        Assert.That(stretched.Stretch.Bottom, Is.EqualTo(8000));
        Assert.That(stretchXml, Does.Contain("fillRect"));
        Assert.That(stretchXml, Does.Not.Contain("<a:tile"));
    }

    [Test]
    public void SolidPatternAndGroupFills_AuthorTransparencyAndRoundTripThroughShapes()
    {
        PptxColorOperations colors = new PptxColorOperations();
        PptxFillOperations fills = new PptxFillOperations();
        PptxFill solid = fills.CreateSolid(colors.CreatePreset("cornflowerBlue"));
        fills.SetTransparency(solid, 30000);
        PptxFill reopenedSolid = fills.ParseXml(fills.ToXml(solid));
        Assert.That(reopenedSolid.Color.Type, Is.EqualTo(PptxColor.Preset));
        Assert.That(reopenedSolid.Color.Alpha, Is.EqualTo(70000));
        Assert.That(reopenedSolid.Transparency, Is.EqualTo(30000));

        PptxFill pattern = fills.CreatePattern("diagCross", colors.CreateRgb("112233"), colors.CreateScheme("accent1"));
        fills.SetTransparency(pattern, 20000);
        PptxFill reopenedPattern = fills.ParseXml(fills.ToXml(pattern));
        Assert.That(reopenedPattern.PatternType, Is.EqualTo("diagCross"));
        Assert.That(reopenedPattern.Color.Alpha, Is.EqualTo(80000));
        Assert.That(reopenedPattern.SecondaryColor.Type, Is.EqualTo(PptxColor.Scheme));
        Assert.That(reopenedPattern.SecondaryColor.Alpha, Is.EqualTo(80000));
        Assert.That(reopenedPattern.Transparency, Is.EqualTo(20000));
        Assert.That(fills.ParseXml(fills.ToXml(fills.CreateGroup())).Type, Is.EqualTo(PptxFill.Group));

        PptxPresentation presentation = PptxDocument.CreateMinimumPresentation().LoadPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));
        shape.SetFill(pattern);
        PptxPresentation reopenedPresentation = Reopen(presentation);
        PptxFill shapeFill = reopenedPresentation.GetSlide(0).GetShapeTree().Get(0).GetFill();
        Assert.That(shapeFill.Type, Is.EqualTo(PptxFill.Pattern));
        Assert.That(shapeFill.Color.Alpha, Is.EqualTo(80000));
    }

    [Test]
    public void FillAuthoring_RejectsInvalidStopsTransparencyCropAndTileScale()
    {
        PptxColorOperations colors = new PptxColorOperations();
        PptxFillOperations fills = new PptxFillOperations();
        List<PptxGradientStop> stops = new List<PptxGradientStop>();
        stops.Add(fills.CreateGradientStop(80000, colors.CreateRgb("000000")));
        stops.Add(fills.CreateGradientStop(20000, colors.CreateRgb("FFFFFF")));
        Assert.Throws<System.ArgumentException>(() => fills.CreateMultiStopGradient(stops, 0.0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => fills.CreateGradientStop(100001, colors.CreateRgb("000000")));

        PptxFill picture = fills.CreatePicture("rId1");
        Assert.Throws<System.ArgumentOutOfRangeException>(() => fills.SetTransparency(picture, -1));
        PptxImageCrop crop = new PptxImageCrop();
        crop.Left = 60000;
        crop.Right = 60000;
        Assert.Throws<System.ArgumentException>(() => fills.SetPictureCrop(picture, crop));
        PptxFillTile tile = new PptxFillTile();
        tile.ScaleX = 0;
        Assert.Throws<System.ArgumentOutOfRangeException>(() => fills.SetPictureTile(picture, tile));
        Assert.Throws<System.ArgumentException>(() => fills.SetPictureCrop(fills.CreateGroup(), new PptxImageCrop()));
    }

    static PptxPresentation Reopen(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        return PptxDocument.OpenBytes(document.SaveBytes()).LoadPresentation();
    }
}
