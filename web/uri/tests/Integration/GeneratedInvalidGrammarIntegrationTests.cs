namespace uri.Tests;

public class GeneratedInvalidGrammarIntegrationTests
{
    [TestCaseSource(nameof(UriInvalidCases))]
    public void UriSingleMutationsHaveStableFirstErrorOffsets(
        InvalidGrammarCase grammarCase)
    {
        IUriParser parser = new UriParser();
        string invalid = Mutate(grammarCase);

        if (grammarCase.AbsoluteProduction)
        {
            UriReference baseline = parser.ParseAbsoluteUri(
                grammarCase.ValidText,
                new UriParseOptions());
            UriParseResult result = parser.TryParseAbsoluteUri(
                invalid,
                new UriParseOptions());
            UriSyntaxException exception = Assert.Throws<
                UriSyntaxException>(() => parser.ParseAbsoluteUri(
                    invalid,
                    new UriParseOptions()))!;

            Assert.That(baseline.OriginalText, Is.EqualTo(
                grammarCase.ValidText));
            AssertUriFailure(result, grammarCase);
            AssertDiagnostic(exception.Diagnostic, grammarCase);
            return;
        }

        UriReference reference = parser.ParseReference(
            grammarCase.ValidText,
            new UriParseOptions());
        UriParseResult stringResult = parser.TryParseReference(
            invalid,
            new UriParseOptions());
        UriParseResult spanResult = parser.TryParseReferenceSpan(
            invalid.AsSpan(),
            new UriParseOptions());
        UriSyntaxException referenceException = Assert.Throws<
            UriSyntaxException>(() => parser.ParseReference(
                invalid,
                new UriParseOptions()))!;

        Assert.That(reference.OriginalText, Is.EqualTo(grammarCase.ValidText));
        AssertUriFailure(stringResult, grammarCase);
        AssertUriFailure(spanResult, grammarCase);
        AssertDiagnostic(referenceException.Diagnostic, grammarCase);
    }

    [TestCaseSource(nameof(IriInvalidCases))]
    public void IriSingleMutationsHaveStableFirstErrorOffsets(
        InvalidGrammarCase grammarCase)
    {
        IIriParser parser = new IriParser();
        string invalid = Mutate(grammarCase);

        if (grammarCase.AbsoluteProduction)
        {
            IriParseResult baseline = parser.TryParseAbsoluteIri(
                grammarCase.ValidText,
                new IriParseOptions());
            IriParseResult first = parser.TryParseAbsoluteIri(
                invalid,
                new IriParseOptions());
            IriParseResult second = parser.TryParseAbsoluteIri(
                invalid,
                new IriParseOptions());

            Assert.That(baseline.Success, Is.True);
            AssertIriFailure(first, grammarCase);
            AssertIriFailure(second, grammarCase);
            return;
        }

        IriReference reference = parser.ParseIriReference(
            grammarCase.ValidText,
            new IriParseOptions());
        IriParseResult stringResult = parser.TryParseIriReference(
            invalid,
            new IriParseOptions());
        IriParseResult spanResult = parser.TryParseIriReferenceSpan(
            invalid.AsSpan(),
            new IriParseOptions());
        IriSyntaxException referenceException = Assert.Throws<
            IriSyntaxException>(() => parser.ParseIriReference(
                invalid,
                new IriParseOptions()))!;

        Assert.That(reference.OriginalText, Is.EqualTo(grammarCase.ValidText));
        AssertIriFailure(stringResult, grammarCase);
        AssertIriFailure(spanResult, grammarCase);
        AssertDiagnostic(referenceException.Diagnostic, grammarCase);
    }

    [TestCaseSource(nameof(UrnInvalidCases))]
    public void UrnSingleMutationsHaveStableFirstErrorOffsets(
        InvalidGrammarCase grammarCase)
    {
        IUrnParser parser = new UrnParser();
        string invalid = Mutate(grammarCase);

        Urn baseline = parser.ParseUrn(
            grammarCase.ValidText,
            new UrnParseOptions());
        UrnParseResult stringResult = parser.TryParseUrn(
            invalid,
            new UrnParseOptions());
        UrnParseResult spanResult = parser.TryParseUrnSpan(
            invalid.AsSpan(),
            new UrnParseOptions());
        UrnSyntaxException exception = Assert.Throws<UrnSyntaxException>(() =>
            parser.ParseUrn(invalid, new UrnParseOptions()))!;

        Assert.That(baseline.OriginalText, Is.EqualTo(grammarCase.ValidText));
        AssertUrnFailure(stringResult, grammarCase);
        AssertUrnFailure(spanResult, grammarCase);
        AssertDiagnostic(exception.Diagnostic, grammarCase);
    }

    private static System.Collections.Generic.IEnumerable<TestCaseData>
        UriInvalidCases()
    {
        yield return GrammarTestCase(
            "URI_scheme",
            new InvalidGrammarCase(
                "http:path",
                0,
                '1',
                DiagnosticDomain.Uri,
                UriDiagnosticCode.InvalidScheme,
                UriComponent.Scheme,
                true));
        yield return GrammarTestCase(
            "URI_userinfo",
            new InvalidGrammarCase(
                "x://user@host/path",
                5,
                '@',
                DiagnosticDomain.Uri,
                UriDiagnosticCode.InvalidUserInfo,
                UriComponent.UserInfo,
                false));
        yield return GrammarTestCase(
            "URI_host",
            new InvalidGrammarCase(
                "x://host/path",
                6,
                '[',
                DiagnosticDomain.Uri,
                UriDiagnosticCode.InvalidHost,
                UriComponent.Host,
                false));
        yield return GrammarTestCase(
            "URI_port",
            new InvalidGrammarCase(
                "x://host:80/path",
                9,
                'x',
                DiagnosticDomain.Uri,
                UriDiagnosticCode.InvalidPort,
                UriComponent.Port,
                false));
        yield return GrammarTestCase(
            "URI_path",
            new InvalidGrammarCase(
                "x:path%20value",
                7,
                'G',
                DiagnosticDomain.Uri,
                UriDiagnosticCode.InvalidPercentEncoding,
                UriComponent.Path,
                false));
        yield return GrammarTestCase(
            "URI_query",
            new InvalidGrammarCase(
                "x:path?query/value",
                7,
                '[',
                DiagnosticDomain.Uri,
                UriDiagnosticCode.InvalidQuery,
                UriComponent.Query,
                false));
        yield return GrammarTestCase(
            "URI_fragment",
            new InvalidGrammarCase(
                "x:path#fragment",
                7,
                '#',
                DiagnosticDomain.Uri,
                UriDiagnosticCode.InvalidFragment,
                UriComponent.Fragment,
                false));
    }

    private static System.Collections.Generic.IEnumerable<TestCaseData>
        IriInvalidCases()
    {
        yield return GrammarTestCase(
            "IRI_scheme",
            new InvalidGrammarCase(
                "http:道",
                0,
                '1',
                DiagnosticDomain.Iri,
                UriDiagnosticCode.InvalidScheme,
                UriComponent.Scheme,
                true));
        yield return GrammarTestCase(
            "IRI_userinfo",
            new InvalidGrammarCase(
                "x://名人@例え/道",
                5,
                '@',
                DiagnosticDomain.Iri,
                UriDiagnosticCode.InvalidUserInfo,
                UriComponent.UserInfo,
                false));
        yield return GrammarTestCase(
            "IRI_host",
            new InvalidGrammarCase(
                "x://例え/道",
                5,
                '\uE000',
                DiagnosticDomain.Iri,
                UriDiagnosticCode.InvalidHost,
                UriComponent.Host,
                false));
        yield return GrammarTestCase(
            "IRI_port",
            new InvalidGrammarCase(
                "x://例え:80/道",
                7,
                'x',
                DiagnosticDomain.Iri,
                UriDiagnosticCode.InvalidPort,
                UriComponent.Port,
                false));
        yield return GrammarTestCase(
            "IRI_path",
            new InvalidGrammarCase(
                "x:道/先",
                2,
                '\uE000',
                DiagnosticDomain.Iri,
                UriDiagnosticCode.InvalidPath,
                UriComponent.Path,
                false));
        yield return GrammarTestCase(
            "IRI_query",
            new InvalidGrammarCase(
                "x:道?問",
                4,
                '\ud800',
                DiagnosticDomain.Iri,
                UriDiagnosticCode.InvalidUnicode,
                UriComponent.Query,
                false));
        yield return GrammarTestCase(
            "IRI_fragment",
            new InvalidGrammarCase(
                "x:道#片",
                4,
                '\uE000',
                DiagnosticDomain.Iri,
                UriDiagnosticCode.InvalidFragment,
                UriComponent.Fragment,
                false));
    }

    private static System.Collections.Generic.IEnumerable<TestCaseData>
        UrnInvalidCases()
    {
        yield return GrammarTestCase(
            "URN_scheme",
            new InvalidGrammarCase(
                "urn:example:value",
                0,
                'x',
                DiagnosticDomain.Urn,
                UriDiagnosticCode.InvalidUrn,
                UriComponent.Urn,
                false));
        yield return GrammarTestCase(
            "URN_namespace_identifier",
            new InvalidGrammarCase(
                "urn:example:value",
                7,
                '_',
                DiagnosticDomain.Urn,
                UriDiagnosticCode.InvalidNamespaceIdentifier,
                UriComponent.UrnNamespaceIdentifier,
                false));
        yield return GrammarTestCase(
            "URN_namespace_specific_string",
            new InvalidGrammarCase(
                "urn:example:value",
                14,
                '[',
                DiagnosticDomain.Urn,
                UriDiagnosticCode.InvalidNamespaceSpecificString,
                UriComponent.UrnNamespaceSpecificString,
                false));
        yield return GrammarTestCase(
            "URN_r_component",
            new InvalidGrammarCase(
                "urn:example:value?+resolver?=query#fragment",
                21,
                ' ',
                DiagnosticDomain.Urn,
                UriDiagnosticCode.InvalidUrn,
                UriComponent.UrnRComponent,
                false));
        yield return GrammarTestCase(
            "URN_q_component",
            new InvalidGrammarCase(
                "urn:example:value?+resolver?=query#fragment",
                29,
                ' ',
                DiagnosticDomain.Urn,
                UriDiagnosticCode.InvalidUrn,
                UriComponent.UrnQComponent,
                false));
        yield return GrammarTestCase(
            "URN_f_component",
            new InvalidGrammarCase(
                "urn:example:value?+resolver?=query#fragment",
                35,
                '[',
                DiagnosticDomain.Urn,
                UriDiagnosticCode.InvalidUrn,
                UriComponent.UrnFComponent,
                false));
    }

    private static TestCaseData GrammarTestCase(
        string name,
        InvalidGrammarCase grammarCase)
    {
        return new TestCaseData(grammarCase).SetName(name);
    }

    private static string Mutate(InvalidGrammarCase grammarCase)
    {
        char[] characters = grammarCase.ValidText.ToCharArray();
        Assert.That(grammarCase.Offset, Is.GreaterThanOrEqualTo(0));
        Assert.That(grammarCase.Offset, Is.LessThan(characters.Length));
        Assert.That(characters[grammarCase.Offset], Is.Not.EqualTo(
            grammarCase.Replacement));
        characters[grammarCase.Offset] = grammarCase.Replacement;
        string invalid = new string(characters);
        int differences = 0;
        int index = 0;
        while (index < characters.Length)
        {
            if (invalid[index] != grammarCase.ValidText[index])
            {
                differences = differences + 1;
            }

            index = index + 1;
        }

        Assert.That(differences, Is.EqualTo(1));
        return invalid;
    }

    private static void AssertUriFailure(
        UriParseResult result,
        InvalidGrammarCase grammarCase)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reference.OriginalText, Is.Empty);
        Assert.That(result.Diagnostic, Is.Not.Null);
        AssertDiagnostic(result.Diagnostic!, grammarCase);
    }

    private static void AssertIriFailure(
        IriParseResult result,
        InvalidGrammarCase grammarCase)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Reference.OriginalText, Is.Empty);
        Assert.That(result.Diagnostic, Is.Not.Null);
        AssertDiagnostic(result.Diagnostic!, grammarCase);
    }

    private static void AssertUrnFailure(
        UrnParseResult result,
        InvalidGrammarCase grammarCase)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Urn.OriginalText, Is.Empty);
        Assert.That(result.Diagnostic, Is.Not.Null);
        AssertDiagnostic(result.Diagnostic!, grammarCase);
    }

    private static void AssertDiagnostic(
        UriDiagnostic diagnostic,
        InvalidGrammarCase grammarCase)
    {
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Domain, Is.EqualTo(grammarCase.Domain));
            Assert.That(diagnostic.Code, Is.EqualTo(grammarCase.Code));
            Assert.That(diagnostic.Severity, Is.EqualTo(
                DiagnosticSeverity.Error));
            Assert.That(diagnostic.Component, Is.EqualTo(
                grammarCase.Component));
            Assert.That(diagnostic.Range.Offset, Is.EqualTo(
                grammarCase.Offset));
            Assert.That(diagnostic.Range.Length, Is.EqualTo(1));
        });
    }

    public class InvalidGrammarCase
    {
        public string ValidText;
        public int Offset;
        public char Replacement;
        public DiagnosticDomain Domain;
        public UriDiagnosticCode Code;
        public UriComponent Component;
        public bool AbsoluteProduction;

        public InvalidGrammarCase(
            string validText,
            int offset,
            char replacement,
            DiagnosticDomain domain,
            UriDiagnosticCode code,
            UriComponent component,
            bool absoluteProduction)
        {
            ValidText = validText;
            Offset = offset;
            Replacement = replacement;
            Domain = domain;
            Code = code;
            Component = component;
            AbsoluteProduction = absoluteProduction;
        }
    }
}
