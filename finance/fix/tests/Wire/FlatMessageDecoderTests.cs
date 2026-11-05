using System;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class FlatMessageDecoderTests
{
    [Test]
    public void DecoderPreservesWireOrderDuplicatesRawBytesAndSourceLocations()
    {
        FixDictionary dictionary = CreateDictionary();
        byte[] prefix = Wire("8=FIX.4.4|9=99|35=D|49=SENDER|56=TARGET|11=A|58=first|999=X|58=second|95=5|96=");
        byte[] payload = [65, 1, 66, 0, 255, 1];
        byte[] source = Combine(Combine(prefix, payload), Wire("10=000|"));
        byte[] original = source.AsSpan().ToArray();

        DecodeResult result = MessageBuilder.DecodeFlatMessage(source, dictionary, new DecodeOptions());
        source[0] = 88;
        source[prefix.Length] = 88;

        Assert.That(result.Status, Is.EqualTo(DecodeStatus.Success));
        Assert.That(result.Message, Is.Not.Null);
        FixMessage message = result.Message!;
        Assert.Multiple(() =>
        {
            Assert.That(message.BeginString, Is.EqualTo("FIX.4.4"));
            Assert.That(message.MsgType, Is.EqualTo("D"));
            Assert.That(message.HeaderFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 8, 9, 35, 49, 56 }));
            Assert.That(message.BodyFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 11, 58, 999, 58, 95, 96 }));
            Assert.That(message.TrailerFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 10 }));
            Assert.That(message.BodyFields[1].Value, Is.EqualTo(Encoding.ASCII.GetBytes("first")));
            Assert.That(message.BodyFields[3].Value, Is.EqualTo(Encoding.ASCII.GetBytes("second")));
            Assert.That(message.BodyFields[1], Is.Not.SameAs(message.BodyFields[3]));
            Assert.That(message.BodyFields[2].IsUnknown, Is.True);
            Assert.That(message.BodyFields[5].IsEncodedData, Is.True);
            Assert.That(message.BodyFields[5].Value, Is.EqualTo(new byte[] { 65, 1, 66, 0, 255 }));
            Assert.That(message.BodyFields[5].SourceOffset, Is.EqualTo(prefix.Length - 3));
            Assert.That(message.BodyFields[5].SourceLength, Is.EqualTo(9));
            Assert.That(message.BodyFields[5].WireIndex, Is.EqualTo(10));
            Assert.That(message.OriginalBytes, Is.EqualTo(original));
            Assert.That(result.FieldResults, Has.Count.EqualTo(12));
            Assert.That(result.FieldResults[10].Field, Is.SameAs(message.BodyFields[5]));
            Assert.That(result.Consumed, Is.EqualTo(original.Length));
            Assert.That(result.Required, Is.EqualTo(original.Length));
        });
    }

    [Test]
    public void EnvelopeExtractionUsesFirstOccurrencesWhileRetainingDuplicates()
    {
        FixDictionary dictionary = CreateDictionary();
        byte[] source = Wire("8=FIX.4.4|9=5|35=D|8=FIXT.1.1|35=0|10=000|");

        DecodeResult result = MessageBuilder.DecodeFlatMessage(source, dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(result.Message!.BeginString, Is.EqualTo("FIX.4.4"));
            Assert.That(result.Message.MsgType, Is.EqualTo("D"));
            Assert.That(result.Message.HeaderFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 8, 9, 35, 8, 35 }));
            Assert.That(result.Message.HeaderFields[0].WireIndex, Is.Zero);
            Assert.That(result.Message.HeaderFields[3].WireIndex, Is.EqualTo(3));
        });
    }

    [Test]
    public void HeaderAndTrailerComponentsRouteTheirNestedFlatFields()
    {
        FixDictionary dictionary = CreateDictionary();
        FixComponentDefinition trailerComponent = new FixComponentDefinition
        {
            Id = 200,
            Members = [new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 89 }]
        };
        dictionary.Components.Add(trailerComponent);
        dictionary.Trailer.Add(new FixMemberDefinition { Kind = FixMemberKind.Component, ReferenceId = 200 });
        dictionary.Fields.Add(new FixFieldDefinition { Tag = 89, FieldType = FixFieldType.String });
        byte[] source = Wire("8=FIX.4.4|9=10|35=D|49=S|89=SIG|10=000|");

        DecodeResult result = MessageBuilder.DecodeFlatMessage(source, dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Message!.HeaderFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 8, 9, 35, 49 }));
            Assert.That(result.Message.TrailerFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 89, 10 }));
        });
    }

    [Test]
    public void EmptyValuesAndOriginalBytePreservationFollowDecodeOptions()
    {
        FixDictionary dictionary = CreateDictionary();
        byte[] source = Wire("8=FIX.4.4|9=5|35=D|58=|10=000|");
        DecodeOptions strict = new DecodeOptions();
        DecodeOptions permissive = new DecodeOptions { AllowEmptyValues = true, PreserveOriginalBytes = false };

        DecodeResult invalid = MessageBuilder.DecodeFlatMessage(source, dictionary, strict);
        DecodeResult valid = MessageBuilder.DecodeFlatMessage(source, dictionary, permissive);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status, Is.EqualTo(DecodeStatus.Invalid));
            Assert.That(invalid.ErrorCode, Is.EqualTo("empty-value"));
            Assert.That(valid.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(valid.Message!.BodyFields[0].Value, Is.Empty);
            Assert.That(valid.Message.OriginalBytes, Is.Empty);
        });
    }

    [Test]
    public void FieldScannerReportsMalformedIncompleteAndLimitedFieldsPrecisely()
    {
        FieldParseResult valid = FieldScanner.ScanField("58=ABC\u0001"u8, 0, 7, 3, false);
        FieldParseResult leadingZero = FieldScanner.ScanField("058=ABC\u0001"u8, 0, 0, 10, false);
        FieldParseResult missingEquals = FieldScanner.ScanField("58ABC\u0001"u8, 0, 0, 10, false);
        FieldParseResult incomplete = FieldScanner.ScanField("58=ABC"u8, 0, 0, 10, false);
        FieldParseResult limited = FieldScanner.ScanField("58=ABCD\u0001"u8, 0, 0, 3, false);
        FieldParseResult invalidOffset = FieldScanner.ScanField("58=ABC\u0001"u8, -1, 0, 3, false);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(valid.Field!.Tag, Is.EqualTo(58));
            Assert.That(valid.Field.WireIndex, Is.EqualTo(7));
            Assert.That(valid.Field.SourceOffset, Is.Zero);
            Assert.That(valid.Field.SourceLength, Is.EqualTo(7));
            Assert.That(leadingZero.ErrorCode, Is.EqualTo("leading-zero"));
            Assert.That(leadingZero.ErrorOffset, Is.Zero);
            Assert.That(missingEquals.ErrorCode, Is.EqualTo("missing-equals"));
            Assert.That(missingEquals.ErrorOffset, Is.EqualTo(5));
            Assert.That(incomplete.Status, Is.EqualTo(DecodeStatus.Incomplete));
            Assert.That(incomplete.Required, Is.EqualTo(7));
            Assert.That(incomplete.ErrorOffset, Is.EqualTo(6));
            Assert.That(limited.Status, Is.EqualTo(DecodeStatus.LimitExceeded));
            Assert.That(limited.ErrorOffset, Is.EqualTo(6));
            Assert.That(invalidOffset.ErrorCode, Is.EqualTo("invalid-offset"));
        });
    }

    [Test]
    public void DecoderReturnsExplicitLimitsAndConsumedOffsetsOnFailure()
    {
        FixDictionary dictionary = CreateDictionary();
        byte[] malformed = Wire("8=FIX.4.4|9=5|35=D|58=OK|058=BAD|");
        int malformedOffset = ByteSearch.IndexOfSequence(malformed, "058="u8);

        DecodeResult invalid = MessageBuilder.DecodeFlatMessage(malformed, dictionary, new DecodeOptions());
        DecodeResult fieldLimit = MessageBuilder.DecodeFlatMessage(Wire("8=FIX.4.4|9=5|35=D|58=ABCD|"), dictionary, new DecodeOptions { MaxFieldSize = 3 });
        DecodeResult countLimit = MessageBuilder.DecodeFlatMessage(Wire("8=FIX.4.4|9=5|35=D|"), dictionary, new DecodeOptions { MaxFieldsPerMessage = 2 });
        DecodeResult messageLimit = MessageBuilder.DecodeFlatMessage(Wire("8=FIX.4.4|"), dictionary, new DecodeOptions { MaxMessageSize = 5 });
        DecodeResult empty = MessageBuilder.DecodeFlatMessage(ReadOnlySpan<byte>.Empty, dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Status, Is.EqualTo(DecodeStatus.Invalid));
            Assert.That(invalid.ErrorCode, Is.EqualTo("leading-zero"));
            Assert.That(invalid.ErrorOffset, Is.EqualTo(malformedOffset));
            Assert.That(invalid.Consumed, Is.EqualTo(malformedOffset));
            Assert.That(fieldLimit.Status, Is.EqualTo(DecodeStatus.LimitExceeded));
            Assert.That(fieldLimit.ErrorCode, Is.EqualTo("limit-exceeded"));
            Assert.That(countLimit.ErrorCode, Is.EqualTo("field-count-limit-exceeded"));
            Assert.That(countLimit.Consumed, Is.GreaterThan(0));
            Assert.That(messageLimit.Status, Is.EqualTo(DecodeStatus.LimitExceeded));
            Assert.That(empty.Status, Is.EqualTo(DecodeStatus.Incomplete));
            Assert.That(empty.Required, Is.EqualTo(1));
        });
    }

    [Test]
    public void DictionaryDataAssociationsEnforceAdjacencyAndExactAvailability()
    {
        FixDictionary dictionary = CreateDictionary();
        byte[] intervening = Wire("8=FIX.4.4|9=5|35=D|95=3|58=X|96=ABC|10=000|");
        byte[] orphan = Wire("8=FIX.4.4|9=5|35=D|96=ABC|10=000|");
        byte[] incomplete = Combine(Wire("8=FIX.4.4|9=5|35=D|95=5|96="), new byte[] { 65, 66, 67, 68, 1 });

        DecodeResult adjacency = MessageBuilder.DecodeFlatMessage(intervening, dictionary, new DecodeOptions());
        DecodeResult orphanResult = MessageBuilder.DecodeFlatMessage(orphan, dictionary, new DecodeOptions());
        DecodeResult incompleteResult = MessageBuilder.DecodeFlatMessage(incomplete, dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(adjacency.ErrorCode, Is.EqualTo("data-field-not-adjacent"));
            Assert.That(adjacency.FieldResults, Has.Count.GreaterThanOrEqualTo(5));
            Assert.That(orphanResult.ErrorCode, Is.EqualTo("data-field-not-adjacent"));
            Assert.That(incompleteResult.Status, Is.EqualTo(DecodeStatus.Incomplete));
            Assert.That(incompleteResult.Required, Is.EqualTo(incomplete.Length + 1));
        });
    }

    private static FixDictionary CreateDictionary()
    {
        FixDictionary dictionary = new FixDictionary();
        dictionary.Fields =
        [
            new FixFieldDefinition { Tag = 8, FieldType = FixFieldType.String },
            new FixFieldDefinition { Tag = 9, FieldType = FixFieldType.Length },
            new FixFieldDefinition { Tag = 10, FieldType = FixFieldType.String },
            new FixFieldDefinition { Tag = 11, FieldType = FixFieldType.String },
            new FixFieldDefinition { Tag = 35, FieldType = FixFieldType.String },
            new FixFieldDefinition { Tag = 49, FieldType = FixFieldType.String },
            new FixFieldDefinition { Tag = 56, FieldType = FixFieldType.String },
            new FixFieldDefinition { Tag = 58, FieldType = FixFieldType.String },
            new FixFieldDefinition { Tag = 95, FieldType = FixFieldType.Length, HasDataTag = true, DataTag = 96 },
            new FixFieldDefinition { Tag = 96, FieldType = FixFieldType.Data, HasLengthTag = true, LengthTag = 95 }
        ];
        FixComponentDefinition routing = new FixComponentDefinition
        {
            Id = 100,
            Members =
            [
                new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 49 },
                new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 56 }
            ]
        };
        dictionary.Components.Add(routing);
        dictionary.Header.Add(new FixMemberDefinition { Kind = FixMemberKind.Component, ReferenceId = 100 });
        dictionary.Trailer.Add(new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 10 });
        return dictionary;
    }

    private static byte[] Wire(string text)
    {
        return Encoding.ASCII.GetBytes(text.Replace('|', (char)FixDefaults.Soh));
    }

    private static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] combined = new byte[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }
}
