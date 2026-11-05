namespace svg.Tests;

public class SvgXmlNamespaceResolverTests
{
    [Test]
    public void DefaultPrefixedAndBuiltInNamespacesAreAssignedAcrossNestedScopes()
    {
        string input = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:x='urn:x' plain='v' x:href='a' xml:lang='en'>" +
            "<g xmlns='urn:inner' xmlns:x='urn:x2'><x:item x:a='1' a='2'/></g>" +
            "<x:item/><none xmlns=''><child/></none></svg>";

        SvgLoadResult result = Load(input);
        List<SvgXmlToken> elements = result.XmlTokens
            .Where(token => token.Kind == SvgXmlTokenKind.StartElement || token.Kind == SvgXmlTokenKind.EmptyElement || token.Kind == SvgXmlTokenKind.EndElement)
            .ToList();
        SvgXmlToken root = elements[0];
        SvgXmlToken innerGroup = elements[1];
        SvgXmlToken innerItem = elements[2];
        SvgXmlToken outerItem = elements[4];
        SvgXmlToken none = elements[5];
        SvgXmlToken child = elements[6];
        SvgXmlToken rootEnd = elements[8];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(root.NamespaceUri, Is.EqualTo(SvgNamespaces.Svg));
            Assert.That(root.Attributes[0].NamespaceUri, Is.EqualTo(SvgNamespaces.Xmlns));
            Assert.That(root.Attributes[1].NamespaceUri, Is.EqualTo(SvgNamespaces.Xmlns));
            Assert.That(root.Attributes[2].NamespaceUri, Is.Empty);
            Assert.That(root.Attributes[3].NamespaceUri, Is.EqualTo("urn:x"));
            Assert.That(root.Attributes[4].NamespaceUri, Is.EqualTo(SvgNamespaces.Xml));
            Assert.That(innerGroup.NamespaceUri, Is.EqualTo("urn:inner"));
            Assert.That(innerItem.NamespaceUri, Is.EqualTo("urn:x2"));
            Assert.That(innerItem.Attributes[0].NamespaceUri, Is.EqualTo("urn:x2"));
            Assert.That(innerItem.Attributes[1].NamespaceUri, Is.Empty);
            Assert.That(outerItem.NamespaceUri, Is.EqualTo("urn:x"));
            Assert.That(none.NamespaceUri, Is.Empty);
            Assert.That(child.NamespaceUri, Is.Empty);
            Assert.That(rootEnd.NamespaceUri, Is.EqualTo(SvgNamespaces.Svg));
        });
    }

    [Test]
    public void DeclarationsApplyToTheirOwnTagRegardlessOfAttributeOrderAndEmptyScopesDoNotLeak()
    {
        string input = "<svg xmlns:p='urn:outer'><p:one p:a='1' xmlns:p='urn:inner'/><p:two p:a='2'/></svg>";

        SvgLoadResult result = Load(input);
        List<SvgXmlToken> empty = result.XmlTokens.Where(token => token.Kind == SvgXmlTokenKind.EmptyElement).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(empty[0].NamespaceUri, Is.EqualTo("urn:inner"));
            Assert.That(empty[0].Attributes[0].NamespaceUri, Is.EqualTo("urn:inner"));
            Assert.That(empty[1].NamespaceUri, Is.EqualTo("urn:outer"));
            Assert.That(empty[1].Attributes[0].NamespaceUri, Is.EqualTo("urn:outer"));
        });
    }

    [Test]
    public void NamespaceNamesUseXmlNormalizedDeclarationValues()
    {
        SvgLoadResult result = Load("<svg p:a='1' xmlns:p='urn:a&amp;b'/>");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.XmlTokens[0].NamespaceUri, Is.Empty);
            Assert.That(result.XmlTokens[0].Attributes[0].NamespaceUri, Is.EqualTo("urn:a&b"));
            Assert.That(result.XmlTokens[0].Attributes[1].NormalizedValue, Is.EqualTo("urn:a&b"));
            Assert.That(result.XmlTokens[0].Attributes[1].NamespaceUri, Is.EqualTo(SvgNamespaces.Xmlns));
        });
    }

    [TestCase("<:svg/>", SvgXmlNamespaceError.InvalidQualifiedName, 1)]
    [TestCase("<svg:/>", SvgXmlNamespaceError.InvalidQualifiedName, 4)]
    [TestCase("<a:b:c xmlns:a='urn:a'/>", SvgXmlNamespaceError.InvalidQualifiedName, 4)]
    [TestCase("<svg a:b:c='x' xmlns:a='urn:a'/>", SvgXmlNamespaceError.InvalidQualifiedName, 8)]
    [TestCase("<p:1name xmlns:p='urn:p'/>", SvgXmlNamespaceError.InvalidQualifiedName, 3)]
    [TestCase("<svg p:1name='x' xmlns:p='urn:p'/>", SvgXmlNamespaceError.InvalidQualifiedName, 7)]
    [TestCase("<svg xmlns:1prefix='urn:p'/>", SvgXmlNamespaceError.InvalidQualifiedName, 11)]
    [TestCase("<p:svg/>", SvgXmlNamespaceError.UndeclaredPrefix, 1)]
    [TestCase("<svg p:a='x'/>", SvgXmlNamespaceError.UndeclaredPrefix, 5)]
    public void InvalidQualifiedNamesAndUndeclaredPrefixesHaveExactOffsets(string input, SvgXmlNamespaceError expectedError, int expectedOffset)
    {
        SvgXmlNamespaceResult resolved = Resolve(input);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Success, Is.False);
            Assert.That(resolved.Error, Is.EqualTo(expectedError));
            Assert.That(resolved.ErrorOffset, Is.EqualTo(expectedOffset));
        });
    }

    [TestCase("<svg xmlns:p=''/>", SvgXmlNamespaceError.EmptyPrefixedNamespace)]
    [TestCase("<svg xmlns:xmlns='urn:no'/>", SvgXmlNamespaceError.ReservedPrefix)]
    [TestCase("<svg xmlns:xml='urn:no'/>", SvgXmlNamespaceError.ReservedPrefix)]
    [TestCase("<svg xmlns:p='http://www.w3.org/XML/1998/namespace'/>", SvgXmlNamespaceError.ReservedNamespace)]
    [TestCase("<p:svg xmlns:p='http://www.w3.org/XML/1998/namespace'/>", SvgXmlNamespaceError.ReservedNamespace)]
    [TestCase("<svg xmlns='http://www.w3.org/XML/1998/namespace'/>", SvgXmlNamespaceError.ReservedNamespace)]
    [TestCase("<svg xmlns:p='http://www.w3.org/2000/xmlns/'/>", SvgXmlNamespaceError.ReservedNamespace)]
    [TestCase("<svg xmlns='http://www.w3.org/2000/xmlns/'/>", SvgXmlNamespaceError.ReservedNamespace)]
    [TestCase("<xmlns:svg/>", SvgXmlNamespaceError.ReservedPrefix)]
    public void ReservedPrefixAndNamespaceConstraintsAreEnforced(string input, SvgXmlNamespaceError expectedError)
    {
        SvgXmlNamespaceResult resolved = Resolve(input);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Success, Is.False);
            Assert.That(resolved.Error, Is.EqualTo(expectedError));
        });
    }

    [Test]
    public void CorrectXmlBindingAndOtherXmlStartingPrefixesRemainUsable()
    {
        string input = "<XMLdata:svg xmlns:xml='http://www.w3.org/XML/1998/namespace' xmlns:XMLdata='http://www.w3.org/2000/svg' xml:lang='en'/>";

        SvgLoadResult result = Load(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.XmlTokens[0].NamespaceUri, Is.EqualTo(SvgNamespaces.Svg));
            Assert.That(result.XmlTokens[0].Attributes[2].NamespaceUri, Is.EqualTo(SvgNamespaces.Xml));
        });
    }

    [TestCase("<svg a='1' a='2'/>", SvgXmlNamespaceError.DuplicateAttribute)]
    [TestCase("<svg xmlns:p='urn:same' xmlns:q='urn:same' p:a='1' q:a='2'/>", SvgXmlNamespaceError.DuplicateAttribute)]
    [TestCase("<svg xmlns:p='one' xmlns:p='two'/>", SvgXmlNamespaceError.DuplicateAttribute)]
    [TestCase("<svg xmlns='one' xmlns='two'/>", SvgXmlNamespaceError.DuplicateAttribute)]
    public void DuplicateAttributesAreDetectedByExpandedName(string input, SvgXmlNamespaceError expectedError)
    {
        SvgXmlNamespaceResult resolved = Resolve(input);

        Assert.Multiple(() =>
        {
            Assert.That(resolved.Success, Is.False);
            Assert.That(resolved.Error, Is.EqualTo(expectedError));
        });
    }

    [Test]
    public void SameLocalAttributesInDifferentNamespacesAreDistinctAndDefaultDoesNotApplyToAttributes()
    {
        string input = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:p='urn:one' xmlns:q='urn:two' a='0' p:a='1' q:a='2'/>";

        SvgLoadResult result = Load(input);
        SvgXmlToken element = result.XmlTokens[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(element.NamespaceUri, Is.EqualTo(SvgNamespaces.Svg));
            Assert.That(element.Attributes[3].NamespaceUri, Is.Empty);
            Assert.That(element.Attributes[4].NamespaceUri, Is.EqualTo("urn:one"));
            Assert.That(element.Attributes[5].NamespaceUri, Is.EqualTo("urn:two"));
        });
    }

    [Test]
    public void LoaderMapsNamespaceFailuresToLocatedXmlNamespaceDiagnostics()
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = "memory:namespaces";
        string input = "<svg>\r\n  <p:item/>\r\n</svg>";

        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, options);
        int expectedOffset = input.IndexOf("p:item", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.XmlNamespace));
            Assert.That(result.Diagnostics[0].Source.Source.Uri, Is.EqualTo("memory:namespaces"));
            Assert.That(result.Diagnostics[0].Source.StartOffset, Is.EqualTo(expectedOffset));
            Assert.That(result.Diagnostics[0].Source.StartLine, Is.EqualTo(2));
            Assert.That(result.Diagnostics[0].Source.StartColumn, Is.EqualTo(4));
        });
    }

    [Test]
    public void ResolverRequiresValidatedArgumentsAndNormalizedNamespaceValues()
    {
        List<SvgXmlToken> empty = new List<SvgXmlToken>();
        SvgXmlNamespaceResult nullSource = new SvgXmlNamespaceResolver().Resolve(null, empty);
        SvgXmlNamespaceResult nullTokens = new SvgXmlNamespaceResolver().Resolve("", null);

        string input = "<svg xmlns:p='urn:p'/>";
        SvgXmlTokenizeResult tokenized = new SvgXmlTokenizer().Tokenize(input, 16, 128);
        SvgXmlNamespaceResult unvalidated = new SvgXmlNamespaceResolver().Resolve(input, tokenized.Tokens);

        Assert.Multiple(() =>
        {
            Assert.That(nullSource.Error, Is.EqualTo(SvgXmlNamespaceError.InvalidArgument));
            Assert.That(nullTokens.Error, Is.EqualTo(SvgXmlNamespaceError.InvalidArgument));
            Assert.That(unvalidated.Error, Is.EqualTo(SvgXmlNamespaceError.InvalidArgument));
        });
    }

    static SvgXmlNamespaceResult Resolve(string input)
    {
        SvgXmlTokenizeResult tokenized = new SvgXmlTokenizer().Tokenize(input, 128, 2048);
        SvgXmlValidationResult validated = new SvgXmlValidator().Validate(input, tokenized.Tokens, 256);
        Assert.That(tokenized.Success, Is.True);
        Assert.That(validated.Success, Is.True);
        return new SvgXmlNamespaceResolver().Resolve(input, tokenized.Tokens);
    }

    static SvgLoadResult Load(string input)
    {
        return new SvgLoader().LoadWithStringAndSvgLoadOptions(input, new SvgLoadOptions());
    }
}
