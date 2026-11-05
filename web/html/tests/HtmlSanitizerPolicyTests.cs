
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSanitizerPolicyTests
{
    [Test]
    public void BuilderCompilesNormalizedConcretePolicyTables()
    {
        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();
        policy.AllowElement("P");
        policy.AllowElement("A");
        policy.AllowElement("DIV");
        policy.BlockElement("SCRIPT", HtmlBlockedElementAction.DropSubtree);
        policy.AllowAttribute("TITLE");
        policy.AllowAttributeOnElement("HREF", "A");
        policy.ForceAttribute("A", "REL", "noopener noreferrer");
        policy.AllowUrlScheme("HTTPS");
        policy.AllowUrlScheme("mailto");
        policy.AllowUrlHost("EXAMPLE.COM");
        policy.AllowCssProperty("COLOR");
        policy.SetAllowDataAttributes(true);
        policy.SetAllowAriaAttributes(true);
        policy.SetDomClobberPrefix("safe-");

        HtmlCompiledSanitizerPolicy compiled = policy.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(compiled.GetAllowedElements(),
                Is.EqualTo(new[] { "a", "div", "p" }));
            Assert.That(compiled.GetBlockedElements(),
                Is.EqualTo(new[] { "script" }));
            Assert.That(compiled.IsElementAllowed("DIV"), Is.True);
            Assert.That(compiled.IsElementBlocked("Script"), Is.True);
            Assert.That(compiled.GetBlockedElementAction("SCRIPT"),
                Is.EqualTo(HtmlBlockedElementAction.DropSubtree));
            Assert.That(compiled.IsAttributeAllowed("title", "p"), Is.True);
            Assert.That(compiled.IsAttributeAllowed("HREF", "A"), Is.True);
            Assert.That(compiled.IsAttributeAllowed("href", "p"), Is.False);
            Assert.That(compiled.GetAllowedAttributesForElement("a"),
                Is.EqualTo(new[] { "href" }));
            Assert.That(compiled.GetForcedAttributesForElement("A"),
                Is.EqualTo(new[] { "rel" }));
            Assert.That(compiled.HasForcedAttribute("a", "REL"), Is.True);
            Assert.That(compiled.GetForcedAttributeValue("A", "rel"),
                Is.EqualTo("noopener noreferrer"));
            Assert.That(compiled.GetAllowedUrlSchemes(),
                Is.EqualTo(new[] { "https", "mailto" }));
            Assert.That(compiled.IsUrlHostAllowed("example.com"), Is.True);
            Assert.That(compiled.IsCssPropertyAllowed("color"), Is.True);
            Assert.That(compiled.GetAllowDataAttributes(), Is.True);
            Assert.That(compiled.GetAllowAriaAttributes(), Is.True);
            Assert.That(compiled.GetDomClobberPrefix(), Is.EqualTo("safe-"));
        });
    }

    [Test]
    public void CopyPreservesIncompleteBuilderStateWithoutSharingMutation()
    {
        HtmlSanitizerPolicy original = new HtmlSanitizerPolicy();
        original.AllowAttributeOnElement("href", "a");

        HtmlSanitizerPolicy copy = original.Copy();
        copy.AllowElement("a");
        copy.AllowElement("em");
        HtmlCompiledSanitizerPolicy compiledCopy = copy.Compile();

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => original.Validate());
            Assert.Throws<InvalidOperationException>(() => original.Compile());
            Assert.That(compiledCopy.IsElementAllowed("a"), Is.True);
            Assert.That(compiledCopy.IsElementAllowed("em"), Is.True);
            Assert.That(compiledCopy.IsAttributeAllowed("href", "a"), Is.True);
        });
    }

    [Test]
    public void CompilationAndCreatePolicyAreIndependentSnapshots()
    {
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("p");
        builder.AllowAttribute("title");
        HtmlCompiledSanitizerPolicy first = builder.Compile();

        builder.AllowElement("strong");
        HtmlCompiledSanitizerPolicy second = builder.Compile();
        HtmlSanitizerPolicy restored = first.CreatePolicy();
        restored.AllowElement("em");
        HtmlCompiledSanitizerPolicy third = restored.Compile();

        string[] snapshot = first.GetAllowedElements();
        snapshot[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(first.GetAllowedElements(), Is.EqualTo(new[] { "p" }));
            Assert.That(first.IsElementAllowed("strong"), Is.False);
            Assert.That(first.IsElementAllowed("em"), Is.False);
            Assert.That(second.GetAllowedElements(),
                Is.EqualTo(new[] { "p", "strong" }));
            Assert.That(third.GetAllowedElements(),
                Is.EqualTo(new[] { "em", "p" }));
            Assert.That(third.IsAttributeAllowed("title", "em"), Is.True);
        });
    }

    [Test]
    public void ElementDecisionCallsUseLastDecisionAndDeduplicateRules()
    {
        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();
        policy.AllowElement("script");
        policy.BlockElement("SCRIPT", HtmlBlockedElementAction.Unwrap);
        policy.BlockElement("script", HtmlBlockedElementAction.EscapeAsText);
        policy.AllowElement("P");
        policy.AllowElement("p");
        policy.AllowAttribute("TITLE");
        policy.AllowAttribute("title");

        HtmlCompiledSanitizerPolicy blocked = policy.Compile();
        policy.AllowElement("script");
        HtmlCompiledSanitizerPolicy allowed = policy.Compile();

        Assert.Multiple(() =>
        {
            Assert.That(blocked.IsElementAllowed("script"), Is.False);
            Assert.That(blocked.GetBlockedElementAction("script"),
                Is.EqualTo(HtmlBlockedElementAction.EscapeAsText));
            Assert.That(blocked.GetAllowedElements(), Is.EqualTo(new[] { "p" }));
            Assert.That(blocked.GetAllowedAttributes(),
                Is.EqualTo(new[] { "title" }));
            Assert.That(allowed.IsElementAllowed("script"), Is.True);
            Assert.That(allowed.IsElementBlocked("script"), Is.False);
        });
    }

    [Test]
    public void InvalidPolicyInputsAndUnresolvedRulesAreRejected()
    {
        HtmlSanitizerPolicy unresolvedAttribute = new HtmlSanitizerPolicy();
        unresolvedAttribute.AllowAttributeOnElement("href", "a");
        HtmlSanitizerPolicy unresolvedForced = new HtmlSanitizerPolicy();
        unresolvedForced.ForceAttribute("a", "rel", "noopener");
        HtmlSanitizerPolicy policy = new HtmlSanitizerPolicy();

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() =>
                unresolvedAttribute.Compile());
            Assert.Throws<InvalidOperationException>(() =>
                unresolvedForced.Compile());
            Assert.Throws<ArgumentNullException>(() => policy.AllowElement(null));
            Assert.Throws<ArgumentException>(() => policy.AllowElement("bad name"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                policy.BlockElement("div", (HtmlBlockedElementAction)99));
            Assert.Throws<ArgumentException>(() =>
                policy.AllowUrlScheme("1https"));
            Assert.Throws<ArgumentException>(() =>
                policy.AllowUrlHost("user@example.com"));
            Assert.Throws<ArgumentException>(() =>
                policy.AllowCssProperty("1color"));
            Assert.Throws<ArgumentException>(() =>
                policy.SetDomClobberPrefix(""));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlSanitizerPolicy.Create(null));
        });
    }

    [Test]
    public void CompiledPolicyRejectsDuplicateOrContradictoryTables()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => CreateCompiled(
                new[] { "p", "P" },
                Array.Empty<HtmlSanitizerBlockedElementRule>()));
            Assert.Throws<ArgumentException>(() => CreateCompiled(
                new[] { "script" },
                new[]
                {
                    new HtmlSanitizerBlockedElementRule(
                        HtmlSanitizerPolicyNames.GetHtmlNamespaceUri(),
                        "script",
                        HtmlBlockedElementAction.DropSubtree)
                }));
        });
    }

    [Test]
    public void CompiledPoliciesCanBeReusedConcurrently()
    {
        HtmlSanitizerPolicy builder = new HtmlSanitizerPolicy();
        builder.AllowElement("a");
        builder.AllowAttributeOnElement("href", "a");
        builder.AllowUrlScheme("https");
        HtmlCompiledSanitizerPolicy compiled = builder.Compile();
        int failures = 0;

        Parallel.For(0, 1000, index =>
        {
            if (!compiled.IsElementAllowed(index % 2 == 0 ? "A" : "a")
                || !compiled.IsAttributeAllowed("HREF", "A")
                || !compiled.IsUrlSchemeAllowed("HTTPS"))
            {
                Interlocked.Increment(ref failures);
            }

            HtmlSanitizerPolicy local = compiled.CreatePolicy();
            local.AllowElement("span");
            if (!local.Compile().IsElementAllowed("span")
                || compiled.IsElementAllowed("span"))
            {
                Interlocked.Increment(ref failures);
            }
        });

        Assert.That(failures, Is.Zero);
    }

    static HtmlCompiledSanitizerPolicy CreateCompiled(string[] allowed,
        HtmlSanitizerBlockedElementRule[] blocked)
    {
        string[] schemes = Array.Empty<string>();
        string[] hosts = Array.Empty<string>();
        return new HtmlCompiledSanitizerPolicy(
            HtmlCompiledSanitizerPolicy.CreateHtmlElementNames(allowed),
            blocked,
            Array.Empty<string>(),
            Array.Empty<HtmlSanitizerElementAttributeRule>(),
            Array.Empty<HtmlSanitizerForcedAttributeRule>(),
            schemes, hosts, Array.Empty<string>(), false, false, "safe-",
            Array.Empty<HtmlSanitizerAttributeRule>(),
            Array.Empty<HtmlSanitizerAttributeValueRule>(),
            HtmlCompiledSanitizerPolicy.CreateDefaultMarkupConfiguration(),
            HtmlCompiledSanitizerPolicy.CreateDefaultUrlPolicy(schemes,
                hosts),
            Array.Empty<HtmlSanitizerUrlAttributeRule>(),
            Array.Empty<HtmlSanitizerAttributeTokenRule>(),
            Array.Empty<string>(), Array.Empty<string>(),
            Array.Empty<string>(), false);
    }
}
