namespace svg.Tests;

public class SvgReferenceResolverTests
{
    [Test]
    public void UnnamespacedHrefWinsOverXLinkAndResolvesSameDocumentTarget()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink'><g id='modern'/><g id='legacy'/><use href='#modern' xlink:href='#legacy'/></svg>", "https://example.test/root.svg");
        SvgNode use = document.DocumentElement.Children[2];

        SvgReferenceResult result = new SvgReferenceResolver().ResolveWithNode(use);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(SvgReferenceStatus.Resolved));
            Assert.That(result.AttributeKind, Is.EqualTo(SvgReferenceAttributeKind.Href));
            Assert.That(result.SelectedAttribute, Is.SameAs(use.Attributes[0]));
            Assert.That(result.HrefAttribute, Is.SameAs(use.Attributes[0]));
            Assert.That(result.XLinkHrefAttribute, Is.SameAs(use.Attributes[1]));
            Assert.That(result.XLinkHrefIgnored, Is.True);
            Assert.That(result.Target, Is.SameAs(document.DocumentElement.Children[0]));
            Assert.That(result.IsSameDocument, Is.True);
            Assert.That(result.ResolvedUri, Is.EqualTo("https://example.test/root.svg#modern"));
            Assert.That(result.ResourceUri, Is.EqualTo("https://example.test/root.svg"));
        });
    }

    [Test]
    public void PresentEmptyHrefIsInvalidAndDoesNotFallBackToXLink()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink'><g id='legacy'/><use href='   ' xlink:href='#legacy'/></svg>", "");
        SvgNode use = document.DocumentElement.Children[1];

        SvgReferenceResult result = new SvgReferenceResolver().ResolveWithNode(use);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(SvgReferenceStatus.Invalid));
            Assert.That(result.AttributeKind, Is.EqualTo(SvgReferenceAttributeKind.Href));
            Assert.That(result.XLinkHrefIgnored, Is.True);
            Assert.That(result.Reference, Is.Null);
            Assert.That(result.Target, Is.Null);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidReference));
            Assert.That(result.Diagnostics.Single().Source, Is.SameAs(use.Attributes[0].SourceSpan));
        });
    }

    [Test]
    public void XLinkHrefIsUsedOnlyWhenUnnamespacedHrefIsAbsent()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink'><symbol id='icon'/><use xlink:href='#icon'/></svg>", "memory:root");
        SvgNode use = document.DocumentElement.Children[1];

        SvgReferenceResult result = new SvgReferenceResolver().ResolveWithNode(use);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.AttributeKind, Is.EqualTo(SvgReferenceAttributeKind.XLinkHref));
            Assert.That(result.HrefAttribute, Is.Null);
            Assert.That(result.XLinkHrefIgnored, Is.False);
            Assert.That(result.Target, Is.SameAs(document.DocumentElement.Children[0]));
            Assert.That(result.ReferenceSource, Is.SameAs(use.Attributes[0].SourceSpan));
        });
    }

    [Test]
    public void MissingReferenceCanBeOptionalOrRequired()
    {
        SvgNode group = Load("<svg xmlns='http://www.w3.org/2000/svg'><g/></svg>", "").DocumentElement.Children[0];
        SvgReferenceResolveOptions requiredOptions = new SvgReferenceResolveOptions();
        requiredOptions.RequireReference = true;

        SvgReferenceResult optional = new SvgReferenceResolver().ResolveWithNode(group);
        SvgReferenceResult required = new SvgReferenceResolver().ResolveWithNodeAndOptions(group, requiredOptions);

        Assert.Multiple(() =>
        {
            Assert.That(optional.Success, Is.True);
            Assert.That(optional.Status, Is.EqualTo(SvgReferenceStatus.Missing));
            Assert.That(optional.Diagnostics, Is.Empty);
            Assert.That(required.Success, Is.False);
            Assert.That(required.Status, Is.EqualTo(SvgReferenceStatus.Missing));
            Assert.That(required.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidReference));
        });
    }

    [Test]
    public void FragmentLookupDecodesPercentEscapesAndUsesFirstDuplicateId()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='a b'/><g id='a b'/><use href='#a%20b'/></svg>", "https://example.test/root.svg");
        SvgNode use = document.DocumentElement.Children[2];

        SvgReferenceResult result = new SvgReferenceResolver().ResolveWithNode(use);
        SvgNode direct = new SvgReferenceResolver().FindFragment(document, "#a%20b");
        SvgNode root = new SvgReferenceResolver().FindFragment(document, "");
        SvgNode invalid = new SvgReferenceResolver().FindFragment(document, "%GG");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Fragment, Is.EqualTo("a%20b"));
            Assert.That(result.FragmentIdentifier, Is.EqualTo("a b"));
            Assert.That(result.Target, Is.SameAs(document.DocumentElement.Children[0]));
            Assert.That(direct, Is.SameAs(document.DocumentElement.Children[0]));
            Assert.That(root, Is.SameAs(document.DocumentElement));
            Assert.That(invalid, Is.Null);
        });
    }

    [Test]
    public void XmlBaseChainResolvesRelativeExternalUriAndRetainsEverySource()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' xml:base='assets/'><g xml:base='../shared/'><use href='icons.svg#mark'/></g></svg>", "https://example.test/docs/root.svg");
        SvgNode use = document.DocumentElement.Children[0].Children[0];

        SvgReferenceResult result = new SvgReferenceResolver().ResolveWithNode(use);
        string nodeApiBase = new SvgNodeApi().ResolveXmlBase(use);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(SvgReferenceStatus.External));
            Assert.That(result.BaseUri, Is.EqualTo("https://example.test/docs/shared/"));
            Assert.That(nodeApiBase, Is.EqualTo(result.BaseUri));
            Assert.That(result.ResolvedUri, Is.EqualTo("https://example.test/docs/shared/icons.svg#mark"));
            Assert.That(result.ResourceUri, Is.EqualTo("https://example.test/docs/shared/icons.svg"));
            Assert.That(result.IsSameDocument, Is.False);
            Assert.That(result.TargetDocument, Is.Null);
            Assert.That(result.Target, Is.Null);
            Assert.That(result.BaseProvenance, Is.EqualTo(new[] { document.DocumentElement.Attributes.Single(attribute => attribute.NamespaceUri == SvgNamespaces.Xml && attribute.LocalName == SvgAttributeNames.Base).SourceSpan, document.DocumentElement.Children[0].Attributes[0].SourceSpan }));
            Assert.That(result.BaseSource, Is.SameAs(document.DocumentElement.Children[0].Attributes[0].SourceSpan));
        });
    }

    [Test]
    public void RelativeSelfReferenceAndFragmentlessSelfReferenceResolveLocally()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='target'/><use href='root.svg#target'/><use href='root.svg'/><use href='HTTPS://EXAMPLE.TEST/docs/root.svg#target'/></svg>", "https://example.test/docs/root.svg");

        SvgReferenceResult fragment = new SvgReferenceResolver().ResolveWithNode(document.DocumentElement.Children[1]);
        SvgReferenceResult wholeDocument = new SvgReferenceResolver().ResolveWithNode(document.DocumentElement.Children[2]);
        SvgReferenceResult caseNormalized = new SvgReferenceResolver().ResolveWithNode(document.DocumentElement.Children[3]);

        Assert.Multiple(() =>
        {
            Assert.That(fragment.Success, Is.True);
            Assert.That(fragment.IsSameDocument, Is.True);
            Assert.That(fragment.Target, Is.SameAs(document.DocumentElement.Children[0]));
            Assert.That(wholeDocument.Success, Is.True);
            Assert.That(wholeDocument.IsSameDocument, Is.True);
            Assert.That(wholeDocument.HasFragment, Is.False);
            Assert.That(wholeDocument.Target, Is.SameAs(document.DocumentElement));
            Assert.That(caseNormalized.Success, Is.True);
            Assert.That(caseNormalized.IsSameDocument, Is.True);
            Assert.That(caseNormalized.Target, Is.SameAs(document.DocumentElement.Children[0]));
        });
    }

    [Test]
    public void CallerSuppliedExternalDocumentProvidesRootOrFragmentTarget()
    {
        SvgDocument source = Load("<svg xmlns='http://www.w3.org/2000/svg'><use href='https://cdn.example.test/icons.svg#mark'/><use href='https://cdn.example.test/icons.svg'/></svg>", "https://example.test/root.svg");
        SvgDocument external = Load("<svg xmlns='http://www.w3.org/2000/svg'><symbol id='mark'/></svg>", "https://cdn.example.test/icons.svg");
        SvgReferenceResolveOptions options = new SvgReferenceResolveOptions();
        options.ExternalDocument = external;

        SvgReferenceResult fragment = new SvgReferenceResolver().ResolveWithNodeAndOptions(source.DocumentElement.Children[0], options);
        SvgReferenceResult root = new SvgReferenceResolver().ResolveWithNodeAndOptions(source.DocumentElement.Children[1], options);

        Assert.Multiple(() =>
        {
            Assert.That(fragment.Success, Is.True);
            Assert.That(fragment.Status, Is.EqualTo(SvgReferenceStatus.Resolved));
            Assert.That(fragment.TargetDocument, Is.SameAs(external));
            Assert.That(fragment.Target, Is.SameAs(external.DocumentElement.Children[0]));
            Assert.That(fragment.TargetSource, Is.SameAs(external.DocumentElement.Children[0].SourceSpan));
            Assert.That(fragment.TargetProvenance, Has.Count.EqualTo(1));
            Assert.That(root.Success, Is.True);
            Assert.That(root.Target, Is.SameAs(external.DocumentElement));
        });
    }

    [Test]
    public void MissingFragmentAndMalformedUrisProduceLocatedFailures()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><use href='#missing'/><use href='bad%2G'/><g xml:base='bad path/'><use href='#x'/></g></svg>", "https://example.test/root.svg");

        SvgReferenceResult missing = new SvgReferenceResolver().ResolveWithNode(document.DocumentElement.Children[0]);
        SvgReferenceResult malformed = new SvgReferenceResolver().ResolveWithNode(document.DocumentElement.Children[1]);
        SvgReferenceResult badBase = new SvgReferenceResolver().ResolveWithNode(document.DocumentElement.Children[2].Children[0]);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Status, Is.EqualTo(SvgReferenceStatus.TargetNotFound));
            Assert.That(missing.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidReference));
            Assert.That(malformed.Success, Is.False);
            Assert.That(malformed.Status, Is.EqualTo(SvgReferenceStatus.Invalid));
            Assert.That(malformed.Diagnostics.Single().Source, Is.SameAs(document.DocumentElement.Children[1].Attributes[0].SourceSpan));
            Assert.That(badBase.Success, Is.False);
            Assert.That(badBase.Status, Is.EqualTo(SvgReferenceStatus.Invalid));
            Assert.That(badBase.Diagnostics.Single().Source, Is.SameAs(document.DocumentElement.Children[2].Attributes[0].SourceSpan));
            Assert.That(badBase.Diagnostics.Single().RelatedSpans.Single(), Is.SameAs(document.DocumentElement.Children[2].Children[0].Attributes[0].SourceSpan));
        });
    }

    [Test]
    public void UriBoundsDistinguishAuthoredAndResolvedLimitFailures()
    {
        SvgDocument authoredDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><use href='123456789'/></svg>", "");
        SvgDocument resolvedDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><use href='b'/></svg>", "abcdefg/");
        SvgReferenceResolveOptions options = new SvgReferenceResolveOptions();
        options.MaxUriCharacters = 8;

        SvgReferenceResult authored = new SvgReferenceResolver().ResolveWithNodeAndOptions(authoredDocument.DocumentElement.Children[0], options);
        SvgReferenceResult resolved = new SvgReferenceResolver().ResolveWithNodeAndOptions(resolvedDocument.DocumentElement.Children[0], options);

        Assert.Multiple(() =>
        {
            Assert.That(authored.Success, Is.False);
            Assert.That(authored.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(resolved.Success, Is.False);
            Assert.That(resolved.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(resolved.BaseUri, Is.EqualTo("abcdefg/"));
        });
    }

    [Test]
    public void BaseAncestryIsBoundedForEditedOrAdversarialTrees()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><use href='#target'/></g><g id='target'/></svg>", "");
        SvgReferenceResolveOptions options = new SvgReferenceResolveOptions();
        options.MaxBaseDepth = 2;

        SvgReferenceResult result = new SvgReferenceResolver().ResolveWithNodeAndOptions(document.DocumentElement.Children[0].Children[0], options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(SvgReferenceStatus.Invalid));
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(result.Diagnostics.Single().Source, Is.SameAs(document.DocumentElement.Children[0].Children[0].Attributes[0].SourceSpan));
        });
    }

    [Test]
    public void ReferenceProvenanceIncludesNestedResourceRequestChain()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='target'/><use href='#target'/></svg>", "memory:root");
        SvgAttribute href = document.DocumentElement.Children[1].Attributes[0];
        SvgSourceUri rootSource = new SvgSourceUri("memory:root");
        SvgSourceSpan requestSpan = new SvgSourceSpan(rootSource, 10, 1, 11, 20, 1, 21);
        SvgSourceUri nestedSource = SvgSourceUri.CreateNested("memory:nested", rootSource, requestSpan, 3);
        SvgSourceSpan nestedSpan = new SvgSourceSpan(nestedSource, 2, 1, 3, 9, 1, 10);
        href.SourceSpan = nestedSpan;

        SvgReferenceResult result = new SvgReferenceResolver().ResolveWithNode(document.DocumentElement.Children[1]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ReferenceSource, Is.SameAs(nestedSpan));
            Assert.That(result.ReferenceProvenance, Is.EqualTo(new[] { nestedSpan, requestSpan }));
            Assert.That(result.ReferenceProvenance[1].Source, Is.SameAs(rootSource));
        });
    }

    static SvgDocument Load(string source, string baseUri)
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = baseUri;
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, options);
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
