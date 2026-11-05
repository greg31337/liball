using System;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class TagValueMessageEncoderTests
{
    [Test]
    public void EmptyDestinationMeasuresExactSizeAndExactDestinationWritesAValidFrame()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage message = CreateMessage("ORDER-1", "PUBLIC ENCODER");
        IMessageEncoder encoder = new TagValueMessageEncoder();

        EncodeResult measured = encoder.EncodeMessage(message, dictionary, Span<byte>.Empty, new EncodeOptions());
        byte[] destination = new byte[measured.RequiredSize];
        EncodeResult written = encoder.EncodeMessage(message, dictionary, destination, new EncodeOptions());
        BodyLengthResult bodyLength = BodyLengthCalculator.CalculateBodyLength(destination);
        int expectedChecksum = ChecksumCalculator.CalculateChecksum(destination.AsSpan(0, bodyLength.ChecksumStart));
        AsciiIntegerResult actualChecksum = ChecksumCalculator.ParseChecksum(destination.AsSpan(bodyLength.ChecksumStart + 3, 3));
        DecodeResult decoded = new TagValueMessageDecoder().DecodeMessage(destination, dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(measured.Status, Is.EqualTo(EncodeStatus.DestinationTooSmall));
            Assert.That(measured.BytesWritten, Is.Zero);
            Assert.That(measured.RequiredSize, Is.GreaterThan(0));
            Assert.That(measured.ErrorCode, Is.EqualTo("destination-too-small"));
            Assert.That(written.Status, Is.EqualTo(EncodeStatus.Success), written.ErrorCode);
            Assert.That(written.BytesWritten, Is.EqualTo(destination.Length));
            Assert.That(written.RequiredSize, Is.EqualTo(destination.Length));
            Assert.That(bodyLength.Success, Is.True);
            Assert.That(bodyLength.Matches, Is.True);
            Assert.That(actualChecksum.Value, Is.EqualTo(expectedChecksum));
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), decoded.ErrorCode);
            Assert.That(decoded.Message!.MsgType, Is.EqualTo("D"));
            Assert.That(Text(decoded.Message.BodyFields.Find(field => field.Tag == 11)!), Is.EqualTo("ORDER-1"));
            Assert.That(Text(decoded.Message.BodyFields.Find(field => field.Tag == 58)!), Is.EqualTo("PUBLIC ENCODER"));
        });
    }

    [Test]
    public void InsufficientDestinationIsUnchangedAndReportsCompleteRequiredSize()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage message = CreateMessage("ORDER-2", "NO PARTIAL WRITE");
        TagValueMessageEncoder encoder = new TagValueMessageEncoder();
        EncodeResult measured = encoder.EncodeMessage(message, dictionary, Span<byte>.Empty, new EncodeOptions());
        byte[] destination = new byte[measured.RequiredSize - 1];
        Array.Fill(destination, (byte)88);
        byte[] original = destination.AsSpan().ToArray();

        EncodeResult result = encoder.EncodeMessage(message, dictionary, destination, new EncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(EncodeStatus.DestinationTooSmall));
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(result.RequiredSize, Is.EqualTo(measured.RequiredSize));
            Assert.That(result.ErrorCode, Is.EqualTo("destination-too-small"));
            Assert.That(result.ErrorMessage, Is.Not.Empty);
            Assert.That(destination, Is.EqualTo(original));
        });
    }

    [Test]
    public void OversizedDestinationWritesOnlyTheReportedPrefix()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage message = CreateMessage("ORDER-3", "PREFIX ONLY");
        TagValueMessageEncoder encoder = new TagValueMessageEncoder();
        EncodeResult measured = encoder.EncodeMessage(message, dictionary, Span<byte>.Empty, new EncodeOptions());
        byte[] destination = new byte[measured.RequiredSize + 5];
        Array.Fill(destination, (byte)88);

        EncodeResult result = encoder.EncodeMessage(message, dictionary, destination, new EncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(EncodeStatus.Success), result.ErrorCode);
            Assert.That(result.BytesWritten, Is.EqualTo(measured.RequiredSize));
            Assert.That(result.RequiredSize, Is.EqualTo(measured.RequiredSize));
            Assert.That(destination[result.BytesWritten - 1], Is.EqualTo(FixDefaults.Soh));
            Assert.That(destination.AsSpan(result.BytesWritten).ToArray(), Is.EqualTo(new byte[] { 88, 88, 88, 88, 88 }));
        });
    }

    [Test]
    public void EncodingOptionsFlowThroughThePublicEncoder()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage message = CreateMessage("ORDER-4", "SECOND");
        message.BodyFields = [Field(58, "FIRST"), Field(11, "ORDER-4"), Field(999, "DROP")];
        TagValueMessageEncoder encoder = new TagValueMessageEncoder();
        EncodeOptions options = new EncodeOptions { PreserveFieldOrder = false, IncludeUnknownFields = false };
        EncodeResult measured = encoder.EncodeMessage(message, dictionary, Span<byte>.Empty, options);
        byte[] destination = new byte[measured.RequiredSize];

        EncodeResult result = encoder.EncodeMessage(message, dictionary, destination, options);
        string wire = Encoding.Latin1.GetString(destination).Replace('\u0001', '|');

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(EncodeStatus.Success), result.ErrorCode);
            Assert.That(wire.IndexOf("11=ORDER-4", StringComparison.Ordinal), Is.LessThan(wire.IndexOf("58=FIRST", StringComparison.Ordinal)));
            Assert.That(wire, Does.Not.Contain("999=DROP"));
        });
    }

    [Test]
    public void ValidationAndMessageLimitsFailWithoutChangingDestination()
    {
        FixDictionary dictionary = CreateDictionary();
        TagValueMessageEncoder encoder = new TagValueMessageEncoder();
        FixMessage invalid = CreateMessage("ORDER-5", "INVALID");
        invalid.BodyFields.Add(Field(54, "X"));
        byte[] invalidDestination = [88, 88, 88, 88];
        FixMessage limited = CreateMessage("ORDER-6", "LIMITED");
        byte[] limitedDestination = new byte[256];
        Array.Fill(limitedDestination, (byte)88);

        EncodeResult invalidResult = encoder.EncodeMessage(invalid, dictionary, invalidDestination, new EncodeOptions());
        EncodeResult limitedResult = encoder.EncodeMessage(limited, dictionary, limitedDestination, new EncodeOptions { MaxMessageSize = 8 });

        Assert.Multiple(() =>
        {
            Assert.That(invalidResult.Status, Is.EqualTo(EncodeStatus.Invalid));
            Assert.That(invalidResult.ErrorCode, Is.EqualTo("invalid-boolean"));
            Assert.That(invalidResult.BytesWritten, Is.Zero);
            Assert.That(invalidDestination, Is.EqualTo(new byte[] { 88, 88, 88, 88 }));
            Assert.That(limitedResult.Status, Is.EqualTo(EncodeStatus.LimitExceeded));
            Assert.That(limitedResult.ErrorCode, Is.EqualTo("limit-exceeded"));
            Assert.That(limitedResult.RequiredSize, Is.GreaterThan(8));
            Assert.That(limitedResult.BytesWritten, Is.Zero);
            Assert.That(limitedDestination, Is.All.EqualTo(88));
        });
    }

    private static FixMessage CreateMessage(string orderId, string text)
    {
        FixMessage message = new FixMessage { BeginString = "FIX.4.4", MsgType = "D" };
        message.HeaderFields.Add(Field(49, "SENDER"));
        message.BodyFields.Add(Field(11, orderId));
        message.BodyFields.Add(Field(58, text));
        return message;
    }

    private static FixDictionary CreateDictionary()
    {
        FixDictionary dictionary = new FixDictionary();
        dictionary.Identity.BeginString = "FIX.4.4";
        dictionary.Fields =
        [
            Definition(8, FixFieldType.String),
            Definition(9, FixFieldType.Length),
            Definition(10, FixFieldType.String),
            Definition(11, FixFieldType.String),
            Definition(35, FixFieldType.String),
            Definition(49, FixFieldType.String),
            Definition(54, FixFieldType.Boolean),
            Definition(58, FixFieldType.String)
        ];
        dictionary.Header = [Member(49)];
        dictionary.Trailer = [Member(10)];
        dictionary.Messages =
        [
            new FixMessageDefinition
            {
                Id = 100,
                MsgType = "D",
                Members = [Member(11), Member(58), Member(54)]
            }
        ];
        return dictionary;
    }

    private static FixFieldDefinition Definition(int tag, FixFieldType fieldType)
    {
        return new FixFieldDefinition { Tag = tag, FieldType = fieldType };
    }

    private static FixMemberDefinition Member(int tag)
    {
        return new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = tag };
    }

    private static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.Latin1.GetBytes(value) };
    }

    private static string Text(FixField field)
    {
        return Encoding.Latin1.GetString(field.Value);
    }
}
