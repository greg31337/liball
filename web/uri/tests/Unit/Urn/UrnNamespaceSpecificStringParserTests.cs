namespace uri.Tests;

public class UrnNamespaceSpecificStringParserTests
{
    [Test]
    public void AssignedNamePreservesSchemeNamespaceNssAndTerminatorBoundary()
    {
        const string text = "URN:Example:a/b%2f?+resolver";

        UrnAssignedNameParseResult result =
            UrnParserCore.ParseAssignedName(
                text.AsSpan(),
                UrnValidationMode.GenericSyntax);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Scheme, Is.EqualTo("URN"));
            Assert.That(result.NamespaceIdentifier, Is.EqualTo("Example"));
            Assert.That(result.NamespaceSpecificString, Is.EqualTo("a/b%2f"));
            Assert.That(result.AssignedName, Is.EqualTo(
                "URN:Example:a/b%2f"));
            Assert.That(result.NextOffset, Is.EqualTo(18));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void EveryAsciiPcharAndSubsequentSlashIsAcceptedWithoutRewriting()
    {
        const string namespaceSpecificString =
            "AZaz09-._~!$&'()*+,;=:@/path//tail/%2f/%FF";
        string text = "urn:example:" + namespaceSpecificString;

        UrnAssignedNameParseResult result =
            UrnParserCore.ParseAssignedName(
                text.AsSpan(),
                UrnValidationMode.GenericSyntax);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.NamespaceSpecificString, Is.EqualTo(
                namespaceSpecificString));
            Assert.That(result.AssignedName, Is.EqualTo(text));
            Assert.That(result.NextOffset, Is.EqualTo(text.Length));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [TestCase("urn:example:a?+resolver", "a", 13)]
    [TestCase("urn:example:a?=query", "a", 13)]
    [TestCase("urn:example:a#fragment", "a", 13)]
    [TestCase("urn:example:a/b", "a/b", 15)]
    public void NssParsingStopsAtOptionalComponentDelimitersOrInputEnd(
        string text,
        string expectedNamespaceSpecificString,
        int expectedOffset)
    {
        UrnAssignedNameParseResult result =
            UrnParserCore.ParseAssignedName(
                text.AsSpan(),
                UrnValidationMode.GenericSyntax);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.NamespaceSpecificString, Is.EqualTo(
                expectedNamespaceSpecificString));
            Assert.That(result.NextOffset, Is.EqualTo(expectedOffset));
            Assert.That(result.AssignedName, Is.EqualTo(
                text.Substring(0, expectedOffset)));
        });
    }

    [TestCase("urn:example:", 12)]
    [TestCase("urn:example:?+resolver", 12)]
    [TestCase("urn:example:?=query", 12)]
    [TestCase("urn:example:#fragment", 12)]
    public void NamespaceSpecificStringMustBePresentBeforeOptionalComponents(
        string text,
        int offset)
    {
        UrnAssignedNameParseResult result =
            UrnParserCore.ParseAssignedName(
                text.AsSpan(),
                UrnValidationMode.GenericSyntax);

        AssertAssignedNameFailure(
            result,
            UriDiagnosticCode.InvalidNamespaceSpecificString,
            offset,
            0,
            UrnParserCore.InvalidNamespaceSpecificStringMessage);
    }

    [TestCase("urn:example:/path", 12)]
    [TestCase("urn:example:a b", 13)]
    [TestCase("urn:example:a[b", 13)]
    [TestCase("urn:example:a]b", 13)]
    [TestCase("urn:example:a\\b", 13)]
    [TestCase("urn:example:a^b", 13)]
    [TestCase("urn:example:a`b", 13)]
    [TestCase("urn:example:a|b", 13)]
    [TestCase("urn:example:aé", 13)]
    public void InvalidOrNonAsciiNssCharactersHaveExactDiagnostics(
        string text,
        int offset)
    {
        UrnAssignedNameParseResult result =
            UrnParserCore.ParseAssignedName(
                text.AsSpan(),
                UrnValidationMode.GenericSyntax);

        AssertAssignedNameFailure(
            result,
            UriDiagnosticCode.InvalidNamespaceSpecificString,
            offset,
            1,
            UrnParserCore.InvalidNamespaceSpecificStringMessage);
    }

    [TestCase("urn:example:a%", 13)]
    [TestCase("urn:example:a%0", 13)]
    [TestCase("urn:example:a%G0", 14)]
    [TestCase("urn:example:a%0G", 15)]
    [TestCase("urn:example:a%?+resolver", 13)]
    [TestCase("urn:example:a%#fragment", 13)]
    public void MalformedNssPercentTripletsHaveUrnDomainExactDiagnostics(
        string text,
        int offset)
    {
        UrnAssignedNameParseResult result =
            UrnParserCore.ParseAssignedName(
                text.AsSpan(),
                UrnValidationMode.GenericSyntax);

        AssertAssignedNameFailure(
            result,
            UriDiagnosticCode.InvalidPercentEncoding,
            offset,
            1,
            DiagnosticFactory.InvalidPercentEncodingMessage);
    }

    [Test]
    public void PercentEncodedSlashIsValidAsTheFirstNssCharacter()
    {
        UrnAssignedNameParseResult result =
            UrnParserCore.ParseAssignedName(
                "urn:example:%2Fpath".AsSpan(),
                UrnValidationMode.GenericSyntax);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.NamespaceSpecificString, Is.EqualTo("%2Fpath"));
            Assert.That(result.AssignedName, Is.EqualTo(
                "urn:example:%2Fpath"));
        });
    }

    [Test]
    public void AssignedNameParsingPropagatesRegisteredNamespacePolicy()
    {
        UrnAssignedNameParseResult generic =
            UrnParserCore.ParseAssignedName(
                "urn:x-test:value".AsSpan(),
                UrnValidationMode.GenericSyntax);
        UrnAssignedNameParseResult registered =
            UrnParserCore.ParseAssignedName(
                "urn:x-test:value".AsSpan(),
                UrnValidationMode.RegisteredNamespace);

        Assert.Multiple(() =>
        {
            Assert.That(generic.Success, Is.True);
            Assert.That(registered.Success, Is.False);
            Assert.That(registered.Diagnostic, Is.Not.Null);
            Assert.That(registered.Diagnostic!.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidNamespaceIdentifier));
            Assert.That(registered.Diagnostic.Component, Is.EqualTo(
                UriComponent.UrnNamespaceIdentifier));
        });
    }

    [TestCase(-1)]
    [TestCase(4)]
    public void InvalidStandaloneNssStartOffsetIsRejected(int startOffset)
    {
        UrnNamespaceSpecificStringParseResult result =
            UrnParserCore.ParseNamespaceSpecificString(
                "abc".AsSpan(),
                startOffset);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.NamespaceSpecificString, Is.Empty);
            Assert.That(result.NextOffset, Is.EqualTo(0));
            Assert.That(result.Diagnostic, Is.Not.Null);
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
                DiagnosticDomain.Urn));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.InvalidInput));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(
                UriComponent.Input));
        });
    }

    private static void AssertAssignedNameFailure(
        UrnAssignedNameParseResult result,
        UriDiagnosticCode code,
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
            Assert.That(result.NamespaceSpecificString, Is.Empty);
            Assert.That(result.AssignedName, Is.Empty);
            Assert.That(result.NextOffset, Is.EqualTo(0));
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(
                DiagnosticDomain.Urn));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Severity, Is.EqualTo(
                DiagnosticSeverity.Error));
            Assert.That(result.Diagnostic.Component, Is.EqualTo(
                UriComponent.UrnNamespaceSpecificString));
            Assert.That(result.Diagnostic.Range.Offset, Is.EqualTo(offset));
            Assert.That(result.Diagnostic.Range.Length, Is.EqualTo(length));
            Assert.That(result.Diagnostic.Message, Is.EqualTo(message));
        });
    }
}
