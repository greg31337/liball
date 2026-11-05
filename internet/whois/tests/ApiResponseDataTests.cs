using whois;

namespace whois.Tests;

public class ApiResponseDataTests
{
    [Test]
    public void RawResponseHasSafeEmptyDefaults()
    {
        WhoisRawResponse response = new WhoisRawResponse();

        Assert.That(response.Request, Is.Not.Null);
        Assert.That(response.ApprovedAddress, Is.Null);
        Assert.That(response.Body, Is.Empty);
        Assert.That(response.StartedAt, Is.EqualTo(default(DateTimeOffset)));
        Assert.That(response.Duration, Is.EqualTo(TimeSpan.Zero));
        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.None));
        Assert.That(response.IsTruncated, Is.False);
        Assert.That(response.Error, Is.Null);
    }

    [Test]
    public void NetworkBoundaryResultsDefaultToFailureWithoutResources()
    {
        WhoisResolution resolution = new WhoisResolution();
        WhoisConnection connection = new WhoisConnection();

        Assert.That(resolution.Addresses, Is.Empty);
        Assert.That(resolution.Status, Is.EqualTo(WhoisExchangeStatus.ResolveFailed));
        Assert.That(resolution.Duration, Is.EqualTo(TimeSpan.Zero));
        Assert.That(resolution.Error, Is.Null);

        Assert.That(connection.ApprovedAddress, Is.Null);
        Assert.That(connection.Stream, Is.Null);
        Assert.That(connection.Status, Is.EqualTo(WhoisExchangeStatus.ConnectFailed));
        Assert.That(connection.Duration, Is.EqualTo(TimeSpan.Zero));
        Assert.That(connection.Error, Is.Null);
    }

    [Test]
    public void NetworkBoundaryResultsRetainOrderedAddressesAndOwnedStream()
    {
        System.Net.IPAddress first = System.Net.IPAddress.Parse("192.0.2.1");
        System.Net.IPAddress second = System.Net.IPAddress.Parse("2001:db8::1");
        using MemoryStream stream = new MemoryStream();
        WhoisResolution resolution = new WhoisResolution();
        WhoisConnection connection = new WhoisConnection();

        resolution.Addresses = new[] { first, second };
        resolution.Status = WhoisExchangeStatus.Complete;
        resolution.Duration = TimeSpan.FromMilliseconds(12);
        connection.ApprovedAddress = second;
        connection.Stream = stream;
        connection.Status = WhoisExchangeStatus.Complete;
        connection.Duration = TimeSpan.FromMilliseconds(8);

        Assert.That(resolution.Addresses, Is.EqualTo(new[] { first, second }));
        Assert.That(resolution.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(resolution.Duration, Is.EqualTo(TimeSpan.FromMilliseconds(12)));
        Assert.That(connection.ApprovedAddress, Is.SameAs(second));
        Assert.That(connection.Stream, Is.SameAs(stream));
        Assert.That(connection.Status, Is.EqualTo(WhoisExchangeStatus.Complete));
        Assert.That(connection.Duration, Is.EqualTo(TimeSpan.FromMilliseconds(8)));
    }

    [Test]
    public void RawResponseRetainsPartialBytesAndFailureContext()
    {
        WhoisRawResponse response = new WhoisRawResponse();
        System.Net.IPAddress address = System.Net.IPAddress.Parse("192.0.2.43");
        IOException error = new IOException("read failed");
        DateTimeOffset startedAt = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

        response.ApprovedAddress = address;
        response.Body = new byte[] { 0, 13, 10, 255 };
        response.StartedAt = startedAt;
        response.Duration = TimeSpan.FromSeconds(2);
        response.Status = WhoisExchangeStatus.ReadFailed;
        response.Stage = WhoisOperationStage.Read;
        response.IsTruncated = false;
        response.Error = error;

        Assert.That(response.Request, Is.Not.Null);
        Assert.That(response.ApprovedAddress, Is.SameAs(address));
        Assert.That(response.Body, Is.EqualTo(new byte[] { 0, 13, 10, 255 }));
        Assert.That(response.StartedAt, Is.EqualTo(startedAt));
        Assert.That(response.Duration, Is.EqualTo(TimeSpan.FromSeconds(2)));
        Assert.That(response.Status, Is.EqualTo(WhoisExchangeStatus.ReadFailed));
        Assert.That(response.Stage, Is.EqualTo(WhoisOperationStage.Read));
        Assert.That(response.IsTruncated, Is.False);
        Assert.That(response.Error, Is.SameAs(error));
    }

    [Test]
    public void TextResponseDefaultsPreserveRawResponseLink()
    {
        WhoisTextResponse response = new WhoisTextResponse();

        Assert.That(response.RawResponse, Is.Not.Null);
        Assert.That(response.Text, Is.Empty);
        Assert.That(response.EncodingName, Is.Empty);
        Assert.That(response.ByteOrderMarkDetected, Is.False);
        Assert.That(response.FallbackUsed, Is.False);
        Assert.That(response.ReplacementUsed, Is.False);
        Assert.That(response.IsDecoded, Is.False);
        Assert.That(response.Error, Is.Null);
    }

    [Test]
    public void FieldRecordAndParsedResponseDefaultsAreUnlinked()
    {
        WhoisField field = new WhoisField();
        WhoisRecord record = new WhoisRecord();
        WhoisParsedResponse response = new WhoisParsedResponse();

        Assert.That(field.Index, Is.EqualTo(-1));
        Assert.That(field.RecordIndex, Is.EqualTo(-1));
        Assert.That(field.SourceLine, Is.Zero);
        Assert.That(field.OriginalName, Is.Empty);
        Assert.That(field.CanonicalName, Is.Empty);
        Assert.That(field.RawValue, Is.Empty);
        Assert.That(field.Value, Is.Empty);

        Assert.That(record.Index, Is.EqualTo(-1));
        Assert.That(record.Fields, Is.Empty);
        Assert.That(record.Lines, Is.Empty);

        Assert.That(response.TextResponse, Is.Not.Null);
        Assert.That(response.Lines, Is.Empty);
        Assert.That(response.Fields, Is.Empty);
        Assert.That(response.Records, Is.Empty);
        Assert.That(response.IsParsed, Is.False);
        Assert.That(response.Error, Is.Null);
    }

    [Test]
    public void ParsedResponsePreservesOrderedSourceLinksAndDuplicates()
    {
        WhoisLine firstLine = new WhoisLine();
        WhoisLine secondLine = new WhoisLine();
        WhoisField firstField = new WhoisField();
        WhoisField secondField = new WhoisField();
        WhoisRecord record = new WhoisRecord();
        WhoisParsedResponse response = new WhoisParsedResponse();

        firstLine.LineNumber = 1;
        firstLine.RawContent = "Status: active";
        firstLine.NormalizedContent = "Status: active";
        firstLine.Kind = WhoisLineKind.Field;
        firstLine.FieldIndex = 0;
        secondLine.LineNumber = 2;
        secondLine.RawContent = "Status: locked";
        secondLine.NormalizedContent = "Status: locked";
        secondLine.Kind = WhoisLineKind.Field;
        secondLine.FieldIndex = 1;

        firstField.Index = 0;
        firstField.RecordIndex = 0;
        firstField.SourceLine = 1;
        firstField.OriginalName = "Status";
        firstField.CanonicalName = "status";
        firstField.RawValue = "active";
        firstField.Value = "active";
        secondField.Index = 1;
        secondField.RecordIndex = 0;
        secondField.SourceLine = 2;
        secondField.OriginalName = "Status";
        secondField.CanonicalName = "status";
        secondField.RawValue = "locked";
        secondField.Value = "locked";

        record.Index = 0;
        record.Fields = new[] { firstField, secondField };
        record.Lines = new[] { firstLine, secondLine };
        response.Lines = new[] { firstLine, secondLine };
        response.Fields = new[] { firstField, secondField };
        response.Records = new[] { record };
        response.IsParsed = true;

        Assert.That(response.Lines, Is.EqualTo(new[] { firstLine, secondLine }));
        Assert.That(response.Fields, Is.EqualTo(new[] { firstField, secondField }));
        Assert.That(response.Fields[0].CanonicalName, Is.EqualTo(response.Fields[1].CanonicalName));
        Assert.That(response.Records, Has.Exactly(1).SameAs(record));
        Assert.That(response.Records[0].Fields, Is.EqualTo(response.Fields));
        Assert.That(response.Records[0].Lines, Is.EqualTo(response.Lines));
        Assert.That(response.IsParsed, Is.True);
    }
}
