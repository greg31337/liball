namespace dns.Tests.Api;

public class MaintenanceDataTests
{
    [Test]
    public void TransferRequestsDefaultToBoundedTcpAxfrAndOwnContext()
    {
        DnsTransferRequest first = new DnsTransferRequest();
        DnsTransferRequest second = new DnsTransferRequest();
        first.Authorization.AllowTransfers = true;
        first.HasCurrentSerial = true;
        first.CurrentSerial = uint.MaxValue;

        Assert.Multiple(() =>
        {
            Assert.That(first.Kind, Is.EqualTo(DnsTransferKind.Axfr));
            Assert.That(first.Class, Is.EqualTo(DnsClass.Internet));
            Assert.That(first.ClassValue, Is.EqualTo(1));
            Assert.That(first.Transport, Is.EqualTo(DnsTransportKind.Tcp));
            Assert.That(first.MaximumMessages, Is.EqualTo(100000));
            Assert.That(first.MaximumRecords, Is.EqualTo(1000000));
            Assert.That(first.MaximumBytes, Is.EqualTo(1073741824));
            Assert.That(first.HasCurrentSerial, Is.True);
            Assert.That(first.CurrentSerial, Is.EqualTo(uint.MaxValue));
            Assert.That(second.Zone, Is.Not.SameAs(first.Zone));
            Assert.That(second.Endpoint, Is.Not.SameAs(first.Endpoint));
            Assert.That(second.TransportOptions, Is.Not.SameAs(first.TransportOptions));
            Assert.That(second.Authorization, Is.Not.SameAs(first.Authorization));
            Assert.That(second.Authorization.AllowTransfers, Is.False);
        });
    }

    [Test]
    public void TransferResultsRetainFallbackSerialsCountsAndIndependentCollections()
    {
        DnsTransferResult first = new DnsTransferResult();
        DnsTransferResult second = new DnsTransferResult();
        first.Success = true;
        first.Kind = DnsTransferKind.Ixfr;
        first.TransportStatus = DnsTransportStatus.Success;
        first.ResponseCode = (DnsResponseCode)4095;
        first.ResponseCodeValue = 4095;
        first.ZoneName = new DnsName();
        first.Zone = new DnsZone();
        first.Messages.Add(new DnsMessage());
        first.JournalEntries.Add(new DnsZoneJournalEntry());
        first.UpToDate = true;
        first.FellBackToAxfr = true;
        first.HasInitialSerial = true;
        first.InitialSerial = 0;
        first.HasFinalSerial = true;
        first.FinalSerial = uint.MaxValue;
        first.MessageCount = int.MaxValue;
        first.RecordCount = int.MaxValue;
        first.ByteCount = long.MaxValue;
        first.Tsig = new DnsTsigResult();
        first.Diagnostics.Add(new DnsDiagnostic());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Kind, Is.EqualTo(DnsTransferKind.Ixfr));
            Assert.That(first.TransportStatus, Is.EqualTo(DnsTransportStatus.Success));
            Assert.That((int)first.ResponseCode, Is.EqualTo(4095));
            Assert.That(first.ResponseCodeValue, Is.EqualTo(4095));
            Assert.That(first.Messages, Has.Count.EqualTo(1));
            Assert.That(first.JournalEntries, Has.Count.EqualTo(1));
            Assert.That(first.UpToDate, Is.True);
            Assert.That(first.FellBackToAxfr, Is.True);
            Assert.That(first.HasInitialSerial, Is.True);
            Assert.That(first.InitialSerial, Is.Zero);
            Assert.That(first.HasFinalSerial, Is.True);
            Assert.That(first.FinalSerial, Is.EqualTo(uint.MaxValue));
            Assert.That(first.MessageCount, Is.EqualTo(int.MaxValue));
            Assert.That(first.RecordCount, Is.EqualTo(int.MaxValue));
            Assert.That(first.ByteCount, Is.EqualTo(long.MaxValue));
            Assert.That(first.Tsig, Is.Not.Null);
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(second.Messages, Is.Empty);
            Assert.That(second.JournalEntries, Is.Empty);
            Assert.That(second.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void NotifyRequestsPreserveUnknownClassSerialAndOwnPolicies()
    {
        DnsNotifyRequest first = new DnsNotifyRequest();
        DnsNotifyRequest second = new DnsNotifyRequest();
        first.Class = (DnsClass)65000;
        first.ClassValue = 65000;
        first.HasSerial = true;
        first.Serial = uint.MaxValue;
        first.TsigKey = new DnsTsigKey();
        first.Authorization.AllowNotify = true;
        first.CancellationRequested = true;

        Assert.Multiple(() =>
        {
            Assert.That((int)first.Class, Is.EqualTo(65000));
            Assert.That(first.ClassValue, Is.EqualTo(65000));
            Assert.That(first.HasSerial, Is.True);
            Assert.That(first.Serial, Is.EqualTo(uint.MaxValue));
            Assert.That(first.TsigKey, Is.Not.Null);
            Assert.That(first.Authorization.AllowNotify, Is.True);
            Assert.That(first.CancellationRequested, Is.True);
            Assert.That(second.Zone, Is.Not.SameAs(first.Zone));
            Assert.That(second.Endpoint, Is.Not.SameAs(first.Endpoint));
            Assert.That(second.TransportOptions, Is.Not.SameAs(first.TransportOptions));
            Assert.That(second.Authorization, Is.Not.SameAs(first.Authorization));
        });
    }

    [Test]
    public void NotifyResultsRetainAcknowledgementTransferAndAttempts()
    {
        DnsNotifyResult first = new DnsNotifyResult();
        DnsNotifyResult second = new DnsNotifyResult();
        DnsTransferResult transfer = new DnsTransferResult();
        first.Success = true;
        first.Acknowledged = true;
        first.TransferRequired = true;
        first.TransferTriggered = true;
        first.ResponseCode = DnsResponseCode.NoError;
        first.ResponseCodeValue = 0;
        first.Response = new DnsMessage();
        first.Attempts.Add(new DnsTransportResult());
        first.Tsig = new DnsTsigResult();
        first.Transfer = transfer;
        first.ElapsedMilliseconds = long.MaxValue;
        first.Diagnostics.Add(new DnsDiagnostic());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Acknowledged, Is.True);
            Assert.That(first.TransferRequired, Is.True);
            Assert.That(first.TransferTriggered, Is.True);
            Assert.That(first.ResponseCode, Is.EqualTo(DnsResponseCode.NoError));
            Assert.That(first.Attempts, Has.Count.EqualTo(1));
            Assert.That(first.Tsig, Is.Not.Null);
            Assert.That(first.Transfer, Is.SameAs(transfer));
            Assert.That(first.ElapsedMilliseconds, Is.EqualTo(long.MaxValue));
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(second.Attempts, Is.Empty);
            Assert.That(second.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void UpdatePrerequisitesPreserveFormsUnknownCodesAndValues()
    {
        DnsUpdatePrerequisite first = new DnsUpdatePrerequisite();
        DnsUpdatePrerequisite second = new DnsUpdatePrerequisite();
        first.Kind = DnsUpdatePrerequisiteKind.RecordSetExistsWithValues;
        first.RecordType = (DnsRecordType)65280;
        first.RecordTypeValue = 65280;
        first.Class = (DnsClass)65000;
        first.ClassValue = 65000;
        first.Records.Add(new DnsResourceRecord());

        Assert.Multiple(() =>
        {
            Assert.That(first.Kind,
                Is.EqualTo(DnsUpdatePrerequisiteKind.RecordSetExistsWithValues));
            Assert.That((int)first.RecordType, Is.EqualTo(65280));
            Assert.That(first.RecordTypeValue, Is.EqualTo(65280));
            Assert.That((int)first.Class, Is.EqualTo(65000));
            Assert.That(first.ClassValue, Is.EqualTo(65000));
            Assert.That(first.Records, Has.Count.EqualTo(1));
            Assert.That(second.Name, Is.Not.SameAs(first.Name));
            Assert.That(second.Records, Is.Empty);
        });
    }

    [Test]
    public void UpdateChangesPreserveMutationCodesTtlAndRecordValues()
    {
        DnsUpdateChange first = new DnsUpdateChange();
        DnsUpdateChange second = new DnsUpdateChange();
        first.Kind = DnsUpdateChangeKind.DeleteRecord;
        first.RecordType = (DnsRecordType)65281;
        first.RecordTypeValue = 65281;
        first.Class = DnsClass.None;
        first.ClassValue = 254;
        first.TimeToLive = uint.MaxValue;
        first.Records.Add(new DnsResourceRecord());

        Assert.Multiple(() =>
        {
            Assert.That(first.Kind, Is.EqualTo(DnsUpdateChangeKind.DeleteRecord));
            Assert.That((int)first.RecordType, Is.EqualTo(65281));
            Assert.That(first.RecordTypeValue, Is.EqualTo(65281));
            Assert.That(first.Class, Is.EqualTo(DnsClass.None));
            Assert.That(first.ClassValue, Is.EqualTo(254));
            Assert.That(first.TimeToLive, Is.EqualTo(uint.MaxValue));
            Assert.That(first.Records, Has.Count.EqualTo(1));
            Assert.That(second.Name, Is.Not.SameAs(first.Name));
            Assert.That(second.Records, Is.Empty);
        });
    }

    [Test]
    public void UpdateRequestsOwnAtomicInputsAndAuthorizationState()
    {
        DnsUpdateRequest first = new DnsUpdateRequest();
        DnsUpdateRequest second = new DnsUpdateRequest();
        first.Prerequisites.Add(new DnsUpdatePrerequisite());
        first.Changes.Add(new DnsUpdateChange());
        first.AdditionalRecords.Add(new DnsResourceRecord());
        first.Message = new DnsMessage();
        first.TsigKey = new DnsTsigKey();
        first.Authorization.AllowUpdates = true;
        first.HasExpectedSerial = true;
        first.ExpectedSerial = uint.MaxValue;
        first.CancellationRequested = true;

        Assert.Multiple(() =>
        {
            Assert.That(first.Class, Is.EqualTo(DnsClass.Internet));
            Assert.That(first.ClassValue, Is.EqualTo(1));
            Assert.That(first.Prerequisites, Has.Count.EqualTo(1));
            Assert.That(first.Changes, Has.Count.EqualTo(1));
            Assert.That(first.AdditionalRecords, Has.Count.EqualTo(1));
            Assert.That(first.Message, Is.Not.Null);
            Assert.That(first.TsigKey, Is.Not.Null);
            Assert.That(first.Authorization.AllowUpdates, Is.True);
            Assert.That(first.HasExpectedSerial, Is.True);
            Assert.That(first.ExpectedSerial, Is.EqualTo(uint.MaxValue));
            Assert.That(first.CancellationRequested, Is.True);
            Assert.That(second.ClientEndpoint,
                Is.Not.SameAs(first.ClientEndpoint));
            Assert.That(second.Prerequisites, Is.Empty);
            Assert.That(second.Changes, Is.Empty);
            Assert.That(second.AdditionalRecords, Is.Empty);
            Assert.That(second.Authorization, Is.Not.SameAs(first.Authorization));
        });
    }

    [Test]
    public void UpdateResultsRetainAtomicOutcomeRawResponseAndSerial()
    {
        DnsUpdateResult first = new DnsUpdateResult();
        DnsUpdateResult second = new DnsUpdateResult();
        DnsZoneTransaction transaction = new DnsZoneTransaction();
        first.Success = true;
        first.Applied = true;
        first.ResponseCode = (DnsResponseCode)4094;
        first.ResponseCodeValue = 4094;
        first.Response = new DnsMessage();
        first.Transaction = transaction;
        first.Tsig = new DnsTsigResult();
        first.HasResultingSerial = true;
        first.ResultingSerial = uint.MaxValue;
        first.Diagnostics.Add(new DnsDiagnostic());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Applied, Is.True);
            Assert.That((int)first.ResponseCode, Is.EqualTo(4094));
            Assert.That(first.ResponseCodeValue, Is.EqualTo(4094));
            Assert.That(first.Response, Is.Not.Null);
            Assert.That(first.Transaction, Is.SameAs(transaction));
            Assert.That(first.Tsig, Is.Not.Null);
            Assert.That(first.HasResultingSerial, Is.True);
            Assert.That(first.ResultingSerial, Is.EqualTo(uint.MaxValue));
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(second.Diagnostics, Is.Empty);
        });
    }
}
