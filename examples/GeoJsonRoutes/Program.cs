using geo;

// Keep the feature metadata together with its longitude/latitude geometry.
string input = File.ReadAllText(SampleFiles.InputPath("river-walk.geojson"));
var codec = new GeoJsonCodec(new GeoJsonReadOptions(), new GeoJsonWriteOptions());
var document = codec.ReadGeoJson(input);
var feature = document.Feature ?? throw new InvalidOperationException("Expected a GeoJSON feature.");
var geometry = feature.Geometry?.Value ?? throw new InvalidOperationException("Expected a route geometry.");
Console.WriteLine($"Route: {feature.Properties.GetProperty("name").GetString()}");
Console.WriteLine($"WKT: {new WktCodec(new WktReadOptions(), new WktWriteOptions()).WriteWkt(geometry)}");
Console.WriteLine("Coordinates are longitude/latitude degrees; no planar distance is reported.");
string saved = codec.WriteGeoJson(document);
Console.WriteLine($"\nSerialized GeoJSON:\n{saved}");
var reopened = codec.ReadGeoJson(saved);
if (reopened.Feature?.Properties.GetProperty("name").GetString() != "River walk")
{
    throw new InvalidOperationException("Route properties changed.");
}
