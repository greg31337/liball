using NUnit.Framework;

namespace version.Tests;

[TestFixture]
public class NodeSemVerInputLengthGuardTests
{
    [Test]
    public void Parse_AcceptsMaxLengthAndRejectsLongerOrNullInput()
    {
        string boundaryVersion = CreateBuildVersion(NodeSemVerInputGuard.MaxInputLength);
        string tooLongVersion = CreateBuildVersion(NodeSemVerInputGuard.MaxInputLength + 1);

        VersionParseResult accepted = NodeSemVer.Parse(boundaryVersion);
        VersionParseResult rejected = NodeSemVer.Parse(tooLongVersion);
        VersionParseResult nullRejected = NodeSemVer.Parse(null);
        VersionParseResult emptyRejected = NodeSemVer.Parse("");

        Assert.That(accepted.IsSuccess(), Is.True);
        Assert.That(accepted.GetVersion().ToNormalizedString(), Is.EqualTo(boundaryVersion));
        AssertVersionLengthFailure(rejected, tooLongVersion);
        AssertVersionNullFailure(nullRejected);
        Assert.That(emptyRejected.IsFailure(), Is.True);
        Assert.That(emptyRejected.GetDiagnostic(0).GetCode(), Is.EqualTo(SemVerDiagnosticCodes.MissingMajor));
    }

    [Test]
    public void Clean_AcceptsMaxLengthAndRejectsLongerNullOrEmptyInput()
    {
        string boundaryVersion = CreateBuildVersion(NodeSemVerInputGuard.MaxInputLength);
        string tooLongVersion = CreateBuildVersion(NodeSemVerInputGuard.MaxInputLength + 1);

        NodeSemVerValidResult accepted = NodeSemVer.CleanResult(boundaryVersion);
        NodeSemVerValidResult rejected = NodeSemVer.CleanResult(tooLongVersion);
        NodeSemVerValidResult nullRejected = NodeSemVer.CleanResult(null);
        NodeSemVerValidResult emptyRejected = NodeSemVer.CleanResult("");

        Assert.That(accepted.IsValid(), Is.True);
        Assert.That(accepted.GetVersionText(), Is.EqualTo(boundaryVersion));
        AssertTextLengthFailure(rejected, tooLongVersion);
        AssertTextNullFailure(nullRejected);
        Assert.That(emptyRejected.IsInvalid(), Is.True);
        Assert.That(emptyRejected.GetDiagnostic(0).GetCode(), Is.EqualTo(SemVerDiagnosticCodes.MissingMajor));
    }

    [Test]
    public void Coerce_AcceptsMaxLengthAndRejectsLongerNullOrEmptyInput()
    {
        string boundaryText = new string('x', NodeSemVerInputGuard.MaxInputLength - "1.2.3".Length) + "1.2.3";
        string tooLongText = new string('x', NodeSemVerInputGuard.MaxInputLength + 1 - "1.2.3".Length) + "1.2.3";

        VersionParseResult accepted = NodeSemVer.Coerce(boundaryText);
        VersionParseResult rejected = NodeSemVer.Coerce(tooLongText);
        VersionParseResult nullRejected = NodeSemVer.Coerce(null);
        VersionParseResult emptyRejected = NodeSemVer.Coerce("");

        Assert.That(accepted.IsSuccess(), Is.True);
        Assert.That(accepted.GetVersion().ToNormalizedString(), Is.EqualTo("1.2.3"));
        AssertVersionLengthFailure(rejected, tooLongText);
        AssertVersionNullFailure(nullRejected);
        Assert.That(emptyRejected.IsFailure(), Is.True);
        Assert.That(emptyRejected.GetDiagnostic(0).GetCode(), Is.EqualTo(SemVerDiagnosticCodes.NoCoercibleVersion));
    }

    [Test]
    public void Comparator_AcceptsMaxLengthAndRejectsLongerNullOrEmptyInput()
    {
        string boundaryComparator = CreateComparator(NodeSemVerInputGuard.MaxInputLength);
        string tooLongComparator = CreateComparator(NodeSemVerInputGuard.MaxInputLength + 1);

        VersionComparatorParseResult accepted = NodePrimitiveComparatorParser.Parse(boundaryComparator);
        VersionComparatorParseResult rejected = NodePrimitiveComparatorParser.Parse(tooLongComparator);
        VersionComparatorParseResult nullRejected = NodePrimitiveComparatorParser.Parse(null);
        VersionComparatorParseResult emptyRejected = NodePrimitiveComparatorParser.Parse("");

        Assert.That(accepted.IsSuccess(), Is.True);
        Assert.That(accepted.GetComparator().GetVersion().ToNormalizedString(), Is.EqualTo(boundaryComparator.Substring(2)));
        AssertRangeLengthFailure(rejected, tooLongComparator);
        AssertRangeNullFailure(nullRejected);
        Assert.That(emptyRejected.IsFailure(), Is.True);
        Assert.That(emptyRejected.GetDiagnostic(0).GetCode(), Is.EqualTo(NodeRangeDiagnosticCodes.MissingComparator));
    }

    [Test]
    public void Range_AcceptsMaxLengthAndRejectsLongerNullWhileEmptyRemainsUniversal()
    {
        string boundaryRange = CreateComparator(NodeSemVerInputGuard.MaxInputLength);
        string tooLongRange = CreateComparator(NodeSemVerInputGuard.MaxInputLength + 1);

        VersionRangeParseResult accepted = NodeSemVer.ParseRange(boundaryRange);
        VersionRangeParseResult rejected = NodeSemVer.ParseRange(tooLongRange);
        VersionRangeParseResult nullRejected = NodeSemVer.ParseRange(null);
        VersionRangeParseResult emptyAccepted = NodeSemVer.ParseRange("");

        Assert.That(accepted.IsSuccess(), Is.True);
        Assert.That(accepted.HasRange(), Is.True);
        AssertRangeLengthFailure(rejected, tooLongRange);
        AssertRangeNullFailure(nullRejected);
        Assert.That(emptyAccepted.IsSuccess(), Is.True);
        Assert.That(emptyAccepted.GetRange().IsUniversal(), Is.True);
    }

    static string CreateBuildVersion(int totalLength)
    {
        string prefix = "1.2.3+";
        return prefix + new string('a', totalLength - prefix.Length);
    }

    static string CreateComparator(int totalLength)
    {
        string prefix = ">=1.2.3+";
        return prefix + new string('a', totalLength - prefix.Length);
    }

    static void AssertVersionLengthFailure(VersionParseResult result, string sourceText)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetSourceText(), Is.EqualTo(sourceText));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(SemVerDiagnosticCodes.InputTooLong));
        Assert.That(result.GetDiagnostic(0).GetIndex(), Is.EqualTo(NodeSemVerInputGuard.MaxInputLength));
    }

    static void AssertVersionNullFailure(VersionParseResult result)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetSourceText(), Is.EqualTo(""));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(SemVerDiagnosticCodes.NullInput));
    }

    static void AssertTextLengthFailure(NodeSemVerValidResult result, string sourceText)
    {
        Assert.That(result.IsInvalid(), Is.True);
        Assert.That(result.GetSourceText(), Is.EqualTo(sourceText));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(SemVerDiagnosticCodes.InputTooLong));
        Assert.That(result.GetDiagnostic(0).GetIndex(), Is.EqualTo(NodeSemVerInputGuard.MaxInputLength));
    }

    static void AssertTextNullFailure(NodeSemVerValidResult result)
    {
        Assert.That(result.IsInvalid(), Is.True);
        Assert.That(result.GetSourceText(), Is.EqualTo(""));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(SemVerDiagnosticCodes.NullInput));
    }

    static void AssertRangeLengthFailure(VersionRangeParseResult result, string sourceText)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetSourceText(), Is.EqualTo(sourceText));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(NodeRangeDiagnosticCodes.InputTooLong));
        Assert.That(result.GetDiagnostic(0).GetIndex(), Is.EqualTo(NodeSemVerInputGuard.MaxInputLength));
    }

    static void AssertRangeLengthFailure(VersionComparatorParseResult result, string sourceText)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetSourceText(), Is.EqualTo(sourceText));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(NodeRangeDiagnosticCodes.InputTooLong));
        Assert.That(result.GetDiagnostic(0).GetIndex(), Is.EqualTo(NodeSemVerInputGuard.MaxInputLength));
    }

    static void AssertRangeNullFailure(VersionRangeParseResult result)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetSourceText(), Is.EqualTo(""));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(NodeRangeDiagnosticCodes.NullInput));
    }

    static void AssertRangeNullFailure(VersionComparatorParseResult result)
    {
        Assert.That(result.IsFailure(), Is.True);
        Assert.That(result.GetSourceText(), Is.EqualTo(""));
        Assert.That(result.GetDiagnosticCount(), Is.EqualTo(1));
        Assert.That(result.GetDiagnostic(0).GetCode(), Is.EqualTo(NodeRangeDiagnosticCodes.NullInput));
    }
}
