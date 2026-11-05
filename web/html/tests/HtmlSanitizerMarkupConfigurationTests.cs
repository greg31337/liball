
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSanitizerMarkupConfigurationTests
{
    const string HtmlNamespaceUri = "http://www.w3.org/1999/xhtml";
    const string SvgNamespaceUri = "http://www.w3.org/2000/svg";

    [Test]
    public void DataAndAriaWildcardsKeepOnlyNonemptyNamesAndHonorBlocks()
    {
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        HtmlNode element = CreateElement("div",
        [
            HtmlAttribute.Create("data-id", "42", span),
            HtmlAttribute.Create("aria-label", "answer", span),
            HtmlAttribute.Create("data-", "empty", span),
            HtmlAttribute.Create("aria-", "empty", span),
            HtmlAttribute.Create("data-secret", "hidden", span),
            HtmlAttribute.Create("x-data", "other", span)
        ]);
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("div");
        builder.SetAllowDataAttributes(true);
        builder.SetAllowAriaAttributes(true);
        builder.BlockAttributeOnElement("data-secret", "div");
        HtmlCompiledSanitizerPolicy policy = builder.Compile();

        HtmlSanitizerAttributes.Apply(element, policy);

        Assert.Multiple(() =>
        {
            Assert.That(element.GetAttributesStorage()
                .Select(value => value.GetName()),
                Is.EqualTo(new[] { "data-id", "aria-label" }));
            Assert.That(policy.IsAttributeAllowed("data-id", "div"), Is.True);
            Assert.That(policy.IsAttributeAllowed("aria-label", "div"),
                Is.True);
            Assert.That(policy.IsAttributeAllowed("data-", "div"), Is.False);
            Assert.That(policy.IsAttributeAllowed("aria-", "div"), Is.False);
            Assert.That(policy.IsAttributeAllowed("data-secret", "div"),
                Is.False);
        });
    }

    [Test]
    public void CustomTemplateAndForeignElementsRequireExplicitOptIns()
    {
        HtmlSanitizerPolicy defaultsBuilder = new HtmlSanitizerPolicy();
        HtmlCompiledSanitizerPolicy defaults = defaultsBuilder.Compile();
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.SetAllowCustomElements(true);
        builder.SetAllowTemplateElements(true);
        builder.AllowForeignNamespace(SvgNamespaceUri);
        builder.BlockElement("blocked-widget",
            HtmlBlockedElementAction.DropSubtree);
        builder.BlockElementInNamespace(SvgNamespaceUri, "script",
            HtmlBlockedElementAction.DropSubtree);
        HtmlCompiledSanitizerPolicy enabled = builder.Compile();
        HtmlDocument document = CreateDocument();
        HtmlNode custom = document.CreateElement("local-widget");
        HtmlNode template = document.CreateElement("template");
        HtmlNode foreign = document.CreateElementInNamespace(SvgNamespaceUri, "circle");

        Assert.Multiple(() =>
        {
            Assert.That(defaults.IsElementAllowed("local-widget"), Is.False);
            Assert.That(defaults.IsElementAllowed("template"), Is.False);
            Assert.That(defaults.IsElementAllowedInNamespace(SvgNamespaceUri, "circle"),
                Is.False);
            Assert.That(enabled.IsElementAllowed("local-widget"), Is.True);
            Assert.That(enabled.IsElementAllowed("annotation-xml"), Is.False);
            Assert.That(enabled.IsElementAllowed("blocked-widget"), Is.False);
            Assert.That(enabled.IsElementAllowed("template"), Is.True);
            Assert.That(enabled.IsElementAllowedInNamespace(SvgNamespaceUri, "circle"),
                Is.True);
            Assert.That(enabled.IsElementAllowedInNamespace(SvgNamespaceUri, "script"),
                Is.False);
            Assert.That(enabled.IsForeignNamespaceAllowed(SvgNamespaceUri),
                Is.True);
            Assert.That(enabled.IsForeignNamespaceAllowed(HtmlNamespaceUri),
                Is.False);
            Assert.That(HtmlSanitizerElements.GetAction(custom, enabled),
                Is.EqualTo(HtmlSanitizerElementAction.Keep));
            Assert.That(HtmlSanitizerElements.GetAction(template, enabled),
                Is.EqualTo(HtmlSanitizerElementAction.Keep));
            Assert.That(HtmlSanitizerElements.GetAction(foreign, enabled),
                Is.EqualTo(HtmlSanitizerElementAction.Keep));
        });
    }

    [Test]
    public void CustomizedBuiltInValuesAreValidatedAndFeatureGated()
    {
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        HtmlSanitizerPolicy disabledBuilder = new HtmlSanitizerPolicy();
        disabledBuilder.AllowElement("button");
        disabledBuilder.AllowAttribute("is");
        HtmlNode disabled = CreateElement("button",
            [HtmlAttribute.Create("is", "safe-button", span)]);

        HtmlSanitizerAttributes.Apply(disabled, disabledBuilder.Compile());

        HtmlSanitizerPolicy enabledBuilder = new HtmlSanitizerPolicy();
        enabledBuilder.AllowElement("button");
        enabledBuilder.SetAllowCustomizedBuiltInElements(true);
        HtmlCompiledSanitizerPolicy enabledPolicy = enabledBuilder.Compile();
        HtmlNode valid = CreateElement("button",
            [HtmlAttribute.Create("is", "safe-button", span)]);
        HtmlNode invalid = CreateElement("button",
            [HtmlAttribute.Create("is", "Button", span)]);
        HtmlNode autonomous = CreateElement("local-widget",
            [HtmlAttribute.Create("is", "safe-button", span)]);
        enabledBuilder.AllowElement("local-widget");
        enabledPolicy = enabledBuilder.Compile();

        HtmlSanitizerAttributes.Apply(valid, enabledPolicy);
        HtmlSanitizerAttributes.Apply(invalid, enabledPolicy);
        HtmlSanitizerAttributes.Apply(autonomous, enabledPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(disabled.HasAttribute("is"), Is.False);
            Assert.That(valid.GetAttribute("is"), Is.EqualTo("safe-button"));
            Assert.That(invalid.HasAttribute("is"), Is.False);
            Assert.That(autonomous.HasAttribute("is"), Is.False);
            Assert.That(enabledPolicy.IsCustomizedBuiltInElementAllowed(
                HtmlNamespaceUri, "button", "safe-button"), Is.True);
            Assert.That(enabledPolicy.IsCustomizedBuiltInElementAllowed(
                HtmlNamespaceUri, "button", "Button"), Is.False);
        });
    }

    [Test]
    public void CommentAndDoctypeActionsDropByDefaultAndKeepWhenEnabled()
    {
        HtmlDocument droppedDocument = CreateDocument();
        HtmlNode droppedDoctype = CreateDoctype(droppedDocument);
        HtmlNode droppedComment = droppedDocument.CreateComment("note");
        droppedDocument.GetRoot().Append(droppedDoctype);
        droppedDocument.GetRoot().Append(droppedComment);
        HtmlCompiledSanitizerPolicy defaults =
            new HtmlSanitizerPolicy().Compile();

        HtmlSanitizerNodeAction doctypeAction = HtmlSanitizerNodes.Apply(
            droppedDoctype, defaults);
        HtmlSanitizerNodeAction commentAction = HtmlSanitizerNodes.Apply(
            droppedComment, defaults);

        HtmlDocument keptDocument = CreateDocument();
        HtmlNode keptDoctype = CreateDoctype(keptDocument);
        HtmlNode keptComment = keptDocument.CreateComment("note");
        HtmlNode unsafeDoctype = keptDocument.CreateDocumentTypeStorage(true,
            "html", true, "legacy", false, "", false,
            HtmlSourceSpan.CreateUnknown());
        keptDocument.GetRoot().Append(keptDoctype);
        keptDocument.GetRoot().Append(keptComment);
        HtmlSanitizerPolicy enabledBuilder = new HtmlSanitizerPolicy();
        enabledBuilder.SetAllowComments(true);
        enabledBuilder.SetAllowDoctype(true);
        HtmlCompiledSanitizerPolicy enabled = enabledBuilder.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(doctypeAction, Is.EqualTo(HtmlSanitizerNodeAction.Drop));
            Assert.That(commentAction, Is.EqualTo(HtmlSanitizerNodeAction.Drop));
            Assert.That(droppedDocument.GetRoot().GetChildren(), Is.Empty);
            Assert.That(HtmlSanitizerNodes.Apply(keptDoctype, enabled),
                Is.EqualTo(HtmlSanitizerNodeAction.Keep));
            Assert.That(HtmlSanitizerNodes.Apply(keptComment, enabled),
                Is.EqualTo(HtmlSanitizerNodeAction.Keep));
            Assert.That(HtmlSanitizerNodes.GetAction(unsafeDoctype, enabled),
                Is.EqualTo(HtmlSanitizerNodeAction.Drop));
            Assert.That(keptDocument.GetRoot().GetChildren().Length,
                Is.EqualTo(2));
            Assert.That(HtmlDomInvariant.FindViolation(keptDocument,
                keptDocument.GetRoot()), Is.Empty);
        });
    }

    [Test]
    public void ConfigurationCopiesAndCompiledSnapshotsAreIndependent()
    {
        HtmlSanitizerPolicy original = new HtmlSanitizerPolicy();
        original.SetAllowCustomElements(true);
        original.SetAllowComments(true);
        original.AllowForeignNamespace(SvgNamespaceUri);
        HtmlSanitizerPolicy copy = original.Copy();
        HtmlCompiledSanitizerPolicy compiled = original.Compile();
        string[] namespaces = compiled.GetAllowedForeignNamespaces();
        namespaces[0] = "changed";
        copy.SetAllowCustomElements(false);
        copy.SetAllowComments(false);
        copy.BlockForeignNamespace(SvgNamespaceUri);
        HtmlCompiledSanitizerPolicy changed = copy.Compile();
        HtmlSanitizerPolicy restored = compiled.CreatePolicy();

        Assert.Multiple(() =>
        {
            Assert.That(compiled.GetAllowCustomElements(), Is.True);
            Assert.That(compiled.GetAllowComments(), Is.True);
            Assert.That(compiled.GetAllowedForeignNamespaces(),
                Is.EqualTo(new[] { SvgNamespaceUri }));
            Assert.That(changed.GetAllowCustomElements(), Is.False);
            Assert.That(changed.GetAllowComments(), Is.False);
            Assert.That(changed.GetAllowedForeignNamespaces(), Is.Empty);
            Assert.That(restored.Compile().IsForeignNamespaceAllowed(
                SvgNamespaceUri), Is.True);
        });
    }

    [Test]
    public void InvalidForeignNamespaceConfigurationIsRejected()
    {
        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();
        HtmlSanitizerPolicy disabledCustomized = new HtmlSanitizerPolicy();
        disabledCustomized.AllowElement("button");
        disabledCustomized.ForceAttribute("button", "is", "safe-button");
        HtmlSanitizerPolicy invalidCustomized = new HtmlSanitizerPolicy();
        invalidCustomized.AllowElement("button");
        invalidCustomized.SetAllowCustomizedBuiltInElements(true);
        invalidCustomized.RequireAttribute("button", "is", "Button");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                policy.AllowForeignNamespace(null));
            Assert.Throws<ArgumentException>(() =>
                policy.AllowForeignNamespace(""));
            Assert.Throws<ArgumentException>(() =>
                policy.AllowForeignNamespace(HtmlNamespaceUri));
            Assert.Throws<ArgumentNullException>(() =>
                new HtmlSanitizerMarkupConfiguration(false, false, false,
                    false, false, false, null));
            Assert.Throws<InvalidOperationException>(() =>
                disabledCustomized.Compile());
            Assert.Throws<InvalidOperationException>(() =>
                invalidCustomized.Compile());
        });
    }

    static HtmlNode CreateElement(string localName,
        HtmlAttribute[] attributes)
    {
        HtmlDocument document = CreateDocument();
        return document.CreateElementStorage(HtmlNamespace.Html,
            HtmlNamespaceUri, localName, localName, "", attributes,
            HtmlSourceSpan.CreateUnknown());
    }

    static HtmlNode CreateDoctype(HtmlDocument document)
    {
        return document.CreateDocumentTypeStorage(true, "html", false, "",
            false, "", false, HtmlSourceSpan.CreateUnknown());
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("about:blank", "about:blank",
            HtmlDocumentMode.NoQuirks, Array.Empty<HtmlDiagnostic>(),
            HtmlSourceSpan.CreateUnknown());
    }
}
