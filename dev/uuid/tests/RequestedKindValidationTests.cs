using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class RequestedKindValidationTests
{
    [Test]
    public void ValidateKind_AcceptsEverySupportedRequestedKind()
    {
        IIdentifier identifier = new Identifier();
        string nanoId = NanoIdSettings.DefaultAlphabet.Substring(0, NanoIdSettings.DefaultSize);

        IdentifierValidationResult uuid = identifier.ValidateKind("550E8400E29B41D4A716446655440000", IdentifierKind.Uuid);
        IdentifierValidationResult ulid = identifier.ValidateKind("01arz3ndektsv4rrffq69g5fav", IdentifierKind.Ulid);
        IdentifierValidationResult ksuid = identifier.ValidateKind("0ujtsYcgvSTl8PAuAdqWYSMnLOv", IdentifierKind.Ksuid);
        IdentifierValidationResult snowflake = identifier.ValidateKind("0004194308096", IdentifierKind.Snowflake);
        IdentifierValidationResult objectId = identifier.ValidateKind("507F1F77BCF86CD799439011", IdentifierKind.ObjectId);
        IdentifierValidationResult nano = identifier.ValidateKind(nanoId, IdentifierKind.NanoId);

        AssertSuccess(uuid, IdentifierKind.Uuid, "550e8400-e29b-41d4-a716-446655440000");
        AssertSuccess(ulid, IdentifierKind.Ulid, "01ARZ3NDEKTSV4RRFFQ69G5FAV");
        AssertSuccess(ksuid, IdentifierKind.Ksuid, "0ujtsYcgvSTl8PAuAdqWYSMnLOv");
        AssertSuccess(snowflake, IdentifierKind.Snowflake, "4194308096");
        AssertSuccess(objectId, IdentifierKind.ObjectId, "507f1f77bcf86cd799439011");
        AssertSuccess(nano, IdentifierKind.NanoId, nanoId);
    }

    [Test]
    public void ValidateKind_UsesRequestedKindForAmbiguousText()
    {
        string text = "000000000000000000000001";
        Identifier identifier = new Identifier();

        IdentifierValidationResult classified = identifier.Validate(text);
        IdentifierValidationResult objectId = identifier.ValidateKind(text, IdentifierKind.ObjectId);
        IdentifierValidationResult snowflake = identifier.ValidateKind(text, IdentifierKind.Snowflake);

        AssertSuccess(classified, IdentifierKind.ObjectId, text);
        AssertSuccess(objectId, IdentifierKind.ObjectId, text);
        AssertSuccess(snowflake, IdentifierKind.Snowflake, "1");
    }

    [Test]
    public void ValidateKind_DoesNotFallBackToAnotherFormat()
    {
        Identifier identifier = new Identifier();

        IdentifierValidationResult uuidAsObjectId = identifier.ValidateKind(
            "550e8400-e29b-41d4-a716-446655440000",
            IdentifierKind.ObjectId);
        IdentifierValidationResult objectIdAsSnowflake = identifier.ValidateKind(
            "507f1f77bcf86cd799439011",
            IdentifierKind.Snowflake);
        IdentifierValidationResult nanoAsUuid = identifier.ValidateKind(
            NanoIdSettings.DefaultAlphabet.Substring(0, NanoIdSettings.DefaultSize),
            IdentifierKind.Uuid);

        Assert.That(uuidAsObjectId.Ok, Is.False);
        Assert.That(uuidAsObjectId.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
        Assert.That(objectIdAsSnowflake.Ok, Is.False);
        Assert.That(objectIdAsSnowflake.Code, Is.EqualTo(IdentifierErrorCode.InvalidCharacter));
        Assert.That(nanoAsUuid.Ok, Is.False);
        Assert.That(nanoAsUuid.Code, Is.EqualTo(IdentifierErrorCode.InvalidLength));
    }

    [Test]
    public void ValidateKind_RejectsUnknownKindsWithoutClassification()
    {
        Identifier identifier = new Identifier();
        string validUuid = "550e8400-e29b-41d4-a716-446655440000";

        IdentifierValidationResult unknown = identifier.ValidateKind(validUuid, IdentifierKind.Unknown);
        IdentifierValidationResult unsupported = identifier.ValidateKind(validUuid, (IdentifierKind)999);

        AssertFailure(unknown, IdentifierErrorCode.UnsupportedKind, "requested identifier kind is unknown");
        AssertFailure(unsupported, IdentifierErrorCode.UnsupportedKind, "requested identifier kind is unknown");
    }

    [Test]
    public void ValidateKind_PreservesSelectedValidatorFailureCodes()
    {
        Identifier identifier = new Identifier();

        IdentifierValidationResult uuid = identifier.ValidateKind(null, IdentifierKind.Uuid);
        IdentifierValidationResult ulid = identifier.ValidateKind(null, IdentifierKind.Ulid);
        IdentifierValidationResult ksuid = identifier.ValidateKind(null, IdentifierKind.Ksuid);
        IdentifierValidationResult snowflake = identifier.ValidateKind(null, IdentifierKind.Snowflake);
        IdentifierValidationResult objectId = identifier.ValidateKind(null, IdentifierKind.ObjectId);
        IdentifierValidationResult nano = identifier.ValidateKind(null, IdentifierKind.NanoId);

        Assert.That(uuid.Code, Is.EqualTo(IdentifierErrorCode.NullInput));
        Assert.That(ulid.Code, Is.EqualTo(IdentifierErrorCode.NullInput));
        Assert.That(ksuid.Code, Is.EqualTo(IdentifierErrorCode.NullInput));
        Assert.That(snowflake.Code, Is.EqualTo(IdentifierErrorCode.NullInput));
        Assert.That(objectId.Code, Is.EqualTo(IdentifierErrorCode.NullInput));
        Assert.That(nano.Code, Is.EqualTo(IdentifierErrorCode.NullInput));
    }

    static void AssertSuccess(IdentifierValidationResult result, IdentifierKind kind, string normalizedText)
    {
        Assert.That(result.Ok, Is.True);
        Assert.That(result.Kind, Is.EqualTo(kind));
        Assert.That(result.NormalizedText, Is.EqualTo(normalizedText));
        Assert.That(result.Code, Is.EqualTo(IdentifierErrorCode.None));
        Assert.That(result.Error, Is.EqualTo(""));
    }

    static void AssertFailure(IdentifierValidationResult result, IdentifierErrorCode code, string error)
    {
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Code, Is.EqualTo(code));
        Assert.That(result.Error, Is.EqualTo(error));
    }
}
