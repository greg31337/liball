using System.Text;

namespace dns.Tests.Internal.Authoritative;

public class UpdateMessageOperationsTests
{
    [Test]
    public void BuildsEncodesAndParsesEveryPrerequisiteAndChangeForm()
    {
        DnsUpdateRequest request = CompleteRequest();

        DnsUpdateMessageResult built =
            UpdateMessageOperations.BuildUpdateMessage(request, 0x1234);
        DnsEncodeResult encoded = DnsMessageEncoder.EncodeMessage(
            built.Message!,
            new DnsEncodeOptions());
        DnsDecodeResult decoded = DnsMessageDecoder.DecodeMessage(
            encoded.Bytes,
            new DnsDecodeOptions());
        DnsUpdateMessageResult parsed =
            UpdateMessageOperations.ParseUpdateMessage(decoded.Message!);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(built.Message!.Header.Identifier,
                Is.EqualTo(0x1234));
            Assert.That(built.Message.Header.Opcode,
                Is.EqualTo(DnsOpcode.Update));
            Assert.That(built.Message.Questions, Has.Count.EqualTo(1));
            Assert.That(built.Message.Questions[0].RecordType,
                Is.EqualTo(DnsRecordType.Soa));
            Assert.That(built.Message.Answers, Has.Count.EqualTo(6));
            Assert.That(built.Message.Authorities, Has.Count.EqualTo(4));
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(parsed.Success, Is.True);
            Assert.That(parsed.Request!.Prerequisites.Select(
                item => item.Kind),
                Is.EqualTo(new[]
                {
                    DnsUpdatePrerequisiteKind.NameInUse,
                    DnsUpdatePrerequisiteKind.NameNotInUse,
                    DnsUpdatePrerequisiteKind.RecordSetExists,
                    DnsUpdatePrerequisiteKind.RecordSetExistsWithValues,
                    DnsUpdatePrerequisiteKind.RecordSetDoesNotExist
                }));
            Assert.That(parsed.Request.Prerequisites[3].Records,
                Has.Count.EqualTo(2));
            Assert.That(parsed.Request.Changes.Select(item => item.Kind),
                Is.EqualTo(new[]
                {
                    DnsUpdateChangeKind.Add,
                    DnsUpdateChangeKind.DeleteRecord,
                    DnsUpdateChangeKind.DeleteRecordSet,
                    DnsUpdateChangeKind.DeleteName
                }));
            Assert.That(parsed.Request.Changes[0].TimeToLive,
                Is.EqualTo(300));
            Assert.That(parsed.Request.Changes[1].Class,
                Is.EqualTo(DnsClass.None));
            Assert.That(parsed.Request.Changes[2].Records, Is.Empty);
            Assert.That(parsed.Request.AdditionalRecords,
                Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void ConstructionRejectsZoneClassOwnerTypeTtlAndRecordViolations()
    {
        DnsUpdateRequest invalidClass = CompleteRequest();
        invalidClass.Class = DnsClass.Any;
        invalidClass.ClassValue = 255;
        DnsUpdateRequest outsideOwner = CompleteRequest();
        outsideOwner.Prerequisites[0].Name = Parse("www.other");
        DnsUpdateRequest invalidType = CompleteRequest();
        invalidType.Prerequisites[2].RecordType = DnsRecordType.Any;
        invalidType.Prerequisites[2].RecordTypeValue = 255;
        DnsUpdateRequest invalidTtl = CompleteRequest();
        invalidTtl.Prerequisites[3].Records[0].TimeToLive = 1;
        DnsUpdateRequest missingValue = CompleteRequest();
        missingValue.Changes[0].Records.Clear();

        DnsUpdateMessageResult classResult =
            UpdateMessageOperations.BuildUpdateMessage(invalidClass, 1);
        DnsUpdateMessageResult ownerResult =
            UpdateMessageOperations.BuildUpdateMessage(outsideOwner, 1);
        DnsUpdateMessageResult typeResult =
            UpdateMessageOperations.BuildUpdateMessage(invalidType, 1);
        DnsUpdateMessageResult ttlResult =
            UpdateMessageOperations.BuildUpdateMessage(invalidTtl, 1);
        DnsUpdateMessageResult valueResult =
            UpdateMessageOperations.BuildUpdateMessage(missingValue, 1);

        Assert.Multiple(() =>
        {
            Assert.That(classResult.Success, Is.False);
            Assert.That(classResult.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidZone));
            Assert.That(ownerResult.Success, Is.False);
            Assert.That(typeResult.Success, Is.False);
            Assert.That(ttlResult.Success, Is.False);
            Assert.That(valueResult.Success, Is.False);
            Assert.That(ownerResult.Diagnostics[0].HasRecordIndex, Is.True);
        });
    }

    [Test]
    public void ParsingRejectsHeaderZoneAndSectionRecordViolations()
    {
        DnsUpdateMessageResult built =
            UpdateMessageOperations.BuildUpdateMessage(
                CompleteRequest(),
                1);
        DnsMessage wrongOpcode = MessageOperations.CloneMessage(
            built.Message!);
        wrongOpcode.Header.Opcode = DnsOpcode.Query;
        wrongOpcode.Header.OpcodeValue = 0;
        DnsMessage wrongZoneType = MessageOperations.CloneMessage(
            built.Message!);
        wrongZoneType.Questions[0].RecordType = DnsRecordType.A;
        wrongZoneType.Questions[0].RecordTypeValue = 1;
        DnsMessage wrongPrerequisiteTtl = MessageOperations.CloneMessage(
            built.Message!);
        wrongPrerequisiteTtl.Answers[0].TimeToLive = 1;
        DnsMessage controlWithData = MessageOperations.CloneMessage(
            built.Message!);
        controlWithData.Authorities[2].UseRawData = true;
        controlWithData.Authorities[2].RawData = [1];

        DnsUpdateMessageResult opcode =
            UpdateMessageOperations.ParseUpdateMessage(wrongOpcode);
        DnsUpdateMessageResult zone =
            UpdateMessageOperations.ParseUpdateMessage(wrongZoneType);
        DnsUpdateMessageResult prerequisite =
            UpdateMessageOperations.ParseUpdateMessage(
                wrongPrerequisiteTtl);
        DnsUpdateMessageResult change =
            UpdateMessageOperations.ParseUpdateMessage(controlWithData);

        Assert.Multiple(() =>
        {
            Assert.That(opcode.Success, Is.False);
            Assert.That(opcode.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.ResponseMismatch));
            Assert.That(zone.Success, Is.False);
            Assert.That(zone.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidZone));
            Assert.That(prerequisite.Success, Is.False);
            Assert.That(change.Success, Is.False);
        });
    }

    [Test]
    public void OrdinaryRecordBoundaryStillRejectsUpdateControlRecords()
    {
        DnsResourceRecord control = new DnsResourceRecord();
        control.Name = Parse("www.example");
        control.RecordType = DnsRecordType.Any;
        control.RecordTypeValue = 255;
        control.Class = DnsClass.Any;
        control.ClassValue = 255;
        control.UseRawData = true;

        DnsEncodeResult result = ResourceRecordCodec.EncodeResourceRecord(
            control,
            new DnsEncodeOptions());

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics[0].Field,
            Is.EqualTo("resourceRecord.type"));
    }

    static DnsUpdateRequest CompleteRequest()
    {
        DnsUpdateRequest result = new DnsUpdateRequest();
        result.Zone = Parse("example");

        DnsUpdatePrerequisite nameInUse = Prerequisite(
            DnsUpdatePrerequisiteKind.NameInUse,
            "www.example",
            DnsRecordType.Reserved);
        DnsUpdatePrerequisite nameNotInUse = Prerequisite(
            DnsUpdatePrerequisiteKind.NameNotInUse,
            "missing.example",
            DnsRecordType.Reserved);
        DnsUpdatePrerequisite setExists = Prerequisite(
            DnsUpdatePrerequisiteKind.RecordSetExists,
            "www.example",
            DnsRecordType.A);
        DnsUpdatePrerequisite setValues = Prerequisite(
            DnsUpdatePrerequisiteKind.RecordSetExistsWithValues,
            "multi.example",
            DnsRecordType.A);
        setValues.Records.Add(AddressRecord(
            "multi.example", [192, 0, 2, 1], 0));
        setValues.Records.Add(AddressRecord(
            "multi.example", [192, 0, 2, 2], 0));
        DnsUpdatePrerequisite setMissing = Prerequisite(
            DnsUpdatePrerequisiteKind.RecordSetDoesNotExist,
            "www.example",
            DnsRecordType.Aaaa);
        result.Prerequisites.Add(nameInUse);
        result.Prerequisites.Add(nameNotInUse);
        result.Prerequisites.Add(setExists);
        result.Prerequisites.Add(setValues);
        result.Prerequisites.Add(setMissing);

        DnsUpdateChange add = Change(
            DnsUpdateChangeKind.Add,
            "new.example",
            DnsRecordType.A,
            300);
        add.Records.Add(AddressRecord(
            "new.example", [192, 0, 2, 10], 300));
        DnsUpdateChange deleteRecord = Change(
            DnsUpdateChangeKind.DeleteRecord,
            "old.example",
            DnsRecordType.A,
            0);
        deleteRecord.Records.Add(AddressRecord(
            "old.example", [192, 0, 2, 20], 300));
        DnsUpdateChange deleteSet = Change(
            DnsUpdateChangeKind.DeleteRecordSet,
            "old.example",
            DnsRecordType.Txt,
            0);
        DnsUpdateChange deleteName = Change(
            DnsUpdateChangeKind.DeleteName,
            "retired.example",
            DnsRecordType.Reserved,
            0);
        result.Changes.Add(add);
        result.Changes.Add(deleteRecord);
        result.Changes.Add(deleteSet);
        result.Changes.Add(deleteName);

        DnsResourceRecord additional = new DnsResourceRecord();
        additional.Name = Parse("metadata.example");
        additional.RecordType = (DnsRecordType)65400;
        additional.RecordTypeValue = 65400;
        additional.Class = DnsClass.Internet;
        additional.ClassValue = 1;
        additional.UseRawData = true;
        additional.RawData = [1, 2, 3];
        result.AdditionalRecords.Add(additional);
        return result;
    }

    static DnsUpdatePrerequisite Prerequisite(
        DnsUpdatePrerequisiteKind kind,
        string name,
        DnsRecordType type)
    {
        DnsUpdatePrerequisite result = new DnsUpdatePrerequisite();
        result.Kind = kind;
        result.Name = Parse(name);
        result.RecordType = type;
        result.RecordTypeValue = (int)type;
        return result;
    }

    static DnsUpdateChange Change(
        DnsUpdateChangeKind kind,
        string name,
        DnsRecordType type,
        uint timeToLive)
    {
        DnsUpdateChange result = new DnsUpdateChange();
        result.Kind = kind;
        result.Name = Parse(name);
        result.RecordType = type;
        result.RecordTypeValue = (int)type;
        result.TimeToLive = timeToLive;
        return result;
    }

    static DnsResourceRecord AddressRecord(
        string name,
        byte[] address,
        uint timeToLive)
    {
        DnsResourceRecord result = new DnsResourceRecord();
        result.Name = Parse(name);
        result.RecordType = DnsRecordType.A;
        result.RecordTypeValue = 1;
        result.Class = DnsClass.Internet;
        result.ClassValue = 1;
        result.TimeToLive = timeToLive;
        result.Data = new DnsRecordData();
        result.Data.Address = new DnsAddressData();
        result.Data.Address.Address = address;
        return result;
    }

    static DnsName Parse(string text)
    {
        DnsName result = new DnsName();
        result.IsAbsolute = true;
        string[] labels = text.Split('.');
        foreach (string label in labels)
        {
            if (label.Length > 0)
            {
                result.Labels.Add(Encoding.ASCII.GetBytes(label));
            }
        }

        return result;
    }
}
