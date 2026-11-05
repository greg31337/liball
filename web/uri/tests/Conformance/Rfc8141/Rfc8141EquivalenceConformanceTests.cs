namespace uri.Tests;

public class Rfc8141EquivalenceConformanceTests
{
    [TestCase("URN:example:a123,z456")]
    [TestCase("urn:EXAMPLE:a123,z456")]
    [TestCase("URN:EXAMPLE:a123,z456")]
    public void SectionThreeSchemeAndNidSpellingsAreEquivalent(
        string candidate)
    {
        AssertAssignedNameComparison(
            "urn:example:a123,z456",
            candidate,
            true,
            UriComponent.None);
    }

    [TestCase("urn:example:a123,z456?+abc")]
    [TestCase("urn:example:a123,z456?=xyz")]
    [TestCase("urn:example:a123,z456#789")]
    [TestCase("urn:example:a123,z456?+abc?=xyz#789")]
    [TestCase("URN:EXAMPLE:a123,z456?+different?=other#fragment")]
    public void SectionThreeRqfComponentsDoNotAffectEquivalence(
        string candidate)
    {
        AssertAssignedNameComparison(
            "urn:example:a123,z456",
            candidate,
            true,
            UriComponent.None);
    }

    [TestCase("urn:example:a123,z456/foo")]
    [TestCase("urn:example:a123,z456/bar")]
    [TestCase("urn:example:a123,z456/baz")]
    public void SectionThreeNssSlashSuffixesRemainSignificant(
        string candidate)
    {
        AssertAssignedNameComparison(
            "urn:example:a123,z456",
            candidate,
            false,
            UriComponent.UrnNamespaceSpecificString);
    }

    [TestCase("urn:example:a123,z456/foo", "urn:example:a123,z456/bar")]
    [TestCase("urn:example:a123,z456/bar", "urn:example:a123,z456/baz")]
    [TestCase("urn:example:a123,z456/foo", "urn:example:a123,z456/baz")]
    public void SectionThreeDifferentSlashSuffixesAreNotEquivalent(
        string left,
        string right)
    {
        AssertAssignedNameComparison(
            left,
            right,
            false,
            UriComponent.UrnNamespaceSpecificString);
    }

    [TestCase("URN:EXAMPLE:a123%2cz456")]
    [TestCase("urn:example:a123%2cZ456")]
    [TestCase("UrN:ExAmPlE:a123%2Cz456")]
    public void SectionThreePercentHexCaseIsNormalizedForComparison(
        string candidate)
    {
        bool expected = candidate != "urn:example:a123%2cZ456";

        AssertAssignedNameComparison(
            "urn:example:a123%2Cz456",
            candidate,
            expected,
            expected ? UriComponent.None :
                UriComponent.UrnNamespaceSpecificString);
    }

    [TestCase("urn:example:a123%2Cz456", "urn:example:a123,z456")]
    [TestCase("urn:example:%41", "urn:example:A")]
    [TestCase("urn:example:%7E", "urn:example:~")]
    [TestCase("urn:example:a%2Fb", "urn:example:a/b")]
    public void PercentEncodedOctetsAreNeverDecodedForEquivalence(
        string left,
        string right)
    {
        AssertAssignedNameComparison(
            left,
            right,
            false,
            UriComponent.UrnNamespaceSpecificString);
    }

    [TestCase("urn:example:abc", "urn:example:ABC")]
    [TestCase("urn:example:aBc", "urn:example:abc")]
    [TestCase("urn:example:path/Part", "urn:example:path/part")]
    public void OrdinaryNssCharactersRemainCaseSensitive(
        string left,
        string right)
    {
        AssertAssignedNameComparison(
            left,
            right,
            false,
            UriComponent.UrnNamespaceSpecificString);
    }

    [TestCase(
        "URN:example:a",
        "urn:example:a",
        UriComponent.Scheme)]
    [TestCase(
        "urn:EXAMPLE:a",
        "urn:example:a",
        UriComponent.UrnNamespaceIdentifier)]
    [TestCase(
        "urn:example:a%2f",
        "urn:example:a%2F",
        UriComponent.UrnNamespaceSpecificString)]
    [TestCase(
        "urn:example:a?+r",
        "urn:example:a",
        UriComponent.UrnRComponent)]
    [TestCase(
        "urn:example:a?=q",
        "urn:example:a",
        UriComponent.UrnQComponent)]
    [TestCase(
        "urn:example:a#f",
        "urn:example:a",
        UriComponent.UrnFComponent)]
    public void ExactComparisonRetainsDistinctionsIgnoredByUrnEquivalence(
        string left,
        string right,
        UriComponent exactDifference)
    {
        IUrnComparer comparer = new UrnComparer();
        UrnParseOptions options = new UrnParseOptions();

        UrnEquivalenceResult exact = comparer.CompareUrnExact(
            left,
            right,
            options);
        UrnEquivalenceResult assigned =
            comparer.CompareUrnAssignedName(left, right, options);

        Assert.Multiple(() =>
        {
            AssertComparison(
                exact,
                false,
                UrnEquivalenceLevel.Exact,
                exactDifference);
            AssertComparison(
                assigned,
                true,
                UrnEquivalenceLevel.AssignedName,
                UriComponent.None);
        });
    }

    [Test]
    public void NamespaceRulesCanEliminateGenericFalseNegatives()
    {
        ExtensionNamespaceHandler handler =
            new ExtensionNamespaceHandler();
        UrnParseOptions options = new UrnParseOptions();
        options.NamespaceHandler = handler;
        IUrnComparer comparer = new UrnComparer();

        UrnEquivalenceResult generic =
            comparer.CompareUrnAssignedName(
                "urn:example:Ab-C",
                "urn:example:abc",
                options);
        UrnEquivalenceResult extended =
            comparer.CompareUrnNamespaceSpecific(
                "urn:example:Ab-C",
                "urn:example:abc",
                options);

        Assert.Multiple(() =>
        {
            AssertComparison(
                generic,
                false,
                UrnEquivalenceLevel.AssignedName,
                UriComponent.UrnNamespaceSpecificString);
            AssertComparison(
                extended,
                true,
                UrnEquivalenceLevel.NamespaceSpecific,
                UriComponent.None);
            Assert.That(handler.ComparisonCount, Is.EqualTo(1));
            Assert.That(handler.NamespaceIdentifier, Is.EqualTo("example"));
            Assert.That(handler.LeftValue, Is.EqualTo("Ab-C"));
            Assert.That(handler.RightValue, Is.EqualTo("abc"));
        });
    }

    [Test]
    public void NamespaceRulesCannotOverturnGenericEquivalence()
    {
        ExtensionNamespaceHandler handler =
            new ExtensionNamespaceHandler();
        handler.ForceDifferent = true;
        UrnParseOptions options = new UrnParseOptions();
        options.NamespaceHandler = handler;
        IUrnComparer comparer = new UrnComparer();

        UrnEquivalenceResult result =
            comparer.CompareUrnNamespaceSpecific(
                "URN:EXAMPLE:a%2f?+r",
                "urn:example:a%2F?=q#f",
                options);

        Assert.Multiple(() =>
        {
            AssertComparison(
                result,
                true,
                UrnEquivalenceLevel.NamespaceSpecific,
                UriComponent.None);
            Assert.That(handler.ComparisonCount, Is.Zero);
        });
    }

    [Test]
    public void NamespaceRulesDoNotCompareAcrossDifferentNids()
    {
        ExtensionNamespaceHandler handler =
            new ExtensionNamespaceHandler();
        UrnParseOptions options = new UrnParseOptions();
        options.NamespaceHandler = handler;
        IUrnComparer comparer = new UrnComparer();

        UrnEquivalenceResult result =
            comparer.CompareUrnNamespaceSpecific(
                "urn:example:Ab-C",
                "urn:other:abc",
                options);

        Assert.Multiple(() =>
        {
            AssertComparison(
                result,
                false,
                UrnEquivalenceLevel.NamespaceSpecific,
                UriComponent.UrnNamespaceIdentifier);
            Assert.That(handler.ComparisonCount, Is.Zero);
        });
    }

    [Test]
    public void NamespaceSpecificModeRequiresAnExplicitHandler()
    {
        IUrnComparer comparer = new UrnComparer();

        UrnEquivalenceResult result =
            comparer.CompareUrnNamespaceSpecific(
                "urn:example:a",
                "urn:example:A",
                new UrnParseOptions());

        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Equal, Is.False);
            Assert.That(result.Level, Is.EqualTo(
                UrnEquivalenceLevel.NamespaceSpecific));
            Assert.That(result.DifferentComponent, Is.EqualTo(
                UriComponent.None));
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
                DiagnosticDomain.Urn));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.UnsupportedNamespace));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(
                UriComponent.UrnNamespaceSpecificString));
        });
    }

    private static void AssertAssignedNameComparison(
        string left,
        string right,
        bool equal,
        UriComponent difference)
    {
        IUrnComparer comparer = new UrnComparer();

        UrnEquivalenceResult result =
            comparer.CompareUrnAssignedName(
                left,
                right,
                new UrnParseOptions());

        AssertComparison(
            result,
            equal,
            UrnEquivalenceLevel.AssignedName,
            difference);
    }

    private static void AssertComparison(
        UrnEquivalenceResult result,
        bool equal,
        UrnEquivalenceLevel level,
        UriComponent difference)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
            Assert.That(result.Equal, Is.EqualTo(equal));
            Assert.That(result.Level, Is.EqualTo(level));
            Assert.That(result.DifferentComponent, Is.EqualTo(difference));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private class ExtensionNamespaceHandler : IUrnNamespaceHandler
    {
        public bool ForceDifferent;
        public int ComparisonCount;
        public string NamespaceIdentifier;
        public string LeftValue;
        public string RightValue;

        public ExtensionNamespaceHandler()
        {
            ForceDifferent = false;
            ComparisonCount = 0;
            NamespaceIdentifier = "";
            LeftValue = "";
            RightValue = "";
        }

        public UriDiagnostic? ValidateNamespaceSpecificString(
            string namespaceIdentifier,
            string namespaceSpecificString)
        {
            return null;
        }

        public UriTextResult NormalizeNamespaceSpecificString(
            string namespaceIdentifier,
            string namespaceSpecificString)
        {
            return new UriTextResult(
                true,
                namespaceSpecificString,
                namespaceSpecificString.Length,
                null);
        }

        public UrnEquivalenceResult CompareNamespaceSpecificStrings(
            string namespaceIdentifier,
            string leftNamespaceSpecificString,
            string rightNamespaceSpecificString)
        {
            ComparisonCount = ComparisonCount + 1;
            NamespaceIdentifier = namespaceIdentifier;
            LeftValue = leftNamespaceSpecificString;
            RightValue = rightNamespaceSpecificString;
            string left = leftNamespaceSpecificString.Replace("-", "");
            string right = rightNamespaceSpecificString.Replace("-", "");
            bool equal = string.Equals(
                left,
                right,
                System.StringComparison.OrdinalIgnoreCase);
            if (ForceDifferent)
            {
                equal = false;
            }

            UriComponent difference =
                UriComponent.UrnNamespaceSpecificString;
            if (equal)
            {
                difference = UriComponent.None;
            }

            return new UrnEquivalenceResult(
                true,
                equal,
                UrnEquivalenceLevel.NamespaceSpecific,
                difference,
                null);
        }
    }
}
