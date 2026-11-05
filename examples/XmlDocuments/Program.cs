using xml;

// The catalog includes attributes and escaped characters in book titles.
string input = File.ReadAllText(SampleFiles.InputPath("catalog.xml"));
var parser = new XmlParser();
var document = parser.ParseXmlDocument(new XmlInput
{
    Kind = XmlInputKind.String,
    Text = input
}, new XmlParserOptions(), null, null, null);
var root = document.DocumentElement ?? throw new InvalidOperationException("Missing catalog.");
Console.WriteLine($"Root: {root.Name?.LocalName}");
foreach (var book in root.Children ?? Array.Empty<XmlNode>())
{
    if (book.Kind != XmlNodeKind.Element)
    {
        continue;
    }

    string title = string.Concat((book.Children ?? Array.Empty<XmlNode>()).Select(n => n.Value));
    string id = book.Attributes?.Single(attribute => attribute.Name?.LocalName == "id").Value ?? "";
    Console.WriteLine($"  {id}: {title}");
}

// The serializer escapes text content such as ampersands and angle brackets.
using var writer = new StringWriter();
new xml.XmlSerializer().SerializeXmlDocument(document, new XmlOutput
{
    Kind = XmlOutputKind.TextWriter,
    TextWriter = writer
}, new XmlSerializationOptions
{
    Indent = true,
    OmitXmlDeclaration = true
});

string serialized = writer.ToString();
Console.WriteLine($"\nSerialized XML:\n{serialized}");
var reopened = parser.ParseXmlDocument(new XmlInput
{
    Kind = XmlInputKind.String,
    Text = serialized
}, new XmlParserOptions(), null, null, null);
if (reopened.DocumentElement?.Name?.LocalName != "catalog")
{
    throw new InvalidOperationException("XML root changed.");
}
