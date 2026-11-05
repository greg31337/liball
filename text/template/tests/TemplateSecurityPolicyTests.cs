using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateSecurityPolicyTests
{
    [Test]
    public void Constructor_BlocksDangerousAndUnderscoreMemberNamesByDefault()
    {
        TemplateSecurityPolicy policy = new TemplateSecurityPolicy();

        Assert.That(policy.CanAccessMember("Name"), Is.True);
        Assert.That(policy.CanAccessMember("_secret"), Is.False);
        Assert.That(policy.CanAccessMember("__proto__"), Is.False);
        Assert.That(policy.CanAccessMember("constructor"), Is.False);
        Assert.That(policy.CanAccessMember("gettype"), Is.False);
    }

    [Test]
    public void MemberAccessPolicy_CanBeRelaxedAndExtended()
    {
        TemplateSecurityPolicy policy = new TemplateSecurityPolicy();
        policy.AllowDangerousMemberNames = true;
        policy.AllowMemberNamesStartingWithUnderscore = true;

        Assert.That(policy.CanAccessMember("_secret"), Is.True);
        Assert.That(policy.CanAccessMember("constructor"), Is.True);
        policy.AllowDangerousMemberNames = false;
        policy.AddBlockedMemberName("Password");
        policy.AddBlockedMemberName("Password");

        Assert.That(policy.CanAccessMember("password"), Is.False);
        Assert.That(policy.BlockedMemberNames.Count, Is.EqualTo(11));
    }

    [Test]
    public void MemberResolver_HonorsSecurityPolicyForMapsAndReflection()
    {
        Dictionary<string, TemplateValue> values = new Dictionary<string, TemplateValue>();
        values["_secret"] = TemplateValue.FromString("hidden");
        values["safe"] = TemplateValue.FromString("shown");
        values["constructor"] = TemplateValue.FromString("unsafe");
        TemplateValue map = TemplateValue.FromMap(values);
        TemplateMemberResolver resolver = new TemplateMemberResolver();
        TemplateValue sample = TemplateValue.FromObject(new UnsafeReflectionSample());

        AssertString(resolver.ResolveMember(map, "safe"), "shown");
        Assert.That(resolver.ResolveMember(map, "_secret").IsMissing(), Is.True);
        Assert.That(resolver.ResolveMember(map, "constructor").IsMissing(), Is.True);
        Assert.That(resolver.ResolveMember(sample, "_Secret").IsMissing(), Is.True);
        Assert.That(resolver.ResolveMember(sample, "prototype").IsMissing(), Is.True);
    }

    [Test]
    public void MemberResolver_BlocksDangerousReflectedMethodsEvenWhenAllowlisted()
    {
        TemplateMemberResolverOptions options = new TemplateMemberResolverOptions();
        options.AllowPublicMethods = true;
        options.AddMemberNamingPolicy(TemplateMemberNamingPolicy.CamelCase);
        options.AddSafeMethod("GetType");
        TemplateMemberResolver resolver = TemplateMemberResolver.Create(options);

        Assert.That(resolver.ResolveMember(TemplateValue.FromObject(new UnsafeReflectionSample()), "getType").IsMissing(), Is.True);
    }

    [Test]
    public void LoaderRoots_AllowOnlyNormalizedPathsInsideConfiguredRoots()
    {
        TemplateSecurityPolicy policy = new TemplateSecurityPolicy();
        string root = Path.Combine(Path.GetTempPath(), "template-security-root");
        string nested = Path.Combine(root, "views", "index.tpl");
        string escaped = Path.Combine(root, "..", "outside.tpl");
        string sibling = root + "-sibling";

        Assert.That(policy.IsLoaderPathAllowed(nested), Is.False);

        policy.AddLoaderRoot(root);
        policy.AddLoaderRoot(root);

        Assert.That(policy.AllowedLoaderRoots.Count, Is.EqualTo(1));
        Assert.That(policy.IsLoaderPathAllowed(root), Is.True);
        Assert.That(policy.IsLoaderPathAllowed(nested), Is.True);
        Assert.That(policy.IsLoaderPathAllowed(escaped), Is.False);
        Assert.That(policy.IsLoaderPathAllowed(sibling), Is.False);
    }

    [Test]
    public void QuotaChecks_UseConfiguredMaximumsAndRejectNegativeMeasurements()
    {
        TemplateSecurityPolicy policy = new TemplateSecurityPolicy();
        policy.MaximumTemplateLength = 10;
        policy.MaximumOutputLength = 20;
        policy.MaximumIncludeDepth = 2;
        policy.MaximumLoopIterations = 3;
        policy.MaximumExpressionDepth = 4;
        policy.MaximumTemplateCount = 5;
        policy.MaximumRenderMilliseconds = 6;
        policy.MaximumRecursionDepth = 7;

        Assert.That(policy.IsTemplateLengthAllowed(10), Is.True);
        Assert.That(policy.IsTemplateLengthAllowed(11), Is.False);
        Assert.That(policy.IsOutputLengthAllowed(20), Is.True);
        Assert.That(policy.IsOutputLengthAllowed(21), Is.False);
        Assert.That(policy.IsIncludeDepthAllowed(2), Is.True);
        Assert.That(policy.IsIncludeDepthAllowed(3), Is.False);
        Assert.That(policy.IsLoopIterationAllowed(3), Is.True);
        Assert.That(policy.IsLoopIterationAllowed(4), Is.False);
        Assert.That(policy.IsExpressionDepthAllowed(4), Is.True);
        Assert.That(policy.IsExpressionDepthAllowed(5), Is.False);
        Assert.That(policy.IsTemplateCountAllowed(5), Is.True);
        Assert.That(policy.IsTemplateCountAllowed(6), Is.False);
        Assert.That(policy.IsRenderTimeAllowed(6), Is.True);
        Assert.That(policy.IsRenderTimeAllowed(7), Is.False);
        Assert.That(policy.IsRecursionDepthAllowed(7), Is.True);
        Assert.That(policy.IsRecursionDepthAllowed(8), Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(() => policy.IsTemplateLengthAllowed(-1));
    }

    [Test]
    public void RenderTimeQuota_ZeroMaximumMeansUnlimited()
    {
        TemplateSecurityPolicy policy = new TemplateSecurityPolicy();

        Assert.That(policy.MaximumRenderMilliseconds, Is.EqualTo(0));
        Assert.That(policy.IsRenderTimeAllowed(1000000), Is.True);
    }

    [Test]
    public void FromOptionsAndApplyOptions_CopyRuntimeQuotas()
    {
        TemplateOptions options = new TemplateOptions();
        options.MaximumTemplateLength = 11;
        options.MaximumOutputLength = 12;
        options.MaximumIncludeDepth = 13;
        options.MaximumLoopIterations = 14;
        options.MaximumExpressionDepth = 15;
        options.MaximumTemplateCount = 16;
        options.MaximumRenderMilliseconds = 17;

        TemplateSecurityPolicy policy = TemplateSecurityPolicy.FromOptions(options);

        Assert.That(policy.MaximumTemplateLength, Is.EqualTo(11));
        Assert.That(policy.MaximumOutputLength, Is.EqualTo(12));
        Assert.That(policy.MaximumIncludeDepth, Is.EqualTo(13));
        Assert.That(policy.MaximumLoopIterations, Is.EqualTo(14));
        Assert.That(policy.MaximumExpressionDepth, Is.EqualTo(15));
        Assert.That(policy.MaximumTemplateCount, Is.EqualTo(16));
        Assert.That(policy.MaximumRenderMilliseconds, Is.EqualTo(17));
    }

    [Test]
    public void ValidateAndMutators_RejectInvalidInputs()
    {
        TemplateSecurityPolicy policy = new TemplateSecurityPolicy();
        TemplateSecurityPolicy invalid = new TemplateSecurityPolicy();
        invalid.MaximumRecursionDepth = 0;

        Assert.Throws<ArgumentNullException>(() => TemplateSecurityPolicy.FromOptions(null!));
        Assert.Throws<ArgumentNullException>(() => policy.ApplyOptions(null!));
        Assert.Throws<ArgumentNullException>(() => policy.AddBlockedMemberName(null!));
        Assert.Throws<ArgumentNullException>(() => policy.CanAccessMember(null!));
        Assert.Throws<ArgumentNullException>(() => policy.IsBlockedMemberName(null!));
        Assert.Throws<ArgumentNullException>(() => policy.AddLoaderRoot(null!));
        Assert.Throws<ArgumentException>(() => policy.AddLoaderRoot(""));
        Assert.Throws<ArgumentNullException>(() => policy.IsLoaderPathAllowed(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => invalid.Validate());
    }

    static void AssertString(TemplateValue value, string expected)
    {
        Assert.That(value.Kind, Is.EqualTo(TemplateValueKind.String));
        Assert.That(value.StringValue, Is.EqualTo(expected));
    }
}

public class UnsafeReflectionSample
{
    public string _Secret = "hidden";

    public string Prototype
    {
        get
        {
            return "unsafe";
        }
    }
}
