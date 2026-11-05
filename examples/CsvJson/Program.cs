using System.Globalization;
using csv;
using json;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// Named columns let the report ignore the order ID and date columns.
string input = File.ReadAllText(SampleFiles.InputPath("cafe-sales.csv"));
Console.WriteLine("CSV sales report -> JSON");
Console.WriteLine($"{"Product",-20} {"Qty",3} {"Price",8} {"Total",8}");
using var source = new StringReader(input);
var options = new CsvReaderOptions();
options.HeaderOptions.HasHeader = true;
var reader = new CsvReader(source, options);
if (!reader.ReadHeader())
{
    throw new InvalidOperationException("The CSV header is missing.");
}

var values = new JsonValueFactory();
var items = new List<JsonValue>();
decimal total = 0;
while (reader.Read())
{
    string product = reader.GetFieldByName("Product")
        ?? throw new InvalidOperationException("Missing product.");
    int quantity = int.Parse(reader.GetFieldByName("Quantity") ?? "", CultureInfo.InvariantCulture);
    decimal price = decimal.Parse(reader.GetFieldByName("UnitPrice") ?? "", CultureInfo.InvariantCulture);
    decimal lineTotal = quantity * price;
    total += lineTotal;
    Console.WriteLine($"{product,-20} {quantity,3} {price,8:F2} {lineTotal,8:F2}");
    items.Add(values.CreateObject(new List<JsonMember>
    {
        new()
        {
            Name = "product",
            Value = values.CreateString(product)
        },
        new()
        {
            Name = "quantity",
            Value = values.CreateNumber(quantity.ToString(CultureInfo.InvariantCulture))
        },
        new()
        {
            Name = "total",
            Value = values.CreateNumber(lineTotal.ToString("F2", CultureInfo.InvariantCulture))
        }
    }));
}

reader.Close();

// Store totals as JSON numbers using invariant formatting, not localized strings.
var root = values.CreateObject(new List<JsonMember>
{
    new()
    {
        Name = "items",
        Value = values.CreateArray(items)
    },
    new()
    {
        Name = "grandTotal",
        Value = values.CreateNumber(total.ToString("F2", CultureInfo.InvariantCulture))
    }
});

var document = new JsonDocument
{
    Root = root
};

string output = new JsonDocumentSerializer().SerializeString(document, new JsonWriterOptions
{
    Formatting = JsonFormatting.Pretty,
    Indentation = "  "
});

Console.WriteLine($"Grand total: {total:F2}\n");
Console.WriteLine(output);
var reopened = new JsonDocumentParser().ParseString(output, new JsonReaderOptions());
if (reopened.Root is null ||
    !new JsonValueComparer().CompareValues(root, reopened.Root, JsonComparisonMode.JsonSemantic))
{
    throw new InvalidOperationException("JSON values changed during serialization.");
}

Console.WriteLine("JSON round trip: values preserved.");
