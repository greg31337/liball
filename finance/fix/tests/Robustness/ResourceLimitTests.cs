using System;
using System.IO;
using System.Linq;
using System.Text;
using fix;

namespace fix.Tests;

[TestFixture]
public class ResourceLimitTests
{
    [Test]
    public void MessageFieldAndFieldCountLimitsAreInclusiveAndRejectOnePastTheBoundary()
    {
        FixDictionary dictionary = MessageDictionary();
        byte[] frame = BuildFrame("35=D\u000111=ABCDEFGH\u0001");
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();

        DecodeResult exactMessage = decoder.DecodeMessage(frame, dictionary, new DecodeOptions { MaxMessageSize = frame.Length });
        DecodeResult limitedMessage = decoder.DecodeMessage(frame, dictionary, new DecodeOptions { MaxMessageSize = frame.Length - 1 });
        DecodeResult exactField = decoder.DecodeMessage(frame, dictionary, new DecodeOptions { MaxFieldSize = 8 });
        DecodeResult limitedField = decoder.DecodeMessage(frame, dictionary, new DecodeOptions { MaxFieldSize = 7 });
        DecodeResult exactCount = decoder.DecodeMessage(frame, dictionary, new DecodeOptions { MaxFieldsPerMessage = 5 });
        DecodeResult limitedCount = decoder.DecodeMessage(frame, dictionary, new DecodeOptions { MaxFieldsPerMessage = 4 });

        Assert.Multiple(() =>
        {
            Assert.That(exactMessage.Status, Is.EqualTo(DecodeStatus.Success), exactMessage.ErrorCode);
            AssertLimit(limitedMessage, "limit-exceeded");
            Assert.That(exactField.Status, Is.EqualTo(DecodeStatus.Success), exactField.ErrorCode);
            AssertLimit(limitedField, "limit-exceeded");
            Assert.That(exactCount.Status, Is.EqualTo(DecodeStatus.Success), exactCount.ErrorCode);
            AssertLimit(limitedCount, "field-count-limit-exceeded");
        });
    }

    [Test]
    public void GroupEntryAndDepthLimitsAreInclusiveAndRejectOnePastTheBoundary()
    {
        FixDictionary dictionary = GroupDictionary();
        byte[] twoEntries = BuildFrame("35=D\u0001453=2\u0001448=P1\u0001448=P2\u0001");
        byte[] nested = BuildFrame("35=D\u0001453=1\u0001448=P1\u0001802=1\u0001523=S1\u0001");
        TagValueMessageDecoder decoder = new TagValueMessageDecoder();

        DecodeResult exactEntries = decoder.DecodeMessage(twoEntries, dictionary, new DecodeOptions { MaxGroupEntries = 2 });
        DecodeResult limitedEntries = decoder.DecodeMessage(twoEntries, dictionary, new DecodeOptions { MaxGroupEntries = 1 });
        DecodeResult exactDepth = decoder.DecodeMessage(nested, dictionary, new DecodeOptions { MaxGroupDepth = 2 });
        DecodeResult limitedDepth = decoder.DecodeMessage(nested, dictionary, new DecodeOptions { MaxGroupDepth = 1 });

        Assert.Multiple(() =>
        {
            Assert.That(exactEntries.Status, Is.EqualTo(DecodeStatus.Success), exactEntries.ErrorCode);
            Assert.That(exactEntries.Message!.BodyGroups.Single().Entries, Has.Count.EqualTo(2));
            AssertLimit(limitedEntries, "group-entry-limit-exceeded");
            Assert.That(exactDepth.Status, Is.EqualTo(DecodeStatus.Success), exactDepth.ErrorCode);
            Assert.That(exactDepth.Message!.BodyGroups.Single().Entries.Single().Groups, Has.Count.EqualTo(1));
            AssertLimit(limitedDepth, "group-depth-limit-exceeded");
        });
    }

    [Test]
    public void OrchestraXmlSizeAndDepthLimitsAreInclusiveAndRejectOnePastTheBoundary()
    {
        string xml = "<repository xmlns=\"http://fixprotocol.io/2026/orchestra/repository\" name=\"Limits\" version=\"1.1\"><extension><value/></extension></repository>";
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        OrchestraReader reader = new OrchestraReader();

        OrchestraReadResult exactSize = ReadOrchestra(reader, bytes, new OrchestraReadOptions { ValidateSchema = false, MaxSourceSize = bytes.Length, MaxXmlDepth = 3 });
        OrchestraReadResult limitedSize = ReadOrchestra(reader, bytes, new OrchestraReadOptions { ValidateSchema = false, MaxSourceSize = bytes.Length - 1, MaxXmlDepth = 3 });
        OrchestraReadResult exactDepth = ReadOrchestra(reader, bytes, new OrchestraReadOptions { ValidateSchema = false, MaxSourceSize = bytes.Length, MaxXmlDepth = 3 });
        OrchestraReadResult limitedDepth = ReadOrchestra(reader, bytes, new OrchestraReadOptions { ValidateSchema = false, MaxSourceSize = bytes.Length, MaxXmlDepth = 2 });

        Assert.Multiple(() =>
        {
            Assert.That(exactSize.Success, Is.True, OrchestraCodes(exactSize));
            Assert.That(limitedSize.Success, Is.False);
            Assert.That(limitedSize.Diagnostics.Select(item => item.Code), Does.Contain("orchestra-source-size-exceeded"));
            Assert.That(exactDepth.Success, Is.True, OrchestraCodes(exactDepth));
            Assert.That(limitedDepth.Success, Is.False);
            Assert.That(limitedDepth.Diagnostics.Select(item => item.Code), Does.Contain("orchestra-xml-depth-exceeded"));
        });
    }

    [Test]
    public void QueueAndStoreRecordLimitsAdmitTheBoundaryAndRejectTheNextUnit()
    {
        TokenBucketMessageThrottle throttle = new TokenBucketMessageThrottle(new FixedClock());
        SessionThrottleSettings settings = new SessionThrottleSettings
        {
            MaxMessages = 10,
            IntervalMilliseconds = 1000,
            QueueCapacity = 2
        };
        ApplicationDecision admitted = throttle.CheckMessageThrottle(new ApplicationMessageContext { PendingQueueCount = 1 }, settings);
        ApplicationDecision full = throttle.CheckMessageThrottle(new ApplicationMessageContext { PendingQueueCount = 2 }, settings);

        MemoryMessageStore store = new MemoryMessageStore(0);
        StoredMessage oversized = Stored(1, FixDefaults.MaxMessageSize + 1);
        StoreRangeResult rejected = store.StoreMessage(oversized);
        oversized.Bytes = [];
        StoredMessage exact = Stored(1, FixDefaults.MaxMessageSize);
        StoreRangeResult stored = store.StoreMessage(exact);
        StoreRangeResult range = store.ReadMessageRange(1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(admitted.Disposition, Is.EqualTo(MessageDisposition.Accept));
            Assert.That(admitted.Code, Is.EqualTo("throttle-accepted"));
            Assert.That(full.Disposition, Is.EqualTo(MessageDisposition.Reject));
            Assert.That(full.Code, Is.EqualTo("message-queue-full"));
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Code, Is.EqualTo("invalid-record-payload"));
            Assert.That(stored.Success, Is.True, stored.Code);
            Assert.That(range.Messages, Has.Count.EqualTo(1));
            Assert.That(range.Messages[0].Bytes, Has.Length.EqualTo(FixDefaults.MaxMessageSize));
        });
    }

    [Test]
    public void GeneratedOutputFileLimitIsInclusiveAndFailureReturnsNoPartialOutput()
    {
        FixDictionary dictionary = GenerationDictionary();
        CSharpModelGenerator generator = new CSharpModelGenerator();
        GenerationResult baseline = generator.GenerateModels(dictionary, new ModelGenerationOptions());
        Assert.That(baseline.Success, Is.True, GenerationCodes(baseline));
        Assert.That(baseline.Files.Count, Is.GreaterThan(1));

        GenerationResult exact = generator.GenerateModels(dictionary, new ModelGenerationOptions { MaxFiles = baseline.Files.Count });
        GenerationResult limited = generator.GenerateModels(dictionary, new ModelGenerationOptions { MaxFiles = baseline.Files.Count - 1 });
        GenerationResult invalid = generator.GenerateModels(dictionary, new ModelGenerationOptions { MaxFiles = 0 });

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True, GenerationCodes(exact));
            Assert.That(exact.Files, Has.Count.EqualTo(baseline.Files.Count));
            Assert.That(limited.Success, Is.False);
            Assert.That(limited.Files, Is.Empty);
            Assert.That(limited.Manifest, Is.Empty);
            Assert.That(limited.Diagnostics.Select(item => item.Code), Does.Contain("generation-file-limit-exceeded"));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Diagnostics.Select(item => item.Code), Does.Contain("invalid-generation-file-limit"));
        });
    }

    private static void AssertLimit(DecodeResult result, string code)
    {
        Assert.That(result.Status, Is.EqualTo(DecodeStatus.LimitExceeded), result.ErrorCode + ":" + result.ErrorMessage);
        Assert.That(result.ErrorCode, Is.EqualTo(code));
    }

    private static OrchestraReadResult ReadOrchestra(OrchestraReader reader, byte[] bytes, OrchestraReadOptions options)
    {
        using MemoryStream source = new MemoryStream(bytes, false);
        return reader.ReadRepository(source, options);
    }

    private static StoredMessage Stored(int sequenceNumber, int byteCount)
    {
        return new StoredMessage
        {
            SequenceNumber = sequenceNumber,
            SendingTimeUtc = new DateTime(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc),
            MsgType = "D",
            Bytes = new byte[byteCount]
        };
    }

    private static byte[] BuildFrame(string bodyText)
    {
        byte[] body = Encoding.ASCII.GetBytes(bodyText);
        byte[] header = Encoding.ASCII.GetBytes("8=FIX.4.4\u00019=" + body.Length + "\u0001");
        byte[] withoutChecksum = Combine(header, body);
        int checksum = ChecksumCalculator.CalculateChecksum(withoutChecksum);
        return Combine(withoutChecksum, Encoding.ASCII.GetBytes("10=" + checksum.ToString("000") + "\u0001"));
    }

    private static byte[] Combine(byte[] first, byte[] second)
    {
        byte[] combined = new byte[first.Length + second.Length];
        first.CopyTo(combined, 0);
        second.CopyTo(combined, first.Length);
        return combined;
    }

    private static FixDictionary MessageDictionary()
    {
        return new FixDictionary
        {
            Fields =
            [
                Field(FixTags.BeginString, "BeginString", FixFieldType.String),
                Field(FixTags.BodyLength, "BodyLength", FixFieldType.Length),
                Field(FixTags.MsgType, "MsgType", FixFieldType.String),
                Field(FixTags.CheckSum, "CheckSum", FixFieldType.String),
                Field(11, "ClOrdID", FixFieldType.String)
            ],
            Header = [Member(FixMemberKind.Field, FixTags.BeginString, 0), Member(FixMemberKind.Field, FixTags.BodyLength, 1), Member(FixMemberKind.Field, FixTags.MsgType, 2)],
            Trailer = [Member(FixMemberKind.Field, FixTags.CheckSum, 0)],
            Messages = [new FixMessageDefinition { Name = "NewOrderSingle", MsgType = "D", Members = [Member(FixMemberKind.Field, 11, 0)] }]
        };
    }

    private static FixDictionary GroupDictionary()
    {
        FixDictionary dictionary = MessageDictionary();
        dictionary.Fields.Add(Field(453, "NoPartyIDs", FixFieldType.NumInGroup));
        dictionary.Fields.Add(Field(448, "PartyID", FixFieldType.String));
        dictionary.Fields.Add(Field(802, "NoPartySubIDs", FixFieldType.NumInGroup));
        dictionary.Fields.Add(Field(523, "PartySubID", FixFieldType.String));
        dictionary.Groups =
        [
            new FixGroupDefinition
            {
                Id = 1,
                Name = "Parties",
                CountTag = 453,
                DelimiterTag = 448,
                Members = [Member(FixMemberKind.Field, 448, 0), Member(FixMemberKind.Group, 2, 1)]
            },
            new FixGroupDefinition
            {
                Id = 2,
                Name = "PartySubIDs",
                CountTag = 802,
                DelimiterTag = 523,
                Members = [Member(FixMemberKind.Field, 523, 0)]
            }
        ];
        dictionary.Messages[0].Members = [Member(FixMemberKind.Group, 1, 0)];
        return dictionary;
    }

    private static FixDictionary GenerationDictionary()
    {
        return new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "LIMITS", ApplicationVersion = "LIMITS" },
            Datatypes = [new FixDatatypeDefinition { Name = "String", FieldType = FixFieldType.String }],
            Fields = [new FixFieldDefinition { Tag = 11, Name = "ClOrdID", DatatypeName = "String", FieldType = FixFieldType.String }],
            Messages =
            [
                new FixMessageDefinition
                {
                    Id = 1,
                    Name = "NewOrderSingle",
                    MsgType = "D",
                    Category = "Orders",
                    Members = [Member(FixMemberKind.Field, 11, 0)]
                }
            ]
        };
    }

    private static FixFieldDefinition Field(int tag, string name, FixFieldType type)
    {
        return new FixFieldDefinition { Tag = tag, Name = name, FieldType = type };
    }

    private static FixMemberDefinition Member(FixMemberKind kind, int referenceId, int position)
    {
        return new FixMemberDefinition { Kind = kind, ReferenceId = referenceId, Position = position, Presence = FixPresence.Optional };
    }

    private static string OrchestraCodes(OrchestraReadResult result)
    {
        return string.Join(",", result.Diagnostics.ConvertAll(item => item.Code));
    }

    private static string GenerationCodes(GenerationResult result)
    {
        return string.Join(",", result.Diagnostics.ConvertAll(item => item.Code));
    }

    private class FixedClock : ISessionClock
    {
        public DateTime GetUtcNow()
        {
            return new DateTime(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc);
        }
    }
}
