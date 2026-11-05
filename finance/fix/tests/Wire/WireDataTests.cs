using fix;

namespace fix.Tests;

[TestFixture]
public class WireDataTests
{
    [Test]
    public void MessageRetainsSectionOrderDuplicatesAndRawBytes()
    {
        FixField beginString = new FixField
        {
            Tag = FixTags.BeginString,
            Value = [70, 73, 88, 46, 52, 46, 52],
            WireIndex = 0,
            SourceOffset = 0,
            SourceLength = 10
        };
        FixField firstText = new FixField
        {
            Tag = FixTags.Text,
            Value = [65, FixDefaults.Soh, 255],
            WireIndex = 4,
            IsUnknown = true,
            IsEncodedData = true
        };
        FixField secondText = new FixField
        {
            Tag = FixTags.Text,
            Value = [66],
            WireIndex = 5
        };
        FixField checksum = new FixField
        {
            Tag = FixTags.CheckSum,
            Value = [48, 48, 48],
            WireIndex = 6
        };
        FixMessage message = new FixMessage
        {
            BeginString = FixBeginStrings.Fix44,
            MsgType = "D",
            OriginalBytes = [56, 61, 70, 73, 88, FixDefaults.Soh]
        };

        message.HeaderFields.Add(beginString);
        message.BodyFields.Add(firstText);
        message.BodyFields.Add(secondText);
        message.TrailerFields.Add(checksum);

        Assert.Multiple(() =>
        {
            Assert.That(message.HeaderFields[0], Is.SameAs(beginString));
            Assert.That(message.BodyFields[0], Is.SameAs(firstText));
            Assert.That(message.BodyFields[1], Is.SameAs(secondText));
            Assert.That(message.BodyFields[0].Tag, Is.EqualTo(message.BodyFields[1].Tag));
            Assert.That(message.BodyFields[0].Value, Is.EqualTo(new byte[] { 65, 1, 255 }));
            Assert.That(message.BodyFields[0].IsUnknown, Is.True);
            Assert.That(message.BodyFields[0].IsEncodedData, Is.True);
            Assert.That(message.TrailerFields[0], Is.SameAs(checksum));
            Assert.That(message.OriginalBytes, Is.EqualTo(new byte[] { 56, 61, 70, 73, 88, 1 }));
        });
    }

    [Test]
    public void RepeatingGroupEntriesAreIndependentAndMayContainNestedGroups()
    {
        FixGroupEntry firstEntry = new FixGroupEntry { EntryIndex = 0 };
        FixGroupEntry secondEntry = new FixGroupEntry { EntryIndex = 1 };
        firstEntry.Fields.Add(new FixField { Tag = 448, Value = [65] });
        secondEntry.Fields.Add(new FixField { Tag = 448, Value = [66] });

        FixGroup nested = new FixGroup
        {
            CountTag = 802,
            DelimiterTag = 523,
            DeclaredCount = 1,
            WireIndex = 9
        };
        nested.Entries.Add(new FixGroupEntry
        {
            EntryIndex = 0,
            Fields = [new FixField { Tag = 523, Value = [83, 85, 66] }]
        });
        firstEntry.Groups.Add(nested);

        FixGroup parties = new FixGroup
        {
            CountTag = 453,
            DelimiterTag = 448,
            DeclaredCount = 2,
            WireIndex = 6,
            Entries = [firstEntry, secondEntry]
        };
        FixGroup emptyGroup = new FixGroup
        {
            CountTag = 711,
            DelimiterTag = 311,
            DeclaredCount = 0
        };
        FixMessage message = new FixMessage();
        message.BodyGroups.Add(parties);
        message.BodyGroups.Add(emptyGroup);

        firstEntry.Fields[0].Value[0] = 90;

        Assert.Multiple(() =>
        {
            Assert.That(parties.Entries, Has.Count.EqualTo(2));
            Assert.That(parties.Entries[0].Fields[0].Value, Is.EqualTo(new byte[] { 90 }));
            Assert.That(parties.Entries[1].Fields[0].Value, Is.EqualTo(new byte[] { 66 }));
            Assert.That(parties.Entries[0].Groups[0], Is.SameAs(nested));
            Assert.That(nested.Entries[0].Fields[0].Value, Is.EqualTo(new byte[] { 83, 85, 66 }));
            Assert.That(emptyGroup.Entries, Is.Empty);
            Assert.That(emptyGroup.DeclaredCount, Is.Zero);
        });
    }

    [Test]
    public void WireOptionsUseBoundedStrictDefaults()
    {
        DecodeOptions decode = new DecodeOptions();
        EncodeOptions encode = new EncodeOptions();
        FrameOptions frame = new FrameOptions();

        Assert.Multiple(() =>
        {
            Assert.That(decode.MaxMessageSize, Is.EqualTo(FixDefaults.MaxMessageSize));
            Assert.That(decode.MaxFieldSize, Is.EqualTo(FixDefaults.MaxFieldSize));
            Assert.That(decode.MaxFieldsPerMessage, Is.EqualTo(FixDefaults.MaxFieldsPerMessage));
            Assert.That(decode.MaxGroupDepth, Is.EqualTo(FixDefaults.MaxGroupDepth));
            Assert.That(decode.MaxGroupEntries, Is.EqualTo(FixDefaults.MaxGroupEntries));
            Assert.That(decode.VerifyBodyLength, Is.True);
            Assert.That(decode.VerifyCheckSum, Is.True);
            Assert.That(decode.RequireCanonicalEnvelope, Is.True);
            Assert.That(decode.AllowEmptyValues, Is.False);
            Assert.That(decode.PreserveUnknownFields, Is.True);
            Assert.That(decode.PreserveOriginalBytes, Is.True);
            Assert.That(decode.TextEncodingName, Is.EqualTo(FixDefaults.Latin1EncodingName));
            Assert.That(encode.MaxMessageSize, Is.EqualTo(FixDefaults.MaxMessageSize));
            Assert.That(encode.PreserveFieldOrder, Is.True);
            Assert.That(encode.IncludeUnknownFields, Is.True);
            Assert.That(encode.FlattenGroups, Is.True);
            Assert.That(encode.RecalculateBodyLength, Is.True);
            Assert.That(encode.RecalculateCheckSum, Is.True);
            Assert.That(frame.MaxFrameSize, Is.EqualTo(FixDefaults.MaxMessageSize));
            Assert.That(frame.RecoverFromJunk, Is.False);
            Assert.That(frame.VerifyBodyLength, Is.True);
            Assert.That(frame.VerifyCheckSum, Is.True);
        });
    }

    [Test]
    public void OperationResultsRetainExplicitSuccessIncompleteAndErrorState()
    {
        FixField field = new FixField { Tag = FixTags.MsgType, Value = [68] };
        FieldParseResult fieldResult = new FieldParseResult
        {
            Status = DecodeStatus.Success,
            Field = field,
            Consumed = 5
        };
        FixMessage message = new FixMessage { MsgType = "D" };
        DecodeResult decode = new DecodeResult
        {
            Status = DecodeStatus.Success,
            Message = message,
            Consumed = 42,
            Required = 42,
            FieldResults = [fieldResult]
        };
        EncodeResult encode = new EncodeResult
        {
            Status = EncodeStatus.DestinationTooSmall,
            RequiredSize = 42,
            ErrorCode = "destination-too-small"
        };
        FrameResult frame = new FrameResult
        {
            Status = FrameStatus.Complete,
            FrameOffset = 3,
            FrameLength = 42,
            Consumed = 45,
            Required = 45,
            IsSofh = true,
            EncodingType = FixEncodingTypes.SbeVersion10LittleEndian
        };
        MessageEditResult edit = new MessageEditResult
        {
            Success = true,
            Message = message,
            AffectedTag = FixTags.MsgType
        };

        Assert.Multiple(() =>
        {
            Assert.That(decode.Message, Is.SameAs(message));
            Assert.That(decode.FieldResults[0].Field, Is.SameAs(field));
            Assert.That(decode.Consumed, Is.EqualTo(42));
            Assert.That(encode.Status, Is.EqualTo(EncodeStatus.DestinationTooSmall));
            Assert.That(encode.RequiredSize, Is.EqualTo(42));
            Assert.That(encode.ErrorCode, Is.EqualTo("destination-too-small"));
            Assert.That(frame.FrameOffset, Is.EqualTo(3));
            Assert.That(frame.FrameLength, Is.EqualTo(42));
            Assert.That(frame.Consumed, Is.EqualTo(45));
            Assert.That(frame.IsSofh, Is.True);
            Assert.That(frame.EncodingType, Is.EqualTo(FixEncodingTypes.SbeVersion10LittleEndian));
            Assert.That(edit.Success, Is.True);
            Assert.That(edit.Message, Is.SameAs(message));
            Assert.That(edit.AffectedTag, Is.EqualTo(FixTags.MsgType));
        });
    }

    [Test]
    public void NewResultContainersHaveSafeNonSuccessDefaults()
    {
        DecodeResult decode = new DecodeResult();
        EncodeResult encode = new EncodeResult();
        FrameResult frame = new FrameResult();
        FieldParseResult field = new FieldParseResult();
        MessageEditResult edit = new MessageEditResult();

        Assert.Multiple(() =>
        {
            Assert.That(decode.Status, Is.EqualTo(DecodeStatus.Incomplete));
            Assert.That(decode.Message, Is.Null);
            Assert.That(decode.ErrorOffset, Is.EqualTo(-1));
            Assert.That(encode.Status, Is.EqualTo(EncodeStatus.Invalid));
            Assert.That(frame.Status, Is.EqualTo(FrameStatus.Incomplete));
            Assert.That(frame.ErrorOffset, Is.EqualTo(-1));
            Assert.That(field.Status, Is.EqualTo(DecodeStatus.Incomplete));
            Assert.That(field.Field, Is.Null);
            Assert.That(edit.Success, Is.False);
            Assert.That(edit.Message, Is.Null);
        });
    }
}
