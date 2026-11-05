using fix;

namespace fix.Tests;

[TestFixture]
public class StaticDataTests
{
    [Test]
    public void CoreTagsMatchStandardAssignments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FixTags.BeginSeqNo, Is.EqualTo(7));
            Assert.That(FixTags.BeginString, Is.EqualTo(8));
            Assert.That(FixTags.BodyLength, Is.EqualTo(9));
            Assert.That(FixTags.CheckSum, Is.EqualTo(10));
            Assert.That(FixTags.EndSeqNo, Is.EqualTo(16));
            Assert.That(FixTags.MsgSeqNum, Is.EqualTo(34));
            Assert.That(FixTags.MsgType, Is.EqualTo(35));
            Assert.That(FixTags.NewSeqNo, Is.EqualTo(36));
            Assert.That(FixTags.PossDupFlag, Is.EqualTo(43));
            Assert.That(FixTags.RefSeqNum, Is.EqualTo(45));
            Assert.That(FixTags.SenderCompID, Is.EqualTo(49));
            Assert.That(FixTags.SenderSubID, Is.EqualTo(50));
            Assert.That(FixTags.SendingTime, Is.EqualTo(52));
            Assert.That(FixTags.TargetCompID, Is.EqualTo(56));
            Assert.That(FixTags.TargetSubID, Is.EqualTo(57));
            Assert.That(FixTags.Text, Is.EqualTo(58));
            Assert.That(FixTags.SecureDataLength, Is.EqualTo(90));
            Assert.That(FixTags.SecureData, Is.EqualTo(91));
            Assert.That(FixTags.RawDataLength, Is.EqualTo(95));
            Assert.That(FixTags.RawData, Is.EqualTo(96));
            Assert.That(FixTags.PossResend, Is.EqualTo(97));
            Assert.That(FixTags.EncryptMethod, Is.EqualTo(98));
            Assert.That(FixTags.HeartBtInt, Is.EqualTo(108));
            Assert.That(FixTags.TestReqID, Is.EqualTo(112));
            Assert.That(FixTags.OnBehalfOfCompID, Is.EqualTo(115));
            Assert.That(FixTags.OnBehalfOfSubID, Is.EqualTo(116));
            Assert.That(FixTags.OrigSendingTime, Is.EqualTo(122));
            Assert.That(FixTags.GapFillFlag, Is.EqualTo(123));
            Assert.That(FixTags.DeliverToCompID, Is.EqualTo(128));
            Assert.That(FixTags.DeliverToSubID, Is.EqualTo(129));
            Assert.That(FixTags.ResetSeqNumFlag, Is.EqualTo(141));
            Assert.That(FixTags.SenderLocationID, Is.EqualTo(142));
            Assert.That(FixTags.TargetLocationID, Is.EqualTo(143));
            Assert.That(FixTags.OnBehalfOfLocationID, Is.EqualTo(144));
            Assert.That(FixTags.DeliverToLocationID, Is.EqualTo(145));
            Assert.That(FixTags.LastMsgSeqNumProcessed, Is.EqualTo(369));
            Assert.That(FixTags.RefTagID, Is.EqualTo(371));
            Assert.That(FixTags.RefMsgType, Is.EqualTo(372));
            Assert.That(FixTags.SessionRejectReason, Is.EqualTo(373));
            Assert.That(FixTags.BusinessRejectRefID, Is.EqualTo(379));
            Assert.That(FixTags.BusinessRejectReason, Is.EqualTo(380));
            Assert.That(FixTags.MaxMessageSize, Is.EqualTo(383));
            Assert.That(FixTags.NoMsgTypes, Is.EqualTo(384));
            Assert.That(FixTags.MsgDirection, Is.EqualTo(385));
            Assert.That(FixTags.Username, Is.EqualTo(553));
            Assert.That(FixTags.Password, Is.EqualTo(554));
            Assert.That(FixTags.NoHops, Is.EqualTo(627));
            Assert.That(FixTags.HopCompID, Is.EqualTo(628));
            Assert.That(FixTags.HopSendingTime, Is.EqualTo(629));
            Assert.That(FixTags.HopRefID, Is.EqualTo(630));
            Assert.That(FixTags.NextExpectedMsgSeqNum, Is.EqualTo(789));
            Assert.That(FixTags.ApplVerID, Is.EqualTo(1128));
            Assert.That(FixTags.CstmApplVerID, Is.EqualTo(1129));
            Assert.That(FixTags.RefApplVerID, Is.EqualTo(1130));
            Assert.That(FixTags.RefCstmApplVerID, Is.EqualTo(1131));
            Assert.That(FixTags.DefaultApplVerID, Is.EqualTo(1137));
            Assert.That(FixTags.ApplExtID, Is.EqualTo(1156));
            Assert.That(FixTags.RefApplExtID, Is.EqualTo(1406));
            Assert.That(FixTags.DefaultCstmApplVerID, Is.EqualTo(1408));
        });
    }

    [Test]
    public void AdministrativeMessageTypesMatchStandardAssignments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FixAdminMessageTypes.Heartbeat, Is.EqualTo("0"));
            Assert.That(FixAdminMessageTypes.TestRequest, Is.EqualTo("1"));
            Assert.That(FixAdminMessageTypes.ResendRequest, Is.EqualTo("2"));
            Assert.That(FixAdminMessageTypes.Reject, Is.EqualTo("3"));
            Assert.That(FixAdminMessageTypes.SequenceReset, Is.EqualTo("4"));
            Assert.That(FixAdminMessageTypes.Logout, Is.EqualTo("5"));
            Assert.That(FixAdminMessageTypes.Logon, Is.EqualTo("A"));
            Assert.That(FixAdminMessageTypes.BusinessMessageReject, Is.EqualTo("j"));
        });
    }

    [Test]
    public void BeginStringsMatchSupportedSessionProfiles()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FixBeginStrings.Fix40, Is.EqualTo("FIX.4.0"));
            Assert.That(FixBeginStrings.Fix41, Is.EqualTo("FIX.4.1"));
            Assert.That(FixBeginStrings.Fix42, Is.EqualTo("FIX.4.2"));
            Assert.That(FixBeginStrings.Fix43, Is.EqualTo("FIX.4.3"));
            Assert.That(FixBeginStrings.Fix44, Is.EqualTo("FIX.4.4"));
            Assert.That(FixBeginStrings.Fixt11, Is.EqualTo("FIXT.1.1"));
        });
    }

    [Test]
    public void ApplicationVersionsMatchApplVerIdCodeSet()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FixApplicationVersions.Fix27, Is.EqualTo("0"));
            Assert.That(FixApplicationVersions.Fix30, Is.EqualTo("1"));
            Assert.That(FixApplicationVersions.Fix40, Is.EqualTo("2"));
            Assert.That(FixApplicationVersions.Fix41, Is.EqualTo("3"));
            Assert.That(FixApplicationVersions.Fix42, Is.EqualTo("4"));
            Assert.That(FixApplicationVersions.Fix43, Is.EqualTo("5"));
            Assert.That(FixApplicationVersions.Fix44, Is.EqualTo("6"));
            Assert.That(FixApplicationVersions.Fix50, Is.EqualTo("7"));
            Assert.That(FixApplicationVersions.Fix50Sp1, Is.EqualTo("8"));
            Assert.That(FixApplicationVersions.Fix50Sp2, Is.EqualTo("9"));
            Assert.That(FixApplicationVersions.FixLatest, Is.EqualTo("10"));
        });
    }

    [Test]
    public void EncodingTypesMatchSofhAssignments()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FixEncodingTypes.PrivateUseMinimum, Is.EqualTo(0x0001));
            Assert.That(FixEncodingTypes.PrivateUseMaximum, Is.EqualTo(0x00FF));
            Assert.That(FixEncodingTypes.GoogleProtocolBuffersVersion10, Is.EqualTo(0x4700));
            Assert.That(FixEncodingTypes.SbeVersion10BigEndian, Is.EqualTo(0x5BE0));
            Assert.That(FixEncodingTypes.SbeVersion20BigEndian, Is.EqualTo(0x5BE1));
            Assert.That(FixEncodingTypes.Asn1PerVersion10, Is.EqualTo(0xA500));
            Assert.That(FixEncodingTypes.Asn1BerVersion10, Is.EqualTo(0xA501));
            Assert.That(FixEncodingTypes.Asn1OerVersion10, Is.EqualTo(0xA502));
            Assert.That(FixEncodingTypes.SbeVersion10LittleEndian, Is.EqualTo(0xEB50));
            Assert.That(FixEncodingTypes.SbeVersion20LittleEndian, Is.EqualTo(0xEB51));
            Assert.That(FixEncodingTypes.TagValue, Is.EqualTo(0xF000));
            Assert.That(FixEncodingTypes.FixmlVersion10, Is.EqualTo(0xF100));
            Assert.That(FixEncodingTypes.Json, Is.EqualTo(0xF500));
            Assert.That(FixEncodingTypes.FastMinimum, Is.EqualTo(0xFA01));
            Assert.That(FixEncodingTypes.FastMaximum, Is.EqualTo(0xFAFF));
            Assert.That(FixEncodingTypes.Bson, Is.EqualTo(0xFB00));
        });
    }

    [Test]
    public void ProcessingDefaultsAreBoundedAndStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(FixDefaults.Soh, Is.EqualTo(0x01));
            Assert.That(FixDefaults.InitialSequenceNumber, Is.EqualTo(1));
            Assert.That(FixDefaults.ChecksumDigits, Is.EqualTo(3));
            Assert.That(FixDefaults.ChecksumModulo, Is.EqualTo(256));
            Assert.That(FixDefaults.SofhHeaderSize, Is.EqualTo(6));
            Assert.That(FixDefaults.HeartbeatIntervalSeconds, Is.EqualTo(30));
            Assert.That(FixDefaults.MaxLatencySeconds, Is.EqualTo(120));
            Assert.That(FixDefaults.MaxTagNumber, Is.EqualTo(999999));
            Assert.That(FixDefaults.MaxFieldSize, Is.EqualTo(1048576));
            Assert.That(FixDefaults.MaxMessageSize, Is.EqualTo(16777216));
            Assert.That(FixDefaults.MaxFieldsPerMessage, Is.EqualTo(4096));
            Assert.That(FixDefaults.MaxGroupDepth, Is.EqualTo(16));
            Assert.That(FixDefaults.MaxGroupEntries, Is.EqualTo(100000));
            Assert.That(FixDefaults.MaxDictionaryDefinitions, Is.EqualTo(1000000));
            Assert.That(FixDefaults.MaxDiagnostics, Is.EqualTo(1000));
            Assert.That(FixDefaults.MaxXmlDepth, Is.EqualTo(64));
            Assert.That(FixDefaults.Latin1EncodingName, Is.EqualTo("iso-8859-1"));
            Assert.That(FixDefaults.MaxMessageSize, Is.GreaterThan(FixDefaults.MaxFieldSize));
        });
    }
}
