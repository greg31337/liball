using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class PublicSecurityEnforcementTests
{
    [Test]
    public void Renderer_RejectsOversizedTextSourcesAndComposedTemplatesBeforeRendering()
    {
        TemplateOptions options = new TemplateOptions();
        options.MaximumTemplateLength = 30;
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterTemplate("oversized", "1234567890123456789012345678901");
        ITemplateRenderer renderer = CreateRenderer(options, registry, new TemplateMemberResolver());
        TemplateSource source = new TemplateSource("large", null, null, null, "1234567890123456789012345678901");

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString(source.Text, EmptyModel()));
        Assert.Throws<InvalidOperationException>(() => renderer.Preparse(source.Text));
        Assert.Throws<InvalidOperationException>(() => renderer.PreparseSource(source));
        Assert.Throws<InvalidOperationException>(() => renderer.PrecompileSource(source));
        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{% include \"oversized\" %}", EmptyModel()));
    }

    [Test]
    public void StrictModeField_ActivatesMissingCallableConversionAndUnsafeMemberFailures()
    {
        TemplateOptions options = new TemplateOptions();
        options.StrictMode = true;
        ITemplateRenderer renderer = CreateRenderer(options, new TemplateRegistry(), new TemplateMemberResolver());
        TemplateValue objectModel = TemplateValue.FromObject(new PublicSecurityModel());

        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{{ missing }}", EmptyModel()));
        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{{ mystery() }}", EmptyModel()));
        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{{ \"text\" - 1 }}", EmptyModel()));
        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{{ GetType }}", objectModel));
        Assert.That(HasDiagnostic(new TemplateSecurityAuditor().AuditOptions(options), "SECURITY003"), Is.False);
    }

    [Test]
    public void MemberAllowlistAndDenylist_AreEnforcedThroughRendering()
    {
        TemplateSecurityPolicy policy = new TemplateSecurityPolicy();
        policy.AllowedMemberNames.Add("Name");
        policy.AllowedMemberNames.Add("Secret");
        policy.BlockedMemberNames.Add("Secret");
        TemplateMemberResolverOptions resolverOptions = new TemplateMemberResolverOptions();
        resolverOptions.SecurityPolicy = policy;
        TemplateMemberResolver resolver = new TemplateMemberResolver();
        resolver.Configure(resolverOptions);
        TemplateOptions options = new TemplateOptions();
        options.StrictUnsafeMembers = true;
        ITemplateRenderer renderer = CreateRenderer(options, new TemplateRegistry(), resolver);
        TemplateValue model = TemplateValue.FromObject(new PublicSecurityModel());

        Assert.That(renderer.RenderToString("{{ Name }}", model), Is.EqualTo("allowed"));
        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{{ Secret }}", model));
        Assert.Throws<InvalidOperationException>(() => renderer.RenderToString("{{ Other }}", model));
    }

    [Test]
    public void MacroRecursion_IsStoppedAtConfiguredDepth()
    {
        TemplateOptions options = new TemplateOptions();
        options.MaximumRecursionDepth = 2;
        ITemplateRenderer renderer = CreateRenderer(options, new TemplateRegistry(), new TemplateMemberResolver());
        string source = "{% macro recurse value %}{% if value > 0 %}{{ recurse(value - 1) }}{% endif %}{% endmacro %}{{ recurse(3) }}";

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => renderer.RenderToString(source, EmptyModel()))!;

        Assert.That(error.Message, Does.Contain("Maximum recursion depth of 2"));
    }

    [Test]
    public void FileLoaderPolicy_RequiresAnAllowedRootAndRejectsSymlinkEscape()
    {
        string parent = Path.Combine(Path.GetTempPath(), "template-security-public-" + Guid.NewGuid().ToString("N"));
        string root = Path.Combine(parent, "root");
        string outside = Path.Combine(parent, "outside");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(outside);
        File.WriteAllText(Path.Combine(root, "inside.tpl"), "inside");
        File.WriteAllText(Path.Combine(outside, "secret.tpl"), "secret");
        Directory.CreateSymbolicLink(Path.Combine(root, "escape"), outside);
        FileTemplateLoader loader = new FileTemplateLoader(root);
        TemplateSecurityPolicy denied = new TemplateSecurityPolicy();
        denied.AllowedLoaderRoots.Add(outside);
        TemplateSecurityPolicy allowed = new TemplateSecurityPolicy();
        allowed.AllowedLoaderRoots.Add(root + Path.DirectorySeparatorChar.ToString());

        Assert.Throws<InvalidOperationException>(() => loader.SetSecurityPolicy(denied));
        loader.SetSecurityPolicy(allowed);
        Assert.That(loader.LoadRequired("inside.tpl").Text, Is.EqualTo("inside"));
        Assert.Throws<InvalidOperationException>(() => loader.Load("escape/secret.tpl"));
        Assert.Throws<InvalidOperationException>(() => loader.GetTemplateNames());

        Directory.Delete(parent, true);
    }

    [Test]
    public async Task AsyncRender_EnforcesElapsedTimeAfterAwaitedCallableCompletes()
    {
        TemplateRegistry registry = new TemplateRegistry();
        registry.RegisterAsyncFunction("slow", delegate(List<TemplateValue> arguments, CancellationToken cancellationToken)
        {
            return DelayValue(cancellationToken);
        });
        TemplateOptions options = new TemplateOptions();
        options.MaximumRenderMilliseconds = 1;
        ITemplateRenderer renderer = CreateRenderer(options, registry, new TemplateMemberResolver());

        Assert.ThrowsAsync<InvalidOperationException>(async () => await renderer.RenderToStringAsync("{{ slow() }}", EmptyModel()));
    }

    [Test]
    public void SecurityConfiguration_RejectsMissingOrInvalidPolicies()
    {
        TemplateMemberResolverOptions resolverOptions = new TemplateMemberResolverOptions();
        resolverOptions.SecurityPolicy = null!;
        TemplateMemberResolver resolver = new TemplateMemberResolver();
        TemplateSecurityPolicy invalidWriterPolicy = new TemplateSecurityPolicy();
        invalidWriterPolicy.MaximumOutputLength = 0;
        ITemplateWriter writer = new TemplateWriter(new StringWriter());

        Assert.Throws<ArgumentNullException>(() => resolver.Configure(resolverOptions));
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.Configure(invalidWriterPolicy));
    }

    static async Task<TemplateValue> DelayValue(CancellationToken cancellationToken)
    {
        await Task.Delay(20, cancellationToken);
        return TemplateValue.FromString("late");
    }

    static ITemplateRenderer CreateRenderer(TemplateOptions options, TemplateRegistry registry, ITemplateMemberResolver resolver)
    {
        ITemplateRenderer renderer = new TemplateRenderer();
        renderer.Configure(options, registry, resolver);
        return renderer;
    }

    static TemplateValue EmptyModel()
    {
        return TemplateValue.FromMap(new Dictionary<string, TemplateValue>());
    }

    static bool HasDiagnostic(List<TemplateDiagnostic> diagnostics, string code)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index].Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}

public class PublicSecurityModel
{
    public string Name = "allowed";
    public string Secret = "denied";
    public string Other = "not allowlisted";
}
