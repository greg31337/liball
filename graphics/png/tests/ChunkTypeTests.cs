using System.Reflection;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class ChunkTypeTests
{
    [Test]
    public void PngChunkType_UsesInternalFieldsOnly()
    {
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        Assert.That(typeof(PngChunkType).IsNotPublic, Is.True);
        Assert.That(typeof(PngChunkType).GetProperties(flags), Is.Empty);
        Assert.That(typeof(PngChunkType).GetEvents(flags), Is.Empty);
        Assert.That(typeof(PngChunkType).GetMethods(flags), Is.Empty);
        Assert.That(typeof(PngChunkType).GetFields(flags), Has.Length.EqualTo(5));
    }

    [Test]
    public void Parse_ConvertsFourBytesToChunkName()
    {
        PngChunkType chunkType = PngChunkTypeParser.Parse(73, 72, 68, 82);

        Assert.That(chunkType.Name, Is.EqualTo("IHDR"));
        Assert.That(chunkType.Byte0, Is.EqualTo(73));
        Assert.That(chunkType.Byte1, Is.EqualTo(72));
        Assert.That(chunkType.Byte2, Is.EqualTo(68));
        Assert.That(chunkType.Byte3, Is.EqualTo(82));
    }

    [Test]
    public void Parse_ReadsChunkTypeFromArrayOffset()
    {
        byte[] data = new byte[] { 0x00, 0xAA, 73, 69, 78, 68, 0xBB };

        PngChunkType chunkType = PngChunkTypeParser.Parse(data, 2);

        Assert.That(chunkType.Name, Is.EqualTo("IEND"));
        Assert.That(chunkType.Byte0, Is.EqualTo(73));
        Assert.That(chunkType.Byte1, Is.EqualTo(69));
        Assert.That(chunkType.Byte2, Is.EqualTo(78));
        Assert.That(chunkType.Byte3, Is.EqualTo(68));
    }

    [Test]
    public void Parse_ReadsFromStartAndLastValidArrayOffset()
    {
        byte[] data = new byte[] { 73, 72, 68, 82, 0, 116, 69, 88, 116 };

        PngChunkType first = PngChunkTypeParser.Parse(data);
        PngChunkType last = PngChunkTypeParser.Parse(data, 5);

        Assert.That(first.Name, Is.EqualTo("IHDR"));
        Assert.That(last.Name, Is.EqualTo("tEXt"));
    }

    [Test]
    public void Parse_DoesNotApplyNamingValidation()
    {
        PngChunkType chunkType = PngChunkTypeParser.Parse(0, 255, 65, 97);

        Assert.That(chunkType.Name.Length, Is.EqualTo(4));
        Assert.That(chunkType.Byte0, Is.EqualTo(0));
        Assert.That(chunkType.Byte1, Is.EqualTo(255));
        Assert.That(chunkType.Byte2, Is.EqualTo(65));
        Assert.That(chunkType.Byte3, Is.EqualTo(97));
    }

    [Test]
    public void Parse_RejectsInvalidArrayInputs()
    {
        byte[] data = new byte[] { 73, 72, 68, 82 };

        Assert.Throws<ArgumentNullException>(() => PngChunkTypeParser.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkTypeParser.Parse(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChunkTypeParser.Parse(data, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngChunkTypeParser.Parse(data, 1));
    }

    [Test]
    public void Flags_ClassifyCriticalPublicReservedUnsafeChunk()
    {
        PngChunkType chunkType = PngChunkTypeParser.Parse(73, 72, 68, 82);

        Assert.That(PngChunkTypeFlags.IsCritical(chunkType), Is.True);
        Assert.That(PngChunkTypeFlags.IsAncillary(chunkType), Is.False);
        Assert.That(PngChunkTypeFlags.IsPublic(chunkType), Is.True);
        Assert.That(PngChunkTypeFlags.IsPrivate(chunkType), Is.False);
        Assert.That(PngChunkTypeFlags.IsReservedBitValid(chunkType), Is.True);
        Assert.That(PngChunkTypeFlags.IsSafeToCopy(chunkType), Is.False);
    }

    [Test]
    public void Flags_ClassifyAncillaryPublicReservedSafeChunk()
    {
        PngChunkType chunkType = PngChunkTypeParser.Parse(116, 69, 88, 116);

        Assert.That(PngChunkTypeFlags.IsCritical(chunkType), Is.False);
        Assert.That(PngChunkTypeFlags.IsAncillary(chunkType), Is.True);
        Assert.That(PngChunkTypeFlags.IsPublic(chunkType), Is.True);
        Assert.That(PngChunkTypeFlags.IsPrivate(chunkType), Is.False);
        Assert.That(PngChunkTypeFlags.IsReservedBitValid(chunkType), Is.True);
        Assert.That(PngChunkTypeFlags.IsSafeToCopy(chunkType), Is.True);
    }

    [Test]
    public void Flags_ClassifyPrivateAndInvalidReservedBit()
    {
        PngChunkType privateChunk = PngChunkTypeParser.Parse(118, 112, 65, 103);
        PngChunkType invalidReserved = PngChunkTypeParser.Parse(97, 98, 99, 100);

        Assert.That(PngChunkTypeFlags.IsPrivate(privateChunk), Is.True);
        Assert.That(PngChunkTypeFlags.IsPublic(privateChunk), Is.False);
        Assert.That(PngChunkTypeFlags.IsReservedBitValid(privateChunk), Is.True);

        Assert.That(PngChunkTypeFlags.IsReservedBitValid(invalidReserved), Is.False);
    }

    [Test]
    public void StringFlags_MatchParsedChunkTypeFlags()
    {
        PngChunkType chunkType = PngChunkTypeParser.Parse(116, 69, 88, 116);

        Assert.That(PngChunkTypeFlags.IsCritical("tEXt"), Is.EqualTo(PngChunkTypeFlags.IsCritical(chunkType)));
        Assert.That(PngChunkTypeFlags.IsAncillary("tEXt"), Is.EqualTo(PngChunkTypeFlags.IsAncillary(chunkType)));
        Assert.That(PngChunkTypeFlags.IsPublic("tEXt"), Is.EqualTo(PngChunkTypeFlags.IsPublic(chunkType)));
        Assert.That(PngChunkTypeFlags.IsPrivate("tEXt"), Is.EqualTo(PngChunkTypeFlags.IsPrivate(chunkType)));
        Assert.That(PngChunkTypeFlags.IsReservedBitValid("tEXt"), Is.EqualTo(PngChunkTypeFlags.IsReservedBitValid(chunkType)));
        Assert.That(PngChunkTypeFlags.IsSafeToCopy("tEXt"), Is.EqualTo(PngChunkTypeFlags.IsSafeToCopy(chunkType)));
    }

    [Test]
    public void StringFlags_ClassifyEachChunkTypeBitIndependently()
    {
        Assert.That(PngChunkTypeFlags.IsCritical("IHDR"), Is.True);
        Assert.That(PngChunkTypeFlags.IsCritical("iHDR"), Is.False);
        Assert.That(PngChunkTypeFlags.IsAncillary("iHDR"), Is.True);

        Assert.That(PngChunkTypeFlags.IsPublic("IHDR"), Is.True);
        Assert.That(PngChunkTypeFlags.IsPrivate("IpDR"), Is.True);
        Assert.That(PngChunkTypeFlags.IsPublic("IpDR"), Is.False);

        Assert.That(PngChunkTypeFlags.IsReservedBitValid("IHDR"), Is.True);
        Assert.That(PngChunkTypeFlags.IsReservedBitValid("IHdR"), Is.False);

        Assert.That(PngChunkTypeFlags.IsSafeToCopy("IHDR"), Is.False);
        Assert.That(PngChunkTypeFlags.IsSafeToCopy("IHDp"), Is.True);
    }

    [Test]
    public void Flags_RejectInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngChunkTypeFlags.IsCritical((PngChunkType)null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkTypeFlags.IsAncillary((PngChunkType)null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkTypeFlags.IsPublic((PngChunkType)null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkTypeFlags.IsPrivate((PngChunkType)null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkTypeFlags.IsReservedBitValid((PngChunkType)null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkTypeFlags.IsSafeToCopy((PngChunkType)null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkTypeFlags.IsCritical((string)null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkTypeFlags.IsAncillary((string)null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkTypeFlags.IsPublic((string)null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkTypeFlags.IsPrivate((string)null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkTypeFlags.IsReservedBitValid((string)null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkTypeFlags.IsSafeToCopy((string)null!));
        Assert.Throws<ArgumentException>(() => PngChunkTypeFlags.IsCritical("ABC"));
        Assert.Throws<ArgumentException>(() => PngChunkTypeFlags.IsCritical("ABCDE"));
        Assert.Throws<ArgumentException>(() => PngChunkTypeFlags.IsSafeToCopy("IHD\u0100"));
    }

    [Test]
    public void Validator_AcceptsValidChunkNames()
    {
        Assert.That(PngChunkTypeValidator.IsValid("IHDR"), Is.True);
        Assert.That(PngChunkTypeValidator.IsValid("tEXt"), Is.True);
        Assert.That(PngChunkTypeValidator.IsValid("vpAg"), Is.True);
    }

    [Test]
    public void Validator_IsValidReturnsFalseForInvalidChunkNames()
    {
        Assert.That(PngChunkTypeValidator.IsValid("ABC"), Is.False);
        Assert.That(PngChunkTypeValidator.IsValid("ABCDE"), Is.False);
        Assert.That(PngChunkTypeValidator.IsValid("IH1R"), Is.False);
        Assert.That(PngChunkTypeValidator.IsValid("IH\u0100R"), Is.False);
        Assert.That(PngChunkTypeValidator.IsValid("abca"), Is.False);
    }

    [Test]
    public void Validator_AcceptsValidParsedChunkType()
    {
        PngChunkType chunkType = PngChunkTypeParser.Parse(116, 69, 88, 116);

        PngValidationResult result = PngChunkTypeValidator.Validate(chunkType);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Warnings, Is.Empty);
    }

    [Test]
    public void Validator_RejectsInvalidLength()
    {
        PngValidationResult result = PngChunkTypeValidator.Validate("ABC");

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_CHUNK_TYPE_LENGTH"));
        Assert.That(result.Errors[0].ChunkType, Is.EqualTo("ABC"));
        Assert.That(result.Errors[0].Offset, Is.EqualTo(0));
        Assert.That(result.Errors[0].IsFatal, Is.True);
    }

    [Test]
    public void Validator_RejectsNonLetterCharacters()
    {
        PngValidationResult stringResult = PngChunkTypeValidator.Validate("IH1R");
        PngValidationResult unicodeResult = PngChunkTypeValidator.Validate("IH\u0100R");
        PngChunkType parsedChunkType = PngChunkTypeParser.Parse(73, 72, 255, 82);
        PngValidationResult parsedResult = PngChunkTypeValidator.Validate(parsedChunkType);

        Assert.That(stringResult.IsValid, Is.False);
        Assert.That(stringResult.Errors[0].Code, Is.EqualTo("PNG_CHUNK_TYPE_CHARACTER"));
        Assert.That(stringResult.Errors[0].Offset, Is.EqualTo(2));

        Assert.That(unicodeResult.IsValid, Is.False);
        Assert.That(unicodeResult.Errors[0].Code, Is.EqualTo("PNG_CHUNK_TYPE_CHARACTER"));
        Assert.That(unicodeResult.Errors[0].Offset, Is.EqualTo(2));

        Assert.That(parsedResult.IsValid, Is.False);
        Assert.That(parsedResult.Errors[0].Code, Is.EqualTo("PNG_CHUNK_TYPE_CHARACTER"));
        Assert.That(parsedResult.Errors[0].Offset, Is.EqualTo(2));
    }

    [Test]
    public void Validator_RejectsLowercaseReservedByte()
    {
        PngValidationResult result = PngChunkTypeValidator.Validate("abca");
        PngChunkType parsedChunkType = PngChunkTypeParser.Parse(97, 98, 99, 100);
        PngValidationResult parsedResult = PngChunkTypeValidator.Validate(parsedChunkType);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Length.EqualTo(1));
        Assert.That(result.Errors[0].Code, Is.EqualTo("PNG_CHUNK_TYPE_RESERVED_BIT"));
        Assert.That(result.Errors[0].Offset, Is.EqualTo(2));
        Assert.That(parsedResult.IsValid, Is.False);
        Assert.That(parsedResult.Errors[0].Code, Is.EqualTo("PNG_CHUNK_TYPE_RESERVED_BIT"));
        Assert.That(parsedResult.Errors[0].Offset, Is.EqualTo(2));
    }

    [Test]
    public void KnownChunkTypes_ClassifiesKnownCriticalAndAncillaryNames()
    {
        Assert.That(PngKnownChunkTypes.IsKnown(PngConstants.ChunkIHDR), Is.True);
        Assert.That(PngKnownChunkTypes.IsKnown(PngConstants.ChunkPLTE), Is.True);
        Assert.That(PngKnownChunkTypes.IsKnown(PngConstants.ChunkIDAT), Is.True);
        Assert.That(PngKnownChunkTypes.IsKnown(PngConstants.ChunkIEND), Is.True);
        Assert.That(PngKnownChunkTypes.IsKnown(PngConstants.ChunkText), Is.True);
        Assert.That(PngKnownChunkTypes.IsKnown(PngConstants.ChunkFrameData), Is.True);
        Assert.That(PngKnownChunkTypes.IsKnown("vpAg"), Is.False);
        Assert.Throws<ArgumentNullException>(() => PngKnownChunkTypes.IsKnown(null!));
    }

    [Test]
    public void Validator_RejectsNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() => PngChunkTypeValidator.IsValid((string)null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkTypeValidator.IsValid((PngChunkType)null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkTypeValidator.Validate((string)null!));
        Assert.Throws<ArgumentNullException>(() => PngChunkTypeValidator.Validate((PngChunkType)null!));
    }
}
