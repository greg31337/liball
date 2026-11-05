namespace uri.Tests;

public class QueryCodecCoreOperationTests
{
    [Test]
    public void AddAppendsOneParameterWithoutChangingTheInputOrder()
    {
        UriQueryParameterValue first = Parameter("a", true, "1");
        UriQueryParameterValue second = Parameter("a", true, "2");
        UriQueryParameterValue added = Parameter("b", false, "ignored");
        UriQueryParameterValue[] original = new UriQueryParameterValue[]
        {
            first,
            second
        };

        UriQueryParameterValue[] result = QueryCodecCore.AddQueryParameter(
            original,
            added);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result[0], Is.SameAs(first));
            Assert.That(result[1], Is.SameAs(second));
            Assert.That(result[2], Is.SameAs(added));
            Assert.That(original, Has.Length.EqualTo(2));
            Assert.That(result, Is.Not.SameAs(original));
        });
    }

    [Test]
    public void ReplaceRemovesEveryOrdinalNameMatchAtTheFirstMatchPosition()
    {
        UriQueryParameterValue upperCase = Parameter("A", true, "upper");
        UriQueryParameterValue replacementOne = Parameter("a", true, "new-1");
        UriQueryParameterValue replacementTwo = Parameter("a", true, "new-2");
        UriQueryParameterValue[] original = new UriQueryParameterValue[]
        {
            Parameter("head", true, "0"),
            Parameter("a", true, "old-1"),
            upperCase,
            Parameter("a", true, "old-2"),
            Parameter("tail", false, "")
        };

        UriQueryParameterValue[] result = QueryCodecCore.ReplaceQueryParameters(
            original,
            "a",
            new UriQueryParameterValue[] { replacementOne, replacementTwo });

        Assert.Multiple(() =>
        {
            AssertNames(result, new string[] { "head", "a", "a", "A", "tail" });
            Assert.That(result[1], Is.SameAs(replacementOne));
            Assert.That(result[2], Is.SameAs(replacementTwo));
            Assert.That(result[3], Is.SameAs(upperCase));
            Assert.That(original[1].Value, Is.EqualTo("old-1"));
            Assert.That(original[3].Value, Is.EqualTo("old-2"));
        });
    }

    [Test]
    public void ReplaceAppendsReplacementsWhenTheNameIsAbsent()
    {
        UriQueryParameterValue appended = Parameter("missing", true, "value");
        UriQueryParameterValue[] original = new UriQueryParameterValue[]
        {
            Parameter("a", true, "1"),
            Parameter("b", true, "2")
        };

        UriQueryParameterValue[] result = QueryCodecCore.ReplaceQueryParameters(
            original,
            "missing",
            new UriQueryParameterValue[] { appended });

        Assert.Multiple(() =>
        {
            AssertNames(result, new string[] { "a", "b", "missing" });
            Assert.That(result[2], Is.SameAs(appended));
        });
    }

    [Test]
    public void RemoveDeletesAllAndOnlyOrdinalNameMatches()
    {
        UriQueryParameterValue upperCase = Parameter("A", true, "keep");
        UriQueryParameterValue[] original = new UriQueryParameterValue[]
        {
            Parameter("a", true, "1"),
            upperCase,
            Parameter("b", true, "2"),
            Parameter("a", false, ""),
            Parameter("c", true, "3")
        };

        UriQueryParameterValue[] result = QueryCodecCore.RemoveQueryParameters(
            original,
            "a");

        Assert.Multiple(() =>
        {
            AssertNames(result, new string[] { "A", "b", "c" });
            Assert.That(result[0], Is.SameAs(upperCase));
            Assert.That(original, Has.Length.EqualTo(5));
        });
    }

    [Test]
    public void OrdinalSortUsesNameValueStateAndValueAndRemainsStable()
    {
        UriQueryParameterValue firstTwo = Parameter("a", true, "2");
        UriQueryParameterValue secondTwo = Parameter("a", true, "2");
        UriQueryParameterValue[] original = new UriQueryParameterValue[]
        {
            Parameter("b", true, "2"),
            Parameter("A", true, "z"),
            firstTwo,
            Parameter("a", false, "ignored"),
            Parameter("a", true, ""),
            Parameter("a", true, "1"),
            secondTwo
        };

        UriQueryParameterValue[] result = QueryCodecCore.SortQueryParameters(original);

        Assert.Multiple(() =>
        {
            AssertParameter(result[0], "A", true, "z");
            AssertParameter(result[1], "a", false, "ignored");
            AssertParameter(result[2], "a", true, "");
            AssertParameter(result[3], "a", true, "1");
            Assert.That(result[4], Is.SameAs(firstTwo));
            Assert.That(result[5], Is.SameAs(secondTwo));
            AssertParameter(result[6], "b", true, "2");
            Assert.That(original[0].Name, Is.EqualTo("b"));
            Assert.That(result, Is.Not.SameAs(original));
        });
    }

    [Test]
    public void DeduplicateKeepsTheFirstExactSemanticPair()
    {
        UriQueryParameterValue firstMissing = Parameter("a", false, "ignored-one");
        UriQueryParameterValue firstValue = Parameter("a", true, "1");
        UriQueryParameterValue[] original = new UriQueryParameterValue[]
        {
            firstMissing,
            Parameter("a", false, "ignored-two"),
            Parameter("a", true, ""),
            firstValue,
            Parameter("A", true, "1"),
            Parameter("a", true, "1"),
            Parameter("a", true, "2"),
            Parameter("a", true, "")
        };

        UriQueryParameterValue[] result = QueryCodecCore.DeduplicateQueryParameters(
            original);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Length.EqualTo(5));
            Assert.That(result[0], Is.SameAs(firstMissing));
            AssertParameter(result[1], "a", true, "");
            Assert.That(result[2], Is.SameAs(firstValue));
            AssertParameter(result[3], "A", true, "1");
            AssertParameter(result[4], "a", true, "2");
            Assert.That(original, Has.Length.EqualTo(8));
        });
    }

    [Test]
    public void EmptyCollectionsProduceIndependentEmptyResults()
    {
        UriQueryParameterValue[] empty = new UriQueryParameterValue[0];
        UriQueryParameterValue[] removed = QueryCodecCore.RemoveQueryParameters(
            empty,
            "a");
        UriQueryParameterValue[] replaced = QueryCodecCore.ReplaceQueryParameters(
            empty,
            "a",
            new UriQueryParameterValue[0]);
        UriQueryParameterValue[] sorted = QueryCodecCore.SortQueryParameters(empty);
        UriQueryParameterValue[] deduplicated = QueryCodecCore.DeduplicateQueryParameters(
            empty);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.Empty);
            Assert.That(replaced, Is.Empty);
            Assert.That(sorted, Is.Empty);
            Assert.That(deduplicated, Is.Empty);
            Assert.That(removed, Is.Not.SameAs(empty));
            Assert.That(replaced, Is.Not.SameAs(empty));
            Assert.That(sorted, Is.Not.SameAs(empty));
            Assert.That(deduplicated, Is.Not.SameAs(empty));
        });
    }

    [Test]
    public void ExplicitOperationsIntegrateWithParseAndCompose()
    {
        UriQueryOptions options = new UriQueryOptions();
        UriQueryParseCoreResult parsed = QueryCodecCore.ParseQuery(
            "b=2&a=2&a=1&a=1&flag&flag=&A=0",
            options);
        UriQueryParameterValue[] unique = QueryCodecCore.DeduplicateQueryParameters(
            parsed.Parameters);
        UriQueryParameterValue[] sorted = QueryCodecCore.SortQueryParameters(unique);
        UriQueryComposeCoreResult composed = QueryCodecCore.ComposeQuery(
            sorted,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True, parsed.Diagnostic?.Message);
            Assert.That(composed.Success, Is.True, composed.Diagnostic?.Message);
            Assert.That(composed.Text, Is.EqualTo("A=0&a=1&a=2&b=2&flag&flag="));
        });
    }

    private static UriQueryParameterValue Parameter(
        string name,
        bool hasValue,
        string value)
    {
        return new UriQueryParameterValue(name, hasValue, value);
    }

    private static void AssertNames(
        UriQueryParameterValue[] parameters,
        string[] names)
    {
        Assert.That(parameters, Has.Length.EqualTo(names.Length));
        int offset = 0;
        while (offset < names.Length)
        {
            Assert.That(parameters[offset].Name, Is.EqualTo(names[offset]));
            offset = offset + 1;
        }
    }

    private static void AssertParameter(
        UriQueryParameterValue parameter,
        string name,
        bool hasValue,
        string value)
    {
        Assert.That(parameter.Name, Is.EqualTo(name));
        Assert.That(parameter.HasValue, Is.EqualTo(hasValue));
        Assert.That(parameter.Value, Is.EqualTo(value));
    }
}
