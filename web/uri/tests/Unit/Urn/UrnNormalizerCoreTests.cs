namespace uri.Tests;

public class UrnNormalizerCoreTests
{
    [TestCase(
        "URN:ExAmPlE:A%2fb%aa%41%7e",
        "urn:example:A%2Fb%AA%41%7E")]
    [TestCase(
        "UrN:urn-7:MiXeD/Path%3aValue",
        "urn:urn-7:MiXeD/Path%3AValue")]
    public void GenericNormalizationChangesOnlyRequiredAssignedNameCase(
        string input,
        string expected)
    {
        Urn urn = ParseUrn(input);

        UriTextResult result = UrnNormalizerCore.NormalizeUrn(
            urn,
            new OperationLimits());

        AssertSuccess(result, expected);
    }

    [Test]
    public void GenericNormalizationNeverPercentDecodesTheNss()
    {
        Urn urn = ParseUrn("URN:Example:%41%7e%2f%3a");

        UriTextResult result = UrnNormalizerCore.NormalizeAssignedName(
            urn,
            new OperationLimits());

        AssertSuccess(result, "urn:example:%41%7E%2F%3A");
        Assert.That(result.Text, Is.Not.EqualTo("urn:example:A~/:"));
    }

    [Test]
    public void FullNormalizationPreservesOptionalComponentsExactly()
    {
        Urn urn = ParseUrn(
            "URN:Example:A%ab?+R%af?x?=Q%bc?+y#F%cd?/z");

        UriTextResult result = UrnNormalizerCore.NormalizeUrn(
            urn,
            new OperationLimits());

        AssertSuccess(
            result,
            "urn:example:A%AB?+R%af?x?=Q%bc?+y#F%cd?/z");
    }

    [Test]
    public void AssignedNameNormalizationExcludesEveryOptionalComponent()
    {
        Urn urn = ParseUrn("URN:Example:A%ab?+r?=q#");

        UriTextResult result = UrnNormalizerCore.NormalizeAssignedName(
            urn,
            new OperationLimits());

        AssertSuccess(result, "urn:example:A%AB");
    }

    [Test]
    public void NormalizationUsesActiveFieldsInsteadOfDerivedTextFields()
    {
        Urn urn = ParseUrn("urn:example:old");
        urn.Scheme = "URN";
        urn.NamespaceIdentifier = "Changed";
        urn.NamespaceSpecificString = "New%afValue";
        urn.AssignedName = "stale-assigned-name";
        urn.HasQComponent = true;
        urn.QComponent = "Query%bc";
        urn.OriginalText = "stale-original-text";

        UriTextResult result = UrnNormalizerCore.NormalizeUrn(
            urn,
            new OperationLimits());

        AssertSuccess(
            result,
            "urn:changed:New%AFValue?=Query%bc");
    }

    [Test]
    public void GenericNormalizationIsIdempotent()
    {
        Urn urn = ParseUrn("URN:Example:A%ab?+R%af?=Q%bc#F%cd");
        UriTextResult first = UrnNormalizerCore.NormalizeUrn(
            urn,
            new OperationLimits());
        Urn normalized = ParseUrn(first.Text);

        UriTextResult second = UrnNormalizerCore.NormalizeUrn(
            normalized,
            new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(second.Text, Is.EqualTo(first.Text));
            Assert.That(second.Length, Is.EqualTo(first.Length));
        });
    }

    [Test]
    public void BothNormalizationFormsHonorExactAndShortOutputLimits()
    {
        Urn urn = ParseUrn("URN:Example:A%ab?+r?=q#f");
        const string normalizedUrn = "urn:example:A%AB?+r?=q#f";
        const string normalizedAssignedName = "urn:example:A%AB";
        OperationLimits exactUrn = CreateOutputLimits(
            normalizedUrn.Length);
        OperationLimits shortUrn = CreateOutputLimits(
            normalizedUrn.Length - 1);
        OperationLimits exactAssignedName = CreateOutputLimits(
            normalizedAssignedName.Length);
        OperationLimits shortAssignedName = CreateOutputLimits(
            normalizedAssignedName.Length - 1);

        UriTextResult completeExact = UrnNormalizerCore.NormalizeUrn(
            urn,
            exactUrn);
        UriTextResult completeShort = UrnNormalizerCore.NormalizeUrn(
            urn,
            shortUrn);
        UriTextResult assignedExact =
            UrnNormalizerCore.NormalizeAssignedName(
                urn,
                exactAssignedName);
        UriTextResult assignedShort =
            UrnNormalizerCore.NormalizeAssignedName(
                urn,
                shortAssignedName);

        Assert.Multiple(() =>
        {
            AssertSuccess(completeExact, normalizedUrn);
            AssertSuccess(assignedExact, normalizedAssignedName);
            AssertFailure(
                completeShort,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Output);
            AssertFailure(
                assignedShort,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Output);
        });
    }

    [Test]
    public void InvalidInputsAndInvalidAssignedNameFieldsFailCleanly()
    {
        Urn wrongScheme = ParseUrn("urn:example:a");
        wrongScheme.Scheme = "http";
        Urn invalidNid = ParseUrn("urn:example:a");
        invalidNid.NamespaceIdentifier = "x";
        Urn invalidNss = ParseUrn("urn:example:a");
        invalidNss.NamespaceSpecificString = "%gg";

        UriTextResult missingUrn = UrnNormalizerCore.NormalizeUrn(
            null,
            new OperationLimits());
        UriTextResult missingLimits = UrnNormalizerCore.NormalizeUrn(
            wrongScheme,
            null);
        UriTextResult scheme = UrnNormalizerCore.NormalizeUrn(
            wrongScheme,
            new OperationLimits());
        UriTextResult nid = UrnNormalizerCore.NormalizeAssignedName(
            invalidNid,
            new OperationLimits());
        UriTextResult nss = UrnNormalizerCore.NormalizeAssignedName(
            invalidNss,
            new OperationLimits());

        Assert.Multiple(() =>
        {
            AssertFailure(
                missingUrn,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Input);
            AssertFailure(
                missingLimits,
                UriDiagnosticCode.InvalidInput,
                UriComponent.Input);
            AssertFailure(
                scheme,
                UriDiagnosticCode.InvalidUrn,
                UriComponent.Urn);
            AssertFailure(
                nid,
                UriDiagnosticCode.InvalidNamespaceIdentifier,
                UriComponent.UrnNamespaceIdentifier);
            AssertFailure(
                nss,
                UriDiagnosticCode.InvalidPercentEncoding,
                UriComponent.UrnNamespaceSpecificString);
        });
    }

    [Test]
    public void AssignedNameNormalizationDoesNotValidateExcludedComponents()
    {
        Urn urn = ParseUrn("URN:Example:A%ab");
        urn.HasRComponent = true;
        urn.RComponent = "";

        UriTextResult assignedName =
            UrnNormalizerCore.NormalizeAssignedName(
                urn,
                new OperationLimits());
        UriTextResult complete = UrnNormalizerCore.NormalizeUrn(
            urn,
            new OperationLimits());

        Assert.Multiple(() =>
        {
            AssertSuccess(assignedName, "urn:example:A%AB");
            AssertFailure(
                complete,
                UriDiagnosticCode.InvalidUrn,
                UriComponent.UrnRComponent);
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

    private static OperationLimits CreateOutputLimits(int maximum)
    {
        OperationLimits limits = new OperationLimits();
        limits.MaxOutputCharacters = maximum;
        return limits;
    }

    private static void AssertSuccess(
        UriTextResult result,
        string expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo(expected));
            Assert.That(result.Length, Is.EqualTo(expected.Length));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertFailure(
        UriTextResult result,
        UriDiagnosticCode code,
        UriComponent component)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.Empty);
            Assert.That(result.Length, Is.Zero);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
                DiagnosticDomain.Urn));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
        });
    }
}
