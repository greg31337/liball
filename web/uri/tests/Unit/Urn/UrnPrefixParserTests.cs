namespace uri.Tests;

public class UrnPrefixParserTests
{
    [TestCase("urn")]
    [TestCase("URN")]
    [TestCase("UrN")]
    [TestCase("uRn")]
    public void SchemeIsAsciiCaseInsensitiveAndItsSpellingIsPreserved(
        string scheme)
    {
        string text = scheme + ":Example:value";

        UrnPrefixParseResult result = UrnParserCore.ParsePrefix(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);

        AssertSuccess(result, scheme, "Example", scheme.Length + 9);
    }

    [Test]
    public void NamespaceIdentifierAcceptsBothLengthBoundariesAndLdhInterior()
    {
        string maximum = "a" + new string('b', 30) + "9";

        UrnPrefixParseResult minimum = UrnParserCore.ParsePrefix(
            "urn:a9:value".AsSpan(),
            UrnValidationMode.GenericSyntax);
        UrnPrefixParseResult interior = UrnParserCore.ParsePrefix(
            "urn:Example-123:value".AsSpan(),
            UrnValidationMode.GenericSyntax);
        UrnPrefixParseResult maximumResult = UrnParserCore.ParsePrefix(
            ("urn:" + maximum + ":value").AsSpan(),
            UrnValidationMode.GenericSyntax);

        AssertSuccess(minimum, "urn", "a9", 7);
        AssertSuccess(interior, "urn", "Example-123", 16);
        AssertSuccess(maximumResult, "urn", maximum, 37);
    }

    [TestCase("urn:a1:value", "a1")]
    [TestCase("urn:x-test:value", "x-test")]
    [TestCase("urn:ab-test:value", "ab-test")]
    [TestCase("urn:urn-0:value", "urn-0")]
    public void GenericModeDoesNotApplyNamespaceRegistrationPolicy(
        string text,
        string expectedNamespaceIdentifier)
    {
        UrnPrefixParseResult result = UrnParserCore.ParsePrefix(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);

        AssertSuccess(
            result,
            "urn",
            expectedNamespaceIdentifier,
            5 + expectedNamespaceIdentifier.Length);
    }

    [TestCase("urn:isbn:value", "isbn")]
    [TestCase("urn:example:value", "example")]
    [TestCase("urn:urn-1:value", "urn-1")]
    [TestCase("urn:URN-42:value", "URN-42")]
    public void RegisteredModeAcceptsEligibleFormalAndInformalIdentifiers(
        string text,
        string expectedNamespaceIdentifier)
    {
        UrnPrefixParseResult result = UrnParserCore.ParsePrefix(
            text.AsSpan(),
            UrnValidationMode.RegisteredNamespace);

        AssertSuccess(
            result,
            "urn",
            expectedNamespaceIdentifier,
            5 + expectedNamespaceIdentifier.Length);
    }

    [TestCase("urn:a1:value", 4, 2)]
    [TestCase("urn:x-test:value", 4, 2)]
    [TestCase("urn:ab-test:value", 4, 3)]
    [TestCase("urn:xn--test:value", 4, 3)]
    [TestCase("urn:urn-0:value", 4, 5)]
    [TestCase("urn:urn-01:value", 4, 6)]
    [TestCase("urn:urn-name:value", 4, 8)]
    public void RegisteredModeRejectsReservedOrMalformedRegistrationNames(
        string text,
        int offset,
        int length)
    {
        UrnPrefixParseResult result = UrnParserCore.ParsePrefix(
            text.AsSpan(),
            UrnValidationMode.RegisteredNamespace);

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidNamespaceIdentifier,
            UriComponent.UrnNamespaceIdentifier,
            offset,
            length,
            UrnParserCore.RegisteredNamespaceIdentifierMessage);
    }

    [TestCase("urn::value", 4, 0)]
    [TestCase("urn:a:value", 4, 1)]
    [TestCase("urn:-ab:value", 4, 1)]
    [TestCase("urn:ab-:value", 6, 1)]
    [TestCase("urn:a_b:value", 5, 1)]
    [TestCase("urn:aé:value", 5, 1)]
    [TestCase("urn:ab?x:value", 6, 1)]
    [TestCase("urn:ab", 6, 0)]
    public void InvalidGenericNamespaceIdentifiersHaveExactDiagnostics(
        string text,
        int offset,
        int length)
    {
        UrnPrefixParseResult result = UrnParserCore.ParsePrefix(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidNamespaceIdentifier,
            UriComponent.UrnNamespaceIdentifier,
            offset,
            length,
            UrnParserCore.InvalidNamespaceIdentifierMessage);
    }

    [Test]
    public void ThirtyThirdNamespaceIdentifierCharacterIsRejectedPrecisely()
    {
        string namespaceIdentifier = "a" + new string('b', 31) + "9";

        UrnPrefixParseResult result = UrnParserCore.ParsePrefix(
            ("urn:" + namespaceIdentifier + ":value").AsSpan(),
            UrnValidationMode.GenericSyntax);

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidNamespaceIdentifier,
            UriComponent.UrnNamespaceIdentifier,
            36,
            1,
            UrnParserCore.InvalidNamespaceIdentifierMessage);
    }

    [TestCase("", 0, 0)]
    [TestCase("u", 1, 0)]
    [TestCase("ur", 2, 0)]
    [TestCase("urn", 3, 0)]
    [TestCase("xrn:ab:value", 0, 1)]
    [TestCase("url:ab:value", 2, 1)]
    [TestCase("urnx:ab:value", 3, 1)]
    public void MissingOrWrongUrnSchemeHasAnExactUrnDiagnostic(
        string text,
        int offset,
        int length)
    {
        UrnPrefixParseResult result = UrnParserCore.ParsePrefix(
            text.AsSpan(),
            UrnValidationMode.GenericSyntax);

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidUrn,
            UriComponent.Urn,
            offset,
            length,
            UrnParserCore.InvalidSchemeMessage);
    }

    [Test]
    public void UnknownValidationModeIsRejectedBeforeReadingInput()
    {
        UrnPrefixParseResult result = UrnParserCore.ParsePrefix(
            "urn:example:value".AsSpan(),
            (UrnValidationMode)99);

        AssertFailure(
            result,
            UriDiagnosticCode.InvalidInput,
            UriComponent.Input,
            0,
            0,
            DiagnosticFactory.InvalidInputMessage);
    }

    private static void AssertSuccess(
        UrnPrefixParseResult result,
        string expectedScheme,
        string expectedNamespaceIdentifier,
        int expectedNextOffset)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Scheme, Is.EqualTo(expectedScheme));
            Assert.That(result.NamespaceIdentifier, Is.EqualTo(
                expectedNamespaceIdentifier));
            Assert.That(result.NextOffset, Is.EqualTo(expectedNextOffset));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertFailure(
        UrnPrefixParseResult result,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        int length,
        string message)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Scheme, Is.Empty);
            Assert.That(result.NamespaceIdentifier, Is.Empty);
            Assert.That(result.NextOffset, Is.EqualTo(0));
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
                DiagnosticDomain.Urn));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Severity, Is.EqualTo(
                DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(component));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(length));
            Assert.That(result.Diagnostic.Message, Is.EqualTo(message));
        });
    }
}
