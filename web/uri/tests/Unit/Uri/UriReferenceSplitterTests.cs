namespace uri.Tests;

public class UriReferenceSplitterTests
{
    [Test]
    public void HierarchicalPartUsesTheOffsetImmediatelyAfterTheScheme()
    {
        string text = "x://host/path?query#fragment";

        UriReferenceSplitResult result = UriParserCore.SplitHierarchicalPart(text.AsSpan(), 2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasAuthority, Is.True);
            Assert.That(result.AuthorityOffset, Is.EqualTo(4));
            Assert.That(result.AuthorityLength, Is.EqualTo(4));
            Assert.That(result.PathOffset, Is.EqualTo(8));
            Assert.That(result.PathLength, Is.EqualTo(5));
            Assert.That(result.HasQuery, Is.True);
            Assert.That(result.QueryOffset, Is.EqualTo(14));
            Assert.That(result.QueryLength, Is.EqualTo(5));
            Assert.That(result.HasFragment, Is.True);
            Assert.That(result.FragmentOffset, Is.EqualTo(20));
            Assert.That(result.FragmentLength, Is.EqualTo(8));
            Assert.That(result.Diagnostic, Is.Null);
        });
        AssertParts(text, result, "host", "/path", "query", "fragment");
    }

    [Test]
    public void PresentEmptyAuthorityQueryAndFragmentRetainDelimiterPresence()
    {
        string text = "x:///path?#";

        UriReferenceSplitResult result = UriParserCore.SplitHierarchicalPart(text.AsSpan(), 2);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasAuthority, Is.True);
            Assert.That(result.AuthorityOffset, Is.EqualTo(4));
            Assert.That(result.AuthorityLength, Is.Zero);
            Assert.That(result.PathOffset, Is.EqualTo(4));
            Assert.That(result.PathLength, Is.EqualTo(5));
            Assert.That(result.HasQuery, Is.True);
            Assert.That(result.QueryOffset, Is.EqualTo(10));
            Assert.That(result.QueryLength, Is.Zero);
            Assert.That(result.HasFragment, Is.True);
            Assert.That(result.FragmentOffset, Is.EqualTo(11));
            Assert.That(result.FragmentLength, Is.Zero);
        });
        AssertParts(text, result, "", "/path", "", "");
    }

    [Test]
    public void AbsentDelimitersRemainDistinctFromEmptyComponents()
    {
        string text = "x:";

        UriReferenceSplitResult result = UriParserCore.SplitHierarchicalPart(text.AsSpan(), 2);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasAuthority, Is.False);
            Assert.That(result.AuthorityOffset, Is.EqualTo(2));
            Assert.That(result.AuthorityLength, Is.Zero);
            Assert.That(result.PathOffset, Is.EqualTo(2));
            Assert.That(result.PathLength, Is.Zero);
            Assert.That(result.HasQuery, Is.False);
            Assert.That(result.QueryOffset, Is.EqualTo(2));
            Assert.That(result.QueryLength, Is.Zero);
            Assert.That(result.HasFragment, Is.False);
            Assert.That(result.FragmentOffset, Is.EqualTo(2));
            Assert.That(result.FragmentLength, Is.Zero);
        });
    }

    [TestCase("", false, "", "", false, "", false, "")]
    [TestCase("//host/path", true, "host", "/path", false, "", false, "")]
    [TestCase("///path", true, "", "/path", false, "", false, "")]
    [TestCase("//", true, "", "", false, "", false, "")]
    [TestCase("/path", false, "", "/path", false, "", false, "")]
    [TestCase("path", false, "", "path", false, "", false, "")]
    [TestCase("?query", false, "", "", true, "query", false, "")]
    [TestCase("#fragment", false, "", "", false, "", true, "fragment")]
    [TestCase("?#", false, "", "", true, "", true, "")]
    public void RelativePartSplitsEveryTopLevelForm(
        string text,
        bool hasAuthority,
        string authority,
        string path,
        bool hasQuery,
        string query,
        bool hasFragment,
        string fragment)
    {
        UriReferenceSplitResult result = UriParserCore.SplitRelativePart(text.AsSpan(), 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.HasAuthority, Is.EqualTo(hasAuthority));
            Assert.That(result.HasQuery, Is.EqualTo(hasQuery));
            Assert.That(result.HasFragment, Is.EqualTo(hasFragment));
        });
        AssertParts(text, result, authority, path, query, fragment);
    }

    [Test]
    public void QueryAndFragmentDelimitersAreAppliedInGrammarOrder()
    {
        string withQuery = "path?one?two#frag?still";
        string fragmentFirst = "path#frag?not-query";

        UriReferenceSplitResult first = UriParserCore.SplitRelativePart(withQuery.AsSpan(), 0);
        UriReferenceSplitResult second = UriParserCore.SplitRelativePart(fragmentFirst.AsSpan(), 0);

        AssertParts(withQuery, first, "", "path", "one?two", "frag?still");
        Assert.Multiple(() =>
        {
            Assert.That(first.HasQuery, Is.True);
            Assert.That(first.HasFragment, Is.True);
            Assert.That(second.HasQuery, Is.False);
            Assert.That(second.HasFragment, Is.True);
        });
        AssertParts(fragmentFirst, second, "", "path", "", "frag?not-query");
    }

    [Test]
    public void EncodedDelimitersRemainInsideTheirCurrentComponent()
    {
        string text = "//host%2Fname/path%3Fpart?x=%23#f%3F";

        UriReferenceSplitResult result = UriParserCore.SplitRelativePart(text.AsSpan(), 0);

        AssertParts(
            text,
            result,
            "host%2Fname",
            "/path%3Fpart",
            "x=%23",
            "f%3F");
    }

    [Test]
    public void AuthorityEndsAtPathQueryOrFragmentBoundary()
    {
        string withPath = "//host/path";
        string withQuery = "//host?query/path";
        string withFragment = "//host#fragment/path";

        UriReferenceSplitResult path = UriParserCore.SplitRelativePart(withPath.AsSpan(), 0);
        UriReferenceSplitResult query = UriParserCore.SplitRelativePart(withQuery.AsSpan(), 0);
        UriReferenceSplitResult fragment = UriParserCore.SplitRelativePart(withFragment.AsSpan(), 0);

        AssertParts(withPath, path, "host", "/path", "", "");
        AssertParts(withQuery, query, "host", "", "query/path", "");
        AssertParts(withFragment, fragment, "host", "", "", "fragment/path");
    }

    [Test]
    public void AuthorityDelimiterIsRecognizedOnlyAtThePartStart()
    {
        string text = "prefixa//b?x";

        UriReferenceSplitResult result = UriParserCore.SplitRelativePart(text.AsSpan(), 6);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasAuthority, Is.False);
            Assert.That(result.PathOffset, Is.EqualTo(6));
        });
        AssertParts(text, result, "", "a//b", "x", "");
    }

    [Test]
    public void HierarchicalAndRelativeEntryPointsShareTheSameBoundaryRules()
    {
        string text = "prefix//host/path?query#fragment";

        UriReferenceSplitResult hierarchical = UriParserCore.SplitHierarchicalPart(text.AsSpan(), 6);
        UriReferenceSplitResult relative = UriParserCore.SplitRelativePart(text.AsSpan(), 6);

        Assert.Multiple(() =>
        {
            Assert.That(relative.HasAuthority, Is.EqualTo(hierarchical.HasAuthority));
            Assert.That(relative.AuthorityOffset, Is.EqualTo(hierarchical.AuthorityOffset));
            Assert.That(relative.AuthorityLength, Is.EqualTo(hierarchical.AuthorityLength));
            Assert.That(relative.PathOffset, Is.EqualTo(hierarchical.PathOffset));
            Assert.That(relative.PathLength, Is.EqualTo(hierarchical.PathLength));
            Assert.That(relative.QueryOffset, Is.EqualTo(hierarchical.QueryOffset));
            Assert.That(relative.QueryLength, Is.EqualTo(hierarchical.QueryLength));
            Assert.That(relative.FragmentOffset, Is.EqualTo(hierarchical.FragmentOffset));
            Assert.That(relative.FragmentLength, Is.EqualTo(hierarchical.FragmentLength));
        });
    }

    [Test]
    public void InvalidPartOffsetsReturnDeterministicFailuresWithoutIndexingInput()
    {
        UriReferenceSplitResult negative = UriParserCore.SplitRelativePart("abc".AsSpan(), -1);
        UriReferenceSplitResult beyond = UriParserCore.SplitHierarchicalPart("abc".AsSpan(), 4);

        AssertInvalidOffset(negative);
        AssertInvalidOffset(beyond);
    }

    private static void AssertParts(
        string text,
        UriReferenceSplitResult result,
        string authority,
        string path,
        string query,
        string fragment)
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                text.Substring(result.AuthorityOffset, result.AuthorityLength),
                Is.EqualTo(authority));
            Assert.That(
                text.Substring(result.PathOffset, result.PathLength),
                Is.EqualTo(path));
            Assert.That(
                text.Substring(result.QueryOffset, result.QueryLength),
                Is.EqualTo(query));
            Assert.That(
                text.Substring(result.FragmentOffset, result.FragmentLength),
                Is.EqualTo(fragment));
        });
    }

    private static void AssertInvalidOffset(UriReferenceSplitResult result)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.HasAuthority, Is.False);
            Assert.That(result.AuthorityLength, Is.Zero);
            Assert.That(result.PathLength, Is.Zero);
            Assert.That(result.HasQuery, Is.False);
            Assert.That(result.QueryLength, Is.Zero);
            Assert.That(result.HasFragment, Is.False);
            Assert.That(result.FragmentLength, Is.Zero);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
        });
    }
}
