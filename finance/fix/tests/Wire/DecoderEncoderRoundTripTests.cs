using System;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class DecoderEncoderRoundTripTests
{
    [Test]
    public void UnknownDuplicateAndRawFieldsRoundTripWithoutByteLoss()
    {
        FixDictionary dictionary = CreateDictionary();
        byte[] rawUnknown = [57, 57, 57, 61, 65, 0, 61, 233, 255, FixDefaults.Soh];
        byte[] body = Combine(Wire("35=D|58=FIRST|"), rawUnknown);
        body = Combine(body, Wire("58=SECOND|999=THIRD|"));
        byte[] source = BuildFrame(body);
        byte[] original = source.AsSpan().ToArray();
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();

        DecodeResult decoded = decoder.DecodeMessage(source, dictionary, new DecodeOptions());
        Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), decoded.ErrorCode);
        Assert.That(decoded.Message, Is.Not.Null);
        FixMessage message = decoded.Message!;
        FixField rawField = message.BodyFields[1];
        source[0] = 88;
        rawUnknown[4] = 88;

        byte[] encoded = Encode(message, dictionary, new EncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(message.BodyFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 58, 999, 58, 999 }));
            Assert.That(message.BodyFields.FindAll(field => field.Tag == 58), Has.Count.EqualTo(2));
            Assert.That(message.BodyFields.FindAll(field => field.Tag == 999), Has.Count.EqualTo(2));
            Assert.That(rawField.IsUnknown, Is.True);
            Assert.That(rawField.Value, Is.EqualTo(new byte[] { 65, 0, 61, 233, 255 }));
            Assert.That(message.OriginalBytes, Is.EqualTo(original));
            Assert.That(encoded, Is.EqualTo(original));
        });
    }

    [Test]
    public void UnknownFieldsCanBeRemovedOnDecodeAndEncode()
    {
        FixDictionary dictionary = CreateDictionary();
        byte[] source = BuildFrame(Wire("35=D|999=UNKNOWN|11=ORDER|"));
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();

        DecodeResult retained = decoder.DecodeMessage(source, dictionary, new DecodeOptions());
        DecodeResult removed = decoder.DecodeMessage(source, dictionary, new DecodeOptions { PreserveUnknownFields = false });
        byte[] encodedWithoutUnknown = Encode(retained.Message!, dictionary, new EncodeOptions { IncludeUnknownFields = false });

        Assert.Multiple(() =>
        {
            Assert.That(retained.Status, Is.EqualTo(DecodeStatus.Success), retained.ErrorCode);
            Assert.That(retained.Message!.BodyFields.Exists(field => field.Tag == 999), Is.True);
            Assert.That(removed.Status, Is.EqualTo(DecodeStatus.Success), removed.ErrorCode);
            Assert.That(removed.Message!.BodyFields.Exists(field => field.Tag == 999), Is.False);
            Assert.That(Display(encodedWithoutUnknown), Does.Not.Contain("|999="));
            Assert.That(Display(encodedWithoutUnknown), Does.Contain("|11=ORDER|"));
        });
    }

    [Test]
    public void EmptyValuesFollowThePublicDecoderPolicy()
    {
        FixDictionary dictionary = CreateDictionary();
        byte[] source = BuildFrame(Wire("35=D|58=|999=|"));
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();

        DecodeResult strict = decoder.DecodeMessage(source, dictionary, new DecodeOptions());
        DecodeResult permissive = decoder.DecodeMessage(source, dictionary, new DecodeOptions { AllowEmptyValues = true });
        EncodeResult knownEmpty = new TagValueMessageEncoder().EncodeMessage(permissive.Message!, dictionary, Span<byte>.Empty, new EncodeOptions());
        FixMessage unknownOnly = new FixMessage { BeginString = "FIX.4.4", MsgType = "D" };
        unknownOnly.BodyFields.Add(new FixField { Tag = 999, Value = [] });
        EncodeResult unknownEmpty = new TagValueMessageEncoder().EncodeMessage(unknownOnly, dictionary, Span<byte>.Empty, new EncodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(strict.Status, Is.EqualTo(DecodeStatus.Invalid));
            Assert.That(strict.ErrorCode, Is.EqualTo("empty-value"));
            Assert.That(permissive.Status, Is.EqualTo(DecodeStatus.Success), permissive.ErrorCode);
            Assert.That(permissive.Message!.BodyFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 58, 999 }));
            Assert.That(permissive.Message.BodyFields[0].Value, Is.Empty);
            Assert.That(permissive.Message.BodyFields[1].Value, Is.Empty);
            Assert.That(permissive.Message.BodyFields[1].IsUnknown, Is.True);
            Assert.That(knownEmpty.Status, Is.EqualTo(EncodeStatus.Invalid));
            Assert.That(knownEmpty.ErrorCode, Is.EqualTo("incomplete"));
            Assert.That(unknownEmpty.Status, Is.EqualTo(EncodeStatus.Invalid));
            Assert.That(unknownEmpty.ErrorCode, Is.EqualTo("empty-value"));
        });
    }

    [Test]
    public void Latin1BytesRoundTripWhileAsciiRejectsThem()
    {
        FixDictionary dictionary = CreateDictionary();
        byte[] body = Combine(Wire("35=D|58="), new byte[] { 233, FixDefaults.Soh });
        byte[] source = BuildFrame(body);
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();

        DecodeResult latin1 = decoder.DecodeMessage(source, dictionary, new DecodeOptions { TextEncodingName = "iso8859-1" });
        DecodeResult ascii = decoder.DecodeMessage(source, dictionary, new DecodeOptions { TextEncodingName = "us-ascii" });
        byte[] encoded = Encode(latin1.Message!, dictionary, new EncodeOptions { TextEncodingName = "latin1" });
        EncodeResult asciiEncoded = new TagValueMessageEncoder().EncodeMessage(latin1.Message!, dictionary, Span<byte>.Empty, new EncodeOptions { TextEncodingName = "ascii" });

        Assert.Multiple(() =>
        {
            Assert.That(latin1.Status, Is.EqualTo(DecodeStatus.Success), latin1.ErrorCode);
            Assert.That(latin1.Message!.BodyFields[0].Value, Is.EqualTo(new byte[] { 233 }));
            Assert.That(encoded, Is.EqualTo(source));
            Assert.That(ascii.Status, Is.EqualTo(DecodeStatus.Invalid));
            Assert.That(ascii.ErrorCode, Is.EqualTo("invalid-character"));
            Assert.That(asciiEncoded.Status, Is.EqualTo(EncodeStatus.Invalid));
            Assert.That(asciiEncoded.ErrorCode, Is.EqualTo("invalid-character"));
        });
    }

    [Test]
    public void EveryInsufficientDestinationIsUnchangedAndReportsTheExactSize()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage message = new FixMessage { BeginString = "FIX.4.4", MsgType = "D" };
        message.HeaderFields.Add(Field(49, "SENDER"));
        message.BodyFields.Add(Field(11, "ORDER"));
        message.BodyFields.Add(Field(58, "TEXT"));
        TagValueMessageEncoder encoder = new TagValueMessageEncoder();
        EncodeResult measured = encoder.EncodeMessage(message, dictionary, Span<byte>.Empty, new EncodeOptions());

        Assert.Multiple(() =>
        {
            for (int size = 0; size < measured.RequiredSize; size++)
            {
                byte[] destination = new byte[size];
                Array.Fill(destination, (byte)88);
                byte[] before = destination.AsSpan().ToArray();

                EncodeResult result = encoder.EncodeMessage(message, dictionary, destination, new EncodeOptions());

                Assert.That(result.Status, Is.EqualTo(EncodeStatus.DestinationTooSmall), $"size {size}");
                Assert.That(result.BytesWritten, Is.Zero, $"size {size}");
                Assert.That(result.RequiredSize, Is.EqualTo(measured.RequiredSize), $"size {size}");
                Assert.That(result.ErrorCode, Is.EqualTo("destination-too-small"), $"size {size}");
                Assert.That(destination, Is.EqualTo(before), $"size {size}");
            }
        });
    }

    [Test]
    public void CanonicalRoundTripNormalizesValuesAndDictionaryOrder()
    {
        FixDictionary dictionary = CreateDictionary();
        byte[] source = BuildFrame(Wire("35=D|58=SECOND|44=-00023.2300|11=ORDER|58=FIRST|"));
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();

        DecodeResult decoded = decoder.DecodeMessage(source, dictionary, new DecodeOptions());
        byte[] canonical = Encode(decoded.Message!, dictionary, new EncodeOptions { PreserveFieldOrder = false });
        DecodeResult roundTrip = decoder.DecodeMessage(canonical, dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), decoded.ErrorCode);
            Assert.That(canonical, Is.Not.EqualTo(source));
            Assert.That(Display(canonical), Does.Contain("|35=D|11=ORDER|44=-23.23|58=SECOND|58=FIRST|10="));
            Assert.That(roundTrip.Status, Is.EqualTo(DecodeStatus.Success), roundTrip.ErrorCode);
            Assert.That(roundTrip.Message!.BodyFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 11, 44, 58, 58 }));
            Assert.That(roundTrip.Message.BodyFields[1].Value, Is.EqualTo("-23.23"u8.ToArray()));
        });
    }

    private static byte[] Encode(FixMessage message, FixDictionary dictionary, EncodeOptions options)
    {
        TagValueMessageEncoder encoder = new TagValueMessageEncoder();
        EncodeResult measured = encoder.EncodeMessage(message, dictionary, Span<byte>.Empty, options);
        if (measured.Status != EncodeStatus.DestinationTooSmall)
        {
            throw new InvalidOperationException(measured.ErrorCode);
        }
        byte[] destination = new byte[measured.RequiredSize];
        EncodeResult encoded = encoder.EncodeMessage(message, dictionary, destination, options);
        if (encoded.Status != EncodeStatus.Success)
        {
            throw new InvalidOperationException(encoded.ErrorCode);
        }
        return destination;
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
            Definition(44, FixFieldType.Price),
            Definition(49, FixFieldType.String),
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
                Members = [Member(11), Member(44), Member(58)]
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

    private static byte[] BuildFrame(byte[] body)
    {
        byte[] header = Encoding.ASCII.GetBytes($"8=FIX.4.4\u00019={body.Length}\u0001");
        byte[] checksumInput = Combine(header, body);
        int checksum = ChecksumCalculator.CalculateChecksum(checksumInput);
        byte[] trailer = Encoding.ASCII.GetBytes($"10={checksum:000}\u0001");
        return Combine(checksumInput, trailer);
    }

    private static byte[] Wire(string text)
    {
        return Encoding.ASCII.GetBytes(text.Replace('|', (char)FixDefaults.Soh));
    }

    private static string Display(byte[] bytes)
    {
        return Encoding.Latin1.GetString(bytes).Replace('\u0001', '|');
    }

    private static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] combined = new byte[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }
}
