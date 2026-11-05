namespace uri.Tests;

public class UrnComparerCoreTests
{
    [Test]
    public void ExactComparisonUsesSerializedComponentState()
    {
        Urn left = ParseUrn("URN:Example:a%2f?+r?=q#");
        Urn right = ParseUrn("URN:Example:a%2f?+r?=q#");
        right.AssignedName = "stale-assigned-name";
        right.OriginalText = "stale-original-text";

        UrnEquivalenceResult result = UrnComparerCore.CompareExact(
            left,
            right);

        AssertEquivalent(result, UrnEquivalenceLevel.Exact);
    }

    [TestCase(
        "URN:example:a",
        "urn:example:a",
        UriComponent.Scheme)]
    [TestCase(
        "urn:Example:a",
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
        "urn:example:a?=other",
        UriComponent.UrnQComponent)]
    [TestCase(
        "urn:example:a#",
        "urn:example:a",
        UriComponent.UrnFComponent)]
    public void ExactComparisonReportsTheFirstSerializedDifference(
        string leftText,
        string rightText,
        UriComponent component)
    {
        UrnEquivalenceResult result = UrnComparerCore.CompareExact(
            ParseUrn(leftText),
            ParseUrn(rightText));

        AssertDifferent(result, UrnEquivalenceLevel.Exact, component);
    }

    [TestCase("URN:EXAMPLE:a", "urn:example:a")]
    [TestCase("urn:example:a%2f%aB", "URN:EXAMPLE:a%2F%Ab")]
    [TestCase(
        "urn:example:a?+resolver?=query#fragment",
        "URN:EXAMPLE:a?+other?=different#")]
    public void AssignedNameComparisonAppliesGenericUrnEquivalence(
        string leftText,
        string rightText)
    {
        UrnEquivalenceResult result =
            UrnComparerCore.CompareAssignedName(
                ParseUrn(leftText),
                ParseUrn(rightText));

        AssertEquivalent(result, UrnEquivalenceLevel.AssignedName);
    }

    [TestCase(
        "urn:example:a",
        "urn:other:a",
        UriComponent.UrnNamespaceIdentifier)]
    [TestCase(
        "urn:example:a",
        "urn:example:A",
        UriComponent.UrnNamespaceSpecificString)]
    [TestCase(
        "urn:example:%41",
        "urn:example:A",
        UriComponent.UrnNamespaceSpecificString)]
    [TestCase(
        "urn:example:a/path",
        "urn:example:a/other",
        UriComponent.UrnNamespaceSpecificString)]
    public void AssignedNameComparisonPreservesSemanticDistinctions(
        string leftText,
        string rightText,
        UriComponent component)
    {
        UrnEquivalenceResult result =
            UrnComparerCore.CompareAssignedName(
                ParseUrn(leftText),
                ParseUrn(rightText));

        AssertDifferent(
            result,
            UrnEquivalenceLevel.AssignedName,
            component);
    }

    [Test]
    public void NamespaceSpecificComparisonRequiresAHandler()
    {
        UrnEquivalenceResult result =
            UrnComparerCore.CompareNamespaceSpecific(
                ParseUrn("urn:example:a"),
                ParseUrn("urn:example:A"),
                null);

        AssertFailure(
            result,
            UrnEquivalenceLevel.NamespaceSpecific,
            UriDiagnosticCode.UnsupportedNamespace,
            UriComponent.UrnNamespaceSpecificString);
    }

    [Test]
    public void NamespaceHandlerCanEliminateGenericFalseNegatives()
    {
        RecordingNamespaceHandler handler =
            new RecordingNamespaceHandler();
        handler.ComparisonEqual = true;

        UrnEquivalenceResult result =
            UrnComparerCore.CompareNamespaceSpecific(
                ParseUrn("URN:ExAmPlE:Ab-C"),
                ParseUrn("urn:example:abc"),
                handler);

        Assert.Multiple(() =>
        {
            AssertEquivalent(
                result,
                UrnEquivalenceLevel.NamespaceSpecific);
            Assert.That(handler.ComparisonCount, Is.EqualTo(1));
            Assert.That(handler.NamespaceIdentifier, Is.EqualTo("example"));
            Assert.That(handler.LeftValue, Is.EqualTo("Ab-C"));
            Assert.That(handler.RightValue, Is.EqualTo("abc"));
        });
    }

    [Test]
    public void NamespaceHandlerCanRetainAnNssDifference()
    {
        RecordingNamespaceHandler handler =
            new RecordingNamespaceHandler();
        handler.ComparisonEqual = false;

        UrnEquivalenceResult result =
            UrnComparerCore.CompareNamespaceSpecific(
                ParseUrn("urn:example:a"),
                ParseUrn("urn:example:b"),
                handler);

        Assert.Multiple(() =>
        {
            AssertDifferent(
                result,
                UrnEquivalenceLevel.NamespaceSpecific,
                UriComponent.UrnNamespaceSpecificString);
            Assert.That(handler.ComparisonCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void NamespaceHandlerCannotOverturnGenericEquivalence()
    {
        RecordingNamespaceHandler handler =
            new RecordingNamespaceHandler();
        handler.ComparisonEqual = false;

        UrnEquivalenceResult result =
            UrnComparerCore.CompareNamespaceSpecific(
                ParseUrn("URN:EXAMPLE:a%2f?+r"),
                ParseUrn("urn:example:a%2F?=q#f"),
                handler);

        Assert.Multiple(() =>
        {
            AssertEquivalent(
                result,
                UrnEquivalenceLevel.NamespaceSpecific);
            Assert.That(handler.ComparisonCount, Is.Zero);
        });
    }

    [Test]
    public void NamespaceHandlerIsNotUsedAcrossDifferentNamespaces()
    {
        RecordingNamespaceHandler handler =
            new RecordingNamespaceHandler();
        handler.ComparisonEqual = true;

        UrnEquivalenceResult result =
            UrnComparerCore.CompareNamespaceSpecific(
                ParseUrn("urn:example:a"),
                ParseUrn("urn:other:a"),
                handler);

        Assert.Multiple(() =>
        {
            AssertDifferent(
                result,
                UrnEquivalenceLevel.NamespaceSpecific,
                UriComponent.UrnNamespaceIdentifier);
            Assert.That(handler.ComparisonCount, Is.Zero);
        });
    }

    [Test]
    public void NamespaceHandlerFailuresArePropagatedAtTheSelectedLevel()
    {
        RecordingNamespaceHandler handler =
            new RecordingNamespaceHandler();
        handler.ComparisonDiagnostic = CreateDiagnostic(
            UriDiagnosticCode.UnsupportedNamespace,
            UriComponent.UrnNamespaceSpecificString);

        UrnEquivalenceResult result =
            UrnComparerCore.CompareNamespaceSpecific(
                ParseUrn("urn:example:a"),
                ParseUrn("urn:example:b"),
                handler);

        Assert.Multiple(() =>
        {
            AssertFailure(
                result,
                UrnEquivalenceLevel.NamespaceSpecific,
                UriDiagnosticCode.UnsupportedNamespace,
                UriComponent.UrnNamespaceSpecificString);
            Assert.That(result.Diagnostic,
                Is.SameAs(handler.ComparisonDiagnostic));
        });
    }

    [Test]
    public void NullInputsFailCleanlyAtEveryLevel()
    {
        Urn urn = ParseUrn("urn:example:a");
        RecordingNamespaceHandler handler =
            new RecordingNamespaceHandler();

        UrnEquivalenceResult exact = UrnComparerCore.CompareExact(
            null,
            urn);
        UrnEquivalenceResult assigned =
            UrnComparerCore.CompareAssignedName(urn, null);
        UrnEquivalenceResult specific =
            UrnComparerCore.CompareNamespaceSpecific(
                null,
                urn,
                handler);

        Assert.Multiple(() =>
        {
            AssertFailure(
                exact,
                UrnEquivalenceLevel.Exact,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Input);
            AssertFailure(
                assigned,
                UrnEquivalenceLevel.AssignedName,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Input);
            AssertFailure(
                specific,
                UrnEquivalenceLevel.NamespaceSpecific,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Input);
        });
    }

    private static Urn ParseUrn(string text)
    {
        UrnParseResult result = UrnParserCore.ParseUrn(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);
        Assert.That(result.Success, Is.True);
        return result.Urn;
    }

    private static UriDiagnostic CreateDiagnostic(
        UriDiagnosticCode code,
        UriComponent component)
    {
        return new UriDiagnostic(
            DiagnosticDomain.Urn,
            code,
            DiagnosticSeverity.Error,
            component,
            new TextRange(0, 0),
            "Namespace comparison failed.");
    }

    private static void AssertEquivalent(
        UrnEquivalenceResult result,
        UrnEquivalenceLevel level)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Equal, Is.True);
            Assert.That(result.Level, Is.EqualTo(level));
            Assert.That(result.DifferentComponent, Is.EqualTo(
                UriComponent.None));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertDifferent(
        UrnEquivalenceResult result,
        UrnEquivalenceLevel level,
        UriComponent component)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Equal, Is.False);
            Assert.That(result.Level, Is.EqualTo(level));
            Assert.That(result.DifferentComponent, Is.EqualTo(component));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertFailure(
        UrnEquivalenceResult result,
        UrnEquivalenceLevel level,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Equal, Is.False);
            Assert.That(result.Level, Is.EqualTo(level));
            Assert.That(result.DifferentComponent, Is.EqualTo(
                UriComponent.None));
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
                DiagnosticDomain.Urn));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
        });
    }

    private class RecordingNamespaceHandler : IUrnNamespaceHandler
    {
        public bool ComparisonEqual;
        public UriDiagnostic? ComparisonDiagnostic;
        public int ComparisonCount;
        public string NamespaceIdentifier;
        public string LeftValue;
        public string RightValue;

        public RecordingNamespaceHandler()
        {
            ComparisonEqual = false;
            ComparisonDiagnostic = null;
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
            if (ComparisonDiagnostic != null)
            {
                return new UrnEquivalenceResult(
                    false,
                    false,
                    UrnEquivalenceLevel.NamespaceSpecific,
                    UriComponent.None,
                    ComparisonDiagnostic);
            }

            UriComponent component =
                UriComponent.UrnNamespaceSpecificString;
            if (ComparisonEqual)
            {
                component = UriComponent.None;
            }

            return new UrnEquivalenceResult(
                true,
                ComparisonEqual,
                UrnEquivalenceLevel.NamespaceSpecific,
                component,
                null);
        }
    }
}
