using fake;

namespace fake.Tests;

[TestFixture]
public class RoundTripValidatorCoverageTests
{
    [Test]
    public void GeneratedBinaryIdentifiersFormatParseAndValidateRoundTrip()
    {
        DateTimeOffset timestamp = DateTimeOffset.FromUnixTimeMilliseconds(
            1900000000123L);
        IdentifierProvider uuid4Text = Fake.Create(14510).Identifier();
        IdentifierProvider uuid4Bytes = Fake.Create(14510).Identifier();
        string uuid4 = uuid4Text.Uuid4();
        byte[] uuid4Binary = uuid4Bytes.Uuid4Bytes();
        IdentifierProvider uuid7Text = Fake.Create(14511).Identifier();
        IdentifierProvider uuid7Bytes = Fake.Create(14511).Identifier();
        string uuid7 = uuid7Text.Uuid7FromTimestamp(timestamp);
        byte[] uuid7Binary = uuid7Bytes.Uuid7BytesFromTimestamp(timestamp);
        IdentifierProvider ulidText = Fake.Create(14512).Identifier();
        IdentifierProvider ulidBytes = Fake.Create(14512).Identifier();
        string ulid = ulidText.UlidFromTimestamp(timestamp);
        byte[] ulidBinary = ulidBytes.UlidBytesFromTimestamp(timestamp);
        IdentifierProvider objectText = Fake.Create(14513).Identifier();
        IdentifierProvider objectBytes = Fake.Create(14513).Identifier();
        string objectId = objectText.ObjectIdFromTimestamp(timestamp);
        byte[] objectBinary = objectBytes.ObjectIdBytesFromTimestamp(timestamp);
        IdentifierProvider sortableText = Fake.Create(14514).Identifier();
        IdentifierProvider sortableBytes = Fake.Create(14514).Identifier();
        string sortable = sortableText.SortableTimestampIdFromTimestamp(timestamp);
        byte[] sortableBinary = sortableBytes.SortableTimestampIdBytesFromTimestamp(
            timestamp);
        IdentifierProvider validator = Fake.Create(14515).Identifier();

        Assert.Multiple(() =>
        {
            Assert.That(validator.FormatUuid(uuid4Binary),
                Is.EqualTo(uuid4));
            Assert.That(validator.ParseUuid(uuid4),
                Is.EqualTo(uuid4Binary));
            Assert.That(validator.ValidateUuid4(uuid4), Is.True);
            Assert.That(validator.ValidateUuid4FromValueUsingReadOnlySpanOfByte(uuid4Binary), Is.True);

            Assert.That(validator.FormatUuid(uuid7Binary),
                Is.EqualTo(uuid7));
            Assert.That(validator.ParseUuid(uuid7),
                Is.EqualTo(uuid7Binary));
            Assert.That(validator.ValidateUuid7(uuid7), Is.True);
            Assert.That(validator.ValidateUuid7FromValueUsingReadOnlySpanOfByte(uuid7Binary), Is.True);
            Assert.That(validator.Uuid7UnixMilliseconds(uuid7Binary),
                Is.EqualTo(1900000000123L));

            Assert.That(validator.FormatUlid(ulidBinary), Is.EqualTo(ulid));
            Assert.That(validator.ParseUlid(ulid), Is.EqualTo(ulidBinary));
            Assert.That(validator.ValidateUlid(ulid), Is.True);
            Assert.That(validator.ValidateUlidFromValueUsingReadOnlySpanOfByte(ulidBinary), Is.True);
            Assert.That(validator.UlidUnixMillisecondsFromValueUsingReadOnlySpanOfByte(ulidBinary),
                Is.EqualTo(1900000000123L));

            Assert.That(validator.FormatObjectId(objectBinary),
                Is.EqualTo(objectId));
            Assert.That(validator.ParseObjectId(objectId),
                Is.EqualTo(objectBinary));
            Assert.That(validator.ValidateObjectId(objectId), Is.True);
            Assert.That(validator.ValidateObjectIdFromValueUsingReadOnlySpanOfByte(objectBinary), Is.True);
            Assert.That(validator.ObjectIdUnixSecondsFromValueUsingReadOnlySpanOfByte(objectBinary),
                Is.EqualTo(1900000000L));

            Assert.That(validator.FormatSortableTimestampId(sortableBinary),
                Is.EqualTo(sortable));
            Assert.That(validator.ParseSortableTimestampId(sortable),
                Is.EqualTo(sortableBinary));
            Assert.That(validator.ValidateSortableTimestampId(sortable),
                Is.True);
            Assert.That(validator.ValidateSortableTimestampIdFromValueUsingReadOnlySpanOfByte(
                sortableBinary), Is.True);
            Assert.That(validator.SortableTimestampIdUnixMillisecondsFromValueUsingReadOnlySpanOfByte(
                sortableBinary), Is.EqualTo(1900000000123L));
        });
    }

    [Test]
    public void GeneratedDomainIdentifiersAreAcceptedByTheirValidators()
    {
        Fake fake = Fake.Create("fr-FR", 14516);
        IbanRecord iban = fake.Finance().IbanRecordFromCountryCode("FR");
        CompanyIdentifierRecord company = fake.Company().Identifiers();
        ProductProfile product = fake.Profile().Product();
        string card = fake.Finance().CardNumber();

        Assert.Multiple(() =>
        {
            Assert.That(fake.Finance().IsIbanValid(iban.Electronic),
                Is.True);
            Assert.That(fake.Finance().IsIbanValid(iban.Print), Is.True);
            Assert.That(fake.Finance().FormatIban(iban.Electronic),
                Is.EqualTo(iban.Print));
            Assert.That(fake.Company().IsBusinessIdValid(
                company.BusinessIdentifier), Is.True);
            Assert.That(fake.Company().IsTaxIdValid(
                company.TaxIdentifier), Is.True);
            Assert.That(fake.Commerce().ValidateGtin(product.Gtin),
                Is.True);
            Assert.That(fake.Finance().IsCardNumberValid(card), Is.True);
        });
    }
}
