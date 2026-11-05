using System;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class EncodedDataAndNestedGroupCodecTests
{
    [Test]
    public void DataAndXmlDataRoundTripEmbeddedSohAndArbitraryOctets()
    {
        FixDictionary dictionary = CreateDictionary();
        byte[] data = [0, FixDefaults.Soh, 61, 127, 128, 255, FixDefaults.Soh];
        byte[] xmlData = [60, 120, FixDefaults.Soh, 0, 255, 62];
        FixMessage message = CreateMessage();
        message.BodyFields =
        [
            Field(95, "999"),
            new FixField { Tag = 96, Value = data, IsEncodedData = true },
            Field(212, "999"),
            new FixField { Tag = 213, Value = xmlData, IsEncodedData = true }
        ];

        byte[] encoded = Encode(message, dictionary, new EncodeOptions());
        DecodeResult decoded = new TagValueMessageDecoder().DecodeMessage(encoded, dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), decoded.ErrorCode);
            Assert.That(decoded.Message!.BodyFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 95, 96, 212, 213 }));
            Assert.That(decoded.Message.BodyFields[0].Value, Is.EqualTo("7"u8.ToArray()));
            Assert.That(decoded.Message.BodyFields[1].Value, Is.EqualTo(data));
            Assert.That(decoded.Message.BodyFields[1].IsEncodedData, Is.True);
            Assert.That(decoded.Message.BodyFields[2].Value, Is.EqualTo("6"u8.ToArray()));
            Assert.That(decoded.Message.BodyFields[3].Value, Is.EqualTo(xmlData));
            Assert.That(decoded.Message.BodyFields[3].IsEncodedData, Is.True);
        });
    }

    [Test]
    public void EncodedDataRequiresAdjacencyAndExactDeclaredLength()
    {
        FixDictionary dictionary = CreateDictionary();
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();
        DecodeResult nonAdjacent = decoder.DecodeMessage(BuildFrame(Wire("35=D|95=3|58=X|96=ABC|")), dictionary, new DecodeOptions());
        DecodeResult declaredTooShort = decoder.DecodeMessage(BuildFrame(Wire("35=D|95=3|96=ABCD|")), dictionary, new DecodeOptions());
        DecodeResult declaredTooLong = decoder.DecodeMessage(BuildFrame(Wire("35=D|95=5|96=ABCD|58=X|")), dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(nonAdjacent.Status, Is.EqualTo(DecodeStatus.Invalid));
            Assert.That(nonAdjacent.ErrorCode, Is.EqualTo("data-field-not-adjacent"));
            Assert.That(declaredTooShort.Status, Is.EqualTo(DecodeStatus.Invalid));
            Assert.That(declaredTooShort.ErrorCode, Is.EqualTo("data-length-mismatch"));
            Assert.That(declaredTooLong.Status, Is.EqualTo(DecodeStatus.Invalid));
            Assert.That(declaredTooLong.ErrorCode, Is.EqualTo("data-length-mismatch"));
        });
    }

    [Test]
    public void ZeroEntryGroupRoundTripsWithoutConsumingTheFollowingField()
    {
        FixDictionary dictionary = CreateDictionary();
        byte[] source = BuildFrame(Wire("35=D|453=0|11=AFTER|"));
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();

        DecodeResult decoded = decoder.DecodeMessage(source, dictionary, new DecodeOptions());
        byte[] encoded = Encode(decoded.Message!, dictionary, new EncodeOptions());
        DecodeResult roundTrip = decoder.DecodeMessage(encoded, dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), decoded.ErrorCode);
            Assert.That(decoded.Message!.BodyGroups, Has.Count.EqualTo(1));
            Assert.That(decoded.Message.BodyGroups[0].DeclaredCount, Is.Zero);
            Assert.That(decoded.Message.BodyGroups[0].Entries, Is.Empty);
            Assert.That(decoded.Message.BodyFields.Find(field => field.Tag == 11)!.Value, Is.EqualTo("AFTER"u8.ToArray()));
            Assert.That(encoded, Is.EqualTo(source));
            Assert.That(roundTrip.Status, Is.EqualTo(DecodeStatus.Success), roundTrip.ErrorCode);
            Assert.That(roundTrip.Message!.BodyGroups[0].Entries, Is.Empty);
        });
    }

    [Test]
    public void IncorrectOuterCountsHaveDistinctDiagnostics()
    {
        FixDictionary dictionary = CreateDictionary();
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();
        DecodeResult zeroWithEntry = decoder.DecodeMessage(BuildFrame(Wire("35=D|453=0|448=P1|")), dictionary, new DecodeOptions());
        DecodeResult tooFew = decoder.DecodeMessage(BuildFrame(Wire("35=D|453=1|448=P1|452=1|448=P2|452=2|")), dictionary, new DecodeOptions());
        DecodeResult tooMany = decoder.DecodeMessage(BuildFrame(Wire("35=D|453=2|448=P1|452=1|11=AFTER|")), dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(zeroWithEntry.ErrorCode, Is.EqualTo("group-count-mismatch"));
            Assert.That(tooFew.ErrorCode, Is.EqualTo("group-count-mismatch"));
            Assert.That(tooMany.ErrorCode, Is.EqualTo("premature-group-termination"));
            Assert.That(zeroWithEntry.ErrorOffset, Is.GreaterThan(0));
            Assert.That(tooFew.ErrorOffset, Is.GreaterThan(0));
            Assert.That(tooMany.ErrorOffset, Is.GreaterThan(0));
        });
    }

    [Test]
    public void WrongDelimiterAndInvalidMemberOrderAreRejectedByTheDecoder()
    {
        FixDictionary dictionary = CreateDictionary();
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();
        DecodeResult delimiter = decoder.DecodeMessage(BuildFrame(Wire("35=D|453=1|447=D|")), dictionary, new DecodeOptions());
        DecodeResult order = decoder.DecodeMessage(BuildFrame(Wire("35=D|453=1|448=P1|452=1|447=D|")), dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(delimiter.Status, Is.EqualTo(DecodeStatus.Invalid));
            Assert.That(delimiter.ErrorCode, Is.EqualTo("missing-group-delimiter"));
            Assert.That(order.Status, Is.EqualTo(DecodeStatus.Invalid));
            Assert.That(order.ErrorCode, Is.EqualTo("group-member-out-of-order"));
        });
    }

    [Test]
    public void NestedGroupsReportWrongDelimiterAndPrematureTermination()
    {
        FixDictionary dictionary = CreateDictionary();
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();
        DecodeResult delimiter = decoder.DecodeMessage(BuildFrame(Wire("35=D|453=1|448=P1|802=1|803=1|")), dictionary, new DecodeOptions());
        DecodeResult termination = decoder.DecodeMessage(BuildFrame(Wire("35=D|453=1|448=P1|802=2|523=S1|803=1|452=1|11=AFTER|")), dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(delimiter.Status, Is.EqualTo(DecodeStatus.Invalid));
            Assert.That(delimiter.ErrorCode, Is.EqualTo("missing-group-delimiter"));
            Assert.That(termination.Status, Is.EqualTo(DecodeStatus.Invalid));
            Assert.That(termination.ErrorCode, Is.EqualTo("premature-group-termination"));
        });
    }

    [Test]
    public void StructuredNestedGroupsSynchronizeCountsAndRoundTrip()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage message = CreateMessage();
        message.BodyFields.Add(Field(11, "AFTER"));
        FixGroup parties = new FixGroup { CountTag = 453, DelimiterTag = 448, DeclaredCount = 99 };
        FixGroupEntry first = new FixGroupEntry
        {
            Fields = [Field(448, "P1"), Field(452, "1")]
        };
        FixGroup nested = new FixGroup { CountTag = 802, DelimiterTag = 523, DeclaredCount = 99 };
        nested.Entries.Add(new FixGroupEntry { Fields = [Field(523, "S1"), Field(803, "1")] });
        nested.Entries.Add(new FixGroupEntry { Fields = [Field(523, "S2"), Field(803, "2")] });
        first.Groups.Add(nested);
        parties.Entries.Add(first);
        parties.Entries.Add(new FixGroupEntry { Fields = [Field(448, "P2"), Field(452, "2")] });
        message.BodyGroups.Add(parties);

        byte[] encoded = Encode(message, dictionary, new EncodeOptions { PreserveFieldOrder = false });
        DecodeResult decoded = new TagValueMessageDecoder().DecodeMessage(encoded, dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(Display(encoded), Does.Contain("|35=D|453=2|448=P1|802=2|523=S1|803=1|523=S2|803=2|452=1|448=P2|452=2|11=AFTER|10="));
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), decoded.ErrorCode);
            Assert.That(decoded.Message!.BodyGroups, Has.Count.EqualTo(1));
            Assert.That(decoded.Message.BodyGroups[0].DeclaredCount, Is.EqualTo(2));
            Assert.That(decoded.Message.BodyGroups[0].Entries, Has.Count.EqualTo(2));
            Assert.That(decoded.Message.BodyGroups[0].Entries[0].Groups, Has.Count.EqualTo(1));
            Assert.That(decoded.Message.BodyGroups[0].Entries[0].Groups[0].DeclaredCount, Is.EqualTo(2));
            Assert.That(decoded.Message.BodyGroups[0].Entries[0].Groups[0].Entries, Has.Count.EqualTo(2));
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

    private static FixMessage CreateMessage()
    {
        return new FixMessage { BeginString = "FIX.4.4", MsgType = "D" };
    }

    private static FixDictionary CreateDictionary()
    {
        FixGroupDefinition nested = new FixGroupDefinition
        {
            Id = 2,
            CountTag = 802,
            DelimiterTag = 523,
            Members = [Member(FixMemberKind.Field, 523), Member(FixMemberKind.Field, 803)]
        };
        FixGroupDefinition parties = new FixGroupDefinition
        {
            Id = 1,
            CountTag = 453,
            DelimiterTag = 448,
            Members =
            [
                Member(FixMemberKind.Field, 448),
                Member(FixMemberKind.Field, 447),
                Member(FixMemberKind.Group, 2),
                Member(FixMemberKind.Field, 452)
            ]
        };
        FixDictionary dictionary = new FixDictionary();
        dictionary.Identity.BeginString = "FIX.4.4";
        dictionary.Fields =
        [
            Definition(8, FixFieldType.String),
            Definition(9, FixFieldType.Length),
            Definition(10, FixFieldType.String),
            Definition(11, FixFieldType.String),
            Definition(35, FixFieldType.String),
            Definition(58, FixFieldType.String),
            new FixFieldDefinition { Tag = 95, FieldType = FixFieldType.Length, HasDataTag = true, DataTag = 96 },
            new FixFieldDefinition { Tag = 96, FieldType = FixFieldType.Data, HasLengthTag = true, LengthTag = 95 },
            new FixFieldDefinition { Tag = 212, FieldType = FixFieldType.Length, HasDataTag = true, DataTag = 213 },
            new FixFieldDefinition { Tag = 213, FieldType = FixFieldType.XmlData, HasLengthTag = true, LengthTag = 212 },
            Definition(447, FixFieldType.Char),
            Definition(448, FixFieldType.String),
            Definition(452, FixFieldType.Int),
            Definition(453, FixFieldType.NumInGroup),
            Definition(523, FixFieldType.String),
            Definition(802, FixFieldType.NumInGroup),
            Definition(803, FixFieldType.Int)
        ];
        dictionary.Trailer = [Member(FixMemberKind.Field, 10)];
        dictionary.Groups = [parties, nested];
        dictionary.Messages =
        [
            new FixMessageDefinition
            {
                Id = 100,
                MsgType = "D",
                Members =
                [
                    Member(FixMemberKind.Field, 95),
                    Member(FixMemberKind.Field, 96),
                    Member(FixMemberKind.Field, 212),
                    Member(FixMemberKind.Field, 213),
                    Member(FixMemberKind.Group, 1),
                    Member(FixMemberKind.Field, 11),
                    Member(FixMemberKind.Field, 58)
                ]
            }
        ];
        return dictionary;
    }

    private static FixFieldDefinition Definition(int tag, FixFieldType fieldType)
    {
        return new FixFieldDefinition { Tag = tag, FieldType = fieldType };
    }

    private static FixMemberDefinition Member(FixMemberKind kind, int referenceId)
    {
        return new FixMemberDefinition { Kind = kind, ReferenceId = referenceId };
    }

    private static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.ASCII.GetBytes(value) };
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
