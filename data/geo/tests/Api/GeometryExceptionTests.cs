using geo;

namespace geo.Tests;

public class GeometryExceptionTests
{
    [Test]
    public void CustomExceptionsPreserveActionableMessages()
    {
        GeometryException geometry = new GeometryException("Geometry operation failed");
        GeometryParseException parse = new GeometryParseException("Unexpected token at offset 12");
        InvalidGeometryException invalid = new InvalidGeometryException("Polygon shell is not closed");
        TopologyException topology = new TopologyException("Overlay graph is not fully noded");
        PrecisionException precision = new PrecisionException("Fixed precision scale must be positive");
        GeoJsonException geoJson = new GeoJsonException("GeoJSON position contains a non-finite ordinate");
        SpatialIndexException index = new SpatialIndexException("Index cannot be modified after packing");

        Assert.Multiple(() =>
        {
            Assert.That(geometry.Message, Is.EqualTo("Geometry operation failed"));
            Assert.That(parse.Message, Is.EqualTo("Unexpected token at offset 12"));
            Assert.That(invalid.Message, Is.EqualTo("Polygon shell is not closed"));
            Assert.That(topology.Message, Is.EqualTo("Overlay graph is not fully noded"));
            Assert.That(precision.Message, Is.EqualTo("Fixed precision scale must be positive"));
            Assert.That(geoJson.Message, Is.EqualTo("GeoJSON position contains a non-finite ordinate"));
            Assert.That(index.Message, Is.EqualTo("Index cannot be modified after packing"));
        });
    }

    [Test]
    public void CustomExceptionsRetainTheirDomainTypeWhenThrown()
    {
        Assert.Throws<GeometryException>(() => throw new GeometryException("geometry"));
        Assert.Throws<GeometryParseException>(() => throw new GeometryParseException("parse"));
        Assert.Throws<InvalidGeometryException>(() => throw new InvalidGeometryException("invalid"));
        Assert.Throws<TopologyException>(() => throw new TopologyException("topology"));
        Assert.Throws<PrecisionException>(() => throw new PrecisionException("precision"));
        Assert.Throws<GeoJsonException>(() => throw new GeoJsonException("geojson"));
        Assert.Throws<SpatialIndexException>(() => throw new SpatialIndexException("index"));
    }
}
