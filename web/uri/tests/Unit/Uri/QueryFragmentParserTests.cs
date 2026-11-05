namespace uri.Tests;

public class QueryFragmentParserTests
{
    [TestCase("", 0, false, "", false, "")]
    [TestCase("prefix", 6, false, "", false, "")]
    [TestCase("?", 0, true, "", false, "")]
    [TestCase("#", 0, false, "", true, "")]
    [TestCase("?#", 0, true, "", true, "")]
    [TestCase("?query", 0, true, "query", false, "")]
    [TestCase("#fragment", 0, false, "", true, "fragment")]
    [TestCase("?query#fragment", 0, true, "query", true, "fragment")]
    [TestCase("path?one?two#frag?still", 4, true, "one?two", true, "frag?still")]
    public void ScannersPreserveAbsentAndPresentEmptyStates(
        string text,
        int startOffset,
        bool hasQuery,
        string query,
        bool hasFragment,
        string fragment)
    {
        QueryFragmentScanResult queryResult = QueryFragmentParser.ScanQuery(
            text.AsSpan(),
            startOffset,
            30);
        QueryFragmentScanResult fragmentResult = QueryFragmentParser.ScanFragment(
            text.AsSpan(),
            queryResult.NextOffset,
            30);

        Assert.Multiple(() =>
        {
            Assert.That(queryResult.Success, Is.True);
            Assert.That(queryResult.Present, Is.EqualTo(hasQuery));
            Assert.That(
                text.Substring(queryResult.ValueOffset, queryResult.ValueLength),
                Is.EqualTo(query));
            Assert.That(fragmentResult.Success, Is.True);
            Assert.That(fragmentResult.Present, Is.EqualTo(hasFragment));
            Assert.That(
                text.Substring(fragmentResult.ValueOffset, fragmentResult.ValueLength),
                Is.EqualTo(fragment));
            Assert.That(fragmentResult.NextOffset, Is.EqualTo(text.Length));
        });
    }

    [Test]
    public void EncodedDelimitersRemainDataWhileRawDelimitersKeepTheirGrammarRoles()
    {
        string text = "?a%3Fb/%23c?d#f%23g/%3Fh?i";

        QueryFragmentScanResult query = QueryFragmentParser.ScanQuery(text.AsSpan(), 0, 0);
        QueryFragmentScanResult fragment = QueryFragmentParser.ScanFragment(
            text.AsSpan(),
            query.NextOffset,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(query.Success, Is.True);
            Assert.That(text.Substring(query.ValueOffset, query.ValueLength), Is.EqualTo("a%3Fb/%23c?d"));
            Assert.That(fragment.Success, Is.True);
            Assert.That(text.Substring(fragment.ValueOffset, fragment.ValueLength), Is.EqualTo("f%23g/%3Fh?i"));
        });
    }

    [Test]
    public void EveryAllowedRawCharacterIsAcceptedByBothComponents()
    {
        string allowed =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~!$&'()*+,;=:@/?";
        string text = "?" + allowed + "#" + allowed;

        QueryFragmentScanResult query = QueryFragmentParser.ScanQuery(text.AsSpan(), 0, 0);
        QueryFragmentScanResult fragment = QueryFragmentParser.ScanFragment(
            text.AsSpan(),
            query.NextOffset,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(query.Success, Is.True);
            Assert.That(query.ValueLength, Is.EqualTo(allowed.Length));
            Assert.That(fragment.Success, Is.True);
            Assert.That(fragment.ValueLength, Is.EqualTo(allowed.Length));
        });
    }

    [Test]
    public void EveryPercentEncodedByteIsAcceptedWithoutCharacterSetAssumptions()
    {
        int value = 0;
        while (value <= 255)
        {
            string triplet = "%" + value.ToString("X2");
            string text = "?" + triplet + "#" + triplet;

            QueryFragmentScanResult query = QueryFragmentParser.ScanQuery(text.AsSpan(), 0, 0);
            QueryFragmentScanResult fragment = QueryFragmentParser.ScanFragment(
                text.AsSpan(),
                query.NextOffset,
                0);

            Assert.That(query.Success, Is.True, triplet);
            Assert.That(fragment.Success, Is.True, triplet);
            value = value + 1;
        }
    }

    [TestCase("?%", 11)]
    [TestCase("?%0", 11)]
    [TestCase("?%G0", 12)]
    [TestCase("?%0G", 13)]
    [TestCase("?ok%G0", 14)]
    [TestCase("?%#fragment", 11)]
    [TestCase("?%A#fragment", 11)]
    public void MalformedQueryTripletsStayBoundedByTheFragmentDelimiter(
        string text,
        int expectedOffset)
    {
        QueryFragmentScanResult result = QueryFragmentParser.ScanQuery(
            text.AsSpan(),
            0,
            10);

        AssertPercentFailure(result, UriComponent.Query, expectedOffset);
    }

    [TestCase("#%", 11)]
    [TestCase("#%0", 11)]
    [TestCase("#%G0", 12)]
    [TestCase("#%0G", 13)]
    [TestCase("#ok%0G", 15)]
    public void MalformedFragmentTripletsReturnExactDiagnostics(
        string text,
        int expectedOffset)
    {
        QueryFragmentScanResult result = QueryFragmentParser.ScanFragment(
            text.AsSpan(),
            0,
            10);

        AssertPercentFailure(result, UriComponent.Fragment, expectedOffset);
    }

    [Test]
    public void NonGrammarCharactersAreRejectedInQueryAndFragment()
    {
        string invalidQuery = "[] \\`\"<>\u0000\u007f\u0080\u00e9";
        string invalidFragment = "#[] \\`\"<>\u0000\u007f\u0080\u00e9";
        int index = 0;
        while (index < invalidQuery.Length)
        {
            QueryFragmentScanResult query = QueryFragmentParser.ScanQuery(
                ("?a" + invalidQuery[index]).AsSpan(),
                0,
                20);
            AssertComponentFailure(
                query,
                UriDiagnosticCode.InvalidQuery,
                UriComponent.Query,
                22,
                DiagnosticFactory.InvalidQueryMessage);
            index = index + 1;
        }

        index = 0;
        while (index < invalidFragment.Length)
        {
            QueryFragmentScanResult fragment = QueryFragmentParser.ScanFragment(
                ("#a" + invalidFragment[index]).AsSpan(),
                0,
                40);
            AssertComponentFailure(
                fragment,
                UriDiagnosticCode.InvalidFragment,
                UriComponent.Fragment,
                42,
                DiagnosticFactory.InvalidFragmentMessage);
            index = index + 1;
        }
    }

    [Test]
    public void InvalidStartOffsetsFailWithoutIndexingInput()
    {
        QueryFragmentScanResult negative = QueryFragmentParser.ScanQuery(
            "?q".AsSpan(),
            -1,
            0);
        QueryFragmentScanResult beyond = QueryFragmentParser.ScanFragment(
            "#f".AsSpan(),
            3,
            0);

        Assert.Multiple(() =>
        {
            AssertInvalidInput(negative);
            AssertInvalidInput(beyond);
        });
    }

    [Test]
    public void FailureOffsetsUseRelativePositionsAndSaturateOverflow()
    {
        QueryFragmentScanResult relative = QueryFragmentParser.ScanQuery(
            "?a[".AsSpan(),
            0,
            -1);
        QueryFragmentScanResult saturated = QueryFragmentParser.ScanFragment(
            "#a[".AsSpan(),
            0,
            int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(relative.Diagnostic, Is.Not.Null);
            Assert.That(relative.Diagnostic!.Range.Offset, Is.EqualTo(2));
            Assert.That(saturated.Diagnostic, Is.Not.Null);
            Assert.That(saturated.Diagnostic!.Range.Offset, Is.EqualTo(int.MaxValue));
        });
    }

    private static void AssertPercentFailure(
        QueryFragmentScanResult result,
        UriComponent component,
        int expectedOffset)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Present, Is.False);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(UriDiagnosticCode.InvalidPercentEncoding));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(expectedOffset));
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(1));
            Assert.That(result.Diagnostic.Message, Is.EqualTo(DiagnosticFactory.InvalidPercentEncodingMessage));
        });
    }

    private static void AssertComponentFailure(
        QueryFragmentScanResult result,
        UriDiagnosticCode code,
        UriComponent component,
        int expectedOffset,
        string expectedMessage)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Present, Is.False);
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(expectedOffset));
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(1));
            Assert.That(result.Diagnostic.Message, Is.EqualTo(expectedMessage));
        });
    }

    private static void AssertInvalidInput(QueryFragmentScanResult result)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Present, Is.False);
        Assert.That(result.ValueOffset, Is.Zero);
        Assert.That(result.ValueLength, Is.Zero);
        Assert.That(result.NextOffset, Is.Zero);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic!.Code, Is.EqualTo(UriDiagnosticCode.InvalidInput));
    }
}
