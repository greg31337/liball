using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class PublicEnumTests
{
    [Test]
    public void CountryCodeKindsHaveStableValues()
    {
        Assert.That(
            Enum.GetValues<CountryCodeKind>(),
            Is.EqualTo(new[]
            {
                CountryCodeKind.Unknown,
                CountryCodeKind.Alpha2,
                CountryCodeKind.Alpha3,
                CountryCodeKind.Numeric3,
                CountryCodeKind.Alpha4,
                CountryCodeKind.Subdivision
            }));
        Assert.That(
            Enum.GetValues<CountryCodeKind>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5 }));
    }

    [Test]
    public void CodeAssignmentStatusesHaveStableValues()
    {
        Assert.That(
            Enum.GetValues<CodeAssignmentStatus>(),
            Is.EqualTo(new[]
            {
                CodeAssignmentStatus.Assigned,
                CodeAssignmentStatus.ExceptionallyReserved,
                CodeAssignmentStatus.TransitionallyReserved,
                CodeAssignmentStatus.IndeterminatelyReserved,
                CodeAssignmentStatus.FormerlyAssigned,
                CodeAssignmentStatus.UserAssigned,
                CodeAssignmentStatus.Unassigned
            }));
        Assert.That(
            Enum.GetValues<CodeAssignmentStatus>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void CountryNameKindsHaveStableValues()
    {
        Assert.That(
            Enum.GetValues<CountryNameKind>(),
            Is.EqualTo(new[]
            {
                CountryNameKind.IsoShort,
                CountryNameKind.IsoOfficial,
                CountryNameKind.Common,
                CountryNameKind.NativeOrLocalized,
                CountryNameKind.Alias,
                CountryNameKind.Deprecated,
                CountryNameKind.Historical
            }));
        Assert.That(
            Enum.GetValues<CountryNameKind>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6 }));
    }

    [Test]
    public void SubdivisionStatusesHaveStableValues()
    {
        Assert.That(
            Enum.GetValues<SubdivisionStatus>(),
            Is.EqualTo(new[]
            {
                SubdivisionStatus.Active,
                SubdivisionStatus.Deprecated,
                SubdivisionStatus.Withdrawn,
                SubdivisionStatus.Unknown
            }));
        Assert.That(
            Enum.GetValues<SubdivisionStatus>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3 }));
    }

    [Test]
    public void CountryChangeKindsHaveStableValues()
    {
        Assert.That(
            Enum.GetValues<CountryChangeKind>(),
            Is.EqualTo(new[]
            {
                CountryChangeKind.SignificantChange,
                CountryChangeKind.Merge,
                CountryChangeKind.Split,
                CountryChangeKind.OtherOrUnknown
            }));
        Assert.That(
            Enum.GetValues<CountryChangeKind>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3 }));
    }

    [Test]
    public void GeographicRegionLevelsHaveStableValues()
    {
        Assert.That(
            Enum.GetValues<GeographicRegionLevel>(),
            Is.EqualTo(new[]
            {
                GeographicRegionLevel.World,
                GeographicRegionLevel.Region,
                GeographicRegionLevel.Subregion,
                GeographicRegionLevel.IntermediateRegion,
                GeographicRegionLevel.CountryOrArea
            }));
        Assert.That(
            Enum.GetValues<GeographicRegionLevel>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
    }

    [Test]
    public void LookupStatusesHaveStableValues()
    {
        Assert.That(
            Enum.GetValues<LookupStatus>(),
            Is.EqualTo(new[]
            {
                LookupStatus.Found,
                LookupStatus.NotFound,
                LookupStatus.Ambiguous,
                LookupStatus.InvalidInput
            }));
        Assert.That(
            Enum.GetValues<LookupStatus>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3 }));
    }

    [Test]
    public void NormalizationStatusesHaveStableValues()
    {
        Assert.That(
            Enum.GetValues<NormalizationStatus>(),
            Is.EqualTo(new[]
            {
                NormalizationStatus.AlreadyCanonical,
                NormalizationStatus.Normalized,
                NormalizationStatus.Replaced,
                NormalizationStatus.Ambiguous,
                NormalizationStatus.Invalid
            }));
        Assert.That(
            Enum.GetValues<NormalizationStatus>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
    }

    [Test]
    public void ValidationPoliciesHaveStableValues()
    {
        Assert.That(
            Enum.GetValues<ValidationPolicy>(),
            Is.EqualTo(new[]
            {
                ValidationPolicy.SyntaxOnly,
                ValidationPolicy.AssignedOnly,
                ValidationPolicy.AnyKnownIso,
                ValidationPolicy.AllowHistorical,
                ValidationPolicy.AllowUserAssigned
            }));
        Assert.That(
            Enum.GetValues<ValidationPolicy>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
    }

    [Test]
    public void ValidationFailureKindsHaveStableValues()
    {
        Assert.That(
            Enum.GetValues<ValidationFailureKind>(),
            Is.EqualTo(new[]
            {
                ValidationFailureKind.NullOrEmpty,
                ValidationFailureKind.InvalidLength,
                ValidationFailureKind.InvalidCharacter,
                ValidationFailureKind.InvalidShape,
                ValidationFailureKind.Unknown,
                ValidationFailureKind.Reserved,
                ValidationFailureKind.FormerlyAssigned,
                ValidationFailureKind.UserAssignedDisallowed,
                ValidationFailureKind.UnknownCountryPrefix,
                ValidationFailureKind.Deprecated,
                ValidationFailureKind.Ambiguous,
                ValidationFailureKind.UnsupportedLocale
            }));
        Assert.That(
            Enum.GetValues<ValidationFailureKind>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }));
    }

    [Test]
    public void SearchModesHaveStableValues()
    {
        Assert.That(
            Enum.GetValues<SearchMode>(),
            Is.EqualTo(new[]
            {
                SearchMode.ExactNormalized,
                SearchMode.Prefix,
                SearchMode.Token,
                SearchMode.Substring,
                SearchMode.Fuzzy
            }));
        Assert.That(
            Enum.GetValues<SearchMode>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
    }

    [Test]
    public void SearchMatchKindsHaveStableValues()
    {
        Assert.That(
            Enum.GetValues<SearchMatchKind>(),
            Is.EqualTo(new[]
            {
                SearchMatchKind.ExactCode,
                SearchMatchKind.ExactName,
                SearchMatchKind.ExactAlias,
                SearchMatchKind.Normalized,
                SearchMatchKind.Prefix,
                SearchMatchKind.Token,
                SearchMatchKind.Substring,
                SearchMatchKind.Fuzzy,
                SearchMatchKind.Localized,
                SearchMatchKind.Historical,
                SearchMatchKind.SubdivisionDerived
            }));
        Assert.That(
            Enum.GetValues<SearchMatchKind>().Select(value => (int)value),
            Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
    }
}
