namespace svg.Tests;

public class SvgXmlLoaderTests
{
    [Test]
    public void LoaderConstructsEveryRetainedNodeKindWithOrderedLinksAndSourceData()
    {
        string input = "<?xml version='1.0'?>\n<!--before--><?edit keep?>" +
            "<svg xmlns='http://www.w3.org/2000/svg' id='root'>a&amp;<![CDATA[b]]><g x='1'/><!--inside--></svg><!--after-->";
        SvgLoadOptions options = new SvgLoadOptions();
        options.BaseUri = "memory:document";

        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, options);
        SvgDocument document = result.Document;
        SvgNode root = document.DocumentElement;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(document.BaseUri, Is.EqualTo("memory:document"));
            Assert.That(document.Source.Uri, Is.EqualTo("memory:document"));
            Assert.That(document.SourceText, Is.SameAs(input));
            Assert.That(document.SourceSpan.StartOffset, Is.Zero);
            Assert.That(document.SourceSpan.EndOffset, Is.EqualTo(input.Length));
            Assert.That(document.DocumentNode.Children.Select(node => node.Kind), Is.EqualTo(new[]
            {
                SvgNodeKind.Text,
                SvgNodeKind.Comment,
                SvgNodeKind.ProcessingInstruction,
                SvgNodeKind.Element,
                SvgNodeKind.Comment
            }));
            Assert.That(root.QualifiedName, Is.EqualTo("svg"));
            Assert.That(root.NamespaceUri, Is.EqualTo(SvgNamespaces.Svg));
            Assert.That(root.Parent, Is.SameAs(document.DocumentNode));
            Assert.That(root.OwnerDocument, Is.SameAs(document));
            Assert.That(root.Attributes.Select(attribute => attribute.QualifiedName), Is.EqualTo(new[] { "xmlns", "id" }));
            Assert.That(root.Attributes, Has.All.Matches<SvgAttribute>(attribute => ReferenceEquals(attribute.OwnerElement, root) && ReferenceEquals(attribute.OwnerDocument, document)));
            Assert.That(root.Children.Select(node => node.Kind), Is.EqualTo(new[]
            {
                SvgNodeKind.Text,
                SvgNodeKind.Text,
                SvgNodeKind.CData,
                SvgNodeKind.Element,
                SvgNodeKind.Comment
            }));
            Assert.That(root.Children.Select(node => node.Value), Is.EqualTo(new[] { "a", "&", "b", "", "inside" }));
            Assert.That(root.RawToken.Kind, Is.EqualTo(SvgXmlTokenKind.StartElement));
            Assert.That(root.EndToken.Kind, Is.EqualTo(SvgXmlTokenKind.EndElement));
            Assert.That(root.SourceSpan.StartOffset, Is.EqualTo(input.IndexOf("<svg", StringComparison.Ordinal)));
            Assert.That(root.SourceSpan.EndOffset, Is.EqualTo(input.IndexOf("</svg>", StringComparison.Ordinal) + 6));
            Assert.That(document.IsDirty, Is.False);
            Assert.That(document.Version, Is.Zero);
            Assert.That(root.IsDirty, Is.False);
        });
    }

    [Test]
    public void DtdsAreProhibitedByDefaultAndOptInIsPreserveOnly()
    {
        string input = "<!DOCTYPE svg SYSTEM 'https://example.invalid/external.dtd'><svg/>";
        SvgLoadResult prohibited = Load(input);
        SvgLoadOptions preserveOptions = new SvgLoadOptions();
        preserveOptions.DtdPolicy = SvgDtdPolicy.PreserveOnly;
        SvgLoadResult preserved = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, preserveOptions);
        SvgNode doctype = preserved.Document.DocumentNode.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(prohibited.Success, Is.False);
            Assert.That(prohibited.Document, Is.Null);
            Assert.That(prohibited.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.MalformedXml));
            Assert.That(prohibited.Diagnostics[0].Source.StartOffset, Is.Zero);
            Assert.That(prohibited.Diagnostics[0].Message, Does.Contain("prohibited"));
            Assert.That(preserved.Success, Is.True);
            Assert.That(doctype.Kind, Is.EqualTo(SvgNodeKind.Doctype));
            Assert.That(doctype.QualifiedName, Is.EqualTo("svg"));
            Assert.That(doctype.Value, Does.Contain("SYSTEM"));
            Assert.That(doctype.RawToken.RawText, Is.EqualTo("<!DOCTYPE svg SYSTEM 'https://example.invalid/external.dtd'>"));
            Assert.That(preserved.Document.DocumentElement.QualifiedName, Is.EqualTo("svg"));
        });
    }

    [Test]
    public void PreserveOnlyNeverExpandsDeclaredEntityReferences()
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.DtdPolicy = SvgDtdPolicy.PreserveOnly;
        string input = "<!DOCTYPE svg [<!ENTITY secret 'expanded'>]><svg>&secret;</svg>";

        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(input, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Document, Is.Null);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.MalformedXml));
            Assert.That(result.Diagnostics[0].Message, Does.Contain("not expanded"));
        });
    }

    [Test]
    public void DefaultDtdPolicyWinsBeforeLaterEntityValidation()
    {
        string input = "<!DOCTYPE svg [<!ENTITY secret 'expanded'>]><svg>&secret;</svg>";

        SvgLoadResult result = Load(input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Source.StartOffset, Is.Zero);
            Assert.That(result.Diagnostics[0].Message, Does.Contain("prohibited"));
            Assert.That(result.Diagnostics[0].Message, Does.Not.Contain("expanded"));
        });
    }

    [TestCase("</svg>", SvgXmlBuildError.UnexpectedEndElement)]
    [TestCase("<svg></g>", SvgXmlBuildError.MismatchedEndElement)]
    [TestCase("<svg>", SvgXmlBuildError.UnclosedElement)]
    [TestCase("<svg/><g/>", SvgXmlBuildError.MultipleDocumentElements)]
    [TestCase("text<svg/>", SvgXmlBuildError.CharacterDataOutsideElement)]
    [TestCase("<svg/>text", SvgXmlBuildError.CharacterDataOutsideElement)]
    [TestCase("", SvgXmlBuildError.MissingDocumentElement)]
    public void StructuralXmlFailuresAreReportedWithoutPublishingPartialDocuments(string input, SvgXmlBuildError expectedError)
    {
        SvgXmlBuildResult built = Build(input, new SvgLoadOptions());
        SvgLoadResult loaded = Load(input);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.False);
            Assert.That(built.Error, Is.EqualTo(expectedError));
            Assert.That(built.Document, Is.Not.Null);
            Assert.That(loaded.Success, Is.False);
            Assert.That(loaded.Document, Is.Null);
            Assert.That(loaded.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.MalformedXml));
        });
    }

    [Test]
    public void DoctypeMultiplicityAndPlacementAreCheckedInPreserveMode()
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.DtdPolicy = SvgDtdPolicy.PreserveOnly;

        SvgXmlBuildResult multiple = Build("<!DOCTYPE svg><!DOCTYPE svg><svg/>", options);
        SvgXmlBuildResult misplaced = Build("<svg/><!DOCTYPE svg>", options);

        Assert.Multiple(() =>
        {
            Assert.That(multiple.Error, Is.EqualTo(SvgXmlBuildError.MultipleDoctypes));
            Assert.That(misplaced.Error, Is.EqualTo(SvgXmlBuildError.MisplacedDoctype));
        });
    }

    [Test]
    public void EveryTreeBudgetStopsConstructionAtItsConfiguredBoundary()
    {
        SvgLoadOptions depthOptions = new SvgLoadOptions();
        depthOptions.Limits.Tree.MaxDepth = 2;
        SvgLoadOptions nodeOptions = new SvgLoadOptions();
        nodeOptions.Limits.Tree.MaxNodes = 2;
        SvgLoadOptions elementAttributeOptions = new SvgLoadOptions();
        elementAttributeOptions.Limits.Tree.MaxAttributesPerElement = 1;
        SvgLoadOptions totalAttributeOptions = new SvgLoadOptions();
        totalAttributeOptions.Limits.Tree.MaxTotalAttributes = 1;
        SvgLoadOptions textOptions = new SvgLoadOptions();
        textOptions.Limits.Tree.MaxTextCharacters = 3;

        SvgXmlBuildResult depth = Build("<a><b><c/></b></a>", depthOptions);
        SvgXmlBuildResult nodes = Build("<a><b/><c/></a>", nodeOptions);
        SvgXmlBuildResult perElement = Build("<a x='1' y='2'/>", elementAttributeOptions);
        SvgXmlBuildResult total = Build("<a x='1'><b y='2'/></a>", totalAttributeOptions);
        SvgXmlBuildResult text = Build("<a>abcd</a>", textOptions);

        Assert.Multiple(() =>
        {
            Assert.That(depth.Error, Is.EqualTo(SvgXmlBuildError.DepthLimitExceeded));
            Assert.That(depth.ErrorOffset, Is.EqualTo(6));
            Assert.That(nodes.Error, Is.EqualTo(SvgXmlBuildError.NodeLimitExceeded));
            Assert.That(nodes.ErrorOffset, Is.EqualTo(7));
            Assert.That(perElement.Error, Is.EqualTo(SvgXmlBuildError.ElementAttributeLimitExceeded));
            Assert.That(perElement.ErrorOffset, Is.EqualTo(9));
            Assert.That(total.Error, Is.EqualTo(SvgXmlBuildError.TotalAttributeLimitExceeded));
            Assert.That(total.ErrorOffset, Is.EqualTo(12));
            Assert.That(text.Error, Is.EqualTo(SvgXmlBuildError.TextLimitExceeded));
            Assert.That(text.ErrorOffset, Is.EqualTo(3));
        });
    }

    [Test]
    public void LoaderMapsAllTreeBudgetFailuresToInputLimitDiagnostics()
    {
        SvgLoadOptions depthOptions = new SvgLoadOptions();
        depthOptions.Limits.Tree.MaxDepth = 1;
        SvgLoadOptions attributeOptions = new SvgLoadOptions();
        attributeOptions.Limits.Tree.MaxAttributesPerElement = 1;
        SvgLoadOptions totalOptions = new SvgLoadOptions();
        totalOptions.Limits.Tree.MaxTotalAttributes = 1;
        SvgLoadOptions textOptions = new SvgLoadOptions();
        textOptions.Limits.Tree.MaxTextCharacters = 1;

        SvgLoadResult depth = new SvgLoader().LoadWithStringAndSvgLoadOptions("<a><b/></a>", depthOptions);
        SvgLoadResult attributes = new SvgLoader().LoadWithStringAndSvgLoadOptions("<a x='1' y='2'/>", attributeOptions);
        SvgLoadResult total = new SvgLoader().LoadWithStringAndSvgLoadOptions("<a x='1'><b y='2'/></a>", totalOptions);
        SvgLoadResult text = new SvgLoader().LoadWithStringAndSvgLoadOptions("<a>ab</a>", textOptions);

        Assert.Multiple(() =>
        {
            Assert.That(new[] { depth, attributes, total, text }, Has.All.Matches<SvgLoadResult>(result => !result.Success && result.Document == null));
            Assert.That(new[] { depth, attributes, total, text }.Select(result => result.Diagnostics[0].Code), Has.All.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
        });
    }

    [Test]
    public void DisablingSourcePreservationKeepsSemanticDataWithoutRawTokensOrSpans()
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.PreserveSource = false;
        options.BaseUri = "memory:no-source";

        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions("<svg id='one'>text</svg>", options);
        SvgDocument document = result.Document;
        SvgNode root = document.DocumentElement;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(document.Source.Uri, Is.EqualTo("memory:no-source"));
            Assert.That(document.SourceText, Is.Null);
            Assert.That(document.SourceSpan, Is.Null);
            Assert.That(document.DocumentNode.SourceSpan, Is.Null);
            Assert.That(root.RawToken, Is.Null);
            Assert.That(root.EndToken, Is.Null);
            Assert.That(root.SourceSpan, Is.Null);
            Assert.That(root.Attributes[0].RawToken, Is.Null);
            Assert.That(root.Attributes[0].SourceSpan, Is.Null);
            Assert.That(root.Attributes[0].Value, Is.EqualTo("one"));
            Assert.That(root.Children[0].Value, Is.EqualTo("text"));
        });
    }

    [Test]
    public void XmlReadersBuildEquivalentBoundedModelsAndRemainCallerOwned()
    {
        string input = "<svg xmlns='http://www.w3.org/2000/svg' id='one'><g/>text</svg>";
        System.IO.StringReader text = new System.IO.StringReader(input);
        System.Xml.XmlReader reader = System.Xml.XmlReader.Create(text);

        SvgLoadResult result = new SvgLoader().LoadWithXmlReaderAndSvgLoadOptions(reader, new SvgLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Document.DocumentElement.NamespaceUri, Is.EqualTo(SvgNamespaces.Svg));
            Assert.That(result.Document.DocumentElement.Attributes.Select(attribute => attribute.QualifiedName), Is.EqualTo(new[] { "xmlns", "id" }));
            Assert.That(result.Document.DocumentElement.Children.Select(node => node.Kind), Is.EqualTo(new[] { SvgNodeKind.Element, SvgNodeKind.Text }));
            Assert.That(result.Document.DocumentElement.RawToken, Is.Null);
            Assert.That(reader.ReadState, Is.EqualTo(System.Xml.ReadState.EndOfFile));
            Assert.That(text.Peek(), Is.EqualTo(-1));
        });

        reader.Dispose();
        text.Dispose();
    }

    [Test]
    public void XmlReaderConstructionEnforcesDtdAndTreePolicies()
    {
        System.Xml.XmlReaderSettings dtdSettings = new System.Xml.XmlReaderSettings();
        dtdSettings.DtdProcessing = System.Xml.DtdProcessing.Parse;
        dtdSettings.XmlResolver = null;
        System.Xml.XmlReader dtdReader = System.Xml.XmlReader.Create(new System.IO.StringReader("<!DOCTYPE svg><svg/>"), dtdSettings);
        SvgLoadResult dtd = new SvgLoader().LoadWithXmlReaderAndSvgLoadOptions(dtdReader, new SvgLoadOptions());

        SvgLoadOptions attributeOptions = new SvgLoadOptions();
        attributeOptions.Limits.Tree.MaxAttributesPerElement = 1;
        System.Xml.XmlReader attributeReader = System.Xml.XmlReader.Create(new System.IO.StringReader("<svg a='1' b='2'/>"));
        SvgLoadResult attributes = new SvgLoader().LoadWithXmlReaderAndSvgLoadOptions(attributeReader, attributeOptions);

        SvgLoadOptions nameOptions = new SvgLoadOptions();
        nameOptions.Limits.Tree.MaxNameCharacters = 3;
        System.Xml.XmlReader nameReader = System.Xml.XmlReader.Create(new System.IO.StringReader("<long/>"));
        SvgLoadResult name = new SvgLoader().LoadWithXmlReaderAndSvgLoadOptions(nameReader, nameOptions);

        Assert.Multiple(() =>
        {
            Assert.That(dtd.Success, Is.False);
            Assert.That(dtd.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.MalformedXml));
            Assert.That(attributes.Success, Is.False);
            Assert.That(attributes.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
            Assert.That(name.Success, Is.False);
            Assert.That(name.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
        });

        dtdReader.Dispose();
        attributeReader.Dispose();
        nameReader.Dispose();
    }

    [Test]
    public void InvalidDtdPolicyIsRejectedBeforeSourceProcessing()
    {
        SvgLoadOptions options = new SvgLoadOptions();
        options.DtdPolicy = (SvgDtdPolicy)999;

        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions("<svg/>", options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InvalidArgument));
            Assert.That(result.Diagnostics[0].Message, Does.Contain("DTD policy"));
        });
    }

    static SvgXmlBuildResult Build(string input, SvgLoadOptions options)
    {
        SvgXmlTokenizeResult tokenized = new SvgXmlTokenizer().Tokenize(input, 1024, 16384);
        SvgXmlValidationResult validated = new SvgXmlValidator().Validate(input, tokenized.Tokens, options.Limits.Tree.MaxNameCharacters);
        SvgXmlNamespaceResult namespaced = new SvgXmlNamespaceResolver().Resolve(input, tokenized.Tokens);
        Assert.That(tokenized.Success, Is.True);
        Assert.That(validated.Success, Is.True);
        Assert.That(namespaced.Success, Is.True);
        return new SvgXmlLoader().Build(input, tokenized.Tokens, options, "memory:test");
    }

    static SvgLoadResult Load(string input)
    {
        return new SvgLoader().LoadWithStringAndSvgLoadOptions(input, new SvgLoadOptions());
    }
}
