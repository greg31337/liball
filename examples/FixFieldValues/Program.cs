using System.Text;
using fix;

var codec = new FixFieldValueCodec();

// Each FIX tag is paired with the type needed to interpret its ASCII value.
var fields = new[]
{
    (Tag: 38, Type: FixFieldType.Qty, Text: "100"),
    (Tag: 44, Type: FixFieldType.Price, Text: "123.45"),
    (Tag: 43, Type: FixFieldType.Boolean, Text: "N"),
    (Tag: 52, Type: FixFieldType.UtcTimestamp, Text: "20260105-09:30:00.123")
};

Console.WriteLine("FIX field values (synthetic message data)");
foreach (var field in fields)
{
    var decoded = codec.DecodeFieldValue(
        Encoding.ASCII.GetBytes(field.Text), field.Tag, field.Type, new DecodeOptions());
    if (decoded.Status != DecodeStatus.Success || decoded.Field is null)
    {
        throw new InvalidOperationException(decoded.ErrorMessage);
    }

    byte[] buffer = new byte[128];
    var saved = codec.EncodeFieldValue(decoded.Field, field.Type, buffer, new EncodeOptions());
    if (saved.Status != EncodeStatus.Success)
    {
        throw new InvalidOperationException(saved.ErrorMessage);
    }

    string text = Encoding.ASCII.GetString(buffer, 0, saved.BytesWritten);
    Console.WriteLine($"Tag {field.Tag,2} {field.Type,-12} {text}");
    if (text != field.Text)
    {
        throw new InvalidOperationException("FIX field changed.");
    }
}

// Malformed numeric text should produce a decode failure, not a price.
var invalid = codec.DecodeFieldValue(Encoding.ASCII.GetBytes("12.3.4"), 44, FixFieldType.Price, new DecodeOptions());
Console.WriteLine($"Malformed price '12.3.4': {invalid.Status}");
