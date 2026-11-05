using icc;

namespace icc.Tests;

public class ProfileModelTests
{
    [Test]
    public void VersionDefaultsToAnUnspecifiedPackedValue()
    {
        IccVersion version = new IccVersion();

        Assert.Multiple(() =>
        {
            Assert.That(version.Major, Is.Zero);
            Assert.That(version.Minor, Is.Zero);
            Assert.That(version.BugFix, Is.Zero);
            Assert.That(version.SubclassMajor, Is.Zero);
            Assert.That(version.SubclassMinor, Is.Zero);
            Assert.That(version.IsSupported, Is.False);
            Assert.That(version.IsFutureMinor, Is.False);
            Assert.That(version.PackedValue, Is.Zero);
        });
    }

    [Test]
    public void ProfileHeaderProvidesCompleteFixedSizeRawStorage()
    {
        IccProfileHeader header = new IccProfileHeader();

        Assert.Multiple(() =>
        {
            Assert.That(header.Version, Is.Not.Null);
            Assert.That(header.Version.Kind, Is.EqualTo(IccVersionKind.Unknown));
            Assert.That(header.ProfileClass, Is.EqualTo(IccProfileClass.Unknown));
            Assert.That(header.DataColorSpace, Is.EqualTo(IccColorSpaceKind.Unknown));
            Assert.That(header.ProfileConnectionSpace, Is.EqualTo(IccPcsKind.None));
            Assert.That(header.RenderingIntentKind, Is.EqualTo(IccRenderingIntent.Perceptual));
            Assert.That(header.CreationDateTime, Is.Not.Null);
            Assert.That(header.ProfileId, Is.Not.Null);
            Assert.That(header.ProfileId.Bytes, Has.Length.EqualTo(16));
            Assert.That(header.ProfileId.IsPresent, Is.False);
            Assert.That(header.IsEmbedded, Is.False);
            Assert.That(header.RequiresEmbeddedData, Is.False);
            Assert.That(header.RequiresMcsSubset, Is.False);
            Assert.That(header.UsesExtendedPcsRange, Is.False);
            Assert.That(header.ContextualBytes, Has.Length.EqualTo(28));
            Assert.That(header.RawBytes, Has.Length.EqualTo(128));
            Assert.That(header.ValidationIssues, Is.Empty);
            Assert.That(header.SpectralPcsSignature, Is.Zero);
            Assert.That(header.SpectralPcs, Is.EqualTo(IccColorSpaceKind.Unknown));
            Assert.That(header.MaterialConnectionSpaceSignature, Is.Zero);
            Assert.That(header.MaterialConnectionSpace, Is.EqualTo(IccColorSpaceKind.Unknown));
            Assert.That(header.ProfileSubclassSignature, Is.Zero);
            Assert.That(header.IccMaxReservedValue, Is.Zero);
        });
    }

    [Test]
    public void ProfileHeadersDoNotShareMutableChildStorage()
    {
        IccProfileHeader first = new IccProfileHeader();
        IccProfileHeader second = new IccProfileHeader();

        first.RawBytes[0] = 42;
        first.ContextualBytes[0] = 43;
        first.ProfileId.Bytes[0] = 44;
        first.ValidationIssues = new IccValidationIssue[] { new IccValidationIssue() };
        first.Version.Major = 5;
        first.CreationDateTime.Year = 2026;

        Assert.Multiple(() =>
        {
            Assert.That(second.RawBytes[0], Is.Zero);
            Assert.That(second.ContextualBytes[0], Is.Zero);
            Assert.That(second.ProfileId.Bytes[0], Is.Zero);
            Assert.That(second.ValidationIssues, Is.Empty);
            Assert.That(second.Version.Major, Is.Zero);
            Assert.That(second.CreationDateTime.Year, Is.Zero);
            Assert.That(first.RawBytes, Is.Not.SameAs(second.RawBytes));
            Assert.That(first.ProfileId, Is.Not.SameAs(second.ProfileId));
        });
    }

    [Test]
    public void TagTableEntryDefaultsToNoOriginalOrSharedIdentity()
    {
        IccTagTableEntry entry = new IccTagTableEntry();

        Assert.Multiple(() =>
        {
            Assert.That(entry.Signature, Is.Zero);
            Assert.That(entry.Offset, Is.Zero);
            Assert.That(entry.Size, Is.Zero);
            Assert.That(entry.OriginalIndex, Is.EqualTo(-1));
            Assert.That(entry.SharedDataId, Is.EqualTo(-1));
        });
    }

    [Test]
    public void TagStartsWithLosslessUnassignedData()
    {
        IccTag tag = new IccTag();

        Assert.Multiple(() =>
        {
            Assert.That(tag.TableEntry, Is.Not.Null);
            Assert.That(tag.Data.Kind, Is.EqualTo(IccTagDataKind.Unknown));
            Assert.That(tag.Data.RawData, Is.Not.Null);
            Assert.That(tag.Data.RawData.HasTypeSignature, Is.False);
            Assert.That(tag.Data.RawData.Bytes, Is.Empty);
            Assert.That(tag.PaddingBytes, Is.Empty);
            Assert.That(tag.IsDecodeAttempted, Is.False);
            Assert.That(tag.DecodeResult.Status, Is.EqualTo(IccOperationStatus.Unspecified));
        });
    }

    [Test]
    public void ProfileStartsAsAnIndependentEmptyEnvelope()
    {
        IccProfile first = new IccProfile();
        IccProfile second = new IccProfile();

        first.Header.DeclaredSize = 128;
        first.ReadResult.Status = IccOperationStatus.Success;
        first.ValidationIssues = new IccValidationIssue[] { new IccValidationIssue() };

        Assert.Multiple(() =>
        {
            Assert.That(first.TagTableEntries, Is.Empty);
            Assert.That(first.Tags, Is.Empty);
            Assert.That(first.ValidationIssues, Has.Length.EqualTo(1));
            Assert.That(second.ValidationIssues, Is.Empty);
            Assert.That(first.OriginalBytes, Is.Empty);
            Assert.That(first.TrailingBytes, Is.Empty);
            Assert.That(first.Location.Offset, Is.EqualTo(-1));
            Assert.That(second.Header.DeclaredSize, Is.Zero);
            Assert.That(second.ReadResult.Status, Is.EqualTo(IccOperationStatus.Unspecified));
            Assert.That(first.Header, Is.Not.SameAs(second.Header));
            Assert.That(first.Tags, Is.Not.SameAs(second.Tags));
            Assert.That(first.ValidationIssues, Is.Not.SameAs(second.ValidationIssues));
            Assert.That(first.ReadResult, Is.Not.SameAs(second.ReadResult));
        });
    }
}
