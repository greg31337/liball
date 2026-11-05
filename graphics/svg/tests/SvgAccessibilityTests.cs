namespace svg.Tests;

public class SvgAccessibilityTests
{
    [Test]
    public void AriaRelationshipsOverrideLabelsTitlesAndDescriptions()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'>" +
            "<text id='first'>First</text><g id='second'><text>Second</text></g><text id='details'>Detailed text</text>" +
            "<rect id='target' aria-labelledby='first second' aria-label='Ignored'><title>Title</title><desc>Fallback description</desc></rect>" +
            "<circle id='described' aria-describedby='details'><desc>Ignored description</desc></circle></svg>";
        SvgResolvedDocument document = Resolve(source);

        SvgAccessibleSemantics named = Semantics(document, "target");
        SvgAccessibleSemantics described = Semantics(document, "described");

        Assert.Multiple(() =>
        {
            Assert.That(named.Success, Is.True);
            Assert.That(named.Name, Is.EqualTo("First Second"));
            Assert.That(named.NameSource, Is.EqualTo(SvgAccessibleNameSource.AriaLabelledBy));
            Assert.That(named.NameReferences.Select(Id), Is.EqualTo(new[] { "first", "second" }));
            Assert.That(named.Description, Is.EqualTo("Fallback description"));
            Assert.That(named.DescriptionSource, Is.EqualTo(SvgAccessibleDescriptionSource.Description));
            Assert.That(described.Description, Is.EqualTo("Detailed text"));
            Assert.That(described.DescriptionSource, Is.EqualTo(SvgAccessibleDescriptionSource.AriaDescribedBy));
            Assert.That(described.DescriptionReferences.Single(), Is.SameAs(document.SourceDocument.Index.GetById("details")));
        });
    }

    [Test]
    public void AriaLabelPrecedesTitleAndTitleDescProvideFallbacks()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect id='label' aria-label='  Labelled   shape '><title>Ignored</title></rect><circle id='fallback'><title>Circle title</title><desc>Circle description</desc></circle></svg>";
        SvgResolvedDocument document = Resolve(source);

        SvgAccessibleSemantics labelled = Semantics(document, "label");
        SvgAccessibleSemantics fallback = Semantics(document, "fallback");

        Assert.Multiple(() =>
        {
            Assert.That(labelled.Name, Is.EqualTo("Labelled shape"));
            Assert.That(labelled.NameSource, Is.EqualTo(SvgAccessibleNameSource.AriaLabel));
            Assert.That(fallback.Name, Is.EqualTo("Circle title"));
            Assert.That(fallback.NameSource, Is.EqualTo(SvgAccessibleNameSource.Title));
            Assert.That(fallback.Description, Is.EqualTo("Circle description"));
            Assert.That(fallback.DescriptionSource, Is.EqualTo(SvgAccessibleDescriptionSource.Description));
        });
    }

    [Test]
    public void ExplicitAndImplicitRolesAreMappedDeterministically()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' id='root'><a id='link' href='next.svg'/><rect id='shape'/><circle id='explicit' role='unknown button'/><g id='invalid' role='madeup'/></svg>";
        SvgResolvedDocument document = Resolve(source);

        SvgAccessibleSemantics root = new SvgAccessibility().ResolveWithNode(document.Root);
        SvgAccessibleSemantics link = Semantics(document, "link");
        SvgAccessibleSemantics shape = Semantics(document, "shape");
        SvgAccessibleSemantics explicitRole = Semantics(document, "explicit");
        SvgAccessibleSemantics invalid = Semantics(document, "invalid");

        Assert.Multiple(() =>
        {
            Assert.That(root.Role, Is.EqualTo("graphics-document"));
            Assert.That(link.Role, Is.EqualTo("link"));
            Assert.That(shape.Role, Is.EqualTo("graphics-symbol"));
            Assert.That(explicitRole.Role, Is.EqualTo("button"));
            Assert.That(explicitRole.ExplicitRole, Is.True);
            Assert.That(invalid.Role, Is.EqualTo("group"));
            Assert.That(invalid.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InvalidValue));
        });
    }

    [Test]
    public void AriaStatesAndPropertiesAreNormalizedAndValidated()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect id='target' aria-checked='MiXeD' aria-expanded='TRUE' aria-valuenow='5.5' aria-level='2' aria-live='POLITE' aria-orientation='diagonal' aria-controls='one two'/></svg>";
        SvgAccessibleSemantics result = Semantics(Resolve(source), "target");

        Assert.Multiple(() =>
        {
            Assert.That(Value(result.States, "aria-checked").Value, Is.EqualTo("mixed"));
            Assert.That(Value(result.States, "aria-expanded").Value, Is.EqualTo("true"));
            Assert.That(Value(result.Properties, "aria-valuenow").Value, Is.EqualTo("5.5"));
            Assert.That(Value(result.Properties, "aria-level").Value, Is.EqualTo("2"));
            Assert.That(Value(result.Properties, "aria-live").Value, Is.EqualTo("polite"));
            Assert.That(Value(result.Properties, "aria-controls").Value, Is.EqualTo("one two"));
            Assert.That(Value(result.Properties, "aria-orientation").Valid, Is.False);
            Assert.That(result.Diagnostics.Count(item => item.Code == SvgDiagnosticCode.InvalidValue), Is.EqualTo(1));
        });
    }

    [Test]
    public void RenderingAndAriaHiddenExcludeSemanticsThroughAncestors()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><g aria-hidden='true'><rect id='aria'><title>Hidden title</title></rect></g><g display='none'><circle id='display'><title>Hidden circle</title></circle></g><rect id='visible'><title>Visible title</title></rect></svg>";
        SvgResolvedDocument document = Resolve(source);

        SvgAccessibleSemantics aria = Semantics(document, "aria");
        SvgAccessibleSemantics display = Semantics(document, "display");
        SvgAccessibleSemantics visible = Semantics(document, "visible");

        Assert.Multiple(() =>
        {
            Assert.That(aria.Hidden, Is.True);
            Assert.That(aria.Name, Is.Empty);
            Assert.That(display.Hidden, Is.True);
            Assert.That(display.Name, Is.Empty);
            Assert.That(visible.Hidden, Is.False);
            Assert.That(visible.Name, Is.EqualTo("Visible title"));
        });
    }

    [Test]
    public void PresentationRemovesSemanticsButIsIgnoredForFocusableOrNamedNodes()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect id='plain' role='presentation'><title>Plain</title></rect><circle id='focusable' role='none' tabindex='0'><title>Focusable</title></circle><path id='named' role='presentation' aria-label='Named'/></svg>";
        SvgResolvedDocument document = Resolve(source);

        SvgAccessibleSemantics plain = Semantics(document, "plain");
        SvgAccessibleSemantics focusable = Semantics(document, "focusable");
        SvgAccessibleSemantics named = Semantics(document, "named");

        Assert.Multiple(() =>
        {
            Assert.That(plain.Presentational, Is.True);
            Assert.That(plain.Role, Is.EqualTo("presentation"));
            Assert.That(plain.Name, Is.Empty);
            Assert.That(focusable.Presentational, Is.False);
            Assert.That(focusable.Focusable, Is.True);
            Assert.That(focusable.Role, Is.EqualTo("graphics-symbol"));
            Assert.That(focusable.Name, Is.EqualTo("Focusable"));
            Assert.That(focusable.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InvalidValue), Is.True);
            Assert.That(named.Presentational, Is.False);
            Assert.That(named.Role, Is.EqualTo("graphics-symbol"));
            Assert.That(named.Name, Is.EqualTo("Named"));
        });
    }

    [Test]
    public void MissingAndCyclicRelationshipsProduceRecoverableDiagnostics()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><g id='a' aria-labelledby='b missing'/><g id='b' aria-labelledby='a'/></svg>";
        SvgAccessibleSemantics result = Semantics(Resolve(source), "a");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Name, Is.Empty);
            Assert.That(result.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InvalidReference && item.Message.Contains("cyclic")), Is.True);
            Assert.That(result.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InvalidReference && item.Message.Contains("missing")), Is.True);
        });
    }

    [Test]
    public void TextAndReferenceLimitsReturnBoundedFailures()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><text id='one'>One</text><text id='two'>Two</text><rect id='target' aria-labelledby='one two'><title>Long title</title></rect></svg>";
        SvgResolvedDocument document = Resolve(source);
        SvgAccessibilityOptions referenceLimit = new SvgAccessibilityOptions();
        referenceLimit.MaximumReferences = 1;
        SvgAccessibilityOptions textLimit = new SvgAccessibilityOptions();
        textLimit.MaximumTextCharacters = 2;

        SvgAccessibleSemantics references = new SvgAccessibility().ResolveWithNodeAndOptions(document.FindSource(document.SourceDocument.Index.GetById("target")), referenceLimit);
        SvgAccessibleSemantics text = new SvgAccessibility().ResolveWithNodeAndOptions(document.FindSource(document.SourceDocument.Index.GetById("target")), textLimit);

        Assert.Multiple(() =>
        {
            Assert.That(references.Success, Is.False);
            Assert.That(references.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InputLimitExceeded), Is.True);
            Assert.That(text.Success, Is.False);
            Assert.That(text.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InputLimitExceeded), Is.True);
        });
    }

    static SvgAccessibilityValue Value(List<SvgAccessibilityValue> values, string name)
    {
        return values.Single(item => item.Name == name);
    }

    static SvgAccessibleSemantics Semantics(SvgResolvedDocument document, string id)
    {
        SvgNode source = document.SourceDocument.Index.GetById(id);
        Assert.That(source, Is.Not.Null);
        SvgResolvedNode node = document.FindSource(source);
        Assert.That(node, Is.Not.Null);
        return new SvgAccessibility().ResolveWithNode(node);
    }

    static SvgResolvedDocument Resolve(string source)
    {
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(loaded.Success, Is.True);
        SvgResolvedDocument result = new SvgDocumentResolver().Resolve(loaded.Document);
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result;
    }

    static string Id(SvgNode node)
    {
        if (node == null) return "";
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == "id") return attribute.Value;
            index = index + 1;
        }
        return "";
    }
}
