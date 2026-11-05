using System;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class CanonicalMessageEncoderTests
{
    [Test]
    public void EncoderEmitsCanonicalEnvelopeAndRecalculatesLengthAndChecksum()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage message = CreateMessage();
        message.HeaderFields =
        [
            Field(9, "999"),
            Field(56, "TARGET"),
            Field(8, "IGNORED"),
            Field(49, "SENDER"),
            Field(35, "IGNORED")
        ];
        message.BodyFields = [Field(58, "HELLO"), Field(44, "-00023.2300"), Field(11, "ORDER")];
        message.TrailerFields = [Field(10, "123")];

        MessageEncodingResult result = CanonicalMessageEncoder.EncodeCanonicalMessage(message, dictionary, new EncodeOptions());

        Assert.That(result.Success, Is.True, result.ErrorCode);
        string wire = Display(result.Bytes);
        BodyLengthResult length = BodyLengthCalculator.CalculateBodyLength(result.Bytes);
        int checksum = ChecksumCalculator.CalculateChecksum(result.Bytes.AsSpan(0, length.ChecksumStart));
        AsciiIntegerResult encodedChecksum = ChecksumCalculator.ParseChecksum(result.Bytes.AsSpan(length.ChecksumStart + 3, 3));
        Assert.Multiple(() =>
        {
            Assert.That(wire, Does.StartWith("8=FIX.4.4|9="));
            Assert.That(wire, Does.Contain("|35=D|56=TARGET|49=SENDER|58=HELLO|44=-23.23|11=ORDER|10="));
            Assert.That(Occurrences("|" + wire, "|8="), Is.EqualTo(1));
            Assert.That(Occurrences("|" + wire, "|9="), Is.EqualTo(1));
            Assert.That(Occurrences("|" + wire, "|35="), Is.EqualTo(1));
            Assert.That(Occurrences("|" + wire, "|10="), Is.EqualTo(1));
            Assert.That(length.Success, Is.True);
            Assert.That(length.Matches, Is.True);
            Assert.That(encodedChecksum.Success, Is.True);
            Assert.That(encodedChecksum.Value, Is.EqualTo(checksum));
            Assert.That(result.Required, Is.EqualTo(result.Bytes.Length));
        });
    }

    [Test]
    public void FieldOrderCanBePreservedOrCanonicalizedFromDictionaryMembers()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage message = CreateMessage();
        message.HeaderFields = [Field(56, "TARGET"), Field(49, "SENDER")];
        message.BodyFields = [Field(58, "TEXT"), Field(11, "ORDER")];

        MessageEncodingResult preserved = CanonicalMessageEncoder.EncodeCanonicalMessage(message, dictionary, new EncodeOptions());
        MessageEncodingResult canonical = CanonicalMessageEncoder.EncodeCanonicalMessage(message, dictionary, new EncodeOptions { PreserveFieldOrder = false });

        Assert.Multiple(() =>
        {
            Assert.That(preserved.Success, Is.True, preserved.ErrorCode);
            Assert.That(IndexOfField(preserved.Bytes, "56=TARGET"), Is.LessThan(IndexOfField(preserved.Bytes, "49=SENDER")));
            Assert.That(IndexOfField(preserved.Bytes, "58=TEXT"), Is.LessThan(IndexOfField(preserved.Bytes, "11=ORDER")));
            Assert.That(canonical.Success, Is.True, canonical.ErrorCode);
            Assert.That(IndexOfField(canonical.Bytes, "49=SENDER"), Is.LessThan(IndexOfField(canonical.Bytes, "56=TARGET")));
            Assert.That(IndexOfField(canonical.Bytes, "11=ORDER"), Is.LessThan(IndexOfField(canonical.Bytes, "58=TEXT")));
        });
    }

    [Test]
    public void EncodedDataIsAdjacentUsesItsByteLengthAndRoundTripsEmbeddedSoh()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage message = CreateMessage();
        byte[] data = [65, 1, 66, 0, 255];
        message.BodyFields = [Field(95, "999"), new FixField { Tag = 96, Value = data, IsEncodedData = true }];

        MessageEncodingResult encoded = CanonicalMessageEncoder.EncodeCanonicalMessage(message, dictionary, new EncodeOptions());
        DecodeResult decoded = new TagValueMessageDecoder().DecodeMessage(encoded.Bytes, dictionary, new DecodeOptions());

        byte[] expectedPair = [57, 53, 61, 53, 1, 57, 54, 61, 65, 1, 66, 0, 255, 1];
        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True, encoded.ErrorCode);
            Assert.That(ByteSearch.IndexOfSequence(encoded.Bytes, expectedPair), Is.GreaterThan(0));
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), decoded.ErrorCode);
            Assert.That(decoded.Message!.BodyFields.Find(field => field.Tag == 95)!.Value, Is.EqualTo("5"u8.ToArray()));
            Assert.That(decoded.Message.BodyFields.Find(field => field.Tag == 96)!.Value, Is.EqualTo(data));
        });
    }

    [Test]
    public void StructuredGroupsFlattenRecursivelyInCanonicalOrderAndSynchronizeCounts()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage message = CreateMessage();
        message.BodyFields = [Field(11, "ORDER")];
        FixGroup parties = new FixGroup { CountTag = 453, DelimiterTag = 448, DeclaredCount = 99 };
        FixGroupEntry first = new FixGroupEntry
        {
            EntryIndex = 0,
            Fields = [FieldAt(452, "1", 5), FieldAt(448, "P1", 1)]
        };
        FixGroup nested = new FixGroup { CountTag = 802, DelimiterTag = 523, DeclaredCount = 20, WireIndex = 3 };
        nested.Entries.Add(new FixGroupEntry { EntryIndex = 0, Fields = [FieldAt(523, "S1", 4)] });
        first.Groups.Add(nested);
        parties.Entries.Add(first);
        parties.Entries.Add(new FixGroupEntry
        {
            EntryIndex = 1,
            Fields = [FieldAt(452, "2", 9), FieldAt(448, "P2", 8)]
        });
        message.BodyGroups.Add(parties);

        EncodeOptions options = new EncodeOptions { PreserveFieldOrder = false };
        MessageEncodingResult encoded = CanonicalMessageEncoder.EncodeCanonicalMessage(message, dictionary, options);
        DecodeResult decoded = new TagValueMessageDecoder().DecodeMessage(encoded.Bytes, dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True, encoded.ErrorCode);
            Assert.That(Display(encoded.Bytes), Does.Contain("|11=ORDER|453=2|448=P1|802=1|523=S1|452=1|448=P2|452=2|10="));
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success), decoded.ErrorCode);
            Assert.That(decoded.Message!.BodyGroups, Has.Count.EqualTo(1));
            Assert.That(decoded.Message.BodyGroups[0].DeclaredCount, Is.EqualTo(2));
            Assert.That(decoded.Message.BodyGroups[0].Entries[0].Groups[0].DeclaredCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void GroupFlatteningCanBeDisabledAndUnknownFieldsCanBeExcluded()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage message = CreateMessage();
        message.BodyFields = [Field(58, "TEXT"), new FixField { Tag = 999, Value = "UNKNOWN"u8.ToArray(), IsUnknown = true }];
        FixGroup parties = new FixGroup { CountTag = 453, DelimiterTag = 448 };
        parties.Entries.Add(new FixGroupEntry { Fields = [Field(448, "P1")] });
        message.BodyGroups.Add(parties);
        EncodeOptions options = new EncodeOptions { FlattenGroups = false, IncludeUnknownFields = false };

        MessageEncodingResult result = CanonicalMessageEncoder.EncodeCanonicalMessage(message, dictionary, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorCode);
            Assert.That(Display(result.Bytes), Does.Contain("|58=TEXT|10="));
            Assert.That(Display(result.Bytes), Does.Not.Contain("|453="));
            Assert.That(Display(result.Bytes), Does.Not.Contain("|448="));
            Assert.That(Display(result.Bytes), Does.Not.Contain("|999="));
        });
    }

    [Test]
    public void RetainedEnvelopeValuesAreUsedWhenRecalculationIsDisabled()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage message = CreateMessage();
        message.HeaderFields = [Field(9, "0005")];
        message.TrailerFields = [Field(10, "123")];
        EncodeOptions options = new EncodeOptions { RecalculateBodyLength = false, RecalculateCheckSum = false };

        MessageEncodingResult result = CanonicalMessageEncoder.EncodeCanonicalMessage(message, dictionary, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorCode);
            Assert.That(Display(result.Bytes), Does.StartWith("8=FIX.4.4|9=5|35=D|"));
            Assert.That(Display(result.Bytes), Does.EndWith("10=123|"));
        });
    }

    [Test]
    public void InvalidInputsAndLimitsReturnExplicitResultsWithoutPartialBytes()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage missingBegin = new FixMessage { MsgType = "D" };
        FixMessage invalidBoolean = CreateMessage();
        invalidBoolean.BodyFields = [Field(54, "X")];
        FixMessage orphanData = CreateMessage();
        orphanData.BodyFields = [new FixField { Tag = 96, Value = [65] }];
        FixMessage missingDelimiter = CreateMessage();
        FixGroup invalidGroup = new FixGroup { CountTag = 453, DelimiterTag = 448 };
        invalidGroup.Entries.Add(new FixGroupEntry { Fields = [Field(452, "1")] });
        missingDelimiter.BodyGroups.Add(invalidGroup);
        FixMessage limitedMessage = CreateMessage();
        limitedMessage.BodyFields = [Field(58, "TOO-LARGE")];

        MessageEncodingResult begin = CanonicalMessageEncoder.EncodeCanonicalMessage(missingBegin, dictionary, new EncodeOptions());
        MessageEncodingResult boolean = CanonicalMessageEncoder.EncodeCanonicalMessage(invalidBoolean, dictionary, new EncodeOptions());
        MessageEncodingResult data = CanonicalMessageEncoder.EncodeCanonicalMessage(orphanData, dictionary, new EncodeOptions());
        MessageEncodingResult group = CanonicalMessageEncoder.EncodeCanonicalMessage(missingDelimiter, dictionary, new EncodeOptions());
        MessageEncodingResult limited = CanonicalMessageEncoder.EncodeCanonicalMessage(limitedMessage, dictionary, new EncodeOptions { MaxMessageSize = 8 });

        Assert.Multiple(() =>
        {
            AssertFailure(begin, EncodeStatus.Invalid, "missing-begin-string");
            AssertFailure(boolean, EncodeStatus.Invalid, "invalid-boolean");
            AssertFailure(data, EncodeStatus.Invalid, "data-field-not-adjacent");
            AssertFailure(group, EncodeStatus.Invalid, "missing-group-delimiter");
            AssertFailure(limited, EncodeStatus.LimitExceeded, "limit-exceeded");
            Assert.That(limited.Required, Is.GreaterThan(8));
        });
    }

    private static void AssertFailure(MessageEncodingResult result, EncodeStatus status, string errorCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(status));
            Assert.That(result.ErrorCode, Is.EqualTo(errorCode));
            Assert.That(result.ErrorMessage, Is.Not.Empty);
            Assert.That(result.Bytes, Is.Empty);
        });
    }

    private static FixMessage CreateMessage()
    {
        return new FixMessage { BeginString = "FIX.4.4", MsgType = "D" };
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
            Definition(54, FixFieldType.Boolean),
            Definition(56, FixFieldType.String),
            Definition(58, FixFieldType.String),
            new FixFieldDefinition { Tag = 95, FieldType = FixFieldType.Length, HasDataTag = true, DataTag = 96 },
            new FixFieldDefinition { Tag = 96, FieldType = FixFieldType.Data, HasLengthTag = true, LengthTag = 95 },
            Definition(448, FixFieldType.String),
            Definition(452, FixFieldType.Int),
            Definition(453, FixFieldType.NumInGroup),
            Definition(523, FixFieldType.String),
            Definition(802, FixFieldType.NumInGroup)
        ];
        dictionary.Header = [Member(FixMemberKind.Field, 49), Member(FixMemberKind.Field, 56)];
        dictionary.Trailer = [Member(FixMemberKind.Field, 10)];
        dictionary.Groups =
        [
            new FixGroupDefinition
            {
                Id = 1,
                CountTag = 453,
                DelimiterTag = 448,
                Members = [Member(FixMemberKind.Field, 448), Member(FixMemberKind.Group, 2), Member(FixMemberKind.Field, 452)]
            },
            new FixGroupDefinition
            {
                Id = 2,
                CountTag = 802,
                DelimiterTag = 523,
                Members = [Member(FixMemberKind.Field, 523)]
            }
        ];
        dictionary.Messages =
        [
            new FixMessageDefinition
            {
                Id = 100,
                MsgType = "D",
                Members =
                [
                    Member(FixMemberKind.Field, 11),
                    Member(FixMemberKind.Field, 44),
                    Member(FixMemberKind.Field, 58),
                    Member(FixMemberKind.Field, 54),
                    Member(FixMemberKind.Field, 95),
                    Member(FixMemberKind.Field, 96),
                    Member(FixMemberKind.Group, 1)
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
        return new FixField { Tag = tag, Value = Encoding.Latin1.GetBytes(value) };
    }

    private static FixField FieldAt(int tag, string value, int wireIndex)
    {
        return new FixField { Tag = tag, Value = Encoding.Latin1.GetBytes(value), WireIndex = wireIndex };
    }

    private static string Display(byte[] bytes)
    {
        return Encoding.Latin1.GetString(bytes).Replace('\u0001', '|');
    }

    private static int IndexOfField(byte[] bytes, string field)
    {
        return ByteSearch.IndexOfSequence(bytes, Encoding.Latin1.GetBytes(field));
    }

    private static int Occurrences(string value, string target)
    {
        int count = 0;
        int offset = 0;
        while (offset < value.Length)
        {
            int found = value.IndexOf(target, offset, StringComparison.Ordinal);
            if (found < 0)
            {
                return count;
            }
            count++;
            offset = found + target.Length;
        }
        return count;
    }
}
