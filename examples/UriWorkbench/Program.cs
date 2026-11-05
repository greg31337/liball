using uri;

var resolved = new UriResolver().ResolveReference(
    "https://example.com/docs/guide/index.html",
    "../api?q=one#intro",
    new UriResolutionOptions());

// ParseReference accepts the fragment as well as the absolute URI components.
var parsed = new uri.UriParser().ParseReference(resolved, new UriParseOptions());
Console.WriteLine($"Resolved: {resolved}");
Console.WriteLine($"Path: {parsed.Path}; query: {parsed.Query}; fragment: {parsed.Fragment}");

// Compose query parameters from unescaped values to avoid double-encoding.
var codec = new UriQueryCodec();
var query = codec.ComposeQuery(new[]
{
    new UriQueryParameter("q", true, "café & tea"),
    new UriQueryParameter("page", true, "2")
}, new UriQueryOptions());
if (!query.Success)
{
    throw new InvalidOperationException("Query composition failed.");
}

Console.WriteLine($"Encoded query: {query.Text}");
var roundTrip = codec.ParseQuery(query.Text, new UriQueryOptions());
if (!roundTrip.Success || roundTrip.Parameters[0].Value != "café & tea")
{
    throw new InvalidOperationException("Query values changed.");
}

foreach (var parameter in roundTrip.Parameters)
{
    Console.WriteLine($"  {parameter.Name} = {parameter.Value}");
}
