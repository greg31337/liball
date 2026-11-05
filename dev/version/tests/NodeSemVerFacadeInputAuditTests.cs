using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeSemVerFacadeInputAuditTests
{
    [Test]
    public void CleanResult_RebasesStrictDiagnosticsToOriginalInput()
    {
        NodeSemVerValidResult missingPatch = NodeSemVer.CleanResult(" \t= v1.2 ");
        NodeSemVerValidResult illegalCharacter = NodeSemVer.CleanResult(" = vbad");

        Assert.That(missingPatch.IsInvalid(), Is.True);
        Assert.That(missingPatch.GetSourceText(), Is.EqualTo(" \t= v1.2 "));
        Assert.That(missingPatch.GetDiagnostic(0).GetSourceText(), Is.EqualTo(" \t= v1.2 "));
        Assert.That(missingPatch.GetDiagnostic(0).GetCode(), Is.EqualTo(SemVerDiagnosticCodes.MissingPatch));
        Assert.That(missingPatch.GetDiagnostic(0).GetIndex(), Is.EqualTo(8));

        Assert.That(illegalCharacter.IsInvalid(), Is.True);
        Assert.That(illegalCharacter.GetDiagnostic(0).GetSourceText(), Is.EqualTo(" = vbad"));
        Assert.That(illegalCharacter.GetDiagnostic(0).GetCode(), Is.EqualTo(SemVerDiagnosticCodes.IllegalCharacter));
        Assert.That(illegalCharacter.GetDiagnostic(0).GetIndex(), Is.EqualTo(4));
    }

    [Test]
    public void VersionParsingFacades_ReturnNullEquivalentResultsForNullEmptyAndWhitespace()
    {
        Assert.That(NodeSemVer.Valid(null), Is.Null);
        Assert.That(NodeSemVer.Valid(""), Is.Null);
        Assert.That(NodeSemVer.Valid("  "), Is.Null);
        Assert.That(NodeSemVer.ValidLoose(null), Is.Null);
        Assert.That(NodeSemVer.ValidLoose(""), Is.Null);
        Assert.That(NodeSemVer.ValidLoose("  "), Is.Null);
        Assert.That(NodeSemVer.Clean(null), Is.Null);
        Assert.That(NodeSemVer.Clean(""), Is.Null);
        Assert.That(NodeSemVer.Clean("  "), Is.Null);
        Assert.That(NodeSemVer.CleanLoose(null), Is.Null);
        Assert.That(NodeSemVer.CleanLoose(""), Is.Null);
        Assert.That(NodeSemVer.CleanLoose("  "), Is.Null);
    }

    [Test]
    public void ParseAndCoerceResults_CarryConsistentDiagnosticsForNullEmptyAndWhitespace()
    {
        AssertParseFailure(NodeSemVer.Parse(null), SemVerDiagnosticCodes.NullInput, "");
        AssertParseFailure(NodeSemVer.Parse(""), SemVerDiagnosticCodes.MissingMajor, "");
        AssertParseFailure(NodeSemVer.Parse("  "), SemVerDiagnosticCodes.MissingMajor, "  ");
        AssertParseFailure(NodeSemVer.Coerce(null), SemVerDiagnosticCodes.NullInput, "");
        AssertParseFailure(NodeSemVer.Coerce(""), SemVerDiagnosticCodes.NoCoercibleVersion, "");
        AssertParseFailure(NodeSemVer.Coerce("  "), SemVerDiagnosticCodes.NoCoercibleVersion, "  ");
    }

    [Test]
    public void RangeFacades_TreatNullAsInvalidAndEmptyOrWhitespaceAsUniversal()
    {
        Assert.That(NodeSemVer.Satisfies(null, ""), Is.False);
        Assert.That(NodeSemVer.Satisfies("1.2.3", null), Is.False);
        Assert.That(NodeSemVer.Satisfies("1.2.3", ""), Is.True);
        Assert.That(NodeSemVer.Satisfies("1.2.3", "  "), Is.True);

        Assert.That(NodeSemVer.ValidRange(null), Is.Null);
        Assert.That(NodeSemVer.ValidRange(""), Is.EqualTo("*"));
        Assert.That(NodeSemVer.ValidRange("  "), Is.EqualTo("*"));
        Assert.That(NodeSemVer.MinVersion(null), Is.Null);
        Assert.That(NodeSemVer.MinVersion(""), Is.EqualTo("0.0.0"));
        Assert.That(NodeSemVer.MinVersion("  "), Is.EqualTo("0.0.0"));

        Assert.That(NodeSemVer.ToComparators(null), Is.EqualTo(new string[0][]));
        AssertUniversalComparators(NodeSemVer.ToComparators(""));
        AssertUniversalComparators(NodeSemVer.ToComparators("  "));
    }

    [Test]
    public void RangeAlgebraFacades_ReturnFalseOrNullForInvalidSemanticInputs()
    {
        Assert.That(NodeSemVer.Intersects(null, "*"), Is.False);
        Assert.That(NodeSemVer.Intersects("*", null), Is.False);
        Assert.That(NodeSemVer.Subset(null, "*"), Is.False);
        Assert.That(NodeSemVer.Subset("*", null), Is.False);
        Assert.That(NodeSemVer.Gtr(null, ""), Is.False);
        Assert.That(NodeSemVer.Ltr("", ""), Is.False);
        Assert.That(NodeSemVer.Outside("  ", "", ">"), Is.False);
        Assert.That(NodeSemVer.SimplifyRange(new string[] { "1.2.3" }, null), Is.Null);
        Assert.That(NodeSemVer.SimplifyRange(new string[] { "" }, "*"), Is.Null);
        Assert.That(NodeSemVer.SimplifyRange(new string[] { "  " }, "*"), Is.Null);
    }

    [Test]
    public void AccessorAndIncrementFacades_ReturnNullEquivalentResultsForNullEmptyAndWhitespace()
    {
        Assert.That(NodeSemVer.GetMajor(null), Is.Null);
        Assert.That(NodeSemVer.GetMinor(""), Is.Null);
        Assert.That(NodeSemVer.GetPatch("  "), Is.Null);
        Assert.That(NodeSemVer.GetPrerelease(null), Is.EqualTo(new string[0]));
        Assert.That(NodeSemVer.GetPrerelease(""), Is.EqualTo(new string[0]));
        Assert.That(NodeSemVer.GetBuildMetadata("  "), Is.EqualTo(new string[0]));
        Assert.That(NodeSemVer.GetPrereleaseText(""), Is.Null);
        Assert.That(NodeSemVer.GetBuildMetadataText("  "), Is.Null);

        Assert.That(NodeSemVer.Major(null), Is.Null);
        Assert.That(NodeSemVer.Minor(""), Is.Null);
        Assert.That(NodeSemVer.Patch("  "), Is.Null);
        Assert.That(NodeSemVer.Premajor(null), Is.Null);
        Assert.That(NodeSemVer.Preminor(""), Is.Null);
        Assert.That(NodeSemVer.Prepatch("  "), Is.Null);
        Assert.That(NodeSemVer.Prerelease(null), Is.Null);
        Assert.That(NodeSemVer.Release(""), Is.Null);
    }

    [Test]
    public void ComparisonAndDiffFacades_ThrowConsistentlyForInvalidVersionInputs()
    {
        AssertVersionArgumentException(delegate { NodeSemVer.Compare(null, "1.2.3"); });
        AssertVersionArgumentException(delegate { NodeSemVer.RCompare("", "1.2.3"); });
        AssertVersionArgumentException(delegate { NodeSemVer.CompareLoose("  ", "1.2.3"); });
        AssertVersionArgumentException(delegate { NodeSemVer.CompareBuild("1.2.3", null); });
        AssertVersionArgumentException(delegate { NodeSemVer.Eq(null, "1.2.3"); });
        AssertVersionArgumentException(delegate { NodeSemVer.Neq("", "1.2.3"); });
        AssertVersionArgumentException(delegate { NodeSemVer.Gt("  ", "1.2.3"); });
        AssertVersionArgumentException(delegate { NodeSemVer.Gte("1.2.3", null); });
        AssertVersionArgumentException(delegate { NodeSemVer.Lt("1.2.3", ""); });
        AssertVersionArgumentException(delegate { NodeSemVer.Lte("1.2.3", "  "); });
        AssertVersionArgumentException(delegate { NodeSemVer.Cmp(null, "=", "1.2.3"); });
        AssertVersionArgumentException(delegate { NodeSemVer.Diff("", "1.2.3"); });
        AssertVersionArgumentException(delegate { NodeSemVer.DiffKind("1.2.3", "  "); });
    }

    static void AssertParseFailure(VersionParseResult result, string expectedCode, string expectedSourceText)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetSourceText(), Is.EqualTo(expectedSourceText));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(expectedCode));
        Assert.That(result.GetDiagnostic(0).GetSourceText(), Is.EqualTo(expectedSourceText));
    }

    static void AssertUniversalComparators(string[][] comparatorSets)
    {
        Assert.That(comparatorSets.Length, Is.EqualTo(1));
        Assert.That(comparatorSets[0], Is.EqualTo(new string[0]));
    }

    static void AssertVersionArgumentException(TestDelegate action)
    {
        Assert.That(action, Throws.TypeOf<System.ArgumentException>());
    }
}
