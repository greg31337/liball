using System;
using fix;

namespace fix.Tests;

[TestFixture]
public class DataFieldScannerTests
{
    [Test]
    public void ScannerUsesDeclaredLengthAcrossEmbeddedSohAndArbitraryOctets()
    {
        FixFieldDefinition length = CreateLengthDefinition(95, 96);
        FixFieldDefinition data = CreateDataDefinition(96, 95, FixFieldType.Data);
        byte[] source =
        [
            57, 53, 61, 53, 1,
            57, 54, 61, 65, 1, 66, 0, 255, 1,
            51, 53, 61, 68, 1
        ];

        DataFieldScanResult result = DataFieldScanner.ScanAssociatedData(source, length, data, 100);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.LengthTag, Is.EqualTo(95));
            Assert.That(result.DataTag, Is.EqualTo(96));
            Assert.That(result.DeclaredLength, Is.EqualTo(5));
            Assert.That(result.ValueStart, Is.EqualTo(8));
            Assert.That(result.Value, Is.EqualTo(new byte[] { 65, 1, 66, 0, 255 }));
            Assert.That(result.Consumed, Is.EqualTo(14));
            Assert.That(result.Required, Is.EqualTo(14));
        });
    }

    [Test]
    public void XmlDataUsesTheSameByteExactLengthAssociationRules()
    {
        FixFieldDefinition length = CreateLengthDefinition(212, 213);
        FixFieldDefinition data = CreateDataDefinition(213, 212, FixFieldType.XmlData);
        byte[] source = [50, 49, 50, 61, 52, 1, 50, 49, 51, 61, 60, 120, 1, 62, 1];

        DataFieldScanResult result = DataFieldScanner.ScanAssociatedData(source, length, data, 4);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Value, Is.EqualTo(new byte[] { 60, 120, 1, 62 }));
            Assert.That(result.Consumed, Is.EqualTo(source.Length));
        });
    }

    [Test]
    public void WriterProducesCanonicalAdjacentFieldsThatRoundTrip()
    {
        FixFieldDefinition length = CreateLengthDefinition(95, 96);
        FixFieldDefinition data = CreateDataDefinition(96, 95, FixFieldType.Data);
        byte[] value = [65, 1, 66, 0, 255];
        byte[] destination = new byte[14];

        ScalarFormatResult written = DataFieldScanner.WriteAssociatedData(length, data, value, destination, 100);
        DataFieldScanResult scanned = DataFieldScanner.ScanAssociatedData(destination, length, data, 100);

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(written.Required, Is.EqualTo(14));
            Assert.That(written.Written, Is.EqualTo(14));
            Assert.That(destination.AsSpan(0, 8).ToArray(), Is.EqualTo(new byte[] { 57, 53, 61, 53, 1, 57, 54, 61 }));
            Assert.That(scanned.Success, Is.True);
            Assert.That(scanned.Value, Is.EqualTo(value));
        });
    }

    [Test]
    public void WriterReportsDestinationAndDataLimitsBeforeChangingOutput()
    {
        FixFieldDefinition length = CreateLengthDefinition(95, 96);
        FixFieldDefinition data = CreateDataDefinition(96, 95, FixFieldType.Data);
        byte[] shortDestination = [88, 88, 88];

        ScalarFormatResult shortResult = DataFieldScanner.WriteAssociatedData(length, data, new byte[] { 65, 66 }, shortDestination, 10);
        ScalarFormatResult limited = DataFieldScanner.WriteAssociatedData(length, data, new byte[] { 65, 66, 67 }, new byte[32], 2);
        ScalarFormatResult empty = DataFieldScanner.WriteAssociatedData(length, data, Array.Empty<byte>(), new byte[32], 10);
        ScalarFormatResult invalidLimit = DataFieldScanner.WriteAssociatedData(length, data, new byte[] { 65 }, new byte[32], -1);

        Assert.Multiple(() =>
        {
            Assert.That(shortResult.ErrorCode, Is.EqualTo("destination-too-small"));
            Assert.That(shortResult.Required, Is.EqualTo(11));
            Assert.That(shortDestination, Is.EqualTo(new byte[] { 88, 88, 88 }));
            Assert.That(limited.ErrorCode, Is.EqualTo("limit-exceeded"));
            Assert.That(empty.ErrorCode, Is.EqualTo("empty-data"));
            Assert.That(invalidLimit.ErrorCode, Is.EqualTo("invalid-limit"));
        });
    }

    [Test]
    public void ScannerEnforcesImmediateAdjacency()
    {
        FixFieldDefinition length = CreateLengthDefinition(95, 96);
        FixFieldDefinition data = CreateDataDefinition(96, 95, FixFieldType.Data);
        byte[] interveningField = [57, 53, 61, 51, 1, 51, 53, 61, 65, 66, 67, 1, 57, 54, 61, 65, 66, 67, 1];

        DataFieldScanResult result = DataFieldScanner.ScanAssociatedData(interveningField, length, data, 100);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("data-field-not-adjacent"));
            Assert.That(result.ErrorOffset, Is.EqualTo(5));
        });
    }

    [Test]
    public void ScannerReportsTooShortAndTooLongPayloadDeclarationsPrecisely()
    {
        FixFieldDefinition length = CreateLengthDefinition(95, 96);
        FixFieldDefinition data = CreateDataDefinition(96, 95, FixFieldType.Data);
        byte[] tooLongDeclaration = [57, 53, 61, 53, 1, 57, 54, 61, 65, 66, 67, 68, 1];
        byte[] tooShortDeclaration = [57, 53, 61, 51, 1, 57, 54, 61, 65, 66, 67, 68, 1];

        DataFieldScanResult incomplete = DataFieldScanner.ScanAssociatedData(tooLongDeclaration, length, data, 100);
        DataFieldScanResult mismatch = DataFieldScanner.ScanAssociatedData(tooShortDeclaration, length, data, 100);

        Assert.Multiple(() =>
        {
            Assert.That(incomplete.ErrorCode, Is.EqualTo("incomplete"));
            Assert.That(incomplete.Required, Is.EqualTo(14));
            Assert.That(incomplete.ErrorOffset, Is.EqualTo(13));
            Assert.That(mismatch.ErrorCode, Is.EqualTo("data-length-mismatch"));
            Assert.That(mismatch.ErrorOffset, Is.EqualTo(11));
        });
    }

    [Test]
    public void ScannerRejectsMalformedLengthAndFieldHeaders()
    {
        FixFieldDefinition length = CreateLengthDefinition(95, 96);
        FixFieldDefinition data = CreateDataDefinition(96, 95, FixFieldType.Data);

        DataFieldScanResult zero = DataFieldScanner.ScanAssociatedData(new byte[] { 57, 53, 61, 48, 1, 57, 54, 61, 1 }, length, data, 100);
        DataFieldScanResult nonNumeric = DataFieldScanner.ScanAssociatedData(new byte[] { 57, 53, 61, 88, 1, 57, 54, 61, 65, 1 }, length, data, 100);
        DataFieldScanResult leadingTagZero = DataFieldScanner.ScanAssociatedData(new byte[] { 48, 57, 53, 61, 49, 1, 57, 54, 61, 65, 1 }, length, data, 100);
        DataFieldScanResult noEquals = DataFieldScanner.ScanAssociatedData(new byte[] { 57, 53, 49, 1 }, length, data, 100);
        DataFieldScanResult limited = DataFieldScanner.ScanAssociatedData(new byte[] { 57, 53, 61, 51, 1, 57, 54, 61, 65, 66, 67, 1 }, length, data, 2);
        DataFieldScanResult empty = DataFieldScanner.ScanAssociatedData(Array.Empty<byte>(), length, data, 100);

        Assert.Multiple(() =>
        {
            Assert.That(zero.ErrorCode, Is.EqualTo("zero-not-allowed"));
            Assert.That(nonNumeric.ErrorCode, Is.EqualTo("invalid-digit"));
            Assert.That(leadingTagZero.ErrorCode, Is.EqualTo("leading-zero"));
            Assert.That(noEquals.ErrorCode, Is.EqualTo("missing-equals"));
            Assert.That(limited.ErrorCode, Is.EqualTo("limit-exceeded"));
            Assert.That(empty.ErrorCode, Is.EqualTo("incomplete"));
            Assert.That(empty.Required, Is.EqualTo(1));
        });
    }

    [Test]
    public void ScannerRequiresBidirectionalDictionaryAssociationsAndCorrectTypes()
    {
        FixFieldDefinition validLength = CreateLengthDefinition(95, 96);
        FixFieldDefinition validData = CreateDataDefinition(96, 95, FixFieldType.Data);
        FixFieldDefinition noAssociationLength = CreateLengthDefinition(95, 96);
        noAssociationLength.HasDataTag = false;
        FixFieldDefinition mismatchData = CreateDataDefinition(96, 94, FixFieldType.Data);
        FixFieldDefinition wrongLengthType = CreateLengthDefinition(95, 96);
        wrongLengthType.FieldType = FixFieldType.Int;
        FixFieldDefinition wrongDataType = CreateDataDefinition(96, 95, FixFieldType.String);
        FixFieldDefinition duplicateTagLength = CreateLengthDefinition(95, 95);
        FixFieldDefinition duplicateTagData = CreateDataDefinition(95, 95, FixFieldType.Data);
        byte[] source = [57, 53, 61, 49, 1, 57, 54, 61, 65, 1];

        DataFieldScanResult missing = DataFieldScanner.ScanAssociatedData(source, noAssociationLength, validData, 100);
        DataFieldScanResult mismatch = DataFieldScanner.ScanAssociatedData(source, validLength, mismatchData, 100);
        DataFieldScanResult lengthType = DataFieldScanner.ScanAssociatedData(source, wrongLengthType, validData, 100);
        DataFieldScanResult dataType = DataFieldScanner.ScanAssociatedData(source, validLength, wrongDataType, 100);
        DataFieldScanResult duplicateTag = DataFieldScanner.ScanAssociatedData(source, duplicateTagLength, duplicateTagData, 100);

        Assert.Multiple(() =>
        {
            Assert.That(missing.ErrorCode, Is.EqualTo("missing-length-association"));
            Assert.That(mismatch.ErrorCode, Is.EqualTo("mismatched-length-association"));
            Assert.That(lengthType.ErrorCode, Is.EqualTo("invalid-length-field-type"));
            Assert.That(dataType.ErrorCode, Is.EqualTo("invalid-data-field-type"));
            Assert.That(duplicateTag.ErrorCode, Is.EqualTo("invalid-associated-tag"));
        });
    }

    private static FixFieldDefinition CreateLengthDefinition(int lengthTag, int dataTag)
    {
        return new FixFieldDefinition
        {
            Tag = lengthTag,
            FieldType = FixFieldType.Length,
            HasDataTag = true,
            DataTag = dataTag
        };
    }

    private static FixFieldDefinition CreateDataDefinition(int dataTag, int lengthTag, FixFieldType fieldType)
    {
        return new FixFieldDefinition
        {
            Tag = dataTag,
            FieldType = fieldType,
            HasLengthTag = true,
            LengthTag = lengthTag
        };
    }
}
