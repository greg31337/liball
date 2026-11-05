using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class SubdivisionHierarchyTests
{
    [Test]
    public void ParentLookupNormalizesCodesAndReportsAbsentParents()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();

        AssertFound(catalog.FindSubdivisionParent("az-bab"), "AZ-NX");
        AssertStatus(catalog.FindSubdivisionParent("AZ-NX"), LookupStatus.NotFound);
        AssertStatus(catalog.FindSubdivisionParent("AZ-ZZZ"), LookupStatus.NotFound);
        AssertStatus(catalog.FindSubdivisionParent("AZ_NX"), LookupStatus.InvalidInput);
        AssertStatus(catalog.FindSubdivisionParent(null!), LookupStatus.InvalidInput);
    }

    [Test]
    public void DirectChildrenUseStableFullCodeOrderAndDetachedSnapshots()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();
        string[] expected =
        {
            "AZ-BAB",
            "AZ-CUL",
            "AZ-KAN",
            "AZ-NV",
            "AZ-ORD",
            "AZ-SAD",
            "AZ-SAH",
            "AZ-SAR"
        };

        Subdivision[] first = catalog.GetSubdivisionChildren("az-nx");
        AssertCodes(first, expected);
        first[0].Code = "changed";

        Subdivision[] second = catalog.GetSubdivisionChildren("AZ-NX");
        AssertCodes(second, expected);
        Assert.That(catalog.GetSubdivisionChildren("AZ-BAB"), Is.Empty);
        Assert.That(catalog.GetSubdivisionChildren("AZ-ZZZ"), Is.Empty);
        Assert.That(catalog.GetSubdivisionChildren("AZ_NX"), Is.Empty);
        Assert.That(catalog.GetSubdivisionChildren(null!), Is.Empty);
    }

    [Test]
    public void AncestorsAreOrderedFromParentToRoot()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();

        AssertCodes(
            catalog.GetSubdivisionAncestors("az-bab"),
            new[] { "AZ-NX" });
        Assert.That(catalog.GetSubdivisionAncestors("AZ-NX"), Is.Empty);
        Assert.That(catalog.GetSubdivisionAncestors("AZ-ZZZ"), Is.Empty);
        Assert.That(catalog.GetSubdivisionAncestors("AZ_NX"), Is.Empty);
    }

    [Test]
    public void DescendantsAreReturnedOnceInStableFullCodeOrder()
    {
        ISubdivisionCatalog catalog = new CountryCodeService();
        string[] expected =
        {
            "GB-AGY",
            "GB-BGE",
            "GB-BGW",
            "GB-CAY",
            "GB-CGN",
            "GB-CMN",
            "GB-CRF",
            "GB-CWY",
            "GB-DEN",
            "GB-FLN",
            "GB-GWN",
            "GB-MON",
            "GB-MTY",
            "GB-NTL",
            "GB-NWP",
            "GB-PEM",
            "GB-POW",
            "GB-RCT",
            "GB-SWA",
            "GB-TOF",
            "GB-VGL",
            "GB-WRX"
        };

        AssertCodes(catalog.GetSubdivisionDescendants("gb-wls"), expected);
        Assert.That(catalog.GetSubdivisionDescendants("GB-AGY"), Is.Empty);
        Assert.That(catalog.GetSubdivisionDescendants("GB-ZZZ"), Is.Empty);
        Assert.That(catalog.GetSubdivisionDescendants("GB_WLS"), Is.Empty);
    }

    [Test]
    public void SyntheticCyclesTerminateWithoutRepeatingTheStartingRow()
    {
        GeneratedSubdivisionRow a = CreateRow("AA-A", "AA-C");
        GeneratedSubdivisionRow b = CreateRow("AA-B", "AA-A");
        GeneratedSubdivisionRow c = CreateRow("AA-C", "AA-B");
        GeneratedSubdivisionRow[] rows = { c, a, b };

        AssertRowCodes(
            SubdivisionHierarchy.GetChildRows(a, rows),
            new[] { "AA-B" });
        AssertRowCodes(
            SubdivisionHierarchy.GetAncestorRows(a, rows),
            new[] { "AA-C", "AA-B" });
        AssertRowCodes(
            SubdivisionHierarchy.GetDescendantRows(a, rows),
            new[] { "AA-B", "AA-C" });

        GeneratedSubdivisionRow self = CreateRow("AA-D", "AA-D");
        GeneratedSubdivisionRow[] selfRows = { self };
        Assert.That(SubdivisionHierarchy.GetAncestorRows(self, selfRows), Is.Empty);
        Assert.That(SubdivisionHierarchy.GetDescendantRows(self, selfRows), Is.Empty);
    }

    [Test]
    public void MissingParentReportsTheReferencedCode()
    {
        GeneratedSubdivisionRow child = CreateRow("AA-A", "AA-Z");

        SubdivisionNotFoundException exception =
            Assert.Throws<SubdivisionNotFoundException>(
                () => SubdivisionHierarchy.GetAncestorRows(
                    child,
                    new[] { child }))!;

        Assert.That(exception.Input, Is.EqualTo("AA-Z"));
        Assert.That(
            SubdivisionHierarchy.FindParentRow(child, new[] { child }),
            Is.Null);
    }

    private static GeneratedSubdivisionRow CreateRow(string code, string parentCode)
    {
        return new GeneratedSubdivisionRow(
            code,
            code.Substring(0, 2),
            code.Substring(3),
            "Test",
            GeneratedSubdivisionStatus.Active,
            parentCode.Length > 0,
            parentCode,
            "test",
            "test",
            code,
            "2026-09-15",
            "test",
            "test",
            parentCode,
            "2026-09-15",
            "parent");
    }

    private static void AssertFound(SubdivisionLookupResult result, string code)
    {
        Assert.That(result.Status, Is.EqualTo(LookupStatus.Found));
        Assert.That(result.Subdivision, Is.Not.Null);
        Assert.That(result.Subdivision!.Code, Is.EqualTo(code));
        Assert.That(result.Candidates, Is.Empty);
    }

    private static void AssertStatus(SubdivisionLookupResult result, LookupStatus status)
    {
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.Subdivision, Is.Null);
        Assert.That(result.Candidates, Is.Empty);
    }

    private static void AssertCodes(Subdivision[] values, string[] expected)
    {
        Assert.That(
            global::System.Array.ConvertAll(values, value => value.Code),
            Is.EqualTo(expected));
    }

    private static void AssertRowCodes(
        GeneratedSubdivisionRow[] values,
        string[] expected)
    {
        Assert.That(
            global::System.Array.ConvertAll(values, value => value.Code),
            Is.EqualTo(expected));
    }
}
