using json;

var document = new JsonDocumentParser().ParseString(
    """{"users":[{"name":"Ada"},{"name":"Grace"}],"a/b":{"~key":42}}""",
    new JsonReaderOptions());
var root = document.Root ?? throw new InvalidOperationException("No JSON root.");
var codec = new JsonPointerCodec();
var navigator = new JsonPointerNavigator();
var serializer = new JsonDocumentSerializer();
Console.WriteLine("JSON Pointer lookups");

// JSON Pointer escapes a slash as ~1 and a literal tilde as ~0.
foreach (string path in new[] { "/users/0/name", "/users/1/name", "/a~1b/~0key" })
{
    var pointer = codec.ParseStringPointer(path);
    var value = navigator.ResolvePointer(root, pointer, JsonDuplicateKeyPolicy.Reject);
    string text = serializer.SerializeString(new JsonDocument { Root = value }, new JsonWriterOptions());
    Console.WriteLine($"{path,-18} -> {text}");
}

// TryResolvePointer reports missing paths without requiring an exception.
var missing = navigator.TryResolvePointer(root, codec.ParseStringPointer("/users/9"), JsonDuplicateKeyPolicy.Reject);
Console.WriteLine($"Missing array element found: {missing.Success}");
