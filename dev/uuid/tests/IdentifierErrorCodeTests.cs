using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class IdentifierErrorCodeTests
{
    [Test]
    public void Codes_HaveStableNumericValues()
    {
        Assert.That((int)IdentifierErrorCode.None, Is.EqualTo(0));
        Assert.That((int)IdentifierErrorCode.NullInput, Is.EqualTo(1));
        Assert.That((int)IdentifierErrorCode.InvalidLength, Is.EqualTo(2));
        Assert.That((int)IdentifierErrorCode.InvalidCharacter, Is.EqualTo(3));
        Assert.That((int)IdentifierErrorCode.InvalidUuidVariant, Is.EqualTo(4));
        Assert.That((int)IdentifierErrorCode.InvalidUuidVersion, Is.EqualTo(5));
        Assert.That((int)IdentifierErrorCode.TimestampOverflow, Is.EqualTo(6));
        Assert.That((int)IdentifierErrorCode.RandomFieldOverflow, Is.EqualTo(7));
        Assert.That((int)IdentifierErrorCode.ClockRollback, Is.EqualTo(8));
        Assert.That((int)IdentifierErrorCode.SequenceExhaustion, Is.EqualTo(9));
        Assert.That((int)IdentifierErrorCode.AlphabetError, Is.EqualTo(10));
        Assert.That((int)IdentifierErrorCode.ValueOverflow, Is.EqualTo(11));
        Assert.That((int)IdentifierErrorCode.InvalidValue, Is.EqualTo(12));
        Assert.That((int)IdentifierErrorCode.UnsupportedKind, Is.EqualTo(13));
    }

    [Test]
    public void ParseResults_ExposeNoneOnSuccessAndSpecificCodesOnFailure()
    {
        UuidParseResult uuidSuccess = new Uuid().Parse("00000000-0000-0000-0000-000000000000");
        UuidParseResult uuidLength = new Uuid().Parse("bad");
        UuidParseResult uuidCharacter = new Uuid().Parse("00000000-0000-0000-0000-00000000000g");
        UlidParseResult ulidLength = new Ulid().Parse("short");
        UlidParseResult ulidCharacter = new Ulid().Parse("0000000000000000000000000I");
        KsuidParseResult ksuidLength = new Ksuid().Parse("short");
        KsuidParseResult ksuidCharacter = new Ksuid().Parse("00000000000000000000000000_");
        ObjectIdParseResult objectIdLength = new ObjectId().Parse("short");
        ObjectIdParseResult objectIdCharacter = new ObjectId().Parse("00000000000000000000000g");
        SnowflakeGenerator snowflake = new Snowflake().DefaultGenerator();
        SnowflakeParseResult snowflakeLength = snowflake.Parse("");
        SnowflakeParseResult snowflakeCharacter = snowflake.Parse("12x");
        IdentifierParseResult unknown = new Identifier().Parse("not an identifier");

        Assert.That(uuidSuccess.Code, Is.EqualTo(IdentifierErrorCode.None));
        Assert.That(uuidLength.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(uuidCharacter.Code, Is.EqualTo(IdentifierErrorCode.InvalidCharacter));
        Assert.That(ulidLength.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(ulidCharacter.Code, Is.EqualTo(IdentifierErrorCode.InvalidCharacter));
        Assert.That(ksuidLength.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(ksuidCharacter.Code, Is.EqualTo(IdentifierErrorCode.InvalidCharacter));
        Assert.That(objectIdLength.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(objectIdCharacter.Code, Is.EqualTo(IdentifierErrorCode.InvalidCharacter));
        Assert.That(snowflakeLength.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(snowflakeCharacter.Code, Is.EqualTo(IdentifierErrorCode.InvalidCharacter));
        Assert.That(unknown.Code, Is.EqualTo(IdentifierErrorCode.UnsupportedKind));
    }

    [Test]
    public void ValidationResults_ExposeUuidAlphabetAndTextCodes()
    {
        Uuid uuid = new Uuid();
        IdentifierValidationResult variant = uuid.ValidateRfc9562Variant("00000000-0000-4000-0000-000000000000");
        IdentifierValidationResult version = uuid.ValidateVersion("00000000-0000-4000-8000-000000000000", UuidVersion.Version7);
        IdentifierValidationResult alphabet = new NanoId().ValidateAlphabet("abca");
        IdentifierValidationResult nanoLength = new NanoId().Validate("short");
        IdentifierValidationResult nanoCharacter = new NanoId().Validate("!!!!!!!!!!!!!!!!!!!!!");
        IdentifierValidationResult valid = new ObjectId().Validate("000000000000000000000000");

        Assert.That(variant.Code, Is.EqualTo(IdentifierErrorCode.InvalidUuidVariant));
        Assert.That(version.Code, Is.EqualTo(IdentifierErrorCode.InvalidUuidVersion));
        Assert.That(alphabet.Code, Is.EqualTo(IdentifierErrorCode.AlphabetError));
        Assert.That(nanoLength.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(nanoCharacter.Code, Is.EqualTo(IdentifierErrorCode.InvalidCharacter));
        Assert.That(valid.Code, Is.EqualTo(IdentifierErrorCode.None));
    }

    [Test]
    public void TimestampResults_ReturnCodesWithoutThrowing()
    {
        byte[] futureUlidBytes = new byte[16];
        int index = 0;
        while (index < 6)
        {
            futureUlidBytes[index] = 255;
            index = index + 1;
        }

        IdentifierTimestampResult overflow = new UlidValue(futureUlidBytes).Timestamp();
        IdentifierTimestampResult badVariant = new Uuid().TimestampText("00000000-0000-7000-0000-000000000000");
        IdentifierTimestampResult badVersion = new Uuid().TimestampText("00000000-0000-4000-8000-000000000000");
        IdentifierTimestampResult badText = new Identifier().TimestampText("not an identifier");
        IdentifierTimestampResult success = new ObjectIdValue(new byte[12]).Timestamp();

        Assert.That(overflow.Ok, Is.False);
        Assert.That(overflow.Code, Is.EqualTo(IdentifierErrorCode.TimestampOverflow));
        Assert.That(badVariant.Code, Is.EqualTo(IdentifierErrorCode.InvalidUuidVariant));
        Assert.That(badVersion.Code, Is.EqualTo(IdentifierErrorCode.InvalidUuidVersion));
        Assert.That(badText.Code, Is.EqualTo(IdentifierErrorCode.UnsupportedKind));
        Assert.That(success.Code, Is.EqualTo(IdentifierErrorCode.None));
    }

    [Test]
    public void ThrowingApis_PreserveParseAndTimestampCodes()
    {
        IdentifierException invalidLength = Assert.Throws<IdentifierException>(() => new Ulid().ParseValue("short"));
        IdentifierException invalidCharacter = Assert.Throws<IdentifierException>(() => new ObjectId().ParseValue("00000000000000000000000g"));
        IdentifierException byteLength = Assert.Throws<IdentifierException>(() => new UuidValue(new byte[15]));
        IdentifierException timestamp = Assert.Throws<IdentifierException>(() => new Uuid().NewV7AtUnixMilliseconds(-1));
        IdentifierException time = Assert.Throws<IdentifierException>(() => new UlidValue(MaxTimestampBytes()).Time());

        Assert.That(invalidLength.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(invalidCharacter.Code, Is.EqualTo(IdentifierErrorCode.InvalidCharacter));
        Assert.That(byteLength.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(timestamp.Code, Is.EqualTo(IdentifierErrorCode.TimestampOverflow));
        Assert.That(time.Code, Is.EqualTo(IdentifierErrorCode.TimestampOverflow));
        Assert.That(timestamp.Message, Is.Not.Empty);
    }

    [Test]
    public void ThrowingApis_ExposeUuidVersionAndVariantCodes()
    {
        UuidValue wrongVersion = new Uuid().ParseValue("00000000-0000-4000-8000-000000000000");
        UuidValue wrongVariant = new Uuid().ParseValue("00000000-0000-1000-0000-000000000000");

        IdentifierException version = Assert.Throws<IdentifierException>(() => new Uuid().V1ToV6(wrongVersion));
        IdentifierException variant = Assert.Throws<IdentifierException>(() => new Uuid().V1ToV6(wrongVariant));

        Assert.That(version.Code, Is.EqualTo(IdentifierErrorCode.InvalidUuidVersion));
        Assert.That(variant.Code, Is.EqualTo(IdentifierErrorCode.InvalidUuidVariant));
    }

    [Test]
    public void MonotonicGenerators_ExposeRollbackRandomAndSequenceCodes()
    {
        UlidGeneratorState rollbackState = new UlidGeneratorState();
        rollbackState.NextAtUnixMilliseconds(10);
        IdentifierException rollback = Assert.Throws<IdentifierException>(() => rollbackState.NextAtUnixMilliseconds(9));

        UlidGeneratorState randomState = UlidGeneratorState.FromLastValue(new UlidValue(MaxRandomnessBytes(10)));
        IdentifierException random = Assert.Throws<IdentifierException>(() => randomState.NextAtUnixMilliseconds(10));

        SnowflakeSettings settings = new SnowflakeSettings(0, 0, 22, 0, SnowflakeClockRollbackBehavior.FailFast);
        SnowflakeGenerator snowflake = new SnowflakeGenerator(settings);
        snowflake.NextAtUnixMilliseconds(10);
        IdentifierException sequence = Assert.Throws<IdentifierException>(() => snowflake.NextAtUnixMilliseconds(10));

        Assert.That(rollback.Code, Is.EqualTo(IdentifierErrorCode.ClockRollback));
        Assert.That(random.Code, Is.EqualTo(IdentifierErrorCode.RandomFieldOverflow));
        Assert.That(sequence.Code, Is.EqualTo(IdentifierErrorCode.SequenceExhaustion));
    }

    [Test]
    public void NanoIdThrowingApis_ExposeAlphabetAndLengthCodes()
    {
        IdentifierException alphabet = Assert.Throws<IdentifierException>(() => new NanoIdSettings("aa", 10));
        IdentifierException size = Assert.Throws<IdentifierException>(() => new NanoId().NewWithSize(-1));

        Assert.That(alphabet.Code, Is.EqualTo(IdentifierErrorCode.AlphabetError));
        Assert.That(size.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
    }

    static byte[] MaxTimestampBytes()
    {
        byte[] bytes = new byte[16];
        int index = 0;
        while (index < 6)
        {
            bytes[index] = 255;
            index = index + 1;
        }

        return bytes;
    }

    static byte[] MaxRandomnessBytes(long unixMilliseconds)
    {
        byte[] bytes = new byte[16];
        BigEndianTools.WriteUInt48(bytes, 0, unixMilliseconds);
        int index = 6;
        while (index < bytes.Length)
        {
            bytes[index] = 255;
            index = index + 1;
        }

        return bytes;
    }
}
