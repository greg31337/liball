using System;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class TagValueDecoderTests
{
    [Test]
    public void DecoderComposesFramingFlatFieldsBinaryDataAndGroups()
    {
        FixDictionary dictionary = CreateDictionary();
        byte[] bodyPrefix = Wire("35=D|49=SENDER|453=1|448=P1|95=5|96=");
        byte[] binaryAndTail = Combine(new byte[] { 65, 1, 66, 0, 255, 1 }, Wire("999=X|"));
        byte[] frame = BuildFrame(Combine(bodyPrefix, binaryAndTail), 0);
        byte[] original = frame.AsSpan().ToArray();
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();

        DecodeResult result = decoder.DecodeMessage(frame, dictionary, new DecodeOptions());
        frame[0] = 88;
        frame[bodyPrefix.Length] = 88;

        Assert.That(result.Status, Is.EqualTo(DecodeStatus.Success), result.ErrorCode);
        Assert.That(result.Message, Is.Not.Null);
        FixMessage message = result.Message!;
        Assert.Multiple(() =>
        {
            Assert.That(message.BeginString, Is.EqualTo("FIX.4.4"));
            Assert.That(message.MsgType, Is.EqualTo("D"));
            Assert.That(message.HeaderFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 8, 9, 35, 49 }));
            Assert.That(message.BodyFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 453, 448, 95, 96, 999 }));
            Assert.That(message.TrailerFields.ConvertAll(field => field.Tag), Is.EqualTo(new[] { 10 }));
            Assert.That(message.BodyFields[3].Value, Is.EqualTo(new byte[] { 65, 1, 66, 0, 255 }));
            Assert.That(message.BodyFields[3].IsEncodedData, Is.True);
            Assert.That(message.BodyFields[4].IsUnknown, Is.True);
            Assert.That(message.BodyGroups, Has.Count.EqualTo(1));
            Assert.That(message.BodyGroups[0].Entries, Has.Count.EqualTo(1));
            Assert.That(Text(message.BodyGroups[0].Entries[0].Fields[0]), Is.EqualTo("P1"));
            Assert.That(message.OriginalBytes, Is.EqualTo(original));
            Assert.That(result.FieldResults, Has.Count.EqualTo(10));
            Assert.That(result.Consumed, Is.EqualTo(original.Length));
            Assert.That(result.Required, Is.EqualTo(original.Length));
        });
    }

    [Test]
    public void DecoderReturnsOnlyTheFirstMessageFromConcatenatedInput()
    {
        FixDictionary dictionary = CreateDictionary();
        byte[] first = BuildFrame(Wire("35=D|49=A|"), 0);
        byte[] second = BuildFrame(Wire("35=0|49=B|"), 0);
        byte[] source = Combine(first, second);

        DecodeResult result = new TagValueMessageDecoder().DecodeMessage(source, dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(result.Message!.MsgType, Is.EqualTo("D"));
            Assert.That(result.Consumed, Is.EqualTo(first.Length));
            Assert.That(result.Required, Is.EqualTo(first.Length));
            Assert.That(result.Message.OriginalBytes.Length, Is.EqualTo(first.Length));
        });
    }

    [Test]
    public void CanonicalAndPermissiveEnvelopePoliciesDifferExplicitly()
    {
        FixDictionary dictionary = CreateDictionary();
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();
        byte[] lateType = BuildFrame(Wire("49=SENDER|35=D|"), 0);
        byte[] duplicateType = BuildFrame(Wire("35=D|35=0|49=SENDER|"), 0);
        DecodeOptions permissive = new DecodeOptions { RequireCanonicalEnvelope = false };

        DecodeResult strictOrder = decoder.DecodeMessage(lateType, dictionary, new DecodeOptions());
        DecodeResult permissiveOrder = decoder.DecodeMessage(lateType, dictionary, permissive);
        DecodeResult strictDuplicate = decoder.DecodeMessage(duplicateType, dictionary, new DecodeOptions());
        DecodeResult permissiveDuplicate = decoder.DecodeMessage(duplicateType, dictionary, permissive);

        Assert.Multiple(() =>
        {
            Assert.That(strictOrder.ErrorCode, Is.EqualTo("invalid-envelope-order"));
            Assert.That(permissiveOrder.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(permissiveOrder.Message!.MsgType, Is.EqualTo("D"));
            Assert.That(strictDuplicate.ErrorCode, Is.EqualTo("duplicate-envelope-field"));
            Assert.That(permissiveDuplicate.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(permissiveDuplicate.Message!.MsgType, Is.EqualTo("D"));
            Assert.That(permissiveDuplicate.Message.HeaderFields.FindAll(field => field.Tag == 35), Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void BodyLengthAndChecksumVerificationOptionsAreHonored()
    {
        FixDictionary dictionary = CreateDictionary();
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();
        byte[] wrongLength = BuildFrame(Wire("35=D|49=SENDER|"), 1);
        byte[] wrongChecksum = BuildFrame(Wire("35=D|49=SENDER|"), 0);
        int bodyStart = FindBodyStart(wrongChecksum);
        wrongChecksum[bodyStart + 8] = 88;

        DecodeResult strictLength = decoder.DecodeMessage(wrongLength, dictionary, new DecodeOptions());
        DecodeResult relaxedLength = decoder.DecodeMessage(wrongLength, dictionary, new DecodeOptions { VerifyBodyLength = false });
        DecodeResult strictChecksum = decoder.DecodeMessage(wrongChecksum, dictionary, new DecodeOptions());
        DecodeResult relaxedChecksum = decoder.DecodeMessage(wrongChecksum, dictionary, new DecodeOptions { VerifyCheckSum = false });

        Assert.Multiple(() =>
        {
            Assert.That(strictLength.ErrorCode, Is.EqualTo("body-length-mismatch"));
            Assert.That(relaxedLength.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(strictChecksum.ErrorCode, Is.EqualTo("checksum-mismatch"));
            Assert.That(relaxedChecksum.Status, Is.EqualTo(DecodeStatus.Success));
        });
    }

    [Test]
    public void UnknownFieldPreservationCanBeDisabledWithoutLosingDiagnostics()
    {
        FixDictionary dictionary = CreateDictionary();
        byte[] frame = BuildFrame(Wire("35=D|999=X|49=SENDER|"), 0);
        DecodeOptions options = new DecodeOptions { PreserveUnknownFields = false };

        DecodeResult result = new TagValueMessageDecoder().DecodeMessage(frame, dictionary, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(result.Message!.BodyFields.Exists(field => field.Tag == 999), Is.False);
            Assert.That(result.FieldResults.Exists(fieldResult => fieldResult.Field != null && fieldResult.Field.Tag == 999), Is.True);
        });
    }

    [Test]
    public void KnownDatatypeFailuresReturnAbsoluteSourceOffsets()
    {
        FixDictionary dictionary = CreateDictionary();
        byte[] frame = BuildFrame(Wire("35=D|54=X|"), 0);
        int fieldOffset = ByteSearch.IndexOfSequence(frame, "54=X"u8);

        DecodeResult result = new TagValueMessageDecoder().DecodeMessage(frame, dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DecodeStatus.Invalid));
            Assert.That(result.ErrorCode, Is.EqualTo("invalid-boolean"));
            Assert.That(result.ErrorOffset, Is.EqualTo(fieldOffset + 3));
            Assert.That(result.ErrorMessage, Is.Not.Empty);
        });
    }

    [Test]
    public void MissingEnvelopeFieldsDictionaryMismatchAndMalformedGroupsAreInvalid()
    {
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();
        FixDictionary dictionary = CreateDictionary();
        byte[] missingType = BuildFrame(Wire("49=SENDER|"), 0);
        byte[] malformedGroup = BuildFrame(Wire("35=D|453=2|448=P1|"), 0);
        FixDictionary mismatchedDictionary = CreateDictionary();
        mismatchedDictionary.Identity.BeginString = "FIXT.1.1";

        DecodeResult missing = decoder.DecodeMessage(missingType, dictionary, new DecodeOptions());
        DecodeResult emptyType = decoder.DecodeMessage(BuildFrame(Wire("35=|"), 0), dictionary, new DecodeOptions { AllowEmptyValues = true });
        DecodeResult mismatch = decoder.DecodeMessage(BuildFrame(Wire("35=D|"), 0), mismatchedDictionary, new DecodeOptions());
        DecodeResult group = decoder.DecodeMessage(malformedGroup, dictionary, new DecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(missing.ErrorCode, Is.EqualTo("missing-msg-type"));
            Assert.That(emptyType.ErrorCode, Is.EqualTo("empty-msg-type"));
            Assert.That(mismatch.ErrorCode, Is.EqualTo("begin-string-mismatch"));
            Assert.That(group.ErrorCode, Is.EqualTo("premature-group-termination"));
            Assert.That(group.ErrorOffset, Is.GreaterThan(0));
        });
    }

    [Test]
    public void IncompleteInputAndLimitsUseExplicitNonThrowingResults()
    {
        FixDictionary dictionary = CreateDictionary();
        byte[] frame = BuildFrame(Wire("35=D|49=SENDER|"), 0);
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();

        DecodeResult incomplete = decoder.DecodeMessage(frame.AsSpan(0, frame.Length - 1), dictionary, new DecodeOptions());
        DecodeResult messageLimit = decoder.DecodeMessage(frame, dictionary, new DecodeOptions { MaxMessageSize = frame.Length - 1 });
        DecodeResult fieldLimit = decoder.DecodeMessage(frame, dictionary, new DecodeOptions { MaxFieldSize = 3 });
        DecodeResult invalidLimit = decoder.DecodeMessage(frame, dictionary, new DecodeOptions { MaxGroupDepth = -1 });
        DecodeResult encoding = decoder.DecodeMessage(frame, dictionary, new DecodeOptions { TextEncodingName = "utf-8" });

        Assert.Multiple(() =>
        {
            Assert.That(incomplete.Status, Is.EqualTo(DecodeStatus.Incomplete));
            Assert.That(incomplete.Required, Is.EqualTo(frame.Length));
            Assert.That(messageLimit.Status, Is.EqualTo(DecodeStatus.LimitExceeded));
            Assert.That(fieldLimit.Status, Is.EqualTo(DecodeStatus.LimitExceeded));
            Assert.That(invalidLimit.ErrorCode, Is.EqualTo("invalid-limit"));
            Assert.That(encoding.ErrorCode, Is.EqualTo("unsupported-text-encoding"));
        });
    }

    private static FixDictionary CreateDictionary()
    {
        FixGroupDefinition group = new FixGroupDefinition
        {
            Id = 1,
            CountTag = 453,
            DelimiterTag = 448,
            Members = [new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 448 }]
        };
        FixMessageDefinition order = new FixMessageDefinition
        {
            Id = 1,
            MsgType = "D",
            Members = [new FixMemberDefinition { Kind = FixMemberKind.Group, ReferenceId = 1 }]
        };
        FixDictionary dictionary = new FixDictionary();
        dictionary.Identity.BeginString = "FIX.4.4";
        dictionary.Fields =
        [
            new FixFieldDefinition { Tag = 8, FieldType = FixFieldType.String },
            new FixFieldDefinition { Tag = 9, FieldType = FixFieldType.Length },
            new FixFieldDefinition { Tag = 10, FieldType = FixFieldType.String },
            new FixFieldDefinition { Tag = 35, FieldType = FixFieldType.String },
            new FixFieldDefinition { Tag = 49, FieldType = FixFieldType.String },
            new FixFieldDefinition { Tag = 54, FieldType = FixFieldType.Boolean },
            new FixFieldDefinition { Tag = 448, FieldType = FixFieldType.String },
            new FixFieldDefinition { Tag = 453, FieldType = FixFieldType.NumInGroup },
            new FixFieldDefinition { Tag = 95, FieldType = FixFieldType.Length, HasDataTag = true, DataTag = 96 },
            new FixFieldDefinition { Tag = 96, FieldType = FixFieldType.Data, HasLengthTag = true, LengthTag = 95 }
        ];
        dictionary.Header.Add(new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 49 });
        dictionary.Trailer.Add(new FixMemberDefinition { Kind = FixMemberKind.Field, ReferenceId = 10 });
        dictionary.Groups.Add(group);
        dictionary.Messages.Add(order);
        return dictionary;
    }

    private static byte[] BuildFrame(byte[] body, int declaredLengthAdjustment)
    {
        int declaredLength = body.Length + declaredLengthAdjustment;
        byte[] header = Encoding.ASCII.GetBytes($"8=FIX.4.4\u00019={declaredLength}\u0001");
        byte[] checksumInput = Combine(header, body);
        int checksum = ChecksumCalculator.CalculateChecksum(checksumInput);
        byte[] trailer = Encoding.ASCII.GetBytes($"10={checksum:000}\u0001");
        return Combine(checksumInput, trailer);
    }

    private static int FindBodyStart(byte[] frame)
    {
        int delimiters = 0;
        for (int index = 0; index < frame.Length; index++)
        {
            if (frame[index] == FixDefaults.Soh)
            {
                delimiters++;
                if (delimiters == 2)
                {
                    return index + 1;
                }
            }
        }
        return -1;
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

    private static string Text(FixField field)
    {
        return Encoding.ASCII.GetString(field.Value);
    }
}
