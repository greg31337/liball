using System;
using System.Linq;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class PublicConstantFlowTests
{
    [Test]
    public void SessionAndReferenceTagsSurviveThePublicMessageCloneFlow()
    {
        int[] tags =
        [
            FixTags.SecureDataLength,
            FixTags.SecureData,
            FixTags.OnBehalfOfSubID,
            FixTags.DeliverToCompID,
            FixTags.DeliverToSubID,
            FixTags.OnBehalfOfLocationID,
            FixTags.DeliverToLocationID,
            FixTags.LastMsgSeqNumProcessed,
            FixTags.MaxMessageSize,
            FixTags.NoMsgTypes,
            FixTags.MsgDirection,
            FixTags.NoHops,
            FixTags.HopCompID,
            FixTags.HopSendingTime,
            FixTags.HopRefID,
            FixTags.RefApplVerID,
            FixTags.RefCstmApplVerID,
            FixTags.RefApplExtID,
            FixTags.DefaultCstmApplVerID
        ];
        FixMessage message = new FixMessage { MsgType = FixAdminMessageTypes.Logon };
        for (int index = 0; index < tags.Length; index++)
        {
            message.BodyFields.Add(new FixField { Tag = tags[index], Value = Encoding.ASCII.GetBytes(index.ToString()) });
        }

        MessageEditResult cloned = new FixMessageOperations().CloneMessage(message);

        Assert.Multiple(() =>
        {
            Assert.That(cloned.Success, Is.True, cloned.ErrorCode);
            Assert.That(cloned.Message, Is.Not.Null);
            Assert.That(cloned.Message!.BodyFields.Select(item => item.Tag), Is.EqualTo(tags));
            Assert.That(cloned.Message.BodyFields, Is.Not.SameAs(message.BodyFields));
        });
    }

    [Test]
    public void MaximumTagConstantDefinesThePublicEditingBoundary()
    {
        FixMessage message = new FixMessage();
        IMessageOperations operations = new FixMessageOperations();

        MessageEditResult maximum = operations.SetField(message, new FixField { Tag = FixDefaults.MaxTagNumber, Value = [1] });
        MessageEditResult beyond = operations.SetField(message, new FixField { Tag = FixDefaults.MaxTagNumber + 1, Value = [2] });

        Assert.Multiple(() =>
        {
            Assert.That(maximum.Success, Is.True, maximum.ErrorCode);
            Assert.That(maximum.AffectedTag, Is.EqualTo(FixDefaults.MaxTagNumber));
            Assert.That(beyond.Success, Is.False);
            Assert.That(beyond.ErrorCode, Is.EqualTo("invalid-tag"));
            Assert.That(message.BodyFields.Single().Tag, Is.EqualTo(FixDefaults.MaxTagNumber));
        });
    }

    [TestCase(FixApplicationVersions.Fix27, false)]
    [TestCase(FixApplicationVersions.Fix30, false)]
    [TestCase(FixApplicationVersions.Fix40, true)]
    [TestCase(FixApplicationVersions.Fix41, true)]
    [TestCase(FixApplicationVersions.Fix42, true)]
    [TestCase(FixApplicationVersions.Fix43, true)]
    [TestCase(FixApplicationVersions.Fix44, true)]
    [TestCase(FixApplicationVersions.Fix50, true)]
    [TestCase(FixApplicationVersions.Fix50Sp1, true)]
    [TestCase(FixApplicationVersions.Fix50Sp2, true)]
    [TestCase(FixApplicationVersions.FixLatest, true)]
    public void EveryApplicationVersionConstantDrivesDictionaryResolution(string version, bool supported)
    {
        DictionaryLoadResult result = new EmbeddedDictionaryProvider(null).ResolveDictionary(new FixDictionaryIdentity { ApplicationVersion = version });

        Assert.That(result.Success, Is.EqualTo(supported), string.Join(",", result.Diagnostics.ConvertAll(item => item.Code)));
        if (supported)
        {
            Assert.That(result.Dictionary, Is.Not.Null);
            Assert.That(result.Dictionary!.Identity.ApplicationVersion, Is.EqualTo(version));
        }
        else
        {
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("dictionary-not-found"));
        }
    }

    [TestCase(BinaryEncodingType.Unknown)]
    [TestCase(BinaryEncodingType.TagValue)]
    [TestCase(BinaryEncodingType.Sbe)]
    [TestCase(BinaryEncodingType.Fixml)]
    [TestCase(BinaryEncodingType.Json)]
    [TestCase(BinaryEncodingType.Protobuf)]
    [TestCase(BinaryEncodingType.Proprietary)]
    public void EveryBinaryEncodingEnumValueSurvivesExternalCodecContextFlow(BinaryEncodingType encoding)
    {
        BinaryMessageContext context = new BinaryMessageContext { EncodingType = encoding, MsgType = "D" };
        IBinaryMessageCodec codec = new ContextCodec();

        BinaryEncodeResult encoded = codec.EncodeBinary(new FixMessage { MsgType = "D" }, context, new byte[1]);
        BinaryDecodeResult decoded = codec.DecodeBinary([68], encoded.Context);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Status, Is.EqualTo(EncodeStatus.Success));
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(encoded.Context.EncodingType, Is.EqualTo(encoding));
            Assert.That(decoded.Context.EncodingType, Is.EqualTo(encoding));
            Assert.That(decoded.Message!.MsgType, Is.EqualTo("D"));
        });
    }

    [Test]
    public void UnknownMemberAndRuleKindsFlowThroughValidationAndWriting()
    {
        FixDictionary dictionary = DictionaryImplementationContractTests.CanonicalDictionary();
        dictionary.Messages[0].Members.Add(new FixMemberDefinition { Name = "Unknown", Kind = FixMemberKind.Unknown, ReferenceId = 99 });
        using System.IO.MemoryStream dictionaryDestination = new System.IO.MemoryStream();
        DictionaryLoadResult dictionaryWrite = new QuickFixDictionaryWriter().WriteDictionary(dictionaryDestination, dictionary, new FixDictionaryOptions());
        OrchestraRule rule = new OrchestraRule { Name = "defaultRule", Kind = OrchestraRuleKind.Unknown, Expression = "true" };
        OrchestraRepository repository = new OrchestraRepository
        {
            Version = OrchestraVersion.Version11,
            Metadata = new OrchestraMetadata { Name = "Rules", Release = "FIX.Latest" },
            Messages = [new OrchestraMessageDefinition { Id = "1", Name = "Order", MessageType = "D", Rules = [rule] }]
        };
        using System.IO.MemoryStream destination = new System.IO.MemoryStream();
        OrchestraReadResult written = new OrchestraWriter().WriteRepository(destination, repository, new OrchestraWriteOptions());
        string xml = Encoding.UTF8.GetString(destination.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(dictionaryWrite.Success, Is.False);
            Assert.That(dictionaryWrite.Diagnostics.Exists(item => item.Code == "unsupported-member-kind"), Is.True);
            Assert.That(written.Success, Is.True, string.Join(",", written.Diagnostics.ConvertAll(item => item.Code)));
            Assert.That(rule.Kind, Is.EqualTo(OrchestraRuleKind.Unknown));
            Assert.That(xml, Does.Contain("<fixr:when>true</fixr:when>"));
        });
    }

    private class ContextCodec : IBinaryMessageCodec
    {
        public BinaryDecodeResult DecodeBinary(ReadOnlySpan<byte> source, BinaryMessageContext context)
        {
            return new BinaryDecodeResult
            {
                Status = source.Length == 0 ? DecodeStatus.Incomplete : DecodeStatus.Success,
                Message = source.Length == 0 ? null : new FixMessage { MsgType = Encoding.ASCII.GetString(source) },
                Context = new BinaryMessageContext { EncodingType = context.EncodingType, MsgType = context.MsgType },
                Consumed = source.Length,
                Required = 1
            };
        }

        public BinaryEncodeResult EncodeBinary(FixMessage message, BinaryMessageContext context, Span<byte> destination)
        {
            destination[0] = (byte)message.MsgType[0];
            return new BinaryEncodeResult
            {
                Status = EncodeStatus.Success,
                Context = new BinaryMessageContext { EncodingType = context.EncodingType, MsgType = message.MsgType },
                BytesWritten = 1,
                RequiredSize = 1
            };
        }
    }
}
