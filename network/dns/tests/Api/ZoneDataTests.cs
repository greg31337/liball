namespace dns.Tests.Api;

public class ZoneDataTests
{
    [Test]
    public void TextOptionsStartReadableAndBounded()
    {
        DnsTextOptions options = new DnsTextOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Canonical, Is.False);
            Assert.That(options.UseMnemonicCodes, Is.True);
            Assert.That(options.IncludeHeader, Is.True);
            Assert.That(options.IncludeSectionNames, Is.True);
            Assert.That(options.MultilineRecords, Is.False);
            Assert.That(options.PreserveInputFormatting, Is.False);
            Assert.That(options.Origin, Is.Null);
            Assert.That(options.IndentSize, Is.EqualTo(2));
            Assert.That(options.MaximumLineLength, Is.EqualTo(120));
        });
    }

    [Test]
    public void TextResultsOwnIndependentDiagnosticCollections()
    {
        DnsTextResult first = new DnsTextResult();
        DnsTextResult second = new DnsTextResult();
        first.Success = true;
        first.Text = "example. 300 IN A 192.0.2.1";
        first.Diagnostics.Add(new DnsDiagnostic());
        first.ZoneDiagnostics.Add(new DnsZoneDiagnostic());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Text, Does.Contain("192.0.2.1"));
            Assert.That(first.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(first.ZoneDiagnostics, Has.Count.EqualTo(1));
            Assert.That(second.Text, Is.Empty);
            Assert.That(second.Diagnostics, Is.Empty);
            Assert.That(second.ZoneDiagnostics, Is.Empty);
        });
    }

    [Test]
    public void ZonesOwnIndependentNodesAndJournals()
    {
        DnsZone first = new DnsZone();
        DnsZone second = new DnsZone();
        first.Apex.IsAbsolute = true;
        first.Class = (DnsClass)65000;
        first.ClassValue = 65000;
        first.HasDefaultTimeToLive = true;
        first.DefaultTimeToLive = uint.MaxValue;
        first.Nodes.Add(new DnsZoneNode());
        first.JournalEntries.Add(new DnsZoneJournalEntry());

        Assert.Multiple(() =>
        {
            Assert.That(first.Apex.IsAbsolute, Is.True);
            Assert.That((int)first.Class, Is.EqualTo(65000));
            Assert.That(first.ClassValue, Is.EqualTo(65000));
            Assert.That(first.DefaultTimeToLive, Is.EqualTo(uint.MaxValue));
            Assert.That(first.Nodes, Has.Count.EqualTo(1));
            Assert.That(first.JournalEntries, Has.Count.EqualTo(1));
            Assert.That(second.Nodes, Is.Empty);
            Assert.That(second.JournalEntries, Is.Empty);
            Assert.That(second.Apex, Is.Not.SameAs(first.Apex));
        });
    }

    [Test]
    public void ZoneNodesOwnRecordSetsAndChildren()
    {
        DnsZoneNode first = new DnsZoneNode();
        DnsZoneNode second = new DnsZoneNode();
        first.IsDelegation = true;
        first.RecordSets.Add(new DnsResourceRecordSet());
        first.Children.Add(new DnsZoneNode());

        Assert.Multiple(() =>
        {
            Assert.That(first.IsDelegation, Is.True);
            Assert.That(first.RecordSets, Has.Count.EqualTo(1));
            Assert.That(first.Children, Has.Count.EqualTo(1));
            Assert.That(second.RecordSets, Is.Empty);
            Assert.That(second.Children, Is.Empty);
            Assert.That(second.Name, Is.Not.SameAs(first.Name));
        });
    }

    [Test]
    public void ZoneLoadOptionsStartWithConservativeDirectiveLimits()
    {
        DnsZoneLoadOptions options = new DnsZoneLoadOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Class, Is.EqualTo(DnsClass.Internet));
            Assert.That(options.ClassValue, Is.EqualTo(1));
            Assert.That(options.AllowInclude, Is.True);
            Assert.That(options.AllowGenerate, Is.True);
            Assert.That(options.ValidateZone, Is.True);
            Assert.That(options.PreserveSourceFormatting, Is.False);
            Assert.That(options.MaximumIncludeDepth, Is.EqualTo(16));
            Assert.That(options.MaximumGeneratedRecords, Is.EqualTo(100000));
            Assert.That(options.MaximumRecords, Is.EqualTo(1000000));
        });
    }

    [Test]
    public void LoadAndValidationResultsOwnDiagnostics()
    {
        DnsZoneLoadResult firstLoad = new DnsZoneLoadResult();
        DnsZoneLoadResult secondLoad = new DnsZoneLoadResult();
        DnsZoneValidationResult firstValidation = new DnsZoneValidationResult();
        DnsZoneValidationResult secondValidation = new DnsZoneValidationResult();
        firstLoad.IncludedFiles.Add("child.zone");
        firstLoad.Diagnostics.Add(new DnsZoneDiagnostic());
        firstValidation.Diagnostics.Add(new DnsZoneDiagnostic());

        Assert.Multiple(() =>
        {
            Assert.That(firstLoad.IncludedFiles, Is.EqualTo(new[] { "child.zone" }));
            Assert.That(firstLoad.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(secondLoad.IncludedFiles, Is.Empty);
            Assert.That(secondLoad.Diagnostics, Is.Empty);
            Assert.That(firstValidation.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(secondValidation.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void ZoneDiagnosticRetainsOptionalSourceLocation()
    {
        DnsZoneDiagnostic diagnostic = new DnsZoneDiagnostic();
        diagnostic.Code = DnsDiagnosticCode.InvalidZone;
        diagnostic.IsError = false;
        diagnostic.Reason = "Glue record is not required.";
        diagnostic.SourcePath = "example.zone";
        diagnostic.HasLine = true;
        diagnostic.Line = 42;
        diagnostic.HasColumn = true;
        diagnostic.Column = 7;
        diagnostic.Name = new DnsName();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Code, Is.EqualTo(DnsDiagnosticCode.InvalidZone));
            Assert.That(diagnostic.IsError, Is.False);
            Assert.That(diagnostic.SourcePath, Is.EqualTo("example.zone"));
            Assert.That(diagnostic.Line, Is.EqualTo(42));
            Assert.That(diagnostic.Column, Is.EqualTo(7));
            Assert.That(diagnostic.Name, Is.Not.Null);
        });
    }

    [Test]
    public void ZoneChangesRetainUnknownRecordCodes()
    {
        DnsZoneChange change = new DnsZoneChange();
        change.Kind = DnsZoneChangeKind.ReplaceRecordSet;
        change.RecordType = (DnsRecordType)65000;
        change.RecordTypeValue = 65000;
        change.Class = (DnsClass)65001;
        change.ClassValue = 65001;
        change.RecordSet = new DnsResourceRecordSet();

        Assert.Multiple(() =>
        {
            Assert.That(change.Kind, Is.EqualTo(DnsZoneChangeKind.ReplaceRecordSet));
            Assert.That((int)change.RecordType, Is.EqualTo(65000));
            Assert.That(change.RecordTypeValue, Is.EqualTo(65000));
            Assert.That((int)change.Class, Is.EqualTo(65001));
            Assert.That(change.ClassValue, Is.EqualTo(65001));
            Assert.That(change.RecordSet, Is.Not.Null);
        });
    }

    [Test]
    public void TransactionsRepresentAtomicSerialWraparound()
    {
        DnsZoneTransaction transaction = new DnsZoneTransaction();
        transaction.Identifier = long.MaxValue;
        transaction.HasExpectedSerial = true;
        transaction.ExpectedSerial = uint.MaxValue;
        transaction.HasResultingSerial = true;
        transaction.ResultingSerial = 0;
        transaction.Changes.Add(new DnsZoneChange());
        transaction.Committed = true;

        Assert.Multiple(() =>
        {
            Assert.That(transaction.Identifier, Is.EqualTo(long.MaxValue));
            Assert.That(transaction.ExpectedSerial, Is.EqualTo(uint.MaxValue));
            Assert.That(transaction.ResultingSerial, Is.Zero);
            Assert.That(transaction.Atomic, Is.True);
            Assert.That(transaction.Committed, Is.True);
            Assert.That(transaction.Changes, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void JournalEntriesOwnIndependentTransactions()
    {
        DnsZoneJournalEntry first = new DnsZoneJournalEntry();
        DnsZoneJournalEntry second = new DnsZoneJournalEntry();
        first.Sequence = 9;
        first.FromSerial = uint.MaxValue;
        first.ToSerial = 0;
        first.RecordedAtUnixMilliseconds = long.MaxValue;
        first.Transaction.Changes.Add(new DnsZoneChange());

        Assert.Multiple(() =>
        {
            Assert.That(first.Sequence, Is.EqualTo(9));
            Assert.That(first.FromSerial, Is.EqualTo(uint.MaxValue));
            Assert.That(first.ToSerial, Is.Zero);
            Assert.That(first.RecordedAtUnixMilliseconds, Is.EqualTo(long.MaxValue));
            Assert.That(first.Transaction.Changes, Has.Count.EqualTo(1));
            Assert.That(second.Transaction.Changes, Is.Empty);
            Assert.That(second.Transaction, Is.Not.SameAs(first.Transaction));
        });
    }
}
