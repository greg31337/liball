using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class GpsCoordinateTests
{
    [Test]
    public void ToDecimalDegrees_ConvertsDegreesMinutesSecondsAndAppliesReference()
    {
        double north = GpsCoordinate.ToDecimalDegrees(45, 30, 0, "N");
        double south = GpsCoordinate.ToDecimalDegrees(45, 30, 0, "S");
        double west = GpsCoordinate.ToDecimalDegrees(122, 15, 0, "W");
        double signed = GpsCoordinate.ToDecimalDegrees(-10, 30, 0, "");

        Assert.That(north, Is.EqualTo(45.5).Within(0.000001));
        Assert.That(south, Is.EqualTo(-45.5).Within(0.000001));
        Assert.That(west, Is.EqualTo(-122.25).Within(0.000001));
        Assert.That(signed, Is.EqualTo(-10.5).Within(0.000001));
    }

    [Test]
    public void FromDegreesMinutesSeconds_CreatesSignedCoordinateWithReferences()
    {
        IImageMetadataFactory factory = new ImageMetadataFactory();
        GpsCoordinate coordinate = factory.CreateGpsCoordinate(45, 30, 0, "S", 122, 15, 0, "W", 15, "1");

        Assert.That(coordinate.IsValid, Is.True);
        Assert.That(coordinate.Latitude, Is.EqualTo(-45.5).Within(0.000001));
        Assert.That(coordinate.Longitude, Is.EqualTo(-122.25).Within(0.000001));
        Assert.That(coordinate.Altitude, Is.EqualTo(-15).Within(0.000001));
        Assert.That(coordinate.LatitudeReference, Is.EqualTo("S"));
        Assert.That(coordinate.LongitudeReference, Is.EqualTo("W"));
        Assert.That(coordinate.AltitudeReference, Is.EqualTo("1"));
    }

    [Test]
    public void FormatDisplay_FormatsCoordinatesUsingCardinalReferences()
    {
        GpsCoordinate coordinate = new GpsCoordinate();
        coordinate.IsValid = true;
        coordinate.Latitude = -45.5;
        coordinate.Longitude = 122.25;
        coordinate.Altitude = 62.5;

        IImageMetadataFormatter formatter = new ImageMetadataFormatter();
        string text = formatter.ToGpsText(coordinate);

        Assert.That(text, Is.EqualTo("45.5 S, 122.25 E, 62.5 m"));
    }

    [Test]
    public void FormatDisplay_PrefersExplicitLatitudeAndLongitudeReferences()
    {
        GpsCoordinate coordinate = new GpsCoordinate();
        coordinate.IsValid = true;
        coordinate.Latitude = 45.5;
        coordinate.Longitude = 122.25;
        coordinate.LatitudeReference = "S";
        coordinate.LongitudeReference = "W";

        IImageMetadataFormatter formatter = new ImageMetadataFormatter();
        string text = formatter.ToGpsText(coordinate);

        Assert.That(text, Is.EqualTo("45.5 S, 122.25 W"));
    }

    [Test]
    public void FormatDisplay_ReturnsEmptyForNullOrInvalidCoordinate()
    {
        GpsCoordinate coordinate = new GpsCoordinate();

        IImageMetadataFormatter formatter = new ImageMetadataFormatter();

        Assert.That(formatter.ToGpsText(null), Is.EqualTo(""));
        Assert.That(formatter.ToGpsText(coordinate), Is.EqualTo(""));
    }
}
